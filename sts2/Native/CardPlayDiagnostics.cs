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
    public static string Version {get {using var manifest=JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(typeof(Runtime).Assembly.Location,".json")));return manifest.RootElement.GetProperty("version").GetString()!;}}
    public static void Postfix(PlayCardAction __instance, ref Task __result)
    {
        if(__instance.Player.Character is HeirCharacter)__result=Observe(__result,__instance.CardModelId.ToString(),__instance.Player);
    }
    public static async Task Observe(Task play, string card,MegaCrit.Sts2.Core.Entities.Players.Player? owner=null)
    {
        try {await play;}
        catch(Exception error) when(error is not OperationCanceledException)
        {
            LastFailure=DiagnosticText.Describe(error,()=>{
                var mods=string.Join(", ",MegaCrit.Sts2.Core.Modding.ModManager.Mods.Where(m=>m.state==MegaCrit.Sts2.Core.Modding.ModLoadState.Loaded).Select(m=>$"{m.manifest?.id} {m.manifest?.version}"));
                var heir=PlayerBloodlines.Heir(owner);
                return $"Spire Legacy {Version}; StS2 {ReleaseInfoManager.Instance.ReleaseInfo?.Version}; {System.Runtime.InteropServices.RuntimeInformation.OSDescription}\nLoaded mods: {mods}\nHeir: {heir.classId}; traits: {string.Join(", ",heir.traits)}\nCard: {card}";
            });
            try{GD.PushError(LastFailure);}catch{ /* Preserve the original card failure. */ }
            throw;
        }
    }
}
