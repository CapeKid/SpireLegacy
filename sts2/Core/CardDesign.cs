namespace SpireLegacy;

public sealed class CardDesign(string id, bool upgraded = false, IEnumerable<string>? inherited = null)
{
    public Design.Row Row { get; } = Design.Get("cards", id);
    public string Id => id;
    public bool Upgraded => upgraded;
    public int Damage => Mercy ? 0 : Math.Max(0, Gene("damage") + Row.Number("damage") + (upgraded ? Row.Number("upgradeDamage") : 0));
    public int Block => Mercy ? Gene("block") + (upgraded ? 3 : 0) : Math.Max(0, Gene("block") + Row.Number("block") + (upgraded ? Row.Number("upgradeBlock") : 0));
    public int Cost => Row.Number("cost") - (upgraded && Row.Flag("upgradeCost") ? 1 : 0);
    public int SpecialAmount => Row.Number("specialAmount") + (upgraded ? Row.Number("upgradeSpecial") : 0);
    private IEnumerable<Design.Row> Genes => Row.Text("rarity") == "BASIC" ? Design.Rows("starter_genes").Where(r => inherited?.Contains(r.Text("id")) == true) : [];
    public bool Mercy => Row.Text("type") == "ATTACK" && Genes.Any(r => r.Flag("mercy"));
    public int Gene(string key) => Genes.Where(r => Row.Text("type") == "ATTACK" ? key is "damage" or "heal" or "poison" || key == "block" && r.Flag("mercy") : key is "block" or "draw").Sum(r => r.Number(key));
    public int Amount(string key)
    {
        if (key == "discard") return Row.Number(key) + (upgraded ? Row.Number("upgradeDiscard") : 0);
        return Row.Number(key) + (key == "draw" ? Gene(key) : 0) + (upgraded && key != "hpLoss" && Row.Number(key) > 0 ? Row.Number("upgradeMagic") : 0);
    }
    public int Magic => Row.Number("magic") + (upgraded ? Row.Number("upgradeMagic") : 0);
    public string Rules()
    {
        var lines = new List<string>();
        if (!Mercy && Damage > 0) lines.Add($"Deal {{Damage}} damage{(Row.Flag("aoe") ? " to ALL enemies" : "")}{(Row.Number("hits") > 1 ? $" {Row.Number("hits")} times" : "")}.");
        if (Block > 0 || Mercy) lines.Add("Gain {Block} Block.");
        foreach (var key in new[] { "hpLoss", "heal", "poison", "vigor", "thorns", "plated", "nextEnergy", "nextDraw", "nextBlock", "draw", "energy", "strength", "dexterity", "weak", "vulnerable", "discard" })
        {
            var amount = Amount(key); if (amount <= 0) continue;
            lines.Add(key switch {
                "hpLoss" => $"Lose {amount} HP.", "heal" => $"Heal {amount} HP.",
                "draw" => $"Draw {amount} card(s).", "discard" => $"Discard {amount} card(s).",
                "nextEnergy" => $"Next turn, gain {amount} Energy.", "nextDraw" => $"Next turn, draw {amount} card(s).", "nextBlock" => $"Next turn, gain {amount} Block.",
                "weak" or "vulnerable" or "poison" => $"Apply {amount} {key}{(Row.Flag("aoe") ? " to ALL enemies" : "")}.",
                _ => $"Gain {amount} {key}." });
        }
        if (Gene("heal") > 0) lines.Add($"Heal {Gene("heal")} HP.");
        if (Mercy) lines.Add($"Apply {Gene("poison") + (upgraded ? 1 : 0)} Poison. This inherited starter is a Skill.");
        if (Row.Text("power") == "reservoir") lines.Add($"Each Arcane Charge adds {Magic} extra attack damage.");
        else if (Row.Text("power") == "quiver") lines.Add($"Hunter Rhythm draws {Magic} additional card(s).");
        else if (Row.Text("power") != "none") lines.Add(Design.Get("power_effects", Row.Text("power")).Text("summary").Replace("!M!", Magic.ToString()));
        if (Row.Text("special") != "none") lines.Add(Design.Get("special_effects", Row.Text("special")).Text("summary").Replace("{n}", SpecialAmount.ToString()));
        return string.Join("\n", lines);
    }
}
