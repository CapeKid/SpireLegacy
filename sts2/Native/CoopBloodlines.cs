using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

public static class CoopBloodlines
{
    public const int Protocol=CoopSelectionState.Protocol;
    internal static readonly CoopSelectionState Selections=new();
    public static IReadOnlyDictionary<ulong,FamilyProfile.FamilyRun> Pending=>Selections.Snapshots;
    public static readonly AsyncLocal<bool> CreatingMultiplayer=new();
    public static StartRunLobby? Lobby {get;private set;}
    private static INetGameService? registered;
    private static string challenge="",ownPayload="";
    private static int epoch,revision;
    private static readonly Dictionary<ulong,string> challenges=new();
    internal static bool TestHoldSelections;
    public static IEnumerable<object> Players(StartRunLobby lobby)=>((System.Collections.IEnumerable)AccessTools.Property(typeof(StartRunLobby),"Players").GetValue(lobby)!).Cast<object>();
    public static object Local(StartRunLobby lobby)=>AccessTools.Property(typeof(StartRunLobby),"LocalPlayer").GetValue(lobby)!;
    public static ulong Id(object player)=>(ulong)AccessTools.Field(player.GetType(),"id").GetValue(player)!;
    public static bool IsReady(object player)=>(bool)AccessTools.Field(player.GetType(),"isReady").GetValue(player)!;
    public static MegaCrit.Sts2.Core.Models.CharacterModel Character(object player)=>(MegaCrit.Sts2.Core.Models.CharacterModel)AccessTools.Field(player.GetType(),"character").GetValue(player)!;
    internal static bool NeedsSnapshot(object player)=>Character(player) is HeirCharacter or MegaCrit.Sts2.Core.Models.Characters.RandomCharacter;
    public static void Attach(StartRunLobby lobby)
    {
        EnterRun();Lobby=lobby;challenge=Guid.NewGuid().ToString();
        if(lobby.NetService.Type==NetGameType.Singleplayer)return;
        registered=lobby.NetService;registered.RegisterMessageHandler<CustomMessageWrapper>(Receive);
        if(lobby.NetService.Type==NetGameType.Host)RosterChanged(lobby);
        else RequestSession();
    }
    public static void Detach()
    {
        registered?.UnregisterMessageHandler<CustomMessageWrapper>(Receive);registered=null;
    }
    public static void EnterRun()
    {
        Detach();Lobby=null;Selections.Reset("");challenge=ownPayload="";epoch=revision=0;challenges.Clear();TestHoldSelections=false;
    }
    private static void Receive(CustomMessageWrapper message,ulong sender)=>message.Message.HandleMessage(sender);
    private static void RequestSession()
    {
        if(Lobby is {} lobby&&lobby.NetService.Type==NetGameType.Client)
            CustomMessageWrapper.Send(new BloodlineLobbyRequest{Challenge=challenge},lobby.NetService);
    }
    internal static void RosterChanged(StartRunLobby lobby)
    {
        if(!ReferenceEquals(lobby,Lobby)||lobby.NetService.Type!=NetGameType.Host)return;
        epoch=checked(epoch+1);revision=0;Selections.Reset(Guid.NewGuid().ToString());
        foreach(var owner in challenges.Keys.Where(owner=>!Players(lobby).Any(p=>Id(p)==owner)).ToArray())challenges.Remove(owner);
        Announce();Publish();
    }
    internal static void SessionRequest(int protocol,string nonce,ulong sender)
    {
        if(Lobby is not {} lobby||lobby.NetService.Type!=NetGameType.Host||protocol!=Protocol||!Guid.TryParse(nonce,out _)||!Players(lobby).Any(p=>Id(p)==sender))return;
        challenges[sender]=nonce;Announce();Publish();
    }
    private static void Announce()
    {
        if(Lobby is not {} lobby||lobby.NetService.Type!=NetGameType.Host)return;
        foreach(var peer in challenges)
            CustomMessageWrapper.Send(new BloodlineLobbySession{Session=Selections.Session,Epoch=epoch,Owner=peer.Key,Challenge=peer.Value},lobby.NetService);
    }
    internal static void SessionReceived(int protocol,string session,int newEpoch,ulong owner,string nonce,ulong sender)
    {
        if(Lobby is not {} lobby||lobby.NetService.Type!=NetGameType.Client||owner!=lobby.NetService.NetId)return;
        var host=(lobby.NetService as INetClientGameService)?.NetClient?.HostNetId;
        if(sender!=host||protocol!=Protocol||!Guid.TryParse(session,out _)||newEpoch<=0)return;
        if(nonce!=challenge){RequestSession();return;}
        if(newEpoch<epoch||newEpoch==epoch&&Selections.Session!=session)return;
        if(newEpoch>epoch){epoch=newEpoch;revision=0;Selections.Reset(session);}
        Publish();
    }
    public static void Resend()
    {
        if(Lobby?.NetService.Type==NetGameType.Client)RequestSession();else {Announce();Publish();}
    }
    private static void Publish()
    {
        if(Lobby is not {} lobby||Selections.Session==""||!Players(lobby).Any(p=>Id(p)==lobby.NetService.NetId))return;
        var ready=IsReady(Local(lobby));
        if(!ready||!NeedsSnapshot(Local(lobby)))ownPayload="";
        SendSelection(ownPayload);
    }
    private static void SendSelection(string payload)
    {
        if(Lobby is not {} lobby||Selections.Session=="")return;
        var next=checked(++revision);
        Selections.Receive(Protocol,Selections.Session,next,payload,lobby.NetService.NetId,false);
        if(!TestHoldSelections)CustomMessageWrapper.Send(new BloodlineLobbyMessage{Session=Selections.Session,Revision=next,Data=payload},lobby.NetService);
    }
    public static bool Ready(StartRunLobby lobby,bool ready)
    {
        if(lobby.NetService.Type==NetGameType.Singleplayer)return true;
        if(!ready){ownPayload="";SendSelection("");return true;}
        if(Selections.Session==""){RequestSession();ManorUi.Notice="Waiting for this lobby's heir selections to synchronize.";return false;}
        var local=Local(lobby);
        if(!NeedsSnapshot(local)){ownPayload="";SendSelection("");return true;}
        if(Runtime.Profile.active!=null){ManorUi.Notice="Resume or abandon your unfinished family climb before starting a co-op run.";ManorUi.Open();return false;}
        if(!IsReady(local)||ownPayload.Length==0)ownPayload=FamilySnapshot.Serialize(FamilySnapshot.Freeze(Runtime.Profile,Runtime.Heir,lobby.NetService.NetId,true));
        FamilySnapshot.ReadNetwork(ownPayload,lobby.NetService.NetId);
        SendSelection(ownPayload);ManorUi.Notice=null;return true;
    }
    public static void Accept(int protocol,string session,int rev,string data,ulong sender)
    {
        if(Lobby is not {} lobby||lobby.NetService.Type==NetGameType.Singleplayer||sender==lobby.NetService.NetId)return;
        if(protocol!=Protocol){ManorUi.Notice="Every player needs the same Spire Legacy co-op version.";return;}
        try {
            var peer=Players(lobby).FirstOrDefault(p=>Id(p)==sender);
            if(peer==null||!Selections.Receive(protocol,session,rev,data,sender,IsReady(peer)))return;
            AccessTools.Method(typeof(StartRunLobby),"BeginRunForAllPlayersIfAllReady").Invoke(lobby,[]);
        }catch(Exception error){GD.PushWarning("Rejected co-op heir snapshot: "+error.Message);ManorUi.Notice="A co-op heir could not synchronize. Unready and choose your heir again.";}
    }
    public static bool Complete(StartRunLobby lobby)=>lobby.NetService.Type==NetGameType.Singleplayer||Selections.Complete(Players(lobby).Where(NeedsSnapshot).Select(Id));
    internal static void CharacterChanging(StartRunLobby lobby,ulong owner,bool resolution)
    {
        if(resolution||!ReferenceEquals(lobby,Lobby)||lobby.NetService.Type==NetGameType.Singleplayer)return;
        Selections.Invalidate(owner);
        if(owner==lobby.NetService.NetId){ownPayload="";if(IsReady(Local(lobby)))lobby.SetReady(false);}
    }
}

