using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Opt-in disposable item/ownership fixtures; no normal-play entry point.
public static class ItemAuditOracle
{
    private static readonly PlayerChoiceContext Context=new BlockingPlayerChoiceContext();
    private static void Require(bool ok,string text){if(!ok)throw new InvalidDataException(text);}
    private static async Task Wait(Func<bool> done){var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+20000;while(!done()){if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Item audit action.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}}
    public static object Clones()
    {
        var results=new List<object>();
        foreach(var canonical in new BloodlineRelic[]{ModelDb.Relic<BloodlineRelic>(),ModelDb.Relic<RefinedBloodlineRelic>()}){
            var first=canonical.ToMutable();var second=canonical.ToMutable();
            foreach(var name in new[]{"firstAttackTargets","costlyDrawTurns"}){
                var field=AccessTools.Field(typeof(BloodlineRelic),name);
                results.Add(new{relic=canonical.Id.ToString(),field=name,shared=ReferenceEquals(field.GetValue(first),field.GetValue(second)),canonicalShared=ReferenceEquals(field.GetValue(first),field.GetValue(canonical))});
            }
        }
        return results;
    }
    private static async Task<CardModel> Add(Player player,string key,bool up=false){var card=player.Creature.CombatState!.CreateCard(NativeCards.Get(key),player);if(up)CardCmd.Upgrade(card);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);return card;}
    private static async Task Play(CardModel card){var enemy=card.Owner.Creature.CombatState!.GetOpponentsOf(card.Owner.Creature).First(c=>c.IsAlive);var energy=card.Owner.PlayerCombatState!.Energy;var cost=card.EnergyCost.GetWithModifiers(CostModifiers.All);var failure=CardPlayDiagnostics.LastFailure;
        Require(card.TryManualPlay(card.TargetType==TargetType.AnyEnemy?enemy:null),"Paid item play refused.");await Wait(()=>card.Pile?.Type is not (PileType.Hand or PileType.Play)&&NCard.FindOnTable(card)==null&&!RunManager.Instance.ActionExecutor.IsRunning);
        var gained=card is LegacyCard legacy?legacy.DesignCard.Amount("energy"):0;
        Require(CardPlayDiagnostics.LastFailure==failure&&card.Owner.PlayerCombatState.Energy==energy-cost+gained,"Item play payment/diagnostic.");}
    public static async Task<object> Relic(RunState run,string key,bool up)
    {
        var player=run.Players.Single();var combat=player.Creature.CombatState!;foreach(var enemy in combat.GetOpponentsOf(player.Creature)){enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}
        player.Creature.SetMaxHpInternal(10000);player.Creature.SetCurrentHpInternal(10000);
        foreach(var card in player.PlayerCombatState!.Hand.Cards.ToArray())await CardPileCmd.Add(card,PileType.Draw,skipVisuals:true);
        await PlayerCmd.GainEnergy(100,player);
        var relic=player.Relics.OfType<FamilyDropRelic>().Single(r=>r.Key==key);
        var first=player.PlayerCombatState.TurnNumber;var block=player.Creature.Block;var draw=player.PlayerCombatState.Hand.Cards.Count;
        var power="knight_forge_memory";var attack=PlayerBloodlines.Heir(player).classId+"_strike";var skill=PlayerBloodlines.Heir(player).classId+"_guard";
        var plays=new List<object>();
        async Task Paid(string cardKey){var card=await Add(player,cardKey,up);var beforeBlock=player.Creature.Block;var beforeHand=player.PlayerCombatState.Hand.Cards.Count;await Play(card);plays.Add(new{card=cardKey,up,block=player.Creature.Block-beforeBlock,draw=player.PlayerCombatState.Hand.Cards.Count-beforeHand+1,vigor=player.Creature.GetPowerAmount<VigorPower>(),charges=player.Creature.GetPower<P_class>()?.Charges,claimed=relic.CombatClaimed,turn=relic.TriggerTurn,mask=relic.TypeMask,counter=relic.DisplayAmount});}
        switch(key){
            case "heirloom_brooch":case "scholar_bookmark":await Paid(power);await Paid(power);break;
            case "training_dummy":case "guardian_knot":await CreatureCmd.GainBlock(player.Creature,20,ValueProp.Unpowered,null);await Paid(attack);await Paid(attack);break;
            case "triune_crest":await Paid(PlayerBloodlines.Heir(player).classId=="ranger"?attack:skill);await Paid(PlayerBloodlines.Heir(player).classId=="ranger"?attack:skill);break;
            case "family_seal":await Paid(power);await Paid(skill);await Paid(attack);await Paid(power);await Paid(skill);await Paid(attack);break;
            case "threadbound_ledger":await Paid("knight_king_s_ransom");await Paid("knight_king_s_ransom");break;
            case "wayfarer_ribbon":await Add(player,skill);await Paid("ranger_empty_quiver");await Paid("ranger_empty_quiver");break;
            case "legacy_hourglass":case "trail_compass":
                // Actual turn start, not an invented counter callback.
                while(player.PlayerCombatState.TurnNumber<relic.Row.Number("threshold")){CombatManager.Instance.SetReadyToEndTurn(player,false);await Wait(()=>player.PlayerCombatState.Phase==PlayerTurnPhase.Play&&player.PlayerCombatState.TurnNumber>first);first=player.PlayerCombatState.TurnNumber;}
                break;
        }
        var restored=(FamilyDropRelic)RelicModel.FromSerializable(relic.ToSerializable());
        Require(restored.CombatClaimed==relic.CombatClaimed&&restored.TriggerTurn==relic.TriggerTurn&&restored.TypeMask==relic.TypeMask&&restored.MaskTurn==relic.MaskTurn&&restored.Status==relic.Status,"Saved relic state mismatch.");
        await RelicCmd.Replace(relic,restored);
        return new{key,up,plays,block=player.Creature.Block-block,draw=player.PlayerCombatState.Hand.Cards.Count-draw,restored.CombatClaimed,restored.TriggerTurn,restored.TypeMask,restored.MaskTurn,counter=restored.DisplayAmount,turn=player.PlayerCombatState.TurnNumber,energy=player.PlayerCombatState.Energy,vigor=player.Creature.GetPowerAmount<VigorPower>(),charges=player.Creature.GetPower<P_class>()?.Charges,roundTrip=true};
    }
    public static async Task<object> CoopPrepare(RunState run,bool up)
    {
        foreach(var enemy in run.Players[0].Creature.CombatState!.GetOpponentsOf(run.Players[0].Creature)){enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}
        foreach(var player in run.Players.OrderBy(p=>p.NetId)){
            foreach(var card in player.PlayerCombatState!.Hand.Cards.ToArray())await CardPileCmd.Add(card,PileType.Draw,skipVisuals:true);
            await PlayerCmd.GainEnergy(30,player);await Add(player,PlayerBloodlines.Heir(player).classId+"_strike",up);
            if(!up)Require((await PotionCmd.TryToProcure(HeirPotions.Get("triune_draught").ToMutable(),player)).success,"Co-op potion procurement.");
        }
        return new{prepared=true,up};
    }
    public static async Task<object> CoopPotion(RunState run)
    {
        var player=PlayerBloodlines.LocalPlayer!;var potion=player.Potions.OfType<FamilyPotion>().Single(p=>p.Key=="triune_draught");
        var others=run.Players.Where(p=>p!=player).Select(p=>new{p.Creature.Block,hand=p.PlayerCombatState!.Hand.Cards.Count,vigor=p.Creature.GetPowerAmount<VigorPower>(),charges=p.Creature.GetPower<P_class>()?.Charges,potions=p.Potions.Count()}).ToArray();
        var block=player.Creature.Block;var hand=player.PlayerCombatState!.Hand.Cards.Count;var vigor=player.Creature.GetPowerAmount<VigorPower>();var completed=0;var checkpoint=RunManager.Instance.ChecksumTracker.NextId;
        potion.ExecutionFinished+=_=>completed++;potion.EnqueueManualUse(player.Creature);await Wait(()=>completed==1&&!RunManager.Instance.ActionExecutor.IsRunning);
        var after=run.Players.Where(p=>p!=player).Select(p=>new{p.Creature.Block,hand=p.PlayerCombatState!.Hand.Cards.Count,vigor=p.Creature.GetPowerAmount<VigorPower>(),charges=p.Creature.GetPower<P_class>()?.Charges,potions=p.Potions.Count()}).ToArray();
        Require(others.SequenceEqual(after)&&!player.Potions.Contains(potion),"Co-op potion ownership.");
        return new{owner=player.NetId,cls=PlayerBloodlines.Heir(player).classId,block=player.Creature.Block-block,draw=player.PlayerCombatState.Hand.Cards.Count-hand,vigor=player.Creature.GetPowerAmount<VigorPower>()-vigor,charges=player.Creature.GetPower<P_class>()?.Charges,otherOwnersUnchanged=true,completed,checksumNext=checkpoint+1};
    }
    public static async Task<object> PotionEdges(RunState run)
    {
        var player=run.Players.Single();var enemies=player.Creature.CombatState!.GetOpponentsOf(player.Creature).ToArray();
        foreach(var potion in player.Potions.ToArray())await PotionCmd.Discard(potion);
        foreach(var key in new[]{"borrowed_breath","briar_vial","triune_draught"})Require((await PotionCmd.TryToProcure(HeirPotions.Get(key).ToMutable(),player)).success,"Empty belt procurement.");
        var slots=player.PotionSlots.ToArray();var rejected=await PotionCmd.TryToProcure(HeirPotions.Get("borrowed_breath").ToMutable(),player);
        Require(!rejected.success&&player.PotionSlots.SequenceEqual(slots),"Full belt changed.");
        var breath=player.Potions.OfType<FamilyPotion>().Single(p=>p.Key=="borrowed_breath");var briar=player.Potions.OfType<FamilyPotion>().Single(p=>p.Key=="briar_vial");
        int Uses()=>CombatManager.Instance.History.Entries.OfType<PotionUsedEntry>().Count(e=>e.Potion==breath||e.Potion==briar);
        async Task Cancel(PotionModel potion,MegaCrit.Sts2.Core.Entities.Creatures.Creature? target){Require(!potion.IsValidTarget(target),"Cancellation fixture target was valid.");potion.EnqueueManualUse(target);await Wait(()=>!potion.IsQueued&&!RunManager.Instance.ActionExecutor.IsRunning);Require(Uses()==0&&player.PotionSlots.SequenceEqual(slots),"Invalid target consumed potion.");}
        await Cancel(briar,player.Creature);await Cancel(breath,enemies.First());
        var hp=enemies.Select(e=>e.CurrentHp).ToArray();
        try{enemies.First().SetCurrentHpInternal(0);Require(!briar.IsValidTarget(enemies.First()),"Dead target validity.");if(enemies.Length>1)await Cancel(briar,enemies.First());
            // Validate an empty battlefield without yielding a frame: otherwise the
            // native victory transition correctly ends combat and cancels all actions.
            foreach(var enemy in enemies)enemy.SetCurrentHpInternal(0);Require(!briar.IsValidTarget(null)&&enemies.All(e=>!briar.IsValidTarget(e)),"Empty battlefield target validity.");}
        finally{for(var i=0;i<enemies.Length;i++)enemies[i].SetCurrentHpInternal(hp[i]);}
        // Construct both genuine native actions before enqueueing; the duplicate must
        // cancel against the now-empty slot, without consuming its neighbouring item.
        var one=new UsePotionAction(breath,player.Creature,true);var two=new UsePotionAction(breath,player.Creature,true);var block=player.Creature.Block;
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(one);RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(two);
        await Wait(()=>Uses()==1&&!RunManager.Instance.ActionExecutor.IsRunning);
        Require(player.Creature.Block-block==6&&!player.Potions.Contains(breath)&&player.Potions.Contains(briar)&&player.Potions.Count()==2,"Duplicate queue effect/consumption.");
        return new{fullBeltRejected=true,invalidTargetCanceled=true,deadTargetCanceled=enemies.Length>1,zeroAliveEnemiesRejectedByTargetValidation=true,duplicateUses=Uses(),duplicateBlock=player.Creature.Block-block,remaining=player.Potions.Select(p=>p.Id.ToString()).ToArray()};
    }
    public static async Task<object> RelicEdges(RunState run)
    {
        var player=run.Players.Single();foreach(var enemy in player.Creature.CombatState!.GetOpponentsOf(player.Creature)){enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}player.Creature.SetMaxHpInternal(10000);player.Creature.SetCurrentHpInternal(10000);
        foreach(var canonical in HeirDropRelics.All)await RelicCmd.Obtain(canonical.ToMutable(),player);
        FamilyDropRelic Relic(string key)=>player.Relics.OfType<FamilyDropRelic>().Single(r=>r.Key==key);
        async Task Sequence(){await PlayerCmd.GainEnergy(40,player);foreach(var card in player.PlayerCombatState!.Hand.Cards.ToArray())await CardPileCmd.Add(card,PileType.Draw,skipVisuals:true);await CreatureCmd.GainBlock(player.Creature,20,ValueProp.Unpowered,null);
            foreach(var key in new[]{"knight_forge_memory",PlayerBloodlines.Heir(player).classId+"_guard",PlayerBloodlines.Heir(player).classId+"_strike","knight_king_s_ransom","knight_king_s_ransom"})await Play(await Add(player,key));
            await Add(player,PlayerBloodlines.Heir(player).classId+"_guard");await Play(await Add(player,"ranger_empty_quiver"));}
        await Sequence();var first=player.PlayerCombatState!.TurnNumber;
        var firstCounters=HeirRelicOracle.Counters(run);var saved=System.Text.Json.JsonSerializer.Serialize(firstCounters);await HeirRelicOracle.RoundTrip(run);Require(saved==System.Text.Json.JsonSerializer.Serialize(HeirRelicOracle.Counters(run)),"Stacked counter round trip.");
        CombatManager.Instance.SetReadyToEndTurn(player,false);await Wait(()=>player.PlayerCombatState.Phase==PlayerTurnPhase.Play&&player.PlayerCombatState.TurnNumber>first);
        foreach(var key in new[]{"training_dummy","threadbound_ledger","wayfarer_ribbon","family_seal"})Require(Relic(key).TriggerTurn==first,"End-turn discard/exhaust claimed a new turn.");
        Require(Relic("family_seal").TypeMask==0&&Relic("trail_compass").CombatClaimed,"Turn-start reset/compass.");
        await Sequence();var second=player.PlayerCombatState.TurnNumber;
        foreach(var key in new[]{"training_dummy","threadbound_ledger","wayfarer_ribbon","family_seal"})Require(Relic(key).TriggerTurn==second,"Per-turn relic failed to rearm.");
        foreach(var key in new[]{"heirloom_brooch","guardian_knot","scholar_bookmark","triune_crest"})Require(Relic(key).CombatClaimed,"Combat cap lost.");
        var beforeReacquire=HeirRelicOracle.Counters(run);await RelicCmd.Remove(Relic("heirloom_brooch"));await RelicCmd.Obtain(HeirDropRelics.Get("heirloom_brooch").ToMutable(),player);Require(!Relic("heirloom_brooch").CombatClaimed,"Fresh relic did not reset.");
        var block=player.Creature.Block;await Play(await Add(player,"knight_forge_memory"));Require(player.Creature.Block-block==9&&Relic("heirloom_brooch").CombatClaimed,"Reacquired relic trigger.");
        var original=player.RelicGrabBag.ToSerializable();var fallback=new List<string>();
        try{var empty=player.RelicGrabBag.ToSerializable();foreach(var list in empty.RelicIdLists.Values)list.Clear();player.RelicGrabBag.LoadFromSerializable(empty);foreach(var rarity in new[]{RelicRarity.Common,RelicRarity.Uncommon,RelicRarity.Rare}){var reward=new MegaCrit.Sts2.Core.Rewards.RelicReward(rarity,player);reward.Populate();Require(reward.Relic!.Id==RelicFactory.FallbackRelic.Id,"Depleted native bag fallback.");fallback.Add(reward.Relic.Id.ToString());}}
        finally{player.RelicGrabBag.LoadFromSerializable(original);}
        return new{firstCounters,secondCounters=beforeReacquire,roundTrip=true,endTurnDidNotClaim=true,rearmed=true,reacquiredBlock=player.Creature.Block-block,fallback};
    }
    public static async Task<object> AntiqueEmpty(RunState run)
    {
        var player=run.Players.Single();var bloodline=player.Relics.OfType<BloodlineRelic>().Single();var bag=run.SharedRelicGrabBag.ToSerializable();var before=player.Relics.Count();string? diagnostic=null;
        try{var empty=run.SharedRelicGrabBag.ToSerializable();foreach(var list in empty.RelicIdLists.Values)list.Clear();run.SharedRelicGrabBag.LoadFromSerializable(empty);bloodline.StartingRelicsGranted=false;try{await bloodline.AfterActEntered();}catch(Exception error){diagnostic=error.ToString();}}
        finally{run.SharedRelicGrabBag.LoadFromSerializable(bag);}
        return new{diagnostic,granted=player.Relics.Count()-before,bloodline.StartingRelicsGranted,added=player.Relics.Skip(before).Select(r=>r.Id.ToString()).ToArray(),rarities=player.Relics.Skip(before).Select(r=>r.Rarity.ToString()).ToArray()};
    }
    public static async Task<object> CoopAttack(RunState run,bool up)
    {
        var player=PlayerBloodlines.LocalPlayer!;var enemy=player.Creature.CombatState!.GetOpponentsOf(player.Creature).First(c=>c.IsAlive);var before=enemy.GetPowerAmount<WeakPower>();
        var card=player.PlayerCombatState!.Hand.Cards.OfType<LegacyCard>().Single(c=>c.Key==PlayerBloodlines.Heir(player).classId+"_strike"&&c.IsUpgraded==up);
        var checkpoint=RunManager.Instance.ChecksumTracker.NextId;await Play(card);
        return new{owner=player.NetId,up,weak=enemy.GetPowerAmount<WeakPower>()-before,checksumNext=checkpoint+1};
    }
}
