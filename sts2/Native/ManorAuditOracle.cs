using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SpireLegacy;

// Reached only through the isolated, opt-in native test bridge.
public static class ManorAuditOracle
{
    private static SceneTree Tree=>(SceneTree)Engine.GetMainLoop();
    private static (ISaveStore Store, string Path) SoloSave()
    {
        var saves = SaveManager.Instance;
        var store = (ISaveStore)AccessTools.Field(typeof(SaveManager), "_saveStore").GetValue(saves)!;
        return (store, RunSaveManager.GetRunSavePath(saves.CurrentProfileId, "current_run.save", true));
    }
    public static string CaptureRecoverySave()
    {
        var (store, path) = SoloSave();
        return store.ReadFile(path) ?? throw new InvalidDataException("Missing native recovery fixture save.");
    }
    public static object StuckRun(string fixture)
    {
        void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }
        Button? Find(string name) => NativeTestBridge.Descendants(Tree.Root).OfType<Button>()
            .SingleOrDefault(button => button.IsVisibleInTree() && button.Name == name);
        void Press(string name) => (Find(name) ?? throw new InvalidDataException("Missing button " + name))
            .EmitSignal(Button.SignalName.Pressed);
        void Open()
        {
            ManorUi.Close();
            ManorUi.Notice = null;
            ManorUi.Open();
        }
        const string clear = "ManorClearStuckRun", confirm = "ManorConfirmClearStuckRun", cancel = "ManorCancelClearStuckRun";
        var profile = Runtime.Profile;
        Require(profile.active is null, "Expected a fresh isolated family.");
        profile.crowns = 250;
        profile.manor[Design.Rows("manor").First().Text("id")] = 1;
        var run = profile.Begin(178);
        profile.Save(Runtime.FamilyPath);
        var original = JsonSerializer.SerializeToNode(profile, FamilyProfile.JsonOptions)!.AsObject();
        original["active"] = null;
        var saves = SaveManager.Instance;
        var (store, solo) = SoloSave();
        var otherProfile = saves.CurrentProfileId == 1 ? 2 : 1;
        var backup = RunSaveManager.GetRunSavePath(otherProfile, "current_run_mp.save", true) + ".backup";
        void WithSave(string path, Action check)
        {
            Require(!store.FileExists(path), "Fixture attempted to overwrite a save.");
            store.CreateDirectory(path[..path.LastIndexOf('/')]);
            store.WriteFile(path, fixture);
            try
            {
                check();
                Require(store.ReadFile(path) == fixture, "Recovery changed the native save.");
            }
            finally { store.DeleteFile(path); }
        }

        WithSave(solo, () =>
        {
            Open();
            var status = StuckRunRecovery.Check(run);
            Require(Find(clear) is null && status == StuckRunRecovery.Status.CurrentSave,
                $"A current native save should block recovery. Status={status}, visible={Find(clear) is not null}, found={saves.HasRunSave}, initialized={saves.IsProfileInitialized}, pending={saves.CurrentRunSaveTask?.Status}.");
        });
        WithSave(backup, () =>
        {
            Open();
            Require(Find(clear) is null && StuckRunRecovery.Check(run) == StuckRunRecovery.Status.OtherProfile,
                "A different profile's backup should block recovery.");
        });
        run.coop = true;
        Open();
        Require(Find(clear) is null && StuckRunRecovery.Check(run) == StuckRunRecovery.Status.CoOp,
            "A remote co-op record should block recovery.");
        run.coop = false;

