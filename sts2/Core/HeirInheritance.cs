namespace SpireLegacy;

public sealed class HeirInheritance(FamilyProfile.Heir heir)
{
    public IEnumerable<Design.Row> Traits => Inheritance.Rows(heir);
    public bool Has(string effect) => Traits.Any(t => t.Text("effect") == effect);
    public int Amount(string effect) => Traits.Where(t => t.Text("effect") == effect).Sum(t => t.Number("amount"));
    public int Stat(string field) => Traits.Sum(t => t.Number(field));
    public int MaxHp(int hp) => Has("fragile") ? 1 : Math.Max(20, hp + Stat("hp"));
    public decimal Healing(decimal amount)
    {
        if (Has("no_heal")) return 0;
        if (Has("super_heal")) amount *= Amount("super_heal");
        if (Has("vegan")) amount = Math.Floor(amount / 2);
        return amount;
    }
    public IReadOnlyList<string> Deck(string cls, int seed)
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
