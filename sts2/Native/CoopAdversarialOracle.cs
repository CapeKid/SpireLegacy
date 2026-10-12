using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Commands;

namespace SpireLegacy;

// Only dispatched by the existing opt-in, isolated-profile bridge.
public static class CoopAdversarialOracle
{
    private static (int protocol,string session,int revision,string payload,ulong owner)? oldPacket;
    private static void Require(bool value,string text){if(!value)throw new InvalidDataException(text);}
    public static object LobbyChecks()
    {
        var lobby=CoopBloodlines.Lobby!;
        var owner=CoopBloodlines.Players(lobby).Where(p=>CoopBloodlines.Id(p)!=lobby.NetService.NetId&&CoopBloodlines.IsReady(p))
            .Select(CoopBloodlines.Id).First(id=>CoopBloodlines.Pending.ContainsKey(id));
        var original=FamilySnapshot.Serialize(CoopBloodlines.Pending[owner]);
        // Reflection permits this minimized probe to compile against the original
        // baseline API as well, proving the old same-ID mutation.
        var accept=AccessTools.Method(typeof(CoopBloodlines),nameof(CoopBloodlines.Accept));
        var ledger=AccessTools.Field(typeof(CoopBloodlines),"Selections")?.GetValue(null);
        var session=ledger==null?"":(string)AccessTools.Property(ledger.GetType(),"Session").GetValue(ledger)!;
        var rev=ledger==null?0:(int)AccessTools.Method(ledger.GetType(),"Revision").Invoke(ledger,[owner])!;
        oldPacket=(CoopBloodlines.Protocol,session,rev,original,owner);
        void Inject(int protocol,string data,string? incarnation=null) {
            accept.Invoke(null,accept.GetParameters().Length==3?[protocol,data,owner]:[protocol,incarnation??session,rev+1,data,owner]);
        }
        var changed=FamilySnapshot.Read(original);changed.heir.classId=changed.heir.classId=="ranger"?"mage":"ranger";
        Inject(CoopBloodlines.Protocol,FamilySnapshot.Serialize(changed));
        var immutable=FamilySnapshot.Serialize(CoopBloodlines.Pending[owner])==original;
        if(!immutable)Inject(CoopBloodlines.Protocol,original);
        var node=JsonNode.Parse(original)!;node.AsObject().Remove("generation");
        Inject(CoopBloodlines.Protocol,node.ToJsonString());
        var completeFields=FamilySnapshot.Serialize(CoopBloodlines.Pending[owner])==original;
        if(!completeFields)Inject(CoopBloodlines.Protocol,original);
        Inject(-1,original);Inject(CoopBloodlines.Protocol,new string('x',8193));
        if(ledger!=null)Inject(CoopBloodlines.Protocol,original,Guid.NewGuid().ToString());
        Require(FamilySnapshot.Serialize(CoopBloodlines.Pending[owner])==original,"Bad lobby packet changed owned snapshot.");
        return new{owner,immutableReady=immutable,requiredFrozenFields=completeFields,badPacketsStable=true};
    }
    public static object Replay()
    {
        var cached=oldPacket??throw new InvalidOperationException("Capture a lobby packet first.");
        var before=JsonSerializer.Serialize(CoopBloodlines.Pending.Select(p=>new{p.Key,value=FamilySnapshot.Serialize(p.Value)}));
        CoopBloodlines.Accept(cached.protocol,cached.session,cached.revision,cached.payload,cached.owner);
        Require(before==JsonSerializer.Serialize(CoopBloodlines.Pending.Select(p=>new{p.Key,value=FamilySnapshot.Serialize(p.Value)})),"Old lobby selection replay changed state.");
        return new{replayRejected=true};
    }
    public static object Scopes()=>new{creating=PlayerBloodlines.Creating.Value!=null,multiplayer=CoopBloodlines.CreatingMultiplayer.Value,eligibility=PlayerBloodlines.EligibilityOwner.Value!=null,healing=InheritedHealing.StartingHp.Value!=null,lobby=CoopBloodlines.Lobby!=null,handler=AccessTools.Field(typeof(CoopBloodlines),"registered").GetValue(null)!=null,pending=CoopBloodlines.Pending.Count};
    public static object Wire()
    {
        var rejected=0;
        foreach(var size in new[]{-1,8193,int.MaxValue,12}) {
            var writer=new PacketWriter();writer.WriteInt(size);
            var reader=new PacketReader();reader.Reset(writer.Buffer[..writer.BytePosition]);
            try{BloodlineWire.Read(reader,8192);throw new Exception("Malformed wire length passed");}
            catch(InvalidDataException){rejected++;}
        }
        var message=new BloodlineLobbyMessage{Session=Guid.NewGuid().ToString(),Revision=7,Data="unicode: é"};
        var packet=new PacketWriter();message.Serialize(packet);var input=new PacketReader();input.Reset(packet.Buffer[..packet.BytePosition]);
        var restored=new BloodlineLobbyMessage();restored.Deserialize(input);
        Require(message.Protocol==restored.Protocol&&message.Session==restored.Session&&message.Revision==restored.Revision&&message.Data==restored.Data,"Native wire roundtrip failed.");
        return new{boundedLengthFailures=rejected,utf8Roundtrip=true};
    }
    public static object SavedOwners(RunState state)
    {
        var players=state.Players.Where(p=>p.Character is HeirCharacter).ToArray();Require(players.Length>=2,"Two Heirs required.");
        var source=players[0];var target=players[1];var original=PlayerBloodlines.State(target)!;var text=FamilySnapshot.Serialize(original);
        var creating=PlayerBloodlines.Creating.Value;var scope=CoopBloodlines.CreatingMultiplayer.Value;var eligibility=PlayerBloodlines.EligibilityOwner.Value;
        var rejected=0;
        var save=source.ToSerializable();save.NetId=target.NetId;
        try{Player.FromSerializable(save);throw new Exception("Wrong-owner player save accepted");}catch(InvalidDataException){rejected++;}
        save=source.ToSerializable();
        try{target.SyncWithSerializedPlayer(save);throw new Exception("Wrong-target sync accepted");}catch(InvalidDataException){rejected++;}
        save=target.ToSerializable();var carrier=save.Relics.Single(r=>r.Id==ModelDb.Relic<BloodlineRelic>().Id);
        var changed=FamilySnapshot.Read(text);changed.heir.classId=changed.heir.classId=="mage"?"ranger":"mage";
        var prop=carrier.Props!.strings!.FindIndex(p=>p.name==nameof(BloodlineRelic.FamilyRunSnapshot));
        carrier.Props.strings[prop]=new(){name=nameof(BloodlineRelic.FamilyRunSnapshot),value=FamilySnapshot.Serialize(changed)};
        try{target.SyncWithSerializedPlayer(save);throw new Exception("Frozen inheritance changed on sync");}catch(InvalidDataException){rejected++;}
        Require(FamilySnapshot.Serialize(PlayerBloodlines.State(target)!)==text&&ReferenceEquals(original,PlayerBloodlines.State(target)),"Rejected sync changed player.");
        Require(PlayerBloodlines.Creating.Value==creating&&CoopBloodlines.CreatingMultiplayer.Value==scope&&PlayerBloodlines.EligibilityOwner.Value==eligibility,"Exception leaked owner context.");
        return new{ownerRefusals=rejected,playerUnchanged=true,contextsRestored=true};
    }
    public static async Task<object> Revival(RunState state)
    {
        var results=new List<object>();
        foreach(var player in state.Players.Where(p=>p.Character is HeirCharacter)) {
            var hp=player.Creature.CurrentHp;var before=InheritedHealing.StartingHp.Value;
            try {
                player.Creature.SetCurrentHpInternal(0);await player.ReviveBeforeCombatEnd();
                Require(player.Creature.CurrentHp==1,"Required co-op revival was suppressed or multiplied.");
                Require(InheritedHealing.StartingHp.Value==before,"Revival leaked scoped healing bypass.");
                results.Add(new{player.NetId,traits=PlayerBloodlines.Heir(player).traits.ToArray(),revivedHp=player.Creature.CurrentHp});
            }finally{player.Creature.SetCurrentHpInternal(hp);}
        }
        return results;
    }
    public static object RandomHeirSeed()
    {
        var lobby=CoopBloodlines.Lobby!;
        var unlock=(MegaCrit.Sts2.Core.Unlocks.UnlockState)AccessTools.Method(typeof(MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby),"GetUnlockState").Invoke(lobby,[])!;
        for(var i=0;i<1000;i++) {
            var seed="COOP-AUDIT-"+i;var rng=new MegaCrit.Sts2.Core.Random.Rng((uint)MegaCrit.Sts2.Core.Helpers.StringHelper.GetDeterministicHashCode(seed),"act_selection");
            MegaCrit.Sts2.Core.Models.ActModel.GetRandomList(rng,unlock,true);
            var resolutions=CoopBloodlines.Players(lobby).Where(p=>CoopBloodlines.Character(p) is MegaCrit.Sts2.Core.Models.Characters.RandomCharacter).Select(p=>rng.NextItem(ModelDb.AllCharacters)).ToArray();
            if(resolutions.Length>0&&resolutions.All(p=>p is HeirCharacter)){lobby.SetSeed(seed);return new{seed,resolvedHeirs=resolutions.Length};}
        }
        throw new InvalidDataException("No native random-Heir seed found.");
    }
}
