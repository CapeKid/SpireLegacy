using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace SpireLegacy;

// Only invoked by the opt-in isolated game bridge.
internal static class RandomEmbarkOracle
{
    internal static async Task<object> Solo(bool blitz, bool heir, bool unseeded, bool stuck)
    {
        var game = NGame.Instance!;
        var tree = (SceneTree)Engine.GetMainLoop();
        if (NRun.Instance is not null) await game.ReturnToMainMenu();
        NModalContainer.Instance?.Clear();
        SaveManager.Instance.SetFtuesEnabled(false);
        Runtime.Profile.GenerateOffers(78);
        Runtime.Profile.selected = null;
        var stack = game.MainMenu!.SubmenuStack;
        var type = blitz ? HeirBlitzBackground.ScreenType ?? throw new InvalidOperationException("Blitz is unavailable.")
            : typeof(NCharacterSelectScreen);
        var submenu = stack.GetSubmenuType(type);
        var screen = (Control)submenu;
        AccessTools.Method(type, "InitializeSingleplayer").Invoke(screen, []);
        stack.Push(submenu);
        NModalContainer.Instance?.Clear();
        var random = NativeTestBridge.Descendants(screen).OfType<NCharacterSelectButton>()
            .Single(button => button.Character is RandomCharacter);
        random.DebugUnlock();
        if (screen is NCharacterSelectScreen standard) ManorCharacterMenu.Select(standard, random);
        else HeirBlitzBackground.Selection!.Invoke(screen, [random]);
        random.GrabFocus();

        var lobby = CoopBloodlines.Lobby!;
        var oldDebugSeed = game.DebugSeedOverride;
        game.DebugSeedOverride = null;
        try
        {
            if (!unseeded)
            {
                var wanted = heir ? (CharacterModel)ModelDb.Character<HeirCharacter>() : ModelDb.Character<Ironclad>();
                var seed = Enumerable.Range(1, 1000).Select(i => SeedHelper.CanonicalizeSeed("RANDOM" + i))
                    .First(candidate => RandomHeirEmbark.Resolve(lobby, candidate).Id == wanted.Id);
                // Standard has no editable seed. Exercise the native debug seed override.
                game.DebugSeedOverride = seed;
            }

            var roll = RandomHeirEmbark.Preview(lobby);
            var expected = RandomHeirEmbark.Resolve(lobby, roll.Seed);
            Require(unseeded ? lobby.Seed is null : roll.IsHeir == heir, "The seed fixture resolved the wrong character.");
            var embark = AccessTools.Method(type, "OnEmbarkPressed");
            var button = AccessTools.Field(type, "_embarkButton").GetValue(screen);
            var (oldActive, oldSelected) = (Runtime.Profile.active, Runtime.Profile.selected);
            if (stuck)
            {
                Runtime.Profile.Begin(78);
                try
                {
                    embark.Invoke(screen, [button]);
                    Require(Find("ManorClearStuckRun") is not null && Find("SpireLegacyConfirmHeir") is null && NRun.Instance is null,
                        "A Random Heir skipped stuck-run recovery.");
                }
                finally
                {
                    ManorUi.Close();
                    Runtime.Profile.active = oldActive;
                    Runtime.Profile.selected = oldSelected;
                }
            }
            embark.Invoke(screen, [button]);
            FamilyProfile.Heir? chosen = null;
            if (expected is HeirCharacter)
            {
                Require(Find("SpireLegacyConfirmHeir") is not null && NRun.Instance is null, "The Heir picker did not open before starting.");
                ManorUi.Close();
                Require(ReferenceEquals(roll, RandomHeirEmbark.Preview(lobby)) && NRun.Instance is null,
                    "Cancelling should keep the Random result and stay at character selection.");
                embark.Invoke(screen, [button]);
                var options = blitz ? Runtime.Profile.BlitzOffers() : Runtime.Profile.offers.ToArray();
                var choice = Array.FindIndex(options, offer => offer.classId == "mage");
                chosen = options[choice];
                Find("SpireLegacyHeirOffer_" + choice)!.EmitSignal(Button.SignalName.Pressed);
                Find("SpireLegacyConfirmHeir")!.EmitSignal(Button.SignalName.Pressed);
            }
            else Require(Find("SpireLegacyConfirmHeir") is null, "A base character rolled into the Heir picker.");

            var background = (Control)AccessTools.Field(type, "_bgContainer").GetValue(screen)!;
            bool HasReveal() => expected is HeirCharacter
                ? background.GetNodeOrNull<TextureRect>(HeirSelectionBackground.NodeName)?.IsVisibleInTree() == true
                    && background.GetChildren().OfType<CanvasItem>().Count(node => node.Visible) == 1
                : background.GetChildren().OfType<Control>()
                    .Any(node => node.IsVisibleInTree() && node.SceneFilePath == expected.CharacterSelectBg);
            bool revealed = HasReveal();
            var deadline = Time.GetTicksMsec() + 22000;
            while (NRun.Instance is null && Time.GetTicksMsec() < deadline)
            {
                revealed |= HasReveal();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            var run = (RunState?)AccessTools.Property(typeof(RunManager), "State").GetValue(RunManager.Instance);
            var player = run?.Players.Single();
            Require(revealed && player?.Character.Id == expected.Id,
                $"Random reveal mismatch. Background={revealed}, expected={expected.Id}, launched={player?.Character.Id}.");
            if (chosen is not null)
            {
                var launched = PlayerBloodlines.Heir(player!);
                Require(launched.classId == chosen.classId && launched.name == chosen.name && launched.traits.SequenceEqual(chosen.traits),
                    "The chosen heir did not carry into the Random run.");
            }
            return new { blitz, heir = expected is HeirCharacter, unseeded, revealed, chosen = chosen?.classId, stuck };
        }
        finally { game.DebugSeedOverride = oldDebugSeed; }
    }

    internal static object CoOpPicker()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var screen = NativeTestBridge.Descendants(tree.Root).OfType<NCharacterSelectScreen>().Single(node => node.IsVisibleInTree());
        var lobby = screen.Lobby;
        Require(lobby.NetService.Type == NetGameType.Host && CoopBloodlines.Character(CoopBloodlines.Local(lobby)) is RandomCharacter,
            "Expected a Random co-op host fixture.");
        ManorAuditOracle.OpenEmbarkPicker();
        Require(Find("SpireLegacyConfirmHeir") is not null && !CoopBloodlines.IsReady(CoopBloodlines.Local(lobby)),
            "Random co-op must show the picker before sending Ready.");
        ManorUi.Close();
        return new { beforeReady = true };
    }

    private static Button? Find(string name) =>
        NativeTestBridge.Descendants(((SceneTree)Engine.GetMainLoop()).Root).OfType<Button>()
            .SingleOrDefault(button => button.IsVisibleInTree() && button.Name == name);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
