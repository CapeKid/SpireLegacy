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
            __result = [HeirArchitectDialogue.Create(PlayerBloodlines.Heir(PlayerBloodlines.EligibilityOwner.Value??PlayerBloodlines.LocalPlayer).classId, charVisits)];
            return;
        }
        if(HeirAncientConversations.TryCreate(__instance,PlayerBloodlines.Heir(PlayerBloodlines.EligibilityOwner.Value??PlayerBloodlines.LocalPlayer).classId,charVisits,out var conversation))__result=[conversation];
    }
}

[HarmonyPatch]
public static class OwnerAncientDialogue
{
    public static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>[
        AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NEventRoom),"SetupLayout"),
        AccessTools.Method(typeof(MegaCrit.Sts2.Core.Models.Events.TheArchitect),"LoadDialogue")];
    public static void Prefix(object __instance,out Player? __state)
    {
        __state=PlayerBloodlines.EligibilityOwner.Value;
        var model=__instance as EventModel??AccessTools.Field(__instance.GetType(),"_event")?.GetValue(__instance) as EventModel;
        if(model?.Owner is {} owner)PlayerBloodlines.EligibilityOwner.Value=owner;
    }
    public static void Finalizer(Player? __state)=>PlayerBloodlines.EligibilityOwner.Value=__state;
}
