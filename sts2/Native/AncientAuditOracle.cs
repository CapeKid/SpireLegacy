using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace SpireLegacy;

// Disposable owned-game profiles only; exposed exclusively through the test bridge.
public static class AncientAuditOracle
{
    private static void Require(bool ok,string message){if(!ok)throw new InvalidDataException(message);}
    private static async Task Wait(Func<bool> done){var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+25000;while(!done()){if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Ancient audit native transition.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}}
    private static EventModel Event(RunState run)=>((EventRoom)run.CurrentRoom!).LocalMutableEvent;
    public static async Task<object> Advance(RunState run,bool controller)
    {
        var layout=NativeTestBridge.Descendants(((SceneTree)Engine.GetMainLoop()).Root).OfType<NAncientEventLayout>().Single(l=>l.IsVisibleInTree());
        var hitbox=(NAncientDialogueHitbox)AccessTools.Field(typeof(NAncientEventLayout),"_dialogueHitbox").GetValue(layout)!;
        var before=(int)AccessTools.Field(typeof(NAncientEventLayout),"_currentDialogueLine").GetValue(layout)!;
        Require(before<3,"Dialogue already finished.");
        if(controller){var hotkey=hitbox.GetHotkey()??throw new InvalidDataException("Missing native dialogue hotkey.");hitbox.GrabFocus();Input.ParseInputEvent(new InputEventAction{Action=hotkey,Pressed=true,Strength=1});await ((SceneTree)Engine.GetMainLoop()).ToSignal((SceneTree)Engine.GetMainLoop(),SceneTree.SignalName.ProcessFrame);Input.ParseInputEvent(new InputEventAction{Action=hotkey,Pressed=false});}
        else hitbox.EmitSignal(NClickableControl.SignalName.Released,hitbox);
        await Wait(()=>(int)AccessTools.Field(typeof(NAncientEventLayout),"_currentDialogueLine").GetValue(layout)! == before+1);
        return AncientDialogueOracle.State(false);
    }
    public static async Task<object> Choose(RunState run)
    {
        var model=Event(run);var option=model.CurrentOptions.First(o=>!o.IsLocked);var before=model.Owner!.Relics.Count();var deck=model.Owner.Deck.Cards.Count;
        var choice=option.Chosen();var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+25000;var rewardSelections=0;
        while(!choice.IsCompleted){
            if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Ancient reward selection.");
            var rewardScreen=NativeTestBridge.Descendants(tree.Root).OfType<NCardRewardSelectionScreen>().FirstOrDefault(n=>n.IsVisibleInTree());
            if(rewardScreen!=null){var holder=NativeTestBridge.Descendants(rewardScreen).OfType<NCardHolder>().FirstOrDefault(h=>h.CardModel!=null&&h.IsVisibleInTree());if(holder!=null){holder.EmitSignal(NCardHolder.SignalName.Pressed,holder);rewardSelections++;}}
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        await choice;Require(option.WasChosen,"Native reward callback did not execute.");
        var after=model.Owner.Relics.Count();var acquired=model.Owner.Relics.Skip(before).Select(r=>r.Id.ToString()).ToArray();
        // Native EventOption guards repeated activation of this same choice.
        await option.Chosen();Require(model.Owner.Relics.Count()==after,"Reward callback acquired twice.");
        return new{key=option.TextKey,finished=model.IsFinished,acquired,deckDelta=model.Owner.Deck.Cards.Count-deck,rewardSelections,once=true,options=model.CurrentOptions.Select(o=>o.TextKey).ToArray()};
    }
    public static async Task<object> Tome(RunState run)
    {
        var player=run.Players.Single();var cls=PlayerBloodlines.Heir(player).classId;var seen=new HashSet<string>();
        for(var i=0;i<60;i++){var tome=(DustyTome)ModelDb.Relic<DustyTome>().ToMutable();tome.SetupForPlayer(player);var card=(LegacyCard)ModelDb.GetById<CardModel>(tome.AncientCard!);Require(card.DesignCard.Row.Text("classId")==cls&&card.Rarity==CardRarity.Ancient,"Tome selected another class/rarity.");seen.Add(card.Key);}
        Require(seen.Count==2,"Tome did not offer both class cards.");
        var reward=(DustyTome)ModelDb.Relic<DustyTome>().ToMutable();reward.SetupForPlayer(player);var before=player.Deck.Cards.Count;await RelicCmd.Obtain(reward,player);
        var gained=player.Deck.Cards.Last();Require(player.Deck.Cards.Count==before+1&&gained.IsUpgraded&&gained.Id==reward.AncientCard,"Tome acquisition identity/upgrade.");
        var restored=CardModel.FromSerializable(gained.ToSerializable());restored.Owner=player;Require(restored.Id==gained.Id&&restored.IsUpgraded&&restored.Owner==player,"Ancient card saved identity.");
        return new{cls,seen=seen.Order().ToArray(),acquired=gained.Id.ToString(),upgrade=gained.CurrentUpgradeLevel,saveRoundTrip=true};
    }
    public static async Task<object> EndingEnter(RunState run,int wins)
    {
        SaveManager.Instance.Progress.GetOrCreateCharacterStats(run.Players.Single().Character.Id).TotalWins=wins;
        run.CurrentActIndex=run.Acts.Count-1;
        await RunManager.Instance.EnterRoomDebug(RoomType.Event,model:ModelDb.Event<TheArchitect>(),showTransition:false);
        RunManager.Instance.ActionExecutor.Unpause();await Wait(()=>Event(run).CurrentOptions.Count>0);
        return new{act=run.CurrentActIndex,cls=PlayerBloodlines.Heir(run.Players.Single()).classId,lines=((AncientDialogue)AccessTools.Field(typeof(TheArchitect),"_dialogue").GetValue(Event(run))!).Lines.Select(l=>l.LineText!.LocEntryKey).ToArray()};
    }
    public static async Task<object> EndingAdvance(RunState run)
    {
        var model=Event(run);var before=(int)AccessTools.Field(typeof(TheArchitect),"_currentLineIndex").GetValue(model)!;
        Require(before<3,"Use final Proceed fixture.");await model.CurrentOptions.Single().Chosen();
        return new{index=(int)AccessTools.Field(typeof(TheArchitect),"_currentLineIndex").GetValue(model)!,options=model.CurrentOptions.Select(o=>o.Title.GetFormattedText()).ToArray()};
    }
    public static async Task<object> EndingWin(RunState run)
    {
        var model=Event(run);Require((int)AccessTools.Field(typeof(TheArchitect),"_currentLineIndex").GetValue(model)! == 3,"Not at final Proceed.");var option=model.CurrentOptions.Single();
        var id=Runtime.Profile.active!.id;var generation=Runtime.Profile.generation;
        await option.Chosen();await Wait(()=>Runtime.Profile.active==null&&Runtime.Profile.settledId==id);
        var after=Runtime.Profile.generation;string? repeatDiagnostic=null;
        try{await option.Chosen();}catch(Exception error){Require(run.IsGameOver&&model.IsFinished,"Unfinished ending repeat failed.");repeatDiagnostic=error.ToString();}
        Require(Runtime.Profile.generation==after&&after==generation+1,"Ending settled twice.");
        return new{nativeGameOver=run.IsGameOver,settled=id,generation=after,once=true,repeatDiagnostic,wins=Runtime.Profile.chronicle.classes.Values.Sum(c=>c.wins)};
    }
    public static async Task<object> Owners(RunState run)
    {
        var prior=PlayerBloodlines.EligibilityOwner.Value;var results=new List<object>();
        foreach(var player in run.Players){var model=(TheArchitect)ModelDb.Event<TheArchitect>().ToMutable();await model.BeginEvent(player,false);var dialogue=(AncientDialogue)AccessTools.Field(typeof(TheArchitect),"_dialogue").GetValue(model)!;var ancientKeys=new List<string>();
            if(player.Character is HeirCharacter){var cls=PlayerBloodlines.Heir(player).classId;Require(dialogue.Lines.All(l=>l.LineText!.LocEntryKey.StartsWith("SPIRELEGACY.ARCHITECT."+cls+".")),"Remote ending used another heir's class.");}
            else Require(dialogue.Lines.All(l=>!l.LineText!.LocEntryKey.StartsWith("SPIRELEGACY")),"Native ending contaminated.");
            try{PlayerBloodlines.EligibilityOwner.Value=player;
                foreach(var ancient in ModelDb.AllAncients)foreach(var visits in new[]{0,1,100}){
                    var lines=ancient.DialogueSet.GetValidDialogues(player.Character.Id,visits,200,true).First().Lines;
                    if(player.Character is HeirCharacter)Require(lines.All(l=>l.LineText!.LocEntryKey.StartsWith("SPIRELEGACY.ANCIENT."+ancient.Id.Entry+"."+PlayerBloodlines.Heir(player).classId+".")),"Remote Ancient selector used another heir.");
                    else Require(lines.All(l=>!l.LineText!.LocEntryKey.StartsWith("SPIRELEGACY")),"Native Ancient selector contaminated.");ancientKeys.Add(lines[0].LineText!.LocEntryKey);
                }
            }finally{PlayerBloodlines.EligibilityOwner.Value=prior;}
            results.Add(new{owner=player.NetId,cls=player.Character.Id.ToString(),keys=dialogue.Lines.Select(l=>l.LineText!.LocEntryKey).ToArray(),ancientKeys});}
        Require(PlayerBloodlines.EligibilityOwner.Value==prior,"Dialogue owner scope leaked.");return results;
    }
}
