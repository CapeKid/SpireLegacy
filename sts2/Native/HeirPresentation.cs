using BaseLib.Patches.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using System.Reflection;
using System.Text.Json;

namespace SpireLegacy;

// These runtime SVG textures are owned by the mod, not imported Godot resources.
// Keep them out of native threaded loading/disposal and resolve them on demand.
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Assets.AssetCache), nameof(MegaCrit.Sts2.Core.Assets.AssetCache.CreateSession))]
public static class HeirRuntimeTexturePreload
{
    public static bool Owns(string path)=>path.StartsWith("res://SpireLegacy/heir/identity/",StringComparison.Ordinal)||path.StartsWith("res://SpireLegacy/heir/presentation/",StringComparison.Ordinal)||path.StartsWith("res://SpireLegacy/heir/relics/",StringComparison.Ordinal);
    public static void Prefix(ref IEnumerable<string> paths)=>paths=paths.Where(p=>!Owns(p)).ToArray();
}
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Assets.AssetCache), nameof(MegaCrit.Sts2.Core.Assets.AssetCache.GetTexture2D))]
public static class HeirRuntimeTextureLookup
{
    public static bool Prefix(string path,ref Texture2D __result)
    {
        if(!HeirRuntimeTexturePreload.Owns(path))return true;
        __result=path.StartsWith("res://SpireLegacy/heir/identity/",StringComparison.Ordinal)
            ?Runtime.ClassIcon(Path.GetFileNameWithoutExtension(path))
            :Runtime.Texture(path["res://SpireLegacy/".Length..]);
        return false;
    }
}

// Keep native gameplay containers; replace their visible identity with original assets.
public static class HeirPresentation
{
    private static readonly Dictionary<string,Resource> Resources=new();
    public static readonly List<string> Sounds=new();
    public static string ClassId
    {
        get {
            if(NRun.Instance!=null && PlayerBloodlines.LocalPlayer is {} player)
                return ClassFor(player);
            return Runtime.Heir.classId;
        }
    }
    public static string ClassFor(Player? player)
    {
        if(PlayerBloodlines.State(player) is {} run)return run.heir.classId;
        var saved=player?.Relics.OfType<BloodlineRelic>().FirstOrDefault()?.SelectedHeir;
        if(!string.IsNullOrEmpty(saved)) {
            try {if(JsonSerializer.Deserialize<FamilyProfile.Heir>(saved,FamilyProfile.JsonOptions) is {} h && h.classId is "knight" or "mage" or "ranger")return h.classId;}
            catch(JsonException) { }
        }
        return Runtime.Heir.classId;
    }
    public static Color Accent(string? cls=null)=>new((cls??ClassId) switch {"mage"=>"BB94E9","ranger"=>"83C690",_=>"DFB968"});
    public static string TexturePath(string name)
    {
        var relative="heir/presentation/"+name+".svg";var path="res://SpireLegacy/"+relative;
        var tex=Runtime.Texture(relative);if(tex.ResourcePath!=path)tex.TakeOverPath(path);return path;
    }
    public static string IconPath
    {
        get => IconFor(ClassId);
    }
    public static string IconFor(string cls){var tex=Runtime.ClassIcon(cls);var path="res://SpireLegacy/heir/identity/"+cls+".svg";if(tex.ResourcePath!=path)tex.TakeOverPath(path);return path;}
    public static RelicIconData Cookie(string? cls=null)
    {
        var c=cls??ClassId;var p=TexturePath(c+"-cookie");return new(p,p,TexturePath(c+"-cookie-outline"));
    }
    public static string Sound(string role)=>"spirelegacy://"+ClassId+"/"+role;
    public static string SoundFor(string cls,string role)=>"spirelegacy://"+cls+"/"+role;
    public static bool PlaySound(string path,float volume)
    {
        if(!path.StartsWith("spirelegacy://",StringComparison.Ordinal))return false;
        var parts=path[14..].Split('/');
        if(parts.Length!=2||parts[0] is not ("knight" or "mage" or "ranger")||parts[1] is not ("attack" or "cast" or "die" or "select" or "transition"))throw new InvalidDataException("Invalid Heir sound.");
        if(MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn)return true;
        var key="audio:"+path;
        if(!Resources.TryGetValue(key,out var resource)) {
            var stream=new AudioStreamWav{Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=22050,Stereo=false};
            var wav=File.ReadAllBytes(Path.Combine(Runtime.ModDirectory,"heir/presentation/"+parts[0]+"-"+parts[1]+".wav"));
            // The authoring route writes canonical PCM WAVs with a 44-byte header.
            stream.Data=wav[44..];Resources[key]=resource=stream;
        }
        var settings=SaveManager.Instance.SettingsSave;
        var audio=new AudioStreamPlayer{Stream=(AudioStream)resource,VolumeLinear=Math.Clamp(volume,0,1)*MathF.Pow(settings.VolumeMaster,2)*MathF.Pow(settings.VolumeSfx,2)};
        var tree=(SceneTree)Engine.GetMainLoop();tree.Root.AddChild(audio);audio.Finished+=audio.QueueFree;audio.Play();
        Sounds.Add(path);if(Sounds.Count>48)Sounds.RemoveAt(0);return true;
    }
    // File-backed resources survive the native loader's deferred disposal boundaries.
    public static string TransitionPath=>Path.Combine(Runtime.ModDirectory,"heir/presentation/"+ClassId+"-transition.tres").Replace('\\','/');
    public static string TrailPath=>Path.Combine(Runtime.ModDirectory,"heir/presentation/"+ClassId+"-trail.tscn").Replace('\\','/');
    public static string TransitionFor(string cls)=>Path.Combine(Runtime.ModDirectory,"heir/presentation/"+cls+"-transition.tres").Replace('\\','/');
    public static string TrailFor(string cls)=>Path.Combine(Runtime.ModDirectory,"heir/presentation/"+cls+"-trail.tscn").Replace('\\','/');
    public static void ArchitectBurst(Node parent,Vector2 position,string cls)
    {
        var root=new Node2D{Name="HeirArchitect"+cls,Position=position};root.SetMeta("spirelegacy_architect",cls);parent.AddChild(root);
        var color=Accent(cls);
        if(cls=="mage") {
            for(var i=0;i<8;i++){var angle=i*Mathf.Tau/8;var v=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));root.AddChild(new Line2D{Points=[v*15,v*75],Width=7,DefaultColor=color,Antialiased=true});}
        } else if(cls=="ranger") {
            for(var i=-1;i<=1;i++){var y=i*22;root.AddChild(new Line2D{Points=[new(-90,y),new(65,y),new(45,y-12),new(65,y),new(45,y+12)],Width=5,DefaultColor=color,Antialiased=true});}
        } else {
            root.AddChild(new Line2D{Points=[new(-90,55),new(-30,20),new(30,-20),new(90,-65)],Width=14,DefaultColor=color,Antialiased=true});
            root.AddChild(new Line2D{Points=[new(-55,-70),new(50,65)],Width=5,DefaultColor=new Color(1,.92f,.7f),Antialiased=true});
        }
        root.Scale=Vector2.One*.35f;var tween=root.CreateTween().SetParallel();tween.TweenProperty(root,"scale",Vector2.One*1.25f,.25);tween.TweenProperty(root,"modulate:a",0f,.4).SetDelay(.15);tween.Finished+=root.QueueFree;
    }
}

