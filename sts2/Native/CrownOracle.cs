using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Isolated native settlement fixture. Never replaces real map entry with debug visits.
public static class CrownOracle
{
    public static object Inspect(RunState state)
    {
        var player=PlayerBloodlines.LocalPlayer!;var earned=CrownAccounting.Earned(state,player);
        return new{floor=state.TotalFloor,gold=player.Gold,earnedFloors=earned.floors,earnedGold=earned.gold,
            finished=state.CurrentRoom?.IsPreFinished,dead=!player.Creature.IsAlive,abandoned=RunManager.Instance.IsAbandoned,
            history=state.MapPointHistory.SelectMany(a=>a).Select(p=>new{type=p.MapPointType.ToString(),rooms=p.Rooms.Select(r=>r.RoomType.ToString()),stats=p.PlayerStats.Select(s=>new{s.PlayerId,s.GoldGained,s.GoldSpent,s.GoldLost,s.GoldStolen})}),bank=Runtime.Profile.crowns};
    }
    public static async Task<object> Play(RunState state,bool upgrade,bool win)
    {
        var player=PlayerBloodlines.LocalPlayer!;var combat=player.Creature.CombatState!;
        var card=combat.CreateCard(NativeCards.Get(PlayerBloodlines.Heir(player).classId+(win?"_strike":"_guard")),player);
        if(upgrade)CardCmd.Upgrade(card);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
        if(win)foreach(var enemy in combat.GetOpponentsOf(player.Creature).Where(e=>e.IsAlive)) {
            foreach(var power in enemy.Powers.ToArray())await PowerCmd.Remove(power);
            enemy.SetCurrentHpInternal(1);enemy.LoseBlockInternal(enemy.Block);
        }
        await PlayerCmd.GainEnergy(3,player);await BalanceOracle.Play(card,combat.GetOpponentsOf(player.Creature).First(e=>e.IsAlive));
        if(win)foreach(var enemy in combat.GetOpponentsOf(player.Creature).Where(e=>e.IsAlive).ToArray()) {
            var next=combat.CreateCard(NativeCards.Get(PlayerBloodlines.Heir(player).classId+"_strike"),player);if(upgrade)CardCmd.Upgrade(next);
            await CardPileCmd.Add(next,PileType.Hand,skipVisuals:true);await PlayerCmd.GainEnergy(1,player);await BalanceOracle.Play(next,enemy);
        }
        if(win){
            var tree=(SceneTree)Engine.GetMainLoop();var until=Time.GetTicksMsec()+30000;
            while(state.CurrentRoom?.IsPreFinished!=true){if(Time.GetTicksMsec()>until)throw new TimeoutException("Real combat victory did not finish.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
        }
        return Inspect(state);
    }
    public static async Task<object> Gold(RunState state,int gained,int spent,int lost,int stolen,int returned)
    {
        var player=PlayerBloodlines.LocalPlayer!;
        if(gained>0)await PlayerCmd.GainGold(gained,player);
        if(spent>0)await PlayerCmd.LoseGold(spent,player,GoldLossType.Spent);
        if(lost>0)await PlayerCmd.LoseGold(lost,player,GoldLossType.Lost);
        if(stolen>0)await PlayerCmd.LoseGold(stolen,player,GoldLossType.Stolen);
        if(returned>0)await PlayerCmd.GainGold(returned,player,wasStolenBack:true);
        return Inspect(state);
    }
}
