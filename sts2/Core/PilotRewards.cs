namespace SpireLegacy;

// Fixture-only reward heuristic; inputs are descriptions, not mutable game objects.
public static class PilotRewards
{
    public record Card(string Id,int Cost,double Damage,double Block,int Draw,int Energy,int Poison,int Strength,bool Power,bool Exhaust,bool Engine,int Weak=0,int Vulnerable=0,string Special="none",int SpecialAmount=0,int Hits=1,bool Attack=false,bool SelfExhaust=false);
    // Reward expectations, not combat damage. Partial payoffs avoid assuming every condition is active.
    private static double ConditionalValue(Card offer,IReadOnlyList<Card> deck)
    {
        double Coverage(Func<Card,bool> supported)=>deck.Count==0?0:Math.Min(.75,2d*deck.Count(supported)/deck.Count);
        return offer.Special switch {
            "execute"=>offer.Damage*Math.Max(0,offer.SpecialAmount-1)*.25,
            "guard_bonus"=>offer.SpecialAmount*Math.Max(1,offer.Hits)*Coverage(c=>c.Block>0),
            "marked_bonus"=>offer.SpecialAmount*Math.Max(1,offer.Hits)*Coverage(c=>c.Poison>0||c.Vulnerable>0),
            "block_damage"=>deck.Where(c=>c.Block>0).Select(c=>c.Block).DefaultIfEmpty(0).Average()*Coverage(c=>c.Block>0)*Math.Max(1,offer.Hits),
            "chain_damage"=>offer.SpecialAmount*Math.Min(2,deck.Count(c=>c.Attack&&c.Cost<=1)/3d)*.75*Math.Max(1,offer.Hits),
            "exhaust_damage"=>offer.SpecialAmount*Math.Min(3,deck.Count(c=>c.Exhaust||c.SelfExhaust)*.5)*Math.Max(1,offer.Hits),
            // One possible retention, discounted for the turn spent holding the card.
            "retain_damage" or "retain_block"=>offer.SpecialAmount*.5,
            _=>0
        };
    }
    public static double Score(Card offer,IReadOnlyList<Card> deck)
    {
        var cost=Math.Max(1,offer.Cost);
        var score=(offer.Damage+offer.Block)/cost+offer.Draw*3+offer.Energy*4+offer.Poison*2+offer.Strength*4+offer.Weak*2+offer.Vulnerable*2+(offer.Power?6:0);
        score+=ConditionalValue(offer,deck)/cost;
        if(deck.Count(c=>c.Block>0)*3<deck.Count)score+=offer.Block/cost;
        if(deck.Count(c=>c.Poison>0)>=2)score+=offer.Poison*2;
        if(deck.Any(c=>c.Engine)&&offer.Exhaust)score+=4;
        if(deck.Count(c=>c.Exhaust)>=2&&offer.Engine)score+=6;
        score-=deck.Count(c=>c.Id==offer.Id)*3;
        return score;
    }
    public static int? Pick(IReadOnlyList<Card> offers,IReadOnlyList<Card> deck)
    {
        if(offers.Count==0)return null;
        var best=Enumerable.Range(0,offers.Count).OrderByDescending(i=>Score(offers[i],deck)).ThenBy(i=>offers[i].Id,StringComparer.Ordinal).First();
        return Score(offers[best],deck)>=5?best:null;
    }
}
