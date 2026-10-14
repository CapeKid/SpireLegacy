using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace SpireLegacy;

// Opt-in pilot only. Deliberately supports one verified defensive potion, not a generic potion solver.
public static class PilotPotions
{
    private static readonly System.Reflection.PropertyInfo Permission=AccessTools.Property(typeof(Player),"CanUseOrRemovePotions")??AccessTools.Property(typeof(Player),"CanRemovePotions")??throw new MissingMemberException("Native potion permission property unavailable");
    public record Used(string Id,int Incoming,int Hp,int PreviousBlock,int GainedBlock);
    public static async Task<Used?> EmergencyBlock(Player player,int incoming,bool preventive=false)
    {
        // Match native combat-use restrictions without accessing a popup that may not exist.
        if(!CombatManager.Instance.IsInProgress||CombatManager.Instance.PlayerActionsDisabled||player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play||player.Creature.CombatState?.CurrentSide!=player.Creature.Side||!(bool)Permission.GetValue(player)!||NOverlayStack.Instance?.Peek()!=null||!(PilotHealth.NeedsEmergencyBlock(player.Creature.CurrentHp,player.Creature.Block,incoming)||preventive&&incoming-player.Creature.Block>=10))return null;
        var tree=(SceneTree)Engine.GetMainLoop();
        var potion=player.Potions.OfType<BlockPotion>().FirstOrDefault(p=>!p.IsQueued&&p.PassesCustomUsabilityCheck&&p.IsValidTarget(player.Creature));
        if(potion==null)return null;
        var hp=player.Creature.CurrentHp;var before=player.Creature.Block;var finished=false;
        Action<MegaCrit.Sts2.Core.Models.AbstractModel> onFinished=_=>finished=true;
        potion.ExecutionFinished+=onFinished;
        try{
            potion.EnqueueManualUse(player.Creature);
            var deadline=Time.GetTicksMsec()+15000;
            while(!finished){if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Emergency Block Potion did not resolve");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
            if(player.Potions.Contains(potion))throw new InvalidDataException("Consumed Block Potion remains in belt");
            return new(potion.Id.ToString(),incoming,hp,before,player.Creature.Block-before);
        }finally{potion.ExecutionFinished-=onFinished;}
    }
    public static async Task<object?> Planned(Player player,bool resourcesOnly=false)
    {
        if(!CombatManager.Instance.IsInProgress||CombatManager.Instance.PlayerActionsDisabled||player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play||!(bool)Permission.GetValue(player)!||NOverlayStack.Instance?.Peek()!=null)return null;
        var enemies=player.Creature.CombatState!.HittableEnemies.Where(e=>e.IsAlive).ToArray();
        var target=enemies.OrderBy(e=>e.CurrentHp+e.Block).FirstOrDefault();
        var energy=player.PlayerCombatState.Energy;var hp=enemies.Sum(e=>e.CurrentHp);
        var potion=player.Potions.FirstOrDefault(p=>!p.IsQueued&&p.PassesCustomUsabilityCheck&&
            (p is EnergyPotion?energy<=1&&player.PlayerCombatState.Hand.Cards.Any(c=>c.EnergyCost.GetWithModifiers(MegaCrit.Sts2.Core.Entities.Cards.CostModifiers.All)>energy):
            !resourcesOnly&&(p is FirePotion&&target!=null&&target.CurrentHp<=20||player.PlayerCombatState.TurnNumber==1&&p is StrengthPotion or DexterityPotion)));
        if(potion==null)return null;
        var recipient=potion is FirePotion?target:player.Creature;
        if(!potion.IsValidTarget(recipient))return null;
        var finished=false;Action<MegaCrit.Sts2.Core.Models.AbstractModel> done=_=>finished=true;potion.ExecutionFinished+=done;
        try{
            potion.EnqueueManualUse(recipient);var tree=(SceneTree)Engine.GetMainLoop();var until=Time.GetTicksMsec()+15000;
            while(!finished){if(Time.GetTicksMsec()>until)throw new TimeoutException("Planned potion did not resolve.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
            if(player.Potions.Contains(potion))throw new InvalidDataException("Planned potion was not consumed.");
            return new{id=potion.Id.ToString(),reason=resourcesOnly?"afford-follow-up":"kill-or-first-turn-scaling",energyBefore=energy,energyAfter=player.PlayerCombatState?.Energy,enemyHpLost=hp-enemies.Sum(e=>e.CurrentHp)};
        }finally{potion.ExecutionFinished-=done;}
    }
}
