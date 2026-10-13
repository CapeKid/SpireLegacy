using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace SpireLegacy;

// Disposable lab fixtures only; all entry points are behind the native test bridge.
public static class PoolAuditOracle
{
    private static MegaCrit.Sts2.Core.Nodes.Cards.NCard[] menuCards=[];
    private static void Require(bool value,string message) {if(!value)throw new InvalidDataException(message);}
    public static object Registry()
    {
        var cards=ModelDb.AllCards.ToArray();var pools=ModelDb.AllCardPools.ToArray();
        Require(cards.Select(c=>c.Id).Distinct().Count()==cards.Length,"Duplicate registration ID.");
        Require(pools.Select(p=>p.Id).Distinct().Count()==pools.Length,"Duplicate pool ID.");
        var historical=Design.Rows("cards").Select(r=>NativeCards.Get(r.Text("id"))).ToArray();
        Require(historical.All(c=>!c.IsMutable&&cards.Single(m=>m.Id==c.Id)==c&&ModelDb.GetById<CardModel>(c.Id)==c),"Historical lookup/canonical identity changed.");
        Require(historical.Cast<LegacyCard>().All(c=>!c.IsUpgraded&&c.TraitSnapshot.Length==0&&c.RetainedGrowth==0&&c.DiscardRewardTurn==-1&&c.DynamicVars.Damage.BaseValue==new CardDesign(c.Key).Damage&&c.DynamicVars.Block.BaseValue==new CardDesign(c.Key).Block),"A historical canonical inherited mutable stats or run state.");
        Require(pools.Where(p=>p is not FamilyCardPool).All(p=>p.AllCards.All(c=>c is not LegacyCard)),"Heir cards contaminated a native pool.");
        var detail=pools.OfType<FamilyCardPool>().Select(p=>new{pool=p.Id.ToString(),cards=p.AllCards.Cast<LegacyCard>().Select(c=>new{key=c.Key,rarity=c.Rarity.ToString(),mutable=c.IsMutable}).ToArray()}).ToArray();
        Require(detail.All(p=>p.cards.All(c=>!c.mutable)),"Mutable pool canonical.");
        return new{expanded=NativePoolParity.Expanded,historical=historical.Length,total=cards.Length,pools=pools.Length,detail};
    }
    public static object Owner(RunState run)
    {
        var checks=new List<object>();
        foreach(var player in run.Players)
        {
            var allowed=player.Character.CardPool.GetUnlockedCards(player.UnlockState,player.RunState.CardMultiplayerConstraint).Where(c=>c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare).ToArray();
            var ids=allowed.Select(c=>c.Id).ToHashSet();var rewards=new List<object>();var shops=new List<object>();
            object Snapshot(CardModel c)=>new{c.Id,c.IsUpgraded,vars=c.DynamicVars.Select(v=>new{v.Key,v.Value.BaseValue}).ToArray()};
            var canon=allowed.Select(Snapshot).ToArray();
            foreach(var room in new[]{RoomType.Monster,RoomType.Elite,RoomType.Boss})
                foreach(var count in new[]{1,3,6,12}.Concat(room==RoomType.Boss?new[]{allowed.Count(c=>c.Rarity==CardRarity.Rare)}:[]))
                    for(var repeat=0;repeat<5;repeat++)
                    {
                        var reward=new CardReward(CardCreationOptions.ForRoom(player,room),count,player);reward.Populate();
                        var cards=reward.Cards.ToArray();Require(cards.Length==count&&cards.Select(c=>c.Id).Distinct().Count()==count,"Reward count/uniqueness.");
                        Require(cards.All(c=>c.Owner==player&&c.IsMutable&&ids.Contains(c.Id)),"Reward owner/pool leaked.");
                        Require(room!=RoomType.Boss||cards.All(c=>c.Rarity==CardRarity.Rare),"Boss reward rarity changed.");
                        rewards.Add(new{room=room.ToString(),count,repeat,ids=cards.Select(c=>c.Id.ToString()).ToArray(),upgraded=cards.Count(c=>c.IsUpgraded)});
                        foreach(var card in cards)run.RemoveCard(card);
                    }
            for(var repeat=0;repeat<10;repeat++)
            {
                var shop=MerchantInventory.CreateForNormalMerchant(player);var cards=shop.CharacterCardEntries.Select(e=>e.CreationResult!.Card).ToArray();
                Require(cards.Length==5&&cards.All(c=>c.Owner==player&&c.IsMutable&&ids.Contains(c.Id)),"Merchant owner/pool leaked.");
                Require(cards.Select(c=>c.Type).SequenceEqual(new[]{CardType.Attack,CardType.Attack,CardType.Skill,CardType.Skill,CardType.Power}),"Merchant type slots changed.");
                Require(shop.ColorlessCardEntries.All(e=>e.CreationResult!.Card is not LegacyCard),"Heir colorless shop contamination.");
                shops.Add(new{repeat,ids=cards.Select(c=>c.Id.ToString()).ToArray()});
                foreach(var entry in shop.CardEntries)run.RemoveCard(entry.CreationResult!.Card);
            }
            var transforms=new List<object>();
            foreach(var card in player.Deck.Cards)
            {
                var choices=CardFactory.GetDefaultTransformationOptions(card,false).ToArray();
                Require(choices.Length>0&&choices.All(c=>ids.Contains(c.Id)&&c.Id!=card.Id),"Transformation pool leaked.");
                var created=CardFactory.CreateRandomCardForTransform(card,false,player.PlayerRng.Rewards);
                Require(created.Owner==player&&ids.Contains(created.Id)&&created.Id!=card.Id,"Transform result owner/pool.");
                transforms.Add(new{original=card.Id.ToString(),created=created.Id.ToString(),choices=choices.Length});run.RemoveCard(created);
            }
            Require(JsonSerializer.Serialize(canon)==JsonSerializer.Serialize(allowed.Select(Snapshot)),"Reward/shop/transform mutated canonical values.");
            checks.Add(new{player.NetId,cls=player.Character is HeirCharacter?PlayerBloodlines.Heir(player).classId:"native",pool=player.Character.CardPool.Id.ToString(),allowed=allowed.Select(c=>c is LegacyCard legacy?legacy.Key:c.Id.ToString()).ToArray(),rewards,shops,transforms,canonicalUnchanged=true});
        }
        return checks;
    }
    public static object Sparse(RunState run)
    {
        var player=run.Players.Single();var allowed=player.Character.CardPool.GetUnlockedCards(player.UnlockState,player.RunState.CardMultiplayerConstraint).Where(c=>c.Rarity==CardRarity.Common).ToArray();var checks=new List<object>();
        foreach(var odds in new[]{CardRarityOddsType.Uniform,CardRarityOddsType.RegularEncounter,CardRarityOddsType.BossEncounter})
        foreach(var size in new[]{0,1,2,6})foreach(var count in new[]{0,1,3,6,7})
        {
            var options=new CardCreationOptions(new[]{player.Character.CardPool},CardCreationSource.Other,odds,c=>allowed.Take(size).Contains(c)).WithFlags(CardCreationFlags.NoUpgradeRoll);
            var started=Time.GetTicksMsec();string? diagnostic=null;CardModel[] cards=[];
            try{cards=CardFactory.CreateForReward(player,count,options).Select(r=>r.Card).ToArray();}
            catch(Exception error){diagnostic=DiagnosticText.Describe(error,()=>"Sparse reward size="+size+", requested="+count);}
            var elapsed=Time.GetTicksMsec()-started;
            Require(elapsed<2000,"Sparse reward selection was not bounded.");
            Require(count<=size?diagnostic==null&&cards.Length==count&&cards.Select(c=>c.Id).Distinct().Count()==count:diagnostic!=null,"Sparse reward contract changed.");
            checks.Add(new{odds=odds.ToString(),size,count,elapsed,offered=cards.Select(c=>c.Id.ToString()).ToArray(),diagnostic});foreach(var card in cards)run.RemoveCard(card);
        }
        return checks;
    }
    public static object Historical(RunState run,bool import)
    {
        var player=run.Players.Single();var path=Path.Combine(Runtime.DataDirectory,"pool-cards.json");var checks=new List<object>();var saves=new List<SerializableCard>();
        if(import)saves=JsonSerializer.Deserialize(File.ReadAllText(path),JsonSerializationUtility.GetTypeInfo<List<SerializableCard>>())!;
        else foreach(var row in Design.Rows("cards"))foreach(var upgraded in new[]{false,true})
        {
            var card=run.CreateCard(NativeCards.Get(row.Text("id")),player);if(upgraded)CardCmd.Upgrade(card);saves.Add(card.ToSerializable());run.RemoveCard(card);
        }
        foreach(var saved in saves)
        {
            var card=(LegacyCard)run.LoadCard(saved,player);
            Require(card.Owner==player&&card.Pool==player.Character.CardPool&&card.Id==saved.Id,"Saved ID/pool/owner changed.");
            var round=card.ToSerializable();Require(round.Id==saved.Id&&card.IsUpgraded==(saved.CurrentUpgradeLevel>0),"Saved upgrade changed.");
            checks.Add(new{key=card.Key,upgrade=card.IsUpgraded,pool=card.Pool.Id.ToString()});run.RemoveCard(card);
        }
        if(!import)File.WriteAllText(path,JsonSerializer.Serialize(saves,JsonSerializationUtility.GetTypeInfo<List<SerializableCard>>()));
        return new{import,checks};
    }
    public static object Modifiers(RunState run)
    {
        var results=new List<object>();
        foreach(var player in run.Players)
        {
            var relic=ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.PrismaticGem>().ToMutable();player.AddRelicInternal(relic,silent:true);
            try
            {
                var input=CardCreationOptions.ForRoom(player,RoomType.Monster).WithFlags(CardCreationFlags.IsCardReward);
                var options=relic.ModifyCardRewardCreationOptions(player,input);
                var possible=options.GetPossibleCards(player).OfType<LegacyCard>().Select(c=>c.Key).ToHashSet();
                var expected=player.Character.CardPool.AllCards.OfType<LegacyCard>().Select(c=>c.Key).ToHashSet();
                var bypass=CardCreationOptions.ForRoom(player,RoomType.Monster).WithFlags(CardCreationFlags.IsCardReward|CardCreationFlags.NoCardPoolModifications);
                Require(relic.ModifyCardRewardCreationOptions(player,bypass).CardPools.SequenceEqual(new[]{player.Character.CardPool}),"Prismatic bypass expanded the pool.");
                var original=player.Character.CardPool.AllCards.First(c=>c.Rarity==CardRarity.Common);
                var constructor=typeof(CardCreationOptions).GetConstructor([typeof(IEnumerable<CardModel>),typeof(CardCreationSource),typeof(CardRarityOddsType)]);
                if(constructor!=null){var custom=((CardCreationOptions)constructor.Invoke([new[]{original},CardCreationSource.Other,CardRarityOddsType.Uniform])).WithFlags(CardCreationFlags.IsCardReward);
                    Require(((IEnumerable<CardModel>)AccessTools.Property(typeof(CardCreationOptions),"CustomCardPool").GetValue(relic.ModifyCardRewardCreationOptions(player,custom))!).SequenceEqual(new[]{original}),"Prismatic changed an explicit custom pool.");}
                var filter=CardCreationOptions.ForNonCombatWithUniformOdds(new[]{player.Character.CardPool},c=>c.Id==original.Id).WithFlags(CardCreationFlags.IsCardReward);
                Require(relic.ModifyCardRewardCreationOptions(player,filter).GetPossibleCards(player).Select(c=>c.Id).Distinct().SequenceEqual(new[]{original.Id}),"Prismatic lost the reward filter.");
                var draws=CardFactory.CreateForReward(player,12,CardCreationOptions.ForRoom(player,RoomType.Monster).WithFlags(CardCreationFlags.IsCardReward|CardCreationFlags.NoUpgradeRoll)).Select(r=>r.Card).ToArray();
                Require(draws.Length==12&&draws.Select(c=>c.Id).Distinct().Count()==12&&draws.All(c=>c.Owner==player&&!c.IsUpgraded),"Prismatic reward uniqueness/owner/no-upgrade flag.");
                var upgraded=0;foreach(var card in draws.Where(c=>c.IsUpgradable)){CardCmd.Upgrade(card);Require(card.IsUpgraded,"Generated card upgrade failed.");upgraded++;}
                results.Add(new{player.NetId,cls=player.Character is HeirCharacter?PlayerBloodlines.Heir(player).classId:"native",pools=options.CardPools.Select(p=>p.Id.ToString()).Order().ToArray(),legacy=possible.Order().ToArray(),ownerPoolOnly=possible.SetEquals(expected),draws=draws.Select(c=>c.Id.ToString()).ToArray(),upgraded,bypassPreserved=true,filterPreserved=true,customPreserved=constructor!=null});
                foreach(var card in draws)run.RemoveCard(card);
            }
            finally {player.RemoveRelicInternal(relic,silent:true);}
        }
        return results;
    }
    public static async Task<object> Library(RunState? run,bool heir)
    {
        var tree=(SceneTree)Engine.GetMainLoop();MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NSubmenuStack stack=run==null?MegaCrit.Sts2.Core.Nodes.NGame.Instance!.MainMenu!.SubmenuStack:MegaCrit.Sts2.Core.Nodes.NRun.Instance!.GlobalUi.SubmenuStack.Stack;
        var library=stack.GetSubmenuType<NCardLibrary>();library.Initialize(run!);
        try
        {
            stack.Push(library);
            if(heir){var filters=(Dictionary<CharacterModel,NCardPoolFilter>)AccessTools.Field(typeof(NCardLibrary),"_cardPoolFilters").GetValue(library)!;
                var filter=filters.Single(p=>p.Key is HeirCharacter).Value;filter.IsSelected=true;AccessTools.Method(typeof(NCardLibrary),"UpdateCardPoolFilter").Invoke(library,[filter]);}
            var deadline=Time.GetTicksMsec()+1000;while(Time.GetTicksMsec()<deadline)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            var grid=(NCardLibraryGrid)AccessTools.Field(typeof(NCardLibrary),"_grid").GetValue(library)!;
            var nodes=NativeTestBridge.Descendants(library).OfType<MegaCrit.Sts2.Core.Nodes.Cards.NCard>().ToArray();if(run==null)menuCards=nodes;
            var rendered=nodes.Select(c=>c.Model).Where(c=>c!=null).ToArray();
            var stale=new[]{"_portraitBlurMaterial","_canvasGroupMaskBlurMaterial","_canvasGroupBlurMaterial","_canvasGroupMaskMaterial"}.Where(name=>menuCards.Concat(nodes).Where(GodotObject.IsInstanceValid).Any(node=>AccessTools.Field(typeof(MegaCrit.Sts2.Core.Nodes.Cards.NCard),name).GetValue(node) is Material material&&!GodotObject.IsInstanceValid(material))).ToArray();
            return new{cls=run!=null&&MegaCrit.Sts2.Core.Context.LocalContext.GetMe(run)?.Character is HeirCharacter character?character.ClassId:Runtime.Heir.classId,rendered=rendered.Length,filtered=grid.VisibleCards.Count(),filteredLegacy=grid.VisibleCards.OfType<LegacyCard>().Select(c=>c.Key).Distinct().ToArray(),legacy=rendered.OfType<LegacyCard>().Select(c=>c.Key).Distinct().ToArray(),staleMaterials=stale};
        }
        finally {stack.Pop();var deadline=Time.GetTicksMsec()+500;while(Time.GetTicksMsec()<deadline)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
    }
    public static async Task<object> ExportRun(RunState run)
    {
        var player=run.Players.Single();var cls=player.Character is HeirCharacter?PlayerBloodlines.Heir(player).classId:"ironclad";
        foreach(var row in Design.Rows("branch_cards").Where(r=>r.Text("classId")==cls))
        {
            var card=run.CreateCard(NativeCards.Get(row.Text("cardId")),player);CardCmd.Upgrade(card);
            await CardPileCmd.Add(card,PileType.Deck,skipVisuals:true);
        }
        await SaveManager.Instance.SaveRun(null);var save=SaveManager.Instance.LoadRunSave();Require(save.Success&&save.SaveData!=null,"Run export failed.");
        File.WriteAllText(Path.Combine(Runtime.DataDirectory,"pool-run.json"),JsonSerializer.Serialize(save.SaveData!,JsonSerializationUtility.GetTypeInfo<SerializableRun>()));
        return new{cls,deck=player.Deck.Cards.OfType<LegacyCard>().Select(c=>new{key=c.Key,upgraded=c.IsUpgraded}).ToArray()};
    }
    public static async Task<object> ImportRun()
    {
        if(MegaCrit.Sts2.Core.Nodes.NRun.Instance!=null)await MegaCrit.Sts2.Core.Nodes.NGame.Instance!.ReturnToMainMenu();
        var save=JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(Runtime.DataDirectory,"pool-run.json")),JsonSerializationUtility.GetTypeInfo<SerializableRun>())!;
        var run=RunState.FromSerializable(save);await RunManager.Instance.SetUpSavedSingleplayer(run,save);
        await MegaCrit.Sts2.Core.Nodes.NGame.Instance!.LoadRun(run,save.PreFinishedRoom);
        var player=run.Players.Single();return new{cls=player.Character is HeirCharacter?PlayerBloodlines.Heir(player).classId:"ironclad",deck=player.Deck.Cards.OfType<LegacyCard>().Select(c=>new{key=c.Key,upgraded=c.IsUpgraded}).ToArray()};
    }
}
