using System.Text.Json;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

public static partial class PowerAuditOracle
{
    // Paid native plays and actual turn transitions; entered only via HEIR_TEST_MODE.
    public static async Task<object> CardRoles(RunState run,string key,bool up,string mode,IDisposable selector)
    {
        using(selector) {
            var player=run.Players.Single();var enemies=await Setup(player);
            var owner=player.Creature;var counter=owner.GetPower<P_class>()!;
            var hand=PileType.Hand.GetPile(player);var exhaust=PileType.Exhaust.GetPile(player);
            var card=(LegacyCard)await Add(player,NativeCards.Get(key),up);var d=card.DesignCard;
            var loaded=(LegacyCard)run.LoadCard(card.ToSerializable(),player);
            Require(loaded.Id==card.Id&&loaded.IsUpgraded==up&&loaded.DesignCard.Rules()==d.Rules(),"Redesigned card save round trip.");run.RemoveCard(loaded);
            async Task Fuel(int count){for(var i=0;i<count;i++)await BranchCommands.Exhaust(Context,await Add(player,ModelDb.Card<DefendIronclad>()));}
            void Codec(LegacyPower power){
                BetaMainCompatibility.CacheSavedProperties(power.GetType());
                var properties=SavedProperties.From(power)??new SavedProperties();
                var json=JsonSerializer.Serialize(properties,new JsonSerializerOptions{IncludeFields=true});
                var copy=(LegacyPower)ModelDb.GetById<PowerModel>(power.Id).ToMutable();
                JsonSerializer.Deserialize<SavedProperties>(json,new JsonSerializerOptions{IncludeFields=true})!.Fill(copy);
                Require(copy.SkillsThisTurn==power.SkillsThisTurn&&copy.ExhaustsThisTurn==power.ExhaustsThisTurn,"Counter property codec.");
                var clone=(LegacyPower)power.MutableClone();clone.ExhaustsThisTurn=99;
                Require(power.ExhaustsThisTurn!=99,"Power clone shared counters.");
            }
            if(key=="knight_forge_memory"){
                await Play(card);await Fuel(2);var vigor=d.Magic*2;
                Require(owner.GetPowerAmount<VigorPower>()==vigor&&owner.Block==0,"Forge Memory must grant Vigor instead of draw/Block.");
                var attack=await Add(player,NativeCards.Get("knight_twin_cut"));var hp=enemies[0].CurrentHp;
                await Play(attack,enemies[0]);Require(hp-enemies[0].CurrentHp==(attack.DynamicVars.Damage.BaseValue+vigor)*2&&owner.GetPowerAmount<VigorPower>()==0,"Exhaust Vigor must cover every hit and expire after the Attack.");
                return new{key,up,mode,vigor,damage=hp-enemies[0].CurrentHp,saveRoundTrip=true};
            }
            if(key=="ranger_ghost_luggage"){
                await Play(card);var fuel=new[]{await Add(player,ModelDb.Card<DefendIronclad>()),await Add(player,ModelDb.Card<DefendIronclad>())};
                await CardCmd.Discard(Context,fuel);
                Require(owner.Block==0&&owner.GetPowerAmount<BlockNextTurnPower>()==d.Magic*2,"Discard must bank Block, with no immediate defense.");
                await BalanceOracle.End(player,true);Require(owner.Block==d.Magic*2&&owner.GetPowerAmount<BlockNextTurnPower>()==0,"Banked Block must arrive exactly once next turn; flush must not add more.");
                await BalanceOracle.End(player,true);Require(owner.Block==0,"Banked Block repeated without a discard.");
                return new{key,up,mode,nextBlock=d.Magic*2,turnBoundary=true,saveRoundTrip=true};
            }
            if(key=="mage_cleanse_sigil"){
                await Apply(player,"weak",2);await Apply(player,"vulnerable",2);await Apply(player,"dexterity",2);
                await Play(card);Require(!owner.Powers.Any(p=>p.Type==MegaCrit.Sts2.Core.Entities.Powers.PowerType.Debuff)&&owner.GetPowerAmount<DexterityPower>()==2&&counter.Charges==2&&owner.Block==0&&card.Pile?.Type==PileType.Exhaust,"Sigil must cleanse, preserve buffs, fill charges and Exhaust without Block.");
                return new{key,up,mode,cost=d.Cost,charges=counter.Charges,saveRoundTrip=true};
            }
            if(key is "nova" or "mage_moonstone_lance"){
                var count=mode=="zero"?0:mode=="one"?1:mode is "cap" or "reset"?5:2;
                for(var i=0;i<count;i++)await Play(await Add(player,ModelDb.Card<DefendIronclad>()));
                if(mode=="reset"){Codec(counter);await BalanceOracle.End(player,true);Require(counter.SkillsThisTurn==0,"Skill count did not reset.");await CardPileCmd.Add(card,PileType.Hand);await PlayerCmd.GainEnergy(30,player);}
                if(mode=="reservoir")await Apply(player,"reservoir",2);
                if(mode=="replay")await Apply(player,"double_tap",1);
                var charges=counter.Charges;var skills=counter.SkillsThisTurn;var hp=enemies.Select(e=>e.CurrentHp).ToArray();
                var before=hand.Cards.Count;await Play(card,enemies[0]);
                var expected=d.Damage+(key=="nova"?charges:skills)*d.SpecialAmount+(mode=="reservoir"?charges*2:0);
                if(mode=="replay")expected+=d.Damage;
                Require(hp[0]-enemies[0].CurrentHp==expected,"Sequencing/charge damage differs: "+key+"/"+mode);
                if(key=="nova")Require(enemies.Select((e,i)=>hp[i]-e.CurrentHp).All(n=>n==expected)&&card.Pile?.Type==PileType.Exhaust,"Nova must hit all targets and Exhaust.");
                else Require(!card.Keywords.Contains(CardKeyword.Retain)&&card.RetainedGrowth==0,"Lance still duplicates retained growth.");
                Require(counter.Charges==0&&hand.Cards.Count-before+1==charges,"Attack must consume charges and draw only once through Arcane Flow.");
                return new{key,up,mode,charges,skills,damage=expected,targets=key=="nova"?enemies.Length:1,flowDraw=charges,saveRoundTrip=true};
            }
            if(key is "mage_returned_prophecy" or "knight_ancestral_salvage"){
                var excluded=new List<CardModel>();
                foreach(var id in new[]{"knight_arms_vault","mage_returned_prophecy","knight_ancestral_salvage"})excluded.Add(await Add(player,NativeCards.Get(id),false,PileType.Exhaust));
                CardModel? eligible=null;var skill=await Add(player,ModelDb.Card<DefendIronclad>(),false,PileType.Exhaust);
                if(key=="mage_returned_prophecy"&&mode=="empty")await CardPileCmd.RemoveFromCombat([skill],skipVisuals:true);
                else if(!(key=="knight_ancestral_salvage"&&mode=="skills")) {
                    eligible=await Add(player,ModelDb.Card<Bash>(),up,PileType.Exhaust);
                    // Attack recovery must skip the earlier Skill; general recovery may choose it.
                    if(key=="mage_returned_prophecy")await CardPileCmd.RemoveFromCombat([skill],skipVisuals:true);
                }
                if(mode=="replay")await Apply(player,"burst",1);
                var before=hand.Cards.Count;await Play(card);
                if(key=="mage_returned_prophecy"){
                    Require(hand.Cards.Count==before-1&&eligible?.Pile?.Type!=PileType.Hand,"Prophecy must not recover or draw immediately.");
                    var pending=owner.GetPower<P_prophecy_return>()!;Require(pending.Amount==d.SpecialAmount*(mode=="replay"?2:1),"Replay must stack the discount.");Codec(pending);
                    if(mode=="hand-cap"){
                        while(hand.Cards.Count<CardPile.MaxCardsInHand)await Add(player,ModelDb.Card<DefendIronclad>());
                        await pending.AfterPlayerTurnStart(Context,player);
                        Require(eligible?.Pile?.Type==PileType.Exhaust,"Prophecy exceeded hand cap.");
                    }else await BalanceOracle.End(player,true);
                    var returned=eligible?.Pile?.Type==PileType.Hand;
                    Require(returned==(mode is not ("empty" or "cancel" or "hand-cap"))&&owner.GetPower<P_prophecy_return>()==null,"Delayed recovery selection/lifetime differs.");
                    if(returned){var discount=Math.Min(2,d.SpecialAmount*(mode=="replay"?2:1));Require(eligible!.EnergyCost.GetWithModifiers(CostModifiers.All)==2-discount,"Recovered card discount differs.");await BalanceOracle.End(player,true);Require(eligible.EnergyCost.GetWithModifiers(CostModifiers.All)==2,"Recovery discount did not expire.");}
                }else {
                    var returned=eligible?.Pile?.Type==PileType.Hand;
                    Require(returned==(mode is not ("cancel" or "skills"))&&skill.Pile?.Type==PileType.Exhaust,"Salvage must select only Attacks.");
                    Require(owner.Block==d.Block&&owner.GetPowerAmount<VigorPower>()==(returned?d.SpecialAmount:0),"Salvage Block/Vigor or declined selection differs.");
                }
                Require(excluded.All(c=>c.Pile?.Type==PileType.Exhaust)&&card.Pile?.Type==PileType.Exhaust,"Exhaust recovery loop exclusions failed.");
                return new{key,up,mode,discount=key=="mage_returned_prophecy"?d.SpecialAmount:0,vigor=key=="knight_ancestral_salvage"?owner.GetPowerAmount<VigorPower>():0,recoveryExclusions=true,saveRoundTrip=true};
            }
            if(key=="knight_phoenix_forge"){
                await Play(card);var hp=enemies[0].CurrentHp;
                await Fuel(2);Require(enemies[0].CurrentHp==hp,"Phoenix burst fired before third Exhaust.");Codec(owner.GetPower<P_exhaust_burst>()!);
                await Fuel(1);Require(hp-enemies[0].CurrentHp==d.Magic,"Third Exhaust did not trigger burst.");
                await Fuel(3);Require(hp-enemies[0].CurrentHp==d.Magic,"Phoenix burst repeated in same turn.");
                await BalanceOracle.End(player,true);var nextHp=enemies[0].CurrentHp;await Fuel(3);Require(nextHp-enemies[0].CurrentHp==d.Magic,"Phoenix burst did not reset next turn.");
                return new{key,up,mode,burst=d.Magic,oncePerTurn=true,turnBoundary=true,saveRoundTrip=true};
            }
            if(key=="knight_iron_furnace"){
                await Play(card);if(mode=="combined"){await Play(await Add(player,NativeCards.Get("knight_forge_memory")));await Play(await Add(player,NativeCards.Get("knight_furnace_breath")));}
                var hp=enemies[0].CurrentHp;
                var status=await Add(player,ModelDb.Card<Wound>());Require(status.Pile?.Type==PileType.Hand&&owner.Block==0,"Adding a Status must not count as drawing it.");
                await CardPileCmd.Add(status,PileType.Draw,CardPilePosition.Top,skipVisuals:true);await CardPileCmd.Draw(Context,1,player);
                Require(status.Pile?.Type==PileType.Exhaust&&owner.Block==d.Magic,"Drawn Status must Exhaust for Block.");
                var curse=await Add(player,ModelDb.Card<Injury>());await CardPileCmd.Add(curse,PileType.Draw,CardPilePosition.Top,skipVisuals:true);await CardPileCmd.Draw(Context,1,player);
                Require(curse.Pile?.Type==PileType.Hand&&owner.Block==d.Magic,"Furnace incorrectly consumed a Curse.");
                if(mode=="combined")Require(owner.GetPowerAmount<VigorPower>()==3&&hp-enemies[0].CurrentHp==6,"Status consumption lost exhaust or Status/Curse draw hooks.");
                return new{key,up,mode,block=d.Magic,statusExhausted=true,curseUnchanged=true,saveRoundTrip=true};
            }
            throw new InvalidDataException("Unknown card role fixture: "+key);
        }
    }
}
