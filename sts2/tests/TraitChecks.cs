using SpireLegacy;

public static class TraitChecks
{
    public static void Run(Action<bool,string> check)
    {
        var rows=Design.Rows("traits").Where(r=>r.Flag("enabled")).ToArray();
        check(rows.Length==61,"Complete enabled trait audit roster");
        check(rows.All(r=>!r.Text("name").Contains('<')&&!r.Text("name").Contains('{')&&!r.Text("summary").Contains("{0}")),"Literal readable trait text");
        var pairs=0;
        foreach(var first in rows)foreach(var second in rows.Where(r=>string.CompareOrdinal(first.Text("id"),r.Text("id"))<0&&FamilyProfile.Compatible(first,r))) {
            var selected=new[]{first,second};var heir=new FamilyProfile.Heir{traits=selected.Select(r=>r.Text("id")).ToList()};var rules=new HeirInheritance(heir);
            foreach(var cls in Design.Rows("classes")) {
                var hp=cls.Number("hp");var expected=selected.Any(r=>r.Text("effect")=="fragile")?1:Math.Max(20,hp+selected.Sum(r=>r.Number("hp")));
                check(rules.MaxHp(hp)==expected,"Pair HP override/clamp "+string.Join("/",heir.traits));
            }
            foreach(var amount in new[]{0m,1m,9m,10m,101m}) {
                var expected=amount;
                if(selected.Any(r=>r.Text("effect")=="no_heal"))expected=0;
                else {foreach(var r in selected.Where(r=>r.Text("effect")=="super_heal"))expected*=r.Number("amount");if(selected.Any(r=>r.Text("effect")=="vegan"))expected=Math.Floor(expected/2);}
                check(rules.Healing(amount)==expected,"Pair healing ordering/rounding");
            }
            foreach(var basic in Design.Rows("cards").Where(r=>r.Text("rarity")=="BASIC"))foreach(var upgraded in new[]{false,true}) {
                var design=new CardDesign(basic.Text("id"),upgraded,heir.traits);var plain=new CardDesign(basic.Text("id"),upgraded,[]);
                var mercy=heir.traits.Contains("cantattack")&&basic.Text("type")=="ATTACK";
                check(design.Mercy==mercy&&design.Damage==(mercy?0:plain.Damage)&&design.Block==(mercy?0:plain.Block),"Starter genotype pair preserves stats and Mercy suppression");
                check(design.MercyPoison==(mercy?(upgraded?3:2):0)&&design.Cost==plain.Cost,"Mercy genotype poison/cost upgrade invariant");
            }
            pairs++;
        }
        for(var seed=0;seed<500;seed++) {
            var profile=new FamilyProfile();profile.GenerateOffers(seed);
            check(profile.offers.Count==3,"Three heir offers");
            foreach(var offer in profile.offers)check(offer.traits.Count==2&&FamilyProfile.Compatible(Design.Get("traits",offer.traits[0]),Design.Get("traits",offer.traits[1])),"Seeded offers honor symmetric exclusions");
        }
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{traits="passed",enabled=rows.Length,compatiblePairs=pairs,offerSeeds=500}));
    }
}
