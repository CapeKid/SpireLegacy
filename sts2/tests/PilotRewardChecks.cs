using SpireLegacy;

public static class PilotRewardChecks
{
    public static void Run(Action<bool,string> check)
    {
        check(!PilotHealth.Affordable(6,6),"Exact HP cost is fatal");
        check(!PilotHealth.Affordable(5,6),"Over-budget HP cost is refused");
        check(PilotHealth.Affordable(7,6),"Survivable HP cost remains available");
        check(PilotHealth.Affordable(1,0),"Low health permits cards without immediate HP cost");
        check(PilotHealth.Penalty(7,6)>PilotHealth.Penalty(80,6),"Near-death self-damage has higher opportunity cost");
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
        var counter=Attack("counter") with{Special="guard_bonus",SpecialAmount=10};
        check(PilotRewards.Pick([Attack("hit",12),counter],balanced)==1,"Block support makes guarded reward competitive");
        check(PilotRewards.Pick([Attack("hit",12),counter],attacks)==0,"No Block support does not promise guarded bonus");
        var ambush=counter with{Id="ambush",Special="marked_bonus"};
        check(PilotRewards.Pick([Attack("hit",12),ambush],[poison,poison])==1,"Poison supports marked reward");
        check(PilotRewards.Pick([Attack("hit",12),ambush],[Attack("mark") with{Vulnerable=2}])==1,"Vulnerable supports marked reward");
        check(PilotRewards.Pick([Attack("hit",12),ambush],[Attack("weak") with{Weak=2}])==0,"Weak alone does not activate marked bonus");
        check(PilotRewards.Pick([Attack("hit",10) with{Cost=2},Attack("axe",9) with{Cost=2,Special="execute",SpecialAmount=2}],balanced)==1,"Partial execution value can distinguish otherwise similar rewards");
        var retained=Attack("retained",3) with{Special="retain_damage",SpecialAmount=6};
        check(PilotRewards.Pick([Attack("hit",5),retained],balanced)==1,"Retention growth has discounted reward value");
        check(PilotRewards.Score(counter,[])==PilotRewards.Score(counter with{Special="none"},[]),"Empty deck provides no guarded support");
        check(PilotRewards.Score(ambush with{Special="unknown"},balanced)==PilotRewards.Score(ambush with{Special="none"},balanced),"Unmodeled conditions are not guessed");
        var repeated=Attack("repeated",12) with{Special="marked_bonus",SpecialAmount=2,Hits=2};
        check(PilotRewards.Pick([Attack("flat",14),repeated],[poison])==1,"Supported marked bonus values each hit of a reward");
        check(PilotRewards.Pick([Attack("flat",14),repeated],attacks)==0,"Repeated marked reward still needs support");
        check(PilotRewards.Pick([Guard("flat",5),Guard("stored",3) with{Special="retain_block",SpecialAmount=6}],balanced)==1,"Retained Block growth is valued without inventing damage");
    }
}
