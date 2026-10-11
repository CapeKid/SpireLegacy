using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Inspect actual native widgets and resource loads in isolated owned-game tests.
public static class HeirPresentationOracle
{
    public static async Task<object> Inspect(RunState run)
    {
        var player=run.Players.Single();var model=player.Character;var tree=(SceneTree)Engine.GetMainLoop();var cls=HeirPresentation.ClassId;
        var counter=NativeTestBridge.Descendants(tree.Root).OfType<NEnergyCounter>().Single();
        // BaseLib anchors the native star counter inside energy. Inspect only energy layers.
        var layers=NativeTestBridge.Descendants(counter.GetNode<Control>("%Layers")).OfType<TextureRect>().Where(t=>t.Name.ToString().StartsWith("Layer")).ToArray();
        if(layers.Length!=5||layers.Any(l=>!l.Texture.ResourcePath.Contains("SpireLegacy/heir/presentation/"+cls+"-energy-")))throw new InvalidDataException("Native energy layers: "+string.Join(";",NativeTestBridge.Descendants(counter).OfType<TextureRect>().Select(l=>l.Name+"="+l.Texture?.ResourcePath)));
        var marker=NativeTestBridge.Descendants(tree.Root).OfType<MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapMarker>().First();
        if(marker.Texture!=Runtime.ClassIcon(cls))throw new InvalidDataException("Wrong native map marker.");
        var cookie=(YummyCookie)ModelDb.Relic<YummyCookie>().ToMutable();cookie.Owner=player;
        var icon=cookie.Icon;var large=cookie.BigIcon;var outline=cookie.IconOutline;
        if(icon!=Runtime.Texture("heir/presentation/"+cls+"-cookie.svg")||large!=icon||outline!=Runtime.Texture("heir/presentation/"+cls+"-cookie-outline.svg"))throw new InvalidDataException("Cookie did not resolve class art.");
        var native=ModelDb.Character<Ironclad>();if(!native.CharacterSelectSfx.Contains("ironclad")||!native.TrailPath.Contains("ironclad"))throw new InvalidDataException("Changed native identity.");
        var trail=MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetScene(model.TrailPath).Instantiate();
        if(trail is not MegaCrit.Sts2.Core.Nodes.Vfx.NCardTrailVfx||trail.GetNodeOrNull<Node>("Sprites/FamilyRibbon")==null)throw new InvalidDataException("Heir trail cannot instantiate.");trail.Free();
        var mat=MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetMaterial(model.CharacterSelectTransitionPath);
        if(mat is not ShaderMaterial material||!material.Shader.Code.Contains("threshold"))throw new InvalidDataException("Heir transition did not load.");
        var before=HeirPresentation.Sounds.Count;
        foreach(var role in new[]{"attack","cast","die","select","transition"})SfxCmd.Play(HeirPresentation.Sound(role));
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var sounds=HeirPresentation.Sounds.Skip(before).ToArray();if(sounds.Length!=5)throw new InvalidDataException("Heir audio routing incomplete.");
        var enemy=player.Creature.CombatState!.GetOpponentsOf(player.Creature).First();
        foreach(var path in model.GetArchitectAttackVfx())VfxCmd.PlayOnCreatureCenter(enemy,path);
        if(NativeTestBridge.Descendants(tree.Root).Count(n=>n.HasMeta("spirelegacy_architect")&&n.GetMeta("spirelegacy_architect").AsString()==cls)!=5)throw new InvalidDataException("Architect effects did not appear in the native scene.");
        return new{cls,energyVisible=counter.IsVisibleInTree(),layers=layers.Select(l=>l.Texture.ResourcePath).ToArray(),markerOriginal=true,cookieOriginal=true,trail=model.TrailPath,transition=model.CharacterSelectTransitionPath,sounds,
            architectEffects=model.GetArchitectAttackVfx().ToArray(),nativeUnchanged=true};
    }
    public static async Task<object> Rest(RunState run)
    {
        var player=run.Players.Single();var room=MegaCrit.Sts2.Core.Nodes.Rooms.NRestSiteRoom.Instance!;
        var sprite=NativeTestBridge.Descendants(room).OfType<HeirSprite>().Single();var old=sprite.Position;
        await Task.Delay(500);var breathing=old!=sprite.Position;
        await new HealRestSiteOption(player).OnSelect();
        if(!breathing||!sprite.Events.Contains("skill"))throw new InvalidDataException("Rest motion="+breathing+" events="+string.Join(",",sprite.Events));
        return new{cls=sprite.ClassId,breathing,events=sprite.Events.ToArray(),visible=sprite.IsVisibleInTree(),scale=sprite.GlobalScale.ToString()};
    }
}
