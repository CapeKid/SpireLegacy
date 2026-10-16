namespace SpireLegacy;

// Fixture-only reward heuristic; inputs are descriptions, not mutable game objects.
public static class PilotRewards
{
    public record Card(string Id,int Cost,double Damage,double Block,int Draw,int Energy,int Poison,int Strength,bool Power,bool Exhaust,bool Engine,int Weak=0,int Vulnerable=0,string Special="none",int SpecialAmount=0,int Hits=1,bool Attack=false,bool SelfExhaust=false,int NextEnergy=0,int NextDraw=0,double NextBlock=0,bool Usable=true,int HpLoss=0);
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
    public static double Score(Card offer,IReadOnlyList<Card> deck,bool defensive=false,int? hp=null)
    {
        // Finite for JSON logs, but always below the reward skip threshold.
        if(!offer.Usable)return -10000;
        var payment=Math.Max(0,offer.HpLoss);
        // Conservative acquisition policy: skip currently fatal payments, without
        // assuming a future heal. Unknown HP still incurs the base payment tax.
        if(payment>0&&hp is {} health&&!PilotHealth.Affordable(health,payment))return -10000;
        var cost=Math.Max(1,offer.Cost);
        var score=(offer.Damage+offer.Block)/cost+offer.Draw*3+offer.Energy*4+offer.Poison*2+offer.Strength*4+offer.Weak*2+offer.Vulnerable*2+(offer.Power?6:0);
        score+=ConditionalValue(offer,deck)/cost;
        // Delayed resources are real rewards, but not available on the turn spent setting them up.
        score+=offer.NextEnergy*2+offer.NextDraw*1.5+offer.NextBlock*.5/cost;
        if(deck.Count(c=>c.Block>0)*3<deck.Count)score+=offer.Block/cost;
        if(deck.Count(c=>c.Poison>0)>=2)score+=offer.Poison*2;
        if(deck.Any(c=>c.Engine)&&offer.Exhaust)score+=4;
        if(deck.Count(c=>c.Exhaust)>=2&&offer.Engine)score+=6;
        score-=deck.Count(c=>c.Id==offer.Id)*3;
        if(defensive&&deck.Count>0){
            // Quality/affordability matter as well as the count of defensive cards.
            // Three immediate Block per average draw is an explicit heuristic target.
            var density=deck.Average(c=>c.Block/Math.Max(1,c.Cost));
            var pressure=Math.Clamp(3-density,0,.5);
            score+=Math.Min(3,offer.Block/cost*pressure);
        }
        // Explicit heuristic units: one point per HP, plus bounded pressure as
        // the remaining health budget shrinks. This is not a measured exchange rate.
        score-=payment*(1+(hp is {} current?Math.Min(2d,payment/(double)Math.Max(1,current-payment)):0));
        return score;
    }
    public static int? Pick(IReadOnlyList<Card> offers,IReadOnlyList<Card> deck,bool defensive=false,int? hp=null)
    {
        if(offers.Count==0)return null;
        var best=Enumerable.Range(0,offers.Count).OrderByDescending(i=>Score(offers[i],deck,defensive,hp)).ThenBy(i=>offers[i].Id,StringComparer.Ordinal).First();
        return Score(offers[best],deck,defensive,hp)>=5?best:null;
    }
}