internal static class BloodlineWire
{
    internal static string Read(PacketReader reader,int limit)
    {
        var size=reader.ReadInt();if(size<0||size>limit||(long)reader.BitPosition+(long)size*8>(long)reader.Buffer.Length*8)throw new InvalidDataException("Invalid co-op message text length.");
        var bytes=new byte[size];reader.ReadBytes(bytes,size);return new System.Text.UTF8Encoding(false,true).GetString(bytes);
    }
}
public sealed class BloodlineLobbyRequest:ICustomMessage
{
    public int Protocol=CoopBloodlines.Protocol;public string Challenge="";
    public bool ShouldBroadcast=>false;public bool ShouldBuffer=>false;
    public void Serialize(PacketWriter writer){writer.WriteInt(Protocol);writer.WriteString(Challenge);}
    public void Deserialize(PacketReader reader){Protocol=reader.ReadInt();Challenge=BloodlineWire.Read(reader,36);}
    public void HandleMessage(ulong senderId)=>CoopBloodlines.SessionRequest(Protocol,Challenge,senderId);
}
public sealed class BloodlineLobbySession:ICustomMessage
{
    public int Protocol=CoopBloodlines.Protocol,Epoch;public ulong Owner;public string Session="",Challenge="";
    public bool ShouldBroadcast=>false;public bool ShouldBuffer=>false;
    public void Serialize(PacketWriter writer){writer.WriteInt(Protocol);writer.WriteInt(Epoch);writer.WriteULong(Owner);writer.WriteString(Session);writer.WriteString(Challenge);}
    public void Deserialize(PacketReader reader){Protocol=reader.ReadInt();Epoch=reader.ReadInt();Owner=reader.ReadULong();Session=BloodlineWire.Read(reader,36);Challenge=BloodlineWire.Read(reader,36);}
    public void HandleMessage(ulong senderId)=>CoopBloodlines.SessionReceived(Protocol,Session,Epoch,Owner,Challenge,senderId);
}
public sealed class BloodlineLobbyMessage:ICustomMessage
{
    public int Protocol=CoopBloodlines.Protocol,Revision;public string Session="",Data="";
    public bool ShouldBroadcast=>true;public bool ShouldBuffer=>false;
    public void Serialize(PacketWriter writer){writer.WriteInt(Protocol);writer.WriteString(Session);writer.WriteInt(Revision);writer.WriteString(Data);}
    public void Deserialize(PacketReader reader){Protocol=reader.ReadInt();Session=BloodlineWire.Read(reader,36);Revision=reader.ReadInt();Data=BloodlineWire.Read(reader,CoopSelectionState.MaxPayloadBytes);}
    public void HandleMessage(ulong senderId)=>CoopBloodlines.Accept(Protocol,Session,Revision,Data,senderId);
}
[HarmonyPatch]
public static class AttachCoopLobby
{
    public static System.Reflection.MethodBase TargetMethod()=>AccessTools.Constructor(typeof(StartRunLobby),[typeof(MegaCrit.Sts2.Core.Runs.GameMode),typeof(INetGameService),typeof(IStartRunLobbyListener),typeof(int)]);
    public static void Postfix(StartRunLobby __instance)=>CoopBloodlines.Attach(__instance);
}
[HarmonyPatch]
public static class RotateBloodlineRoster
{
    public static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{"TryAddPlayerInFirstAvailableSlot","OnDisconnectedFromClientAsHost"}.Select(name=>AccessTools.Method(typeof(StartRunLobby),name));
    public static void Prefix(StartRunLobby __instance)=>CoopBloodlines.RosterChanged(__instance);
}
[HarmonyPatch(typeof(StartRunLobby),"ChangeCharacter")]
public static class InvalidateChangedBloodline
{
    public static void Prefix(StartRunLobby __instance,object[] __args)=>CoopBloodlines.CharacterChanging(__instance,(ulong)__args[0],__args.Length>2&&(bool)__args[2]);
}
[HarmonyPatch(typeof(StartRunLobby),"OnDisconnected")]
public static class ClearDisconnectedBloodline
{
    public static void Prefix(StartRunLobby __instance){if(ReferenceEquals(CoopBloodlines.Lobby,__instance))CoopBloodlines.EnterRun();}
}
[HarmonyPatch(typeof(StartRunLobby),nameof(StartRunLobby.SetReady))]
public static class SyncReadyHeir
{
    public static bool Prefix(StartRunLobby __instance,bool ready)=>CoopBloodlines.Ready(__instance,ready);
}
[HarmonyPatch(typeof(StartRunLobby),nameof(StartRunLobby.IsAboutToBeginGame))]
public static class RequireSyncedHeirs
{
    public static void Postfix(StartRunLobby __instance,ref bool __result)=>__result&=CoopBloodlines.Complete(__instance);
}
[HarmonyPatch(typeof(StartRunLobby),"BeginRunLocally")]
public static class ScopeCoopCreation
{
    public static void Prefix(StartRunLobby __instance,out bool __state){__state=CoopBloodlines.CreatingMultiplayer.Value;CoopBloodlines.CreatingMultiplayer.Value=__instance.NetService.Type!=NetGameType.Singleplayer;}
    public static void Finalizer(bool __state)=>CoopBloodlines.CreatingMultiplayer.Value=__state;
}
[HarmonyPatch(typeof(RunManager),"InitializeShared")]
public static class TransferCoopHandler
{
    public static void Prefix()=>CoopBloodlines.EnterRun();
}
[HarmonyPatch(typeof(StartRunLobby),nameof(StartRunLobby.CleanUp))]
public static class LeaveCoopLobby
{
    public static void Prefix(StartRunLobby __instance,bool disconnectSession)
    {
        if(disconnectSession&&ReferenceEquals(CoopBloodlines.Lobby,__instance))CoopBloodlines.EnterRun();
    }
}
