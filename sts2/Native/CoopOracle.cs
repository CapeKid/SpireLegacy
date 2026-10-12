using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Called only through the opt-in owned-game bridge. Uses the game's real ENet
// handshake and character-select flow, including version/mod compatibility checks.
public static class CoopOracle
{
    private static object CreateNetworkService(Type type)
    {
        var empty=type.GetConstructor(Type.EmptyTypes);
        if(empty!=null)return empty.Invoke([]);
        var versionType=AccessTools.TypeByName("MegaCrit.Sts2.Core.Multiplayer.PeerVersionInfo");
        var version=AccessTools.Method(versionType,"LocalDefault").Invoke(null,[]);
        return Activator.CreateInstance(type,[version])!;
    }
    private static JoinFlow NewJoinFlow()
    {
        var empty=typeof(JoinFlow).GetConstructor(Type.EmptyTypes);
        if(empty!=null)return (JoinFlow)empty.Invoke([]);
        var service=CreateNetworkService(AccessTools.TypeByName("MegaCrit.Sts2.Core.Multiplayer.NetClientGameService"));
        return (JoinFlow)Activator.CreateInstance(typeof(JoinFlow),[service])!;
    }
    private static INetGameService Service(JoinFlow flow)=>(INetGameService)AccessTools.Property(typeof(JoinFlow),"NetService").GetValue(flow)!;
    public static MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.LoadRunLobby? SavedLobby;
    public static async Task<object> Resume(JsonElement request,bool host)
    {
        var tree=(SceneTree)Engine.GetMainLoop();
        var menu=NGame.Instance!.MainMenu!;
        var screen=menu.SubmenuStack.GetSubmenuType<NMultiplayerLoadGameScreen>();
        var port=(ushort)request.GetProperty("port").GetInt32();
        if(host){
            var save=MegaCrit.Sts2.Core.Saves.SaveManager.Instance.LoadAndCanonicalizeMultiplayerRunSave(1);
            if(!save.Success||save.SaveData==null)throw new InvalidDataException("No native multiplayer save.");
            var service=(NetHostGameService)CreateNetworkService(typeof(NetHostGameService));var error=service.StartENetHost(port,4);
            if(error.HasValue)throw new InvalidOperationException(error.ToString());
            screen.InitializeAsHost(service,save.SaveData);
        }else{
            var flow=NewJoinFlow();
            var join=await flow.Begin(new ENetClientConnectionInitializer(request.TryGetProperty("peer",out var resumePeer)?resumePeer.GetUInt64():2,"127.0.0.1",port),tree);
            if(!join.loadJoinResponse.HasValue)throw new InvalidOperationException("Expected native saved lobby.");
            screen.InitializeAsClient(Service(flow),join.loadJoinResponse.Value);
        }
        SavedLobby=(MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.LoadRunLobby)AccessTools.Field(typeof(NMultiplayerLoadGameScreen),"_runLobby").GetValue(screen)!;
        menu.SubmenuStack.Push(screen);
        return new{loading=true};
    }
    public static object Vote()
    {
        var run=(RunState)AccessTools.Property(typeof(RunManager),"State").GetValue(RunManager.Instance)!;
        var next=run.CurrentMapPoint?.Children.FirstOrDefault()??run.Map.StartingMapPoint;
        var vote=new MapVote{coord=next.coord,mapGenerationCount=RunManager.Instance.MapSelectionSynchronizer.MapGenerationCount};
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new MegaCrit.Sts2.Core.GameActions.VoteForMapCoordAction(PlayerBloodlines.LocalPlayer!,run.MapLocation,vote));
        return new{voted=next.coord.ToString()};
    }
    public static async Task<object> Play(bool upgraded=false)
    {
        var player=PlayerBloodlines.LocalPlayer!;
        var combat=player.Creature.CombatState!;
        var card=player.PlayerCombatState!.Hand.Cards.Where(c=>c.CanPlay()).OrderBy(c=>c.Type==MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack?1:0).First();
        if(upgraded&&!card.IsUpgraded)throw new InvalidDataException("Apply the fixture upgrade on every peer before playing.");
        var energy=player.PlayerCombatState.Energy;var cost=card.EnergyCost.GetWithModifiers(MegaCrit.Sts2.Core.Entities.Cards.CostModifiers.All);
        var target=card.TargetType==MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy?combat.GetOpponentsOf(player.Creature).First(c=>c.IsAlive):null;
        if(!card.TryManualPlay(target))throw new InvalidDataException("Native co-op manual play refused "+card.Id);
        var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+15000;
        while(card.Pile?.Type is MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand or MegaCrit.Sts2.Core.Entities.Cards.PileType.Play){
            if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Native co-op card did not resolve: "+card.Id);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        deadline=Time.GetTicksMsec()+2500;
        while(MegaCrit.Sts2.Core.Nodes.Cards.NCard.FindOnTable(card)!=null){
            if(Time.GetTicksMsec()>deadline)throw new InvalidDataException("Native co-op card remains on the table: "+card.Id);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        deadline=Time.GetTicksMsec()+15000;
        while(RunManager.Instance.ActionExecutor.IsRunning){
            if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Card action did not finish: "+card.Id);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        return new{played=true,card=card.Id.ToString(),upgraded=card.IsUpgraded,cost,energyBefore=energy,energyAfter=player.PlayerCombatState.Energy,pile=card.Pile?.Type.ToString(),tableNodePresent=false};
    }
    public static object RoundTrip()
    {
        var run=(RunState)AccessTools.Property(typeof(RunManager),"State").GetValue(RunManager.Instance)!;
        var checks=new List<object>();
        foreach(var player in run.Players){
            var data=player.ToSerializable();
            var restored=Player.FromSerializable(data);
            if(player.Character is not HeirCharacter){if(restored.Character.Id!=player.Character.Id||PlayerBloodlines.State(restored)!=null)throw new InvalidDataException("Native player inherited a bloodline.");checks.Add(new{player.NetId,native=true});continue;}
            var expected=FamilySnapshot.Serialize(PlayerBloodlines.State(player)!);
            if(FamilySnapshot.Serialize(PlayerBloodlines.State(restored)!)!=expected)throw new InvalidDataException("Owner snapshot changed during native player serialization.");
            var expectedTraits=JsonSerializer.Serialize(PlayerBloodlines.Heir(player).traits);
            if(restored.Deck.Cards.OfType<LegacyCard>().Any(c=>c.TraitSnapshot!=expectedTraits))throw new InvalidDataException("A restored card inherited another player's traits.");
            var sample=(LegacyCard)NativeCards.Get(PlayerBloodlines.Heir(player).classId+"_strike").ToMutable();
            sample.Owner=player;
            if(sample.TraitSnapshot!=expectedTraits||sample.Pool!=player.Character.CardPool)throw new InvalidDataException("New card ownership did not capture the correct bloodline.");
            checks.Add(new{player.NetId,restored=PlayerBloodlines.Heir(restored).classId,pool=restored.Character.CardPool.Id.ToString(),traits=PlayerBloodlines.Heir(restored).traits,cards=restored.Deck.Cards.Count});
        }
        return checks;
    }
    public static object UpgradeOwnedCard(ulong owner)
    {
        var run=(RunState)AccessTools.Property(typeof(RunManager),"State").GetValue(RunManager.Instance)!;
        var player=run.Players.Single(p=>p.NetId==owner);
        var card=player.PlayerCombatState!.Hand.Cards.Where(c=>c.CanPlay()).OrderBy(c=>c.Type==MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack?1:0).First();
        MegaCrit.Sts2.Core.Commands.CardCmd.Upgrade(card);
        return new{owner,card=card.Id.ToString(),index=player.PlayerCombatState.Hand.Cards.ToList().IndexOf(card),upgraded=card.IsUpgraded};
    }
    public static async Task<object> Lobby(JsonElement request,bool host)
    {
        if(NRun.Instance!=null)throw new InvalidOperationException("Return to the menu before creating a lab lobby.");
        if(Runtime.Profile.active!=null)throw new InvalidOperationException("Lab family has an unfinished run.");
        var choice=request.GetProperty("class").GetString()!;
        var peer=host?1UL:request.TryGetProperty("peer",out var requestedPeer)?requestedPeer.GetUInt64():2UL;
        Runtime.Profile.selected=new FamilyProfile.Heir{name=host?"Host heir":"Guest heir "+peer,classId=choice is "ironclad" or "random"?request.TryGetProperty("heirClass",out var frozenClass)?frozenClass.GetString()!:"knight":choice,traits=request.TryGetProperty("traits",out var traits)?traits.EnumerateArray().Select(t=>t.GetString()!).ToList():[]};
        Runtime.Profile.family=host?"Host family":"Guest family";
        Runtime.Profile.banner=(int)(peer%4);
        Runtime.Profile.generation=host?2:4;
        if(request.TryGetProperty("manor",out var manor))Runtime.Profile.manor=manor.EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value.GetInt32());
        Runtime.Profile.crowns=host?123:234;
        if(request.TryGetProperty("lesson",out var lesson)){
            var cls=lesson.GetString()!;Runtime.Profile.chronicle.classes[cls]=new(){returns=1,maxFloor=6};
        }
        FamilyChronicle.Reconcile(Runtime.Profile);
        Runtime.Profile.Save(Runtime.FamilyPath);
        foreach(var key in new[]{"accept_tutorials_ftue","map_select_ftue","combat_rules_ftue","shuffle_ftue","multiplayer_ftue"})MegaCrit.Sts2.Core.Saves.SaveManager.Instance.MarkFtueAsComplete(key);
        var tree=(SceneTree)Engine.GetMainLoop();
        var menu=NGame.Instance!.MainMenu!;
        var screen=menu.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
        var port=(ushort)request.GetProperty("port").GetInt32();
        if(host) {
            var service=(NetHostGameService)CreateNetworkService(typeof(NetHostGameService));
            var error=service.StartENetHost(port,4);
            if(error.HasValue)throw new InvalidOperationException(error.ToString());
            screen.InitializeMultiplayerAsHost(service,4);
        }else {
            var flow=NewJoinFlow();
            var join=await flow.Begin(new ENetClientConnectionInitializer(peer,"127.0.0.1",port),tree);
            if(!join.joinResponse.HasValue)throw new InvalidOperationException("Expected a native lobby join response.");
            screen.InitializeMultiplayerAsClient(Service(flow),join.joinResponse.Value);
        }
        menu.SubmenuStack.Push(screen);
        var button=NativeTestBridge.Descendants(screen).OfType<NCharacterSelectButton>().Single(b=>choice=="ironclad"?b.Character is MegaCrit.Sts2.Core.Models.Characters.Ironclad:choice=="random"?b.Character is MegaCrit.Sts2.Core.Models.Characters.RandomCharacter:b.Character is HeirCharacter);
        screen.SelectCharacter(button,button.Character);
        return State();
    }
    public static object State()
    {
        var run=AccessTools.Property(typeof(RunManager),"State").GetValue(RunManager.Instance) as RunState;
        return new{local=run==null?CoopBloodlines.Lobby?.NetService.NetId:RunManager.Instance.NetService.NetId,
            paused=((SceneTree)Engine.GetMainLoop()).Paused,pending=CoopBloodlines.Pending.Keys.ToArray(),
            actionRunning=run!=null&&RunManager.Instance.ActionExecutor.IsRunning,
            session=CoopBloodlines.Selections.Session,protocol=CoopBloodlines.Protocol,
            family=Runtime.Profile.family,familyId=Runtime.Profile.familyId,crowns=Runtime.Profile.crowns,active=Runtime.Profile.active?.id,
            lobby=run==null&&CoopBloodlines.Lobby is {} lobby?CoopBloodlines.Players(lobby).Select(p=>new{id=CoopBloodlines.Id(p),character=CoopBloodlines.Character(p).Id.ToString(),ready=CoopBloodlines.IsReady(p),snapshot=CoopBloodlines.Pending.GetValueOrDefault(CoopBloodlines.Id(p))?.heir.classId}).ToArray():null,
            floor=run?.TotalFloor,room=run?.CurrentRoom?.GetType().Name,
            players=run?.Players.Select(p=>new{p.NetId,character=p.Character.Id.ToString(),heir=JsonSerializer.SerializeToElement(PlayerBloodlines.Heir(p),FamilyProfile.JsonOptions),snapshot=PlayerBloodlines.State(p)==null?(JsonElement?)null:JsonSerializer.SerializeToElement(PlayerBloodlines.State(p),FamilyProfile.JsonOptions),hp=p.Creature.CurrentHp,maxHp=p.Creature.MaxHp,block=p.Creature.Block,energy=p.PlayerCombatState?.Energy,phase=p.PlayerCombatState?.Phase.ToString(),pool=p.Character.CardPool.Id.ToString(),deck=p.Deck.Cards.Select(c=>c.Id.ToString()).ToArray(),hand=p.PlayerCombatState?.Hand.Cards.Select(c=>c.Id.ToString()).ToArray(),powers=p.Creature.Powers.Select(power=>new{id=power.Id.ToString(),power.Amount,charges=power is P_class cp?cp.Charges:0}).ToArray()}).ToArray()};
    }
}
