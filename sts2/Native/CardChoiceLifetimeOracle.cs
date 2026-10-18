using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace SpireLegacy;

// Opt-in fault fixture: reproduce a late hand callback against a replacement
// synchronizer. This controls the race; it is not a claim of Deck reproduction.
public static class CardChoiceLifetimeOracle
{
    public static async Task<object> Check(RunState run,bool upgraded,string mode)
    {
        var manager=RunManager.Instance;var player=run.Players.Single();
        await SaveManager.Instance.SaveRun(null);
        var saved=SaveManager.Instance.LoadRunSave();
        if(!saved.Success||saved.SaveData==null)throw new InvalidDataException("Missing fixture save.");
        var shadow=RunState.FromSerializable(saved.SaveData);
        using var replacement=new PlayerChoiceSynchronizer(new NetSingleplayerGameService(),shadow);
        var original=manager.PlayerChoiceSynchronizer;
        var combat=player.Creature.CombatState!;
        foreach(var old in player.PlayerCombatState!.Hand.Cards.ToArray())await CardPileCmd.Add(old,PileType.Draw,skipVisuals:true);
        for(var i=0;i<5;i++)await CardPileCmd.Add(combat.CreateCard(NativeCards.Get("ranger_guard"),player),PileType.Hand,skipVisuals:true);
        var card=(LegacyCard)combat.CreateCard(NativeCards.Get("mage_cold_read"),player);
        if(upgraded)CardCmd.Upgrade(card);
        await CardPileCmd.Add(card,PileType.Play,skipVisuals:true);
        // Beta adds a required Player property; construct the common fixture
        // reflectively so the same oracle binary also loads on regular.
        var play=Activator.CreateInstance<CardPlay>();
        foreach(var pair in new Dictionary<string,object?>{{"Card",card},{"Player",player},{"Target",null},{"ResultPile",PileType.Discard},{"Resources",new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0}},{"IsAutoPlay",false},{"PlayIndex",0},{"PlayCount",1}})
            typeof(CardPlay).GetProperty(pair.Key)?.SetValue(play,pair.Value);
        var before=CardPlayDiagnostics.LastFailure;
        var pending=CardPlayDiagnostics.Observe(CardEffects.Execute(card,new BlockingPlayerChoiceContext(),play),card.Key,player);
        var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+10000;
        while(!NPlayerHand.Instance!.IsInCardSelection&&!pending.IsCompleted){if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Choice did not open.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
        Exception? failure=null;
        try {
            if(mode is "changed-sync" or "active-sync-error") {
                AccessTools.Property(typeof(RunManager),nameof(RunManager.PlayerChoiceSynchronizer)).SetValue(manager,replacement);
                if(mode=="changed-sync")AccessTools.Property(typeof(RunManager),"State").SetValue(manager,shadow);
            }
            else if(mode=="ended-run")AccessTools.Property(typeof(RunManager),"State").SetValue(manager,null);
            else if(mode!="valid")throw new ArgumentException("Unknown choice lifetime mode.");
            var selected=mode=="valid"?player.PlayerCombatState.Hand.Cards.Take(upgraded?2:1).ToArray():[];
            // Match NPlayerHand._ExitTree's completion when the UI goes away.
            var completion=(TaskCompletionSource<IEnumerable<MegaCrit.Sts2.Core.Models.CardModel>>)AccessTools.Field(typeof(NPlayerHand),"_selectionCompletionSource").GetValue(NPlayerHand.Instance)!;
            completion.TrySetResult(selected);
            while(!pending.IsCompleted){if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Choice did not finish.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
            try{await pending;}catch(Exception error){failure=error;}
        }finally {
            AccessTools.Property(typeof(RunManager),"State").SetValue(manager,run);
            AccessTools.Property(typeof(RunManager),nameof(RunManager.PlayerChoiceSynchronizer)).SetValue(manager,original);
        }
        // The global patch must leave unrelated native synchronization errors visible.
        var unscopedRejected=false;
        try{replacement.SyncLocalChoice(player,0,PlayerChoiceResult.FromIndexes([]));}
        catch(ArgumentOutOfRangeException){unscopedRejected=true;}
        return new{upgraded,mode,error=failure?.GetType().Name,cancelled=failure is OperationCanceledException,stack=failure?.StackTrace,diagnosticChanged=CardPlayDiagnostics.LastFailure!=before,unscopedRejected};
    }
}
