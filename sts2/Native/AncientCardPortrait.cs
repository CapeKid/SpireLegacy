using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace SpireLegacy;

[HarmonyPatch(typeof(NCard), "Reload")]
internal static class AncientCardPortrait
{
    private static readonly StringName OriginalStretch = "spire_legacy_original_ancient_stretch";

    private static void Postfix(NCard __instance)
    {
        if (__instance.GetNodeOrNull<TextureRect>("%AncientPortrait") is not { } portrait) return;
        if (__instance.Model is LegacyCard { Rarity: CardRarity.Ancient })
        {
            if (!portrait.HasMeta(OriginalStretch))
                portrait.SetMeta(OriginalStretch, (int)portrait.StretchMode);

            // Fit the landscape illustration at the top of the Ancient frame, above the rules.
            portrait.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        }
        else if (portrait.HasMeta(OriginalStretch))
        {
            // NCard instances are pooled and can next show a native Ancient card.
            portrait.StretchMode = (TextureRect.StretchModeEnum)portrait.GetMeta(OriginalStretch).AsInt32();
            portrait.RemoveMeta(OriginalStretch);
        }
    }
}
