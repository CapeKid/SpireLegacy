using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace SpireLegacy;

internal static class HeirSelectionBackground
{
    internal const string NodeName = "SpireLegacyBackground";
    internal const string TexturePath = "heir/ui/select-bg.png";

    internal static void Apply(Control background)
    {
        foreach (var child in background.GetChildren().OfType<CanvasItem>())
            child.Hide();

        var art = background.GetNodeOrNull<TextureRect>(NodeName);
        if (art is null)
        {
            art = new TextureRect
            {
                Name = NodeName,
                Texture = Runtime.Texture(TexturePath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            background.AddChild(art);
            art.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        art.Show();
    }
}

[HarmonyPatch]
internal static class HeirRandomSelectionBackground
{
    internal static MethodBase[] RevealMethods()
    {
        return new[] { typeof(NCharacterSelectScreen), HeirBlitzBackground.ScreenType }.OfType<Type>()
            .Select(screen => AccessTools.Method(screen, "DoRandomCharacterChange", [typeof(Player)]))
            .OfType<MethodBase>().ToArray();
    }
    private static IEnumerable<MethodBase> TargetMethods() => RevealMethods();

    private static bool Prefix(Player __0, Control ____bgContainer, ref Task __result)
    {
        if (__0.Character is not HeirCharacter heir) return true;

        // The native reveal looks up the character's packed scene, which falls back to Ironclad for the Heir.
        Reveal(heir, ____bgContainer);
        __result = Cmd.Wait(1, false);
        return false;
    }

    internal static void Reveal(HeirCharacter heir, Control background)
    {
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short, 90);
        SfxCmd.Play(heir.CharacterSelectSfx);
        HeirSelectionBackground.Apply(background);
    }
}

[HarmonyPatch]
internal static class HeirRegularRandomBackground
{
    internal static readonly MethodInfo? Selection = AccessTools.Method(typeof(NCharacterSelectScreen),
        "OnLocalCharacterChangedForRandom", [typeof(CharacterModel)]);

    private static MethodBase? TargetMethod() => Selection;

    private static bool Prefix(CharacterModel __0, Control ____bgContainer, ref bool ____delayEmbarkForCharacterSelect)
    {
        if (__0 is not HeirCharacter heir) return true;
        HeirRandomSelectionBackground.Reveal(heir, ____bgContainer);
        ____delayEmbarkForCharacterSelect = true;
        return false;
    }
}

[HarmonyPatch]
internal static class HeirBlitzBackground
{
    internal static readonly Type? ScreenType = typeof(CharacterModel).Assembly
        .GetType("MegaCrit.Sts2.Core.Nodes.Screens.Blitz.NBlitzScreen");
    internal static readonly MethodInfo? Selection = ScreenType == null ? null :
        AccessTools.Method(ScreenType, "SelectCharacter", [typeof(NCharacterSelectButton)]);

    private static MethodBase? TargetMethod() => Selection;

    private static void Postfix(Control __instance, NCharacterSelectButton __0, Control ____bgContainer, Control ____backButton)
    {
        var showsHeir = __0.Character is HeirCharacter;
        ManorUi.AttachMode(__instance, ____backButton, showsHeir);
        if (showsHeir && !__0.IsLocked)
            HeirSelectionBackground.Apply(____bgContainer);
    }
}

[HarmonyPatch]
internal static class HeirDreadBackground
{
    internal static readonly Type? ScreenType = typeof(CharacterModel).Assembly
        .GetType("MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen");
    private static readonly Type? RotatingCharacter = typeof(CharacterModel).Assembly
        .GetType("MegaCrit.Sts2.Core.Models.Characters.RotatingCharacter");
    internal static readonly MethodInfo? SetVisuals = ScreenType == null ? null :
        AccessTools.Method(ScreenType, "SetSelectedCharacterVisuals", [typeof(CharacterModel)]);

    private static MethodBase? TargetMethod() => SetVisuals;

    private static void Postfix(Control __instance, CharacterModel __0, Control ____bgContainer, Control ____backButton,
        CharacterModel? ____rotatingDisplayCharacterOverride, CharacterModel? ____rotatingCharacter)
    {
        // During a rotation, the button represents the character currently shown in the preview.
        var displayed = __0.GetType() == RotatingCharacter
            ? ____rotatingDisplayCharacterOverride ?? ____rotatingCharacter ?? __0
            : __0;
        var showsHeir = displayed is HeirCharacter;
        ManorUi.AttachMode(__instance, ____backButton, showsHeir);
        if (showsHeir)
            HeirSelectionBackground.Apply(____bgContainer);
    }
}
