using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace SpireLegacy;

// Native card widgets survive scene transitions in NodePool. AssetCache disposes
// their unseen/locked portrait materials, but Reload only checks for null.
// Let native Reload reacquire invalid materials through its own asset cache.
[HarmonyPatch(typeof(NCard),"Reload")]
public static class CardMaterialRecovery
{
    private static readonly FieldInfo[] Materials=new[]{"_portraitBlurMaterial","_canvasGroupMaskBlurMaterial","_canvasGroupBlurMaterial","_canvasGroupMaskMaterial"}
        .Select(name=>AccessTools.Field(typeof(NCard),name) is {IsStatic:false} field&&typeof(Material).IsAssignableFrom(field.FieldType)?field:throw new MissingFieldException(typeof(NCard).FullName,name)).ToArray();
    public static void Prefix(NCard __instance)
    {
        foreach(var field in Materials)
            if(field.GetValue(__instance) is Material material&&!GodotObject.IsInstanceValid(material))field.SetValue(__instance,null);
    }
}
