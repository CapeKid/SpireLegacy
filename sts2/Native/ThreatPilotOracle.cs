using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Synthetic calibration only. No measured campaign uses these HP/Block injections.
public static class ThreatPilotOracle
{
    public static async Task<object> Probe(RunState run,string key,string kind)
    {
        if(key is not ("knight_strike" or "mage_strike" or "ranger_strike" or "ironclad"))throw new ArgumentException("Unknown threat control card.");
        if(kind is not ("threat" or "covered" or "nonlethal"))throw new ArgumentException("Unknown threat calibration.");
        var player=run.Players.Single();
        if((key=="ironclad")!=(player.Character is MegaCrit.Sts2.Core.Models.Characters.Ironclad))throw new InvalidDataException("Native control character mismatch.");
        await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.NibbitsNormal>().ToMutable(),showTransition:false);
        RunManager.Instance.ActionExecutor.Unpause();
        var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+30000;
        while(player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play){
            if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Threat fixture play phase.");
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        var hand=PileType.Hand.GetPile(player);
        foreach(var card in hand.Cards.ToArray())await CardPileCmd.Add(card,PileType.Draw,skipVisuals:true);
        var enemies=player.Creature.CombatState!.HittableEnemies.Where(e=>e.IsAlive).ToArray();
        var attacker=enemies.Single(e=>TacticalPilot.Threat(player,e)>0);
        var harmless=enemies.Single(e=>TacticalPilot.Threat(player,e)==0);
        attacker.SetCurrentHpInternal(kind=="nonlethal"?attacker.MaxHp:2);harmless.SetCurrentHpInternal(1);
        var incoming=TacticalPilot.Incoming(player);
        if(kind=="covered")await CreatureCmd.GainBlock(player.Creature,incoming,MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,null);
        var model=key=="ironclad"?ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>():NativeCards.Get(key);
        var strike=player.Creature.CombatState.CreateCard(model,player);
        await CardPileCmd.Add(strike,PileType.Hand,skipVisuals:true);
        var blockBefore=player.Creature.Block;var energyBefore=player.PlayerCombatState!.Energy;
        var attackerHp=attacker.CurrentHp;var harmlessHp=harmless.CurrentHp;
        var decision=TacticalPilot.Choose(player,incoming)??throw new InvalidDataException("No threat fixture decision.");
        if(decision.Card!=strike)throw new InvalidDataException("Unexpected threat fixture card.");
        await BalanceOracle.Play(decision.Card,decision.Target);
        return new{key,kind,incoming,blockBefore,chosenAttacker=decision.Target==attacker,
            attackerHpBefore=attackerHp,attackerHpAfter=attacker.CurrentHp,
            harmlessHpBefore=harmlessHp,harmlessHpAfter=harmless.CurrentHp,
            attackerDead=!attacker.IsAlive,harmlessDead=!harmless.IsAlive,
            energySpent=energyBefore-player.PlayerCombatState.Energy,decision.Score,decision.Reason};
    }
}
