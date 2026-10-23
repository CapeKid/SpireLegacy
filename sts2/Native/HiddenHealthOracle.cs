using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace SpireLegacy;

// Opt-in inspection of the native widgets, not a replacement health simulation.
public static class HiddenHealthOracle
{
    public static async Task<object> Inspect(RunState run, bool changeHp)
    {
        var player=run.Players.Single();
        var before=player.Creature.CurrentHp;
        if(changeHp) {
            player.Creature.SetCurrentHpInternal(before-10);
            await CreatureCmd.Heal(player.Creature,3,false);
            if(player.Creature.CurrentHp!=before-7)throw new InvalidDataException("Health masking changed healing or HP.");
            await CreatureCmd.GainBlock(player.Creature,5,MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,null);
        }
        var tree=(SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var nodes=NativeTestBridge.Descendants(tree.Root).ToArray();
        var bars=nodes.OfType<NHealthBar>().Where(b=>b.IsVisibleInTree()).Select(b=>{
            var creature=(Creature)AccessTools.Field(typeof(NHealthBar),"_creature").GetValue(b)!;
            return new{owner=creature.Player?.NetId,enemy=creature.IsEnemy,hidden=HiddenHeirHealth.Applies(creature.Player),
                hpVisible=b.HpBarContainer.IsVisibleInTree(),textVisible=b.GetNode<Control>("%HpLabel").IsVisibleInTree(),
                blockVisible=b.GetNode<Control>("%BlockContainer").IsVisibleInTree(),block=creature.Block};
        }).ToArray();
        var top=nodes.OfType<NTopBarHp>().Single();
        var hidden=HiddenHeirHealth.Applies(player);
        if(top.IsVisibleInTree()==hidden)throw new InvalidDataException("Top bar health visibility disagrees with trait.");
        foreach(var bar in bars) {
            if(bar.hpVisible==bar.hidden||bar.hidden&&bar.textVisible)throw new InvalidDataException("Health bar visibility disagrees with its owner.");
            if(bar.block>0&&!bar.blockVisible)throw new InvalidDataException("Hiding HP also hid Block.");
        }
        if(run.CurrentRoom is MegaCrit.Sts2.Core.Rooms.CombatRoom&&!bars.Any(b=>b.owner==player.NetId))throw new InvalidDataException("Player health bar was not inspected.");
        return new{hidden,topVisible=top.IsVisibleInTree(),hp=player.Creature.CurrentHp,maxHp=player.Creature.MaxHp,bars};
    }
}
