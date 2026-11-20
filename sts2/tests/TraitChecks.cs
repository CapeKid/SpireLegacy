using SpireLegacy;

public static class TraitChecks
{
    public static void Run(Action<bool,string> check)
    {
        var rows=Design.Rows("traits").Where(r=>r.Flag("enabled")).ToArray();
        check(rows.Length==61,"Complete enabled trait audit roster");
        check(rows.All(r=>!r.Text("name").Contains('<')&&!r.Text("name").Contains('{')&&!r.Text("summary").Contains("{0}")),"Literal readable trait text");
        foreach(var retired in Design.Rows("traits").Where(r=>!r.Flag("enabled"))) {
            var snapshot=FamilySnapshot.Freeze(new FamilyProfile(),new(){traits=[retired.Text("id")]},2,true);
            check(FamilySnapshot.ReadPlayerSave(FamilySnapshot.Serialize(snapshot),2).heir.traits.Contains(retired.Text("id")),"Known retired traits preserve historical saved inheritance");
            try{FamilySnapshot.ReadNetwork(FamilySnapshot.Serialize(snapshot),2);check(false,"Retired trait cannot enter a new network selection");}catch(InvalidDataException){check(true,"Retired trait cannot enter a new network selection");}
        }
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
            foreach(var offer in profile.offers)check(offer.traits.Count==2&&offer.traits.All(t=>Design.Get("traits",t).Flag("enabled"))&&FamilyProfile.Compatible(Design.Get("traits",offer.traits[0]),Design.Get("traits",offer.traits[1])),"Seeded offers honor enabled roster and symmetric exclusions");
        }
        var family = new FamilyProfile();
        family.offers =
        [
            new() { name = "Ada", classId = "knight", traits = ["revealallchests", "bonuschestgold"] },
            new() { name = "Iris", classId = "mage", traits = ["itemsgoflying", "cointimer"] },
            new() { name = "June", classId = "ranger", traits = ["cheeronkills", "lowerstoreprice"] },
        ];
        var blitz = family.BlitzOffers();
        var repeated = family.BlitzOffers();
        for (var i = 0; i < blitz.Length; i++)
        {
            var heir = blitz[i];
            check(heir.name == family.offers[i].name && heir.classId == family.offers[i].classId &&
                heir.traits.SequenceEqual(repeated[i].traits) && heir.traits.Count == 2 &&
                heir.traits.All(id => id is not ("revealallchests" or "itemsgoflying" or "cointimer") && Design.Get("traits", id).Flag("enabled")) &&
                FamilyProfile.Compatible(Design.Get("traits", heir.traits[0]), Design.Get("traits", heir.traits[1])),
                "Blitz offers replace excluded traits with stable, compatible picks");
        }
        check(blitz[0].traits.Contains("bonuschestgold") && blitz[2].traits.SequenceEqual(family.offers[2].traits) &&
            family.offers[1].traits.SequenceEqual(["itemsgoflying", "cointimer"]),
            "Blitz preserves eligible traits and the saved offers used in other modes");
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{traits="passed",enabled=rows.Length,compatiblePairs=pairs,offerSeeds=500}));
    }
}
