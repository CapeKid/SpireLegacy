namespace SpireLegacy;

// Fixture-only reward heuristic; inputs are descriptions, not mutable game objects.
public static class PilotRewards
{
    public record Card(string Id,int Cost,double Damage,double Block,int Draw,int Energy,int Poison,int Strength,bool Power,bool Exhaust,bool Engine,int Weak=0,int Vulnerable=0);
    public static double Score(Card offer,IReadOnlyList<Card> deck)
    {
        var cost=Math.Max(1,offer.Cost);
        var score=(offer.Damage+offer.Block)/cost+offer.Draw*3+offer.Energy*4+offer.Poison*2+offer.Strength*4+offer.Weak*2+offer.Vulnerable*2+(offer.Power?6:0);
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
