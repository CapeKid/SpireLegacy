using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace SpireLegacy;

// Test-only: hold a legal native selector while exercising the actual save boundary.
public static class CardChoiceBoundaryOracle
{
    private sealed class Held : ICardSelector
    {
        public bool Requested;public CardModel[] Options=[];public int Min,Max;
        private readonly TaskCompletionSource<IEnumerable<CardModel>> completion=new();
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options,int minSelect,int maxSelect)
        {Options=options.ToArray();Min=minSelect;Max=maxSelect;Requested=true;return completion.Task;}
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,IReadOnlyList<CardRewardAlternative> alternatives)=>new(){card=options.FirstOrDefault()?.Card};
        public void Complete(bool decline) {if(decline&&Min!=0)throw new InvalidDataException("Choice cannot legally be declined.");completion.TrySetResult(Options.Take(decline?0:Math.Min(Max,Math.Max(1,Min))));}
    }
    private static async Task Wait(Func<bool> done)
    {var tree=(SceneTree)Engine.GetMainLoop();var until=Time.GetTicksMsec()+20000;while(!done()){if(Time.GetTicksMsec()>until)throw new TimeoutException("Native held-choice boundary.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}}
    private static void Require(bool value,string text){if(!value)throw new InvalidDataException(text);}
    public static async Task<object> Check(RunState run,string key,bool upgraded,bool decline)
    {
        var player=run.Players.Single();var combat=player.Creature.CombatState!;var held=new Held();
        var method=AccessTools.Method(typeof(CardSelectCmd),"UseSelector",[typeof(ICardSelector),typeof(bool)])??AccessTools.Method(typeof(CardSelectCmd),"UseSelector",[typeof(ICardSelector)]);
        using var scope=(IDisposable)method.Invoke(null,method.GetParameters().Length==2?[held,false]:[held])!;
        try {
            var guard=PlayerBloodlines.Heir(player).classId+"_guard";
            foreach(var old in player.PlayerCombatState!.Hand.Cards.ToArray())await CardPileCmd.Add(old,PileType.Draw,skipVisuals:true);
            for(var i=0;i<5;i++) {
                await CardPileCmd.Add(combat.CreateCard(NativeCards.Get(guard),player),PileType.Draw,skipVisuals:true);
                await CardPileCmd.Add(combat.CreateCard(NativeCards.Get(guard),player),PileType.Hand,skipVisuals:true);
                await CardPileCmd.Add(combat.CreateCard(NativeCards.Get(guard),player),PileType.Discard,skipVisuals:true);
                await CardPileCmd.Add(combat.CreateCard(NativeCards.Get(guard),player),PileType.Exhaust,skipVisuals:true);
            }
            var canonical=key.StartsWith("native:")?ModelDb.GetById<CardModel>(ModelDb.GetId(typeof(CardModel).Assembly.GetType("MegaCrit.Sts2.Core.Models.Cards."+key[7..],true)!)):NativeCards.Get(key);
            var card=combat.CreateCard(canonical,player);if(upgraded)CardCmd.Upgrade(card);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
            var deck=player.Deck.Cards.Select(c=>c.Id.ToString()+":"+c.CurrentUpgradeLevel).ToArray();
            await PlayerCmd.GainEnergy(10,player);var energy=player.PlayerCombatState!.Energy;var cost=card.EnergyCost.GetWithModifiers(CostModifiers.All);var failure=CardPlayDiagnostics.LastFailure;
            Require(card.TryManualPlay(card.TargetType==TargetType.AnyEnemy?combat.GetOpponentsOf(player.Creature).First(e=>e.IsAlive):null),"Held-choice card refused.");
            await Wait(()=>held.Requested);
            Require(card.Pile?.Type==PileType.Play&&player.PlayerCombatState.Energy==energy-cost,"Held card not pending after one payment.");
            await SaveManager.Instance.SaveRun(null);
            var save=SaveManager.Instance.LoadRunSave();Require(save.Success&&save.SaveData!=null,"Save during choice failed.");
            var saved=save.SaveData!.Players.Single(p=>p.NetId==player.NetId);
            Require(saved.Deck.Select(c=>c.Id?.ToString()+":"+c.CurrentUpgradeLevel).SequenceEqual(deck),"Pending choice lost or added a durable deck card.");
            held.Complete(decline);
            await Wait(()=>card.Pile?.Type is not (PileType.Hand or PileType.Play));await Wait(()=>NCard.FindOnTable(card)==null&&!RunManager.Instance.ActionExecutor.IsRunning);
            Require(CardPlayDiagnostics.LastFailure==failure,"Held-choice paid action failed.");
            Require(player.Deck.Cards.Select(c=>c.Id.ToString()+":"+c.CurrentUpgradeLevel).SequenceEqual(deck),"Resolved choice changed durable deck identity.");
            return new {key,upgraded,decline,held.Min,held.Max,options=held.Options.Length,pile=card.Pile?.Type.ToString(),cost,deck,saveDuringChoice=true,deckPreserved=true,tableNodePresent=false};
        }finally{held.Complete(false);}
    }
}
