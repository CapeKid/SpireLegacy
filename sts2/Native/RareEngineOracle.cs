using System.Text.Json;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace SpireLegacy;

public static partial class PowerAuditOracle
{
    public static async Task<object> RareEngines(RunState run,string key,bool up,string mode,IDisposable selector)
    {
        using(selector){
            var player=run.Players.Single();var enemies=await Setup(player);var owner=player.Creature;
            var hand=PileType.Hand.GetPile(player);var cls=owner.GetPower<P_class>()!;
            var card=(LegacyCard)await Add(player,NativeCards.Get(key),up);var d=card.DesignCard;
            var restored=(LegacyCard)run.LoadCard(card.ToSerializable(),player);
            Require(restored.IsUpgraded==up&&restored.Id==card.Id&&restored.DesignCard.Rules()==d.Rules(),"Rare engine card round trip.");run.RemoveCard(restored);
            if(mode=="late-setup")for(var i=0;i<3;i++)await Play(await Add(player,ModelDb.Card<StrikeIronclad>()),enemies[0]);
            await Play(card);if(mode=="stacked")await Play(await Add(player,NativeCards.Get(key),up));
            var power=owner.Powers.OfType<LegacyPower>().Single(p=>p.Key==d.Row.Text("power"));
            void Codec(){
                BetaMainCompatibility.CacheSavedProperties(power.GetType());
                var json=JsonSerializer.Serialize(SavedProperties.From(power),new JsonSerializerOptions{IncludeFields=true});
                var copy=(LegacyPower)ModelDb.GetById<PowerModel>(power.Id).ToMutable();
                JsonSerializer.Deserialize<SavedProperties>(json,new JsonSerializerOptions{IncludeFields=true})!.Fill(copy);
                Require(copy.RetainedLastTurn==power.RetainedLastTurn&&copy.EchoTriggered==power.EchoTriggered&&copy.SkillsThisTurn==power.SkillsThisTurn&&copy.MomentumAttacks==power.MomentumAttacks,"Rare engine property codec.");
                var clone=(LegacyPower)power.MutableClone();clone.RetainedLastTurn=!power.RetainedLastTurn;clone.EchoTriggered=!power.EchoTriggered;clone.MomentumAttacks=99;
                Require(power.MomentumAttacks!=99&&clone.RetainedLastTurn!=power.RetainedLastTurn&&clone.EchoTriggered!=power.EchoTriggered,"Rare engine clone shares counters.");
            }
            async Task ClearDamageModifiers(){
                foreach(var p in owner.Powers.Where(p=>p.Type==MegaCrit.Sts2.Core.Entities.Powers.PowerType.Debuff).ToArray())await PowerCmd.Remove(p);
                foreach(var enemy in enemies){foreach(var p in enemy.Powers.ToArray())await PowerCmd.Remove(p);enemy.LoseBlockInternal(enemy.Block);}
            }
            async Task Skill()=>await Play(await Add(player,ModelDb.Card<DefendIronclad>()));
            if(key=="mage_eternal_bookmark"){
                Require(owner.GetPower<P_well_laid>()==null,"Bookmark must not supply retention.");
                var retained=0;
                if(mode=="measured"){
                    var common=(LegacyCard)await Add(player,NativeCards.Get("ranger_measured_breath"),up);await Play(common);
                    for(var i=0;i<3;i++)await Add(player,ModelDb.Card<DefendIronclad>());
                    retained=common.DesignCard.Magic;
                }else if(mode!="empty"){
                    retained=mode=="hand-cap"?CardPile.MaxCardsInHand:1;
                    for(var i=0;i<retained;i++)await Add(player,NativeCards.Get("mage_stored_ember"));
                }
                if(mode=="no-draw"){
                    await power.AfterFlush(Context,player,[],hand.Cards.ToArray());Codec();
                    await Apply(player,"no_draw",1);var before=hand.Cards.Count;await power.AfterPlayerTurnStart(Context,player);
                    Require(hand.Cards.Count==before&&!power.RetainedLastTurn,"No Draw must block and consume the retention payout.");
                    await PowerCmd.Remove(owner.GetPower<NoDrawPower>()!);await power.AfterPlayerTurnStart(Context,player);
                    Require(hand.Cards.Count==before,"Blocked payout repeated after No Draw removal.");
                    return new{key,up,mode,drawn=0,noCatchUp=true,saveRoundTrip=true};
                }
                if(retained>0){await power.AfterFlush(Context,player,[],hand.Cards.ToArray());Codec();}
                await BalanceOracle.End(player,true);
                var expected=Math.Min(CardPile.MaxCardsInHand,retained+5+(retained>0?power.Amount:0));
                Require(hand.Cards.Count==expected&&!power.RetainedLastTurn,"Bookmark actual retain/turn-start draw differs.");
                if(mode=="hand-cap"){
                    await CardPileCmd.Add(hand.Cards.ToArray(),PileType.Discard,skipVisuals:true);await power.AfterPlayerTurnStart(Context,player);
                    Require(hand.Cards.Count==0,"Full-hand payout repeated after space became available.");
                }else{
                    if(owner.GetPower<P_well_laid>() is {} measured)await PowerCmd.Remove(measured);
                    await CardPileCmd.Add(hand.Cards.Where(c=>c.Keywords.Contains(CardKeyword.Retain)).ToArray(),PileType.Discard,skipVisuals:true);
                    await BalanceOracle.End(player,true);Require(hand.Cards.Count==5,"Bookmark paid again without retention.");
                }
                return new{key,up,mode,retained,firstHand=expected,bonusDrawRequested=retained>0?power.Amount:0,turnBoundary=true,saveRoundTrip=true};
            }
            if(key=="mage_archmage_thesis"){
                Require(owner.Block==0,"Thesis must not grant initial Block.");
                if(mode=="woven")await Play(await Add(player,NativeCards.Get("mage_woven_incantation"),up));
                if(mode=="burst-skills"){
                    await Apply(player,"burst",1);await Skill();Require(power.SkillsThisTurn==2&&owner.GetPower<P_thesis_ready>()==null,"Replayed Skills count as two plays.");await Skill();
                }else{
                    await Skill();await Skill();Require(owner.GetPower<P_thesis_ready>()==null,"Thesis triggered before third Skill.");await Skill();
                }
                Require(power.EchoTriggered&&power.SkillsThisTurn==3&&owner.GetPower<P_thesis_ready>()?.Amount==power.Amount,"Thesis third-Skill readiness/stack differs.");Codec();
                if(mode=="woven")Require(owner.Block==15+(up?4:3),"Woven Incantation defense changed.");
                if(mode=="expire"){
                    await BalanceOracle.End(player,true);Require(owner.GetPower<P_thesis_ready>()==null&&!power.EchoTriggered&&power.SkillsThisTurn==0,"Unspent replay did not expire/reset.");
                    await PlayerCmd.GainEnergy(30,player);await Skill();await Skill();await Skill();
                }
                await ClearDamageModifiers();
                if(mode=="double-tap")await Apply(player,"double_tap",1);
                var attack=await Add(player,ModelDb.Card<TwinStrike>());var beforeAttacks=cls.AttacksThisTurn;var beforeHand=hand.Cards.Count;var hp=enemies[0].CurrentHp;
                var plays=1+power.Amount+(mode=="double-tap"?1:0);await Play(attack,enemies[0]);
                Require(hp-enemies[0].CurrentHp==attack.DynamicVars.Damage.BaseValue*2*plays&&cls.AttacksThisTurn-beforeAttacks==plays,"Thesis replay must cover the complete multi-hit Attack.");
                Require(hand.Cards.Count-beforeHand+1==2&&owner.GetPower<P_thesis_ready>()==null,"Thesis duplicated Arcane Flow or failed to consume readiness.");
                for(var i=0;i<3;i++)await Skill();Require(owner.GetPower<P_thesis_ready>()==null,"Thesis retriggered in same turn.");
                beforeAttacks=cls.AttacksThisTurn;await Play(await Add(player,ModelDb.Card<StrikeIronclad>()),enemies[0]);Require(cls.AttacksThisTurn==beforeAttacks+1,"Later Attack received another replay.");
                if(mode=="next-turn"){
                    await BalanceOracle.End(player,true);await PlayerCmd.GainEnergy(30,player);
                    Require(!power.EchoTriggered&&power.SkillsThisTurn==0,"Used Thesis did not reset next turn.");
                    await Skill();await Skill();await Skill();Require(owner.GetPower<P_thesis_ready>()?.Amount==power.Amount,"Thesis could not ready next turn.");
                }
                return new{key,up,mode,attackPlays=plays,hits=2*plays,oncePerTurn=true,flowDraw=2,saveRoundTrip=true};
            }
            if(key=="ranger_hunt_without_end"){
                Require(owner.GetPowerAmount<VigorPower>()==0,"Hunt must not grant initial Vigor.");
                if(mode=="tracking")await Play(await Add(player,NativeCards.Get("ranger_tracking_rhythm"),up));
                var count=mode is "zero" or "late-setup"?0:mode=="one"?1:mode=="cap"?5:mode=="replay"?1:3;
                if(mode=="replay")await Apply(player,"double_tap",1);
                for(var i=0;i<count;i++)await Play(await Add(player,ModelDb.Card<StrikeIronclad>()),enemies[0]);
                var counted=Math.Min(3,mode=="late-setup"?3:mode=="replay"?2:count);
                Require(power.MomentumAttacks==counted,"Hunt counted cards instead of resolved Attack plays, or exceeded its cap.");Codec();
                var existing=owner.GetPowerAmount<VigorPower>();Require(existing==(mode=="tracking"?(up?3:2):0),"Tracking Rhythm immediate Vigor changed.");
                await BalanceOracle.End(player,true);var vigor=owner.GetPowerAmount<VigorPower>();
                Require(vigor==existing+counted*power.Amount&&power.MomentumAttacks==0,"Hunt next-turn Vigor payout/reset differs.");
                await BalanceOracle.End(player,true);Require(owner.GetPowerAmount<VigorPower>()==vigor,"Hunt paid again without new Attacks.");
                return new{key,up,mode,counted,vigor,turnBoundary=true,saveRoundTrip=true};
            }
            throw new InvalidDataException("Unknown rare engine fixture: "+key);
        }
    }
}
