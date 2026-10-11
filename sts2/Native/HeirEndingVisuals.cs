using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Outside combat the native game-over screen creates fresh placeholder visuals.
// Those have no NCreature, so the combat _Ready patch never sees them.
[HarmonyPatch(typeof(NGameOverScreen),"MoveCreaturesToDifferentLayerAndDisableUi")]
public static class HeirEndingVisuals
{
    private static FamilyProfile.Heir EndedHeir(Player player)
    {
        var saved=player.Relics.OfType<BloodlineRelic>().FirstOrDefault()?.SelectedHeir;
        if(!string.IsNullOrEmpty(saved))
        {
            try {if(JsonSerializer.Deserialize<FamilyProfile.Heir>(saved,FamilyProfile.JsonOptions) is {} heir&&Design.Rows("classes").Any(r=>r.Text("id")==heir.classId))return heir;}
            catch(JsonException) { }
        }
        return Runtime.Heir;
    }
    public static void Postfix(NGameOverScreen __instance)
    {
        var run=(RunState)AccessTools.Field(typeof(NGameOverScreen),"_runState").GetValue(__instance)!;
        var container=(Control)AccessTools.Field(typeof(NGameOverScreen),"_creatureContainer").GetValue(__instance)!;
        var visuals=container.GetChildren().OfType<NCreatureVisuals>().ToArray();
        var merchants=container.GetChildren().OfType<MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantCharacter>().ToArray();
        for(var index=0;index<run.Players.Count;index++)
        {
            var player=run.Players[index];if(player.Character is not HeirCharacter)continue;
            var dead=player.Creature.IsDead;
            if(NCombatRoom.Instance?.GetCreatureNode(player.Creature) is {} combat)
            {
                combat.Visuals.GetCurrentBody().Visible=false;
                var existing=combat.Visuals.GetNodeOrNull<HeirSprite>("SpireLegacyHeir");
                if(existing!=null&&dead)existing.PlayPose("dead");
                continue;
            }
            if(index<merchants.Length)
            {
                var merchant=merchants[index];
                foreach(var child in merchant.GetChildren().OfType<Node2D>().Where(n=>n.GetClass()=="SpineSprite"))child.Visible=false;
                if(merchant.GetNodeOrNull<HeirSprite>("SpireLegacyRoomHeir") is {} roomSprite&&dead)roomSprite.PlayPose("dead");
                continue;
            }
            if(index>=visuals.Length)continue;
            var visual=visuals[index];visual.GetCurrentBody().Visible=false;
            if(visual.GetNodeOrNull<HeirSprite>("SpireLegacyEndingHeir")!=null)continue;
            var heir=EndedHeir(player);
            var factor=Inheritance.Rows(heir).Aggregate(1f,(value,row)=>value*row.Json.GetProperty("scale").GetSingle());
            // Rest-site game-over clones inherit the small native Spine root scale.
            // Keep native placement/direction while restoring our normal screen size.
            var size=factor/Math.Max(.01f,Math.Abs(visual.Scale.Y));
            var sprite=HeirSprite.Create(heir.classId,size);sprite.Name="SpireLegacyEndingHeir";
            visual.AddChild(sprite);TraitAppearance.Attach(visual,sprite,size,heir);
            sprite.PlayPose(dead?"dead":"victory");
        }
    }
    public static object Diagnostic()
    {
        var tree=(SceneTree)Engine.GetMainLoop();
        return NativeTestBridge.Descendants(tree.Root).OfType<NGameOverScreen>().Select(screen=>new{
            visible=screen.IsVisibleInTree(),
            sprites=NativeTestBridge.Descendants(screen).OfType<HeirSprite>().Select(s=>new{classId=s.ClassId,pose=s.Animation.ToString(),frame=s.Frame,frames=s.SpriteFrames.GetFrameCount(s.Animation),deathFrames=s.DeathFrames.ToArray(),visible=s.IsVisibleInTree(),scale=s.GlobalScale.ToString(),position=s.GlobalPosition.ToString(),
                placeholderVisible=s.GetParent() is NCreatureVisuals visual?visual.GetCurrentBody().IsVisibleInTree():s.GetParent().GetChildren().OfType<Node2D>().Any(n=>n.GetClass()=="SpineSprite"&&n.IsVisibleInTree())}).ToArray()
        }).ToArray();
    }
}
