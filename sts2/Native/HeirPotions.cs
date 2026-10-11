using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace SpireLegacy;

public sealed class HeirPotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName=>"ironclad";
    public override Color LabOutlineColor=>new("C4A464");
    protected override IEnumerable<PotionModel> GenerateAllPotions()=>HeirPotions.All;
}
public static class HeirPotions
{
    private static readonly Dictionary<string,Type> types=typeof(HeirPotions).Assembly.GetTypes()
        .Where(t=>!t.IsAbstract&&typeof(FamilyPotion).IsAssignableFrom(t)).ToDictionary(t=>t.Name[2..]);
    public static PotionModel Get(string key)=>ModelDb.GetById<PotionModel>(ModelDb.GetId(types[key]));
    public static IEnumerable<PotionModel> All=>Design.Rows("heir_potions").Select(r=>Get(r.Text("id")));
}
public abstract class FamilyPotion : CustomPotionModel
{
    protected FamilyPotion():base(false) { }
    public string Key=>GetType().Name[2..];
    public Design.Row Row=>Design.Get("heir_potions",Key);
    public override PotionRarity Rarity=>Enum.Parse<PotionRarity>(Row.Text("rarity"));
    public override PotionUsage Usage=>PotionUsage.CombatOnly;
    public override TargetType TargetType=>Enum.Parse<TargetType>(Row.Text("target"));
    public override List<(string,string)> Localization=>[("title",Row.Text("title")),("description",Row.Text("description"))];
    public override string CustomPackedImagePath=>TexturePath("path");
    public override string CustomPackedOutlinePath=>TexturePath("outline");
    public override string CustomLargeImagePath=>TexturePath("path");
    private string TexturePath(string field)
    {
        var relative=Design.Get("potion_art",Key).Text(field);var path="res://SpireLegacy/"+relative;
        var texture=Runtime.Texture(relative);if(texture.ResourcePath!=path)texture.TakeOverPath(path);return path;
    }
    public override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if(Key=="briar_vial") {yield return HoverTipFactory.FromPower<PoisonPower>();yield return HoverTipFactory.FromPower<WeakPower>();}
            else {yield return HoverTipFactory.Static(StaticHoverTip.Block);if(Key=="triune_draught")yield return HoverTipFactory.FromPower<VigorPower>();}
        }
    }
    protected override async Task OnUse(PlayerChoiceContext context,Creature? target)
    {
        AssertValidForTargetedPotion(target);
        if(!IsValidTarget(target))throw new InvalidOperationException("Invalid family potion target.");
        if(Key=="briar_vial")
        {
            await PowerCmd.Apply<PoisonPower>(context,target,Row.Number("amount"),Owner.Creature,null);
            await PowerCmd.Apply<WeakPower>(context,target,Row.Number("extra"),Owner.Creature,null);return;
        }
        if(Key=="borrowed_breath")
        {
            await CreatureCmd.GainBlock(target,Row.Number("amount"),ValueProp.Unpowered,null);
            await CardPileCmd.Draw(context,Row.Number("extra"),Owner);return;
        }
        switch(Runtime.Heir.classId)
        {
            case "knight":
                await CreatureCmd.GainBlock(target,Row.Number("amount"),ValueProp.Unpowered,null);
                await PowerCmd.Apply<VigorPower>(context,target,Row.Number("extra"),Owner.Creature,null);break;
            case "mage":
                if(target.GetPower<P_class>() is {} charge)charge.Charges=Math.Min(2,charge.Charges+Row.Number("extra"));
                await CardPileCmd.Draw(context,1,Owner);break;
            case "ranger":
                await PowerCmd.Apply<VigorPower>(context,target,3,Owner.Creature,null);
                await CardPileCmd.Draw(context,Row.Number("extra"),Owner);break;
        }
    }
}
public sealed class T_borrowed_breath : FamilyPotion { }
public sealed class T_briar_vial : FamilyPotion { }
public sealed class T_triune_draught : FamilyPotion { }

// Keep direct native texture call sites on original distributed artwork as well.
[HarmonyPatch]
public static class HeirPotionArt
{
    public static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{"get_Image","get_Outline"}.Select(name=>AccessTools.Method(typeof(PotionModel),name));
    public static bool Prefix(PotionModel __instance,System.Reflection.MethodBase __originalMethod,ref Texture2D __result)
    {
        if(__instance is not FamilyPotion potion)return true;
        var outline=__originalMethod.Name.Contains("Outline");
        __result=Runtime.Texture(Design.Get("potion_art",potion.Key).Text(outline?"outline":"path"));return false;
    }
}
[HarmonyPatch]
public static class HeirPotionPaths
{
    public static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{"get_ImagePath","get_OutlinePath"}.Select(name=>AccessTools.Method(typeof(PotionModel),name));
    public static bool Prefix(PotionModel __instance,System.Reflection.MethodBase __originalMethod,ref string __result)
    {
        if(__instance is not FamilyPotion potion)return true;
        __result=__originalMethod.Name.Contains("Outline")?potion.CustomPackedOutlinePath:potion.CustomPackedImagePath;return false;
    }
}
