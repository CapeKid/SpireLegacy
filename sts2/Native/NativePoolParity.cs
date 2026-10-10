using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace SpireLegacy;

public static class NativePoolParity
{
    // Read the built-in list, excluding cards injected by other mods. The native method
    // avoids recursive mod-pool enumeration while ModelDb initializes custom pools.
    private static readonly Lazy<bool> expanded = new(()=> {
        var pool=ModelDb.CardPool<IroncladCardPool>();
        var cards=(CardModel[])AccessTools.Method(pool.GetType(),"GenerateAllCards").Invoke(pool,[])!;
        return cards.Count(c=>c.Rarity==CardRarity.Uncommon)>36 || cards.Count(c=>c.Rarity==CardRarity.Rare)>26;
    });
    public static bool Expanded => expanded.Value;
    public static IEnumerable<Design.Row> Rows(string classId) => Design.Pool(classId,Expanded);
}
