using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;

namespace SpireLegacy;

// Heirs retain their original rare cards rather than inventing an Ancient rarity
// for the migrated card set. The native relic still upgrades and previews it.
[HarmonyPatch(typeof(DustyTome), nameof(DustyTome.SetupForPlayer))]
public static class HeirDustyTome
{
    public static bool Prefix(DustyTome __instance, Player player)
    {
        if (player.Character is not HeirCharacter) return true;
        var cards = player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == CardRarity.Rare);
        __instance.AncientCard = player.PlayerRng.Rewards.NextItem(cards)!.Id;
        return false;
    }
}

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
