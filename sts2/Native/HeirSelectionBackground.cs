using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

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
