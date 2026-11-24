using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

[HarmonyPatch]
internal static class HeirCompendiumPool
{
    internal const string SelectorName = "SpireLegacyHeirClasses";
    private static readonly string[] Labels = ["All Classes", "Knight", "Mage", "Ranger"];

    private static CardPoolModel[] ClassPools() =>
        [ModelDb.CardPool<KnightCardPool>(), ModelDb.CardPool<MageCardPool>(), ModelDb.CardPool<RangerCardPool>()];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
    private static void SetInitialPool(NCardLibrary __instance, Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters,
        Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters, IRunState? ____runState)
    {
        var heir = FindHeir(____cardPoolFilters);
        if (heir is null) return;

        if (____runState is { } run)
        {
            // The library opened from a run uses the local player's frozen class.
            var character = LocalContext.GetMe(run)?.Character as HeirCharacter ?? ModelDb.Character<HeirCharacter>();
            var pool = character.CardPool;
            ____poolFilters[heir] = card => pool.AllCardIds.Contains(card.Id);
            return;
        }

        var pools = ClassPools();
        var all = pools.SelectMany(pool => pool.AllCardIds).ToHashSet();
        ____poolFilters[heir] = card => all.Contains(card.Id);
        var selector = GetSelector(__instance) ?? AddSelector(__instance, heir, index =>
        {
            if (!heir.IsSelected) return;
            if (index == 0) ____poolFilters[heir] = card => all.Contains(card.Id);
            else
            {
                var pool = pools[index - 1];
                ____poolFilters[heir] = card => pool.AllCardIds.Contains(card.Id);
            }
            heir.EmitSignal(NCardPoolFilter.SignalName.Toggled, heir);
        });
        if (selector is null) return;

        var buttons = selector.GetChildren().OfType<Button>().ToArray();
        for (var index = 0; index < buttons.Length; index++)
        {
            buttons[index].Text = HeirLocalization.Text(Labels[index]);
            HeirLocalization.ApplyFont(buttons[index]);
            buttons[index].SetPressedNoSignal(index == 0);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
    private static void OnOpened(NCardLibrary __instance, Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters, IRunState? ____runState) =>
        UpdateSelectorVisibility(__instance, ____cardPoolFilters, ____runState);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCardLibrary), "UpdateCardPoolFilter")]
    private static void OnFilterChanged(NCardLibrary __instance, Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters, IRunState? ____runState) =>
        UpdateSelectorVisibility(__instance, ____cardPoolFilters, ____runState);

    private static void UpdateSelectorVisibility(NCardLibrary library, Dictionary<CharacterModel, NCardPoolFilter> filters, IRunState? run)
    {
        if (FindHeir(filters) is { } heir && GetSelector(library) is { } selector)
            selector.Visible = run is null && heir.IsSelected;
    }

    private static NCardPoolFilter? FindHeir(Dictionary<CharacterModel, NCardPoolFilter> filters) =>
        filters.FirstOrDefault(entry => entry.Key is HeirCharacter).Value;

    private static HBoxContainer? GetSelector(NCardLibrary library) =>
        library.GetNodeOrNull<HBoxContainer>(SelectorName);

    private static HBoxContainer? AddSelector(NCardLibrary library, NCardPoolFilter heir, Action<int> select)
    {
        if (library.GetNodeOrNull<Control>("%CardGrid") is not { } grid) return null;

        var selector = new HBoxContainer { Name = SelectorName, Visible = false };
        selector.AddThemeConstantOverride("separation", 8);
        library.AddChild(selector);
        var group = new ButtonGroup();
        var buttons = new List<Button>();
        for (var index = 0; index < Labels.Length; index++)
        {
            var choice = index;
            var button = ManorUi.MakeButton(Labels[index], () => select(choice), $"HeirClass{index}");
            button.ToggleMode = true;
            button.ButtonGroup = group;
            button.CustomMinimumSize = new(120, 38);
            button.AddThemeFontSizeOverride("font_size", 20);
            foreach (var state in new[] { "normal", "hover" })
            {
                var surface = (StyleBoxFlat)button.GetThemeStylebox(state);
                surface.ContentMarginLeft = surface.ContentMarginRight = 8;
                surface.ContentMarginTop = surface.ContentMarginBottom = 4;
            }
            var pressed = (StyleBoxFlat)button.GetThemeStylebox("normal").Duplicate();
            pressed.BgColor = new("514228");
            pressed.BorderColor = new("D0A75F");
            button.AddThemeStyleboxOverride("pressed", pressed);
            button.GuiInput += input =>
            {
                if (!input.IsActionPressed(MegaInput.select) || input.IsActionPressed("ui_accept")) return;
                button.AcceptEvent();
                button.ButtonPressed = true;
                select(choice);
            };
            selector.AddChild(button);
            buttons.Add(button);
        }

        var previousBottom = heir.FocusNeighborBottom;
        var gridTop = grid.OffsetTop;
        var clipsGrid = grid.ClipContents;
        void PositionSelector() => selector.Position = new(grid.Position.X + 16, grid.Position.Y - selector.Size.Y - 8);
        grid.Resized += PositionSelector;
        selector.VisibilityChanged += () =>
        {
            heir.FocusNeighborBottom = selector.Visible ? buttons[0].GetPath() : previousBottom;
            // Give the selector its own row above the grid at every scroll position.
            grid.OffsetTop = gridTop + (selector.Visible ? selector.GetCombinedMinimumSize().Y + 16 : 0);
            grid.ClipContents = selector.Visible || clipsGrid;
            PositionSelector();
        };
        buttons[0].FocusNeighborLeft = heir.GetPath();
        selector.Resized += PositionSelector;
        return selector;
    }
}
