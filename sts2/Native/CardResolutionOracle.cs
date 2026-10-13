using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Opt-in owned-game fixture. Real paid actions and native card scopes are the oracle.
public static class CardResolutionOracle
{
    private static readonly PileType[] Piles=[PileType.Draw,PileType.Hand,PileType.Discard,PileType.Exhaust,PileType.Play];
    private static void Require(bool value,string message) {if(!value)throw new InvalidDataException(message);}
    private static async Task Wait(Func<bool> done,string what)
    {
        var tree=(SceneTree)Engine.GetMainLoop();var until=Time.GetTicksMsec()+20000;
        while(!done()) {if(Time.GetTicksMsec()>until)throw new TimeoutException(what);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
    }
    public static async Task<object> Play(RunState run,string key,bool upgraded,string mode,IDisposable selector)
    {
        using(selector) {
            var player=run.Players.Single();var creature=player.Creature;var combat=creature.CombatState!;
            var context=new BlockingPlayerChoiceContext();var native=key.StartsWith("native:");
            var row=native?null:Design.Get("cards",key);
            var old=CardPile.GetCards(player,Piles).ToArray();await CardPileCmd.RemoveFromCombat(old,skipVisuals:true);
            var enemies=combat.GetOpponentsOf(creature).Where(e=>e.IsAlive).ToArray();
            Require(enemies.Length>0,"Fixture needs a live native enemy.");
            foreach(var enemy in enemies) {
                foreach(var power in enemy.Powers.ToArray())await PowerCmd.Remove(power);
                enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(mode=="lethal"?1:mode=="zero-enemies"?0:10000);
                enemy.LoseBlockInternal(enemy.Block);
            }
            creature.SetCurrentHpInternal(mode=="self-death"?1:creature.MaxHp-20);
            creature.LoseBlockInternal(creature.Block);
            async Task<LegacyCard> Add(string id,PileType pile)
            {
                var item=(LegacyCard)combat.CreateCard(NativeCards.Get(id),player);
                await CardPileCmd.Add(item,pile,skipVisuals:true);return item;
            }
            var guard=PlayerBloodlines.Heir(player).classId+"_guard";
            if(mode!="empty") {
                for(var i=0;i<(mode=="full"?9:3);i++)await Add(guard,PileType.Hand);
                for(var i=0;i<20;i++)await Add(guard,PileType.Draw);
                await Add(guard,PileType.Discard);await Add(guard,PileType.Exhaust);
            }
            var canonical=native?ModelDb.GetById<CardModel>(ModelDb.GetId(typeof(CardModel).Assembly.GetType("MegaCrit.Sts2.Core.Models.Cards."+key[7..],true)!)):NativeCards.Get(key);
            var card=combat.CreateCard(canonical,player);
            if(upgraded)CardCmd.Upgrade(card);
            if(mode=="save-copy") {
                var loaded=run.LoadCard(card.ToSerializable(),player);var copy=combat.CloneCard(loaded);run.RemoveCard(loaded);combat.RemoveCard(card);card=copy;
            }
            if(mode=="dupe") {
                // Native cloning chooses its scope from current pile membership.
                await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
                var method=AccessTools.Method(typeof(CardModel),"CreateDupe");
                card=(CardModel)method.Invoke(card,method.GetParameters().Select(p=>p.ParameterType==typeof(Player)?player:p.HasDefaultValue?p.DefaultValue:throw new MissingMethodException("Unknown dupe parameter.")).ToArray())!;
            }
            if(mode=="replay")await PowerCmd.Apply(context,LegacyPowers.Get(card.Type==CardType.Attack?"double_tap":"burst").ToMutable(),creature,1,creature,null);
            if(mode=="discount")card.SetToFreeThisTurn();
            await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
            await PlayerCmd.GainEnergy(20,player);
            var countBefore=CardPile.GetCards(player,Piles).Count();
            var hp=creature.CurrentHp;var block=creature.Block;var energy=player.PlayerCombatState!.Energy;
            var enemyHp=enemies.Select(e=>e.CurrentHp).ToArray();
            var cost=card.EnergyCost.GetWithModifiers(CostModifiers.All);var failure=CardPlayDiagnostics.LastFailure;
            var target=card.TargetType==TargetType.AnyEnemy?enemies[0]:null;
            if(mode=="invalid") {
                Require(card.TargetType==TargetType.AnyEnemy,"Invalid-target fixture needs a targeted card.");
                enemies[0].SetCurrentHpInternal(0);
                Require(!card.TryManualPlay(enemies[0])&&!card.TryManualPlay(null),"Invalid targeted action accepted.");
                enemies[0].SetCurrentHpInternal(enemyHp[0]);
                Require(card.Pile?.Type==PileType.Hand&&player.PlayerCombatState.Energy==energy,"Rejected play spent energy or moved its card.");
                return new {key,upgraded,mode,rejected=true,cost,pile="Hand",conserved=true};
            }
            Require(card.TryManualPlay(target),"Native paid play refused "+key+" / "+mode);
            await Wait(()=>!creature.IsAlive||card.Pile?.Type is not (PileType.Hand or PileType.Play),"Paid card unresolved: "+key);
            await Wait(()=>!creature.IsAlive||NCard.FindOnTable(card)==null,"Card remains hovering: "+key);
            await Wait(()=>!RunManager.Instance.ActionExecutor.IsRunning,"Native card action did not complete: "+key);
            Require(CardPlayDiagnostics.LastFailure==failure,"Paid action raised a new exception: "+CardPlayDiagnostics.LastFailure);
            if(!creature.IsAlive||!enemies.Any(e=>e.IsAlive))return new {key,upgraded,mode,terminal=true,alive=creature.IsAlive,pile=card.Pile?.Type.ToString(),newFailure=false};
            var actual=CardPile.GetCards(player,Piles).ToArray();
            Require(actual.Distinct().Count()==actual.Length,"Card appears in multiple native piles.");
            Require(actual.All(c=>c.Owner==player&&combat.ContainsCard(c)&&c.Pile!.Cards.Count(x=>ReferenceEquals(x,c))==1),"Card scope/owner/pile disagreement.");
            var expectedPile=card.Type==CardType.Power||card.IsDupe?PileType.None:card.Keywords.Contains(CardKeyword.Exhaust)?PileType.Exhaust:PileType.Discard;
            Require((card.Pile?.Type??PileType.None)==expectedPile,"Wrong result pile: "+key);
            // Native RemoveFromCombat removes pile membership, retaining the model
            // in combat scope until cleanup (including native Powers and dupes).
            Require(expectedPile!=PileType.None||!actual.Contains(card),"Removed card remains in a native pile.");
            var plays=mode=="replay"?2:1;
            var design=(card as LegacyCard)?.DesignCard;
            var generated=row?.Text("special")=="wounds"?design!.SpecialAmount*plays:0;
            Require(actual.Length==countBefore+generated-(expectedPile==PileType.None?1:0),"Native card count was not conserved: "+key);
            Require(player.PlayerCombatState.Energy-energy==-cost+(design?.Amount("energy")??0)*plays,"Paid energy mismatch: "+key);
            var saved=card.ToSerializable();var restored=run.LoadCard(saved,player);
            Require(restored.Id==card.Id&&restored.IsUpgraded==upgraded,"Resolved card identity did not round-trip.");
            // Native SerializableCard excludes transient dupe/keyword overrides.
            // Dupes are removed from play, never promised as durable deck copies.
            if(!card.IsDupe)Require(restored.GetDescriptionForPile(PileType.Deck)==card.GetDescriptionForPile(PileType.Deck),"Resolved card text did not round-trip.");
            else if(card is LegacyCard legacy)Require(((LegacyCard)restored).DesignCard.Rules()==legacy.DesignCard.Rules(),"Dupe underlying saved design differs.");
            run.RemoveCard(restored);
            return new {key,upgraded,mode,cost,pile=expectedPile.ToString(),energyDelta=player.PlayerCombatState.Energy-energy,hpDelta=creature.CurrentHp-hp,blockDelta=creature.Block-block,
                damage=enemies.Select((e,i)=>enemyHp[i]-e.CurrentHp).ToArray(),hand=player.PlayerCombatState.Hand.Cards.Count,cardCount=actual.Length,generated,conserved=true,saveRoundTrip=true,tableNodePresent=false};
        }
    }
}
