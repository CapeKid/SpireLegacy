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
        // Native preloader explicitly disposes resources between menu/run scenes.
        // A managed dictionary reference does not keep that disposed Godot object usable.
        if (Textures.TryGetValue(path, out var texture) && GodotObject.IsInstanceValid(texture)) return texture;
        var fullPath = Path.Combine(ModDirectory, path);
        var image = new Image();
        if (Path.GetExtension(path).Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            var error = image.LoadSvgFromString(File.ReadAllText(fullPath));
            if (error != Error.Ok) throw new InvalidDataException("Invalid mod icon: " + path + " (" + error + ")");
        }
        else image = Image.LoadFromFile(fullPath);
        if (image.IsEmpty()) throw new InvalidDataException("Missing mod illustration: " + path);
        return Textures[path] = ImageTexture.CreateFromImage(image);
    }
    public static Texture2D TraitIcon(string id) => Texture(Design.Get("icon_art", "trait_" + id).Text("path"));
    public static Texture2D ClassIcon(string id) => Texture(Design.Get("icon_art", "class_" + id).Text("path"));
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
            manager.SettingsSave.SeenEaDisclaimer = true;
            manager.SettingsSave.Fullscreen = false;
            manager.SettingsSave.WindowSize = new(1280,800);
            manager.SettingsSave.ModSettings = existingSettings.ModSettings;
            GD.Print("Spire Legacy isolated test profile: ", OS.GetUserDataDir(), "; Steam Cloud save backend disabled.");
            if (!OS.GetUserDataDir().Contains("SpireLegacy2PortLab")) throw new InvalidOperationException("Refusing test launch without the isolated Godot user directory.");
        }
        Profile = FamilyProfile.Load(FamilyPath);
        Inheritance.CurrentHeir = () => Heir;
        if (Profile.offers.Count == 0) { Profile.GenerateOffers(System.Environment.TickCount); Profile.Save(FamilyPath); }
        new Harmony("CapeKid.SpireLegacy2").PatchAll(Assembly.GetExecutingAssembly());
        NativeTestBridge.Initialize();
        HeirVisuals.Initialize();
        GD.Print("Spire Legacy StS2 port initialized: ", Design.Rows("cards").Count, " card definitions.");
    }
    public static int Bonus(string effect) => Profile.active?.bonuses.GetValueOrDefault(effect) ?? Profile.Bonus(effect);
}
