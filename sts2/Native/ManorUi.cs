using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace SpireLegacy;

public static class ManorUi
{
    private static CanvasLayer? modal;
    private static VBoxContainer? content;
    private static ScrollContainer? scroll;
    private static Button? closeButton;
    private static ManorInput? input;
    private static Action back = RequestClose;
    private static readonly Dictionary<Node,Node.ProcessModeEnum> inputModes=new();
    private static int navigationGeneration;
    private static string page="";
    private static readonly Dictionary<string,(string? Focus,int Scroll)> locations=new();
    private static bool wasPaused;
    private static Input.MouseModeEnum mouseMode;
    private static NCharacterSelectScreen? selectionScreen;
    private static Node? ownerRun;
    private static NCharacterSelectScreen? ownerSelection;
    private static readonly List<string> cart = new();
    private static readonly Dictionary<ulong,Button> launchers = new();
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();
    public static bool IsOpen => modal != null && GodotObject.IsInstanceValid(modal);
    internal static bool ValidateContext()
    {
        if(!IsOpen)return false;
        if(!ReferenceEquals(ownerRun,MegaCrit.Sts2.Core.Nodes.NRun.Instance)
            || ownerSelection is {} screen && (!GodotObject.IsInstanceValid(screen)||!screen.IsVisibleInTree())) {
            Close();return false;
        }
        return true;
    }
    public static string? Notice {get;set;}
    public static readonly IScreenContext ScreenContext = new ManorContext();
    private sealed class ManorContext : IScreenContext
    {
        public Control? DefaultFocusedControl => content == null ? null : NavigationControls().FirstOrDefault(c=>c is Button);
    }
    private static IEnumerable<Node> Descendants(Node root)
    {
        yield return root;
        foreach(var child in root.GetChildren()) foreach(var node in Descendants(child)) yield return node;
    }
    private static List<Control> NavigationControls() => content == null ? [] :
        Descendants(content).OfType<Control>().Where(c=>c.FocusMode==Control.FocusModeEnum.All && c.IsVisibleInTree() && (c is not Button b || !b.Disabled)).Concat(closeButton is null ? [] : new Control[]{closeButton}).ToList();
    internal static void Navigate(int direction)
    {
        var controls=NavigationControls(); if(controls.Count==0)return;
        var index=controls.IndexOf(Tree.Root.GuiGetFocusOwner());
        controls[Math.Clamp(index+direction,0,controls.Count-1)].GrabFocus();
    }
    internal static void Page(int direction)
    {
        if(scroll is null)return;
        var bar=scroll.GetVScrollBar();
        scroll.ScrollVertical=(int)Math.Clamp(scroll.ScrollVertical+direction*scroll.Size.Y*.8f,0,Math.Max(0,bar.MaxValue-bar.Page));
    }
    internal static void GoBack() => back();
    internal static void Activate()
    {
        if(Tree.Root.GuiGetFocusOwner() is Button {Disabled:false} button) button.EmitSignal(Button.SignalName.Pressed);
    }
    private static void ConfigureNavigation(string? restore=null,int? position=null)
    {
        if(locations.TryGetValue(page,out var location)) {restore ??= location.Focus;position ??= location.Scroll;}
        var savedPosition=position ?? 0;
        var controls=NavigationControls();
        for(var i=0;i<controls.Count;i++)
        {
            var item=controls[i];
            // The frame callback handles directions once; disable simultaneous native GUI movement.
            var path=item.GetPath();
            item.FocusNeighborTop=path;item.FocusNeighborBottom=path;item.FocusNeighborLeft=path;item.FocusNeighborRight=path;
            if(item!=closeButton) {
                var captured=item;
                item.FocusEntered += () => { if(scroll is not null && content!.IsAncestorOf(captured)) scroll.EnsureControlVisible(captured); };
            }
        }
        var target=controls.FirstOrDefault(c=>c.Name.ToString()==restore) ?? ScreenContext.DefaultFocusedControl;
        target?.GrabFocus(); RestoreAfterLayout(target,savedPosition,++navigationGeneration);
    }
    private static async void RestoreAfterLayout(Control? target,int position,int generation)
    {
        // Newly rebuilt container children have no layout yet; wait before scrolling to them.
        await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
        await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);
        if(generation!=navigationGeneration || !IsOpen || target is null || !GodotObject.IsInstanceValid(target))return;
        scroll!.ScrollVertical=position;target.GrabFocus();
        if(content!.IsAncestorOf(target))scroll.EnsureControlVisible(target);
    }
    public static readonly Color[] BannerColors = [new("D8AA47"),new("8798E6"),new("CD6681"),new("76A76C")];
    private static readonly string[] BannerNames = ["Sun", "Moon", "Rose", "Oak"];
    public static Color BannerColorFor(int banner)=>BannerColors[Math.Clamp(banner,0,3)];
    public static Color BannerColor => BannerColors[Math.Clamp(Runtime.Profile.banner,0,3)];
    public static void Attach(NCharacterSelectScreen screen, CharacterModel character)
    {
        selectionScreen = screen;
        var id = screen.GetInstanceId();
        if (!launchers.TryGetValue(id,out var button) || !GodotObject.IsInstanceValid(button))
        {
            button = MakeButton("Family Manor & Heir Lab (Y)",Open);
            button.Name = "SpireLegacyManor"; button.Size = new(420,60);
            button.AddThemeFontSizeOverride("font_size",22);
            screen.AddChild(button); launchers[id] = button;
            var captured=button;
            var shortcutWasHeld=false;
            void Poll() {
                // Steam Input emits action events during the controller node's _Process,
                // after SceneTree.ProcessFrame. Track held-state edges across frames.
                var shortcutHeld=Input.IsActionPressed("ui_accept") || Input.IsActionPressed("controller_face_button_north");
                var shortcutPressed=shortcutHeld && !shortcutWasHeld;shortcutWasHeld=shortcutHeld;
                if(!GodotObject.IsInstanceValid(screen) || !screen.IsVisibleInTree() || !captured.Visible || IsOpen || ActiveScreenContext.Instance.GetCurrentScreen()!=screen)return;
                var heir=Descendants(screen).OfType<NCharacterSelectButton>().FirstOrDefault(b=>b.Character is HeirCharacter);
                if(heir is null)return;
                HeirSelectIcon.Apply(heir);
                var rect=heir.GetGlobalRect();
                var info=(Control)AccessTools.Field(typeof(NCharacterSelectScreen),"_infoPanel").GetValue(screen)!;
                // Ascension occupies the band above the portraits. Reserve its right
                // side and put the Manor under the character information on the left.
                var ascension=(Control)AccessTools.Field(typeof(NCharacterSelectScreen),"_ascensionPanel").GetValue(screen)!;
                var x=info.GetGlobalRect().Position.X;
                // The native panel includes the Ascension icon to the left of its text.
                if(ascension.Visible)x=Math.Min(x,ascension.GetGlobalRect().Position.X-captured.Size.X-40);
                captured.GlobalPosition=new(x,rect.Position.Y-captured.Size.Y-24);
                heir.FocusNeighborBottom=captured.GetPath();captured.FocusNeighborTop=heir.GetPath();
                if(shortcutPressed)Open();
            }
            Tree.ProcessFrame+=Poll;
            screen.TreeExiting+=()=>{Tree.ProcessFrame-=Poll;launchers.Remove(id);};
        }
        button.Visible = character is HeirCharacter;
        if (character is not HeirCharacter) return;
        var background = (Control)AccessTools.Field(typeof(NCharacterSelectScreen),"_bgContainer").GetValue(screen)!;
        foreach (var child in background.GetChildren().OfType<CanvasItem>()) child.Visible = false;
        var art = background.GetNodeOrNull<TextureRect>("SpireLegacyBackground");
        if(art is null) {
            art = new TextureRect { Name="SpireLegacyBackground",Texture = Runtime.Texture("heir/ui/select-bg.png"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = Control.MouseFilterEnum.Ignore };
            background.AddChild(art); art.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        art.Visible=true;
    }
    public static Button MakeButton(string text, Action action, string? key=null)
    {
        // Node identity survives rebuilding and must not depend on enabled siblings.
        var button = new Button { Name=key ?? "Manor_"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)))[..16], Text = text, CustomMinimumSize = new(0,54), FocusMode = Control.FocusModeEnum.All };
        var normal=new StyleBoxFlat {BgColor=new("28344D"),BorderColor=new("526581"),BorderWidthLeft=1,BorderWidthRight=1,BorderWidthTop=1,BorderWidthBottom=1,CornerRadiusTopLeft=8,CornerRadiusTopRight=8,CornerRadiusBottomLeft=8,CornerRadiusBottomRight=8,ContentMarginLeft=18,ContentMarginRight=18,ContentMarginTop=10,ContentMarginBottom=10};
        button.AddThemeStyleboxOverride("normal",normal);
        var focus=new StyleBoxFlat {BgColor=Colors.Transparent,BorderColor=new("FFE0A0"),BorderWidthLeft=4,BorderWidthRight=4,BorderWidthTop=4,BorderWidthBottom=4,CornerRadiusTopLeft=8,CornerRadiusTopRight=8,CornerRadiusBottomLeft=8,CornerRadiusBottomRight=8};
        button.AddThemeStyleboxOverride("focus",focus);
        var hover=(StyleBoxFlat)normal.Duplicate(); hover.BgColor=new("3C4F70"); button.AddThemeStyleboxOverride("hover",hover);
        button.AddThemeColorOverride("font_color",new("F5F3EC"));
        button.AddThemeFontSizeOverride("font_size",24);
        var owner=modal;
        button.Pressed += () => {
            // Queued input can still refer to a button from the previous page.
            if(!GodotObject.IsInstanceValid(button)||!button.IsInsideTree()||button.IsQueuedForDeletion()||button.Disabled)return;
            if(owner is not null && (!ReferenceEquals(owner,modal)||!IsOpen))return;
            action();
        };
        return button;
    }
    private static Label Text(string value, int size = 23)
    {
        var label = new Label { Text = value, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FocusMode=Control.FocusModeEnum.None };
        label.AddThemeFontSizeOverride("font_size",size); return label;
    }
    public static void Open()
    {
        if (IsOpen) return;
        cart.Clear(); locations.Clear();page=""; wasPaused = Tree.Paused;
        ownerRun=MegaCrit.Sts2.Core.Nodes.NRun.Instance;
        ownerSelection=ownerRun is null && selectionScreen is {} screen && GodotObject.IsInstanceValid(screen) && screen.IsVisibleInTree()?screen:null;
        foreach(var node in new Node?[]{NInputManager.Instance,NControllerManager.Instance})
            if(node is not null) {inputModes[node]=node.ProcessMode;node.ProcessMode=Node.ProcessModeEnum.Always;}
        mouseMode = Input.MouseMode; Input.MouseMode = Input.MouseModeEnum.Visible;
        modal = new CanvasLayer { Name = "SpireLegacyManorModal", Layer = 110, ProcessMode = Node.ProcessModeEnum.Always };
        Tree.Root.AddChild(modal);
        var shade = input = new ManorInput();
        modal.AddChild(shade); shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var backdrop=new ColorRect {Color=new(0.025f,.035f,.07f,.97f),MouseFilter=Control.MouseFilterEnum.Ignore};
        shade.AddChild(backdrop); backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var margins = new MarginContainer(); shade.AddChild(margins); margins.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (var name in new[] {"margin_left","margin_right","margin_top","margin_bottom"}) margins.AddThemeConstantOverride(name,35);
        var frame=new VBoxContainer(); margins.AddChild(frame);
        scroll = new ScrollContainer { Name="ManorScroll", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, FollowFocus = false, HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled };
        scroll.GetVScrollBar().CustomMinimumSize=new(28,0);
        frame.AddChild(scroll);
        content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation",16); scroll.AddChild(content);
        var footer=new HBoxContainer(); frame.AddChild(footer);
        var help=Text("D-pad / left stick: move · A: select · B: back / close · LB / RB: scroll",18);
        help.FocusMode=Control.FocusModeEnum.None; footer.AddChild(help);
        closeButton=MakeButton("Close / Back (B)",GoBack); closeButton.CustomMinimumSize=new(245,60); footer.AddChild(closeButton);
        var closeStyle=(StyleBoxFlat)closeButton.GetThemeStylebox("normal").Duplicate(); closeStyle.BgColor=new("674D21"); closeStyle.BorderColor=new("E4BB69"); closeButton.AddThemeStyleboxOverride("normal",closeStyle);
        var multiplayer=CoopBloodlines.Lobby?.NetService.Type is MegaCrit.Sts2.Core.Multiplayer.Game.NetGameType.Host or MegaCrit.Sts2.Core.Multiplayer.Game.NetGameType.Client
            || PlayerBloodlines.State(PlayerBloodlines.LocalPlayer)?.coop==true;
        Tree.Paused = multiplayer?wasPaused:true; Render(); ActiveScreenContext.Instance.Update(); Tree.ProcessFrame+=input.Poll;
    }
    private static void BeginPage(string next)
    {
        if(page.Length>0)locations[page]=(Tree.Root.GuiGetFocusOwner()?.Name.ToString(),scroll?.ScrollVertical ?? 0);
        page=next;Clear();
    }
    private static void Clear()
    {
        foreach (var child in content!.GetChildren()) { content.RemoveChild(child); child.QueueFree(); }
        if(scroll is not null)scroll.ScrollVertical=0;
    }
    private static void AddButton(string text, Action action) => content!.AddChild(MakeButton(text,action));
    private static void AddText(string text, int size = 23) => content!.AddChild(Text(text,size));
    private static bool CanConfigure() => (PlayerBloodlines.State(PlayerBloodlines.LocalPlayer)??Runtime.Profile.active) is null
        && !(CoopBloodlines.Lobby is {} lobby && CoopBloodlines.IsReady(CoopBloodlines.Local(lobby)));
    private static void Render()
    {
        back=RequestClose;
        BeginPage("manor"); var profile = Runtime.Profile;
        var heading = Text($"{profile.family} Family Manor — generation {profile.generation + 1}",32); heading.Modulate = BannerColor; content!.AddChild(heading);
        AddText($"Crowns: {profile.crowns} · Banner: {BannerNames[profile.banner]} · Last heir earned {profile.lastEarned}");
        AddText($"Spire Legacy {CardPlayDiagnostics.Version} · StS2 {MegaCrit.Sts2.Core.Debug.ReleaseInfoManager.Instance.ReleaseInfo?.Version}",20);
        if(CardPlayDiagnostics.LastFailure is {} cardFailure) {
            AddText("Card play failed. Full diagnostic:",28);
            AddText(cardFailure,20);
            AddButton("Copy card-play diagnostic",()=>DisplayServer.ClipboardSet(cardFailure));
        }
        if(Notice is {} notice) AddText(notice,26);
        var options = new VBoxContainer(); content.AddChild(options);
        options.AddChild(MakeButton("Name your family",() => EditValue(false)));
        var unread=Design.Rows("family_chronicle").Count(r=>profile.chronicle.unlocked.Contains(r.Text("id"))&&!profile.chronicle.read.Contains(r.Text("id")));
        options.AddChild(MakeButton($"Family Chronicle · {unread} unread",ShowChronicle,"ManorChronicle"));
        options.AddChild(MakeButton("Change banner",() => { profile.banner = (profile.banner + 1) % 4; profile.Save(Runtime.FamilyPath); Render(); }));
        if (Design.Get("systems","playtest_crowns").Flag("enabled")) options.AddChild(MakeButton("PLAYTEST: Set crowns",() => EditValue(true)));
        options.AddChild(MakeButton("Close",RequestClose));
        var playing=PlayerBloodlines.State(PlayerBloodlines.LocalPlayer)??profile.active;
        var ready=CoopBloodlines.Lobby is {} lobby&&CoopBloodlines.IsReady(CoopBloodlines.Local(lobby));
        if (playing is {} active)
        {
            AddText("A climb is in progress. Manor purchases and heir selection are available after it ends.");
            HeirDetails(active.heir,false);
        }
        else
        {
            AddText("Choose your next heir",28);
            if(ready)AddText("Unready in the lobby before changing your heir or manor. Ready freezes your selection for every player.");
            foreach (var heir in profile.offers) HeirDetails(heir,!ready);
        }
        AddText("Manor upgrades — select upgrades, then review and confirm to spend crowns",28);
        var trial = profile.PreviewPurchases(cart) ?? profile;
        foreach (var row in Design.Rows("manor"))
        {
            var id = row.Text("id");
            AddText($"{row.Text("name")} · level {trial.Level(id)}/{row.Number("maxLevel")} · {row.Text("summary")}");
            var button = MakeButton($"Select upgrade — {trial.Cost(row)} crowns",() => { if (CanConfigure() && profile.PreviewPurchases(cart.Append(id)) is not null) { cart.Add(id); Render(); } },"ManorUpgrade_"+id);
            button.Disabled = playing != null || ready || profile.PreviewPurchases(cart.Append(id)) is null;
            content.AddChild(button);
        }
        if (cart.Count > 0)
        {
            AddText($"Selected {cart.Count} upgrades · total {profile.crowns - trial.crowns} crowns · remaining {trial.crowns}");
            AddButton("Review purchases",Review);
            AddButton("Clear selected upgrades",() => { cart.Clear(); Render(); });
        }
        ConfigureNavigation();
    }
    private static void HeirDetails(FamilyProfile.Heir heir, bool selectable)
    {
        var cls = Design.Get("classes",heir.classId);
        var selected = Runtime.Profile.selected is {} chosen&&chosen.name==heir.name&&chosen.classId==heir.classId&&chosen.traits.SequenceEqual(heir.traits);
        AddText($"{(selected ? "✓ " : "")}{heir.name} {Runtime.Profile.family} — {cls.Text("name")}",27);
        AddText(cls.Text("summary"));
        foreach (var trait in Inheritance.Rows(heir))
        {
            var detail = new HBoxContainer(); content!.AddChild(detail);
            detail.AddChild(new TextureRect { Texture = Runtime.TraitIcon(trait.Text("id")), CustomMinimumSize = new(48,48), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
            detail.AddChild(Text($"{trait.Text("name")}: {trait.Text("summary")}\nCrown bonus: {trait.Number("goldBonus")}%"));
        }
        if (selectable) content!.AddChild(MakeButton(selected ? "Selected for next climb" : "Choose this heir",() => { if(CanConfigure())Runtime.Profile.CommitHeirSelection(heir,Runtime.FamilyPath); Render(); },"ManorHeir_"+Runtime.Profile.offers.IndexOf(heir)));
    }
    public static void ShowChronicle()
    {
        back=Render;BeginPage("chronicle");var profile=Runtime.Profile;
        AddText($"{profile.family} · Family Chronicle",32);
        AddText("Family history and class lessons. Finish a climb to record its milestones; new relics enter future climbs. Existing cards, relics and potions remain available.");
        AddText("D-pad / stick: choose an entry · A: read · LB / RB: scroll · B: return",20);
        foreach(var row in Design.Rows("family_chronicle")) {
            var id=row.Text("id");var unlocked=profile.chronicle.unlocked.Contains(id);
            AddText(row.Text("requirement"));
            if(!unlocked)AddText(FamilyChronicle.Progress(profile,row),20);
            AddText(FamilyChronicle.Reward(row),20);
            var button=MakeButton((unlocked?profile.chronicle.read.Contains(id)?"Read: ":"NEW · Read: ":"Locked: ")+row.Text("title"),()=>ReadChronicle(id));
            button.Disabled=!unlocked;content!.AddChild(button);
        }
        if(profile.chronicle.motto.Length>0)AddButton("Clear family motto",()=>{SaveMotto("");ShowChronicle();});
        AddButton("Back to Family Manor",Render);ConfigureNavigation();
    }
    public static void ReadChronicle(string id)
    {
        var profile=Runtime.Profile;if(!profile.chronicle.unlocked.Contains(id))return;
        var row=Design.Get("family_chronicle",id);
        // Persist before rendering, so a failed write cannot silently mark a story read.
        if(!profile.CommitChronicleRead(id,Runtime.FamilyPath))return;
        back=ShowChronicle;BeginPage("story_"+id);AddText(row.Text("title"),32);
        AddText(row.Text("story").Replace("{Family}",profile.family),25);
        AddText(FamilyChronicle.Reward(row),23);
        if(row.Text("rewardKind")=="relic")AddText(Design.Get("heir_relics",row.Text("rewardId")).Text("description"));
        if(row.Text("rewardKind")=="motto")AddButton("Use family motto: "+row.Text("rewardId"),()=>{SaveMotto(row.Text("rewardId"));ReadChronicle(id);});
        AddButton("Back to Chronicle",ShowChronicle);ConfigureNavigation();
    }
    private static void SaveMotto(string motto)
    {
        Runtime.Profile.CommitMotto(motto,Runtime.FamilyPath);
    }
    private static void Review()
    {
        back=Render;
        BeginPage("review"); var profile = Runtime.Profile; var trial = profile.PreviewPurchases(cart);
        if (trial == null) { cart.Clear(); Render(); return; }
        AddText("Confirm manor purchases",32);
        foreach (var group in cart.GroupBy(x => x)) AddText($"{Design.Get("manor",group.Key).Text("name")} × {group.Count()}");
        AddText($"Spend {profile.crowns - trial.crowns} crowns? You will have {trial.crowns} left.");
        AddButton("Confirm and save",() => { if (CanConfigure() && profile.CommitPurchases(cart,Runtime.FamilyPath)) cart.Clear(); Render(); });
        AddButton("Back to selection",Render); ConfigureNavigation();
    }
    private static void EditValue(bool crowns)
    {
        back=Render;
        BeginPage(crowns ? "crowns" : "name"); AddText(crowns ? "PLAYTEST: Set crown balance" : "Name your family",32);
        var entry = new LineEdit { Name="ManorEditor", Text = crowns ? Runtime.Profile.crowns.ToString() : Runtime.Profile.family, MaxLength = crowns ? 10 : 24, VirtualKeyboardEnabled = true, CustomMinimumSize = new(0,60) };
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
            var keys = new HFlowContainer(); content.AddChild(keys);
            foreach (var letter in letters) { var captured = letter; keys.AddChild(MakeButton(letter.ToString(),() => { if (entry.Text.Length < entry.MaxLength) entry.Text += captured; })); }
        }
        AddButton("Backspace",() => { if (entry.Text.Length > 0) entry.Text = entry.Text[..^1]; });
        if (!crowns) AddButton("Space",() => { if (entry.Text.Length < 24) entry.Text += " "; });
        AddButton("Clear text",() => entry.Text = ""); ConfigureNavigation(entry.Name);
    }
    private static void RequestClose()
    {
        if (cart.Count == 0) { Close(); return; }
        back=Render;
        BeginPage("discard"); AddText("Discard the selected upgrades? Your crowns have not been spent.",30);
        AddButton("Keep selecting",Render); AddButton("Discard and close",Close); ConfigureNavigation();
    }
    public static void Close()
    {
        if (!IsOpen) return;
        navigationGeneration++;
        if(input is not null)Tree.ProcessFrame-=input.Poll;
        input=null;cart.Clear(); modal?.QueueFree(); modal = null; content=null;scroll=null;closeButton=null; Tree.Paused = wasPaused; Input.MouseMode = mouseMode;
        foreach(var pair in inputModes)if(GodotObject.IsInstanceValid(pair.Key))pair.Key.ProcessMode=pair.Value;
        inputModes.Clear();
        ActiveScreenContext.Instance.Update();
        if (selectionScreen is {} screen && GodotObject.IsInstanceValid(screen) && screen.IsVisibleInTree())
        {
            var button = (NCharacterSelectButton?)AccessTools.Field(typeof(NCharacterSelectScreen),"_selectedButton").GetValue(screen);
            if (button?.Character is HeirCharacter && !CoopBloodlines.IsReady(CoopBloodlines.Local(screen.Lobby))) {screen.SelectCharacter(button,button.Character);button.GrabFocus();}
        }
    }
}

