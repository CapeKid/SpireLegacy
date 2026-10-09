using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using System.Text.Json;

namespace SpireLegacy;

// Observe the real paid play action, including failures before OnPlay is reached.
// Preserve the original exception and never retry a partially executed card.
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
public static class CardPlayDiagnostics
{
    public static string? LastFailure { get; private set; }
    public static string Version {get {using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(Runtime.ModDirectory,"SpireLegacy.json")));return manifest.RootElement.GetProperty("version").GetString()!;}}
    public static void Postfix(PlayCardAction __instance, ref Task __result)
    {
        if(__instance.Player.Character is HeirCharacter)__result=Observe(__result,__instance.CardModelId.ToString());
    }
    public static async Task Observe(Task play, string card)
    {
        try {await play;}
        catch(Exception error) when(error is not OperationCanceledException)
        {
            var mods=string.Join(", ",MegaCrit.Sts2.Core.Modding.ModManager.Mods.Where(m=>m.state==MegaCrit.Sts2.Core.Modding.ModLoadState.Loaded).Select(m=>$"{m.manifest?.id} {m.manifest?.version}"));
            LastFailure=$"Spire Legacy {Version}; StS2 {ReleaseInfoManager.Instance.ReleaseInfo?.Version}; {System.Runtime.InteropServices.RuntimeInformation.OSDescription}\nLoaded mods: {mods}\nHeir: {Runtime.Heir.classId}; traits: {string.Join(", ",Runtime.Heir.traits)}\nCard: {card}\n{error}";
            GD.PushError(LastFailure);
            throw;
        }
    }
}
