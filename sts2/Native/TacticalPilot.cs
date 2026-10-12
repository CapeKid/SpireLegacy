using HarmonyLib;
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
    private static int Cost(CardModel card)=>card.EnergyCost.CostsX?card.Owner.PlayerCombatState!.Energy:Math.Max(0,card.EnergyCost.GetWithModifiers(CostModifiers.All));
    private static decimal Damage(CardModel card,Creature target)
    {
        if(card is LegacyCard mercy&&mercy.DesignCard.Mercy)return 0;
        if(!card.DynamicVars.TryGetValue("Damage",out var variable))return 0;
        var old=variable.PreviewValue;
        try{variable.UpdateCardPreview(card,CardPreviewMode.Normal,target,true);return Math.Max(0,variable.PreviewValue)*(card is LegacyCard legacy?Math.Max(1,legacy.DesignCard.Row.Number("hits")):1);}
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
            foreach(var target in card.TargetType==TargetType.AnyEnemy?enemies.Cast<Creature?>():new Creature?[]{null}){
                var cost=Cost(card);var reason="utility";
                var damage=target!=null?(double)Damage(card,target):card.TargetType==TargetType.AllEnemies?enemies.Sum(e=>(double)Damage(card,e)):card.TargetType==TargetType.RandomEnemy&&enemies.Length>0?enemies.Average(e=>(double)Damage(card,e)):0;
                var score=damage/Math.Max(1,cost);
                score+=Math.Min(need,Block(card,player.Creature))*1.6/Math.Max(1,cost);
                if(card is LegacyCard lc){
                    var d=lc.DesignCard;
                    score+=d.Amount("draw")*3+d.Amount("energy")*8;
                    if(card.Type==CardType.Power)score+=8;
                    if(target!=null&&d.Amount("vulnerable")>0&&target.GetPowerAmount<VulnerablePower>()==0){
                        // Account for one affordable follow-up and one future attack, not a full tree search.
                        var follow=hand.Where(c=>c!=card&&c.Type==CardType.Attack&&Cost(c)<=energy-cost).Select(c=>(double)Damage(c,target)).DefaultIfEmpty(0).Max();
                        score+=(follow*.5+(d.Amount("vulnerable")>1?follow*.5:0))/Math.Max(1,cost);
                        if(follow>0)reason="vulnerable-follow-up";
                    }
                    if(target!=null&&d.Amount("weak")>0)score+=need*.25;
                    if(d.Amount("poison")>0)score+=d.Amount("poison")*1.5/Math.Max(1,cost)*(card.TargetType==TargetType.AllEnemies?enemies.Length:1);
                    if(lc.DesignCard.Row.Text("special") is "exhaust_one" or "exhaust_nonattacks"){
                        var engine=player.Creature.GetPowerAmount<FeelNoPainPower>()+player.Creature.Powers.OfType<LegacyPower>().Where(p=>p.Key=="exhaust_damage").Sum(p=>p.Amount);
                        var fuel=hand.Any(c=>c!=card&&c.Type is CardType.Status or CardType.Curse);
                        score+=engine*3+(fuel?4:0);
                        if(engine>0&&fuel)reason="exhaust-engine-fuel";
                    }
                    // Single-hit estimates only. Multi-hit/execute/reflect/phase effects need a richer solver.
                    if(target!=null&&lc.DesignCard.Row.Number("hits")==1&&lc.DesignCard.Row.Text("special")=="none"&&damage>=target.CurrentHp+target.Block){score+=100;reason="estimated-lethal";}
                }
                choices.Add(new(card,target,score,reason));
            }
        }
        return choices.OrderByDescending(c=>c.Score).ThenBy(c=>c.Card.Id.ToString(),StringComparer.Ordinal).ThenBy(c=>c.Target?.CurrentHp??0).FirstOrDefault();
    }
    public static IDisposable Selector()
    {
        var method=AccessTools.Method(typeof(CardSelectCmd),"UseSelector",[typeof(ICardSelector),typeof(bool)])??AccessTools.Method(typeof(CardSelectCmd),"UseSelector",[typeof(ICardSelector)]);
        return (IDisposable)method.Invoke(null,method.GetParameters().Length==2?[new FuelSelector(),false]:[new FuelSelector()])!;
    }
    public static async Task<object> Probe(RunState run,string kind)
    {
        using(Selector()){
            var player=run.Players.Single();var hand=PileType.Hand.GetPile(player);
            foreach(var card in hand.Cards.ToArray())await CardPileCmd.Add(card,PileType.Draw,skipVisuals:true);
            var enemies=player.Creature.CombatState!.HittableEnemies.ToArray();
            foreach(var enemy in enemies){enemy.SetMaxHpInternal(1000);enemy.SetCurrentHpInternal(1000);}
            async Task<LegacyCard> Add(string key){var card=(LegacyCard)player.Creature.CombatState.CreateCard(NativeCards.Get(key),player);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);return card;}
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
                var volley=await Add("volley");await Add("knight_strike");
                var decision=Choose(player,0)!;
                if(decision.Card!=volley||decision.Target!=null)throw new InvalidDataException("Tactical pilot undervalued area damage.");
                var before=enemies.Select(e=>e.CurrentHp).ToArray();await BalanceOracle.Play(volley);
                return new{kind,picked=volley.Key,damage=enemies.Select((e,i)=>before[i]-e.CurrentHp).ToArray()};
            }
            throw new ArgumentException("Unknown tactical probe.");
        }
    }
    private sealed class FuelSelector:ICardSelector
    {
        private static double Value(CardModel card)=>card.Type is CardType.Status or CardType.Curse?-100:BalanceOracle.Score(card,0);
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options,int minSelect,int maxSelect)
        {
            var cards=options.ToArray();var recovery=cards.Length>0&&cards.All(c=>c.Pile?.Type is PileType.Exhaust or PileType.Discard);
            var ordered=recovery?cards.OrderByDescending(Value):cards.OrderBy(Value);
            return Task.FromResult<IEnumerable<CardModel>>(ordered.ThenBy(c=>c.Id.ToString(),StringComparer.Ordinal).Take(Math.Min(maxSelect,minSelect)).ToArray());
        }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,IReadOnlyList<CardRewardAlternative> alternatives)=>new(){card=options.OrderByDescending(o=>Value(o.Card)).FirstOrDefault()?.Card};
    }
}
