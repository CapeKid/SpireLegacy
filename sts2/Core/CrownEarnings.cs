namespace SpireLegacy;

// Settlement inputs are earned progress, independent of Chronicle's reached floor.
public static class CrownEarnings
{
    public static (int floors,int gold) Qualify(int completedPoints,bool completedCombat,bool diedAfterPlay,long gained,long spent,long lost,long stolen,int remainingGold)
    {
        if(!completedCombat&&!diedAfterPlay)return (0,0);
        var floors=Math.Max(0,completedPoints);
        if(diedAfterPlay)floors=(int)Math.Min(int.MaxValue,(long)floors+1);
        // Conservatively spend earned gold first; returned stolen gold cancels theft.
        var unspent=Math.Max(0,gained);
        foreach(var debit in new[]{spent,lost,stolen})unspent-=Math.Min(unspent,Math.Max(0,debit));
        return (floors,(int)Math.Min(Math.Max(0,remainingGold),unspent));
    }
}
