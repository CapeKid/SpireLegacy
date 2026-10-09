using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace SpireLegacy;

// Keep historical cards discoverable without enlarging the 75-card reward pools.
[HarmonyPatch(typeof(ModelDb),"get_AllCards")]
public static class AllLegacyCards
{
    public static void Postfix(ref IEnumerable<CardModel> __result) => __result = __result.Concat(Design.Rows("cards").Select(r=>NativeCards.Get(r.Text("id")))).Distinct();
}

[HarmonyPatch(typeof(ModelDb),"get_AllCardPools")]
public static class AllHeirPools
{
    public static void Postfix(ref IEnumerable<CardPoolModel> __result) => __result = __result.Concat(new CardPoolModel[] {ModelDb.CardPool<KnightCardPool>(),ModelDb.CardPool<MageCardPool>(),ModelDb.CardPool<RangerCardPool>()}).Distinct();
}
