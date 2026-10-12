using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace SpireLegacy;

// Opt-in pilot only. Deliberately supports one verified defensive potion, not a generic potion solver.
public static class PilotPotions
{
    public record Used(string Id,int Incoming,int Hp,int PreviousBlock,int GainedBlock);
    public static async Task<Used?> EmergencyBlock(Player player,int incoming)
    {
        // Match native combat-use restrictions without accessing a popup that may not exist.
        if(!CombatManager.Instance.IsInProgress||CombatManager.Instance.PlayerActionsDisabled||player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play||player.Creature.CombatState?.CurrentSide!=player.Creature.Side||!player.CanRemovePotions||NOverlayStack.Instance?.Peek()!=null||!PilotHealth.NeedsEmergencyBlock(player.Creature.CurrentHp,player.Creature.Block,incoming))return null;
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
}