[HarmonyPatch]
public static class HeirOneShotAudio
{
    public static IEnumerable<MethodBase> TargetMethods()=>typeof(NAudioManager).GetMethods().Where(m=>m.Name=="PlayOneShot");
    public static bool Prefix(string path,float volume)=>!HeirPresentation.PlaySound(path,volume);
}
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapMarker),"Initialize")]
public static class HeirMapMarker
{
    public static void Postfix(MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapMarker __instance,Player player) {if(player.Character is HeirCharacter)__instance.Texture=Runtime.ClassIcon(HeirPresentation.ClassFor(player));}
}
[HarmonyPatch(typeof(CharacterModel),"get_IconOutlineTexture")]
public static class HeirAuxOutline
{
    public static void Postfix(CharacterModel __instance,ref Texture2D __result) {if(__instance is HeirCharacter heir)__result=Runtime.Texture("heir/presentation/"+heir.ClassId+"-outline.svg");}
}
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Commands.VfxCmd),"PlayOnCreatureCenter")]
public static class HeirArchitectEffects
{
    public static bool Prefix(MegaCrit.Sts2.Core.Entities.Creatures.Creature target,string path)
    {
        if(!path.StartsWith("spirelegacy/architect/",StringComparison.Ordinal))return true;
        var cls=path.Split('/')[2];var node=target.GetCreatureNode();
        if(node!=null&&!target.IsDead&&target.GetVfxContainer() is {} container)HeirPresentation.ArchitectBurst(container,node.VfxSpawnPosition,cls);
        return false;
    }
}
[HarmonyPatch(typeof(RelicModel),"get_PackedIconPath")]
public static class HeirCookieIcon
{
    public static bool Prefix(RelicModel __instance,ref string __result) {if(__instance is YummyCookie&&__instance.IsMutable&&__instance.Owner?.Character is HeirCharacter){__result=HeirPresentation.Cookie(HeirPresentation.ClassFor(__instance.Owner)).PackedIconPath!;return false;}return true;}
}
[HarmonyPatch(typeof(RelicModel),"get_PackedIconOutlinePath")]
public static class HeirCookieOutline
{
    public static bool Prefix(RelicModel __instance,ref string __result) {if(__instance is YummyCookie&&__instance.IsMutable&&__instance.Owner?.Character is HeirCharacter){__result=HeirPresentation.Cookie(HeirPresentation.ClassFor(__instance.Owner)).PackedIconOutlinePath!;return false;}return true;}
}
[HarmonyPatch(typeof(RelicModel),"get_BigIconPath")]
public static class HeirCookieLarge
{
    public static bool Prefix(RelicModel __instance,ref string __result) {if(__instance is YummyCookie&&__instance.IsMutable&&__instance.Owner?.Character is HeirCharacter){__result=HeirPresentation.Cookie(HeirPresentation.ClassFor(__instance.Owner)).BigIconPath!;return false;}return true;}
}

