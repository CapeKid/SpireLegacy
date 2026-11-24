using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace SpireLegacy;

internal static class StuckRunRecovery
{
    internal enum Status { Clearable, LiveRun, CoOp, CurrentSave, OtherProfile, Unknown }

    // Use the game's own save store so this checks both local and Steam-backed saves.
    private static readonly System.Reflection.FieldInfo? SaveStore = AccessTools.Field(typeof(SaveManager), "_saveStore");
    private static readonly string[] RunFiles = ["current_run.save", "current_run_mp.save"];

    internal static Status Check(FamilyProfile.FamilyRun run)
    {
        if (NRun.Instance is not null || RunManager.Instance.IsInProgress) return Status.LiveRun;
        var saves = SaveManager.Instance;
        try
        {
            if (!saves.IsProfileInitialized || saves.CurrentRunSaveTask is { IsCompleted: false })
                return Status.Unknown;
            if (saves.HasRunSave || saves.HasMultiplayerRunSave) return Status.CurrentSave;
            if (SaveStore?.GetValue(saves) is not ISaveStore store) return Status.Unknown;

            // The family file is shared across the game's three profile slots.
            // Check modded paths directly, even when a mod mismatch hides Continue.
            for (var profile = 1; profile <= 3; profile++)
            foreach (var file in RunFiles)
            {
                var path = RunSaveManager.GetRunSavePath(profile, file, true);
                if (store.FileExists(path) || store.FileExists(path + ".backup"))
                    return profile == saves.CurrentProfileId ? Status.CurrentSave : Status.OtherProfile;
            }

            // A client's game may have no local copy of the host's resumable run.
            if (run.coop || CoopBloodlines.Lobby?.NetService.Type is NetGameType.Host or NetGameType.Client)
                return Status.CoOp;
            return Status.Clearable;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return Status.Unknown;
        }
    }

    internal static string? Guidance(Status status) => status switch
    {
        Status.LiveRun => "Finish or abandon the current climb before starting another.",
        Status.CoOp => "Ask the co-op host to resume or abandon that climb. Leave the lobby before clearing an old solo climb.",
        Status.CurrentSave => "A saved climb still exists. Use Continue or Abandon on the main menu. If those options are hidden, restore the mod setup used for that run.",
        Status.OtherProfile => "Another game profile has a saved climb. Switch profiles to check for Continue or Abandon.",
        Status.Unknown => "The game could not check for saved climbs. Return to the main menu and try again.",
        _ => null
    };
}
