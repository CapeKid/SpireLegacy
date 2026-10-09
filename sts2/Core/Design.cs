using System.Reflection;
using System.Text.Json;

namespace SpireLegacy;

/// <summary>The original authoring sheets are the shared source of truth for the port.</summary>
public static class Design
{
    private static readonly Dictionary<string, Row[]> Tables = new();
    public static IReadOnlyList<Row> Rows(string table)
    {
        if (Tables.TryGetValue(table, out var rows)) return rows;
        var assembly = typeof(Design).Assembly;
        var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith($".{table}.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var doc = JsonDocument.Parse(stream);
        return Tables[table] = doc.RootElement.EnumerateArray().Select(e => new Row(e.Clone())).ToArray();
    }
    public static Row Get(string table, string id) => Rows(table).Single(r => r.Text("id") == id);
    public static IEnumerable<Row> Pool(string classId) => Rows("card_pools").Where(r => r.Text("classId") == classId).Select(r => Get("cards", r.Text("cardId")));
    public sealed record Row(JsonElement Json)
    {
        public string Text(string key) => Json.GetProperty(key).GetString()!;
        public int Number(string key) => Json.GetProperty(key).GetInt32();
        public bool Flag(string key) => Json.GetProperty(key).GetBoolean();
        public string[] List(string key) => Json.GetProperty(key).EnumerateArray().Select(e => e.GetString()!).ToArray();
    }
}
