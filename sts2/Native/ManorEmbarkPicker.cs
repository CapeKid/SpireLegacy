using Godot;
using L = SpireLegacy.HeirLocalization;

namespace SpireLegacy;

public static partial class ManorUi
{
    internal static void OpenEmbarkPicker(Control screen, bool multiplayer, Func<bool> canEmbark, Action embark)
    {
        if (IsOpen || Runtime.Profile.offers.Count == 0) return;

        var offers = Runtime.Profile.offers.ToArray();
        var selected = Array.FindIndex(offers, heir => SameOffer(heir, Runtime.Profile.selected));
        selected = Math.Max(0, selected);
        var cards = new List<(PanelContainer panel, Button button)>();
        var message = Text("", 20);
        message.Modulate = new("FFBC9D");
        message.Hide();
        var confirm = MakeButton("", Confirm, "SpireLegacyConfirmHeir");
        confirm.CustomMinimumSize = new(235, 60);
        var gold = PickerSurface(new("EBC785"), new("F9DCA3"), 2);
        confirm.AddThemeStyleboxOverride("normal", gold);
        confirm.AddThemeStyleboxOverride("hover", PickerSurface(new("FFE2A9"), new("FFF0CA"), 2));
        foreach (var color in new[] { "font_color", "font_hover_color", "font_focus_color", "font_pressed_color" })
            confirm.AddThemeColorOverride(color, new("281C18"));

        OpenModal(() =>
        {
            back = Close;
            BeginPage("embark-heir");
            var heading = Text("Choose your next heir", 39);
            heading.Modulate = new("F6DDB0");
            content!.AddChild(heading);
            AddText(L.Format($"The {Runtime.Profile.family} family"), 22);
            content.AddChild(message);
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 14);
            content.AddChild(row);
            for (var i = 0; i < offers.Length; i++)
            {
                var index = i;
                var card = PickerCard(offers[i], () => { selected = index; UpdateSelection(); }, i);
                cards.Add(card);
                row.AddChild(card.panel);
            }
            UpdateSelection();
            ConfigureNavigation(cards[selected].button.Name);
        }, screen, confirm);

        void UpdateSelection()
        {
            for (var i = 0; i < cards.Count; i++)
            {
                var current = i == selected;
                cards[i].panel.AddThemeStyleboxOverride("panel", current
                    ? PickerSurface(new("382E3E"), new("F0CC88"), 3)
                    : PickerSurface(new("2D2736"), new("615665"), 3));
                cards[i].button.SetPressedNoSignal(current);
            }
            var name = offers[selected].name;
            confirm.Text = multiplayer ? L.Format($"Ready as {name}") : L.Format($"Embark as {name}");
        }

        void Confirm()
        {
            if (!ValidateContext() || !canEmbark() || !CanConfigure()) { Close(); return; }
            var heir = Runtime.Profile.offers.FirstOrDefault(offer => SameOffer(offer, offers[selected]));
            if (heir == null) { Close(); return; }
            try
            {
                if (!Runtime.Profile.CommitHeirSelection(heir, Runtime.FamilyPath)) return;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                GD.PushWarning("Could not save the selected heir: " + error.Message);
                message.Text = L.Text("Could not save your heir selection. Check your family save and try again.");
                message.Show();
                return;
            }
            Notice = null;
            Close();
            embark();
        }
    }

    private static bool SameOffer(FamilyProfile.Heir offer, FamilyProfile.Heir? selected) =>
        selected != null && offer.classId == selected.classId && offer.name == selected.name && offer.traits.SequenceEqual(selected.traits);

    private static (PanelContainer panel, Button button) PickerCard(FamilyProfile.Heir heir, Action select, int index)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
        var padding = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        foreach (var side in new[] { "left", "right", "top", "bottom" }) padding.AddThemeConstantOverride("margin_" + side, 16);
        panel.AddChild(padding);
        var card = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        card.AddThemeConstantOverride("separation", 8);
        padding.AddChild(card);

        var art = Design.Get("character_art", heir.classId);
        var frame = art.Json.GetProperty("regions")[0];
        card.AddChild(new TextureRect
        {
            CustomMinimumSize = new(0, 178),
            Texture = new AtlasTexture { Atlas = Runtime.Texture(art.Text("path")), Region = new Rect2(frame[0].GetInt32(), frame[1].GetInt32(), frame[2].GetInt32(), frame[3].GetInt32()) },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        card.AddChild(Text(heir.name, 30));
        var classLabel = Text(Design.Get("classes", heir.classId).Localized("name"), 22);
        classLabel.Modulate = new("F0CC88");
        card.AddChild(classLabel);
        foreach (var trait in Inheritance.Rows(heir))
        {
            var name = Text(trait.Localized("name"), 23);
            name.Modulate = new("F1D5A7");
            card.AddChild(name);
            card.AddChild(Text(trait.Localized("summary"), 20));
        }
        var crownBonus = Math.Clamp(Inheritance.Rows(heir).Sum(trait => trait.Number("goldBonus")), -100, 100);
        var crowns = Text(crownBonus == 0 ? "Standard legacy crowns" : L.Format($"{crownBonus:+0;-0}% legacy crowns"), 20);
        crowns.Modulate = new("F0CC88");
        card.AddChild(crowns);

        var button = MakeButton("", select, "SpireLegacyHeirOffer_" + index);
        button.ToggleMode = true;
        foreach (var style in new[] { "normal", "hover", "pressed", "hover_pressed" })
            button.AddThemeStyleboxOverride(style, new StyleBoxEmpty());
        panel.AddChild(button);
        return (panel, button);
    }

    private static StyleBoxFlat PickerSurface(Color fill, Color border, int width) => new()
    {
        BgColor = fill, BorderColor = border,
        BorderWidthLeft = width, BorderWidthTop = width, BorderWidthRight = width, BorderWidthBottom = width,
        CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
    };
}
