using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace SpireLegacy;

// Explicitly opt-in fixtures operating on the isolated native save backend only.
public static class AncientDialogueOracle
{
    public static object Inspect()
    {
        var original=Runtime.Heir.classId;var checks=new List<object>();
        try {
            foreach(var ancient in ModelDb.AllAncients) {
                if(!AncientConversation.Supported.Contains(ancient.Id.Entry))throw new InvalidDataException("New Ancient needs an authored conversation: "+ancient.Id);
                var optionIds=ancient.AllPossibleOptions.Select(o=>o.TextKey).ToArray();
                var set=ancient.DialogueSet;
                foreach(var cls in new[]{"knight","mage","ranger"})foreach(var visits in new[]{0,1,4,10,100})foreach(var total in new[]{0,200}) {
                    Runtime.Heir.classId=cls;
                    var dialogue=set.GetValidDialogues(ModelDb.Character<HeirCharacter>().Id,visits,total,true).Single();
                    var content=AncientConversation.For(ancient.Id.Entry,cls,visits);
                    if(dialogue.Lines.Count!=4||dialogue.IsRepeating!=(visits>0))throw new InvalidDataException("Wrong Ancient visit sequence.");
                    for(var i=0;i<4;i++) {
                        var line=dialogue.Lines[i];
                        if(line.LineText!.GetFormattedText()!=content[i].Text||line.Speaker!=(i%2==0?AncientDialogueSpeaker.Ancient:AncientDialogueSpeaker.Character)||!line.LineText.LocEntryKey.StartsWith("SPIRELEGACY.ANCIENT."))throw new InvalidDataException("Wrong Ancient line/speaker/localization.");
                        if(i<3&&line.NextButtonText?.GetFormattedText()!=(i%2==0?"Respond":"Continue"))throw new InvalidDataException("Wrong response prompt.");
                    }
                    checks.Add(new{ancient=ancient.Id.Entry,cls,visits,total,keys=dialogue.Lines.Select(l=>l.LineText!.LocEntryKey).ToArray()});
                }
                if(!optionIds.SequenceEqual(ancient.AllPossibleOptions.Select(o=>o.TextKey)))throw new InvalidDataException("Changed Ancient option catalog.");
                var native=set.GetValidDialogues(ModelDb.Character<Ironclad>().Id,0,0,true).First();
                if(native.Lines.Any(l=>l.LineText!.LocEntryKey.StartsWith("SPIRELEGACY")))throw new InvalidDataException("Changed native Ancient dialogue.");
            }
        } finally {Runtime.Heir.classId=original;}
        return new{checks=checks.Count,ancients=ModelDb.AllAncients.Select(a=>a.Id.Entry).ToArray(),details=checks};
    }
    public static AncientEventModel Prepare(string entry,int visits)
    {
        var model=ModelDb.AllAncients.Single(a=>a.Id.Entry==entry);
        var stats=SaveManager.Instance.Progress.GetOrCreateAncientStats(model.Id);
        stats.CharStats.Clear();stats.CharStats.Add(new AncientCharacterStats{Character=ModelDb.Character<HeirCharacter>().Id,Losses=visits});
        return model;
    }
    public static object State(bool advance)
    {
        var tree=(SceneTree)Engine.GetMainLoop();var layout=NativeTestBridge.Descendants(tree.Root).OfType<NAncientEventLayout>().Single(l=>l.IsVisibleInTree());
        var dialogue=(List<AncientDialogueLine>)AccessTools.Field(typeof(NAncientEventLayout),"_dialogue").GetValue(layout)!;
        var index=(int)AccessTools.Field(typeof(NAncientEventLayout),"_currentDialogueLine").GetValue(layout)!;
        var hitbox=(NAncientDialogueHitbox)AccessTools.Field(typeof(NAncientEventLayout),"_dialogueHitbox").GetValue(layout)!;
        if(advance){if(index>=dialogue.Count-1)throw new InvalidDataException("Already on final line.");hitbox.EmitSignal(NClickableControl.SignalName.Released,hitbox);index=(int)AccessTools.Field(typeof(NAncientEventLayout),"_currentDialogueLine").GetValue(layout)!;}
        return new{index,lines=dialogue.Select(l=>new{speaker=l.Speaker.ToString(),key=l.LineText!.LocEntryKey,text=l.LineText.GetFormattedText(),next=l.NextButtonText?.GetFormattedText()}).ToArray(),
            // Before the last line, native accept hotkey advances dialogue without focus.
            focusable=index<dialogue.Count-1?hitbox.IsVisibleInTree()&&hitbox.IsEnabled&&!string.IsNullOrEmpty(hitbox.GetHotkey()):layout.DefaultFocusedControl?.FocusMode==Control.FocusModeEnum.All,options=layout.OptionButtons.Select(o=>o.Name.ToString()).ToArray()};
    }
}
