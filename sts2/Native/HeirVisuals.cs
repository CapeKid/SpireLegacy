using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SpireLegacy;

public static class HeirVisuals
{
    private static readonly List<(NCreature creature, HeirSprite sprite, Polygon2D banner,Label symbol, TraitAppearance appearance)> actors = new();
    private static readonly string[] Symbols=["☀","☾","✿","♣"];
    public static void Initialize() => ((SceneTree)Engine.GetMainLoop()).ProcessFrame += Update;
    public static void Shutdown()=>((SceneTree)Engine.GetMainLoop()).ProcessFrame-=Update;
    public static void AttachRoomPortrait(Node2D visual,MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        foreach(var child in visual.GetChildren().OfType<Node2D>().Where(n=>n.GetClass()=="SpineSprite"))child.Visible=false;
        if (visual.GetNodeOrNull<Node>("SpireLegacyRoomHeir") != null) return;
        // Native rest-site roots shrink large Spine art; compensate for our smaller atlas.
        var factor = PlayerBloodlines.Traits(player).Traits.Aggregate(1f,(value,row)=>value * row.Json.GetProperty("scale").GetSingle());
        var roomSize = (visual is MegaCrit.Sts2.Core.Nodes.RestSite.NRestSiteCharacter ? 3.6f : 1.4f) * factor;
        var portrait = HeirSprite.Create(PlayerBloodlines.Heir(player).classId, roomSize);
        portrait.Name = "SpireLegacyRoomHeir"; portrait.Modulate = Colors.White;
        visual.AddChild(portrait);
        HeirRoomMotion.Attach(visual,portrait,visual is MegaCrit.Sts2.Core.Nodes.RestSite.NRestSiteCharacter);
        TraitAppearance.Attach(visual,portrait,roomSize,PlayerBloodlines.Heir(player));
    }
    public static void Attach(NCreature creature)
    {
        if (creature.Entity.Player?.Character is not HeirCharacter) return;
        var player=creature.Entity.Player!;
        var visuals = creature.Visuals;
        visuals.GetCurrentBody().Visible = false;
        if (visuals.GetNodeOrNull<Node>("SpireLegacyHeir") != null) return;
        var factor = PlayerBloodlines.Traits(player).Traits.Aggregate(1f,(value,row)=>value * row.Json.GetProperty("scale").GetSingle());
        var sprite = HeirSprite.Create(PlayerBloodlines.Heir(player).classId, factor);
        sprite.Name = "SpireLegacyHeir";
        visuals.AddChild(sprite);
        var badge = new Sprite2D { Texture = Runtime.ClassIcon(PlayerBloodlines.Heir(player).classId), Position = new(0,-230 * factor - 35), Scale = Vector2.One * .3f };
        visuals.AddChild(badge);
        var banner = new Polygon2D { Name = "FamilyBanner", Polygon = [new(-190,-210),new(-135,-210),new(-135,-155),new(-163,-135),new(-190,-155)], Color = ManorUi.BannerColorFor(Banner(player)), ZIndex = 2 };
        visuals.AddChild(banner);
        var symbol = new Label { Text = Symbols[Banner(player)], Position = new(-184,-201), MouseFilter = Control.MouseFilterEnum.Ignore };
        symbol.AddThemeFontSizeOverride("font_size",32); banner.AddChild(symbol);
        var motto=new Label {Name="FamilyMotto",Text=Motto(player),Position=new(-225,-125),Size=new(220,50),AutowrapMode=TextServer.AutowrapMode.WordSmart,MouseFilter=Control.MouseFilterEnum.Ignore};
        motto.AddThemeFontSizeOverride("font_size",18);banner.AddChild(motto);
        actors.Add((creature,sprite,banner,symbol,TraitAppearance.Attach(visuals,sprite,factor,PlayerBloodlines.Heir(player))));
    }
    private static int Banner(MegaCrit.Sts2.Core.Entities.Players.Player player)=>PlayerBloodlines.State(player) is {coop:true} run?run.banner:Runtime.Profile.banner;
    private static string Motto(MegaCrit.Sts2.Core.Entities.Players.Player player)=>PlayerBloodlines.State(player) is {coop:true} run?run.motto:Runtime.Profile.chronicle.motto;
    public static void Animate(MegaCrit.Sts2.Core.Entities.Creatures.Creature creature, string pose)
    {
        foreach (var actor in actors.Where(a => GodotObject.IsInstanceValid(a.sprite) && a.creature.Entity == creature)) actor.sprite.PlayPose(pose);
    }
    public static void DamageFlash(MegaCrit.Sts2.Core.Entities.Creatures.Creature creature)
    {
        foreach(var actor in actors.Where(a=>GodotObject.IsInstanceValid(a.sprite) && a.creature.Entity==creature))actor.appearance.DamageFlash();
    }
    public static object Diagnostic() => actors.Where(a => GodotObject.IsInstanceValid(a.sprite)).Select(a => new {
        owner=a.creature.Entity.Player!.NetId,bannerIndex=Banner(a.creature.Entity.Player!),motto=Motto(a.creature.Entity.Player!),
        classId = a.sprite.ClassId, pose = a.sprite.Animation.ToString(), frame = a.sprite.Frame,
        frames = a.sprite.SpriteFrames.GetFrameCount(a.sprite.Animation), deathFrames = a.sprite.DeathFrames.ToArray(), visible = a.sprite.IsVisibleInTree(), tint = a.sprite.Modulate.ToString(), events = a.sprite.Events.ToArray(),
        position = a.sprite.GlobalPosition.ToString(), scale = a.sprite.Scale.ToString(), hp = a.creature.Entity.CurrentHp,
        bannerVisible = a.banner.IsVisibleInTree(), bannerZ = a.banner.ZIndex
    }).ToArray();
    private static void Update()
    {
        TraitAppearance.Update();
        actors.RemoveAll(actor => !GodotObject.IsInstanceValid(actor.creature) || !GodotObject.IsInstanceValid(actor.sprite));
        foreach (var actor in actors)
        {
            var player=actor.creature.Entity.Player!;
            actor.creature.Visuals.GetCurrentBody().Visible = false;
            if (actor.creature.Entity.IsDead) actor.sprite.PlayPose("dead");
            actor.banner.Color = ManorUi.BannerColorFor(Banner(player));
            actor.symbol.Text=Symbols[Banner(player)];
            actor.banner.GetNode<Label>("FamilyMotto").Text=Motto(player);
        }
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.RestSite.NRestSiteCharacter),"_Ready")]
public static class HeirRestPortrait
{
    public static void Postfix(MegaCrit.Sts2.Core.Nodes.RestSite.NRestSiteCharacter __instance) {
        if(__instance.Player.Character is HeirCharacter)HeirVisuals.AttachRoomPortrait(__instance,__instance.Player);
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom),"_Ready")]
public static class HeirMerchantPortrait
{
    public static void Postfix(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom __instance) {
        var players=(List<MegaCrit.Sts2.Core.Entities.Players.Player>)AccessTools.Field(__instance.GetType(),"_players").GetValue(__instance)!;
        for(var index=0;index<Math.Min(players.Count,__instance.PlayerVisuals.Count);index++)if(players[index].Character is HeirCharacter)HeirVisuals.AttachRoomPortrait(__instance.PlayerVisuals[index],players[index]);
    }
}

