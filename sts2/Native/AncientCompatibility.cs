using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;

namespace SpireLegacy;

// Original Heir dialogue only; native characters and reward logic are untouched.
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
        if(HeirAncientConversations.TryCreate(__instance,Runtime.Heir.classId,charVisits,out var conversation))__result=[conversation];
    }
}
