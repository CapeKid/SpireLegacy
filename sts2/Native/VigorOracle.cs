using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Temporary observation hooks are installed only inside the guarded native oracle.
public static class VigorOracle
{
    private static CardModel? observed;
    private static readonly List<int> hits=[];
    private static int attacks;
    public static void DamageObserved(object[] __args)
    {
        if(observed!=null&&__args.OfType<CardModel>().Any(c=>c==observed)){var result=__args.OfType<DamageResult>().Single();if(result.Props.IsPoweredAttack())hits.Add(result.TotalDamage);}
    }
    public static void AttackObserved(object[] __args)
    {
        if(observed!=null&&__args.OfType<AttackCommand>().Any(c=>c.ModelSource==observed))attacks++;
    }
    public static async Task<object> Check(RunState run,string key,bool upgrade,IDisposable selector)
    {
        const string owner="CapeKid.SpireLegacy2.VigorProbe";
        var harmony=new Harmony(owner);var player=run.Players.Single();var combat=player.Creature.CombatState!;
        var enemies=combat.GetOpponentsOf(player.Creature).Where(e=>e.IsAlive).ToArray();
        if(enemies.Length==0)throw new InvalidDataException("Vigor probe needs live targets.");
        try {
            foreach(var pair in new[]{("AfterDamageGiven",nameof(DamageObserved)),("AfterAttack",nameof(AttackObserved))})
                harmony.Patch(typeof(Hook).GetMethods(BindingFlags.Public|BindingFlags.Static).Single(m=>m.Name==pair.Item1),prefix:new HarmonyMethod(typeof(VigorOracle),pair.Item2));
            using(selector) {
                async Task<object> Play(int vigor) {
                    foreach(var creature in enemies.Append(player.Creature)) {
                        foreach(var power in creature.Powers.ToArray())await PowerCmd.Remove(power);
                        creature.SetMaxHpInternal(10000);creature.SetCurrentHpInternal(10000);
                        await BranchCommands.LoseBlock(new BlockingPlayerChoiceContext(),creature,creature.Block,player.Creature);
                    }
                    // Deliberately isolate Vigor from inherited class-charge bonuses.
                    foreach(var held in PileType.Hand.GetPile(player).Cards.ToArray())await CardPileCmd.Add(held,PileType.Discard,skipVisuals:true);
                    await PlayerCmd.GainEnergy(10,player);
                    if(vigor>0)await PowerCmd.Apply<VigorPower>(new BlockingPlayerChoiceContext(),player.Creature,vigor,player.Creature,null);
                    var canonical=key=="native_twin_strike"?ModelDb.Card<TwinStrike>():NativeCards.Get(key);
                    var card=combat.CreateCard(canonical,player);if(upgrade)CardCmd.Upgrade(card);
                    await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
                    observed=card;hits.Clear();attacks=0;var energy=player.PlayerCombatState!.Energy;
                    var target=card.TargetType==TargetType.AnyEnemy?enemies[0]:null;
                    if(!card.TryManualPlay(target))throw new InvalidDataException("Vigor manual play refused "+key);
                    var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+20000;
                    while(card.Pile?.Type is PileType.Hand or PileType.Play){if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Vigor card resolution "+key);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
                    while(NCard.FindOnTable(card)!=null){if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Vigor card table cleanup "+key);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
                    return new{hits=hits.ToArray(),attacks,vigorRemaining=player.Creature.GetPowerAmount<VigorPower>(),energySpent=energy-player.PlayerCombatState.Energy,pile=card.Pile?.Type.ToString(),tableNodePresent=false};
                }
                var baseline=await Play(0);var boosted=await Play(7);
                return new{key,upgrade,targetCount=enemies.Length,baseline,boosted};
            }
        }finally{observed=null;NativeStartup.Remove(owner);}
    }
}