        Open();
        Press(clear);
        Press(cancel);
        Require(profile.active?.id == run.id && Find(clear) is not null, "Cancelling changed the active record.");
        Press(clear);
        WithSave(solo, () =>
        {
            Press(confirm);
            Require(profile.active?.id == run.id && Find(clear) is null, "Confirmation did not recheck for a save.");
        });
        Open();
        Press(clear);
        Press(confirm);
        var reloaded = FamilyProfile.Load(Runtime.FamilyPath);
        var after = JsonSerializer.SerializeToNode(reloaded, FamilyProfile.JsonOptions);
        Require(profile.active is null && JsonNode.DeepEquals(original, after), "Recovery changed more than the active record.");
        Require(Find(clear) is null, "Recovery was still available after clearing the record.");
        ManorUi.Close();
        return new { cleared = true, currentSaveProtected = true, otherProfileBackupProtected = true, coOpProtected = true,
            cancelPreservedRecord = true, recheckedAtConfirmation = true, progressPreserved = true };
    }

    public static object LaunchRun()
    {
        var button=NativeTestBridge.Descendants(Tree.Root).OfType<Button>().Single(b=>b.IsVisibleInTree()&&b.Text.EndsWith(" · Family / Traits"));
        button.EmitSignal(Button.SignalName.Pressed);return Inspect();
    }
    public static object Duplicate(string text)
    {
        var button=NativeTestBridge.Descendants(Tree.Root).OfType<Button>().First(b=>b.IsVisibleInTree()&&!b.Disabled&&b.Text==text);
        button.GrabFocus();button.EmitSignal(Button.SignalName.Pressed);button.EmitSignal(Button.SignalName.Pressed);
        return new {removed=!button.IsInsideTree(),state=Inspect()};
    }
    public static object Traits(int offset)
    {
        if(Runtime.Profile.active!=null)throw new InvalidOperationException("End the private run before replacing offers.");
        var rows=Design.Rows("traits").OrderByDescending(r=>r.Text("summary").Length).Skip(offset).Take(3).ToArray();
        if(rows.Length==0)throw new ArgumentOutOfRangeException(nameof(offset));
        Runtime.Profile.offers=rows.Select((r,i)=>new FamilyProfile.Heir{name="ABCDEFGHIJKLMNOPQRSTUVWX",classId=new[]{"knight","mage","ranger"}[i%3],traits=[r.Text("id")]}).ToList();
        Runtime.Profile.Save(Runtime.FamilyPath);
        ManorUi.Close();
        OpenEmbarkPicker();
        return new {total=Design.Rows("traits").Count(),traits=rows.Select(r=>new{id=r.Text("id"),name=r.Text("name"),summary=r.Text("summary")}).ToArray()};
    }
    public static void OpenEmbarkPicker()
    {
        var type = typeof(NCharacterSelectScreen);
        var screen = NativeTestBridge.Descendants(Tree.Root).OfType<NCharacterSelectScreen>().Single(node => node.IsVisibleInTree());
        var button = AccessTools.Field(type, "_embarkButton").GetValue(screen);
        AccessTools.Method(type, "OnEmbarkPressed").Invoke(screen, [button]);
    }
    public static object Inspect()
    {
        var root=NativeTestBridge.Descendants(Tree.Root).OfType<ManorInput>().SingleOrDefault();
        var focus=Tree.Root.GuiGetFocusOwner();
        object Rect(Control c)=>new {x=c.GetGlobalRect().Position.X,y=c.GetGlobalRect().Position.Y,width=c.Size.X,height=c.Size.Y};
        return new {open=ManorUi.IsOpen,paused=Tree.Paused,window=DisplayServer.WindowGetSize().ToString(),viewport=Tree.Root.GetVisibleRect().Size.ToString(),
            focus=focus?.Name.ToString(),focusType=focus?.GetType().Name,focusText=focus is Button b?b.Text:focus is LineEdit e?e.Text:null,
            controls=root==null?[]:NativeTestBridge.Descendants(root).OfType<Control>().Where(c=>c.IsVisibleInTree()).Select(c=>new {name=c.Name.ToString(),type=c.GetType().Name,focusable=c.FocusMode==Control.FocusModeEnum.All,disabled=c is Button b&&b.Disabled,text=c is Button button?button.Text:c is Label label?label.Text:c is LineEdit edit?edit.Text:null,rect=Rect(c)}).ToArray()};
    }
    public static async Task<object> Resize(int width,int height)
    {
        if(width is <800 or >2560||height is <600 or >1600)throw new ArgumentOutOfRangeException();
        var settings=MegaCrit.Sts2.Core.Saves.SaveManager.Instance.SettingsSave;
        settings.Fullscreen=false;settings.WindowSize=new(width,height);settings.WindowPosition=new(-2000,-2000);
        settings.AspectRatioSetting=width*10==height*16?MegaCrit.Sts2.Core.Settings.AspectRatioSetting.SixteenByTen:
            width*3==height*4?MegaCrit.Sts2.Core.Settings.AspectRatioSetting.FourByThree:MegaCrit.Sts2.Core.Settings.AspectRatioSetting.SixteenByNine;
        MegaCrit.Sts2.Core.Nodes.NGame.Instance!.ApplyDisplaySettings();
        for(var i=0;i<4;i++)await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
        return Inspect();
    }
}
