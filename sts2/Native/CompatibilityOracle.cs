using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace SpireLegacy;

// Only reachable through the guarded disposable-game oracle.
public static class CompatibilityOracle
{
    public static async Task<object> ExportPlayer(Player player)
    {
        var key=Design.Rows("branch_cards").First(r=>r.Text("classId")==PlayerBloodlines.Heir(player).classId).Text("cardId");
        var added=player.RunState.CreateCard(NativeCards.Get(key),player);CardCmd.Upgrade(added);
        await CardPileCmd.Add(added,PileType.Deck,skipVisuals:true);
        var save=player.ToSerializable();
        File.WriteAllText(Path.Combine(Runtime.DataDirectory,"compat-player.json"),JsonSerializer.Serialize(save,MegaCrit.Sts2.Core.Saves.JsonSerializationUtility.GetTypeInfo<MegaCrit.Sts2.Core.Saves.Runs.SerializablePlayer>()));
        return new{key,exported=true,snapshot=FamilySnapshot.Serialize(PlayerBloodlines.State(player)!)};
    }
    public static object ImportPlayer()
    {
        var before=FamilyHash();
        var save=JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(Runtime.DataDirectory,"compat-player.json")),MegaCrit.Sts2.Core.Saves.JsonSerializationUtility.GetTypeInfo<MegaCrit.Sts2.Core.Saves.Runs.SerializablePlayer>())!;
        var restored=Player.FromSerializable(save);
        if(FamilyHash()!=before)throw new InvalidDataException("Cross-branch player preview changed the local family.");
        return new{snapshot=FamilySnapshot.Serialize(PlayerBloodlines.State(restored)!),cards=restored.Deck.Cards.OfType<LegacyCard>().Select(c=>new{key=c.Key,upgrade=c.IsUpgraded}).ToArray(),familyUnchanged=true};
    }
    private static SceneTree Tree=>(SceneTree)Engine.GetMainLoop();
    private static string FamilyHash()=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Runtime.FamilyPath)));
    public static object Reinitialize()
    {
        var profile=Runtime.Profile;var hash=FamilyHash();var before=JsonSerializer.Serialize(NativeStartup.Inventory());var subscriptions=Tree.GetSignalConnectionList(SceneTree.SignalName.ProcessFrame).Count;
        Runtime.Initialize();Runtime.Initialize();
        if(!ReferenceEquals(profile,Runtime.Profile)||FamilyHash()!=hash||JsonSerializer.Serialize(NativeStartup.Inventory())!=before||Tree.GetSignalConnectionList(SceneTree.SignalName.ProcessFrame).Count!=subscriptions)throw new InvalidDataException("Repeated initialization changed state or installed duplicate hooks.");
        return new{unchanged=true,patchClasses=NativeStartup.PatchClasses.Length,inventory=NativeStartup.Inventory()};
    }
    public static object Rollback()
    {
        const string owner="CapeKid.SpireLegacy2.StartupAudit";var hash=FamilyHash();var before=JsonSerializer.Serialize(NativeStartup.Inventory());string detail;
        try{NativeStartup.Install(owner,[typeof(HeirRuntimeTexturePreload),typeof(HeirRuntimeTextureLookup)],nameof(HeirRuntimeTextureLookup));throw new InvalidDataException("Missing API fixture unexpectedly passed.");}
        catch(InvalidOperationException failure){detail=failure.ToString();}
        if(FamilyHash()!=hash||before!=JsonSerializer.Serialize(NativeStartup.Inventory())||Harmony.GetAllPatchedMethods().Any(m=>Harmony.GetPatchInfo(m)!.Owners.Contains(owner)))throw new InvalidDataException("Failed patch installation left state or hooks behind.");
        return new{rolledBack=true,error=detail};
    }
    public static object Scopes(Player player)
    {
        var original=PlayerBloodlines.Creating.Value;var coop=CoopBloodlines.CreatingMultiplayer.Value;var marker=new FamilyProfile.FamilyRun{id="ambient-marker"};
        var failures=0;
        try{
            PlayerBloodlines.Creating.Value=marker;CoopBloodlines.CreatingMultiplayer.Value=true;
            try{Player.CreateForNewRun(ModelDb.Character<HeirCharacter>(),UnlockState.all,ulong.MaxValue);throw new InvalidDataException("Missing owner fixture passed.");}
            catch(InvalidDataException e) when(e.Message.Contains("synchronized family snapshot")){failures++;}
            if(!ReferenceEquals(PlayerBloodlines.Creating.Value,marker))throw new InvalidDataException("Creation scope leaked after prefix exception.");
            CoopBloodlines.CreatingMultiplayer.Value=false;
            var saved=player.ToSerializable();var relic=saved.Relics.Single(r=>r.Id==ModelDb.Relic<BloodlineRelic>().Id);
            var values=relic.Props!.strings!;var index=values.FindIndex(v=>v.name==nameof(BloodlineRelic.FamilyRunSnapshot));var field=values[index];field.value="{";values[index]=field;
            try{Player.FromSerializable(saved);throw new InvalidDataException("Malformed snapshot fixture passed.");}catch(JsonException){failures++;}
            if(!ReferenceEquals(PlayerBloodlines.Creating.Value,marker))throw new InvalidDataException("Saved-player scope leaked after prefix exception.");
        }finally{PlayerBloodlines.Creating.Value=original;CoopBloodlines.CreatingMultiplayer.Value=coop;}
        return new{restored=true,failures};
    }
    public static async Task<object> NativeControl(bool patches,bool upgraded)
    {
        if(Runtime.Profile.active!=null)throw new InvalidOperationException("Settle the test heir before native control.");
        if(NRun.Instance!=null)await NGame.Instance!.ReturnToMainMenu();
        var original=Runtime.Profile.selected;var hash=FamilyHash();
        if(!patches)NativeStartup.Remove(NativeStartup.Owner);
        try{
            Runtime.Profile.selected=new(){classId="mage",traits=["megahealth","diva","cantattack"]};
            foreach(var key in new[]{"accept_tutorials_ftue","map_select_ftue","combat_rules_ftue","shuffle_ftue"})MegaCrit.Sts2.Core.Saves.SaveManager.Instance.MarkFtueAsComplete(key);
            await NGame.Instance!.StartNewSingleplayerRun(ModelDb.Character<Ironclad>(),true,ModelDb.ActsByIndex.Select(a=>a.First()).ToArray(),[],"SPIRELEGACY-CONTROL",GameMode.Standard,0);
            var run=(RunState)AccessTools.Property(typeof(RunManager),"State").GetValue(RunManager.Instance)!;
            await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,model:run.Act.AllEncounters.First(e=>e.RoomType==MegaCrit.Sts2.Core.Rooms.RoomType.Monster).ToMutable(),showTransition:false);
            RunManager.Instance.ActionExecutor.Unpause();
            var player=run.Players.Single();var deadline=Time.GetTicksMsec()+15000;
            while(player.PlayerCombatState!.Phase!=MegaCrit.Sts2.Core.Combat.PlayerTurnPhase.Play){if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Native control startup");await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);}
            player.Creature.SetCurrentHpInternal(player.Creature.MaxHp-10);var hp=player.Creature.CurrentHp;
            await CreatureCmd.Heal(player.Creature,5,false);var healing=player.Creature.CurrentHp-hp;
            async Task<object> Play(CardType type){
                var card=player.Deck.Cards.First(c=>c.Type==type&&c.Rarity==CardRarity.Basic);
                card=player.Creature.CombatState!.CreateCard(ModelDb.GetById<CardModel>(card.Id),player);if(upgraded)CardCmd.Upgrade(card);
                await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
                var target=card.TargetType==TargetType.AnyEnemy?player.Creature.CombatState.GetOpponentsOf(player.Creature).First(c=>c.IsAlive):null;
                var enemyHp=target?.CurrentHp;var energy=player.PlayerCombatState.Energy;var block=player.Creature.Block;
                if(!card.TryManualPlay(target))throw new InvalidDataException("Native control refused "+card.Id);
                var end=Time.GetTicksMsec()+15000;
                while(card.Pile?.Type is PileType.Hand or PileType.Play){if(Time.GetTicksMsec()>end)throw new TimeoutException("Native control card did not resolve");await Tree.ToSignal(Tree,SceneTree.SignalName.ProcessFrame);}
                return new{card=card.Id.ToString(),damage=enemyHp-target?.CurrentHp,block=player.Creature.Block-block,energy=player.PlayerCombatState.Energy-energy,pile=card.Pile?.Type.ToString()};
            }
            var attack=await Play(CardType.Attack);var guard=await Play(CardType.Skill);
            var heirSprites=NativeTestBridge.Descendants(Tree.Root).OfType<HeirSprite>().Count(s=>s.IsVisibleInTree());
            await (Task)AccessTools.Method(typeof(RunManager),"AbandonInternal").Invoke(RunManager.Instance,[])!;
            await NGame.Instance.ReturnToMainMenu();
            if(FamilyHash()!=hash)throw new InvalidDataException("Native character modified family state.");
            return new{upgraded,maxHp=player.Creature.MaxHp,healing,attack,guard,heirSprites,familyUnchanged=true};
        }finally{Runtime.Profile.selected=original;if(!patches)NativeStartup.Install(NativeStartup.Owner,NativeStartup.PatchClasses);}
    }
}
