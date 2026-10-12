using System.Security.Cryptography;
using System.Text.Json;

namespace SpireLegacy;

// A cooperating-process lock plus optimistic revision prevents stale bank overwrites.
public static class FamilyStore
{
    internal static readonly AsyncLocal<Action<string>?> Fault=new();
    private static void Point(string step)=>Fault.Value?.Invoke(step);
    private static string? Revision(string path){if(!File.Exists(path))return null;var info=new FileInfo(path);if(info.Length>FamilyData.MaxBytes)return "oversize:"+info.Length+":"+info.LastWriteTimeUtc.Ticks;using var stream=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(stream));}
    private static FileStream Lock(string path)=>new(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
    private static string ReadText(string path)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(stream.Length>FamilyData.MaxBytes)throw new InvalidDataException("Family file exceeds 8 MiB: "+path);
        using var reader=new StreamReader(stream);return reader.ReadToEnd();
    }
    private static FamilyProfile Read(string path)
    {
        var text=ReadText(path);using var doc=FamilyData.Document(text);
        if(!doc.RootElement.TryGetProperty("family",out _)||!doc.RootElement.TryGetProperty("crowns",out _)||!doc.RootElement.TryGetProperty("manor",out _))throw new InvalidDataException("Not a recognized family save: "+path);
        FamilyData.Required(doc.RootElement,"generation","banner","lastEarned","offers","settledId");
        if(doc.RootElement.GetProperty("offers").ValueKind==JsonValueKind.Array)foreach(var heir in doc.RootElement.GetProperty("offers").EnumerateArray())FamilyData.HeirFields(heir);
        if(doc.RootElement.TryGetProperty("selected",out var selected)&&selected.ValueKind!=JsonValueKind.Null)FamilyData.HeirFields(selected);
        if(doc.RootElement.TryGetProperty("active",out var run)&&run.ValueKind!=JsonValueKind.Null){FamilyData.Required(run,"id","heir","bonuses");FamilyData.HeirFields(run.GetProperty("heir"));}
        var p=JsonSerializer.Deserialize<FamilyProfile>(text,FamilyProfile.JsonOptions)??throw new InvalidDataException("Missing family.");
        // Missing fields are legacy migrations; explicit nulls remain malformed.
        if(!doc.RootElement.TryGetProperty("familyId",out _))p.familyId="";
        if(!doc.RootElement.TryGetProperty("chronicle",out _))p.chronicle=new();
        if(p.familyId=="")p.familyId=Guid.NewGuid().ToString();
        if(p.active!=null&&doc.RootElement.TryGetProperty("active",out var active)&&active.ValueKind==JsonValueKind.Object){
            if(!active.TryGetProperty("familyId",out _))p.active.familyId=p.familyId;
            if(!active.TryGetProperty("family",out _))p.active.family=p.family;
            if(!active.TryGetProperty("generation",out _))p.active.generation=p.generation;
            if(!active.TryGetProperty("banner",out _))p.active.banner=p.banner;
        }
        FamilyData.Profile(p);return p;
    }
    public static FamilyProfile Load(string requested)
    {
        var path=Path.GetFullPath(requested);if(Directory.Exists(path))throw new IOException("Family file path is a directory: "+path);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var lease=Lock(path);var revision=Revision(path);FamilyProfile p;bool recovered=false;
        if(!File.Exists(path)&&!File.Exists(path+".bak")){p=new();FamilyChronicle.Reconcile(p);p.Track(path,null);return p;}
        try{p=Read(path);}
        catch(Exception first) when(first is JsonException or InvalidDataException or FileNotFoundException){
            try{p=Read(path+".bak");recovered=true;}
            catch(Exception second) when(second is JsonException or InvalidDataException or FileNotFoundException){throw new InvalidDataException("Cannot read family or backup at "+path+"; both are preserved. Restore a valid family backup before starting.",new AggregateException(first,second));}
        }
        p.Track(path,revision);var changed=p.RefundRetiredLevels();changed|=FamilyChronicle.Reconcile(p);changed|=p.schema==0;p.schema=1;
        // Re-serialize detects legacy absent fields without inventing lost identity twice.
        if(!recovered){using var original=FamilyData.Document(ReadText(path));changed|=!original.RootElement.TryGetProperty("familyId",out var identity)||identity.GetString()==""||!original.RootElement.TryGetProperty("chronicle",out _);if(p.active!=null&&original.RootElement.GetProperty("active").ValueKind==JsonValueKind.Object)changed|=!original.RootElement.GetProperty("active").TryGetProperty("familyId",out _);}
        if(recovered||changed)Write(p,path,!recovered);
        if(recovered)p.RecoveryNotice="Family restored from its valid backup. Any damaged primary was preserved beside it.";
        return p;
    }
    public static void Save(FamilyProfile p,string requested)
    {
        var path=Path.GetFullPath(requested);if(Directory.Exists(path))throw new IOException("Family file path is a directory: "+path);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try{using var lease=Lock(path);Write(p,path,true);}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){throw new IOException("Cannot complete family save at "+path+". Close other game instances and check folder permissions, then reload before retrying.",error);}
    }
    private static void Write(FamilyProfile p,string path,bool rotate)
    {
        FamilyData.Profile(p);var current=Revision(path);
        if(!p.Expected(path,current))throw new IOException("Family changed in another process. Close the other game and reload the family before saving; no progress was overwritten.");
        var bytes=JsonSerializer.SerializeToUtf8Bytes(p,FamilyProfile.JsonOptions);if(bytes.Length>FamilyData.MaxBytes)throw new InvalidDataException("Family exceeds 8 MiB; files preserved.");
        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";string? backupTemp=null;
        try{
            Point("before-write");using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes);Point("after-write");stream.Flush(true);Point("after-flush");}
            Point("before-backup");
            if(current!=null){
                if(rotate){Read(path);backupTemp=path+"."+Guid.NewGuid().ToString("N")+".backup.tmp";using(var source=File.OpenRead(path))using(var dest=new FileStream(backupTemp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){source.CopyTo(dest);dest.Flush(true);}File.Move(backupTemp,path+".bak",true);backupTemp=null;}
                else File.Move(path,path+".corrupt."+Guid.NewGuid().ToString("N"));
            }
            Point("before-commit");File.Move(temp,path,true);p.Track(path,Convert.ToHexString(SHA256.HashData(bytes)));Point("after-commit");
        }finally{if(File.Exists(temp))File.Delete(temp);if(backupTemp!=null&&File.Exists(backupTemp))File.Delete(backupTemp);}
    }
}
