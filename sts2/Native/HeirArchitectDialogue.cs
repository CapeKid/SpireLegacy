using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Events;
using System.Runtime.CompilerServices;

namespace SpireLegacy;

// Track both canonical and mutable ending sets without changing any other Ancient.
[HarmonyPatch(typeof(TheArchitect), "get_DialogueSet")]
public static class HeirArchitectDialogue
{
    private static readonly ConditionalWeakTable<AncientDialogueSet, object> EndingSets = new();
    public static void Postfix(AncientDialogueSet __result) => EndingSets.GetValue(__result, _ => new());
    public static bool IsEnding(AncientDialogueSet set) => EndingSets.TryGetValue(set, out _);

    public static AncientDialogue Create(string classId, int previousWins)
    {
        var first = previousWins == 0;
        var content = ArchitectConversation.ForClass(classId, first);
        var dialogue = new AncientDialogue(content.Select(_ => "").ToArray())
        {
            VisitIndex = first ? 0 : 1, IsRepeating = !first,
            EndAttackers = ArchitectAttackers.Both
        };
        var translations = new Dictionary<string, string>();
        for (var i = 0; i < content.Count; i++)
        {
            var key = $"SPIRELEGACY.ARCHITECT.{classId}.{(first ? "first" : "return")}.{i}";
            translations[key] = content[i].Text;
            var line = dialogue.Lines[i];
            line.Speaker = content[i].Architect ? AncientDialogueSpeaker.Ancient : AncientDialogueSpeaker.Character;
            line.LineText = new LocString("ancients", key);
            if (i < content.Count - 1)
            {
                translations[key + ".next"] = content[i].Architect ? "Respond" : "Continue";
                line.NextButtonText = new LocString("ancients", key + ".next");
            }
        }
        // Merge only namespaced original text into the current locale table.
        LocManager.Instance.GetTable("ancients").MergeWith(translations);
        return dialogue;
    }
}
