using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Synthetic HP boundary checks; no injected result counts as a campaign.
public static class HpCostPilotOracle
{
    public static async Task<object> Probe(RunState run,string key,bool upgraded)
    {
        CardModel canonical=key switch{
            "native_offering"=>ModelDb.Card<Offering>(),"native_bloodletting"=>ModelDb.Card<Bloodletting>(),
            "native_hemokinesis"=>ModelDb.Card<Hemokinesis>(),"native_haunt"=>ModelDb.Card<Haunt>(),
            _ when Design.Get("cards",key).Number("hpLoss")>0=>NativeCards.Get(key),
            _=>throw new ArgumentException("Unknown HP-payment calibration card.")};
        var player=run.Players.Single();
        var native=key.StartsWith("native_",StringComparison.Ordinal);
        if(native!=(player.Character is MegaCrit.Sts2.Core.Models.Characters.Ironclad))throw new InvalidDataException("HP control character mismatch.");
        await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.NibbitsNormal>().ToMutable(),showTransition:false);
        RunManager.Instance.ActionExecutor.Unpause();
        var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+30000;
        while(player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play){
            if(Time.GetTicksMsec()>deadline)throw new TimeoutException("HP-payment fixture play phase.");
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        var hand=PileType.Hand.GetPile(player);
        foreach(var held in hand.Cards.ToArray())await CardPileCmd.Add(held,PileType.Draw,skipVisuals:true);
        foreach(var enemy in player.Creature.CombatState!.HittableEnemies){enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}
        var card=player.Creature.CombatState.CreateCard(canonical,player);
        if(upgraded)CardCmd.Upgrade(card);
        await CardPileCmd.Add(card,PileType.Hand,skipVisuals:true);
        await PlayerCmd.SetEnergy(10,player);
        await CreatureCmd.GainBlock(player.Creature,50,ValueProp.Unpowered,null);
        var expectedLoss=card is LegacyCard legacy?legacy.DesignCard.Amount("hpLoss"):
            key=="native_haunt"?0:(int)card.DynamicVars["HpLoss"].BaseValue;
        var estimatedLoss=PilotCardInfo.ImmediateHpLoss(card);
        var exactHp=Math.Max(1,expectedLoss);player.Creature.SetCurrentHpInternal(exactHp);
        var exactSelected=TacticalPilot.Choose(player,0)?.Card==card;
        var underHp=Math.Max(1,expectedLoss-1);player.Creature.SetCurrentHpInternal(underHp);
        var underSelected=TacticalPilot.Choose(player,0)?.Card==card;
        player.Creature.SetCurrentHpInternal(exactHp);
        var fallback=player.Creature.CombatState.CreateCard(native?ModelDb.Card<DefendIronclad>():NativeCards.Get(PlayerBloodlines.Heir(player).classId+"_guard"),player);
        await CardPileCmd.Add(fallback,PileType.Hand,skipVisuals:true);
        var fallbackSelected=TacticalPilot.Choose(player,0)?.Card==fallback;
        await CardPileCmd.Add(fallback,PileType.Draw,skipVisuals:true);
        var safeHp=expectedLoss+1;player.Creature.SetCurrentHpInternal(safeHp);
        var safeSelected=TacticalPilot.Choose(player,0)?.Card==card;
        var blockBefore=player.Creature.Block;
        // Play only the survivable case, through the normal paid-card path.
        if(safeSelected)await BalanceOracle.Play(card);
        return new{key,upgraded,expectedLoss,estimatedLoss,exactHp,exactSelected,underHp,underSelected,fallbackSelected,
            safeHp,safeSelected,hpAfter=player.Creature.CurrentHp,actualHpLost=safeHp-player.Creature.CurrentHp,
            alive=player.Creature.IsAlive,blockBefore,blockAfter=player.Creature.Block,pile=card.Pile?.Type.ToString()};
    }
}
