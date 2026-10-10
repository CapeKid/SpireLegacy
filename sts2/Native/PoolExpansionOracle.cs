using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

/// <summary>Owned-game test fixture; accessible only through the opt-in native bridge.</summary>
public static class PoolExpansionOracle
{
    public static async Task<object> Play(RunState run,string key,bool upgrade,IDisposable selector)
    {
        using(selector)
        {
            var player=run.Players.Single();var combat=player.Creature.CombatState!;
            var enemy=combat.GetOpponentsOf(player.Creature).First(e=>e.IsAlive);
            player.Creature.SetCurrentHpInternal(player.Creature.MaxHp-20);
            if(key=="knight_royal_execution")enemy.SetCurrentHpInternal(enemy.MaxHp/2);
            if(key=="ranger_serpent_oil")await PowerCmd.Apply<PoisonPower>(new BlockingPlayerChoiceContext(),enemy,3,player.Creature,null);
            var fuel=combat.CreateCard(NativeCards.Get(player.Character is HeirCharacter ? Runtime.Heir.classId+"_guard" : "knight_guard"),player);
            await CardPileCmd.Add(fuel,PileType.Exhaust,skipVisuals:true);
            var discarded=combat.CreateCard(NativeCards.Get(Runtime.Heir.classId+"_guard"),player);
            await CardPileCmd.Add(discarded,PileType.Discard,skipVisuals:true);
            var card=(LegacyCard)combat.CreateCard(NativeCards.Get(key),player);
            if(upgrade)CardCmd.Upgrade(card);
            await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
            var hand=PileType.Hand.GetPile(player);
            var costs=hand.Cards.Where(c=>c!=card&&!c.EnergyCost.CostsX).Select(c=>c.EnergyCost.GetWithModifiers(CostModifiers.All)).ToArray();
            var hp=player.Creature.CurrentHp;var enemyHp=enemy.CurrentHp;var block=player.Creature.Block;var energy=player.PlayerCombatState!.Energy;
            var target=card.TargetType==TargetType.AnyEnemy?enemy:null;
            var cost=card.EnergyCost.GetWithModifiers(CostModifiers.All);
            if(!card.TryManualPlay(target))throw new InvalidDataException("Native manual play refused "+key);
            var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+15000;
            while(card.Pile?.Type is PileType.Hand or PileType.Play && Time.GetTicksMsec()<deadline)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            if(card.Pile?.Type is PileType.Hand or PileType.Play)throw new TimeoutException("Card did not resolve: "+key);
            deadline=Time.GetTicksMsec()+2500;
            while(NCard.FindOnTable(card)!=null&&Time.GetTicksMsec()<deadline)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            if(NCard.FindOnTable(card)!=null)throw new InvalidDataException("Card remains hovering: "+key);
            var saved=card.ToSerializable();var restored=(LegacyCard)run.LoadCard(saved,player);
            if(restored.Key!=key||restored.IsUpgraded!=upgrade||restored.DesignCard.Rules()!=card.DesignCard.Rules())throw new InvalidDataException("New pool card save mismatch: "+key);
            run.RemoveCard(restored);
            return new{key,upgrade,pile=card.Pile?.Type.ToString(),cost,hpChange=player.Creature.CurrentHp-hp,enemyDamage=enemyHp-enemy.CurrentHp,blockChange=player.Creature.Block-block,energyChange=player.PlayerCombatState.Energy-energy,
                poison=enemy.GetPowerAmount<PoisonPower>(),charges=player.Creature.GetPower<P_class>()?.Charges,
                hand=hand.Cards.Select(c=>c is LegacyCard legacy?legacy.Key:c.Id.ToString()).ToArray(),
                costsBefore=costs,costsAfter=hand.Cards.Where(c=>!c.EnergyCost.CostsX).Select(c=>c.EnergyCost.GetWithModifiers(CostModifiers.All)).ToArray(),
                powers=player.Creature.Powers.Select(p=>new{id=p.Id.ToString(),amount=p.Amount}).ToArray(),
                fuelReturned=hand.Cards.Contains(fuel),saveRoundTrip=true,tableNodePresent=false};
        }
    }
}
