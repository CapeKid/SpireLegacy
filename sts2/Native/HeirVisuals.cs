using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SpireLegacy;

public static class HeirVisuals
{
    private static readonly List<(NCreature creature, Sprite2D sprite, Polygon2D banner,Label symbol)> actors = new();
    private static readonly string[] Symbols=["☀","☾","✿","♣"];
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Update;
    public static void AttachRoomPortrait(Node2D visual)
    {
        if(!ContentBootstrap.Ready)return;
        foreach(var child in visual.GetChildren().OfType<Node2D>().Where(n=>n.GetClass()=="SpineSprite"))child.Visible=false;
        var image=Runtime.CacheTexture("hero.png");
        visual.AddChild(new Sprite2D {Name="SpireLegacyRoomHeir",Texture=image,Scale=Vector2.One*.75f,Position=new(0,-image.GetHeight()*.375f),Modulate=ManorUi.BannerColor});
    }
    public static void Attach(NCreature creature)
    {
        if (creature.Entity.Player?.Character is not HeirCharacter || !ContentBootstrap.Ready) return;
        var visuals = creature.Visuals;
        visuals.GetCurrentBody().Visible = false;
        var texture = Runtime.CacheTexture("hero.png");
        var factor = Inheritance.Traits.Aggregate(.5f,(value,row)=>value * row.Json.GetProperty("scale").GetSingle());
        var sprite = new Sprite2D { Name = "SpireLegacyHeir", Texture = texture, Scale = Vector2.One * factor, Position = new(0,-texture.GetHeight() * factor / 2) };
        if(Inheritance.Has("gray")) sprite.Material=new ShaderMaterial {Shader=new Shader {Code="shader_type canvas_item; void fragment() { vec4 c=texture(TEXTURE,UV)*COLOR; float g=dot(c.rgb,vec3(0.299,0.587,0.114)); COLOR=vec4(vec3(g),c.a); }"}};
        visuals.AddChild(sprite);
        var badge = new Sprite2D { Texture = Runtime.CacheTexture(Design.Get("classes",Runtime.Heir.classId).Text("asset") + ".png"), Position = new(0,-texture.GetHeight() * factor - 35), Scale = Vector2.One * .3f };
        visuals.AddChild(badge);
        var banner = new Polygon2D { Name = "FamilyBanner", Polygon = [new(48,-165),new(115,-165),new(115,-85),new(81,-65),new(48,-85)], Color = ManorUi.BannerColor };
        visuals.AddChild(banner);
        var symbol = new Label { Text = Symbols[Runtime.Profile.banner], Position = new(57,-153), MouseFilter = Control.MouseFilterEnum.Ignore };
        symbol.AddThemeFontSizeOverride("font_size",40); banner.AddChild(symbol);
        actors.Add((creature,sprite,banner,symbol));
    }
    private static void Update()
    {
        actors.RemoveAll(actor => !GodotObject.IsInstanceValid(actor.creature) || !GodotObject.IsInstanceValid(actor.sprite));
        foreach (var actor in actors)
        {
            var color = ManorUi.BannerColor;
            if (Inheritance.Has("gray")) color = Colors.LightGray;
            else if (Inheritance.Has("blue")) color = Colors.Blue;
            else if (Inheritance.Has("sepia")) color = new(.7f,.55f,.3f);
            else if (Inheritance.Has("nature")) color = Colors.Green;
            else if (Inheritance.Has("medium")) color = new(.65f,.45f,.9f);
            else if (Inheritance.Has("festive")) color = Colors.Crimson;
            else if (Inheritance.Has("rainbow")) { var t = Time.GetTicksMsec() % 6000 / 6000f * Mathf.Tau; color = new(.65f+.35f*Mathf.Sin(t),.65f+.35f*Mathf.Sin(t+2),.65f+.35f*Mathf.Sin(t+4)); }
            else if (Inheritance.Has("histrionic") && actor.creature.Entity.CurrentHp < actor.creature.Entity.MaxHp) color = new(1,.5f,.5f);
            actor.sprite.Modulate = color; actor.banner.Color = ManorUi.BannerColor;
            actor.symbol.Text=Symbols[Runtime.Profile.banner];
        }
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.RestSite.NRestSiteCharacter),"_Ready")]
public static class HeirRestPortrait
{
    public static void Postfix(MegaCrit.Sts2.Core.Nodes.RestSite.NRestSiteCharacter __instance) {
        if(__instance.Player.Character is HeirCharacter)HeirVisuals.AttachRoomPortrait(__instance);
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom),"_Ready")]
public static class HeirMerchantPortrait
{
    public static void Postfix(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom __instance) {
        var players=(List<MegaCrit.Sts2.Core.Entities.Players.Player>)AccessTools.Field(__instance.GetType(),"_players").GetValue(__instance)!;
        for(var index=0;index<Math.Min(players.Count,__instance.PlayerVisuals.Count);index++)if(players[index].Character is HeirCharacter)HeirVisuals.AttachRoomPortrait(__instance.PlayerVisuals[index]);
    }
}

