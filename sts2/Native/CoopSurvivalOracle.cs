using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Only reachable from the opt-in isolated bridge. Choose a native encounter,
// then use ordinary drawn cards/energy and enemy turns. Never assign HP, grant
// cards/resources, invoke revival directly or replace native choice handling.
public static class CoopSurvivalOracle
{
    public static async Task<object> Enter()
    {
        await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,
            model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.SlimesWeak>().ToMutable(),showTransition:false);
        RunManager.Instance.ActionExecutor.Unpause();
        return CoopOracle.State();
    }
    public static async Task<object> Step(bool attack)
    {
        var player=PlayerBloodlines.LocalPlayer!;
        if(!CombatManager.Instance.IsInProgress||player.Creature.IsDead||RunManager.Instance.ActionExecutor.IsRunning||player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play)
            return new{action="wait",state=CoopOracle.State()};
        var card=player.PlayerCombatState.Hand.Cards.FirstOrDefault(c=>c.CanPlay()&&
            (attack?c.Type==CardType.Attack:c is LegacyCard lc&&lc.Key=="knight_guard"));
        card??=player.PlayerCombatState.Hand.Cards.FirstOrDefault(c=>c.CanPlay()&&c is MegaCrit.Sts2.Core.Models.Cards.Slimed);
        if(card==null){CoopOracle.EndTurn();return new{action="end-turn",state=CoopOracle.State()};}
        var energy=player.PlayerCombatState.Energy;
        var cost=card.EnergyCost.GetWithModifiers(CostModifiers.All);
        var target=card.TargetType==TargetType.AnyEnemy?player.Creature.CombatState!.GetOpponentsOf(player.Creature).First(c=>c.IsAlive):null;
        if(!card.TryManualPlay(target))throw new InvalidDataException("Survival probe native paid play refused.");
        var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+15000;
        while(card.Pile?.Type is PileType.Hand or PileType.Play||NCard.FindOnTable(card)!=null||RunManager.Instance.ActionExecutor.IsRunning){
            if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Survival probe paid action did not finish.");
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        return new{action="play",card=card.Id.ToString(),energyBefore=energy,energyAfter=player.PlayerCombatState?.Energy,cost,pile=card.Pile?.Type.ToString(),state=CoopOracle.State()};
    }
}
