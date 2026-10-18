using SpireLegacy;
using System.Text.Json;

if(LifecycleChecks.Child(args)||PersistenceChecks.Child(args))return;
var count = 0;
void Check(bool value, string message) { count++; if (!value) throw new Exception(message); }
CoopChecks.Run(Check);
TraitChecks.Run(Check);
CrownChecks.Run(Check);
var coopFamilies=new[]{new FamilyProfile{family="First family",generation=3},new FamilyProfile{family="Second family",generation=7}};
var coopHeirs=new[]{new FamilyProfile.Heir{name="First",classId="knight",traits=["easybreakables"]},new FamilyProfile.Heir{name="Second",classId="mage",traits=[]}};
var coopRuns=coopFamilies.Select((p,i)=>FamilySnapshot.Freeze(p,coopHeirs[i],(ulong)(i+1),true)).ToArray();
for(var i=0;i<coopRuns.Length;i++) {
    var coopRun=coopRuns[i];var family=coopFamilies[i];family.active=coopRun;
    var restored=FamilySnapshot.ReadNetwork(FamilySnapshot.Serialize(coopRun),coopRun.ownerNetId);
    Check(restored.heir.classId==coopHeirs[i].classId&&restored.familyId==family.familyId&&!restored.quick,"Co-op heir identity and no quick-start");
    Check(FamilySnapshot.CanSettle(family,coopRun,coopRun.ownerNetId),"Owner may settle its active co-op climb");
    Check(!FamilySnapshot.CanSettle(family,coopRun,coopRun.ownerNetId+1),"Another player cannot settle the family");
    Check(!FamilySnapshot.CanSettle(coopFamilies[1-i],coopRun,coopRun.ownerNetId),"Other family cannot settle the coopRun");
    family.Settle(coopRun.id,3,0,false);
    Check(!FamilySnapshot.CanSettle(family,coopRun,coopRun.ownerNetId),"Settlement cannot repeat");
}
Check(coopRuns[0].familyId!=coopRuns[1].familyId&&coopRuns[0].id!=coopRuns[1].id,"Independent family and run IDs");
coopFamilies[0].selected=new(){classId="knight",traits=[]};var newerCoopRun=coopFamilies[0].Begin(3);coopFamilies[0].Settle(newerCoopRun.id,2,0,false);
var replayCrowns=coopFamilies[0].crowns;coopFamilies[0].active=coopRuns[0];
Check(!FamilySnapshot.CanSettle(coopFamilies[0],coopRuns[0],1),"Older settled snapshot cannot replay after another climb");
Check(coopFamilies[0].Settle(coopRuns[0].id,50,999,true)==0&&coopFamilies[0].crowns==replayCrowns,"Recorded run IDs prevent crown replay after an intervening settlement");
coopFamilies[0].active=null;
coopHeirs[0].traits.Clear();coopFamilies[0].family="Changed later";
Check(coopRuns[0].heir.traits.SequenceEqual(new[]{"easybreakables"})&&coopRuns[0].family=="First family","Ready snapshot is independent of later UI edits");
void RejectCoop(FamilyProfile.FamilyRun run,ulong sender,string reason) {
    var rejected=false;try{FamilySnapshot.ReadNetwork(FamilySnapshot.Serialize(run),sender);}catch{rejected=true;}
    Check(rejected,reason);
}
RejectCoop(coopRuns[0],99,"Authenticated sender must match snapshot owner");
var invalidCoop=FamilySnapshot.Read(FamilySnapshot.Serialize(coopRuns[0]));invalidCoop.bonuses["strength"]=999;
RejectCoop(invalidCoop,1,"Manor bonuses must stay within design limits");
invalidCoop=FamilySnapshot.Read(FamilySnapshot.Serialize(coopRuns[0]));invalidCoop.chronicleRelics.Add("invented_relic");
RejectCoop(invalidCoop,1,"Only original Chronicle unlocks synchronize");
invalidCoop=FamilySnapshot.Read(FamilySnapshot.Serialize(coopRuns[0]));invalidCoop.heir.traits.Add("easybreakables");
RejectCoop(invalidCoop,1,"Duplicate traits cannot synchronize");
Check(new HeirInheritance(coopRuns[0].heir).Has("first_attack_block_loss")&&!new HeirInheritance(coopRuns[1].heir).Has("first_attack_block_loss"),"Inherited mechanics are independent per owner");
var ancientScripts=new HashSet<string>();
Check(Design.Rows("ancient_dialogue").Count==48,"Eight Ancients, three classes, two visit conversations");
foreach(var npc in AncientConversation.Supported)foreach(var cls in new[]{"knight","mage","ranger"})foreach(var visits in new[]{0,1,4,10,100}) {
    var lines=AncientConversation.For(npc,cls,visits);
    Check(lines.Count==4&&lines.Select(l=>l.Ancient).SequenceEqual(new[]{true,false,true,false}),"Ancient/Heir speakers "+npc+cls);
    Check(lines.All(l=>l.Text.Length is >0 and <=160&&!l.Text.Contains('{')),"Readable literal dialogue "+npc+cls);
    if(visits<2)Check(ancientScripts.Add(string.Join("\n",lines.Select(l=>l.Text))),"Distinct first/return class dialogue "+npc+cls);
    else Check(lines.SequenceEqual(AncientConversation.For(npc,cls,1)),"Safe shared visit count "+npc+cls);
}
Check(AncientConversation.For("FUTURE_ANCIENT","unknown",100).All(l=>!string.IsNullOrWhiteSpace(l.Text)),"Original fallback for a future Ancient/class");
var ancients=Design.Rows("ancient_cards");
Check(ancients.Count==6&&ancients.Select(r=>r.Text("cardId")).Distinct().Count()==6,"Six distinct Ancient rewards");
foreach(var cls in new[]{"knight","mage","ranger"}) {
    var ancientPool=ancients.Where(r=>r.Text("classId")==cls).Select(r=>Design.Get("cards",r.Text("cardId"))).ToArray();
    Check(ancientPool.Length==2&&ancientPool.All(r=>r.Text("rarity")=="ANCIENT"&&r.Text("classId")==cls),cls+" dedicated Ancient rewards");
    foreach(var card in ancientPool) {
        Check(!Design.Pool(cls,true).Any(r=>r.Text("id")==card.Text("id")),"Ancient excluded from ordinary parity "+card.Text("id"));
        var normal=new CardDesign(card.Text("id"));var up=new CardDesign(card.Text("id"),true);
        Check(normal.Rules()!=up.Rules()||normal.Cost!=up.Cost||normal.Block!=up.Block||normal.Damage!=up.Damage,"Ancient useful upgrade "+card.Text("id"));
    }
}
var dropRelics=Design.Rows("heir_relics");
var stories=Design.Rows("family_chronicle");
Check(stories.Count==9&&stories.Select(r=>r.Text("id")).Distinct().Count()==9,"Nine original Chronicle entries");
Check(stories.All(r=>r.Text("story").Length is >200 and <1200&&!r.Text("story").Contains("Ironclad")),"Original readable story entries");
var freshStory=new FamilyProfile();FamilyChronicle.Reconcile(freshStory);
Check(freshStory.chronicle.unlocked.SetEquals(["founding"])&&!FamilyChronicle.Relics(freshStory).Any(),"Fresh family baseline story, three relics locked");
Check(!FamilyChronicle.SelectMotto(freshStory,"Unbroken Oath"),"Locked motto cannot be selected");
foreach(var storyClass in new[]{"knight","mage","ranger"}) {
    freshStory.selected=new(){classId=storyClass,traits=[]};var lessonRun=freshStory.Begin(81);
    Check(lessonRun.chronicleRelics.Count==FamilyChronicle.Relics(freshStory).Count(),"Run unlock snapshot");
    freshStory.Settle(lessonRun.id,5,0,false);
    Check(!freshStory.chronicle.unlocked.Contains(storyClass+"_lesson"),"Five floors below class lesson threshold");
    freshStory.selected=new(){classId=storyClass,traits=[]};lessonRun=freshStory.Begin(82);
    var priorCrowns=freshStory.crowns;freshStory.Settle(lessonRun.id,6,0,false);
    Check(freshStory.chronicle.unlocked.Contains(storyClass+"_lesson")&&!freshStory.chronicle.unlocked.Contains(storyClass+"_victory"),"Completed failed climb unlocks lesson without invented victory");
    var recorded=freshStory.chronicle.classes[storyClass].returns;
    Check(freshStory.Settle(lessonRun.id,50,999,true)==0&&freshStory.chronicle.classes[storyClass].returns==recorded,"Repeat settlement cannot add wins or rewards");
    Check(freshStory.chronicle.classes[storyClass].wins==0&&freshStory.crowns-priorCrowns==6*Design.Get("systems","legacy").Number("amount"),"Story adds no crown bonus");
    freshStory.selected=new(){classId=storyClass,traits=[]};var wonStoryRun=freshStory.Begin(83);
    freshStory.Settle(wonStoryRun.id,50,0,true);
    var victoryStory=Design.Get("family_chronicle",storyClass+"_victory");
    Check(freshStory.chronicle.unlocked.Contains(storyClass+"_victory")&&FamilyChronicle.SelectMotto(freshStory,victoryStory.Text("rewardId")),"Victory unlocks original motto");
}
Check(FamilyChronicle.Relics(freshStory).Distinct().Count()==3,"All class lessons unlock three shared family relics");
var frozenStory=new FamilyProfile();FamilyChronicle.Reconcile(frozenStory);frozenStory.selected=new(){classId="mage",traits=[]};var frozenStoryRun=frozenStory.Begin(1);
frozenStory.chronicle.classes["knight"]=new(){maxFloor=6};FamilyChronicle.Reconcile(frozenStory);
Check(!FamilyChronicle.RelicAvailable(frozenStory,"guardian_knot"),"New family unlock cannot alter active run eligibility");
frozenStory.Settle(frozenStoryRun.id,1,0,false);frozenStory.Begin(2);
Check(FamilyChronicle.RelicAvailable(frozenStory,"guardian_knot"),"Next run captures unlocked family relic");
Check(dropRelics.Count==10&&dropRelics.Select(r=>r.Text("id")).Distinct().Count()==10,"Ten distinct exclusive relic drops, seven baseline plus three Chronicle unlocks");
Check(dropRelics.All(r=>new[]{"Common","Uncommon","Rare"}.Contains(r.Text("rarity"))),"No starter in ordinary drop definitions");
foreach(var relic in dropRelics)
{
    Check(!relic.Text("description").Contains('{')&&relic.Number("extra")<=1,"Complete bounded relic text/resources "+relic.Text("id"));
    Check(Design.Get("relic_art",relic.Text("id")).Text("license")=="MIT","Original relic artwork "+relic.Text("id"));
}
Check(Design.Rows("relic_art").Select(r=>r.Text("sha256")).Distinct().Count()==10,"Distinct relic silhouettes");
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
// Recovery text must correspond to a non-empty native selection at both levels.
foreach (var row in Design.Rows("cards").Where(r => r.Text("special") is "exhume" or "retrieve"))
foreach (var upgraded in new[] { false, true })
    Check(new CardDesign(row.Text("id"), upgraded).SpecialAmount > 0, row.Text("id") + " recovery selects at least one card");