[HarmonyPatch(typeof(CharacterModel),"get_IconTexture")]
public static class HeirHistoryPortrait
{
    public static void Postfix(CharacterModel __instance,ref Texture2D __result) {
        if(__instance is HeirCharacter)__result=Runtime.Texture("heir/ui/select-icon.png");
    }
}

[HarmonyPatch(typeof(MegaCrit.sts2.Core.Nodes.TopBar.NTopBarPortrait),"Initialize")]
public static class HeirTopBarPortrait
{
    public static void Postfix(MegaCrit.sts2.Core.Nodes.TopBar.NTopBarPortrait __instance,MegaCrit.Sts2.Core.Entities.Players.Player player) {
        if(player.Character is not HeirCharacter heir)return;
        foreach(var child in __instance.GetChildren().OfType<CanvasItem>())child.Visible=false;
        __instance.AddChild(heir.CustomIcon);
    }
}

[HarmonyPatch(typeof(NCreature),nameof(NCreature._Ready))]
public static class HeirCombatPortrait
{
    public static void Postfix(NCreature __instance) => HeirVisuals.Attach(__instance);
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectButton),"Init")]
public static class HeirSelectIcon
{
    public static void Postfix(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectButton __instance) => Apply(__instance);
    internal static void Apply(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectButton button)
    {
        if(button.Character is not HeirCharacter)return;
        // Native model getters require CompressedTexture2D; external PNGs are ImageTexture.
        // TextureRect accepts either, so replace the actual picker textures instead.
        foreach(var field in new[]{"_icon","_iconAdd"})
            if(AccessTools.Field(button.GetType(),field).GetValue(button) is TextureRect icon) {
                icon.Texture=Runtime.Texture("heir/ui/heir-portrait.png");
                icon.ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize;
                // Picker portraits fill a tall tile; a square cutout creates empty bands.
                icon.StretchMode=TextureRect.StretchModeEnum.Scale;
            }
    }
}

[HarmonyPatch(typeof(NRun),nameof(NRun._Ready))]
public static class HeirRunDetailsButton
{
    public static void Postfix(NRun __instance)
    {
        var state = (MegaCrit.Sts2.Core.Runs.RunState)AccessTools.Field(typeof(NRun),"_state").GetValue(__instance)!;
        if (!state.Players.Any(p=>p.Character is HeirCharacter)) return;
        var heir = Runtime.Heir;
        var button = ManorUi.MakeButton($"{heir.name} {Runtime.Profile.family} · Family / Traits",ManorUi.Open);
        button.Position = new(30,150); button.Size = new(580,54);
        __instance.AddChild(button);
        var traits=new HBoxContainer {Position=new(30,212)};
        __instance.AddChild(traits);
        foreach(var row in Inheritance.Rows(heir)) {
            var info=$"{row.Text("name")}: {row.Text("summary")}\nCrown bonus: {row.Number("goldBonus")}%";
            if(ContentBootstrap.Ready) traits.AddChild(new TextureRect {Texture=Runtime.CacheTexture(row.Text("asset")+".png"),CustomMinimumSize=new(48,48),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,TooltipText=info,MouseFilter=Control.MouseFilterEnum.Stop});
            traits.AddChild(new Label {Text=row.Text("name"),TooltipText=info,MouseFilter=Control.MouseFilterEnum.Stop});
        }
    }
}

[HarmonyPatch(typeof(PowerModel),"get_Icon")]
public static class HeirClassPowerIcon
{
    public static bool Prefix(PowerModel __instance,ref Texture2D __result) {
        if(__instance is not P_class || !ContentBootstrap.Ready) return true;
        __result=Runtime.CacheTexture(Design.Get("classes",Runtime.Heir.classId).Text("asset")+".png");return false;
    }
}

[HarmonyPatch(typeof(RelicModel),"get_Icon")]
public static class HeirBloodlineIcon
{
    public static bool Prefix(RelicModel __instance,ref Texture2D __result) {
        if(__instance is not BloodlineRelic) return true;
        __result=Runtime.Texture("heir/ui/select-icon.png");return false;
    }
}
