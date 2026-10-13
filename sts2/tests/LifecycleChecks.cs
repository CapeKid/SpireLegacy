using System.Text.Json;
using SpireLegacy;

public static class LifecycleChecks
{
    public static bool Child(string[] args)
    {
        if(args.Length==0||args[0]!="--lifecycle-crash")return false;
        var profile=FamilyProfile.Load(args[1]);
        FamilyStore.Fault.Value=s=>{if(s==args[2]){File.WriteAllText(args[3],"ready");Thread.Sleep(Timeout.Infinite);}};
        profile.CommitSettlement(profile.active!,profile.active!.ownerNetId,6,100,true,args[1]);return true;
    }
    public static void Run(Action<bool,string> check,string root)
    {
        var dir=Path.Combine(root,"lifecycle");Directory.CreateDirectory(dir);
        var p=new FamilyProfile { selected=new(){name="Failing heir",classId="mage",traits=[]} };
        var run=p.Begin(49);var path=Path.Combine(dir,"settlement.json");p.Save(path);
        var before=JsonSerializer.Serialize(p,FamilyProfile.JsonOptions);
        FamilyStore.Fault.Value=s=>{if(s=="before-commit")throw new IOException("Settlement write interrupted");};
        try { p.CommitSettlement(run,0,6,100,true,path); }
        catch(IOException) { }
        finally { FamilyStore.Fault.Value=null; }
        check(JsonSerializer.Serialize(p,FamilyProfile.JsonOptions)==before,"Failed settlement must preserve in-memory bank, generation, active heir and Chronicle");
        p=new(){crowns=100};path=Path.Combine(dir,"purchase.json");p.Save(path);
        FamilyStore.Fault.Value=s=>{if(s=="after-commit")throw new IOException("Purchase response interrupted after commit");};
        try {p.CommitPurchases(["vitality"],path);}
        catch(IOException) { }
        finally {FamilyStore.Fault.Value=null;}
        check(p.crowns==FamilyProfile.Load(path).crowns&&p.Level("vitality")==1,"Post-commit interruption must leave live purchases matching durable bank");
        var serial=0;
        string PathFor()=>Path.Combine(dir,"case-"+(++serial)+".json");
        string State(FamilyProfile f)=>JsonSerializer.Serialize(f,FamilyProfile.JsonOptions);
        FamilyProfile Fresh(string cls="knight") {
            var f=new FamilyProfile{crowns=1000,selected=new(){name="Lifecycle",classId=cls,traits=[]}};
            FamilyChronicle.Reconcile(f);f.GenerateOffers(49);return f;
        }
        void FailAt(string step,Action action) {
            FamilyStore.Fault.Value=s=>{if(s==step)throw new IOException("Lifecycle fixture: "+s);};
            try {action();check(false,"Fault must reach requested transition boundary: "+step);}
            catch(IOException) { }
            finally {FamilyStore.Fault.Value=null;}
        }
        foreach(var step in new[]{"before-write","after-write","after-flush","before-backup","before-commit","after-commit"}) {
            var committed=step=="after-commit";
            foreach(var transition in new[]{"start","settle","purchase","adopt","read","motto","select"}) {
                var f=Fresh();var file=PathFor();FamilyProfile.FamilyRun snapshot;
                if(transition=="motto"){f.chronicle.classes["knight"]=new(){returns=1,wins=1};FamilyChronicle.Reconcile(f);}
                if(transition=="settle")snapshot=f.Begin(49);
                else snapshot=FamilySnapshot.Freeze(f,f.selected!);
                f.Save(file);var old=State(f);
                FailAt(step,()=>{switch(transition) {
                    case "start":f.CommitRunStart(snapshot,file);break;
                    case "settle":f.CommitSettlement(snapshot,0,6,100,true,file);break;
                    case "purchase":f.CommitPurchases(["vitality","smith"],file);break;
                    case "read":f.CommitChronicleRead("founding",file);break;
                    case "motto":f.CommitMotto("Unbroken Oath",file);break;
                    case "select":f.CommitHeirSelection(new(){name="New choice",classId="ranger",traits=[]},file);break;
                    default:f.CommitAdoption(snapshot,0,file);break;
                }});
                check(State(f)==State(FamilyProfile.Load(file)),"Live/disk agreement at "+transition+" "+step);
                check((State(f)!=old)==committed,"Only durable commit changes "+transition+" "+step);
                if(transition=="settle"&&committed) {
                    check(f.generation==1&&f.crowns==1036&&f.chronicle.classes["knight"].wins==1,"Complete settlement record at durable boundary");
                    check(f.CommitSettlement(snapshot,0,50,999,true,file)==0&&State(f)==State(FamilyProfile.Load(file)),"Retry after committed response cannot repeat payout");
                }
            }
        }
        // Independent reference model: deterministic mixed legal and adversarial
        // transitions. Check durable bank, generation and records after every step.
        for(var seed=0;seed<12;seed++) {
            var rng=new Random(seed);var f=Fresh();var file=PathFor();f.Save(file);
            var bank=1000;var generation=0;var activeTreasury=0;string? activeId=null;
            var settled=new HashSet<string>();var oldRuns=new List<FamilyProfile.FamilyRun>();
            var returns=new Dictionary<string,int>();var wins=new Dictionary<string,int>();
            var levels=new Dictionary<string,int>();
            for(var turn=0;turn<100;turn++) {
                switch(rng.Next(7)) {
                    case 0 when activeId==null:
                        var cls=new[]{"knight","mage","ranger"}[rng.Next(3)];
                        f.selected=new(){name="Model",classId=cls,traits=[]};
                        var next=FamilySnapshot.Freeze(f,f.selected,coop:turn%2==1,owner:(ulong)(turn%2+1));
                        check(next.coop?f.CommitAdoption(next,next.ownerNetId,file):f.CommitRunStart(next,file),"Model valid solo/co-op embark");
                        activeId=next.id;activeTreasury=levels.GetValueOrDefault("vault")*5;oldRuns.Add(next);break;
                    case 1 when activeId!=null:
                        var current=f.active!;var floors=rng.Next(51);var gold=rng.Next(401);var victory=rng.Next(4)==0;
                        var amount=(floors*2+gold/25+(victory?20:0))*(100+activeTreasury)/100;
                        check(f.CommitSettlement(current,current.ownerNetId,floors,gold,victory,file)==amount,"Model crown formula");
                        bank+=amount;generation++;settled.Add(activeId);activeId=null;
                        returns[current.heir.classId]=returns.GetValueOrDefault(current.heir.classId)+1;
                        if(victory)wins[current.heir.classId]=wins.GetValueOrDefault(current.heir.classId)+1;
                        break;
                    case 2:
                        var beforeState=State(f);var beforeBytes=File.ReadAllBytes(file);
                        if(oldRuns.FirstOrDefault(r=>settled.Contains(r.id)) is {} replay)
                            check(f.CommitSettlement(replay,replay.ownerNetId,50,999,true,file)==0,"Historical replay rejected");
                        else check(f.Settle(Guid.NewGuid().ToString(),50,999,true)==0,"Unknown run callback rejected");
                        check(State(f)==beforeState&&beforeBytes.SequenceEqual(File.ReadAllBytes(file)),"Invalid callback never writes or changes family");break;
                    case 3:
                        var row=Design.Rows("manor")[rng.Next(6)];var id=row.Text("id");var level=levels.GetValueOrDefault(id);
                        var price=row.Number("baseCost")+level*row.Number("costStep");var prerequisite=row.Text("requires");
                        var allowed=activeId==null&&level<row.Number("maxLevel")&&bank>=price&&(prerequisite=="none"||levels.GetValueOrDefault(prerequisite)>0);
                        check(f.CommitPurchases([id],file)==allowed,"Reference purchase prerequisite/cap/bank decision");
                        if(allowed){bank-=price;levels[id]=level+1;}
                        break;
                    case 4:
                        f=FamilyProfile.Load(file);check(!FamilyChronicle.Reconcile(f),"Reload reconciliation is stable");break;
                    case 5 when activeId!=null:
                        var wrong=FamilySnapshot.Read(FamilySnapshot.Serialize(f.active!));wrong.familyId=Guid.NewGuid().ToString();
                        check(f.CommitSettlement(wrong,wrong.ownerNetId,50,999,true,file)==0,"Mismatched family cannot settle");
                        wrong=FamilySnapshot.Read(FamilySnapshot.Serialize(f.active!));wrong.ownerNetId+=99;
                        check(f.CommitSettlement(wrong,wrong.ownerNetId,50,999,true,file)==0,"Same-ID foreign owner cannot settle");break;
                    default:
                        check(f.PreviewPurchases(["unknown"])==null&&!f.CommitPurchases([],file),"Invalid/empty purchase queue rejected");break;
                }
                check(f.crowns==bank&&f.generation==generation&&f.active?.id==activeId,"Reference model bank/generation/active");
                check(f.chronicle.settledRuns.SetEquals(settled),"Reference model exactly-once run ledger");
                check(returns.All(h=>f.chronicle.classes[h.Key].returns==h.Value)&&wins.All(h=>f.chronicle.classes[h.Key].wins==h.Value),"Reference class return/victory counts");
                check(levels.All(l=>f.Level(l.Key)==l.Value),"Reference manor levels");
                check(State(f)==State(FamilyProfile.Load(file)),"Every model transition survives restart");
            }
        }
        foreach(var cls in new[]{"knight","mage","ranger"})foreach(var threshold in new[]{5,6,7})foreach(var victories in new[]{0,1,2}) {
            var f=Fresh(cls);f.chronicle.classes[cls]=new(){maxFloor=threshold,returns=3,wins=victories};
            f.generation=1;FamilyChronicle.Reconcile(f);var file=PathFor();f.Save(file);
            check(f.chronicle.unlocked.Contains(cls+"_lesson")== (threshold>=6),"Chronicle floor threshold below/at/above");
            check(f.chronicle.unlocked.Contains(cls+"_victory")== (victories>=1),"Chronicle victory threshold below/at/above");
            var motto=Design.Get("family_chronicle",cls+"_victory").Text("rewardId");
            check(FamilyChronicle.SelectMotto(f,motto)==(victories>0),"Motto requires earned class victory");
            foreach(var entry in f.chronicle.unlocked)f.chronicle.read.Add(entry);f.Save(file);
            check(State(FamilyProfile.Load(file))==State(f)&&!FamilyChronicle.Reconcile(f),"Chronicle read/motto/reconciliation restart stable");
            var snapshot=FamilySnapshot.Freeze(f,f.selected!);var captured=FamilySnapshot.Serialize(snapshot);
            f.chronicle.classes[cls].maxFloor=100;f.chronicle.classes[cls].wins=100;FamilyChronicle.Reconcile(f);f.chronicle.motto="";
            check(FamilySnapshot.Serialize(snapshot)==captured,"Chronicle rewards and motto frozen during climb");
        }
        foreach(var generation in new[]{0,1,2})foreach(var level in new[]{0,1,2}) {
            var f=Fresh();f.generation=generation;f.manor["vitality"]=level;FamilyChronicle.Reconcile(f);
            check(f.chronicle.unlocked.Contains("founding")&&f.chronicle.unlocked.Contains("first_return")== (generation>0)&&f.chronicle.unlocked.Contains("restoration")== (level>0),"Story thresholds below/at/above");
        }
        var max=Fresh();max.crowns=max.generation=int.MaxValue;var maxRun=max.Begin(49);path=PathFor();max.Save(path);
        check(max.CommitSettlement(maxRun,0,int.MaxValue,int.MaxValue,true,path)>0&&max.crowns==int.MaxValue&&max.generation==int.MaxValue,"Extreme settlement saturates bank/generation");
        p=Fresh();p.crowns=200;path=PathFor();p.Save(path);
        check(p.PreviewPurchases(["vitality","smith"])?.crowns==0,"Prerequisites may be purchased in order in a single queue");
        check(p.PreviewPurchases(["smith","vitality"])==null,"Wrong prerequisite order rejects entire queue");
        p.crowns=199;p.Save(path);check(!p.CommitPurchases(["vitality","smith"],path)&&p.Level("vitality")==0,"Confirmation rechecks changed crown balance atomically");
        p.crowns=200;p.Save(path);check(p.CommitPurchases(["vitality","smith"],path)&&p.crowns==0,"Exact-cost queue leaves zero crowns");
        p=Fresh();p.crowns=int.MaxValue;path=PathFor();p.Save(path);
        check(!p.CommitPurchases(Enumerable.Repeat("vitality",6).ToArray(),path)&&p.Level("vitality")==0,"Repeated IDs cannot exceed cap or partially spend");
        check(p.CommitPurchases(Enumerable.Repeat("vitality",5).ToArray(),path)&&p.Level("vitality")==5,"Repeated IDs within cap are valid staged levels");
        var orphan=Fresh();var orphanRun=orphan.Begin(49);orphanRun.floors=6;path=PathFor();orphan.Save(path);
        var replacement=FamilySnapshot.Freeze(orphan,new(){name="Selected replacement",classId="ranger",traits=[]});
        check(orphan.CommitRunStart(replacement,path)&&orphan.generation==1&&orphan.crowns==1000&&orphan.chronicle.classes["knight"].maxFloor==6&&!orphan.active!.quick&&orphan.active.generation==1&&orphan.active.heir.classId=="ranger","Replacing orphan preserves reached progress without inventing earned crowns or repeating quickstart");
        check(!orphan.CommitRunStart(replacement,path)&&orphan.generation==1,"Duplicate embark callback cannot settle the replacement");
        p=Fresh();path=PathFor();p.Save(path);var oldGeneration=p.generation;var oldBank=p.crowns;
        check(p.CommitHeirSelection(new(){name="Cancelled choice",classId="ranger",traits=[]},path)&&p.CommitHeirSelection(null,path)&&p.active==null&&p.selected==null&&p.generation==oldGeneration&&p.crowns==oldBank,"Choose/cancel never starts or settles a climb");
        check(!p.CommitChronicleRead("knight_victory",path)&&!p.CommitMotto("Unbroken Oath",path),"Locked story/motto cannot be committed");
        p=Fresh();path=PathFor();var bound=p.Begin(49);bound.floors=3;p.Save(path);
        check(p.CommitChronicleRead("founding",path)&&ReferenceEquals(p.active,bound),"Story read must preserve the native-bound active run object");
        bound.floors=6;p.Save(path);check(FamilyProfile.Load(path).active!.floors==6,"Room progress after menu transaction must reach the family save");
        foreach(var step in new[]{"after-write","after-flush","before-commit","after-commit"}) {
            var f=Fresh();var snapshot=f.Begin(49);var file=PathFor();f.Save(file);var marker=file+".ready";
            using var child=PersistenceChecks.Spawn("--lifecycle-crash",file,step,marker);
            PersistenceChecks.AwaitFile(marker,child);
            if(!string.Equals(child.MainModule!.FileName,Environment.ProcessPath,OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal))throw new Exception("Unexpected lifecycle fixture child image");
            child.Kill();child.WaitForExit();f=FamilyProfile.Load(file);
            check(f.crowns==(step=="after-commit"?1036:1000)&&f.generation==(step=="after-commit"?1:0),"Killed settlement process leaves complete old/new bank and generation");
            check(f.chronicle.settledRuns.Contains(snapshot.id)==(step=="after-commit"),"Crash cannot separate receipt from payout");
            f.CommitSettlement(snapshot,0,6,100,true,file);f=FamilyProfile.Load(file);
            check(f.crowns==1036&&f.generation==1&&f.chronicle.classes["knight"].returns==1&&f.chronicle.classes["knight"].wins==1,"Restart/replay after killed process pays exactly once");
        }
        // Formula bounds across every allowed trait pair and no/full Treasury.
        var payoutCount=0;var largestEarly=0;var largestWin=0;
        var traits=Design.Rows("traits").Where(t=>t.Flag("enabled")).ToArray();
        foreach(var first in traits)foreach(var second in traits.Where(t=>FamilyProfile.Compatible(first,t)))foreach(var treasury in new[]{0,4}) {
            var bonus=Math.Clamp(100+first.Number("goldBonus")+second.Number("goldBonus"),0,200);
            foreach(var victory in new[]{false,true}) {
                var f=Fresh();f.manor["vault"]=treasury;f.selected!.traits=[first.Text("id"),second.Text("id")];var snapshot=f.Begin(49);
                var amount=f.Settle(snapshot.id,victory?50:1,victory?200:0,victory);
                var expected=(victory?128:2)*bonus*(100+treasury*5)/10000;
                check(amount==expected,"Crown formula across compatible traits and Treasury");
                if(victory)largestWin=Math.Max(largestWin,amount);else largestEarly=Math.Max(largestEarly,amount);payoutCount++;
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(new{lifecycle="passed",modelSeeds=12,modelTransitions=1200,faultTransitions=42,chronicleThresholds=27,crashBoundaries=4,payoutCount,largestEarly,largestWin}));
    }
}
