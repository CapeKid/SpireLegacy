using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;

namespace SpireLegacy;

// Use original class-specific Heir endings; retain the existing fallback for other Ancients.
[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
public static class HeirAncientDialogue
{
    public static void Postfix(AncientDialogueSet __instance, ModelId characterId, int charVisits,
        int totalVisits, bool allowAnyCharacterDialogues, ref IEnumerable<AncientDialogue> __result)
    {
        if (characterId != ModelDb.Character<HeirCharacter>().Id) return;
        if (HeirArchitectDialogue.IsEnding(__instance))
        {
            __result = [HeirArchitectDialogue.Create(Runtime.Heir.classId, charVisits)];
            return;
        }
        if (__result.Any()) return;
        __result = __instance.GetValidDialogues(ModelDb.Character<Ironclad>().Id, charVisits, totalVisits, allowAnyCharacterDialogues);
    }
}
