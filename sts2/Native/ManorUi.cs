using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace SpireLegacy;

public static class ManorUi
{
    private static CanvasLayer? modal;
    private static VBoxContainer? content;
    private static bool wasPaused;
    private static Input.MouseModeEnum mouseMode;
    private static NCharacterSelectScreen? selectionScreen;
    private static readonly List<string> cart = new();
    private static readonly Dictionary<ulong,Button> launchers = new();
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();
    public static bool IsOpen => modal != null && GodotObject.IsInstanceValid(modal);
    public static string? Notice {get;set;}
    public static readonly IScreenContext ScreenContext = new ManorContext();
    private sealed class ManorContext : IScreenContext
    {
        public Control? DefaultFocusedControl => content == null ? null : Descendants(content).OfType<Button>().FirstOrDefault(b=>!b.Disabled);
    }
    private static IEnumerable<Node> Descendants(Node root)
    {
        yield return root;
        foreach(var child in root.GetChildren()) foreach(var node in Descendants(child)) yield return node;
    }
    public static void PollInput()
    {
        if (IsOpen && Input.IsActionJustPressed("ui_cancel")) RequestClose();
    }
    public static readonly Color[] BannerColors = [new("D8AA47"),new("8798E6"),new("CD6681"),new("76A76C")];
    private static readonly string[] BannerNames = ["Sun", "Moon", "Rose", "Oak"];
    public static Color BannerColor => BannerColors[Math.Clamp(Runtime.Profile.banner,0,3)];
    public static void Attach(NCharacterSelectScreen screen, CharacterModel character)
    {
        selectionScreen = screen;
        var id = screen.GetInstanceId();
        if (!launchers.TryGetValue(id,out var button) || !GodotObject.IsInstanceValid(button))
        {
            button = MakeButton("Family Manor & Heir Lab",Open);
            button.Name = "SpireLegacyManor"; button.Position = new(45,740); button.Size = new(485,60);
            screen.AddChild(button); launchers[id] = button;
        }
        button.Visible = character is HeirCharacter;
        if (character is not HeirCharacter) return;
        var background = (Control)AccessTools.Field(typeof(NCharacterSelectScreen),"_bgContainer").GetValue(screen)!;
        foreach (var child in background.GetChildren().OfType<CanvasItem>()) child.Visible = false;
        var art = new TextureRect { Texture = Runtime.Texture("heir/ui/select-bg.png"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = Control.MouseFilterEnum.Ignore };
        background.AddChild(art); art.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }
    public static Button MakeButton(string text, Action action)
    {
        var button = new Button { Text = text, CustomMinimumSize = new(0,54), FocusMode = Control.FocusModeEnum.All };
        button.AddThemeFontSizeOverride("font_size",24); button.Pressed += action;
        return button;
    }
    private static Label Text(string value, int size = 23)
    {
        var label = new Label { Text = value, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size",size); return label;
    }
    public static void Open()
    {
        if (IsOpen) return;
        cart.Clear(); wasPaused = Tree.Paused;
        mouseMode = Input.MouseMode; Input.MouseMode = Input.MouseModeEnum.Visible;
        modal = new CanvasLayer { Name = "SpireLegacyManorModal", Layer = 110, ProcessMode = Node.ProcessModeEnum.Always };
        Tree.Root.AddChild(modal);
        var shade = new ColorRect { Color = new(0.025f,.035f,.07f,.97f), MouseFilter = Control.MouseFilterEnum.Stop };
        modal.AddChild(shade); shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var margins = new MarginContainer(); shade.AddChild(margins); margins.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (var name in new[] {"margin_left","margin_right","margin_top","margin_bottom"}) margins.AddThemeConstantOverride(name,35);
        var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, FollowFocus = true };
        margins.AddChild(scroll);
        content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation",16); scroll.AddChild(content);
        Tree.Paused = true; Render(); ActiveScreenContext.Instance.Update();
    }
    private static void Clear()
    {
        foreach (var child in content!.GetChildren()) { content.RemoveChild(child); child.QueueFree(); }
    }
    private static void AddButton(string text, Action action) => content!.AddChild(MakeButton(text,action));
    private static void AddText(string text, int size = 23) => content!.AddChild(Text(text,size));
    private static void Render()
    {
        Clear(); var profile = Runtime.Profile;
        var heading = Text($"{profile.family} Family Manor — generation {profile.generation + 1}",32); heading.Modulate = BannerColor; content!.AddChild(heading);
        AddText($"Crowns: {profile.crowns} · Banner: {BannerNames[profile.banner]} · Last heir earned {profile.lastEarned}");
        if(Notice is {} notice) AddText(notice,26);
        if (ContentBootstrap.Failure is {} failure)
        {
            AddText("Rogue Legacy 2 content preparation failed. Full diagnostic log:",28);
            AddText(failure,20);
            AddButton("Retry content preparation",() => { ContentBootstrap.Prepare(); Render(); });
        }
        var options = new HBoxContainer(); content.AddChild(options);
        options.AddChild(MakeButton("Name your family",() => EditValue(false)));
        options.AddChild(MakeButton("Change banner",() => { profile.banner = (profile.banner + 1) % 4; profile.Save(Runtime.FamilyPath); Render(); }));
        if (Design.Get("systems","playtest_crowns").Flag("enabled")) options.AddChild(MakeButton("PLAYTEST: Set crowns",() => EditValue(true)));
        options.AddChild(MakeButton("Close",RequestClose));
        if (profile.active is {} active)
        {
            AddText("A climb is in progress. Manor purchases and heir selection are available after it ends.");
            HeirDetails(active.heir,false);
        }
        else
        {
            AddText("Choose your next heir",28);
            foreach (var heir in profile.offers) HeirDetails(heir,true);
        }
        AddText("Manor upgrades — select upgrades, then review and confirm to spend crowns",28);
        var trial = profile.PreviewPurchases(cart) ?? profile;
        foreach (var row in Design.Rows("manor"))
        {
            var id = row.Text("id");
            AddText($"{row.Text("name")} · level {trial.Level(id)}/{row.Number("maxLevel")} · {row.Text("summary")}");
            var button = MakeButton($"Select upgrade — {trial.Cost(row)} crowns",() => { if (profile.PreviewPurchases(cart.Append(id)) is not null) { cart.Add(id); Render(); } });
            button.Disabled = profile.active != null || profile.PreviewPurchases(cart.Append(id)) is null;
            content.AddChild(button);
        }
        if (cart.Count > 0)
        {
            AddText($"Selected {cart.Count} upgrades · total {profile.crowns - trial.crowns} crowns · remaining {trial.crowns}");
            AddButton("Review purchases",Review);
            AddButton("Clear selected upgrades",() => { cart.Clear(); Render(); });
        }
        FocusFirst();
    }
    private static void HeirDetails(FamilyProfile.Heir heir, bool selectable)
    {
        var cls = Design.Get("classes",heir.classId);
        var selected = ReferenceEquals(Runtime.Profile.selected,heir);
        AddText($"{(selected ? "✓ " : "")}{heir.name} {Runtime.Profile.family} — {cls.Text("name")}",27);
        AddText(cls.Text("summary"));
        foreach (var trait in Inheritance.Rows(heir))
        {
            var detail = new HBoxContainer(); content!.AddChild(detail);
            if (ContentBootstrap.Ready) detail.AddChild(new TextureRect { Texture = Runtime.CacheTexture(trait.Text("asset") + ".png"), CustomMinimumSize = new(48,48), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
            detail.AddChild(Text($"{trait.Text("name")}: {trait.Text("summary")}\nCrown bonus: {trait.Number("goldBonus")}%"));
        }
        if (selectable) AddButton(selected ? "Selected for next climb" : "Choose this heir",() => { Runtime.Profile.selected = heir; Runtime.Profile.Save(Runtime.FamilyPath); Render(); });
    }
    private static void Review()
    {
        Clear(); var profile = Runtime.Profile; var trial = profile.PreviewPurchases(cart);
        if (trial == null) { cart.Clear(); Render(); return; }
        AddText("Confirm manor purchases",32);
        foreach (var group in cart.GroupBy(x => x)) AddText($"{Design.Get("manor",group.Key).Text("name")} × {group.Count()}");
        AddText($"Spend {profile.crowns - trial.crowns} crowns? You will have {trial.crowns} left.");
        AddButton("Confirm and save",() => { if (profile.CommitPurchases(cart,Runtime.FamilyPath)) cart.Clear(); Render(); });
        AddButton("Back to selection",Render); FocusFirst();
    }
    private static void EditValue(bool crowns)
    {
        Clear(); AddText(crowns ? "PLAYTEST: Set crown balance" : "Name your family",32);
        var entry = new LineEdit { Text = crowns ? Runtime.Profile.crowns.ToString() : Runtime.Profile.family, MaxLength = crowns ? 10 : 24, VirtualKeyboardEnabled = true, CustomMinimumSize = new(0,60) };
        entry.AddThemeFontSizeOverride("font_size",28); content!.AddChild(entry);
        var error = Text(""); content.AddChild(error);
        AddButton("Save",() =>
        {
            if (crowns)
            {
                if (!int.TryParse(entry.Text,out var amount) || amount < 0) { error.Text = "Enter a whole number from 0 to 2147483647."; return; }
                Runtime.Profile.crowns = amount; cart.Clear();
            }
            else
            {
                var name = entry.Text.Trim(); if (name.Length == 0) { error.Text = "Enter a family name."; return; }
                Runtime.Profile.family = name;
            }
            Runtime.Profile.Save(Runtime.FamilyPath); Render();
        });
        AddButton("Cancel",Render);
        AddText("Keyboard / Steam Deck: type normally or use these on-screen keys.");
        foreach (var letters in crowns ? new[] {"1234567890"} : new[] {"ABCDEFGHIJKLM","NOPQRSTUVWXYZ","abcdefghijklm","nopqrstuvwxyz"})
        {
            var keys = new HBoxContainer(); content.AddChild(keys);
            foreach (var letter in letters) { var captured = letter; keys.AddChild(MakeButton(letter.ToString(),() => { if (entry.Text.Length < entry.MaxLength) entry.Text += captured; })); }
        }
        AddButton("Backspace",() => { if (entry.Text.Length > 0) entry.Text = entry.Text[..^1]; });
        if (!crowns) AddButton("Space",() => { if (entry.Text.Length < 24) entry.Text += " "; });
        AddButton("Clear text",() => entry.Text = ""); entry.GrabFocus();
    }
    private static void RequestClose()
    {
        if (cart.Count == 0) { Close(); return; }
        Clear(); AddText("Discard the selected upgrades? Your crowns have not been spent.",30);
        AddButton("Keep selecting",Render); AddButton("Discard and close",Close); FocusFirst();
    }
    private static void FocusFirst() => ScreenContext.DefaultFocusedControl?.GrabFocus();
    public static void Close()
    {
        if (!IsOpen) return;
        cart.Clear(); modal?.QueueFree(); modal = null; Tree.Paused = wasPaused; Input.MouseMode = mouseMode;
        ActiveScreenContext.Instance.Update();
        if (selectionScreen is {} screen && GodotObject.IsInstanceValid(screen) && screen.IsVisibleInTree())
        {
            var button = (NCharacterSelectButton?)AccessTools.Field(typeof(NCharacterSelectScreen),"_selectedButton").GetValue(screen);
            if (button?.Character is HeirCharacter) screen.SelectCharacter(button,button.Character);
        }
    }
}

[HarmonyPatch(typeof(ActiveScreenContext),nameof(ActiveScreenContext.GetCurrentScreen))]
public static class ManorControllerContext
{
    public static bool Prefix(ref IScreenContext? __result)
    {
        if (!ManorUi.IsOpen) return true;
        __result = ManorUi.ScreenContext; return false;
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen),nameof(NCharacterSelectScreen.SelectCharacter))]
public static class ManorCharacterMenu
{
    public static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel) => ManorUi.Attach(__instance,characterModel);
}
