using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.AccessControl;
using System.Security.Principal;
using SpireLegacy;

public static class PersistenceChecks
{
    public static bool Child(string[] args)
    {
        if(args.Length==0||!args[0].StartsWith("--family-"))return false;
        var profile=FamilyProfile.Load(args[1]);
        if(args[0]=="--family-crash"){
            profile.crowns+=7;FamilyStore.Fault.Value=step=>{if(step==args[2]){File.WriteAllText(args[3],"ready");Thread.Sleep(Timeout.Infinite);}};profile.Save(args[1]);return true;
        }
        File.WriteAllText(args[2],"loaded");var deadline=DateTime.UtcNow.AddSeconds(20);while(!File.Exists(args[3])){if(DateTime.UtcNow>deadline)return true;Thread.Sleep(20);}
        profile.crowns+=int.Parse(args[4]);try{profile.Save(args[1]);File.WriteAllText(args[2]+".result","saved");}catch(IOException){File.WriteAllText(args[2]+".result","conflict");}return true;
    }
    private static Process Spawn(params string[] args)
    {
        var exe=Environment.ProcessPath!;var info=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true};
        if(Path.GetFileNameWithoutExtension(exe)=="dotnet")info.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
        foreach(var arg in args)info.ArgumentList.Add(arg);return Process.Start(info)!;
    }
    private static void AwaitFile(string path,Process p)
    {
        var deadline=DateTime.UtcNow.AddSeconds(20);while(!File.Exists(path)){if(p.HasExited||DateTime.UtcNow>deadline)throw new Exception("Fixture child failed before marker: "+path);Thread.Sleep(20);}
    }
    public static void Run(Action<bool,string> check,string root)
    {
        var dir=Path.Combine(root,"persistence");Directory.CreateDirectory(dir);var serial=0;
        string PathFor()=>Path.Combine(dir,"family-"+(++serial)+".json");
        string Seed(){var path=PathFor();var p=new FamilyProfile{crowns=100};FamilyChronicle.Reconcile(p);p.Save(path);p.crowns=200;p.Save(path);return path;}
        var missing=Seed();File.Delete(missing);var restored=FamilyProfile.Load(missing);check(restored.crowns==100&&File.Exists(missing),"Missing primary restores backup instead of fresh zero-crown family");
        var corrupt=Seed();var good=File.ReadAllBytes(corrupt+".bak");File.WriteAllText(corrupt,"broken");restored=FamilyProfile.Load(corrupt);
        check(restored.crowns==100&&File.ReadAllBytes(corrupt+".bak").SequenceEqual(good)&&Directory.GetFiles(dir,Path.GetFileName(corrupt)+".corrupt.*").Any(),"Recovery preserves good backup and quarantines damaged primary");
        File.WriteAllText(corrupt,"broken twice");check(FamilyProfile.Load(corrupt).crowns==100,"Second primary corruption still recovers original backup");
        var both=Seed();File.WriteAllText(both,"bad primary");File.WriteAllText(both+".bak","bad backup");try{FamilyProfile.Load(both);check(false,"Both corrupt must refuse");}catch(InvalidDataException e){check(e.InnerException is AggregateException&&File.ReadAllText(both)=="bad primary"&&File.ReadAllText(both+".bak")=="bad backup","Both corrupt preserved with aggregate diagnostics");}
        var future=Seed();var data=JsonNode.Parse(File.ReadAllText(future))!;data["schema"]=2;File.WriteAllText(future,data.ToJsonString());var futureBytes=File.ReadAllBytes(future);
        try{FamilyProfile.Load(future);check(false,"Future schema cannot silently downgrade");}catch(UnsupportedFamilySchemaException){check(File.ReadAllBytes(future).SequenceEqual(futureBytes),"Future schema refuses without backup fallback");}
        var changedSchema=Seed();File.WriteAllText(changedSchema,"{\"schema\":2,\"new_family_format\":true}");try{FamilyProfile.Load(changedSchema);check(false,"Future layout cannot fall back");}catch(UnsupportedFamilySchemaException){check(File.ReadAllText(changedSchema).Contains("new_family_format"),"Future schema checked before old required fields");}
        var futureHistory=Seed();var futureNode=JsonNode.Parse(File.ReadAllText(futureHistory))!;futureNode["chronicle"]=JsonNode.Parse("{\"schema\":2,\"classes\":[]}");File.WriteAllText(futureHistory,futureNode.ToJsonString());try{FamilyProfile.Load(futureHistory);check(false,"Future Chronicle shape cannot fall back");}catch(UnsupportedFamilySchemaException){check(File.ReadAllText(futureHistory).Contains("schema"),"Future Chronicle checked before old deserialization");}
        var mutations=new List<Action<JsonNode>>{
            n=>n["family"]=null,n=>n["familyId"]=null,n=>n["familyId"]="bad-id",n=>n["family"]=new string('x',25),n=>n["manor"]=null,n=>n["offers"]=null,n=>n["chronicle"]=null,
            n=>n["crowns"]=-1,n=>n["generation"]=-1,n=>n["lastEarned"]=-1,n=>n["banner"]=4,n=>n["manor"]!["unknown"]=1,n=>n["manor"]!["smith"]=-1,n=>n["manor"]!["smith"]=6,
            n=>n["offers"]=new JsonArray((JsonNode?)null),n=>n["selected"]=JsonNode.Parse("{\"name\":\"x\",\"classId\":\"invalid\",\"traits\":[]}"),
            n=>n["selected"]=JsonNode.Parse("{\"name\":null,\"classId\":\"mage\",\"traits\":[]}"),n=>n["selected"]=JsonNode.Parse("{\"name\":\"x\",\"classId\":\"mage\",\"traits\":null}"),
            n=>n["selected"]=JsonNode.Parse("{\"name\":\"x\",\"classId\":\"mage\",\"traits\":[\"unknown\"]}"),n=>n["selected"]=JsonNode.Parse("{\"name\":\"x\",\"classId\":\"mage\",\"traits\":[\"easybreakables\",\"easybreakables\"]}"),
            n=>n["chronicle"]!["classes"]=null,n=>n["chronicle"]!["unlocked"]=null,n=>n["chronicle"]!["read"]=null,n=>n["chronicle"]!["settledRuns"]=null,
            n=>n["chronicle"]!["unlocked"]=new JsonArray("unknown"),n=>n["chronicle"]!["read"]=new JsonArray("unknown"),n=>n["chronicle"]!["settledRuns"]=new JsonArray("invalid"),
            n=>n["chronicle"]!["classes"]!["mage"]=JsonNode.Parse("{\"returns\":-1}"),n=>n["chronicle"]!["unlocked"]=new JsonArray("founding","founding")
        };
        foreach(var mutate in mutations){var path=Seed();var node=JsonNode.Parse(File.ReadAllText(path))!;mutate(node);File.WriteAllText(path,node.ToJsonString());check(FamilyProfile.Load(path).crowns==100,"Malformed fields recover known backup");}
        foreach(var field in new[]{"generation","banner","lastEarned","offers","settledId"}){var path=Seed();var node=JsonNode.Parse(File.ReadAllText(path))!;node.AsObject().Remove(field);File.WriteAllText(path,node.ToJsonString());check(FamilyProfile.Load(path).crowns==100,"Missing progress field cannot silently default: "+field);}
        var duplicate=Seed();File.WriteAllText(duplicate,File.ReadAllText(duplicate).Replace("\"crowns\": 200","\"crowns\": 200, \"crowns\": 999"));check(FamilyProfile.Load(duplicate).crowns==100,"Duplicate object keys cannot silently overwrite counters");
        var large=Seed();File.WriteAllText(large,new string(' ',FamilyData.MaxBytes+1));check(FamilyProfile.Load(large).crowns==100,"Oversized primary bounded before parsing");
        foreach(var step in new[]{"before-write","after-write","after-flush","before-backup","before-commit","after-commit"}){
            var path=Seed();var p=FamilyProfile.Load(path);p.crowns=333;FamilyStore.Fault.Value=s=>{if(s==step)throw new IOException("Injected disk failure at "+s);};try{p.Save(path);check(false,"Injected failure must propagate");}catch(IOException){ }finally{FamilyStore.Fault.Value=null;}
            check(FamilyProfile.Load(path).crowns==(step=="after-commit"?333:200),"Fault preserves atomic old-or-new family: "+step);check(FamilyProfile.Load(path+".bak").crowns is 100 or 200,"Fault leaves readable backup: "+step);
        }
        var locked=Seed();var loaded=FamilyProfile.Load(locked);using(var handle=File.Open(locked,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){try{loaded.Save(locked);check(false,"Locked primary cannot overwrite");}catch(IOException){check(true,"Locked primary fails before touching backup");}}
        using(var handle=File.Open(locked+".bak",FileMode.Open,FileAccess.ReadWrite,FileShare.None)){loaded.crowns=800;try{loaded.Save(locked);check(false,"Locked backup cannot commit");}catch(IOException){check(File.ReadAllText(locked).Contains("200"),"Locked backup prevents primary mutation");}}
        loaded=FamilyProfile.Load(locked);
        var bank=FamilyProfile.Load(locked).crowns;File.SetAttributes(locked,FileAttributes.ReadOnly);try{loaded.Save(locked);check(false,"Read-only primary cannot overwrite");}catch(Exception error) when(error is IOException or UnauthorizedAccessException){check(true,"Read-only primary preserves progress");}finally{File.SetAttributes(locked,FileAttributes.Normal);}check(FamilyProfile.Load(locked).crowns==bank,"Read-only failure keeps primary");
        if(OperatingSystem.IsWindows()){
            var aclDir=Path.Combine(dir,"readonly-folder");Directory.CreateDirectory(aclDir);var aclPath=Path.Combine(aclDir,"family.json");var aclProfile=new FamilyProfile{crowns=901};FamilyChronicle.Reconcile(aclProfile);aclProfile.Save(aclPath);
            var info=new DirectoryInfo(aclDir);var original=new DirectorySecurity();original.SetSecurityDescriptorSddlForm(info.GetAccessControl(AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access),AccessControlSections.Access);var blocked=new DirectorySecurity();blocked.SetSecurityDescriptorSddlForm(original.GetSecurityDescriptorSddlForm(AccessControlSections.Access),AccessControlSections.Access);
            blocked.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!,FileSystemRights.WriteData|FileSystemRights.AppendData|FileSystemRights.CreateDirectories,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Deny));
            try{info.SetAccessControl(blocked);try{aclProfile.Save(aclPath);check(false,"Read-only folder cannot save");}catch(Exception e) when(e is IOException or UnauthorizedAccessException){check(File.ReadAllText(aclPath).Contains("901"),"Real read-only folder ACL preserves progress");}}finally{info.SetAccessControl(original);}
            check(FamilyProfile.Load(aclPath).crowns==901,"Read-only folder ACL restored");
        }
        var shared=Seed();var a=FamilyProfile.Load(shared);var b=FamilyProfile.Load(shared);a.crowns=444;a.Save(shared);b.crowns=555;try{b.Save(shared);check(false,"Stale profile cannot overwrite bank");}catch(IOException){check(FamilyProfile.Load(shared).crowns==444,"Revision rejects stale in-memory bank");}
        var bought=FamilyProfile.Load(shared);check(bought.CommitPurchases(["vitality"],shared),"Purchase carries revision through staged clone");bought.Save(shared);check(FamilyProfile.Load(shared).Level("vitality")==1,"Purchase updates original revision");
        var pair=Seed();var markerA=pair+".a";var markerB=pair+".b";var go=pair+".go";using(var one=Spawn("--family-writer",pair,markerA,go,"5")){
            AwaitFile(markerA,one);using var two=Spawn("--family-writer",pair,markerB,go,"9");AwaitFile(markerB,two);File.WriteAllText(go,"go");if(!one.WaitForExit(20000)||!two.WaitForExit(20000))throw new Exception("Writer fixture timed out");var outcomes=new[]{File.ReadAllText(markerA+".result"),File.ReadAllText(markerB+".result")};check(outcomes.Count(s=>s=="saved")==1&&outcomes.Count(s=>s=="conflict")==1&&FamilyProfile.Load(pair).crowns is 205 or 209,"Two processes sharing path: one save, one explicit conflict");
        }
        foreach(var step in new[]{"after-write","after-flush","before-commit","after-commit"}){
            var path=Seed();var marker=path+".ready";using var child=Spawn("--family-crash",path,step,marker);AwaitFile(marker,child);
            var image=child.MainModule!.FileName;if(!string.Equals(image,Environment.ProcessPath,OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal))throw new Exception("Unexpected fixture child image");child.Kill();child.WaitForExit();check(FamilyProfile.Load(path).crowns==(step=="after-commit"?207:200),"Actual process termination leaves old/new progress: "+step);
        }
        var legacy=Seed();var old=JsonNode.Parse(File.ReadAllText(legacy))!;old.AsObject().Remove("familyId");old.AsObject().Remove("chronicle");old.AsObject().Remove("schema");File.WriteAllText(legacy,old.ToJsonString());var migrated=FamilyProfile.Load(legacy);check(FamilyProfile.Load(legacy).familyId==migrated.familyId&&migrated.crowns==200,"Missing legacy identity persists once");
        var oldActive=Seed();var legacyProfile=FamilyProfile.Load(oldActive);legacyProfile.selected=new(){name="Legacy heir",classId="mage",traits=[]};legacyProfile.Begin(19);legacyProfile.Save(oldActive);var legacyNode=JsonNode.Parse(File.ReadAllText(oldActive))!;legacyNode.AsObject().Remove("familyId");legacyNode.AsObject().Remove("chronicle");foreach(var field in new[]{"familyId","family","generation","banner","ownerNetId","coop","motto","chronicleRelics"})legacyNode["active"]!.AsObject().Remove(field);File.WriteAllText(oldActive,legacyNode.ToJsonString());var legacyLoaded=FamilyProfile.Load(oldActive);check(legacyLoaded.active!.familyId==legacyLoaded.familyId&&legacyLoaded.active.family==legacyLoaded.family&&FamilyProfile.Load(oldActive).active!.id==legacyLoaded.active.id,"StS1-shaped active family metadata migrates without replacing heir/run");
        var emptyId=Seed();var emptyNode=JsonNode.Parse(File.ReadAllText(emptyId))!;emptyNode["familyId"]="";File.WriteAllText(emptyId,emptyNode.ToJsonString());var emptyLoaded=FamilyProfile.Load(emptyId);check(emptyLoaded.familyId==FamilyProfile.Load(emptyId).familyId,"Empty legacy identity persists once");
        var originalCopy=Seed();var copied=PathFor();File.Copy(originalCopy,copied);var imported=FamilyProfile.Load(copied);var originalFamily=FamilyProfile.Load(originalCopy);imported.crowns=654;imported.Save(copied);check(imported.familyId==originalFamily.familyId&&FamilyProfile.Load(originalCopy).crowns==200&&FamilyProfile.Load(copied).crowns==654,"Copied family retains identity with independent path revision/bank");
        var pmax=new FamilyProfile{generation=int.MaxValue,crowns=int.MaxValue,selected=new(){classId="knight"}};var run=pmax.Begin(4);pmax.Settle(run.id,1,0,false);check(pmax.generation==int.MaxValue&&pmax.crowns==int.MaxValue,"Extreme counters saturate instead of wrapping");
        var local=new FamilyProfile();var own=local.Begin(2);var foreign=new FamilyProfile();foreign.active=own;check(!FamilySnapshot.CanSettle(foreign,own,1),"Foreign solo snapshot cannot credit another bank");
        own.coop=true;check(!FamilySnapshot.CanSettle(local,own,1),"Legacy zero-owner exemption never applies to co-op");own.coop=false;
        var snap=JsonNode.Parse(FamilySnapshot.Serialize(own))!;snap["heir"]=null;try{FamilySnapshot.Read(snap.ToJsonString());check(false,"Null snapshot heir must reject");}catch(InvalidDataException){check(true,"Null snapshot is rejected before native adoption");}
        foreach(var field in new[]{"heir","bonuses"}){var node=JsonNode.Parse(FamilySnapshot.Serialize(own))!;node.AsObject().Remove(field);try{FamilySnapshot.Read(node.ToJsonString());check(false,"Missing snapshot effects cannot default");}catch(InvalidDataException){check(true,"Missing frozen field rejects: "+field);}}
        Console.WriteLine(JsonSerializer.Serialize(new{persistence="passed",root=dir,fuzzCases=mutations.Count,crashSteps=4,twoProcesses=true}));
    }
}
