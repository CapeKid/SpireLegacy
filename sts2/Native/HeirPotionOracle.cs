using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;
// Opt-in owned-game test oracle, never invoked during normal play.
public static class HeirPotionOracle
{
    public static object Inspect(RunState run)
    {
        var player=run.Players.Single();var own=HeirPotions.All.ToArray();var expected=own.Select(p=>p.Id).ToHashSet();
        // Public-beta removed the blacklist argument from this inspection API.
        var method=HarmonyLib.AccessTools.Method(typeof(PotionFactory),"GetPotionOptions");
        var options=((IEnumerable<PotionModel>)method.Invoke(null,method.GetParameters().Length==1?[player]:[player,Array.Empty<PotionModel>()])!).ToArray();
        if(options.Select(p=>p.Id).Distinct().Count()!=options.Length)throw new InvalidDataException("Duplicate native potion options.");
        if(!expected.IsSubsetOf(options.Select(p=>p.Id)))throw new InvalidDataException("Missing exclusive potion options.");
        if(!ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(player.UnlockState).All(p=>options.Count(o=>o.Id==p.Id)==1))throw new InvalidDataException("Shared potion availability changed.");
        var drops=new HashSet<ModelId>();var generated=new HashSet<ModelId>();var shops=new HashSet<ModelId>();var rng=player.PlayerRng.Shops;
        for(var i=0;i<1800;i++)
        {
            var reward=new PotionReward(player);reward.Populate();if(reward.Potion is FamilyPotion) drops.Add(reward.Potion.Id);
            var potion=PotionFactory.CreateRandomPotionInCombat(player,rng);if(potion is FamilyPotion)generated.Add(potion.Id);
        }
        for(var i=0;i<500;i++)foreach(var entry in MerchantInventory.CreateForNormalMerchant(player).PotionEntries)if(entry.Model is FamilyPotion)shops.Add(entry.Model.Id);
        if(!expected.SetEquals(drops)||!expected.SetEquals(generated)||!expected.SetEquals(shops))throw new InvalidDataException("Missing native potion source.");
        var directory=Path.Combine(Runtime.DataDirectory,"potion-icons");Directory.CreateDirectory(directory);
        foreach(var potion in own)
        {
            var key=((FamilyPotion)potion).Key;
            if(potion.DynamicDescription.GetFormattedText().Contains('{')||potion.Image.GetWidth()!=128||potion.Outline?.GetWidth()!=128)throw new InvalidDataException("Potion text/art invalid.");
            if(!ResourceLoader.Exists(potion.ImagePath)||ResourceLoader.Load<Texture2D>(potion.ImagePath)!=potion.Image)throw new InvalidDataException("Potion texture cache path invalid.");
            potion.Image.GetImage().SavePng(Path.Combine(directory,key+".png"));potion.Outline!.GetImage().SavePng(Path.Combine(directory,key+"-outline.png"));
        }
        return new{options=options.Select(p=>p.Id.ToString()).ToArray(),drops=drops.Select(p=>p.ToString()).ToArray(),generated=generated.Select(p=>p.ToString()).ToArray(),shops=shops.Select(p=>p.ToString()).ToArray(),icons=directory,
            native=ModelDb.AllCharacters.Where(c=>c is not HeirCharacter).Select(c=>new{character=c.Id.ToString(),potions=c.PotionPool.AllPotions.Select(p=>new{id=p.Id.ToString(),rarity=p.Rarity.ToString()}).ToArray()}).ToArray()};
    }
    public static async Task<object> Obtain(RunState run,string key,bool shop)
    {
        var player=run.Players.Single();var potion=HeirPotions.Get(key).ToMutable();
        if(shop)
        {
            var entry=new MerchantPotionEntry(potion,player);var gold=player.Gold;await PlayerCmd.GainGold(entry.Cost,player);
            if(!await entry.OnTryPurchaseWrapper(null)||player.Gold!=gold)throw new InvalidDataException("Native potion purchase failed.");
        }
        else if(!await new PotionReward(potion,player).SelectUnsynchronized())throw new InvalidDataException("Native potion reward failed.");
        if(!player.Potions.Contains(potion))throw new InvalidDataException("Potion absent after procurement.");
        var restored=PotionModel.FromSerializable(potion.ToSerializable(player.PotionSlots.ToList().IndexOf(potion)));
        if(restored.Id!=potion.Id||restored.TargetType!=potion.TargetType||restored.DynamicDescription.GetFormattedText()!=potion.DynamicDescription.GetFormattedText())throw new InvalidDataException("Potion serialization mismatch.");
        return new{key,saveRoundTrip=true,slots=player.PotionSlots.Select(p=>p?.Id.ToString()).ToArray()};
    }
    public static async Task<object> Use(RunState run,string key,bool controller=false)
    {
        var player=run.Players.Single();var potion=player.Potions.OfType<FamilyPotion>().Single(p=>p.Key==key);var enemy=player.Creature.CombatState!.GetOpponentsOf(player.Creature).First(e=>e.IsAlive);
        var target=potion.TargetType==MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy?enemy:player.Creature;
        if(!potion.IsValidTarget(target)||potion.IsValidTarget(potion.TargetType==MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy?player.Creature:enemy))throw new InvalidDataException("Wrong native potion target.");
        var block=player.Creature.Block;var hp=player.Creature.CurrentHp;var hand=player.PlayerCombatState!.Hand.Cards.Count;var energy=player.PlayerCombatState.Energy;var completed=false;
        var tree=(SceneTree)Engine.GetMainLoop();var cancelPreserved=false;
        potion.ExecutionFinished+=_=>completed=true;
        if(controller)
        {
            var holder=NativeTestBridge.Descendants(tree.Root).OfType<NPotionHolder>().Single(h=>h.Potion?.Model==potion);
            var manager=MegaCrit.Sts2.Core.Nodes.CommonUi.NControllerManager.Instance!;
            var mode=HarmonyLib.AccessTools.Property(manager.GetType(),"IsUsingController");
            if(mode!=null)mode.SetValue(manager,true);
            else {mode=HarmonyLib.AccessTools.Property(manager.GetType(),"InputType");mode.SetValue(manager,Enum.Parse(mode.PropertyType,"Controller"));}
            if(potion.TargetType==MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy)
            {
                var canceled=holder.UsePotion();
                for(var i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                Input.ParseInputEvent(new InputEventAction{Action="ui_cancel",Pressed=true,Strength=1});
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                Input.ParseInputEvent(new InputEventAction{Action="ui_cancel",Pressed=false});
                for(var i=0;i<30&&!canceled.IsCompleted;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                if(!canceled.IsCompleted)throw new InvalidDataException("Controller potion cancel did not complete.");
                await canceled;cancelPreserved=!completed&&player.Potions.Contains(potion);
                if(!cancelPreserved)throw new InvalidDataException("Controller cancel consumed potion.");
            }
            var use=holder.UsePotion();
            if(potion.TargetType==MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy)
            {
                for(var i=0;i<4;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                Input.ParseInputEvent(new InputEventAction{Action="ui_select",Pressed=true,Strength=1});
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                Input.ParseInputEvent(new InputEventAction{Action="ui_select",Pressed=false});
                for(var i=0;i<30&&!use.IsCompleted;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                if(!use.IsCompleted)throw new InvalidDataException("Controller potion target confirm did not complete.");
            }
            await use;
        }
        else potion.EnqueueManualUse(target);
        var deadline=Time.GetTicksMsec()+15000;
        while(!completed&&Time.GetTicksMsec()<deadline)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        if(!completed||player.Potions.Contains(potion))throw new InvalidDataException("Potion native queue did not resolve/remove.");
        return new{key,completed,controller,cancelPreserved,hpChange=player.Creature.CurrentHp-hp,blockChange=player.Creature.Block-block,handChange=player.PlayerCombatState.Hand.Cards.Count-hand,energyChange=player.PlayerCombatState.Energy-energy,
            charges=player.Creature.GetPower<P_class>()?.Charges,powers=player.Creature.Powers.Select(p=>new{id=p.Id.ToString(),amount=p.Amount}).ToArray(),enemyPowers=enemy.Powers.Select(p=>new{id=p.Id.ToString(),amount=p.Amount}).ToArray()};
    }
    public static async Task<object> Focus(string key)
    {
        var holder=NativeTestBridge.Descendants(((SceneTree)Engine.GetMainLoop()).Root).OfType<NPotionHolder>().Single(h=>h.Potion?.Model is FamilyPotion p&&p.Key==key);
        if(holder.Potion!.Image.Texture!=Runtime.Texture(Design.Get("potion_art",key).Text("path")))throw new InvalidDataException("Native potion belt did not display original artwork.");
        holder.GrabFocus();var tree=(SceneTree)Engine.GetMainLoop();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        return new{focused=tree.Root.GuiGetFocusOwner()==holder,right=holder.FocusNeighborRight.ToString()};
    }
}
