namespace SpireLegacy;

public static class Inheritance
{
    public static IEnumerable<Design.Row> Traits => RuntimeTraits();
    // A profile argument keeps inheritance testable without loading the game engine.
    public static IEnumerable<Design.Row> Rows(FamilyProfile.Heir heir) => heir.traits.Select(id => Design.Get("traits", id));
    private static IEnumerable<Design.Row> RuntimeTraits() => Rows(CurrentHeir());
    public static Func<FamilyProfile.Heir> CurrentHeir { private get; set; } = () => new();
    public static bool Has(string effect) => Traits.Any(t => t.Text("effect") == effect);
    public static int Amount(string effect) => Traits.Where(t => t.Text("effect") == effect).Sum(t => t.Number("amount"));
    public static int Stat(string field) => Traits.Sum(t => t.Number(field));
    public static int MaxHp(int hp) => Has("fragile") ? 1 : Math.Max(20, hp + Stat("hp"));
    public static decimal Healing(decimal amount)
    {
        if (Has("no_heal")) return 0;
        if (Has("super_heal")) amount *= Amount("super_heal");
        if (Has("vegan")) amount = Math.Floor(amount / 2);
        return amount;
    }
    public static IReadOnlyList<string> Deck(string cls, int seed)
    {
        var result = Design.Get("decks", cls).List("cards").ToArray();
        if (!Has("kit")) return result;
        var random = new Random(seed);
        var classes = Design.Rows("classes").ToArray();
        for (var i = 0; i < result.Length; i++)
        {
            var source = Design.Get("decks", classes[random.Next(classes.Length)].Text("id")).List("cards");
            result[i] = source[random.Next(source.Length)];
        }
        return result;
    }
}
