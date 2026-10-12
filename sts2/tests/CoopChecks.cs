using System.Text.Json.Nodes;
using SpireLegacy;

public static class CoopChecks
{
    public static void Run(Action<bool,string> check)
    {
        var profile=new FamilyProfile();FamilyChronicle.Reconcile(profile);
        var run=FamilySnapshot.Freeze(profile,new(){name="Peer",classId="mage",traits=[]},2,true);
        var node=JsonNode.Parse(FamilySnapshot.Serialize(run))!;node.AsObject().Remove("generation");
        var rejected=false;try{FamilySnapshot.ReadNetwork(node.ToJsonString(),2);}catch(InvalidDataException){rejected=true;}
        check(rejected,"A missing co-op generation must not silently alter starter genetics");
        var fields=new[]{"family","motto","ownerNetId","generation","banner","coop","floors","quick","chronicleRelics"};
        foreach(var field in fields) {
            node=JsonNode.Parse(FamilySnapshot.Serialize(run))!;node.AsObject().Remove(field);
            rejected=false;try{FamilySnapshot.ReadNetwork(node.ToJsonString(),2);}catch(InvalidDataException){rejected=true;}
            check(rejected,"Network snapshot requires frozen field "+field);
        }
        foreach(var bonus in new[]{"hp","strength","dexterity","draw","gold","heal"}) {
            node=JsonNode.Parse(FamilySnapshot.Serialize(run))!;node["bonuses"]!.AsObject().Remove(bonus);
            rejected=false;try{FamilySnapshot.ReadNetwork(node.ToJsonString(),2);}catch(InvalidDataException){rejected=true;}
            check(rejected,"Network snapshot requires bonus "+bonus);
        }
        check(FamilySnapshot.ReadPlayerSave(FamilySnapshot.Serialize(run),2).heir.classId=="mage","Correct remote saved owner preserved");
        try{FamilySnapshot.ReadPlayerSave(FamilySnapshot.Serialize(run),1);check(false,"Wrong saved owner rejected");}catch(InvalidDataException){check(true,"Wrong saved owner rejected");}
        var legacy=FamilySnapshot.Freeze(profile,run.heir,0,false);
        check(FamilySnapshot.ReadPlayerSave(FamilySnapshot.Serialize(legacy),99).id==legacy.id,"Legacy zero-owner solo remains compatible");
        var state=new CoopSelectionState();var session=Guid.NewGuid().ToString();state.Reset(session);var payload=FamilySnapshot.Serialize(run);
        check(state.Receive(2,session,1,payload,2,false)&&state.Complete([2]),"First ready selection accepted");
        check(state.Receive(2,session,1,payload,2,true),"Identical duplicate is idempotent");
        var changed=FamilySnapshot.Read(payload);changed.heir.classId="ranger";var other=FamilySnapshot.Serialize(changed);
        check(!state.Receive(2,session,2,other,2,true)&&state.Snapshots[2].heir.classId=="mage","Same-ID ready mutation rejected");
        check(state.Receive(2,session,2,"",2,true)&&!state.Complete([2]),"Unready revokes before native unready delivery");
        check(!state.Receive(2,session,1,payload,2,false),"Delayed ready cannot undo newer revocation");
        check(state.Receive(2,session,3,other,2,false)&&state.Snapshots[2].heir.classId=="ranger","Unready owner may freeze a new selection");
        var alias=state.Snapshots[2];alias.heir.classId="knight";
        check(state.Snapshots[2].heir.classId=="ranger","Callers cannot mutate ledger snapshots");
        var old=session;session=Guid.NewGuid().ToString();state.Reset(session);
        check(!state.Receive(2,old,999,payload,2,false)&&state.Snapshots.Count==0,"Old lobby/rejoined slot cannot replay");
        check(!state.Receive(1,session,1,payload,2,false)&&!state.Receive(2,session,0,payload,2,false),"Unsupported protocol/revision cannot mutate");
        try{state.Receive(2,session,1,payload,3,false);check(false,"Envelope owner mismatch");}catch(InvalidDataException){check(true,"Envelope owner mismatch");}
        for(ulong owner=1;owner<=4;owner++) {
            var peer=FamilySnapshot.Freeze(new FamilyProfile(),new(){classId=owner%2==0?"mage":"knight",traits=[]},owner,true);
            check(state.Receive(2,session,1,FamilySnapshot.Serialize(peer),owner,false),"Up to four independent peers");
        }
        check(state.Complete([1,2,3,4])&&!state.Complete([1,2,3,4,5]),"All required owners, including a future random Heir, form start barrier");
        check(!state.Receive(2,session,1,"",5,false),"Unknown-owner flood stays bounded");
        state.Invalidate(3);check(!state.Complete([1,2,3,4])&&state.Revision(3)==1,"Character switch revokes without forgetting anti-replay revision");
        var changes=0;
        for(var seed=0;seed<40;seed++) {
            var random=new Random(seed);var ledger=new CoopSelectionState();var incarnation=Guid.NewGuid().ToString();ledger.Reset(incarnation);
            var latest=new int[4];var ready=new bool[4];var captures=new Dictionary<ulong,string>();
            for(var step=0;step<100;step++) {
                var index=random.Next(4);var owner=(ulong)(index+1);var revision=++latest[index];var isReady=random.Next(2)==0;
                var data=isReady?FamilySnapshot.Serialize(FamilySnapshot.Freeze(new FamilyProfile(),new(){classId=new[]{"knight","mage","ranger"}[index%3],traits=[]},owner,true)):"";
                check(ledger.Receive(2,incarnation,revision,data,owner,false),"Reference current owner message");
                ready[index]=isReady;if(isReady)captures[owner]=data;else captures.Remove(owner);
                if(revision>1)check(!ledger.Receive(2,incarnation,revision-1,data,owner,false),"Reference delayed message rejected");
                check(ledger.Complete([1,2,3,4])==ready.All(v=>v),"Independent four-owner ready model");
                check(ledger.Snapshots.Count==captures.Count&&captures.All(p=>FamilySnapshot.Serialize(ledger.Snapshots[p.Key])==p.Value),"Independent frozen owner payload model");
                changes++;
            }
        }
        var mutated=FamilySnapshot.Read(payload);mutated.floors=15;mutated.quick=true;
        check(FamilySnapshot.SameInheritance(run,mutated),"Mutable run counters may synchronize without changing inheritance");
        mutated.heir.classId="ranger";check(!FamilySnapshot.SameInheritance(run,mutated),"Reconnect cannot change frozen class");
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{coop="passed",fourOwnerTransitions=changes,missingFields=fields.Length+6}));
    }
}