foreach (var upgraded in new[] { false, true })
{
    var quiver = new CardDesign("ranger_second_quiver", upgraded);
    var meditate = new CardDesign("meditate", upgraded);
    Check(quiver.Cost == meditate.Cost && quiver.Amount("draw") >= meditate.Amount("draw") && quiver.Amount("energy") >= meditate.Amount("energy") && quiver.Amount("hpLoss") == 0, "Second Quiver avoids a strictly worse shared resource card");
    var reading = new CardDesign("mage_ash_reading", upgraded);
    Check(reading.Cost == 0 && reading.Amount("hpLoss") == 0 && reading.Row.Flag("exhaust") && reading.SpecialAmount == 1, "Free positive-draw Ash Reading is single use and exhausts fuel");
    var meditation = new CardDesign("knight_battle_meditation", upgraded);
    Check(meditation.Cost > 0 && meditation.Amount("discard") == 1, "Paid Battle Meditation keeps a fixed discard price");
    var prophecy = new CardDesign("mage_returned_prophecy", upgraded);
    var vault = new CardDesign("knight_arms_vault", upgraded);
    Check(prophecy.Cost == vault.Cost && prophecy.SpecialAmount == vault.SpecialAmount && prophecy.Amount("draw") > vault.Amount("draw"), "Rare recovery offers extra draw over uncommon recovery");
    var thesis = new CardDesign("mage_grand_thesis", upgraded);
    Check(thesis.Cost == 1 && thesis.Row.Flag("exhaust") && thesis.SpecialAmount == 1, "Grand Thesis has affordable draw without a wasted charge upgrade");
    Check(new CardDesign("mage_spell_notes",upgraded).Rules().StartsWith($"Scry {(upgraded?4:3)}.\nDraw 1 card(s)."),"Spell Notes text matches native Scry-before-draw execution");
}
var baseMeditation = new CardDesign("knight_battle_meditation");
var upgradedMeditation = new CardDesign("knight_battle_meditation", true);
Check(upgradedMeditation.Amount("draw") - upgradedMeditation.Amount("discard") > baseMeditation.Amount("draw") - baseMeditation.Amount("discard"), "Battle Meditation upgrade improves net hand size");
foreach (var row in Design.Rows("cards").Where(r => r.Text("special") == "exhume"))
    Check(!new CardDesign(row.Text("id")).RecoverableFromExhaust, row.Text("id") + " cannot participate in discounted recovery cycles");
