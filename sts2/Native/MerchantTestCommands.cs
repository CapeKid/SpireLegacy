using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// The opt-in test harness uses the runtime shop API on each supported branch.
internal static class MerchantTestCommands
{
    private static readonly MethodInfo? GetSynchronizer = typeof(RunManager).GetProperty("MerchantSynchronizer")?.GetMethod;
    private static readonly MethodInfo GetInventory = ApiContract.Require(
        GetSynchronizer?.ReturnType ?? typeof(MerchantRoom), "GetLocalInventory", typeof(MerchantInventory), Type.EmptyTypes);
    private static readonly MethodInfo? PurchaseLocal = GetSynchronizer is { } getter
        ? ApiContract.Require(getter.ReturnType, "PurchaseLocalEntry", typeof(Task<bool>), [typeof(MerchantEntry)])
        : null;
    private static readonly MethodInfo PurchaseEntry = ApiContract.Require(typeof(MerchantEntry),
        GetSynchronizer is null ? "OnTryPurchaseWrapper" : "OnTryPurchaseUnsynchronized",
        typeof(Task<bool>), [typeof(MerchantInventory), typeof(bool)]);

    private static object Synchronizer() => ApiContract.Invoke(GetSynchronizer!, RunManager.Instance, [])
        ?? throw new InvalidOperationException("The run has no merchant synchronizer.");

    internal static MerchantInventory LocalInventory(MerchantRoom room) =>
        (MerchantInventory)ApiContract.Invoke(GetInventory, GetSynchronizer is null ? room : Synchronizer(), [])!;

    internal static Task<bool> Purchase(MerchantEntry entry, MerchantInventory inventory) =>
        PurchaseLocal is null
            ? (Task<bool>)ApiContract.Invoke(PurchaseEntry, entry, [inventory, false])!
            : (Task<bool>)ApiContract.Invoke(PurchaseLocal, Synchronizer(), [entry])!;

    internal static Task<bool> PurchaseDetached(MerchantEntry entry) =>
        (Task<bool>)ApiContract.Invoke(PurchaseEntry, entry, [null, false])!;
}
