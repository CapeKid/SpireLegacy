using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;
namespace SpireLegacy;

// Rest-site co-op slots flip their Spine children rather than the room root.
[HarmonyPatch(typeof(NRestSiteCharacter),nameof(NRestSiteCharacter.FlipX))]
public static class HeirRoomFacing
{
    public static void Postfix(NRestSiteCharacter __instance)
    {
        if(__instance.GetNodeOrNull<HeirSprite>("SpireLegacyRoomHeir") is not {} sprite)return;
        var body=__instance.GetChildren().OfType<Node2D>().FirstOrDefault(n=>n.GetClass()=="SpineSprite");
        if(body!=null)sprite.SetFacingLeft(body.Scale.X<0);
    }
}
