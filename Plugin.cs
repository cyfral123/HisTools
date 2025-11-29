using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UI;
using UnityEngine;

[BepInPlugin("com.cyfral.HisTools", "HisTools", "0.1.0")]
public class Plugin : BaseUnityPlugin
{
    public static string ConfigDir { get; private set; }
    public static string Name { get; private set; }
    public static string RoutesConfigPath => Path.Combine(ConfigDir, "Routes");
    public static string RoutesStateConfigFilePath => Path.Combine(ConfigDir, "routes_state.json");
    public static string SettingsConfigPath => Path.Combine(ConfigDir, "Settings");
    public static string FeaturesStateConfigFilePath => Path.Combine(ConfigDir, "features_state.json");
    public static string SpeedrunStatsDir => Path.Combine(ConfigDir, "SpeedrunStats");

    private static readonly FeatureFactory s_featureFactory = new();

    public static float AnimationDuration = 0.15f;
    public static float MaxBackgroundAlpha = 0.90f;

    public static ConfigEntry<string> BackgroundHtml;
    public static ConfigEntry<string> AccentHtml;
    public static ConfigEntry<string> EnabledHtml;
    public static ConfigEntry<string> RouteLabelDisabledColorHtml;
    public static ConfigEntry<int> RouteLabelDisabledOpacityHtml;
    public static ConfigEntry<string> RouteLabelEnabledColorHtml;
    public static ConfigEntry<int> RouteLabelEnabledOpacityHtml;
    public static ConfigEntry<KeyCode> FeaturesMenuToggleKey;

    private void Awake()
    {
        Name = Info.Metadata.Name;
        ConfigDir = Path.Combine(Paths.BepInExRootPath, Name);

        BackgroundHtml = Config.Bind("Palette", "Background", "#282828", "Background color");
        AccentHtml = Config.Bind("Palette", "Accent", "#6869c3", "Main color");
        EnabledHtml = Config.Bind("Palette", "Enabled", "#9e97d3", "Color for activated elements");

        RouteLabelDisabledColorHtml = Config.Bind("Palette", "RouteLabelDisabledColor", "#320e0e", "Color for enabled route label text");
        RouteLabelDisabledOpacityHtml = Config.Bind("Palette", "RouteLabelDisabledOpacity", 50,
            new ConfigDescription("Color for Enabled route label text",
            new AcceptableValueRange<int>(0, 100)
        ));

        RouteLabelEnabledColorHtml = Config.Bind("Palette", "RouteLabelEnabledColor", "#bbff28", "Color for disabled route label text");
        RouteLabelEnabledOpacityHtml = Config.Bind("Palette", "RouteLabelEnabledOpacity", 100,
            new ConfigDescription("Color for Disabled route label text",
            new AcceptableValueRange<int>(0, 100)
        ));

        FeaturesMenuToggleKey = Config.Bind("General", "FeaturesMenuToggleKey", KeyCode.RightShift, "Toggle features menu");

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll();

        var pluginDir = Directory.CreateDirectory(ConfigDir);
        Directory.CreateDirectory(RoutesConfigPath);
        Directory.CreateDirectory(SettingsConfigPath);
        Directory.CreateDirectory(SpeedrunStatsDir);
        ExtractBuiltinStuff();

        var HisToolsMenu = new GameObject("HisTools_FeaturesMenu");
        DontDestroyOnLoad(HisToolsMenu);
        HisToolsMenu.AddComponent<FeaturesMenu>();

        var miscCPos = new Vector2(-250, 0);
        var visualCPos = new Vector2(0, 0);
        var pathCPos = new Vector2(250, 0);

        InitFeature(visualCPos, "Visual", new DebugInfo());
        InitFeature(visualCPos, "Visual", new CustomFog());
        InitFeature(visualCPos, "Visual", new CustomHandhold());
        InitFeature(visualCPos, "Visual", new ShowItemInfo());
        InitFeature(pathCPos, "Path", new RoutePlayer());
        InitFeature(pathCPos, "Path", new RouteRecorder());
        InitFeature(miscCPos, "Misc", new FreeBuying());
        InitFeature(miscCPos, "Misc", new SpeedrunStats());

        var buttonFactory = new UIButtonFactory();
        buttonFactory.CreateAllButtons(FeatureRegistry.GetAll());

        var featureSettingsConfig = new Config.Settings();
        RecoverState.FeaturesState(FeaturesStateConfigFilePath);

        EventBus.Subscribe<GameStartEvent>(_ => FeaturesMenu.EnsureHisToolsMenuInitialized());
        EventBus.Subscribe<FeatureSettingsMenuToggleEvent>(e => SettingsPanelController.Instance.HandleSettingsToggle(e.Feature));
    }

    private void InitFeature(Vector2 categoryPosition, string categoryName, IFeature feature)
    {
        s_featureFactory.Register(feature.Name, () => feature);
        s_featureFactory.Create(feature.Name);
        FeaturesMenu.AssignFeatureToCategory(feature, categoryName, categoryPosition);
    }

    private void ExtractBuiltinStuff()
    {
        var builtinsDir = Path.GetDirectoryName(Info.Location);
        ExtractBuiltinZips(builtinsDir, ConfigDir);
    }

    public static void ExtractBuiltinZips(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            Utils.Logger.Debug($"ExtractBuiltinZips: Source folder does not exist: {sourceDir}");
            return;
        }

        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        var zipFiles = Directory.GetFiles(sourceDir, "*.zip");

        if (zipFiles.Length > 0)
        {
            Utils.Logger.Info($"Extracting builtin zips from '{sourceDir}' to '{targetDir}'...");
        }

        foreach (var zip in zipFiles)
        {
            Utils.Logger.Info($"Extracting '{zip}'...");
            var folderName = Path.GetFileNameWithoutExtension(zip);
            var destPath = Path.Combine(targetDir, folderName);


            try
            {
                ZipFile.ExtractToDirectory(zip, destPath);
            }
            catch (Exception ex)
            {
                Utils.Logger.Error($"Failed to extract '{zip}': {ex.Message}");
                continue;
            }

            if (File.Exists(zip))
                File.Delete(zip);

            Utils.Logger.Info($"Extracted to '{destPath}'");
        }
    }
}