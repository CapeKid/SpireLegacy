using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.TestSupport;

namespace SpireLegacy;

// Diagnostic heuristic, not an expert solver. Preview estimates do not execute combat effects.
public static class TacticalPilot
{
    public record Decision(CardModel Card,Creature? Target,double Score,string Reason);
    public static int Threat(Player player,Creature enemy)=>enemy.IsAlive?enemy.Monster!.NextMove.Intents.OfType<MegaCrit.Sts2.Core.MonsterMoves.Intents.AttackIntent>().Sum(a=>a.GetTotalDamage(new[]{player.Creature},enemy)):0;
    public static int Incoming(Player player)=>player.Creature.CombatState!.GetOpponentsOf(player.Creature).Sum(e=>Threat(player,e));
    private static int Cost(CardModel card)=>card.EnergyCost.CostsX?card.Owner.PlayerCombatState!.Energy:Math.Max(0,card.EnergyCost.GetWithModifiers(CostModifiers.All));
    private static decimal Damage(CardModel card,Creature target)
    {
        if(card is LegacyCard mercy&&mercy.DesignCard.Mercy)return 0;
        if(!card.DynamicVars.TryGetValue("Damage",out var variable))return 0;
        var old=variable.PreviewValue;
        try{variable.UpdateCardPreview(card,CardPreviewMode.Normal,target,true);return Math.Max(0,variable.PreviewValue)*PilotCardInfo.Hits(card);}
        finally{variable.PreviewValue=old;}
    }
    private static double Block(CardModel card,Creature owner)
    {
        if(!card.DynamicVars.TryGetValue("Block",out var variable))return 0;
        var old=variable.PreviewValue;
        try{variable.UpdateCardPreview(card,CardPreviewMode.Normal,owner,true);return Math.Max(0,(double)variable.PreviewValue);}
        finally{variable.PreviewValue=old;}
    }
    public static Decision? Choose(Player player,int incoming)
    {
        var hand=PileType.Hand.GetPile(player).Cards;
        var enemies=player.Creature.CombatState!.HittableEnemies.Where(e=>e.IsAlive).ToArray();
        var energy=player.PlayerCombatState!.Energy;var need=Math.Max(0,incoming-player.Creature.Block);
        var choices=new List<Decision>();
        foreach(var card in hand.Where(c=>c.CanPlay())){
            var hpLoss=PilotCardInfo.ImmediateHpLoss(card);
            if(!PilotHealth.Affordable(player.Creature.CurrentHp,hpLoss))continue;
            foreach(var target in card.TargetType==TargetType.AnyEnemy?enemies.Cast<Creature?>():new Creature?[]{null}){
                var cost=Cost(card);var reason="utility";
                var damage=target!=null?(double)Damage(card,target):card.TargetType==TargetType.AllEnemies?enemies.Sum(e=>(double)Damage(card,e)):card.TargetType==TargetType.RandomEnemy&&enemies.Length>0?enemies.Average(e=>(double)Damage(card,e)):0;
                var score=damage/Math.Max(1,cost);
                score+=Math.Min(need,Block(card,player.Creature))*1.6/Math.Max(1,cost);
                {
                    int Amount(string effect)=>PilotCardInfo.Amount(card,effect);
                    score+=Amount("draw")*3+Amount("energy")*8;
                    if(card.Type==CardType.Power)score+=8;
                    if(target!=null&&Amount("vulnerable")>0&&target.GetPowerAmount<VulnerablePower>()==0){
                        // Account for one affordable follow-up and one future attack, not a full tree search.
                        var follow=hand.Where(c=>c!=card&&c.Type==CardType.Attack&&Cost(c)<=energy-cost).Select(c=>(double)Damage(c,target)).DefaultIfEmpty(0).Max();
                        score+=(follow*.5+(Amount("vulnerable")>1?follow*.5:0))/Math.Max(1,cost);
                        if(follow>0)reason="vulnerable-follow-up";
                    }
                    if(target!=null&&Amount("weak")>0)score+=need*.25;
                    if(Amount("poison")>0)score+=Amount("poison")*1.5/Math.Max(1,cost)*(card.TargetType==TargetType.AllEnemies?enemies.Length:1);
                    if(PilotCardInfo.ExhaustsOthers(card)){
                        var engine=player.Creature.GetPowerAmount<FeelNoPainPower>()+player.Creature.Powers.OfType<LegacyPower>().Where(p=>p.Key=="exhaust_damage").Sum(p=>p.Amount);
                        var fuel=hand.Any(c=>c!=card&&c.Type is CardType.Status or CardType.Curse);
                        score+=engine*3+(fuel?4:0);
                        if(engine>0&&fuel)reason="exhaust-engine-fuel";
                    }
                    // Known single-hit paths only; the native preview already includes conditional bonuses.
                    // Multi-hit, reflect and phase effects need a richer solver.
                    if(target!=null&&PilotCardInfo.SimpleAttack(card)&&damage>=target.CurrentHp+target.Block){
                        // Equal lethals should remove an immediate attacker before a harmless
                        // lower-HP target. Covered damage has no extra survival value. Only
                        // verified single-hit paths receive this credit, not speculative kills.
                        score+=100+Math.Min(need,Threat(player,target))*1.6;
                        reason="estimated-lethal";
                    }
                }
                score-=PilotHealth.Penalty(player.Creature.CurrentHp,hpLoss);
                // Do not spend a dormant Block/chain finisher for no estimated payoff.
                if(card is LegacyCard dormant&&dormant.DesignCard.Row.Text("special") is "block_damage" or "chain_damage"&&score<=0)continue;
                choices.Add(new(card,target,score,reason));
            }
        }
        return choices.OrderByDescending(c=>c.Score).ThenBy(c=>c.Card.Id.ToString(),StringComparer.Ordinal).ThenBy(c=>c.Target?.CurrentHp??0).FirstOrDefault();
    }
    public static IDisposable Selector(bool planned=false)
    {
        var method=AccessTools.Method(typeof(CardSelectCmd),"UseSelector",[typeof(ICardSelector),typeof(bool)])??AccessTools.Method(typeof(CardSelectCmd),"UseSelector",[typeof(ICardSelector)]);
        return (IDisposable)method.Invoke(null,method.GetParameters().Length==2?[new FuelSelector(planned),false]:[new FuelSelector(planned)])!;
    }
    public static async Task<object> Probe(RunState run,string kind)
    {
        using(Selector()){
            // Quick-start encounters can contain only one enemy. Explicitly use two for target/area checks.
            await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.NibbitsNormal>().ToMutable(),showTransition:false);
            RunManager.Instance.ActionExecutor.Unpause();
            var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+30000;
            while(run.Players.Single().PlayerCombatState?.Phase!=PlayerTurnPhase.Play){if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Tactical fixture combat did not start.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
            var player=run.Players.Single();var hand=PileType.Hand.GetPile(player);
            foreach(var card in hand.Cards.ToArray())await CardPileCmd.Add(card,PileType.Draw,skipVisuals:true);
            var enemies=player.Creature.CombatState!.HittableEnemies.ToArray();
            foreach(var enemy in enemies){enemy.SetMaxHpInternal(1000);enemy.SetCurrentHpInternal(1000);}
            async Task<LegacyCard> Add(string key,bool upgraded=false){var card=(LegacyCard)player.Creature.CombatState.CreateCard(NativeCards.Get(key),player);if(upgraded)CardCmd.Upgrade(card);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);return card;}
            var upgraded=kind.EndsWith("-upgraded",StringComparison.Ordinal);
            var scenario=upgraded?kind[..^9]:kind;
            if(scenario is "delayed-energy" or "delayed-draw" or "delayed-block" or "delayed-spellweave"){
                var key=scenario switch{"delayed-energy"=>"knight_brave_advance","delayed-draw"=>"ranger_patient_hunter","delayed-spellweave"=>"mage_spellweave",_=>"knight_reinforce"};
                var delayed=await Add(key,upgraded);var description=PilotCardInfo.Describe(delayed);
                var design=delayed.DesignCard;
                if(description.NextEnergy!=design.Amount("nextEnergy")||description.NextDraw!=design.Amount("nextDraw")||description.NextBlock!=design.Amount("nextBlock"))throw new InvalidDataException("Delayed reward metadata missing");
                var chosen=true;
                if(scenario=="delayed-energy"){
                    var alternative=(LegacyCard)run.CreateCard(NativeCards.Get("knight_executioner_axe"),player);
                    chosen=PilotRewards.Pick([PilotCardInfo.Describe(alternative),description],player.Deck.Cards.Select(PilotCardInfo.Describe).ToArray())==1;
                    run.RemoveCard(alternative);
                    if(!chosen)throw new InvalidDataException("Actual delayed Energy reward still undervalued");
                }
                var energy=player.PlayerCombatState!.Energy;
                var blockBefore=player.Creature.Block;
                await BalanceOracle.Play(delayed,delayed.TargetType==TargetType.AnyEnemy?enemies[0]:null);
                var immediateEnergy=player.PlayerCombatState.Energy;
                var immediateBlock=player.Creature.Block-blockBefore;
                var queuedEnergy=player.Creature.GetPowerAmount<EnergyNextTurnPower>();
                var queuedDraw=player.Creature.GetPowerAmount<DrawCardsNextTurnPower>();
                var queuedBlock=player.Creature.GetPowerAmount<BlockNextTurnPower>();
                if(queuedEnergy!=description.NextEnergy||queuedDraw!=description.NextDraw||queuedBlock!=(decimal)description.NextBlock)throw new InvalidDataException("Delayed native powers disagree with reward metadata");
                if(immediateEnergy!=energy-delayed.DesignCard.Cost)throw new InvalidDataException("Delayed Energy was granted immediately");
                await BalanceOracle.End(player,true);
                if(scenario=="delayed-spellweave"){
                    var firstEnergy=player.PlayerCombatState.Energy;var firstHand=hand.Cards.Count;
                    var restored=(LegacyCard)run.LoadCard(delayed.ToSerializable(),player);
                    var roundTrip=restored.EnergyCost.GetWithModifiers(CostModifiers.All)==1&&restored.DesignCard.Rules()==delayed.DesignCard.Rules();run.RemoveCard(restored);
                    if(delayed.Pile?.Type!=PileType.Discard)throw new InvalidDataException("Spellweave must remain reusable");
                    await CardPileCmd.Add(delayed,PileType.Hand,skipVisuals:true);
                    energy=player.PlayerCombatState.Energy;await BalanceOracle.Play(delayed);
                    var replaySpent=energy-player.PlayerCombatState.Energy;
                    var replayPile=delayed.Pile?.Type.ToString();
                    await BalanceOracle.End(player,true);
                    return new{kind,key,upgraded,immediateEnergy,immediateBlock,firstEnergy,firstHand,replaySpent,
                        secondEnergy=player.PlayerCombatState.Energy,secondHand=hand.Cards.Count,queuedEnergy=player.Creature.GetPowerAmount<EnergyNextTurnPower>(),
                        queuedDraw=player.Creature.GetPowerAmount<DrawCardsNextTurnPower>(),roundTrip,pile=replayPile};
                }
                return new{kind,key,upgraded,chosen,description.NextEnergy,description.NextDraw,description.NextBlock,immediateEnergy,deliveredEnergy=player.PlayerCombatState.Energy,deliveredHand=hand.Cards.Count,deliveredBlock=player.Creature.Block};
            }
            if(scenario is "block-finisher" or "chain-finisher" or "exhaust-finisher"){
                var key=scenario switch{"block-finisher"=>"knight_shield_slam","chain-finisher"=>"ranger_finishing_flurry",_=>"knight_ashen_advance"};
                var finisher=await Add(key,upgraded);var target=enemies.Last();
                var before=Choose(player,0);
                if(scenario!="exhaust-finisher"&&before!=null)throw new InvalidDataException("Dormant finisher was selected with zero payoff");
                var expected=0;
                if(scenario=="block-finisher"){
                    expected=8;await CreatureCmd.GainBlock(player.Creature,expected,MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,null);
                }else if(scenario=="chain-finisher"){
                    // Paid attacks establish the real class counter before evaluating the finisher.
                    for(var i=0;i<2;i++){var fuel=await Add("knight_strike");await BalanceOracle.Play(fuel,enemies[0]);}
                    if(player.Creature.GetPower<P_class>()?.AttacksThisTurn!=2)throw new InvalidDataException("Native chain fuel counter missing");
                    expected=finisher.DesignCard.SpecialAmount*2;
                }else{
                    // Native exhaust commands establish a pile, including its damage cap boundary.
                    for(var i=0;i<11;i++){
                        var fuel=player.Creature.CombatState.CreateCard(ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Wound>(),player);
                        await CardPileCmd.Add(fuel,PileType.Hand,skipVisuals:true);
                        await BranchCommands.Exhaust(new BlockingPlayerChoiceContext(),fuel);
                    }
                    expected=finisher.DesignCard.Damage+finisher.DesignCard.SpecialAmount*10;
                }
                target.SetCurrentHpInternal(expected);
                var decision=Choose(player,0)!;
                if(decision.Card!=finisher||decision.Target!=target||decision.Reason!="estimated-lethal")throw new InvalidDataException("Active finisher lethal priority missing");
                var hp=target.CurrentHp;await BalanceOracle.Play(finisher,target);
                return new{kind,key,upgraded,expected,damage=hp-target.CurrentHp,targetDead=!target.IsAlive,dormantDeclined=before==null,decision.Reason};
            }
            if(scenario is "execute" or "guarded" or "marked" or "retained"){
                var key=scenario switch{"execute"=>"knight_executioner_axe","guarded"=>"knight_counter_lunge","marked"=>"ranger_ambush",_=>"knight_patient_riposte"};
                var conditional=await Add(key,upgraded);await Add("knight_strike");
                var target=enemies.Last();var design=conditional.DesignCard;
                var expected=design.Damage;
                var beforeReason="";
                if(scenario=="execute"){
                    expected*=design.SpecialAmount;
                    // Distinguish the exact half-health boundary from one HP above it.
                    enemies[0].SetMaxHpInternal(expected*2);enemies[0].SetCurrentHpInternal(expected+1);
                    target.SetMaxHpInternal(expected*2);target.SetCurrentHpInternal(expected);
                }else if(scenario is "guarded" or "marked"){
                    expected+=design.SpecialAmount;target.SetCurrentHpInternal(expected);
                    beforeReason=Choose(player,0)!.Reason;
                    if(beforeReason=="estimated-lethal")throw new InvalidDataException("Inactive condition received lethal priority");
                    if(scenario=="guarded")await CreatureCmd.GainBlock(player.Creature,2,MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,null);
                    else await PowerCmd.Apply<PoisonPower>(new BlockingPlayerChoiceContext(),target,1,player.Creature,null);
                }else{
                    // Real turn flushing applies intrinsic Retain growth; no direct growth injection.
                    await CreatureCmd.GainBlock(player.Creature,999,MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,null);
                    await BalanceOracle.End(player,false);
                    foreach(var other in hand.Cards.Where(c=>c!=conditional).ToArray())await CardPileCmd.Add(other,PileType.Draw,skipVisuals:true);
                    if(!hand.Cards.Contains(conditional)||conditional.RetainedGrowth!=design.SpecialAmount)throw new InvalidDataException("Native retention growth missing");
                    expected+=design.SpecialAmount;target.SetCurrentHpInternal(expected);
                }
                var decision=Choose(player,0)!;
                if(decision.Card!=conditional||decision.Target!=target||decision.Reason!="estimated-lethal")throw new InvalidDataException("Conditional lethal priority missing");
                var description=PilotCardInfo.Describe(conditional);
                if(description.Special!=design.Row.Text("special")||description.SpecialAmount!=design.SpecialAmount)throw new InvalidDataException("Reward adapter lost conditional metadata");
                var hp=target.CurrentHp;await BalanceOracle.Play(conditional,target);
                return new{kind,key,upgraded,expected,damage=hp-target.CurrentHp,targetDead=!target.IsAlive,beforeReason,decision.Reason,growth=conditional.RetainedGrowth,rewardSpecial=description.Special,rewardAmount=description.SpecialAmount};
            }
            if(kind=="vulnerable"){
                var challenge=await Add("challenge");var strike=await Add("knight_strike");await Add("knight_guard");
                var energy=player.PlayerCombatState!.Energy;var first=Choose(player,0)!;
                if(first.Card!=challenge)throw new InvalidDataException("Tactical pilot missed affordable Vulnerable follow-up.");
                await BalanceOracle.Play(first.Card,first.Target);
                var vulnerable=first.Target!.GetPowerAmount<VulnerablePower>();var hp=first.Target.CurrentHp;
                var second=Choose(player,0)!;
                if(second.Card!=strike||second.Target!=first.Target)throw new InvalidDataException("Tactical pilot missed marked follow-up target.");
                await BalanceOracle.Play(second.Card,second.Target);
                return new{kind,first=challenge.Key,second=strike.Key,vulnerable,followUpDamage=hp-first.Target.CurrentHp,energySpent=energy-player.PlayerCombatState.Energy};
            }
            if(kind=="exhaust"){
                var reforge=await Add("knight_reforge");await Add("knight_strike");
                var wound=player.Creature.CombatState.CreateCard(ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Wound>(),player);await CardPileCmd.Add(wound,PileType.Hand,skipVisuals:true);
                await PowerCmd.Apply<FeelNoPainPower>(new BlockingPlayerChoiceContext(),player.Creature,3,player.Creature,null);
                var decision=Choose(player,0)!;var before=player.Creature.Block;
                if(decision.Card!=reforge)throw new InvalidDataException("Tactical pilot missed exhaust engine and status fuel.");
                await BalanceOracle.Play(reforge);
                if(wound.Pile?.Type!=PileType.Exhaust)throw new InvalidDataException("Tactical selector did not exhaust status fuel.");
                return new{kind,picked=reforge.Key,fuelExhausted=true,blockGained=player.Creature.Block-before};
            }
            if(kind=="lethal"){
                var strike=await Add("knight_strike");await Add("knight_guard");enemies.Last().SetCurrentHpInternal(1);
                var decision=Choose(player,20)!;
                if(decision.Card!=strike||decision.Target!=enemies.Last())throw new InvalidDataException("Tactical pilot missed non-first lethal target.");
                await BalanceOracle.Play(strike,decision.Target);
                return new{kind,picked=strike.Key,targetDead=!enemies.Last().IsAlive,decision.Reason};
            }
            if(kind=="area"){
                var volley=await Add("knight_sweeping_edge");await Add("knight_strike");
                var decision=Choose(player,0)!;
                if(decision.Card!=volley||decision.Target!=null)throw new InvalidDataException("Tactical pilot undervalued area damage.");
                var before=enemies.Select(e=>e.CurrentHp).ToArray();await BalanceOracle.Play(volley);
                return new{kind,picked=volley.Key,damage=enemies.Select((e,i)=>before[i]-e.CurrentHp).ToArray()};
            }
            throw new ArgumentException("Unknown tactical probe.");
        }
    }
    private sealed class FuelSelector(bool planned=false):ICardSelector
    {
        private double Value(CardModel card)=>card.Type is CardType.Status or CardType.Curse?-100:BalanceOracle.Score(card,planned&&card.Owner?.Creature.CombatState!=null?Math.Max(0,Incoming(card.Owner)-card.Owner.Creature.Block):0);
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options,int minSelect,int maxSelect)
        {
            var cards=options.ToArray();var recovery=cards.Length>0&&cards.All(c=>c.Pile?.Type is PileType.Exhaust or PileType.Discard);
            var ordered=recovery?cards.OrderByDescending(Value):cards.OrderBy(Value);
            return Task.FromResult<IEnumerable<CardModel>>(ordered.ThenBy(c=>c.Id.ToString(),StringComparer.Ordinal).Take(planned?Math.Max(minSelect,Math.Min(1,maxSelect)):Math.Min(maxSelect,minSelect)).ToArray());
        }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,IReadOnlyList<CardRewardAlternative> alternatives)=>new(){card=options.OrderByDescending(o=>Value(o.Card)).FirstOrDefault()?.Card};
    }
}
