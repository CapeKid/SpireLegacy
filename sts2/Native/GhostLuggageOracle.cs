using System.Text.Json;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace SpireLegacy;

public static partial class PowerAuditOracle
{
    public static async Task<object> Luggage(RunState run,bool up,string mode,IDisposable selector)
    {
        using(selector) {
            var player=run.Players.Single();var enemies=await Setup(player);
            var hand=PileType.Hand.GetPile(player);
            var attack=await Add(player,mode=="x-cost"?ModelDb.Card<Whirlwind>():ModelDb.Card<Bash>());
            if(mode=="zero-cost")attack.EnergyCost.SetThisCombat(0);
            var originalCost=attack.EnergyCost.GetWithModifiers(CostModifiers.All);
            if(mode=="played")await Play(attack,enemies[0]);
            else if(mode!="flush-only")await CardCmd.Discard(Context,attack);
            if(mode is "exhausted" or "wrong-copy")await BranchCommands.Exhaust(Context,attack);
            if(mode=="wrong-copy")await Add(player,ModelDb.Card<Bash>(),false,PileType.Discard);
            var skill=await Add(player,ModelDb.Card<DefendIronclad>());await CardCmd.Discard(Context,skill);
            if(mode=="stale")await BalanceOracle.End(player,true);
            var card=(LegacyCard)await Add(player,NativeCards.Get("ranger_ghost_luggage"),up);
            Require(card.EnergyCost.GetWithModifiers(CostModifiers.All)==(up?1:2),"Ghost Luggage base/upgrade cost differs.");
            await Play(card);
            CardModel? second=null;
            if(mode=="stacked") {
                await Play(await Add(player,NativeCards.Get("ranger_ghost_luggage"),up));
                second=await Add(player,ModelDb.Card<Bash>());await CardCmd.Discard(Context,second);
            }
            var power=player.Creature.GetPower<P_discard_return>()!;
            Require(player.Creature.GetPower<P_discard_next_block>()==null,"Ghost still grants delayed Block.");
            if(mode=="hand-cap")for(var i=0;i<CardPile.MaxCardsInHand;i++)(await Add(player,ModelDb.Card<DefendIronclad>())).GiveSingleTurnRetain();
            await BalanceOracle.End(player,true);
            var expected=mode is not ("cancel" or "hand-cap" or "played" or "flush-only" or "exhausted" or "wrong-copy" or "stale");
            Require((attack.Pile?.Type==PileType.Hand)==expected,"Previous-turn discard eligibility/recovery differs: "+mode);
            Require(skill.Pile?.Type!=PileType.Hand,"Discarded Skill qualified as an Attack.");
            if(second!=null)Require(second.Pile?.Type==PileType.Hand,"Stacked Ghost must recover a second Attack.");
            var expectedCost=expected&&!attack.EnergyCost.CostsX?Math.Max(0,originalCost-1):originalCost;
            Require(attack.EnergyCost.GetWithModifiers(CostModifiers.All)==expectedCost,"Recovery discount/full-hand/X cost differs.");
            BetaMainCompatibility.CacheSavedProperties(power.GetType());
            var saved=JsonSerializer.Serialize(SavedProperties.From(power),new JsonSerializerOptions{IncludeFields=true});
            var copy=(LegacyPower)ModelDb.GetById<PowerModel>(power.Id).ToMutable();
            JsonSerializer.Deserialize<SavedProperties>(saved,new JsonSerializerOptions{IncludeFields=true})!.Fill(copy);
            Require(copy.GhostRecoveryTurn==power.GhostRecoveryTurn,"Recovery turn codec differs.");
            var clone=(LegacyPower)power.MutableClone();clone.GhostRecoveryTurn=-99;Require(power.GhostRecoveryTurn!=-99,"Clone shares claim state.");
            await CardPileCmd.Add(hand.Cards.ToArray(),PileType.Discard,skipVisuals:true);
            await power.AfterPlayerTurnStart(Context,player);
            Require(hand.Cards.Count==0,"Duplicate callback or a previously full hand paid again.");
            await BalanceOracle.End(player,true);
            Require(attack.EnergyCost.GetWithModifiers(CostModifiers.All)==originalCost,"Discount survived past its turn.");
            Require(attack.Pile?.Type!=PileType.Hand,"Old discard paid on a later turn.");
            // A fresh discard on this turn must qualify again next turn.
            await CardPileCmd.Add(attack,PileType.Hand,skipVisuals:true);await CardCmd.Discard(Context,attack);
            await BalanceOracle.End(player,true);
            Require((attack.Pile?.Type==PileType.Hand)==(mode!="cancel"),"Recurring recovery stopped after its first turn.");
            return new{up,mode,firstReturned=expected,cost=up?1:2,discountExpires=true,repeatGuard=true,recurs=true,propertyCodec=true};
        }
    }
}