Check(new CardDesign("mage_guard").RecoverableFromExhaust && new CardDesign("mage_memory_prism").RecoverableFromExhaust, "Recovery restriction preserves ordinary cards and discard recovery");
foreach(var row in Design.Rows("cards").Where(r=>r.Text("special") is "exhume" or "retrieve"))
    Check(!new CardDesign(row.Text("id")).RecoverableFromDiscard,row.Text("id")+" cannot cycle through discounted discard recovery");
Check(new CardDesign("ranger_rapid_fire").RecoverableFromDiscard,"Discard recovery still returns ordinary Attacks");
foreach (var upgraded in new[] {false,true})
{
    var secondWind=new CardDesign("knight_second_wind",upgraded);
    Check(secondWind.Cost==1&&secondWind.SpecialAmount==1,"Second Wind requires only one fuel card");
    var familiar=new CardDesign("mage_rune_familiar",upgraded);
    Check(familiar.Cost==1&&familiar.Magic==(upgraded?2:1),"Rune Familiar upgrade improves payoff rather than repairing cost");
    var vigil=new CardDesign("ranger_long_vigil",upgraded);
    Check(vigil.Cost==2&&vigil.Damage==(upgraded?19:15),"Long Vigil has an affordable retained finisher budget");
    foreach(var poisonId in new[]{"ranger_poison_tip","mage_toxic_cloud"})
        Check(new CardDesign(poisonId,upgraded).Amount("poison")== (upgraded?6:4),poisonId+" supplies a useful Poison seed without global manor scaling");
}
foreach (var traitId in new[] { "nocolor", "oldyellowtint", "colortrails", "fmffan" }) Check(Design.Get("traits", traitId).Number("goldBonus") == 0, "No free crown bonus: " + traitId);
foreach(var upgraded in new[]{false,true}){
    var nativeBludgeon=upgraded?42:32;
    var execution=new CardDesign("knight_royal_execution",upgraded);
    var spear=new CardDesign("mage_celestial_spear",upgraded);
    Check(execution.Cost==3&&execution.Damage<nativeBludgeon&&execution.Damage*execution.SpecialAmount>nativeBludgeon,"Royal Execution remains below native unconditional budget and rewards its health condition");
    Check(spear.Cost==3&&spear.Damage+4<nativeBludgeon,"Celestial Spear plus full Smith and two charges stays below native Bludgeon");
    var cascade=new CardDesign("ranger_arrow_cascade",upgraded);
    Check(cascade.Row.Flag("aoe")&&cascade.Row.Flag("exhaust")&&cascade.SpecialAmount<6&&cascade.Row.Number("damage")==0,"Arrow Cascade retains setup and single-use limits below native Finisher's per-attack base");
}
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
var storyPath=Path.Combine(directory,"chronicle.json");freshStory.chronicle.read.Add("mage_lesson");freshStory.Save(storyPath);
var savedStory=FamilyProfile.Load(storyPath);
Check(savedStory.chronicle.unlocked.SetEquals(freshStory.chronicle.unlocked)&&savedStory.chronicle.read.Contains("mage_lesson")&&savedStory.chronicle.motto=="Open Trail"&&savedStory.chronicle.classes["mage"].wins==1,"Chronicle, read state, motto and class records survive reload");
var oldStoryPath=Path.Combine(directory,"pre-chronicle.json");
var oldStory=new FamilyProfile{family="Old House",generation=8,crowns=777,manor=new(){["smith"]=1},selected=new(){name="Old Heir",classId="mage",traits=[]}};
var oldStoryRun=oldStory.Begin(44);oldStoryRun.bonuses["hp"]=17;
var oldStoryJson=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(oldStory,FamilyProfile.JsonOptions))!;oldStoryJson.AsObject().Remove("chronicle");oldStoryJson["active"]!.AsObject().Remove("chronicleRelics");File.WriteAllText(oldStoryPath,oldStoryJson.ToJsonString());
var adoptedStory=FamilyProfile.Load(oldStoryPath);
Check(adoptedStory.family=="Old House"&&adoptedStory.crowns==777&&adoptedStory.Level("smith")==1&&adoptedStory.active!.id==oldStoryRun.id&&adoptedStory.active.bonuses["hp"]==17,"Old family and active bonuses preserved");
Check(adoptedStory.chronicle.unlocked.SetEquals(["founding","first_return","restoration"])&&adoptedStory.chronicle.classes.Count==0&&adoptedStory.active!.chronicleRelics.Count==0,"Truthful legacy migration cannot invent class feats or alter old run pools");
Check(!FamilyChronicle.Reconcile(adoptedStory)&&FamilyProfile.Load(oldStoryPath).crowns==777,"Repeated migration is idempotent");
profile.family = "Cape House"; profile.crowns = 1000; profile.Save(path);
var first = Design.Rows("manor").First(r => r.Text("requires") == "none");
var id = first.Text("id"); var price = profile.Cost(first);
Check(profile.PreviewPurchases([id])!.crowns == 1000 - price && profile.crowns == 1000 && profile.Level(id) == 0, "Staging cannot mutate");
Check(profile.CommitPurchases([id], path) && profile.crowns == 1000 - price && profile.Level(id) == 1, "Confirmed atomic purchase");
Check(profile.chronicle.unlocked.Contains("restoration")&&FamilyProfile.Load(path).chronicle.unlocked.Contains("restoration"),"Only confirmed upgrade unlocks restoration story");
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
var isolatedRoot=Path.Combine(directory,"isolation");Directory.CreateDirectory(isolatedRoot);
Check(!TestIsolation.Validate(null,"personal",null,null),"Normal launches never enable testing");
Check(!TestIsolation.Validate("0","personal",null,null),"Explicit disabled test mode");
Check(TestIsolation.Validate("1","C:/User/SpireLegacy2PortLabOwned",Path.Combine(isolatedRoot,"family"),isolatedRoot),"Dedicated profile accepts child family directory");
foreach(var bad in new[]{"true","yes","2"}) {var rejected=false;try{TestIsolation.Validate(bad,"SpireLegacy2PortLab",Path.Combine(isolatedRoot,"child"),isolatedRoot);}catch(InvalidOperationException){rejected=true;}Check(rejected,"Invalid mode fails closed");}
foreach(var pair in new[]{("personal",Path.Combine(isolatedRoot,"child"),isolatedRoot),("SpireLegacy2PortLab",(string?)null,isolatedRoot),("SpireLegacy2PortLab",Path.Combine(isolatedRoot,"child"),(string?)null),("SpireLegacy2PortLab",isolatedRoot,isolatedRoot),("SpireLegacy2PortLab",Path.Combine(isolatedRoot,"..","outside"),isolatedRoot),("SpireLegacy2PortLab","relative",isolatedRoot)}) {
 var rejected=false;try{TestIsolation.Validate("1",pair.Item1,pair.Item2,pair.Item3);}catch(InvalidOperationException){rejected=true;}Check(rejected,"Unsafe test profile rejected before access");
}
var receiptPath=Path.Combine(directory,"receipts.txt");var receiptId=Guid.NewGuid().ToString("N");new TestRequestLedger().Claim(receiptId,receiptPath);
var replayRejected=false;try{new TestRequestLedger().Claim(receiptId,receiptPath);}catch(InvalidDataException){replayRejected=true;}Check(replayRejected,"Restart cannot replay a claimed mutation");
var originalDiagnostic=new InvalidOperationException("Original fixture",new ArgumentException("Inner fixture"));
var textDiagnostic=DiagnosticText.Describe(originalDiagnostic,()=>throw new FileNotFoundException("Missing manifest"));
Check(textDiagnostic.Contains("Original fixture")&&textDiagnostic.Contains("Inner fixture")&&textDiagnostic.Contains("metadata unavailable"),"Diagnostic metadata failure preserves original and inner exception");
Check(!DiagnosticText.Describe(new Exception("api_key=fixtureSecret token=fixtureToken"),()=>"Bearer fixtureAuth").Contains("fixtureSecret"),"Diagnostic secrets redacted");
var startup=new StartupGate();var starts=0;
Check(startup.Run(()=>starts++),"First startup executes");Check(!startup.Run(()=>starts++)&&starts==1,"Repeated startup adds no side effects");
var parallelGate=new StartupGate();var parallelCount=0;Parallel.For(0,12,_=>parallelGate.Run(()=>parallelCount++));Check(parallelCount==1,"Concurrent initialization executes once");
var failedGate=new StartupGate();var startupFailure=new InvalidOperationException("Fixture API missing");
try{failedGate.Run(()=>throw startupFailure);}catch(InvalidOperationException e){Check(ReferenceEquals(e,startupFailure),"Startup preserves original failure");}
try{failedGate.Run(()=>starts++);Check(false,"Failed startup cannot retry");}catch(InvalidOperationException e){Check(ReferenceEquals(e.InnerException,startupFailure)&&starts==1,"Failed startup cannot resume partially");}
var recursiveGate=new StartupGate();try{recursiveGate.Run(()=>recursiveGate.Run(()=>{}));Check(false,"Recursive startup rejected");}catch(InvalidOperationException){Check(true,"Recursive initialization fails clearly");}
var resolved=ApiContract.Require(typeof(ApiFixture),"Operation",typeof(Task),[typeof(int)]);
Check(resolved.GetParameters().Single().ParameterType==typeof(int),"Exact signature ignores unrelated overloads");
Check(ApiContract.Require(typeof(ApiFixture),"GenericTask",typeof(Task),Type.EmptyTypes).ReturnType==typeof(Task<int>),"Generic Task return remains compatible");
foreach(var name in new[]{"Missing","WrongReturn"})try{ApiContract.Require(typeof(ApiFixture),name,typeof(Task),Type.EmptyTypes);Check(false,"Drift must fail");}catch(MissingMethodException e){Check(e.Message.Contains(name)&&e.Message.Contains("Expected")&&e.Message.Contains("Available"),"API failure identifies expected and available signatures");}
try{ApiContract.Invoke(typeof(ApiFixture).GetMethod("ThrowOriginal")!,null,[]);Check(false,"Reflection must propagate failure");}catch(InvalidOperationException e){Check(ReferenceEquals(e,ApiFixture.Failure),"Reflection preserves original exception identity");}
PilotRewardChecks.Run(Check);
foreach(var upgraded in new[]{false,true}){
    var slow=new CardDesign("mage_slow_incantation",upgraded);
    Check(slow.Cost==1&&slow.Amount("nextEnergy")==1&&slow.Amount("nextDraw")==1&&slow.Amount("nextBlock")==3,"Slow Incantation stays paid with unchanged delayed payout");
    Check(slow.Block==(upgraded?2:0)&&!slow.Row.Flag("exhaust"),"Slow Incantation upgrades protection without free cycling");
    var quiver=new CardDesign("ranger_second_quiver",upgraded);
    Check(quiver.Amount("draw")==2&&quiver.Amount("energy")==1+(upgraded?1:0)&&quiver.Row.Flag("exhaust"),"Second Quiver matches native Adrenaline draw/Energy rather than upgrading both");
    Check(new CardDesign("mage_mana_fountain",upgraded).Amount("energy")==2+(upgraded?1:0),"Mana Fountain has a bounded single-use Energy burst");
    var bookmark=new CardDesign("mage_eternal_bookmark",upgraded);
    Check(bookmark.Cost==1&&bookmark.Magic==2+(upgraded?1:0),"Rare Bookmark improves retention rather than repairing a costly setup");
    var recovery=new CardDesign("knight_battlefield_recovery",upgraded);
    Check(recovery.Cost==1&&recovery.Amount("draw")==2+(upgraded?1:0)&&recovery.Amount("heal")==4+(upgraded?1:0)&&recovery.Row.Flag("exhaust"),"Battlefield Recovery leaves Energy to use drawn cards without increasing healing");
    var avalanche=new CardDesign("knight_iron_avalanche",upgraded);
    Check(avalanche.Cost==2&&avalanche.SpecialAmount==18&&avalanche.Row.Flag("exhaust"),"Iron Avalanche fits one paid guard plus its conditional single-use attack in three Energy");
}
foreach(var card in Design.Rows("cards").Where(c=>c.Text("id")!="ranger_second_quiver")){
    Check(card.Number("upgradeDraw")==-1,"Existing draw upgrades preserve the shared resource increment: "+card.Text("id"));
}
foreach(var upgraded in new[]{false,true}){
    var spellweave=new CardDesign("mage_spellweave",upgraded);
    Check(spellweave.Cost==1,"Spellweave always pays Energy, including after upgrade");
    Check(spellweave.Amount("nextEnergy")==1&&spellweave.Amount("nextDraw")==2,"Spellweave upgrade cannot increase delayed resource payout");
    Check(spellweave.Block==(upgraded?2:0)&&!spellweave.Row.Flag("exhaust"),"Spellweave upgrades modest immediate protection while remaining reusable");
}
LifecycleChecks.Run(Check,directory);
foreach(var up in new[]{false,true}) {
    var reset=new CardDesign("ranger_trail_reset",up);
    Check(reset.Row.Flag("exhaust")&&reset.Cost==(up?0:1)&&reset.Row.Text("special")=="refill_hand","Trail Reset keeps its hand exchange and cost but cannot replay the free Reflex loop");
}
foreach(var up in new[]{false,true}) {
    var meditation=new CardDesign("knight_battle_meditation",up);var study=new CardDesign("study",up);
    Check(meditation.Cost==1&&meditation.Amount("draw")==3+(up?1:0)&&meditation.Amount("discard")==1,"Battle Meditation matches the paid native dig budget without a free upgrade");
    Check(meditation.Amount("draw")-meditation.Amount("discard")==study.Amount("draw"),"Meditation preserves Study hand advantage while digging one card deeper");
}
foreach(var up in new[]{false,true}) {
    var dose=new CardDesign("ranger_double_dose",up);var reserve=new CardDesign("ranger_venom_reserve",up);
    Check(dose.Cost==1&&!dose.Row.Flag("exhaust")&&!dose.Row.Flag("retain"),"Double Dose is a paid repeatable poison starter");
    Check(dose.Row.Text("special")=="poison_twice"&&dose.SpecialAmount==(up?4:3)&&dose.Rules()==$"Apply {(up?4:3)} Poison twice.","Double Dose has two fixed doses and an amount upgrade");
    Check(reserve.Row.Text("special")=="double_poison"&&reserve.Row.Flag("retain")&&reserve.Row.Flag("exhaust")&&reserve.Cost==(up?0:1),"Venom Reserve remains the retained one-use multiplier");
}
PersistenceChecks.Run(Check,directory);
Console.WriteLine(JsonSerializer.Serialize(new { result = "passed", assertions = count, cards = 360, inheritedTraits = traitIds.Length, familyFixture = directory }));

public static class ApiFixture
{
 public static readonly InvalidOperationException Failure=new("Original reflected exception");
 public static Task Operation(int value)=>Task.CompletedTask;
 public static Task Operation(string value)=>Task.CompletedTask;
 public static Task<int> GenericTask()=>Task.FromResult(1);
 public static int WrongReturn()=>0;
 public static void ThrowOriginal()=>throw Failure;
}
