using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

[HarmonyPatch]
internal static class BlitzTraits
{
    // Regular builds predating Blitz lack both the enum member and the merchant API.
    private static readonly GameMode? Blitz = Enum.TryParse<GameMode>("Blitz", out var mode) ? mode : null;
    private static readonly MethodInfo? MerchantFactory = typeof(MerchantInventory).GetMethod("CreateForBlitzMerchant", [typeof(Player)]);
    private static readonly PropertyInfo? ChoiceCount = typeof(MerchantInventory).GetProperty("BlitzChoicesRemaining");
    internal static bool HasMerchantApi => Blitz.HasValue && MerchantFactory != null && ChoiceCount is { CanRead: true, CanWrite: true };
    internal static bool IsBlitz(GameMode mode) => mode == Blitz;

    private static MethodBase TargetMethod() => MerchantFactory!;

    private static void Postfix(Player __0, MerchantInventory __result)
    {
        // Blitz has exactly one Merchant in Act 1. Apply the bonus to that shop's starting allowance.
        if (__0.RunState.CurrentActIndex != 0 || !PlayerBloodlines.Traits(__0).Has("shop")) return;
        var choices = (int)ChoiceCount!.GetValue(__result)!;
        ChoiceCount.SetValue(__result, choices + 1);
    }

    internal static string Summary(Design.Row trait, bool blitz) => !blitz ? trait.Localized("summary") : trait.Text("id") switch
    {
        "bonuschestgold" => HeirLocalization.Text("Lose 8 maximum HP. Find a random potion in each treasure room."),
        "lowerstoreprice" => HeirLocalization.Text("Take one extra ware at the first Merchant."),
        "cheeronkills" => HeirLocalization.Text("Start combat Vulnerable for 1 turn. Gain 2 Vigor whenever an enemy dies."),
        _ => trait.Localized("summary"),
    };
}
