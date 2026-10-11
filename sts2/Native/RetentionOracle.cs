using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Only invoked by the opt-in owned-game bridge. Exercise native end-turn flushing,
// not just a direct call to the new power's selection hook.
public static class RetentionOracle
{
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
