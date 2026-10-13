using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace SpireLegacy;

// Keep all inherited hooks and the saved run carrier when Orobas refines the starter.
public sealed class RefinedBloodlineRelic : BloodlineRelic
{
    public override List<(string,string)> Localization=>[
        ("title","Refined Bloodline"),
        ("description","Draw 1 additional card each turn. Your class, inherited traits and purchased manor upgrades still shape this climb."),
        ("flavor","The family's potential, awakened.")];
    public override decimal ModifyHandDraw(Player player,decimal count)=>base.ModifyHandDraw(player,count)+(player==Owner?1:0);
    public override RelicModel? GetUpgradeReplacement()=>null;
}

[HarmonyPatch(typeof(RelicCmd),nameof(RelicCmd.Replace))]
public static class PreserveRefinedBloodline
{
    public static void Prefix(RelicModel original,RelicModel replace)
    {
        if(original is not BloodlineRelic bloodline||replace is not RefinedBloodlineRelic refined)return;
        refined.FamilyRunSnapshot=bloodline.FamilyRunSnapshot;
        refined.SelectedHeir=bloodline.SelectedHeir;
        refined.StartingRelicsGranted=bloodline.StartingRelicsGranted;
    }
}

[HarmonyPatch(typeof(TouchOfOrobas),nameof(TouchOfOrobas.SetupForPlayer))]
public static class ExcludeAlreadyRefinedBloodline
{
    public static bool Prefix(Player player,ref bool __result)
    {
        if(player.Relics.FirstOrDefault(r=>r.Rarity==MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Starter) is not RefinedBloodlineRelic)return true;
        __result=false;return false;
    }
}
