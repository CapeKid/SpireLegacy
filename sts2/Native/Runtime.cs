using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace SpireLegacy;

[ModInitializer(nameof(Initialize))]
public static class Runtime
{
    public static string ModDirectory => Path.GetDirectoryName(typeof(Runtime).Assembly.Location)!;
    public static string DataDirectory => System.Environment.GetEnvironmentVariable("HEIR_DATA_DIR") ?? Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "SpireLegacy2");
    public static string FamilyPath => Path.Combine(DataDirectory, "family.json");
    public static FamilyProfile Profile { get; private set; } = new();
    public static FamilyProfile.Heir Heir => Profile.active?.heir ?? Profile.selected ?? Profile.offers.First();
    private static readonly Dictionary<string, Texture2D> Textures = new();
    public static Texture2D Texture(string path)
    {
        if (Textures.TryGetValue(path, out var texture)) return texture;
        var image = Image.LoadFromFile(Path.Combine(ModDirectory, path));
        if (image.IsEmpty()) throw new InvalidDataException("Missing mod illustration: " + path);
        return Textures[path] = ImageTexture.CreateFromImage(image);
    }
    public static Texture2D CacheTexture(string name)
    {
        var key = "cache:" + name;
        if (Textures.TryGetValue(key,out var result)) return result;
        var image = Image.LoadFromFile(Path.Combine(ContentBootstrap.CacheDirectory,name));
        if (image.IsEmpty()) throw new InvalidDataException("Missing owned-content image: " + name);
        return Textures[key] = ImageTexture.CreateFromImage(image);
    }
    public static void Initialize()
    {
        if (System.Environment.GetEnvironmentVariable("HEIR_TEST_MODE") == "1")
        {
            var existingSettings = MegaCrit.Sts2.Core.Saves.SaveManager.Instance.SettingsSave;
            var localStore = new MegaCrit.Sts2.Core.Saves.GodotFileIo(MegaCrit.Sts2.Core.Saves.UserDataPathProvider.GetAccountScopedBasePath(null));
            var manager = new MegaCrit.Sts2.Core.Saves.SaveManager(localStore);
            MegaCrit.Sts2.Core.Saves.SaveManager.MockInstanceForTesting(manager);
            manager.InitSettingsDataForTest();
            manager.SettingsSave.Language = "eng";
            manager.SettingsSave.Fullscreen = false;
            manager.SettingsSave.WindowSize = new(1280,800);
            manager.SettingsSave.ModSettings = existingSettings.ModSettings;
            GD.Print("Spire Legacy isolated test profile: ", OS.GetUserDataDir(), "; Steam Cloud save backend disabled.");
            if (!OS.GetUserDataDir().Contains("SpireLegacy2PortLab")) throw new InvalidOperationException("Refusing test launch without the isolated Godot user directory.");
        }
        Profile = FamilyProfile.Load(FamilyPath);
        Inheritance.CurrentHeir = () => Heir;
        if (Profile.offers.Count == 0) { Profile.GenerateOffers(System.Environment.TickCount); Profile.Save(FamilyPath); }
        ContentBootstrap.Prepare();
        new Harmony("CapeKid.SpireLegacy2").PatchAll(Assembly.GetExecutingAssembly());
        NativeTestBridge.Initialize();
        HeirVisuals.Initialize();
        ((SceneTree)Engine.GetMainLoop()).ProcessFrame += ManorUi.PollInput;
        GD.Print("Spire Legacy StS2 port initialized: ", Design.Rows("cards").Count, " card definitions.");
    }
    public static int Bonus(string effect) => Profile.active?.bonuses.GetValueOrDefault(effect) ?? Profile.Bonus(effect);
}
