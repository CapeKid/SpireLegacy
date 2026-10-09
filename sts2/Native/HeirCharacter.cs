using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace SpireLegacy;

public sealed class HeirCharacter : PlaceholderCharacterModel
{
    public override Color NameColor => new("C4A464");
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override Control CustomIcon => new TextureRect { Texture = Runtime.Texture("heir/ui/select-icon.png"), CustomMinimumSize = new(64,64), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
    public override int StartingHp => Inheritance.MaxHp(Design.Get("classes", Runtime.Heir.classId).Number("hp") + Runtime.Bonus("hp"));
    public override CardPoolModel CardPool => Runtime.Heir.classId switch { "mage" => ModelDb.CardPool<MageCardPool>(), "ranger" => ModelDb.CardPool<RangerCardPool>(), _ => ModelDb.CardPool<KnightCardPool>() };
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<HeirRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<SharedPotionPool>();
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<BloodlineRelic>()];
    public override IEnumerable<CardModel> StartingDeck
    {
        get
        {
            var cls = Runtime.Heir.classId;
            foreach (var key in Inheritance.Deck(cls, Runtime.Profile.generation)) yield return NativeCards.Get(key);
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
    public override string EnergyColorName => "ironclad";
    public override Color DeckEntryCardColor => new("C4A464");
    public override bool IsColorless => false;
    protected override CardModel[] GenerateAllCards() => Design.Pool(ClassId).Select(r => NativeCards.Get(r.Text("id"))).ToArray();
}
public sealed class KnightCardPool : FamilyCardPool { protected override string ClassId => "knight"; }
public sealed class MageCardPool : FamilyCardPool { protected override string ClassId => "mage"; }
public sealed class RangerCardPool : FamilyCardPool { protected override string ClassId => "ranger"; }
public sealed class HeirRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "ironclad";
    protected override RelicModel[] GenerateAllRelics() => [ModelDb.Relic<BloodlineRelic>()];
}
public static class NativeCards
{
    private static readonly Dictionary<string, Type> Types = typeof(NativeCards).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(LegacyCard).IsAssignableFrom(t)).ToDictionary(t => t.Name[2..]);
    public static CardModel Get(string key) => ModelDb.GetById<CardModel>(ModelDb.GetId(Types[key]));
}