// A normally triggers Embark globally. Let it activate a focused Manor button instead.
[HarmonyPatch(typeof(NCharacterSelectScreen),"OnEmbarkPressed")]
public static class ManorSelectAction
{
    public static bool Prefix(NCharacterSelectScreen __instance)
    {
        if(ManorUi.IsOpen)return false;
        if(__instance.GetViewport().GuiGetFocusOwner() is Button {Name:var name} && name=="SpireLegacyManor") {ManorUi.Open();return false;}
        if(__instance.Lobby.NetService.Type!=MegaCrit.Sts2.Core.Multiplayer.Game.NetGameType.Singleplayer && Runtime.Profile.active!=null && CoopBloodlines.Character(CoopBloodlines.Local(__instance.Lobby)) is HeirCharacter or MegaCrit.Sts2.Core.Models.Characters.RandomCharacter) {
            ManorUi.Notice="Resume or abandon your unfinished family climb before starting a co-op run.";ManorUi.Open();return false;
        }
        return true;
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

[HarmonyPatch(typeof(NCharacterSelectScreen),"get_InitialFocusedControl")]
public static class PreserveHeirControllerFocus
{
    public static void Postfix(NCharacterSelectScreen __instance,ref Control __result)
    {
        // Switching from touch/mouse to controller otherwise focuses the first
        // portrait, selects Ironclad, and hides the Heir's shortcut before it runs.
        var selected=(NCharacterSelectButton?)AccessTools.Field(typeof(NCharacterSelectScreen),"_selectedButton").GetValue(__instance);
        if(selected?.Character is HeirCharacter)__result=selected;
    }
}
