namespace SpireLegacy;

// Transport-independent selection ledger. A host-issued lobby incarnation and
// monotonic owner revision prevent old packets from becoming new selections.
public sealed class CoopSelectionState
{
    public const int Protocol=2, MaxPeers=4, MaxPayloadBytes=8192;
    public string Session {get;private set;}="";
    private sealed record Entry(int Revision,string Payload);
    private readonly Dictionary<ulong,Entry> entries=new();
    public IReadOnlyDictionary<ulong,FamilyProfile.FamilyRun> Snapshots=>entries.Where(e=>e.Value.Payload.Length>0)
        .ToDictionary(e=>e.Key,e=>FamilySnapshot.Read(e.Value.Payload));
    public int Revision(ulong owner)=>entries.GetValueOrDefault(owner)?.Revision??0;
    public void Invalidate(ulong owner){if(entries.TryGetValue(owner,out var entry))entries[owner]=entry with {Payload=""};}
    public void Reset(string session)
    {
        if(session!=""&&!Guid.TryParse(session,out _))throw new InvalidDataException("Invalid co-op lobby incarnation.");
        Session=session;entries.Clear();
    }
    public bool Receive(int protocol,string session,int revision,string payload,ulong sender,bool nativeReady)
    {
        if(protocol!=Protocol||Session==""||session!=Session||revision<=0||sender==0)return false;
        if(System.Text.Encoding.UTF8.GetByteCount(payload)>MaxPayloadBytes)throw new InvalidDataException("Co-op selection payload exceeds limit.");
        var prior=entries.GetValueOrDefault(sender);
        if(prior!=null&&revision<=prior.Revision)return revision==prior.Revision&&payload==prior.Payload;
        if(prior==null&&entries.Count>=MaxPeers)return false;
        var canonical=payload.Length==0?"":FamilySnapshot.Serialize(FamilySnapshot.ReadNetwork(payload,sender));
        // Unready is a revocation even if the native unready packet is delayed.
        // A frozen ready selection cannot otherwise change in place.
        if(prior!=null&&nativeReady&&prior.Payload.Length>0&&canonical.Length>0&&canonical!=prior.Payload)return false;
        entries[sender]=new(revision,canonical);return true;
    }
    public bool Complete(IEnumerable<ulong> required)=>required.All(owner=>entries.TryGetValue(owner,out var entry)&&entry.Payload.Length>0);
}