[HarmonyPatch(typeof(CharacterModel),"get_IconTexture")]
public static class HeirHistoryPortrait
{
    public static void Postfix(CharacterModel __instance,ref Texture2D __result) {
        if(__instance is HeirCharacter heir)__result=Runtime.ClassIcon(heir.ClassId);
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
        var player=state.Players.FirstOrDefault(p=>p.NetId==MegaCrit.Sts2.Core.Runs.RunManager.Instance.NetService.NetId);
        if(player?.Character is not HeirCharacter)return;
        var heir = PlayerBloodlines.Heir(player);
        var family=PlayerBloodlines.State(player) is {coop:true} bloodline?bloodline.family:Runtime.Profile.family;
        var button = ManorUi.MakeButton($"{heir.name} {family} · Family / Traits",ManorUi.Open);
        button.Position = new(30,150); button.Size = new(580,54);
        __instance.AddChild(button);
        var traits=new HBoxContainer {Position=new(30,212)};
        __instance.AddChild(traits);
        foreach(var row in Inheritance.Rows(heir)) {
            var info=$"{row.Text("name")}: {row.Text("summary")}\nCrown bonus: {row.Number("goldBonus")}%";
            traits.AddChild(new TextureRect {Texture=Runtime.TraitIcon(row.Text("id")),CustomMinimumSize=new(48,48),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,TooltipText=info,MouseFilter=Control.MouseFilterEnum.Stop});
            traits.AddChild(new Label {Text=row.Text("name"),TooltipText=info,MouseFilter=Control.MouseFilterEnum.Stop});
        }
    }
}

[HarmonyPatch(typeof(PowerModel),"get_Icon")]
public static class HeirClassPowerIcon
{
    public static bool Prefix(PowerModel __instance,ref Texture2D __result) {
        if(__instance is not P_class) return true;
        __result=Runtime.ClassIcon(PlayerBloodlines.Heir(__instance.IsMutable?__instance.Owner.Player:null).classId);return false;
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

[HarmonyPatch(typeof(NCreature),nameof(NCreature.SetAnimationTrigger))]
public static class HeirAnimationTrigger
{
    public static void Postfix(NCreature __instance, string trigger)
    {
        if (__instance.Entity.Player?.Character is not HeirCharacter) return;
        var pose = trigger switch { "Attack" => "attack", "Cast" or "PowerUp" => "skill", "Hit" => "hurt", "Dead" => "dead", "Revive" => "idle", _ => null };
        if (pose != null) HeirVisuals.Animate(__instance.Entity,pose);
    }
}
