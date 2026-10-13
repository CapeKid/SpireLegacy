using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

public static class CrownAccounting
{
    public static (int floors,int gold) Earned(RunState state,Player player)
    {
        var points=state.MapPointHistory.SelectMany(a=>a).ToArray();
        var current=state.CurrentMapPointHistoryEntry;
        var finished=state.CurrentRoom?.IsPreFinished==true||state.CurrentRoom?.IsVictoryRoom==true;
        var complete=points.Where(p=>!ReferenceEquals(p,current)||finished).ToArray();
        var combat=complete.Any(p=>p.Rooms.Any(r=>r.RoomType is RoomType.Monster or RoomType.Elite or RoomType.Boss));
        var diedAfterPlay=!player.Creature.IsAlive&&state.CurrentRoom is CombatRoom {IsPreFinished:false}&&
            CombatManager.Instance.History.CardPlaysFinished.Any(e=>e.CardPlay.Card.Owner==player&&!e.CardPlay.IsAutoPlay);
        var stats=points.SelectMany(p=>p.PlayerStats).Where(p=>p.PlayerId==player.NetId).ToArray();
        return CrownEarnings.Qualify(complete.Count(p=>p.MapPointType is not (MapPointType.Ancient or MapPointType.Unassigned)),combat,diedAfterPlay,
            stats.Sum(s=>(long)s.GoldGained),stats.Sum(s=>(long)s.GoldSpent),stats.Sum(s=>(long)s.GoldLost),stats.Sum(s=>(long)s.GoldStolen),player.Gold);
    }
}
