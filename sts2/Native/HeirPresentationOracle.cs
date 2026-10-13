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
    public static async Task<object> NeowEnergy(RunState run)
    {
        var player=run.Players.Single();var cls=HeirPresentation.ClassFor(player);
        var tree=(SceneTree)Engine.GetMainLoop();
        var layout=NativeTestBridge.Descendants(tree.Root).OfType<MegaCrit.Sts2.Core.Nodes.Events.NAncientEventLayout>().Single(l=>l.IsVisibleInTree());
        var neow=(MegaCrit.Sts2.Core.Models.Events.Neow)AccessTools.Field(layout.GetType(),"_ancientEvent").GetValue(layout)!;
        var native=Runtime.Texture("heir/presentation/"+cls+"-energy-text.svg");
        var reference=MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetTexture2D("res://images/packed/sprite_fonts/ironclad_energy_icon.png");
        if(native.GetSize()!=reference.GetSize())throw new InvalidDataException("Inline energy size differs from native: "+native.GetSize()+" / "+reference.GetSize());
        var pool=(FamilyCardPool)player.Character.CardPool;
        var relicPool=ModelDb.RelicPool<HeirRelicPool>();
        if(pool.TextEnergyIconPath==pool.BigEnergyIconPath||relicPool.TextEnergyIconPath==relicPool.BigEnergyIconPath)throw new InvalidDataException("Text still uses large energy artwork.");
        if(Runtime.Texture("heir/presentation/"+cls+"-energy-icon.svg").GetSize()!=new Vector2(128,128))throw new InvalidDataException("Large energy artwork changed.");
        // Use actual Neow option models and native widgets, forcing an energy-bearing choice.
        layout.ClearOptions();
        layout.AddOptions(neow.AllPossibleOptions.Where(o=>o.Relic is BoomingConch or PhialHolster or SmallCapsule));
        layout.OnSetupComplete();
        for(var i=0;i<3;i++){AncientDialogueOracle.State(true);await Task.Delay(250);}
        await Task.Delay(800);
        var energy=layout.OptionButtons.Single(b=>b.Option.Relic is BoomingConch);
        var label=energy.GetNode<MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel>("%Text");
        var text=label.Text;
        if(!text.Contains("-energy-text.svg"))throw new InvalidDataException("Neow did not format the small inline icon.");
        var height=label.GetContentHeight();
        // Recreate the old unsized image in the same widget; this must increase its text height.
        label.Text=text.Replace("-energy-text.svg","-energy-icon.svg");
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var oldHeight=label.GetContentHeight();
        label.Text=text;
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        if(oldHeight<=height||height>label.Size.Y+1)throw new InvalidDataException("Neow inline layout: fixed="+height+", old="+oldHeight+", box="+label.Size);
        // Native button roots include overlapping decorative glow/padding. Measure text.
        var boxes=layout.OptionButtons.Select(b=>b.GetNode<MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel>("%Text")).Select(l=>new Rect2(l.GlobalPosition,new Vector2(l.Size.X,l.GetContentHeight()))).ToArray();
        for(var i=1;i<boxes.Length;i++)if(boxes[i-1].Intersects(boxes[i]))throw new InvalidDataException("Neow option boxes overlap.");
        return new{cls,inlineSize=native.GetSize().ToString(),nativeSize=reference.GetSize().ToString(),height,oldHeight,text,options=boxes.Select(b=>b.ToString()).ToArray(),focusable=AncientDialogueOracle.State(false)};
    }
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
        if(trail is not MegaCrit.Sts2.Core.Nodes.Vfx.NCardTrailVfx||trail.GetNodeOrNull<Node>("Trails/FamilyRibbon")==null)throw new InvalidDataException("Heir trail cannot instantiate.");trail.Free();
        var mat=MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetMaterial(model.CharacterSelectTransitionPath);
        if(mat is not ShaderMaterial material||!material.Shader.Code.Contains("threshold"))throw new InvalidDataException("Heir transition did not load.");
        var roles=new[]{"attack","cast","die","select","transition"};
        foreach(var role in roles)SfxCmd.Play(HeirPresentation.Sound(role));
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var sounds=HeirPresentation.Sounds.TakeLast(roles.Length).ToArray();if(!sounds.SequenceEqual(roles.Select(HeirPresentation.Sound)))throw new InvalidDataException("Heir audio routing incomplete.");
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
