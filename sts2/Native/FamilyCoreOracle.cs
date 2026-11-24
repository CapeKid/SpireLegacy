using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Opt-in owned-game fixture for the shared family core and its class lines.
// Paid native plays and real turn transitions are the oracle; the driver
// (build/family-core/native-check.py) owns the expected values.
public static class FamilyCoreOracle
{
    private static readonly PileType[] Piles=[PileType.Draw,PileType.Hand,PileType.Discard,PileType.Exhaust,PileType.Play];
    private static readonly BlockingPlayerChoiceContext Context=new();
    private static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
    private static async Task Wait(Func<bool> done,string what)
    {
        var tree=(SceneTree)Engine.GetMainLoop();var until=Time.GetTicksMsec()+20000;
        while(!done()){if(Time.GetTicksMsec()>until)throw new TimeoutException(what);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
    }
    private static P_class Counter(Player player)=>player.Creature.GetPower<P_class>()??throw new InvalidDataException("Missing class power.");
    private static Creature[] Enemies(Player player)=>player.Creature.CombatState!.GetOpponentsOf(player.Creature).Where(e=>e.IsAlive).ToArray();
    private static object Snapshot(Player player)
    {
        var counter=Counter(player);var combat=player.PlayerCombatState!;
        return new{
            hand=combat.Hand.Cards.Count,block=player.Creature.Block,energy=combat.Energy,charges=counter.Charges,attacks=counter.AttacksThisTurn,powersPlayed=counter.PowersPlayed,
            playerWeak=player.Creature.GetPowerAmount<WeakPower>(),
            enemies=Enemies(player).Select(e=>new{hp=e.CurrentHp,weak=e.GetPowerAmount<WeakPower>(),vulnerable=e.GetPowerAmount<VulnerablePower>()}).ToArray(),
            cards=combat.Hand.Cards.Select(c=>new{id=c.Id.Entry,key=(c as LegacyCard)?.Key,cost=c.EnergyCost.GetWithModifiers(CostModifiers.All),exhaust=c.Keywords.Contains(CardKeyword.Exhaust),innate=c.Keywords.Contains(CardKeyword.Innate)}).ToArray()};
    }
    private static async Task Reset(Player player)
    {
        await CardPileCmd.RemoveFromCombat(CardPile.GetCards(player,Piles).ToArray(),skipVisuals:true);
        foreach(var creature in player.Creature.CombatState!.GetOpponentsOf(player.Creature).Append(player.Creature)){
            foreach(var power in creature.Powers.ToArray())await PowerCmd.Remove(power);
            creature.LoseBlockInternal(creature.Block);
            if(!creature.IsPlayer&&creature.IsAlive){creature.SetMaxHpInternal(10000);creature.SetCurrentHpInternal(10000);}
        }
        await CardEffects.Apply(Context,"class",player.Creature,1,player.Creature,null);
        await PlayerCmd.GainEnergy(30,player);
        for(var i=0;i<40;i++){var filler=player.Creature.CombatState.CreateCard(ModelDb.Card<DefendIronclad>(),player);await CardPileCmd.Add(filler,PileType.Draw,skipVisuals:true);}
    }
    private static async Task<CardModel> Play(Player player,string key,bool up,Creature? target)
    {
        var card=player.Creature.CombatState!.CreateCard(NativeCards.Get(key),player);
        if(up)CardCmd.Upgrade(card);
        await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
        var failure=CardPlayDiagnostics.LastFailure;
        Require(card.TryManualPlay(card.TargetType==TargetType.AnyEnemy?target??Enemies(player)[0]:null),"Paid play refused: "+key);
        await Wait(()=>card.Pile?.Type is not (PileType.Hand or PileType.Play)&&NCard.FindOnTable(card)==null&&!RunManager.Instance.ActionExecutor.IsRunning,"Card unresolved: "+key);
        Require(CardPlayDiagnostics.LastFailure==failure,"Card action raised: "+CardPlayDiagnostics.LastFailure);
        return card;
    }
    // One scripted step: optionally reset, preset class counters and debuffs,
    // play cards (the last one measured), or end a protected turn.
    public static async Task<object> Step(RunState run,System.Text.Json.JsonElement request)
    {
        var player=run.Players.Single();
        bool Flag(string name)=>request.TryGetProperty(name,out var v)&&v.GetBoolean();
        int? Number(string name)=>request.TryGetProperty(name,out var v)?v.GetInt32():null;
        if(Flag("reset"))await Reset(player);
        var counter=Counter(player);var enemies=Enemies(player);
        if(Flag("extraEnemy")&&enemies.Length<2){
            // A second enemy keeps combat alive when a kill path is measured.
            var extra=await CreatureCmd.Add(enemies[0].Monster!.CanonicalInstance.ToMutable(),player.Creature.CombatState!,enemies[0].Side,null);
            extra.SetMaxHpInternal(10000);extra.SetCurrentHpInternal(10000);enemies=Enemies(player);}
        if(Number("charges") is {} charges)counter.Charges=charges;
        if(Number("attacks") is {} attacks)counter.AttacksThisTurn=attacks;
        if(Number("enemyWeak") is {} weak&&weak>0)await PowerCmd.Apply<WeakPower>(Context,enemies[0],weak,player.Creature,null);
        if(Number("playerWeak") is {} selfWeak&&selfWeak>0)await PowerCmd.Apply<WeakPower>(Context,player.Creature,selfWeak,null,null);
        if(Number("enemyHp") is {} hp)enemies[0].SetCurrentHpInternal(hp);
        if(Number("retain") is {} retain)for(var i=0;i<retain;i++){
            var kept=player.Creature.CombatState!.CreateCard(NativeCards.Get("mage_stored_ember"),player);await CardPileCmd.Add(kept,PileType.Hand,skipVisuals:true);}
        var before=Snapshot(player);var target=enemies.FirstOrDefault();var targetAlive=target?.IsAlive==true;
        object? cost=null;
        if(request.TryGetProperty("costOf",out var costOf)){
            var probe=player.Creature.CombatState!.CreateCard(NativeCards.Get(costOf.GetString()!),player);
            if(Flag("upgrade"))CardCmd.Upgrade(probe);
            await CardPileCmd.Add(probe,PileType.Hand,skipVisuals:true);
            cost=probe.EnergyCost.GetWithModifiers(CostModifiers.All);
            await CardPileCmd.RemoveFromCombat([probe],skipVisuals:true);
            before=Snapshot(player);
        }
        if(request.TryGetProperty("play",out var plays))foreach(var key in plays.EnumerateArray())await Play(player,key.GetString()!,Flag("upgrade"),target);
        if(Flag("flush")){
            // Native AfterFlush with the hand that stays: the same hook end of turn invokes.
            var retained=player.PlayerCombatState!.Hand.Cards.Where(c=>c.Keywords.Contains(CardKeyword.Retain)).ToArray();
            foreach(var power in player.Creature.Powers.OfType<LegacyPower>().ToArray())await power.AfterFlush(Context,player,[],retained);
        }
        if(Flag("endTurn")){
            var turn=player.PlayerCombatState!.TurnNumber;
            await BalanceOracle.End(player,true);
            await Wait(()=>player.PlayerCombatState.Phase==PlayerTurnPhase.Play&&player.PlayerCombatState.TurnNumber>turn,"Next turn did not start.");
        }
        return new{before,after=Snapshot(player),cost,targetKilled=targetAlive&&target is {IsAlive:false},enemyCount=enemies.Length};
    }
    public static object Deck(RunState run)=>run.Players.Single().Deck.Cards.Select(c=>(c as LegacyCard)?.Key??c.Id.Entry).ToArray();
    public static object Text(RunState run,string key)
    {
        var player=run.Players.Single();
        var card=player.Creature.CombatState is {} combat?combat.CreateCard(NativeCards.Get(key),player):run.CreateCard(NativeCards.Get(key),player);
        return new{key,description=card.GetDescriptionForPile(PileType.Hand),canonical=NativeCards.Get(key).GetDescriptionForPile(PileType.Deck)};
    }
}
