using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Synthetic policy calibration. Injected hands/HP are never campaign evidence.
public static class PacifistPilotOracle
{
    public static async Task<object> Probe(RunState run,string key,bool upgraded)
    {
        if(key is not ("knight_strike" or "mage_strike" or "ranger_strike"))throw new ArgumentException("Unknown starter.");
        var player=run.Players.Single();var pacifist=PlayerBloodlines.Traits(player).Has("pacifist");
        await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.NibbitsNormal>().ToMutable(),showTransition:false);
        RunManager.Instance.ActionExecutor.Unpause();var tree=(SceneTree)Engine.GetMainLoop();var until=Time.GetTicksMsec()+30000;
        while(player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play){
            if(Time.GetTicksMsec()>until)throw new TimeoutException("Pacifist calibration play phase.");
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        var combat=player.Creature.CombatState!;var hand=PileType.Hand.GetPile(player);
        foreach(var card in new[]{PileType.Hand,PileType.Draw,PileType.Discard}.SelectMany(p=>p.GetPile(player).Cards).ToArray())await CardPileCmd.Add(card,PileType.Exhaust,skipVisuals:true);
        var enemy=combat.HittableEnemies.First();foreach(var e in combat.HittableEnemies){e.SetMaxHpInternal(1000);e.SetCurrentHpInternal(1000);}
        async Task<LegacyCard> Add(string id){var c=(LegacyCard)combat.CreateCard(NativeCards.Get(id),player);await CardPileCmd.Add(c,PileType.Hand,skipVisuals:true);return c;}
        var starter=await Add(key);if(upgraded)CardCmd.Upgrade(starter);
        var blocked=await Add("knight_merciless");
        var study=combat.CreateCard(NativeCards.Get("study"),player);
        var description=PilotCardInfo.Describe(starter);
        var blockedInfo=PilotCardInfo.Describe(blocked);
        var reward=PilotRewards.Pick([blockedInfo,PilotCardInfo.Describe(study)],player.Deck.Cards.Select(PilotCardInfo.Describe).ToArray(),true);
        var fuelExhausted=false;var guarded=false;var choseStarter=false;
        if(pacifist){
            if(blockedInfo.Usable||blocked.CanPlay()||reward!=1)throw new InvalidDataException("Pacifist reward legality mismatch.");
            var reforge=await Add("knight_reforge");
            using(TacticalPilot.Selector(true))await BalanceOracle.Play(reforge);
            fuelExhausted=blocked.Pile?.Type==PileType.Exhaust;
            if(!fuelExhausted||starter.Pile?.Type!=PileType.Hand)throw new InvalidDataException("Pacifist fuel selection lost the Poison source.");
            await CardPileCmd.Add(reforge,PileType.Hand,skipVisuals:true);
            guarded=TacticalPilot.WouldExhaustLastOffense(reforge);
            choseStarter=TacticalPilot.Choose(player,100)?.Card==starter;
            if(!guarded||!choseStarter)throw new InvalidDataException("Pilot would consume its last Poison source.");
        }else if(!blockedInfo.Usable)throw new InvalidDataException("Normal heir attack incorrectly blocked.");
        var hp=enemy.CurrentHp;var poison=enemy.GetPowerAmount<PoisonPower>();var energy=player.PlayerCombatState!.Energy;
        await BalanceOracle.Play(starter,enemy);
        var poisonApplied=enemy.GetPowerAmount<PoisonPower>()-poison;
        if(poisonApplied!=description.Poison||poisonApplied!=(pacifist?upgraded?3:2:0))throw new InvalidDataException("Poison descriptor differs from actual native effect.");
        var spent=energy-player.PlayerCombatState.Energy;var multiGuarded=false;
        if(pacifist){
            foreach(var c in hand.Cards.ToArray())await CardPileCmd.Add(c,PileType.Exhaust,skipVisuals:true);
            await CardPileCmd.Add(starter,PileType.Hand,skipVisuals:true);
            await Add("aim");var transmutation=await Add("mage_transmutation");
            multiGuarded=TacticalPilot.WouldExhaustLastOffense(transmutation);
            if(!multiGuarded)throw new InvalidDataException("Mandatory two-card exhaust would consume the last Poison source.");
        }
        return new{key,upgraded,pacifist,poisonApplied,description.Poison,blockedUsable=blockedInfo.Usable,reward,
            fuelExhausted,guarded,multiGuarded,choseStarter,damage=hp-enemy.CurrentHp,energySpent=spent};
    }
}
