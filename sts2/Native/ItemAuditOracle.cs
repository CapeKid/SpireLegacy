using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
        }
        return new{prepared=true,up};
    }
    public static async Task<object> CoopAttack(RunState run,bool up)
    {
        var player=PlayerBloodlines.LocalPlayer!;var enemy=player.Creature.CombatState!.GetOpponentsOf(player.Creature).First(c=>c.IsAlive);var before=enemy.GetPowerAmount<WeakPower>();
        var card=player.PlayerCombatState!.Hand.Cards.OfType<LegacyCard>().Single(c=>c.Key==PlayerBloodlines.Heir(player).classId+"_strike"&&c.IsUpgraded==up);
        var checkpoint=RunManager.Instance.ChecksumTracker.NextId;await Play(card);
        return new{owner=player.NetId,up,weak=enemy.GetPowerAmount<WeakPower>()-before,checksumNext=checkpoint+1};
    }
}
