using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace SpireLegacy;

public sealed class HeirCharacter : PlaceholderCharacterModel
{
    internal FamilyProfile.FamilyRun? Bloodline {get;set;}
    public string ClassId=>Bloodline?.heir.classId??Runtime.Heir.classId;
    private HeirInheritance Inheritance=>new(Bloodline?.heir??Runtime.Heir);
    private int Bonus(string effect)=>Bloodline?.bonuses.GetValueOrDefault(effect)??Runtime.Bonus(effect);
    public override Color NameColor => new("C4A464");
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override Control CustomIcon => new TextureRect { Texture = Runtime.ClassIcon(ClassId), CustomMinimumSize = new(64,64), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
    public override string CustomIconTexturePath=>HeirPresentation.IconFor(ClassId);
    public override string CustomIconOutlineTexturePath=>HeirPresentation.TexturePath(ClassId+"-outline");
    public override CustomEnergyCounter? CustomEnergyCounter=>new(i=>HeirPresentation.TexturePath(ClassId+"-energy-"+i),new Color("20232D"),HeirPresentation.Accent(ClassId));
    public override string CustomTrailPath=>HeirPresentation.TrailFor(ClassId);
    public override string CustomCharacterSelectTransitionPath=>HeirPresentation.TransitionFor(ClassId);
    public override string CharacterSelectSfx=>HeirPresentation.SoundFor(ClassId,"select");
    public override string CharacterTransitionSfx=>HeirPresentation.SoundFor(ClassId,"transition");
    public override string CustomAttackSfx=>HeirPresentation.SoundFor(ClassId,"attack");
    public override string CustomCastSfx=>HeirPresentation.SoundFor(ClassId,"cast");
    public override string CustomDeathSfx=>HeirPresentation.SoundFor(ClassId,"die");
    public override Color EnergyLabelOutlineColor=>new("20232D");
    public override Color DialogueColor=>new("3E344A");
    public override Color MapDrawingColor=>HeirPresentation.Accent(ClassId);
    public override Color RemoteTargetingLineColor=>HeirPresentation.Accent(ClassId);
    public override List<string> GetArchitectAttackVfx()=>Enumerable.Range(0,5).Select(i=>"spirelegacy/architect/"+ClassId+"/"+i).ToList();
    public override int StartingHp => Inheritance.MaxHp(Design.Get("classes", ClassId).Number("hp") + Bonus("hp"));
    public override CardPoolModel CardPool => ClassId switch { "mage" => ModelDb.CardPool<MageCardPool>(), "ranger" => ModelDb.CardPool<RangerCardPool>(), _ => ModelDb.CardPool<KnightCardPool>() };
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<HeirRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<HeirPotionPool>();
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<BloodlineRelic>()];
    public override IEnumerable<CardModel> StartingDeck
    {
        get
        {
            var cls = ClassId;
            foreach (var key in Inheritance.Deck(cls, Bloodline?.generation??Runtime.Profile.generation)) yield return NativeCards.Get(key);
        }
    }
    public override List<(string, string)> Localization => [
        ("title", "The Heir"), ("titleObject", "The Heir"), ("description", "A family of Knight, Mage and Ranger heirs. Inherited traits shape each climb; crowns rebuild the Family Manor."),
        ("pronounObject", "them"), ("pronounSubject", "they"), ("pronounPossessive", "theirs"), ("possessiveAdjective", "their"), ("cardsModifierTitle", "Heir Cards"), ("cardsModifierDescription", "Shared family cards and class specialties."),
        ("aromaPrinciple", "Your family taught you to hone what you already carry."),
        ("goldMonologue", "Another fortune for the family, another burden for its heir.") ];
}

public abstract class FamilyCardPool : CustomCardPoolModel
{
    protected abstract string ClassId { get; }
    public override string Title => "Spire Legacy";
    public override string BigEnergyIconPath=>HeirPresentation.TexturePath(ClassId+"-energy-icon");
    public override string TextEnergyIconPath=>BigEnergyIconPath;
    public override Color ShaderColor=>HeirPresentation.Accent(ClassId);
    public override Color DeckEntryCardColor => new("C4A464");
    public override bool IsColorless => false;
    protected override CardModel[] GenerateAllCards() => NativePoolParity.Rows(ClassId).Concat(Design.Rows("ancient_cards").Where(r=>r.Text("classId")==ClassId).Select(r=>Design.Get("cards",r.Text("cardId")))).Select(r => NativeCards.Get(r.Text("id"))).ToArray();
}
public sealed class KnightCardPool : FamilyCardPool { protected override string ClassId => "knight"; }
public sealed class MageCardPool : FamilyCardPool { protected override string ClassId => "mage"; }
public sealed class RangerCardPool : FamilyCardPool { protected override string ClassId => "ranger"; }
public sealed class HeirRelicPool : RelicPoolModel,ICustomEnergyIconPool
{
    public override string EnergyColorName=>BaseLib.Patches.UI.CustomEnergyIconPatches.GetEnergyColorName(Id);
    public string BigEnergyIconPath=>HeirPresentation.TexturePath(HeirPresentation.ClassId+"-energy-icon");
    public string TextEnergyIconPath=>BigEnergyIconPath;
    protected override RelicModel[] GenerateAllRelics() => [ModelDb.Relic<BloodlineRelic>(),..HeirDropRelics.All];
    public override IEnumerable<RelicModel> GetUnlockedRelics(MegaCrit.Sts2.Core.Unlocks.UnlockState unlockState)=>AllRelics.Where(r=>r is not FamilyDropRelic relic||(PlayerBloodlines.EligibilityOwner.Value is {} player ? PlayerBloodlines.Eligible(player,relic.Key) : FamilyChronicle.RelicAvailable(Runtime.Profile,relic.Key)));
}
public static class NativeCards
{
    private static readonly Dictionary<string, Type> Types = typeof(NativeCards).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(LegacyCard).IsAssignableFrom(t)).ToDictionary(t => t.Name[2..]);
    public static CardModel Get(string key) => ModelDb.GetById<CardModel>(ModelDb.GetId(Types[key]));
}
