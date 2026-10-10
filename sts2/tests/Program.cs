using SpireLegacy;
using System.Text.Json;

var count = 0;
void Check(bool value, string message) { count++; if (!value) throw new Exception(message); }
Check(Design.Rows("cards").Count == 360, "Preserve every historical card");
foreach (var cls in new[] { "knight", "mage", "ranger" })
{
    var pool = Design.Pool(cls).ToArray();
    Check(pool.Length == 75 && pool.Select(r => r.Text("id")).Distinct().Count() == 75, cls + " pool");
    Check(Design.Rows("card_pools").Count(r => r.Text("classId") == cls && r.Text("scope") == "shared") == 30, cls + " shared core");
    Check(Design.Get("decks", cls).List("cards").Length == 10, cls + " starters");
}
foreach (var row in Design.Rows("cards"))
foreach (var upgrade in new[] { false, true })
{
    var card = new CardDesign(row.Text("id"), upgrade);
    Check(card.Cost >= 0 && card.Damage >= 0 && card.Block >= 0, card.Id + " valid stats");
    Check(!string.IsNullOrWhiteSpace(card.Rules()), card.Id + " full rules");
}
var traitIds = Design.Rows("traits").Where(r => r.Flag("enabled")).Select(r => r.Text("id")).ToArray();
// Cosmetic/platform-only traits must not quietly grant relic-like combat effects.
foreach (var traitId in new[] { "easybreakables", "fart", "fmffan", "mushroomgrow", "projectilesnowalls", "mapreveal" })
{
    var trait = Design.Get("traits", traitId);
    Check(trait.Text("effect") == "none" && trait.Number("amount") == 0, traitId + " has no invented combat effect");
    Check(new[] { "hp", "strength", "dexterity", "draw", "heal" }.All(key => trait.Number(key) == 0), traitId + " has no hidden stats");
}
foreach (var gene in Design.Rows("starter_genes").Where(g => g.Text("id") != "cantattack"))
    Check(new[] { "damage", "block", "draw", "heal", "poison" }.All(key => gene.Number(key) == 0), "No overlapping starter bonuses: " + gene.Text("id"));
Check(Design.Get("traits", "revealallchests").Text("effect") == "treasure_gold" && Design.Get("traits", "bonuschestgold").Text("effect") == "treasure_gold", "Treasure traits reward treasure rooms");
Check(Design.Get("traits", "bounceterrain").Number("hp") == -12, "Clownanthropy retains a health drawback");
Check(Design.Get("traits", "nomanacap").Text("effect") == "overcharge", "Limitless energy carries combat risk");
foreach (var upgraded in new[] { false, true })
{
    var barrier = new CardDesign("flame", upgraded);
    Check(barrier.Row.Text("type") == "SKILL" && barrier.Damage == 0 && barrier.Block == (upgraded ? 7 : 5), "Flame Barrier is defensive");
    Check(barrier.Row.Text("power") == "flame_barrier" && barrier.Magic == (upgraded ? 3 : 2), "Flame Barrier retaliation");
    Check(barrier.Rules().Contains("this turn") && !barrier.Rules().Contains("Deal {Damage}"), "Flame Barrier text matches its effects");
}
foreach (var trait in traitIds)
foreach (var cls in new[] { "knight", "mage", "ranger" })
foreach (var card in Design.Get("decks", cls).List("cards").Distinct())
{
    var design = new CardDesign(card, true, [trait]);
    Check(design.Damage >= 0 && design.Block >= 0 && design.Rules().Length > 0, "Inherited starter " + trait + ": " + card);
}
var profile = new FamilyProfile();
for (var seed = 0; seed < 100; seed++)
{
    profile.GenerateOffers(seed);
    Check(profile.offers.Select(h => h.classId).Distinct().Count() == 3, "Class options");
    foreach (var heir in profile.offers) Check(FamilyProfile.Compatible(Design.Get("traits", heir.traits[0]), Design.Get("traits", heir.traits[1])), "Compatible traits");
}
profile.selected = new() { name = "Test", classId = "knight", traits = [] };
var run = profile.Begin(4);
Check(profile.Settle(run.id, 50, 200, true) == 128, "Slower victory crowns");
Check(profile.Settle(run.id, 50, 200, true) == 0 && profile.generation == 1, "Idempotent settlement");
var directory = Path.Combine(Path.GetTempPath(), "SpireLegacy2Checks-" + Guid.NewGuid());
var path = Path.Combine(directory, "family.json");
profile.family = "Cape House"; profile.crowns = 1000; profile.Save(path);
var first = Design.Rows("manor").First(r => r.Text("requires") == "none");
var id = first.Text("id"); var price = profile.Cost(first);
Check(profile.PreviewPurchases([id])!.crowns == 1000 - price && profile.crowns == 1000 && profile.Level(id) == 0, "Staging cannot mutate");
Check(profile.CommitPurchases([id], path) && profile.crowns == 1000 - price && profile.Level(id) == 1, "Confirmed atomic purchase");
Check(FamilyProfile.Load(path).family == "Cape House" && FamilyProfile.Load(path).Level(id) == 1, "Persist family and manor");
var snapshot = profile.crowns;
Directory.CreateDirectory(Path.Combine(directory, "blocked"));
try { profile.CommitPurchases([id], Path.Combine(directory, "blocked")); throw new Exception("Expected write failure"); } catch (IOException) { }
Check(profile.crowns == snapshot && profile.Level(id) == 1, "Failed save cannot spend crowns");
File.WriteAllText(path, "broken");
Check(FamilyProfile.Load(path).family == "Cape House", "Backup recovery");
Console.WriteLine(JsonSerializer.Serialize(new { result = "passed", assertions = count, cards = 360, inheritedTraits = traitIds.Length, familyFixture = directory }));
