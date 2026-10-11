using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Invoked only by the opt-in isolated owned-game test bridge.
public static class NimbleOracle
{
    public static object Eligibility(RunState run)
    {
        var player=run.Players.Single();var checks=0;var eligible=0;
        foreach(var row in Design.Rows("cards")) {
            var card=(LegacyCard)run.CreateCard(NativeCards.Get(row.Text("id")),player);
            for(var up=0;up<2;up++) {
                if(up==1)CardCmd.Upgrade(card);
                var d=card.DesignCard;
                var expected=d.Block>0||row.Number("nextBlock")>0||row.Text("special") is "discard_block" or "exhaust_block" or "exhaust_nonattacks" or "double_block";
                if(ModelDb.Enchantment<Nimble>().CanEnchant(card)!=expected)throw new InvalidDataException("Nimble eligibility: "+card.Key);
                if(expected) {
                    eligible++;
                    CardCmd.Enchant<Nimble>(card,3);
                    card.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,card.DynamicVars);
                    if(row.Number("nextBlock")>0 && card.NextBlock.PreviewValue!=d.Amount("nextBlock")+3)throw new InvalidDataException("Delayed Block enchantment preview: "+card.Key);
                    if(card.HasSpecialBlock && card.SpecialBlock.PreviewValue!=d.SpecialAmount+3)throw new InvalidDataException("Secondary Block enchantment preview: "+card.Key);
                    var saved=(LegacyCard)run.LoadCard(card.ToSerializable(),player);
                    saved.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,saved.DynamicVars);
                    if(saved.Enchantment?.Amount!=3 || saved.GainsBlock!=expected || saved.GetDescriptionForPile(PileType.Deck)!=card.GetDescriptionForPile(PileType.Deck))throw new InvalidDataException("Enchanted save round trip: "+card.Key);
                    if(up==1) {
                        CardCmd.Downgrade(saved);
                        if(row.Number("nextBlock")>0 && saved.NextBlock.BaseValue!=new CardDesign(card.Key).Amount("nextBlock"))throw new InvalidDataException("Delayed Block downgrade: "+card.Key);
                    }
                    run.RemoveCard(saved);
                    CardCmd.ClearEnchantment(card);
                }
                checks++;
            }
            run.RemoveCard(card);
        }
        return new{checks,eligible,saveRoundTrips=eligible};
    }

    public static async Task<object> Play(RunState run,string key,bool upgrade,IDisposable selector)
    {
        using(selector) {
            var player=run.Players.Single();var creature=player.Creature;var combat=creature.CombatState!;
            var context=new BlockingPlayerChoiceContext();var tree=(SceneTree)Engine.GetMainLoop();
            var hand=PileType.Hand.GetPile(player);
            foreach(var held in hand.Cards.ToArray())await CardPileCmd.Add(held,PileType.Draw,skipVisuals:true);
            var card=(LegacyCard)combat.CreateCard(NativeCards.Get(key),player);
            if(upgrade)CardCmd.Upgrade(card);
            var expected=card.DesignCard;
            CardCmd.Enchant<Nimble>(card,3);
            await PowerCmd.Apply<DexterityPower>(context,creature,2,creature,null);
            var fuel=0;
            if(card.RowForTestSpecial() is "exhaust_block" or "exhaust_nonattacks") {
                for(var i=0;i<2;i++) {
                    var guard=combat.CreateCard(NativeCards.Get(Runtime.Heir.classId+"_guard"),player);
                    await CardPileCmd.Add(guard,card.RowForTestSpecial()=="exhaust_block"?PileType.Exhaust:PileType.Hand,skipVisuals:true);
                    fuel++;
                }
            }
            await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
            card.UpdateDynamicVarPreview(CardPreviewMode.Normal,null,card.DynamicVars);
            var description=card.GetDescriptionForPile(PileType.Hand);
            var before=creature.Block;var energy=player.PlayerCombatState!.Energy;
            var cost=card.EnergyCost.GetWithModifiers(CostModifiers.All);
            if(cost>energy)await PlayerCmd.GainEnergy(cost-energy,player);
            energy=player.PlayerCombatState.Energy;
            var enemy=combat.GetOpponentsOf(creature).First(e=>e.IsAlive);
            if(!card.TryManualPlay(card.TargetType==TargetType.AnyEnemy?enemy:null))throw new InvalidDataException("Paid Nimble play refused: "+key);
            var deadline=Time.GetTicksMsec()+15000;
            while(card.Pile?.Type is PileType.Hand or PileType.Play && Time.GetTicksMsec()<deadline)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            if(card.Pile?.Type is PileType.Hand or PileType.Play)throw new TimeoutException("Nimble play did not resolve: "+key);
            var expectedImmediate=expected.Block>0?expected.Block+5:0;
            if(fuel>0)expectedImmediate+=(expected.SpecialAmount+3)*fuel;
            if(creature.Block-before!=expectedImmediate)throw new InvalidDataException($"Nimble immediate Block {key}: {creature.Block-before}, expected {expectedImmediate}");
            if(player.PlayerCombatState.Energy-energy!=-cost)throw new InvalidDataException("Nimble paid energy: "+key);
            var next=creature.GetPower<BlockNextTurnPower>();var nextAmount=next?.Amount??0;
            if(expected.Amount("nextBlock")>0) {
                if(nextAmount!=expected.Amount("nextBlock")+5)throw new InvalidDataException("Nimble delayed Block amount: "+key);
                creature.LoseBlockInternal(creature.Block);
                await next!.AfterBlockCleared(creature);
                if(creature.Block!=nextAmount || creature.HasPower<BlockNextTurnPower>())throw new InvalidDataException("Delayed delivery reapplied modifiers: "+key);
            }
            var discardBlock=0;
            if(card.RowForTestSpecial()=="discard_block") {
                await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
                before=creature.Block;
                await CardCmd.Discard(context,card);
                discardBlock=creature.Block-before;
                if(discardBlock!=expected.SpecialAmount+3)throw new InvalidDataException("Nimble discard Block: "+key);
            }
            return new{key,upgrade,description,immediate=expectedImmediate,nextAmount,discardBlock,cost,paid=true,tableNodePresent=NCard.FindOnTable(card)!=null};
        }
    }
    private static string RowForTestSpecial(this LegacyCard card)=>card.DesignCard.Row.Text("special");
}