public partial class HeirRoomMotion:Node2D
{
    private HeirSprite sprite=null!;private bool rest;private double clock;
    public static void Attach(Node2D parent,HeirSprite sprite,bool rest) {
        var motion=new HeirRoomMotion{Name="HeirRoomMotion",sprite=sprite,rest=rest};parent.AddChild(motion);
        // External mod classes are not imported Godot scripts. Use a native signal callback.
        var tree=(SceneTree)Engine.GetMainLoop();tree.ProcessFrame+=motion.Update;motion.TreeExiting+=()=>tree.ProcessFrame-=motion.Update;
    }
    private void Update()
    {
        if(!GodotObject.IsInstanceValid(sprite)||!sprite.IsVisibleInTree()||((SceneTree)Engine.GetMainLoop()).Paused||sprite.Animation=="dead")return;
        clock+=GetProcessDeltaTime();
        // Feet remain anchored. Small translations add breath to resting and shopping poses.
        sprite.Position=new Vector2(0,(float)Math.Sin(clock*(rest?1.6:2.1))*(rest?2.2f:1.2f));
    }
}
[HarmonyPatch]
public static class HeirRestResponse
{
    public static IEnumerable<MethodBase> TargetMethods()=>typeof(MegaCrit.Sts2.Core.Entities.RestSite.RestSiteOption).Assembly.GetTypes()
        .Where(t=>!t.IsAbstract&&typeof(MegaCrit.Sts2.Core.Entities.RestSite.RestSiteOption).IsAssignableFrom(t)).Select(t=>AccessTools.DeclaredMethod(t,"OnSelect")).OfType<MethodBase>();
    public static void Prefix(MegaCrit.Sts2.Core.Entities.RestSite.RestSiteOption __instance) {
        var player=(Player)AccessTools.Property(typeof(MegaCrit.Sts2.Core.Entities.RestSite.RestSiteOption),"Owner").GetValue(__instance)!;
        if(player.Character is not HeirCharacter)return;
        var tree=(SceneTree)Engine.GetMainLoop();
        foreach(var room in NativeTestBridge.Descendants(tree.Root).OfType<MegaCrit.Sts2.Core.Nodes.RestSite.NRestSiteCharacter>().Where(r=>r.Player==player))
            room.GetNodeOrNull<HeirSprite>("SpireLegacyRoomHeir")?.PlayPose(__instance.OptionId=="SMITH"?"attack":"skill");
    }
}
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Entities.Merchant.MerchantEntry),"InvokePurchaseCompleted")]
public static class HeirShopPurchase
{
    public static void Postfix(MegaCrit.Sts2.Core.Entities.Merchant.MerchantEntry __instance) {
        if(AccessTools.Field(typeof(MegaCrit.Sts2.Core.Entities.Merchant.MerchantEntry),"_player").GetValue(__instance) is not Player player||player.Character is not HeirCharacter)return;
        var tree=(SceneTree)Engine.GetMainLoop();
        foreach(var sprite in NativeTestBridge.Descendants(tree.Root).OfType<HeirSprite>().Where(s=>s.Name=="SpireLegacyRoomHeir"&&s.IsVisibleInTree()&&s.HasMeta("spirelegacy_owner")&&s.GetMeta("spirelegacy_owner").AsUInt64()==player.NetId))sprite.PlayPose("skill");
    }
}
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantCharacter),"PlayAnimation")]
public static class HeirShopResponse
{
    public static void Postfix(MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantCharacter __instance,string anim) {__instance.GetNodeOrNull<HeirSprite>("SpireLegacyRoomHeir")?.PlayPose(anim.Contains("relaxed")?"idle":"skill");}
}
