using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

/// <summary>Private-profile native eligibility/acquisition/save oracle, opt-in bridge only.</summary>
public static class HeirRelicOracle
{
    public static object Inspect(RunState run)
    {
        var player=run.Players.Single();var original=player.RelicGrabBag.ToSerializable();
        var expected=HeirDropRelics.All.Where(r=>r.IsAllowed(run)).Select(r=>r.Id).ToHashSet();
        var bagIds=original.RelicIdLists.Values.SelectMany(v=>v).ToArray();
        if(!expected.IsSubsetOf(bagIds)||bagIds.Distinct().Count()!=bagIds.Length||bagIds.Contains(ModelDb.Relic<BloodlineRelic>().Id))throw new InvalidDataException("Wrong Heir relic grab bag.");
        var seenDrops=new HashSet<ModelId>();var seenShops=new HashSet<ModelId>();
        foreach(var rarity in new[]{RelicRarity.Common,RelicRarity.Uncommon,RelicRarity.Rare})
            for(var i=0;i<150;i++)
            {
                var reward=new RelicReward(rarity,player);reward.Populate();var relic=reward.Relic!;
                if(relic is BloodlineRelic)throw new InvalidDataException("Starter offered as reward.");
                if(relic is FamilyDropRelic)seenDrops.Add(relic.Id);
            }
        player.RelicGrabBag.LoadFromSerializable(original);
        for(var i=0;i<220;i++)
            foreach(var entry in MerchantInventory.CreateForNormalMerchant(player).RelicEntries)
            {
                if(entry.Model is BloodlineRelic)throw new InvalidDataException("Starter offered in shop.");
                if(entry.Model is FamilyDropRelic)seenShops.Add(entry.Model.Id);
            }
        player.RelicGrabBag.LoadFromSerializable(original);
        if(!expected.SetEquals(seenDrops)||!expected.SetEquals(seenShops))throw new InvalidDataException("Missing exclusive native reward/shop relic.");
        var images=Path.Combine(Runtime.DataDirectory,"relic-icons");Directory.CreateDirectory(images);
        foreach(var relic in HeirDropRelics.All)
        {
            var key=((FamilyDropRelic)relic).Key;
            if(relic.DynamicDescription.GetFormattedText().Contains('{'))throw new InvalidDataException("Unresolved relic description.");
            var icon=relic.Icon;var big=relic.BigIcon;var outline=relic.IconOutline;
            if(!ResourceLoader.Exists(relic.PackedIconPath)||ResourceLoader.Load<Texture2D>(relic.PackedIconPath)!=icon)throw new InvalidDataException("Native relic texture path does not resolve original icon.");
            if(icon.GetWidth()!=128||big.GetWidth()!=128||outline.GetWidth()!=128)throw new InvalidDataException("Wrong original relic icon.");
            icon.GetImage().SavePng(Path.Combine(images,key+".png"));outline.GetImage().SavePng(Path.Combine(images,key+"-outline.png"));
        }
        return new {drops=seenDrops.Select(r=>r.ToString()).ToArray(),shops=seenShops.Select(r=>r.ToString()).ToArray(),rewards=450,inventories=220,icons=images,
            native=ModelDb.AllCharacters.Where(c=>c is not HeirCharacter).Select(c=>new{character=c.Id.ToString(),relics=c.RelicPool.AllRelics.Select(r=>new{id=r.Id.ToString(),rarity=r.Rarity.ToString()}).ToArray()}).ToArray(),
            own=ModelDb.RelicPool<HeirRelicPool>().AllRelics.Select(r=>new{id=r.Id.ToString(),rarity=r.Rarity.ToString(),title=r.Title.GetFormattedText(),text=r.DynamicDescription.GetFormattedText()}).ToArray()};
    }
    public static async Task<object> Obtain(RunState run,string key,bool shop)
    {
        var player=run.Players.Single();var canonical=HeirDropRelics.Get(key);
        var rolled=player.RelicGrabBag.PullFromFront(canonical.Rarity,r=>r.Id==canonical.Id,run)??throw new InvalidDataException("Exclusive relic absent from native bag.");
        if(shop)
        {
            var entry=new MerchantRelicEntry(rolled.ToMutable(),player);var before=player.Gold;
            await PlayerCmd.GainGold(entry.Cost,player);
            if(!await entry.OnTryPurchaseWrapper(null))throw new InvalidDataException("Native shop purchase failed.");
            if(player.Gold!=before)throw new InvalidDataException("Wrong shop charge.");
        }
        else {var reward=new RelicReward(rolled.ToMutable(),player);if(!await reward.SelectUnsynchronized())throw new InvalidDataException("Native reward claim failed.");}
        return Counters(run);
    }
    public static object Counters(RunState run)=>run.Players.Single().Relics.OfType<FamilyDropRelic>().Select(r=>new{key=r.Key,r.CombatClaimed,r.TriggerTurn,r.TypeMask,r.MaskTurn,counter=r.DisplayAmount,status=r.Status.ToString()}).ToArray();
    public static async Task<object> RoundTrip(RunState run)
    {
        var player=run.Players.Single();
        foreach(var old in player.Relics.OfType<FamilyDropRelic>().ToArray())
        {
            var restored=(FamilyDropRelic)RelicModel.FromSerializable(old.ToSerializable());
            if(restored.Key!=old.Key||restored.CombatClaimed!=old.CombatClaimed||restored.TriggerTurn!=old.TriggerTurn||restored.TypeMask!=old.TypeMask||restored.MaskTurn!=old.MaskTurn||restored.Status!=old.Status)throw new InvalidDataException("Relic saved counter mismatch.");
            await RelicCmd.Replace(old,restored);
        }
        return Counters(run);
    }
}
