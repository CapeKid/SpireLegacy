using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Disposable-profile bridge only. Real native model identity/save transitions.
public static class TraitAuditOracle
{
    private static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
    public static object Transfer(RunState run)
    {
        var local=run.Players.First(p=>p.Character is HeirCharacter);
        Player Peer(ulong id,string[] traits) {
            var data=local.ToSerializable();data.NetId=id;
            var snapshot=FamilySnapshot.Read(FamilySnapshot.Serialize(PlayerBloodlines.State(local)!));
            snapshot.ownerNetId=id;snapshot.coop=true;snapshot.heir.traits=traits.ToList();
            var carrier=data.Relics.Single(r=>r.Id==ModelDb.Relic<BloodlineRelic>().Id);
            var index=carrier.Props!.strings!.FindIndex(p=>p.name==nameof(BloodlineRelic.FamilyRunSnapshot));
            carrier.Props.strings[index]=new(){name=nameof(BloodlineRelic.FamilyRunSnapshot),value=FamilySnapshot.Serialize(snapshot)};
            return Player.FromSerializable(data);
        }
        var pacifist=Peer(991,["cantattack"]);var ordinary=Peer(992,[]);var checks=new List<object>();
        foreach(var cls in new[]{"knight","mage","ranger"})foreach(var upgrade in new[]{false,true}) {
            var card=(LegacyCard)NativeCards.Get(cls+"_strike").ToMutable();card.Owner=pacifist;
            if(upgrade)CardCmd.Upgrade(card);
            var source=card.Type.ToString();card.Owner=null!;card.Owner=ordinary;
            var expected=new CardDesign(card.Key,upgrade,[]);
            var rebound=card.Type==MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack&&card.DynamicVars.Damage.BaseValue==expected.Damage&&card.TraitSnapshot=="[]";
            card.Owner=null!;card.Owner=pacifist;
            var returned=card.Type==MegaCrit.Sts2.Core.Entities.Cards.CardType.Skill&&card.DynamicVars.Damage.BaseValue==0;
            checks.Add(new{card.Key,upgrade,source,rebound,returned});
        }
        return checks;
    }
    public static async Task<object> Probe(RunState run)
    {
        var player=run.Players.Single();var heir=PlayerBloodlines.Heir(player);var rows=Inheritance.Rows(heir).ToArray();
        var relic=player.Relics.OfType<BloodlineRelic>().Single();
        var expectedHp=rows.Any(r=>r.Text("effect")=="fragile")?1:Math.Max(20,Design.Get("classes",heir.classId).Number("hp")+rows.Sum(r=>r.Number("hp")));
        Require(player.Creature.MaxHp==expectedHp,"Native starting HP disagrees with trait fields.");
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
                var duplicate=(LegacyCard)card.CreateDupe();if(duplicate.Owner==null)duplicate.Owner=player;Check(duplicate);
                var loaded=(LegacyCard)run.LoadCard(card.ToSerializable(),player);Check(loaded);run.RemoveCard(loaded);
            }
            CardCmd.Downgrade(card);Require(card.DynamicVars.Damage.BaseValue==new CardDesign(card.Key,false,heir.traits).Damage,"Downgraded gene mismatch.");run.RemoveCard(card);
        }
        return new{traits=heir.traits.ToArray(),classId=heir.classId,hp=expectedHp,healing=actual,draw,maxEnergy=expectedEnergy,price,mutations,scopes=CoopAdversarialOracle.Scopes()};
    }
}
