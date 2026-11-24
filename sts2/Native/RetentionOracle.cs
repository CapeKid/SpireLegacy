using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Only invoked by the opt-in owned-game bridge.
public static class RetentionOracle
{
    public static async Task<object> QueenBinding(RunState run, string? cleanse)
    {
        await RunManager.Instance.EnterRoomDebug(RoomType.Boss, model: ModelDb.Encounter<QueenBoss>().ToMutable(), showTransition: false);
        RunManager.Instance.ActionExecutor.Unpause();
        var player = run.Players.Single();
        var owner = player.Creature;
        var combat = owner.CombatState!;
        var tree = (SceneTree)Engine.GetMainLoop();
        var deadline = Time.GetTicksMsec() + 20000;
        while (player.PlayerCombatState?.Phase != PlayerTurnPhase.Play)
        {
            if (Time.GetTicksMsec() > deadline) throw new TimeoutException("Queen combat did not start.");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        var hand = PileType.Hand.GetPile(player);
        foreach (var card in hand.Cards.ToArray())
            await CardPileCmd.Add(card, PileType.Discard, skipVisuals: true);

        for (var i = 0; i < 12; i++)
            await CardPileCmd.Add(combat.CreateCard(NativeCards.Get("mage_guard"), player), PileType.Draw, skipVisuals: true);

        var cases = new (CardModel Model, bool Upgrade, bool Retain)[]
        {
            (NativeCards.Get("mage_warding_verse"), false, true),
            (NativeCards.Get("mage_warding_verse"), true, true),
            (NativeCards.Get(cleanse is null ? "mage_crystal_seed" : "mage_cold_read"), false, false),
            (NativeCards.Get("mage_hourglass"), true, true),
            (ModelDb.Card<DefendIronclad>(), false, true),
            (NativeCards.Get("mage_guard"), false, false),
        };
        var originals = new List<CardModel>();
        foreach (var (model, upgrade, retain) in cases.Reverse())
        {
            var card = combat.CreateCard(model, player);
            if (upgrade) CardCmd.Upgrade(card);
            if (retain && card is not LegacyCard) card.AddKeyword(CardKeyword.Retain);
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top, skipVisuals: true);
            originals.Add(card);
        }
        originals.Reverse();
        await BalanceOracle.End(player, true);
        if (owner.GetPower<ChainsOfBindingPower>() is null
            || originals.Take(3).Any(card => !hand.Cards.Contains(card) || card.Affliction is not Bound))
            throw new InvalidDataException("The Queen did not bind the first three drawn cards.");
        foreach (var card in originals.Skip(3))
        {
            if (!hand.Cards.Contains(card)) await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            if (card.Affliction is null) await CardCmd.Afflict<Bound>(card, 1);
        }

        var active = Design.Rows("card_pools").Concat(Design.Rows("branch_cards"))
            .Select(row => row.Text("cardId")).Distinct();
        var offHand = new List<CardModel>();
        foreach (var key in active)
        {
            if (!new CardDesign(key, true).Retain) continue;
            foreach (var upgraded in new[] { false, true })
            {
                if (!new CardDesign(key, upgraded).Retain) continue;
                var card = combat.CreateCard(NativeCards.Get(key), player);
                if (upgraded) CardCmd.Upgrade(card);
                await CardPileCmd.Add(card, PileType.Discard, skipVisuals: true);
                await CardCmd.Afflict<Bound>(card, 1);
                if (card.Affliction is not Bound) throw new InvalidDataException("Could not bind " + card.Id);
                offHand.Add(card);
            }
        }

        string? VisibleText(CardModel card)
        {
            var view = NativeTestBridge.Descendants(tree.Root).OfType<NCard>()
                .FirstOrDefault(node => node.IsVisibleInTree() && ReferenceEquals(node.Model, card));
            return view is not null && AccessTools.Field(typeof(NCard), "_descriptionLabel").GetValue(view) is RichTextLabel label
                ? label.GetParsedText() : null;
        }
        var before = originals.Select(VisibleText).ToArray();
        if (cleanse is null) await BalanceOracle.End(player, true);
        else
        {
            var sigil = combat.CreateCard(NativeCards.Get(cleanse), player);
            await CardPileCmd.Add(sigil, PileType.Hand);
            if (!sigil.TryManualPlay(null)) throw new InvalidDataException("Cleanse refused paid native play.");
            var cleanseDeadline = Time.GetTicksMsec() + 10000;
            while (sigil.Pile?.Type is PileType.Hand or PileType.Play && Time.GetTicksMsec() < cleanseDeadline)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (owner.GetPower<ChainsOfBindingPower>() is not null) throw new InvalidDataException("Cleanse did not remove Chains.");
        }
        var results = originals.Select((card, i) => new { id = card.Id.Entry, upgraded = cases[i].Upgrade, retained = hand.Cards.Contains(card),
            bound = card.Affliction is Bound, playable = card.CanPlay(), rules = card.GetDescriptionForPile(PileType.Hand), before = before[i], after = VisibleText(card) }).ToArray();
        return new { cleanse, results, offHand = offHand.Count, stillBoundOffHand = offHand.Where(card => card.Affliction is Bound).Select(card => card.Id.Entry).ToArray() };
    }

