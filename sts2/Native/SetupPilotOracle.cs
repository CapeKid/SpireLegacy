using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

// Synthetic ordering calibration. Injected hand/HP/energy are never campaign evidence.
public static class SetupPilotOracle
{
    public static async Task<object> Probe(RunState run,string kind,bool sequence)
    {
        if(kind is not ("safe" or "energy" or "lethal" or "threat" or "covered"))throw new ArgumentException("Unknown setup calibration.");
        var player=run.Players.Single();
        var cls=player.Character is HeirCharacter?PlayerBloodlines.Heir(player).classId:"ironclad";
        if(cls=="ironclad"&&player.Character is not MegaCrit.Sts2.Core.Models.Characters.Ironclad)throw new ArgumentException("Unknown setup control character.");
        await RunManager.Instance.EnterRoomDebug(MegaCrit.Sts2.Core.Rooms.RoomType.Monster,model:ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.NibbitsNormal>().ToMutable(),showTransition:false);
        RunManager.Instance.ActionExecutor.Unpause();
        var tree=(SceneTree)Engine.GetMainLoop();var deadline=Time.GetTicksMsec()+30000;
        while(player.PlayerCombatState?.Phase!=PlayerTurnPhase.Play){
            if(Time.GetTicksMsec()>deadline)throw new TimeoutException("Setup fixture play phase.");
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        }
        var hand=PileType.Hand.GetPile(player);
        foreach(var card in hand.Cards.ToArray())await CardPileCmd.Add(card,PileType.Draw,skipVisuals:true);
        var enemies=player.Creature.CombatState!.HittableEnemies.Where(e=>e.IsAlive).ToArray();
        foreach(var enemy in enemies){enemy.SetMaxHpInternal(1000);enemy.SetCurrentHpInternal(1000);}
        var setupKey=cls switch{"knight"=>"knight_weapons_master","mage"=>"surge","ranger"=>"ranger_patient_hunter",_=>"native_inflame"};
        var attackKey=cls switch{"knight"=>"knight_redoubled_blow","mage"=>"mage_conduit_lance","ranger"=>"ranger_rapid_fire",_=>"native_twin_strike"};
        var setup=player.Creature.CombatState.CreateCard(cls=="ironclad"?ModelDb.Card<Inflame>():NativeCards.Get(setupKey),player);
        var attack=player.Creature.CombatState.CreateCard(cls=="ironclad"?ModelDb.Card<TwinStrike>():NativeCards.Get(attackKey),player);
        // The upgraded five-hit Lance outranks Surge's generic Power score;
        // the ordinary version already loses that comparison and needs no reordering.
        if(cls=="mage")CardCmd.Upgrade(attack);
        await CardPileCmd.Add(setup,PileType.Hand,skipVisuals:true);await CardPileCmd.Add(attack,PileType.Hand,skipVisuals:true);
        var attackCost=attack.EnergyCost.GetWithModifiers(CostModifiers.All);
        await PlayerCmd.SetEnergy(kind=="energy"?attackCost:3,player);
        if(kind=="lethal")enemies[0].SetCurrentHpInternal(1);
        if(kind=="covered")await CreatureCmd.GainBlock(player.Creature,100,ValueProp.Unpowered,null);
        var incoming=kind is "threat" or "covered"?100:0;
        var decision=TacticalPilot.Choose(player,incoming,sequence)??throw new InvalidDataException("No setup fixture decision.");
        var selectedSetup=decision.Card==setup;
        var target=decision.Target??enemies[0];
        decimal Preview(){var v=attack.DynamicVars["Damage"];var old=v.PreviewValue;try{v.UpdateCardPreview(attack,CardPreviewMode.Normal,target,true);return v.PreviewValue*PilotCardInfo.Hits(attack);}finally{v.PreviewValue=old;}}
        var beforePreview=Preview();var energyBefore=player.PlayerCombatState!.Energy;
        if(selectedSetup){
            await BalanceOracle.Play(setup);
            var follow=TacticalPilot.Choose(player,incoming,sequence)??throw new InvalidDataException("Setup stranded its Attack.");
            if(follow.Card!=attack)throw new InvalidDataException("Unexpected setup follow-up.");
            target=follow.Target!;
        }
        var buffedPreview=Preview();var hp=target.CurrentHp;
        var strength=player.Creature.GetPowerAmount<StrengthPower>();var vigor=player.Creature.GetPowerAmount<VigorPower>();
        await BalanceOracle.Play(attack,target);
        var damage=hp-target.CurrentHp;
        if(kind!="lethal"&&damage!=buffedPreview)throw new InvalidDataException("Native damage disagrees with calibrated preview.");
        return new{cls,kind,sequence,setupKey,attackKey,attackUpgraded=attack.IsUpgraded,selectedSetup,decision.Reason,energyBefore,energySpent=energyBefore-player.PlayerCombatState.Energy,
            hits=PilotCardInfo.Hits(attack),beforePreview,buffedPreview,damage,strength,vigor,vigorRemaining=player.Creature.GetPowerAmount<VigorPower>(),targetDead=!target.IsAlive};
    }
}
