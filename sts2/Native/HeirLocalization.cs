using System.Text.Json;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Models;

namespace SpireLegacy;

internal static class HeirLocalization
{
    private static readonly Lazy<Dictionary<string, string>> Chinese = new(() =>
    {
        using var stream = typeof(HeirLocalization).Assembly.GetManifestResourceStream("SpireLegacy.Localization.zhs.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    });

    internal static string Text(string english) =>
        LocManager.Instance?.Language == "zhs" ? Chinese.Value.GetValueOrDefault(english, english) : english;

    internal static string Format(FormattableString text) => string.Format(Text(text.Format), text.GetArguments());

    internal static string Localized(this Design.Row row, string field) => Text(row.Text(field));

    internal static List<(string, string)> Entries(params (string Key, string English)[] entries) =>
        entries.Select(entry => (entry.Key, Text(entry.English))).ToList();

    internal static void ApplyFont(Control control)
    {
        if (LocManager.Instance?.Language == "zhs" && FontManager.GetSubstituteFont("zhs", FontType.Regular) is {} font)
            control.AddThemeFontOverride("font", font);
        else
            control.RemoveThemeFontOverride("font");
    }

    private static readonly (Type Model, string Table)[] Tables =
    [
        (typeof(LegacyCard), "cards"), (typeof(HeirCharacter), "characters"),
        (typeof(LegacyPower), "powers"), (typeof(FamilyDropRelic), "relics"),
        (typeof(BloodlineRelic), "relics"), (typeof(FamilyPotion), "potions")
    ];

    private static readonly Type[] ModelTypes = typeof(HeirLocalization).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && typeof(ILocalizationProvider).IsAssignableFrom(type)).ToArray();
    private static bool modelsReady;

    private static void RegisterModels()
    {
        foreach (var (model, table) in Tables)
        {
            var translations = new Dictionary<string, string>();
            foreach (var type in ModelTypes.Where(model.IsAssignableFrom))
            {
                var id = ModelDb.GetId(type);
                var content = (ILocalizationProvider)ModelDb.GetById<AbstractModel>(id);
                foreach (var (key, value) in content.Localization ?? [])
                    translations.Add($"{id.Entry}.{key}", value);
            }
            LocManager.Instance.GetTable(table).MergeWith(translations);
        }
    }

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
    private static class InitialModelLocalization
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            RegisterModels();
            modelsReady = true;
        }
    }

    [HarmonyPatch(typeof(LocManager), "TriggerLocaleChange")]
    private static class ReloadModelLocalization
    {
        // The first locale load can occur before the native model database exists.
        private static void Prefix()
        {
            if (modelsReady) RegisterModels();
        }
    }
}
