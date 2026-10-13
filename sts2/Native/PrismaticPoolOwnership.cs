using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// UnlockState enumerates the canonical Heir, whose class follows the local menu.
// Prismatic Gem must expand the recipient's frozen class, not the local peer's.
[HarmonyPatch(typeof(PrismaticGem),nameof(PrismaticGem.ModifyCardRewardCreationOptions))]
public static class PrismaticPoolOwnership
{
    public static void Prefix(CardCreationOptions options,out CardPoolModel[] __state)=>__state=options.CardPools.ToArray();
    public static void Postfix(PrismaticGem __instance,Player player,CardPoolModel[] __state,ref CardCreationOptions __result)
    {
        if(__instance.Owner!=player||__result.CustomCardPool!=null||!__result.Flags.HasFlag(CardCreationFlags.IsCardReward)||__result.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications)||!__result.CardPools.Any(p=>p is FamilyCardPool))return;
        var families=player.Character is HeirCharacter
            ?__state.OfType<FamilyCardPool>().Cast<CardPoolModel>().Append(player.Character.CardPool)
            :new CardPoolModel[]{ModelDb.CardPool<KnightCardPool>(),ModelDb.CardPool<MageCardPool>(),ModelDb.CardPool<RangerCardPool>()};
        // Materialize before WithCardPools clears its mutable input list.
        var pools=__result.CardPools.Concat(__state).Where(p=>p is not FamilyCardPool).Concat(families).Distinct().ToArray();
        __result=__result.WithCardPools(pools,__result.CardPoolFilter);
    }
}
