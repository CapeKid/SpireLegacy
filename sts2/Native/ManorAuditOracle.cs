using Godot;
namespace SpireLegacy;

// Reached only through the isolated, opt-in native test bridge.
public static class ManorAuditOracle
{
    private static SceneTree Tree=>(SceneTree)Engine.GetMainLoop();
    public static object Duplicate(string text)
    {
        var button=NativeTestBridge.Descendants(Tree.Root).OfType<Button>().First(b=>b.IsVisibleInTree()&&!b.Disabled&&b.Text==text);
        button.GrabFocus();button.EmitSignal(Button.SignalName.Pressed);button.EmitSignal(Button.SignalName.Pressed);
        return new {removed=!button.IsInsideTree(),state=Inspect()};
    }
    public static object Traits()
    {
        if(Runtime.Profile.active!=null)throw new InvalidOperationException("End the private run before replacing offers.");
        var rows=Design.Rows("traits").OrderByDescending(r=>r.Text("summary").Length).ToArray();
        Runtime.Profile.offers=rows.Select((r,i)=>new FamilyProfile.Heir{name="ABCDEFGHIJKLMNOPQRSTUVWX",classId=new[]{"knight","mage","ranger"}[i%3],traits=[r.Text("id")]}).ToList();
        Runtime.Profile.Save(Runtime.FamilyPath);ManorUi.Close();ManorUi.Open();
        return new {traits=rows.Select(r=>new{id=r.Text("id"),name=r.Text("name"),summary=r.Text("summary")}).ToArray()};
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