    // Exercise native end-turn flushing and the retention selection together.
    public static async Task<object> Check(RunState run,bool upgrade,bool stack,bool intrinsic,bool fullHand)
    {
        var player=run.Players.Single();var combat=player.Creature.CombatState!;
        var tree=(SceneTree)Engine.GetMainLoop();var hand=PileType.Hand.GetPile(player);
        async Task PlayBreath(bool upgraded)
        {
            var card=combat.CreateCard(NativeCards.Get("ranger_measured_breath"),player);
            if(upgraded)CardCmd.Upgrade(card);
            await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
            if(!card.TryManualPlay(null))throw new InvalidDataException("Measured Breath refused paid native play.");
            var deadline=Time.GetTicksMsec()+15000;
            while(card.Pile?.Type is PileType.Hand or PileType.Play && Time.GetTicksMsec()<deadline)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            if(card.Pile?.Type is PileType.Hand or PileType.Play)throw new TimeoutException("Measured Breath did not resolve.");
        }
        var energy=player.PlayerCombatState!.Energy;
        await PlayBreath(upgrade);if(stack)await PlayBreath(false);
        var power=player.Creature.GetPower<P_well_laid>()??throw new InvalidDataException("Missing custom retention power.");
        var amount=(upgrade?2:1)+(stack?1:0);var energySpent=energy-player.PlayerCombatState.Energy;
        if(power.Amount!=amount||player.PlayerCombatState.Energy!=energy-(stack?2:1))throw new InvalidDataException("Wrong Measured Breath amount/cost.");
        // Keep the draw pile full so previously discarded fixture cards cannot be
        // shuffled and redrawn, which would obscure whether retain has expired.
        foreach(var old in hand.Cards.ToArray())await CardPileCmd.Add(old,PileType.Discard,skipVisuals:true);
        for(var i=0;i<30;i++)await CardPileCmd.Add(combat.CreateCard(NativeCards.Get(Runtime.Heir.classId+"_guard"),player),PileType.Draw,skipVisuals:true);
        var originals=new List<CardModel>();
        for(var i=0;i<5;i++) {
            var card=combat.CreateCard(NativeCards.Get(Runtime.Heir.classId+"_guard"),player);
            await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);originals.Add(card);
        }
        CardModel? retained=null;
        if(intrinsic) {
            var row=Design.Rows("cards").First(r=>r.Flag("retain"));
            retained=combat.CreateCard(NativeCards.Get(row.Text("id")),player);
            await CardPileCmd.Add(retained,PileType.Hand,skipVisuals:true);
        }
        if(fullHand)await CardEffects.Apply(new BlockingPlayerChoiceContext(),"equilibrium",player.Creature,1,player.Creature,null);
        var selector=new RetentionSelector(amount);
        async Task EndTurn()
        {
            await CreatureCmd.GainBlock(player.Creature,999,ValueProp.Unpowered,null);
            var turn=player.PlayerCombatState.TurnNumber;
            CombatManager.Instance.SetReadyToEndTurn(player,false);
            var deadline=Time.GetTicksMsec()+20000;
            while((player.PlayerCombatState.Phase!=PlayerTurnPhase.Play||player.PlayerCombatState.TurnNumber<=turn)&&Time.GetTicksMsec()<deadline)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            if(player.PlayerCombatState.TurnNumber<=turn||player.PlayerCombatState.Phase!=PlayerTurnPhase.Play)throw new TimeoutException("Retention turn did not finish.");
        }
        var use=AccessTools.Method(typeof(CardSelectCmd),"UseSelector",[typeof(ICardSelector),typeof(bool)])??AccessTools.Method(typeof(CardSelectCmd),"UseSelector",[typeof(ICardSelector)]);
        using((IDisposable)use.Invoke(null,use.GetParameters().Length==2?[selector,false]:[selector])!) {
            await EndTurn();
            var first=originals.Count(c=>hand.Cards.Contains(c));
            if(first!=(fullHand?5:amount)||(!fullHand&&(selector.Calls!=1||selector.Maximum!=amount||selector.Minimum!=0)))throw new InvalidDataException("Incorrect bounded retention: "+first);
            if(retained!=null&&(!hand.Cards.Contains(retained)||selector.Options.Contains(retained)))throw new InvalidDataException("Intrinsic Retain consumed a choice.");
            if(originals.Any(c=>c.Keywords.Contains(CardKeyword.Retain)||c.ShouldRetainThisTurn))throw new InvalidDataException("Temporary Retain survived turn cleanup.");
            var firstCalls=selector.Calls;selector.Choose=0;
            await EndTurn();
            var second=originals.Count(c=>hand.Cards.Contains(c));
            if(second!=0||selector.Calls!=firstCalls+1||power.Amount!=amount)throw new InvalidDataException("Unselected cards retained permanently.");
            if(retained!=null&&!hand.Cards.Contains(retained))throw new InvalidDataException("Intrinsic Retain lost on next turn.");
            return new{upgrade,stack,intrinsic,fullHand,amount,energySpent,firstRetained=first,secondRetained=second,firstCalls,selectionCalls=selector.Calls,selector.Minimum,selector.Maximum,title=power.Title.GetFormattedText(),description=power.Description.GetFormattedText(),powerId=power.Id.ToString()};
        }
    }
    private sealed class RetentionSelector(int choose):ICardSelector
    {
        public int Choose=choose,Calls,Minimum,Maximum;
        public CardModel[] Options=[];
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options,int minSelect,int maxSelect)
        {Calls++;Minimum=minSelect;Maximum=maxSelect;Options=options.ToArray();return Task.FromResult(Options.Take(Math.Min(Choose,maxSelect)).AsEnumerable());}
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,IReadOnlyList<CardRewardAlternative> alternatives)=>new(){card=options.FirstOrDefault()?.Card};
    }
}
