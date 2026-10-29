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
                if(mode=="reset"){
                    Codec(counter);await BalanceOracle.End(player,true);Require(counter.SkillsThisTurn==0,"Skill count did not reset.");
                    // The intervening real monster turn can apply Shrink or gain
                    // Block. Isolate the counter reset from those damage modifiers.
                    foreach(var power in owner.Powers.Where(p=>p!=counter).ToArray())await PowerCmd.Remove(power);
                    foreach(var enemy in enemies){foreach(var power in enemy.Powers.ToArray())await PowerCmd.Remove(power);enemy.LoseBlockInternal(enemy.Block);}
                    await CardPileCmd.Add(card,PileType.Hand);await PlayerCmd.GainEnergy(30,player);
                }
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
                        Require(eligible?.Pile?.Type==PileType.Discard&&hand.Cards.Count==CardPile.MaxCardsInHand,"Prophecy must use native full-hand overflow to discard, without a discount.");
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
                await Play(card);var power=owner.GetPower<P_exhaust_burst>()!;
                var hp=enemies.Select(e=>e.CurrentHp).ToArray();
                async Task PhoenixFuel(int count){foreach(var enemy in enemies)enemy.LoseBlockInternal(enemy.Block);await Fuel(count);}
                void Damage(int bursts){for(var i=0;i<enemies.Length;i++)Require(hp[i]-enemies[i].CurrentHp==power.Amount*bursts,"Phoenix AoE burst total differs.");}
                if(mode=="legacy-capped")power.ExhaustsThisTurn=3;
                await PhoenixFuel(2);Damage(0);Require(power.ExhaustsThisTurn==2,"Phoenix partial counter differs.");Codec(power);
                if(mode=="stacked"){
                    await Play(await Add(player,NativeCards.Get(key),up));
                    Require(power.Amount==d.Magic*2&&power.ExhaustsThisTurn==2,"Stacking reset Phoenix progress or damage.");
                }
                var bursts=0;
                if(mode=="ethereal"){
                    var ethereal=await Add(player,ModelDb.Card<Dazed>());
                    await BalanceOracle.End(player,true);bursts=1;Damage(bursts);
                    Require(ethereal.Pile?.Type==PileType.Exhaust&&power.ExhaustsThisTurn==0,"Ethereal Exhaust failed to complete a group.");
                    await PhoenixFuel(2);
                }
                await BalanceOracle.End(player,true);Damage(bursts);
                Require(power.ExhaustsThisTurn==2,"Phoenix progress was lost at a turn boundary.");Codec(power);
                await PhoenixFuel(1);Damage(++bursts);Require(power.ExhaustsThisTurn==0,"Phoenix did not reset after a burst.");
                await PhoenixFuel(6);bursts+=2;Damage(bursts);
                await PhoenixFuel(1);Require(power.ExhaustsThisTurn==1,"Remainder was lost after multiple bursts.");Codec(power);
                await BalanceOracle.End(player,true);Require(power.ExhaustsThisTurn==1,"One-card remainder did not survive next turn.");
                await PhoenixFuel(2);Damage(++bursts);
                Require(power.ExhaustsThisTurn==0,"Final burst left an invalid remainder.");
                return new{key,up,mode,burst=power.Amount,bursts,repeatedPerTurn=true,turnBoundary=true,saveRoundTrip=true};
            }
            if(key=="knight_castle_heart"){
                var previewChecks=0;
                if(!up)foreach(var row in Design.Rows("cards")) {
                    var preview=(LegacyCard)player.Creature.CombatState!.CreateCard(NativeCards.Get(row.Text("id")),player);
                    var blockBefore=preview.DynamicVars.Block.BaseValue;
                    preview.UpgradeInternal();
                    Require(preview.DynamicVars.Block.WasJustUpgraded==(row.Number("upgradeBlock")!=0),"Block upgrade highlight differs: "+preview.Key);
                    Require(preview.DynamicVars.Damage.WasJustUpgraded==(row.Number("upgradeDamage")!=0),"Damage upgrade highlight differs: "+preview.Key);
                    if(preview.Key=="knight_castle_heart") {
                        Require(preview.DynamicVars.Block.BaseValue==blockBefore&&preview.DynamicVars.Block.ToHighlightedString(false)==blockBefore.ToString(),"Castle Heart still highlights unchanged Block.");
                        Require(preview.EnergyCost.GetWithModifiers(CostModifiers.All)==1,"Castle Heart cost-only preview differs.");
                    }
                    player.Creature.CombatState.RemoveCard(preview);previewChecks++;
                }
                await Play(card);var power=owner.GetPower<P_metallicize>()!;
                var native=ModelDb.Power<PlatingPower>();
                Require(power.PackedIconPath==native.PackedIconPath&&power.ResolvedBigIconPath==native.ResolvedBigIconPath,"Castle Heart is not using native armor icons.");
                Require(MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetTexture2D(power.PackedIconPath).GetWidth()>0,"Armor icon did not load.");
                Require(owner.Block==d.Block&&power.Amount==d.Magic,"Castle Heart base Block/armor changed.");
                await power.BeforeSideTurnEnd(Context,owner.Side,[owner]);
                Require(owner.Block==d.Block+d.Magic,"Castle Heart armor no longer grants its original Block.");
                return new{key,up,mode,packedIcon=power.PackedIconPath,bigIcon=power.ResolvedBigIconPath,block=owner.Block,armor=power.Amount,previewChecks};
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
