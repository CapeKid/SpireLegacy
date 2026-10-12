using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Disposable-profile bridge only. Real native model identity/save transitions.
public static class TraitAuditOracle
{
    private static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
    private static LegacyCard? redrawCard;
    public static async Task<object> Transforms(RunState run)
    {
        var player=run.Players.Single();var cls=PlayerBloodlines.Heir(player).classId;var results=new List<object>();
        foreach(var key in new[]{cls+"_guard",cls+"_strike","ranger_barbed_arrow"}) {
            var original=player.Deck.Cards.OfType<LegacyCard>().First(c=>c.DesignCard.Row.Text("rarity")=="BASIC");
            var replacement=(LegacyCard)run.CreateCard(NativeCards.Get(key),player);
            var result=await CardCmd.Transform(original,replacement);
            Require(result is {success:true}&&result.Value.cardAdded==replacement&&replacement.Owner==player&&!player.Deck.Cards.Contains(original),"Native transform failed identity/owner conservation.");
            Require(replacement.TraitSnapshot==JsonSerializer.Serialize(PlayerBloodlines.Heir(player).traits),"Transform lost frozen owner traits.");
            var expected=new CardDesign(key,false,PlayerBloodlines.Heir(player).traits);
            Require(replacement.DynamicVars.Damage.BaseValue==expected.Damage&&replacement.DynamicVars.Block.BaseValue==expected.Block,"Transform delivered wrong starter genetics.");
            var loaded=(LegacyCard)run.LoadCard(replacement.ToSerializable(),player);
            Require(loaded.DesignCard.Rules()==replacement.DesignCard.Rules()&&loaded.Type==replacement.Type,"Transformed card save mismatch.");run.RemoveCard(loaded);
            results.Add(new{key,type=replacement.Type.ToString(),removedOriginal=true,owner=player.NetId});
        }
        return results;
    }
    public static async Task<object> Restrictions(RunState run)
    {
        var player=run.Players.Single();var combat=player.Creature.CombatState!;
        var context=new BlockingPlayerChoiceContext();await PlayerCmd.GainEnergy(20,player);
        var results=new List<object>();
        foreach(var canonical in new CardModel[]{ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.FlashOfSteel>(),ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(),NativeCards.Get("ranger_barbed_arrow"),ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Finesse>()}) {
            var card=combat.CreateCard(canonical,player);await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
            var allowed=!(PlayerBloodlines.Traits(player).Has("pacifist")&&card.Type==CardType.Attack);
            var energy=player.PlayerCombatState!.Energy;var target=card.TargetType==TargetType.AnyEnemy?combat.GetOpponentsOf(player.Creature).First(c=>c.IsAlive):null;
            var accepted=card.TryManualPlay(target);Require(accepted==allowed,"Pacifist native/colorless play restriction disagrees: "+card.Id);
            if(accepted) {
                var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+15000;
                while(card.Pile?.Type is PileType.Hand or PileType.Play||MegaCrit.Sts2.Core.Runs.RunManager.Instance.ActionExecutor.IsRunning) {
                    if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Native/colorless card did not resolve.");await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                }
            }else Require(card.Pile?.Type==PileType.Hand&&player.PlayerCombatState.Energy==energy,"Rejected Attack spent energy or left hand.");
            results.Add(new{card=card.Id.ToString(),allowed,accepted,energyBefore=energy,energyAfter=player.PlayerCombatState.Energy,pile=card.Pile?.Type.ToString()});
        }
        return results;
    }
    public static async Task<object> Triggers(RunState run)
    {
        var player=run.Players.Single();var rules=PlayerBloodlines.Traits(player);
        var relic=player.Relics.OfType<BloodlineRelic>().Single();var context=new BlockingPlayerChoiceContext();
        var enemy=player.Creature.CombatState!.GetOpponentsOf(player.Creature).First(c=>c.IsAlive);
        var checks=new List<string>();
        if(rules.Has("hurt_energy")) {
            await CreatureCmd.Damage(context,player.Creature,2,ValueProp.Move,enemy);
            var energy=player.PlayerCombatState!.Energy;
            await relic.AfterPlayerTurnStart(context,player);Require(player.PlayerCombatState.Energy==energy+rules.Amount("hurt_energy"),"Pending hurt Energy not delivered.");
            energy=player.PlayerCombatState.Energy;await relic.AfterPlayerTurnStart(context,player);Require(player.PlayerCombatState.Energy==energy,"Pending Energy delivered twice.");checks.Add("hurt_energy");
        }
        if(rules.Has("coin_loss")||rules.Stat("heal")>0) {
            var gold=player.Gold;player.Creature.SetCurrentHpInternal(player.Creature.MaxHp-10);var hp=player.Creature.CurrentHp;
            await relic.AfterCombatVictory((MegaCrit.Sts2.Core.Rooms.CombatRoom)run.CurrentRoom!);
            Require(player.Gold==Math.Max(0,gold-rules.Amount("coin_loss")),"Victory gold loss mismatch.");
            Require(player.Creature.CurrentHp==hp+rules.Healing(rules.Stat("heal")),"Victory inherited healing mismatch.");checks.Add("victory");
        }
        if(rules.Has("diva")||rules.Has("explosions")) {
            var hp=player.Creature.CurrentHp;var gold=player.Gold;
            await CreatureCmd.Kill(enemy);
            Require(player.Gold==gold+rules.Amount("diva"),"Enemy death gold mismatch.");
            Require(player.Creature.CurrentHp==hp-rules.Amount("explosions"),"Enemy death explosion mismatch.");checks.Add("native_death");
        }
        return new{checks};
    }
    public static async Task<object> Redraw(RunState run,bool reuse=false)
    {
        var player=run.Players.Single();var context=new BlockingPlayerChoiceContext();
        var card=reuse?redrawCard??throw new InvalidOperationException("No prior redraw fixture."):(LegacyCard)player.Creature.CombatState!.CreateCard(NativeCards.Get(PlayerBloodlines.Heir(player).classId+"_strike"),player);
        Require(card.Owner==player&&player.Creature.CombatState!.ContainsCard(card),"Redraw fixture belongs to another combat.");redrawCard=card;
        await CardPileCmd.Add(card,PileType.Draw,CardPilePosition.Top,skipVisuals:true);
        await CardPileCmd.Draw(context,1,player);
        var first=card.EnergyCost.GetWithModifiers(CostModifiers.All);
        var firstPile=card.Pile?.Type.ToString();
        if(card.Pile?.Type==PileType.Hand)await CardCmd.Discard(context,card);
        await CardPileCmd.Add(card,PileType.Draw,CardPilePosition.Top,skipVisuals:true);
        await CardPileCmd.Draw(context,1,player);
        var second=card.EnergyCost.GetWithModifiers(CostModifiers.All);
        return new{first,second,firstPile,secondPile=card.Pile?.Type.ToString(),repeatedDrawStable=first==second};
    }
    public static object Transfer(RunState run)
    {
        var local=run.Players.First(p=>p.Character is HeirCharacter);
        Player Peer(ulong id,string[] traits) {
            var data=local.ToSerializable();data.NetId=id;
            var snapshot=FamilySnapshot.Read(FamilySnapshot.Serialize(PlayerBloodlines.State(local)!));
            snapshot.ownerNetId=id;snapshot.coop=true;snapshot.heir.traits=traits.ToList();snapshot.heir.classId=id==991?"mage":"ranger";
            var carrier=data.Relics.Single(r=>r.Id==ModelDb.Relic<BloodlineRelic>().Id);
            var index=carrier.Props!.strings!.FindIndex(p=>p.name==nameof(BloodlineRelic.FamilyRunSnapshot));
            carrier.Props.strings[index]=new(){name=nameof(BloodlineRelic.FamilyRunSnapshot),value=FamilySnapshot.Serialize(snapshot)};
            var peer=Player.FromSerializable(data);peer.RunState=run;return peer;
        }
        var pacifist=Peer(991,["cantattack"]);var ordinary=Peer(992,[]);var checks=new List<object>();
        void Assign(LegacyCard card,Player owner) {
            var give=AccessTools.Method(typeof(CardModel),"GiveToAnotherPlayer");
            if(give!=null)give.Invoke(card,[owner]);else {card.Owner=null!;card.Owner=owner;}
        }
        foreach(var row in Design.Rows("cards").Where(r=>r.Text("rarity")=="BASIC"))foreach(var upgrade in new[]{false,true}) {
            var card=(LegacyCard)run.CreateCard(NativeCards.Get(row.Text("id")),pacifist);
            if(upgrade)CardCmd.Upgrade(card);
            var source=card.Type.ToString();Assign(card,ordinary);
            var expected=new CardDesign(card.Key,upgrade,[]);
            var normalType=Enum.Parse<CardType>(row.Text("type"),true);
            var rebound=card.Type==normalType&&card.DynamicVars.Damage.BaseValue==expected.Damage&&card.DynamicVars.Block.BaseValue==expected.Block&&card.TraitSnapshot=="[]";
            Assign(card,pacifist);
            var mercy=new CardDesign(card.Key,upgrade,["cantattack"]);
            var returned=card.Type==(mercy.Mercy?CardType.Skill:normalType)&&card.DynamicVars.Damage.BaseValue==mercy.Damage&&card.DynamicVars.Block.BaseValue==mercy.Block;
            var method=AccessTools.Method(typeof(CardModel),"CreateDupe");
            var duplicate=(LegacyCard)method.Invoke(card,method.GetParameters().Select(p=>p.ParameterType==typeof(Player)?ordinary:p.HasDefaultValue?p.DefaultValue:throw new MissingMethodException("Unsupported duplicate parameter: "+p)).ToArray())!;
            if(duplicate.Owner!=ordinary){duplicate.Owner=null!;duplicate.Owner=ordinary;}
            var duplicatedRecipient=duplicate.Type==normalType&&duplicate.DynamicVars.Damage.BaseValue==expected.Damage&&duplicate.DynamicVars.Block.BaseValue==expected.Block;
            card.DynamicVars.Damage.BaseValue+=2;Assign(card,ordinary);
            var preservesBuff=card.DynamicVars.Damage.BaseValue==expected.Damage+2;
            checks.Add(new{card.Key,upgrade,source,rebound,returned,duplicatedRecipient,preservesBuff});
            run.RemoveCard(duplicate);run.RemoveCard(card);
        }
        return checks;
    }
    public static async Task<object> Probe(RunState run)
    {
        var player=run.Players.Single();var heir=PlayerBloodlines.Heir(player);var rows=Inheritance.Rows(heir).ToArray();
        var relic=player.Relics.OfType<BloodlineRelic>().Single();
        var expectedHp=rows.Any(r=>r.Text("effect")=="fragile")?1:Math.Max(20,Design.Get("classes",heir.classId).Number("hp")+rows.Sum(r=>r.Number("hp")));
        Require(player.Creature.MaxHp==expectedHp,"Native starting HP disagrees with trait fields.");
        Require(player.Creature.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>()==rows.Sum(r=>r.Number("strength")),"Starting inherited Strength mismatch.");
        Require(player.Creature.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>()==rows.Sum(r=>r.Number("dexterity")),"Starting inherited Dexterity mismatch.");
        var before=player.Creature.CurrentHp;
        decimal expectedHeal=9;
        if(rows.Any(r=>r.Text("effect")=="no_heal"))expectedHeal=0;
        else {foreach(var row in rows.Where(r=>r.Text("effect")=="super_heal"))expectedHeal*=row.Number("amount");if(rows.Any(r=>r.Text("effect")=="vegan"))expectedHeal=Math.Floor(expectedHeal/2);}
        var actual=9m;InheritedHealing.Prefix(player.Creature,ref actual);Require(actual==expectedHeal,"Inherited native healing prefix mismatch.");
        if(expectedHp>20) {
            player.Creature.SetCurrentHpInternal(expectedHp-20);
            await CreatureCmd.Heal(player.Creature,9,false);
            Require(player.Creature.CurrentHp==expectedHp-20+(int)expectedHeal,"Native healing delivered wrong amount.");
            player.Creature.SetCurrentHpInternal(before);
        }
        var expectedEnergy=3+rows.Where(r=>r.Text("effect") is "energy" or "overcharge").Sum(r=>r.Number("amount"));
        Require(relic.ModifyMaxEnergy(player,3)==expectedEnergy,"Energy ownership mismatch.");
        var draw=5+rows.Sum(r=>r.Number("draw"))+(player.PlayerCombatState?.TurnNumber==1?rows.Where(r=>r.Text("effect")=="opening_draw_energy").Sum(r=>r.Number("amount")):0);
        Require(relic.ModifyHandDraw(player,5)==draw,"Draw ownership mismatch.");
        var price=100*(1-rows.Where(r=>r.Text("effect")=="shop").Sum(r=>r.Number("amount"))/100m);
        Require(relic.ModifyMerchantPrice(player,null!,100)==price,"Merchant modifier mismatch.");
        var mutations=0;
        foreach(var basic in Design.Rows("cards").Where(r=>r.Text("rarity")=="BASIC")) {
            var card=(LegacyCard)run.CreateCard(NativeCards.Get(basic.Text("id")),player);
            foreach(var upgraded in new[]{false,true}) {
                if(upgraded)CardCmd.Upgrade(card);
                var expected=new CardDesign(card.Key,upgraded,heir.traits);
                void Check(LegacyCard sample) {
                    Require(sample.DynamicVars.Damage.BaseValue==expected.Damage&&sample.DynamicVars.Block.BaseValue==expected.Block&&sample.DesignCard.Rules()==expected.Rules()&&sample.DesignCard.Cost==expected.Cost,"Inherited starter transition mismatch: "+sample.Key);
                    Require(sample.Type==(expected.Mercy?MegaCrit.Sts2.Core.Entities.Cards.CardType.Skill:Enum.Parse<MegaCrit.Sts2.Core.Entities.Cards.CardType>(basic.Text("type"),true)),"Inherited starter type mismatch.");
                    mutations++;
                }
                Check(card);
                var duplicateMethod=AccessTools.Method(typeof(CardModel),"CreateDupe");
                var duplicate=(LegacyCard)duplicateMethod.Invoke(card,duplicateMethod.GetParameters().Select(p=>p.ParameterType==typeof(Player)?player:p.HasDefaultValue?p.DefaultValue:throw new MissingMethodException("Unsupported duplicate parameter: "+p)).ToArray())!;
                if(duplicate.Owner==null)duplicate.Owner=player;Check(duplicate);
                var loaded=(LegacyCard)run.LoadCard(card.ToSerializable(),player);Check(loaded);run.RemoveCard(loaded);
            }
            CardCmd.Downgrade(card);Require(card.DynamicVars.Damage.BaseValue==new CardDesign(card.Key,false,heir.traits).Damage,"Downgraded gene mismatch.");run.RemoveCard(card);
        }
        return new{traits=heir.traits.ToArray(),classId=heir.classId,hp=expectedHp,healing=actual,draw,maxEnergy=expectedEnergy,price,mutations,scopes=CoopAdversarialOracle.Scopes()};
    }
}
