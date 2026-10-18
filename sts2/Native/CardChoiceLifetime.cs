using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Native hand teardown completes its pending selection. Do not submit that
// reply to a new run's synchronizer or resume a disposed run's card action.
public static class CardChoiceLifetime
{
    private sealed record Binding(Player Player,IRunState Run);
    private static readonly AsyncLocal<Binding?> current=new();
    private static readonly System.Reflection.PropertyInfo state=AccessTools.Property(typeof(RunManager),"State");

    public static async Task<T> Run<T>(Player player,Func<Task<T>> choose)
    {
        var previous=current.Value;
        var binding=new Binding(player,player.RunState);
        current.Value=binding;
        try {ValidateSubmission(player);return await choose();}
        finally {current.Value=previous;}
    }

    internal static void ValidateSubmission(Player player)
    {
        if(current.Value is not {} binding||!ReferenceEquals(binding.Player,player))return;
        var manager=RunManager.Instance;
        if(manager.IsCleaningUp||!ReferenceEquals(state.GetValue(manager),binding.Run))
            throw new OperationCanceledException("The card choice's original run context is no longer active.");
    }
}

[HarmonyPatch(typeof(PlayerChoiceSynchronizer),nameof(PlayerChoiceSynchronizer.SyncLocalChoice))]
public static class CancelObsoleteCardChoice
{
    public static void Prefix(Player player)=>CardChoiceLifetime.ValidateSubmission(player);
}
