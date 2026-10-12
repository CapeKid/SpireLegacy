using SpireLegacy;

public static class PilotRewardChecks
{
    public static void Run(Action<bool,string> check)
    {
        PilotRewards.Card Attack(string id,int damage=6)=>new(id,1,damage,0,0,0,0,0,false,false,false);
        PilotRewards.Card Guard(string id,int block=5)=>new(id,1,0,block,0,0,0,0,false,false,false);
        var balanced=Enumerable.Range(0,6).Select(i=>Attack("a"+i)).Concat(Enumerable.Range(0,4).Select(i=>Guard("g"+i))).ToArray();
        var attacks=balanced.Where(c=>c.Damage>0).ToArray();
        check(PilotRewards.Pick([Attack("hit",10),Guard("ward",6)],attacks)==1,"Reward policy fills defensive deck deficit");
        var duplicate=Attack("duplicate");var full=balanced.Concat(new[]{duplicate,duplicate}).ToArray();
        check(PilotRewards.Pick([duplicate,new("power",1,0,0,0,0,0,0,true,false,false)],full)==1,"Reward policy prefers new scaling to third redundant attack");
        check(PilotRewards.Pick([duplicate with{Cost=3},Guard("poor",1) with{Cost=3}],full)==null,"Reward policy can skip weak redundant offers");
        var fuel=new PilotRewards.Card("fuel",1,0,0,1,0,0,0,false,true,false);
        check(PilotRewards.Pick([Attack("hit",10),new("engine",1,0,0,0,0,0,0,true,false,true)],[fuel,fuel])==1,"Reward policy builds on existing exhaust support");
        var poison=new PilotRewards.Card("seed",1,0,0,0,0,4,0,false,false,false);
        check(PilotRewards.Pick([Attack("hit",10),poison with{Id="new",Poison=3}],[poison,poison])==1,"Reward policy builds on established Poison");
        var tie=new[]{Attack("z"),Attack("a")};
        check(tie[PilotRewards.Pick(tie,balanced)!.Value].Id=="a","Reward tie breaks deterministically by stable ID");
        check(PilotRewards.Pick([],balanced)==null,"Empty reward offer is safe");
    }
}
