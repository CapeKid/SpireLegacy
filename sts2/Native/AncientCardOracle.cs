using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Opt-in owned-game fixture; native paid play remains the source of truth.
public static class AncientCardOracle
{
    public static async Task<object> Check(RunState run,string key,bool upgrade,IDisposable selector)
    {
        using(selector) {
            var player=run.Players.Single();var creature=player.Creature;var combat=creature.CombatState!;
            var context=new BlockingPlayerChoiceContext();var hand=PileType.Hand.GetPile(player);
            var attack=combat.CreateCard(NativeCards.Get(Runtime.Heir.classId+"_strike"),player);
            await CardPileCmd.Add(attack,PileType.Hand,skipVisuals:true);
            if(key=="knight_kingbreaker")await CreatureCmd.GainBlock(creature,1,ValueProp.Unpowered,null);
            var played=await PoolExpansionOracle.Play(run,key,upgrade,selector:new NoopScope());
            if(key=="knight_kingbreaker"&&creature.GetPowerAmount<StrengthPower>()!=2)throw new InvalidDataException("Guarded finisher did not grant exactly two Strength.");
            if(key=="knight_royal_arsenal"&&(!attack.IsUpgraded||player.Deck.Cards.Any(c=>c.IsUpgraded)))throw new InvalidDataException("Arsenal did not isolate upgrades to combat hand.");
            var firstDraw=0;var secondDraw=0;var resetDraw=0;
            async Task Skill()
            {
                var skill=combat.CreateCard(NativeCards.Get("mage_guard"),player);
                await CardPileCmd.Add(skill,PileType.Hand,skipVisuals:true);
                var energy=player.PlayerCombatState!.Energy;var cost=skill.EnergyCost.GetWithModifiers(CostModifiers.All);
                if(!skill.TryManualPlay(null))throw new InvalidDataException("Ancient card paid Skill refused.");
                var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+15000;
                while((skill.Pile?.Type is PileType.Hand or PileType.Play||RunManager.Instance.ActionExecutor.IsRunning)&&Time.GetTicksMsec()<deadline)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                if(skill.Pile?.Type is PileType.Hand or PileType.Play||player.PlayerCombatState.Energy!=energy-cost)throw new InvalidDataException("Ancient Skill payment/resolution.");
            }
            if(key=="mage_arcane_conductor") {
                await PlayerCmd.GainEnergy(3,player);
                var before=hand.Cards.Count;await Skill();firstDraw=hand.Cards.Count-before;
                if(firstDraw!=1||creature.GetPower<P_class>()?.Charges!=2)throw new InvalidDataException("First Skill charge/draw mismatch.");
                before=hand.Cards.Count;await Skill();secondDraw=hand.Cards.Count-before;
                if(secondDraw!=0||creature.GetPower<P_class>()?.Charges!=2)throw new InvalidDataException("Arcane Echo repeated or exceeded its cap.");
                await CreatureCmd.GainBlock(creature,999,ValueProp.Unpowered,null);
                var turn=player.PlayerCombatState!.TurnNumber;CombatManager.Instance.SetReadyToEndTurn(player,false);
                var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+20000;
                while((player.PlayerCombatState.Phase!=PlayerTurnPhase.Play||player.PlayerCombatState.TurnNumber<=turn)&&Time.GetTicksMsec()<deadline)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                if(player.PlayerCombatState.TurnNumber<=turn)throw new TimeoutException("Arcane reset turn.");
                before=hand.Cards.Count;await Skill();resetDraw=hand.Cards.Count-before;
                if(resetDraw!=1)throw new InvalidDataException("Arcane Echo did not reset next turn.");
            }
            return new{key,upgrade,played,firstDraw,secondDraw,resetDraw,handAttackUpgraded=attack.IsUpgraded};
        }
    }
    private sealed class NoopScope:IDisposable {public void Dispose() {}}
}
