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
    public const int Protocol=1;
    public static readonly Dictionary<ulong,FamilyProfile.FamilyRun> Pending=new();
    public static readonly AsyncLocal<bool> CreatingMultiplayer=new();
    public static StartRunLobby? Lobby {get;private set;}
    private static INetGameService? registered;
    // LobbyPlayer moved namespaces on public-beta. Keep it out of our assembly
    // signatures so one release DLL can load on both native branches.
    public static IEnumerable<object> Players(StartRunLobby lobby)=>((System.Collections.IEnumerable)AccessTools.Property(typeof(StartRunLobby),"Players").GetValue(lobby)!).Cast<object>();
    public static object Local(StartRunLobby lobby)=>AccessTools.Property(typeof(StartRunLobby),"LocalPlayer").GetValue(lobby)!;
    public static ulong Id(object player)=>(ulong)AccessTools.Field(player.GetType(),"id").GetValue(player)!;
    public static bool IsReady(object player)=>(bool)AccessTools.Field(player.GetType(),"isReady").GetValue(player)!;
    public static MegaCrit.Sts2.Core.Models.CharacterModel Character(object player)=>(MegaCrit.Sts2.Core.Models.CharacterModel)AccessTools.Field(player.GetType(),"character").GetValue(player)!;
    public static void Attach(StartRunLobby lobby)
    {
        Detach();Pending.Clear();Lobby=lobby;
        if(lobby.NetService.Type==NetGameType.Singleplayer)return;
        registered=lobby.NetService;
        registered.RegisterMessageHandler<CustomMessageWrapper>(Receive);
        if(lobby.NetService.Type==NetGameType.Client)
            CustomMessageWrapper.Send(new BloodlineLobbyRequest(),lobby.NetService);
    }
    public static void Detach()
    {
        registered?.UnregisterMessageHandler<CustomMessageWrapper>(Receive);registered=null;
    }
    public static void EnterRun(){Detach();Lobby=null;}
    private static void Receive(CustomMessageWrapper message,ulong sender)=>message.Message.HandleMessage(sender);
    public static void Resend()
    {
        if(Lobby is {} lobby&&Pending.TryGetValue(lobby.NetService.NetId,out var snapshot))
            CustomMessageWrapper.Send(new BloodlineLobbyMessage{Data=FamilySnapshot.Serialize(snapshot)},lobby.NetService);
    }
    public static bool Ready(StartRunLobby lobby,bool ready)
    {
        if(lobby.NetService.Type==NetGameType.Singleplayer||!ready)return true;
        if(Runtime.Profile.active!=null&&Character(Local(lobby)) is HeirCharacter or MegaCrit.Sts2.Core.Models.Characters.RandomCharacter) {
            ManorUi.Notice="Resume or abandon your unfinished family climb before starting a co-op run.";
            ManorUi.Open();return false;
        }
        var snapshot=FamilySnapshot.Freeze(Runtime.Profile,Runtime.Heir,lobby.NetService.NetId,coop:true);
        // Validate our own data by the same rules peers enforce before marking ready.
        snapshot=FamilySnapshot.ReadNetwork(FamilySnapshot.Serialize(snapshot),lobby.NetService.NetId);
        Pending[lobby.NetService.NetId]=snapshot;
        CustomMessageWrapper.Send(new BloodlineLobbyMessage{Data=FamilySnapshot.Serialize(snapshot)},lobby.NetService);
        ManorUi.Notice=null;return true;
    }
    public static void Accept(int protocol,string data,ulong sender)
    {
        if(Lobby is not {} lobby||lobby.NetService.Type==NetGameType.Singleplayer)return;
        if(protocol!=Protocol) {ManorUi.Notice="Every player needs the same Spire Legacy co-op version.";return;}
        // Reliable snapshot packets can precede the client's initial lobby roster.
        // The native transport supplies the authenticated sender ID; retain the
        // snapshot until the roster arrives, then only embark with roster members.
        if(sender==lobby.NetService.NetId)return;
        try {
            var snapshot=FamilySnapshot.ReadNetwork(data,sender);
            var player=Players(lobby).FirstOrDefault(p=>Id(p)==sender);
            if(player!=null&&IsReady(player)&&Pending.TryGetValue(sender,out var prior)&&prior.id!=snapshot.id)return;
            Pending[sender]=snapshot;
            AccessTools.Method(typeof(StartRunLobby),"BeginRunForAllPlayersIfAllReady").Invoke(lobby,[]);
        }catch(Exception error){GD.PushWarning("Rejected co-op heir snapshot: "+error.Message);ManorUi.Notice="A co-op heir could not synchronize. Unready and choose your heir again.";}
    }
    public static bool Complete(StartRunLobby lobby)=>lobby.NetService.Type==NetGameType.Singleplayer||Players(lobby).Where(p=>Character(p) is HeirCharacter).All(p=>Pending.ContainsKey(Id(p)));
}

public sealed class BloodlineLobbyRequest:ICustomMessage
{
    public bool ShouldBroadcast=>true;
    public bool ShouldBuffer=>false;
    public void Serialize(PacketWriter writer){}
    public void Deserialize(PacketReader reader){}
    public void HandleMessage(ulong senderId)=>CoopBloodlines.Resend();
}

public sealed class BloodlineLobbyMessage:ICustomMessage
{
    public int Protocol=CoopBloodlines.Protocol;
    public string Data="";
    public bool ShouldBroadcast=>true;
    public bool ShouldBuffer=>false;
    public void Serialize(PacketWriter writer){writer.WriteInt(Protocol);writer.WriteString(Data);}
    public void Deserialize(PacketReader reader){Protocol=reader.ReadInt();Data=reader.ReadString();}
    public void HandleMessage(ulong senderId)=>CoopBloodlines.Accept(Protocol,Data,senderId);
}

[HarmonyPatch]
public static class AttachCoopLobby
{
    public static System.Reflection.MethodBase TargetMethod()=>AccessTools.Constructor(typeof(StartRunLobby),[typeof(MegaCrit.Sts2.Core.Runs.GameMode),typeof(INetGameService),typeof(IStartRunLobbyListener),typeof(int)]);
    public static void Postfix(StartRunLobby __instance)=>CoopBloodlines.Attach(__instance);
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
        if(disconnectSession&&ReferenceEquals(CoopBloodlines.Lobby,__instance)){
            CoopBloodlines.EnterRun();CoopBloodlines.Pending.Clear();
        }
    }
}
