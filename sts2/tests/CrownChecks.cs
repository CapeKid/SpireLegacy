using SpireLegacy;
public static class CrownChecks
{
    public static void Run(Action<bool,string> check)
    {
        check(CrownEarnings.Qualify(0,false,false,0,0,0,0,99)==(0,0),"Starting gold cannot pay crowns");
        check(CrownEarnings.Qualify(1,false,false,100,0,0,0,199)==(0,0),"Neow/free events without combat cannot pay crowns");
        check(CrownEarnings.Qualify(0,false,true,0,0,0,0,99)==(1,0),"Real first-fight death retains participation credit");
        check(CrownEarnings.Qualify(3,true,false,75,0,0,0,174)==(3,75),"Earned gold excludes starting capital");
        check(CrownEarnings.Qualify(3,true,false,75,50,0,0,124)==(3,25),"Earned gold is spent first");
        check(CrownEarnings.Qualify(3,true,false,75,100,0,0,74)==(3,0),"Spending beyond gains cannot convert starting gold");
        check(CrownEarnings.Qualify(3,true,false,75,0,5,20,149)==(3,50),"Lost/stolen gold cannot convert");
        check(CrownEarnings.Qualify(3,true,false,75,0,0,0,10)==(3,10),"Conversion cannot exceed actual gold");
        for(var remaining=0;remaining<200;remaining++)for(var gain=0;gain<200;gain++) {
            var a=CrownEarnings.Qualify(2,true,false,gain,40,5,10,remaining);
            check(a.floors==2&&a.gold==Math.Min(remaining,Math.Max(0,gain-55)),"Earned-gold conservation grid");
        }
        var family=new FamilyProfile{selected=new(){classId="knight",traits=[]}};var run=family.Begin(1);
        check(family.Settle(run.id,1,0,false,6)==2&&family.chronicle.classes["knight"].maxFloor==6,"Chronicle uses reached floor separately from paid progress");
    }
}
