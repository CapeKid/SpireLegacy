using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Bounded effect isolation, reachable only through the disposable test bridge.
public static class BalanceAuditOracle
{
    public static readonly Dictionary<string,string[]> Builds=new(){
        ["armor"]=["fortify","knight_shield_slam","knight_living_fortress","knight_double_guard"],
        ["exhaust"]=["knight_second_wind","knight_burnished_legacy","knight_cast_off","knight_forge_sparks"],
        ["blood"]=["knight_bloodied_blade","knight_forged_in_pain","knight_blood_price","knight_veteran_training"],
        ["strength"]=["oath","knight_sword_dance","knight_ancestral_fury","knight_warlord_s_command"],
        ["sequencing"]=["mage_warding_verse","mage_rune_familiar","reservoir","mage_spell_threads"],
        ["mage-exhaust"]=["mage_ash_reading","mage_secret_thesis","mage_transmutation","mage_returned_prophecy"],
        ["mage-retained"]=["mage_tower_library","mage_stored_ember","mage_crystal_seed","mage_eternal_bookmark"],
        ["delayed"]=["mage_spellweave","mage_comet_calendar","knight_brave_advance","study"],
        ["discard"]=["ranger_sort_quiver","ranger_pocket_wind","ranger_spring_nock","ranger_light_pack"],
        ["poison"]=["ranger_poison_tip","ranger_razor_fletching","ranger_double_dose","ranger_poisoned_arsenal"],
        ["retention"]=["ranger_perch_discipline","ranger_long_vigil","mage_stored_ember","mage_crystal_seed"],
        ["chain"]=["ranger_rapid_fire","ranger_tracking_rhythm","ranger_arrow_cascade","quiver"]
    };
    private static readonly PileType[] Piles=[PileType.Hand,PileType.Draw,PileType.Discard,PileType.Exhaust,PileType.Play];
    private static async Task<List<CardModel>> Setup(Player player,bool upgraded,bool native)
    {
        var combat=player.Creature.CombatState!;
        await CardPileCmd.RemoveFromCombat(CardPile.GetCards(player,Piles).ToArray(),skipVisuals:true);
        foreach(var old in player.Creature.Powers.OfType<LegacyPower>().Where(p=>p.Key=="discard_block").ToArray())await PowerCmd.Remove(old);
        await CardEffects.Apply(new BlockingPlayerChoiceContext(),"discard_block",player.Creature,2,player.Creature,null);
        var copies=new List<CardModel>();
        foreach(var _ in Enumerable.Range(0,2)){
            var card=combat.CreateCard(native?ModelDb.Card<CalculatedGamble>():NativeCards.Get("ranger_trail_reset"),player);
            if(upgraded)CardCmd.Upgrade(card);
            await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);copies.Add(card);
        }
        await CardPileCmd.Add(combat.CreateCard(ModelDb.Card<Reflex>(),player),PileType.Hand,skipVisuals:true);
        return copies;
    }
    public static async Task<object> ResetLoop(RunState run,bool upgraded,bool native,IDisposable selector)
    {
        using(selector) {
            var player=PlayerBloodlines.LocalPlayer!;
            var combat=player.Creature.CombatState!;
            foreach(var enemy in combat.GetOpponentsOf(player.Creature)){enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}
            // Native Reflex is a legal cross-color enabler. Its discard draw can
            // replenish the exchange; no injected Energy or forged reward hooks.
            var copies=await Setup(player,upgraded,native);
            var before=player.PlayerCombatState!.Energy;var block=player.Creature.Block;
            var peers=run.Players.Where(p=>p!=player).Select(p=>new{p.NetId,energy=p.PlayerCombatState!.Energy,block=p.Creature.Block,hand=PileType.Hand.GetPile(p).Cards.Count}).ToArray();
            var samples=new List<object>();
            for(var i=0;i<12;i++){
                var next=PileType.Hand.GetPile(player).Cards.FirstOrDefault(c=>copies.Contains(c)&&c.CanPlay());
                if(next==null)break;
                await BalanceOracle.Play(next);
                samples.Add(new{energy=player.PlayerCombatState.Energy,block=player.Creature.Block,hand=PileType.Hand.GetPile(player).Cards.Count,exhaust=copies.Count(c=>c.Pile?.Type==PileType.Exhaust)});
            }
            var isolated=peers.All(p=>{var peer=run.Players.Single(x=>x.NetId==p.NetId);return peer.PlayerCombatState!.Energy==p.energy&&peer.Creature.Block==p.block&&PileType.Hand.GetPile(peer).Cards.Count==p.hand;});
            var saved=copies.Select(c=>{var loaded=run.LoadCard(c.ToSerializable(),player);var result=loaded.Keywords.Contains(CardKeyword.Exhaust)==c.Keywords.Contains(CardKeyword.Exhaust);run.RemoveCard(loaded);return result;}).All(x=>x);
            return new{owner=player.NetId,upgraded,native,plays=samples.Count,energySpent=before-player.PlayerCombatState.Energy,blockGained=player.Creature.Block-block,exhausted=copies.Count(c=>c.Pile?.Type==PileType.Exhaust),roundTrip=saved,ownerIsolated=isolated,samples};
        }
    }
    public static async Task<object> CoopPrepare(RunState run,bool upgraded)
    {
        foreach(var enemy in run.Players[0].Creature.CombatState!.GetOpponentsOf(run.Players[0].Creature)){enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}
        foreach(var player in run.Players.OrderBy(p=>p.NetId))await Setup(player,upgraded,false);
        return new{upgraded,owners=run.Players.Count};
    }
    public static async Task<object> CoopPlay(RunState run,bool upgraded)
    {
        var player=PlayerBloodlines.LocalPlayer!;
        var peers=run.Players.Where(p=>p!=player).Select(p=>new{p,energy=p.PlayerCombatState!.Energy,block=p.Creature.Block,hand=PileType.Hand.GetPile(p).Cards.Count,charges=p.Creature.GetPower<P_class>()!.Charges}).ToArray();
        var cards=PileType.Hand.GetPile(player).Cards.OfType<LegacyCard>().Where(c=>c.Key=="ranger_trail_reset").ToArray();
        if(cards.Length!=2)throw new InvalidDataException("Expected two prepared Trail Reset cards.");
        var before=player.PlayerCombatState!.Energy;var block=player.Creature.Block;
        foreach(var card in cards)await BalanceOracle.Play(card);
        var isolated=peers.All(o=>o.p.PlayerCombatState!.Energy==o.energy&&o.p.Creature.Block==o.block&&PileType.Hand.GetPile(o.p).Cards.Count==o.hand&&o.p.Creature.GetPower<P_class>()!.Charges==o.charges);
        if(!isolated||cards.Any(c=>c.Pile?.Type!=PileType.Exhaust))throw new InvalidDataException("Reset loop escaped Exhaust or affected another owner.");
        return new{owner=player.NetId,cls=PlayerBloodlines.Heir(player).classId,upgraded,energySpent=before-player.PlayerCombatState.Energy,blockGained=player.Creature.Block-block,exhausted=2,ownerIsolated=isolated,checksumNext=RunManager.Instance.ChecksumTracker.NextId,checksumEnabled=RunManager.Instance.ChecksumTracker.IsEnabled};
    }
    public static object References()
    {
        var cards=new CardModel[]{ModelDb.Card<StrikeIronclad>(),ModelDb.Card<DefendIronclad>(),ModelDb.Card<Prepared>(),ModelDb.Card<Acrobatics>(),ModelDb.Card<Adrenaline>(),ModelDb.Card<CalculatedGamble>(),ModelDb.Card<Backflip>(),ModelDb.Card<FlashOfSteel>(),ModelDb.Card<Bloodletting>(),ModelDb.Card<Turbo>(),ModelDb.Card<WellLaidPlans>(),ModelDb.Card<DodgeAndRoll>(),ModelDb.Card<ChargeBattery>(),ModelDb.Card<LegSweep>(),ModelDb.Card<Buffer>(),ModelDb.Card<Deflect>(),ModelDb.Card<Finisher>(),ModelDb.Card<Bludgeon>(),ModelDb.Card<Pyre>()};
        return cards.SelectMany(c=>new[]{false,true}.Select(up=>{var card=c.ToMutable();if(up)CardCmd.Upgrade(card);return new{id=card.Id.ToString(),upgraded=up,cost=card.EnergyCost.GetWithModifiers(CostModifiers.All),rarity=card.Rarity.ToString(),keywords=card.Keywords.Select(k=>k.ToString()).ToArray(),vars=card.DynamicVars.Select(v=>new{name=v.Key,value=v.Value.BaseValue}).ToArray()};})).ToArray();
    }
    public static async Task<object> NativeFlashLoop(RunState run,bool upgraded)
    {
        var player=run.Players.Single();var combat=player.Creature.CombatState!;
        await CardPileCmd.RemoveFromCombat(CardPile.GetCards(player,Piles).ToArray(),skipVisuals:true);
        var enemy=combat.GetOpponentsOf(player.Creature).First(e=>e.IsAlive);enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);
        var cards=new List<CardModel>();
        for(var i=0;i<2;i++){var card=combat.CreateCard(ModelDb.Card<FlashOfSteel>(),player);if(upgraded)CardCmd.Upgrade(card);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);cards.Add(card);}
        var energy=player.PlayerCombatState!.Energy;var hp=enemy.CurrentHp;
        for(var i=0;i<12;i++)await BalanceOracle.Play(cards.First(c=>c.Pile?.Type==PileType.Hand),enemy);
        return new{plays=12,energySpent=energy-player.PlayerCombatState.Energy,damage=hp-enemy.CurrentHp,exhausted=cards.Count(c=>c.Pile?.Type==PileType.Exhaust)};
    }
}
