using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// BaseLib captures Character.CardPool when creating the cached library filter.
// An Heir's pool can change in the menu; a saved/co-op Heir instead keeps its
// frozen owner pool. Refresh just this filter before native builds its query.
[HarmonyPatch(typeof(NCardLibrary),nameof(NCardLibrary.OnSubmenuOpened))]
public static class HeirCompendiumPool
{
    public static void Prefix(Dictionary<CharacterModel,NCardPoolFilter> ____cardPoolFilters,
        Dictionary<NCardPoolFilter,Func<CardModel,bool>> ____poolFilters,IRunState? ____runState)
    {
        var character=____runState==null?null:LocalContext.GetMe(____runState)?.Character as HeirCharacter;
        var pool=character?.CardPool??ModelDb.Character<HeirCharacter>().CardPool;
        foreach(var entry in ____cardPoolFilters.Where(p=>p.Key is HeirCharacter))
            ____poolFilters[entry.Value]=card=>pool.AllCardIds.Contains(card.Id);
    }
}
