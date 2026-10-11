using SpireLegacy;
using System.Text.Json;

var count = 0;
void Check(bool value, string message) { count++; if (!value) throw new Exception(message); }
var dropRelics=Design.Rows("heir_relics");
Check(dropRelics.Count==7&&dropRelics.Select(r=>r.Text("id")).Distinct().Count()==7,"Seven distinct exclusive relic drops");
Check(dropRelics.All(r=>new[]{"Common","Uncommon","Rare"}.Contains(r.Text("rarity"))),"No starter in ordinary drop definitions");
foreach(var relic in dropRelics)
{
    Check(!relic.Text("description").Contains('{')&&relic.Number("extra")<=1,"Complete bounded relic text/resources "+relic.Text("id"));
    Check(Design.Get("relic_art",relic.Text("id")).Text("license")=="MIT","Original relic artwork "+relic.Text("id"));
}
Check(Design.Rows("relic_art").Select(r=>r.Text("sha256")).Distinct().Count()==7,"Distinct relic silhouettes");
var icons = Design.Rows("icon_art");
var familyPotions=Design.Rows("heir_potions");
Check(familyPotions.Count==3&&familyPotions.Select(r=>r.Text("id")).Distinct().Count()==3,"Three exclusive potion identities");
foreach(var potion in familyPotions)
{
    Check(new[]{"Common","Uncommon","Rare"}.Contains(potion.Text("rarity"))&&new[]{"Self","AnyEnemy"}.Contains(potion.Text("target")),"Valid native potion rarity/target "+potion.Text("id"));
    Check(!potion.Text("description").Contains('{')&&potion.Number("amount")>0&&potion.Number("extra")>0,"Complete potion text/resources "+potion.Text("id"));
    Check(Design.Get("potion_art",potion.Text("id")).Text("license")=="MIT","Original potion artwork "+potion.Text("id"));
}
Check(Design.Rows("potion_art").Select(r=>r.Text("sha256")).Distinct().Count()==3,"Distinct potion silhouettes");
Check(icons.Count == Design.Rows("traits").Count + 3, "Every saved trait ID and class has an original icon");
Check(icons.Select(r => r.Text("sha256")).Distinct().Count() == icons.Count, "Original trait/class icons are distinct");
foreach (var trait in Design.Rows("traits")) Check(Design.Get("icon_art", "trait_" + trait.Text("id")).Text("license") == "MIT", "Original trait icon " + trait.Text("id"));
foreach (var cls in new[] { "knight", "mage", "ranger" }) Check(Design.Get("icon_art", "class_" + cls).Text("path").StartsWith("heir/icons/"), "Packaged original class icon " + cls);
var endingScripts = new HashSet<string>();
foreach (var cls in new[] { "knight", "mage", "ranger" })
foreach (var firstEnding in new[] { false, true })
{
    var lines = ArchitectConversation.ForClass(cls, firstEnding);
    Check(lines.Count == 4 && lines.Select(l => l.Architect).SequenceEqual(new[] { true, false, true, false }), cls + " ending alternates Architect and Heir");
    Check(lines.All(l => !string.IsNullOrWhiteSpace(l.Text) && !l.Text.Contains('{')), cls + " ending has complete literal dialogue");
    Check(endingScripts.Add(string.Join("\n", lines.Select(l => l.Text))), cls + " unique first/return ending");
}
Check(Design.Rows("cards").Count == 360, "Preserve every historical card");
foreach (var cls in new[] { "knight", "mage", "ranger" })
{
    var pool = Design.Pool(cls).ToArray();
    Check(pool.Length == 85 && pool.Select(r => r.Text("id")).Distinct().Count() == 85, cls + " regular pool");
    Check(Design.Rows("card_pools").Count(r => r.Text("classId") == cls && r.Text("scope") == "shared") == 34, cls + " shared core");
    var expanded=Design.Pool(cls,true).ToArray();
    Check(expanded.Length==88 && expanded.Select(r=>r.Text("id")).Distinct().Count()==88,cls+" beta pool");
    foreach(var item in new[]{("COMMON",20,20),("UNCOMMON",36,38),("RARE",26,27)})
        Check(pool.Count(r=>r.Text("rarity")==item.Item1)==item.Item2 && expanded.Count(r=>r.Text("rarity")==item.Item1)==item.Item3,cls+" native "+item.Item1+" reward parity");
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
foreach (var cls in new[] { "knight", "mage", "ranger" })
foreach (var starterId in Design.Get("decks", cls).List("cards").Distinct())
foreach (var upgraded in new[] { false, true })
{
    var ordinary = new CardDesign(starterId, upgraded);
    var pacifist = new CardDesign(starterId, upgraded, ["cantattack"]);
    if (ordinary.Row.Text("type") == "ATTACK")
    {
        Check(pacifist.Mercy && pacifist.Damage == 0 && pacifist.Block == 0 && pacifist.Cost == 1, starterId + " Mercy cannot supply free defense or damage");
        Check(pacifist.MercyPoison == (upgraded ? 3 : 2) && pacifist.Rules().Contains($"Apply {pacifist.MercyPoison} Poison") && !pacifist.Rules().Contains("Block"), starterId + " Mercy upgrade improves only Poison");
    }
    else Check(!pacifist.Mercy && pacifist.Block == ordinary.Block && pacifist.Rules() == ordinary.Rules(), starterId + " Pacifist preserves separate defensive starters");
}
Inheritance.CurrentHeir = () => new() { classId = "ranger", traits = ["cantattack"] };
Check(Inheritance.Has("pacifist") && Inheritance.Amount("pacifist") == 0, "Pacifist still forbids Attacks without granting opening Poison");
Inheritance.CurrentHeir = () => new();
Check(Design.Get("traits", "cantattack").Number("goldBonus") == 150, "Pacifist retains its requested crown bonus");
var pacifistPayout = new FamilyProfile { selected = new() { classId = "ranger", traits = ["cantattack"] } };
var pacifistRun = pacifistPayout.Begin(1);
Check(pacifistPayout.Settle(pacifistRun.id, 50, 200, true) == 256, "Pacifist retains its existing capped double crown payout");
foreach (var upgraded in new[] { false, true })
{
    var ward = new CardDesign("mage_perfect_ward", upgraded);
    Check(ward.Cost == 2 && ward.Magic == (upgraded ? 2 : 1) && ward.Row.Flag("exhaust"), "Ward has a bounded, single-use prevention budget");
    var timing = new CardDesign("ranger_perfect_timing", upgraded);
    Check(timing.Cost == (upgraded ? 2 : 3) && timing.Magic == 1 && timing.Amount("hpLoss") == 3, "Timing upgrade lowers setup cost without doubling recurring energy");
    Check(timing.Rules().Contains("gain 1 Energy") && timing.Rules().Contains("Lose 3 HP"), "Timing text matches its recurring energy and HP price");
    var tripwire = new CardDesign("ranger_tripwire", upgraded);
    Check(tripwire.Block == (upgraded ? 5 : 3) && tripwire.Amount("weak") == 1, "Tripwire upgrades defense without extending Weak");
}
foreach (var cardId in Design.Rows("card_pools").Select(r => r.Text("cardId")).Distinct())
{
    var normal = new CardDesign(cardId); var upgraded = new CardDesign(cardId, true);
    Check(normal.Cost != upgraded.Cost || normal.Damage != upgraded.Damage || normal.Block != upgraded.Block || normal.Rules() != upgraded.Rules(), cardId + " has an effective upgrade");
}
foreach (var upgraded in new[] { false, true })
{
    var angel = new CardDesign("knight_guardian_angel", upgraded);
    Check(angel.Cost == (upgraded ? 1 : 2) && angel.Magic == 1, "Guardian Angel upgrades efficiency instead of prevention capacity");
    var mantle = new CardDesign("mage_astral_mantle", upgraded);
    Check(mantle.Cost == (upgraded ? 1 : 2) && mantle.Magic == 1, "Astral Mantle upgrades setup without doubling permanent Block");
}
Check(Design.Rows("cards").Where(c => c.Number("hits") > 1).All(c => c.Number("upgradeDamage") <= 1), "Multi-hit upgrade budget");
foreach (var traitId in new[] { "nocolor", "oldyellowtint", "colortrails", "fmffan" }) Check(Design.Get("traits", traitId).Number("goldBonus") == 0, "No free crown bonus: " + traitId);
var oldManor = new FamilyProfile { crowns = 5, manor = new() { ["smith"] = 3, ["armory"] = 3, ["garden"] = 4 } };
Check(oldManor.RefundRetiredLevels() && oldManor.crowns == 965, "Refund retired levels at their original purchase prices");
Check(oldManor.Bonus("strength") == 2 && oldManor.Bonus("dexterity") == 2 && oldManor.Bonus("heal") == 2, "Retuned manor stats");
Check(!oldManor.RefundRetiredLevels() && oldManor.crowns == 965, "Refund migration is idempotent");
var maxTraitTreasury = new FamilyProfile { selected = new() { classId = "knight", traits = ["megahealth"] }, manor = new() { ["vault"] = 4 } };
var treasuryRun = maxTraitTreasury.Begin(1);
Check(maxTraitTreasury.Settle(treasuryRun.id, 20, 100, false) == 105, "Treasury multiplies trait-capped earnings, rounding once");
var ordinaryTreasury = new FamilyProfile { selected = new() { classId = "knight", traits = [] }, manor = new() { ["vault"] = 4 } };
var ordinaryTreasuryRun = ordinaryTreasury.Begin(1);
Check(ordinaryTreasury.Settle(ordinaryTreasuryRun.id, 20, 100, false) == 52, "Treasury remains useful without trait bonuses");
// Previously inert traits now expose explicit bounded gameplay effects.
foreach (var (adaptedId,effect,amount,hp) in new[] {
    ("enemyknockedfar","first_attack_weak",1,0), ("fart","opening_weak",1,0), ("fmffan","first_skill_block",2,0),
    ("mushroomgrow","first_hurt_block",3,-6), ("projectilesnowalls","first_attack_shred",3,-6),
    ("mapreveal","opening_draw_energy",1,0) })
{
    var trait = Design.Get("traits",adaptedId);
    Check(trait.Text("effect") == effect && trait.Number("amount") == amount, adaptedId + " explicit gameplay effect");
    Check(trait.Number("hp") == hp && !trait.Text("summary").Contains("no equivalent"), adaptedId + " documented drawback");
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
var ordinaryPayout = new FamilyProfile { selected = new FamilyProfile.Heir { classId="knight",traits=[] } };
var ordinaryRun = ordinaryPayout.Begin(1);
var ordinaryCrowns = ordinaryPayout.Settle(ordinaryRun.id,20,100,false);
var heroPayout = new FamilyProfile { selected = new FamilyProfile.Heir { classId="knight",traits=["megahealth"] } };
var heroRun = heroPayout.Begin(1);
Check(heroPayout.Settle(heroRun.id,20,100,false) == ordinaryCrowns * 2, "Hero Complex doubles actual settlement crowns");
Check(heroPayout.Settle(heroRun.id,20,100,false) == 0, "Hero Complex cannot settle twice");
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
var migrationPath = Path.Combine(directory, "legacy-manor.json");
var legacyProfile = new FamilyProfile { family = "Legacy Test", crowns = 5, manor = new() { ["smith"] = 3, ["armory"] = 3, ["garden"] = 4 } };
var legacyRun = legacyProfile.Begin(1);
legacyRun.bonuses["strength"] = 3; legacyRun.bonuses["heal"] = 4;
legacyProfile.Save(migrationPath);
var migrated = FamilyProfile.Load(migrationPath);
Check(migrated.crowns == 965 && migrated.family == "Legacy Test" && migrated.Level("garden") == 2, "Loading migrates and persists legacy manor refunds");
Check(migrated.active!.id == legacyRun.id && migrated.active.bonuses["strength"] == 3 && migrated.active.bonuses["heal"] == 4, "Migration preserves an unfinished run's bonus snapshot");
Check(FamilyProfile.Load(migrationPath).crowns == 965, "Reload cannot award refunds again");
Inheritance.CurrentHeir = () => new() { classId = "mage", traits = ["vampire", "superhealer"] };
Check(Inheritance.Healing(Design.Get("manor", "garden").Number("maxLevel") + Inheritance.Stat("heal")) == 10, "Full Garden/Vampirism/Super Healer healing is reduced from 16 to 10");
Inheritance.CurrentHeir = () => new();
Console.WriteLine(JsonSerializer.Serialize(new { result = "passed", assertions = count, cards = 360, inheritedTraits = traitIds.Length, familyFixture = directory }));
