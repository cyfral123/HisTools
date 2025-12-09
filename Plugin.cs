using System;
using System.IO;
using System.IO.Compression;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HisTools.Features;
using HisTools.Features.Controllers;
using HisTools.UI;
using HisTools.UI.Controllers;
using HisTools.Utils;
using UnityEngine;

namespace HisTools;


[BepInPlugin(Constants.PluginGuid, Constants.PluginName, Constants.PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;
    private static readonly FeatureFactory FeatureFactory = new();
    private Harmony _harmony;

    // Configuration entries
    public static ConfigEntry<string> BackgroundHtml { get; private set; }
    public static ConfigEntry<string> AccentHtml { get; private set; }
    public static ConfigEntry<string> EnabledHtml { get; private set; }
    public static ConfigEntry<string> RouteLabelDisabledColorHtml { get; private set; }
    public static ConfigEntry<int> RouteLabelDisabledOpacityHtml { get; private set; }
    public static ConfigEntry<string> RouteLabelEnabledColorHtml { get; private set; }
    public static ConfigEntry<int> RouteLabelEnabledOpacityHtml { get; private set; }
    public static ConfigEntry<KeyCode> FeaturesMenuToggleKey { get; private set; }

    private void Awake()
    {
        Instance = this;
        try
        {
            InitializeConfiguration();
            InitializeHarmony();
            CreateRequiredDirectories();
            ExtractBuiltinStuff();
            InitializeUI();
            InitializeFeatures();
            SubscribeToEvents();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize {Constants.PluginName}: {ex}");
            throw;
        }
    }

    private void InitializeConfiguration()
    {
        // Palette settings
        BackgroundHtml = Config.Bind("Palette", "Background", "#282828", "Background color");
        AccentHtml = Config.Bind("Palette", "Accent", "#6869c3", "Main color");
        EnabledHtml = Config.Bind("Palette", "Enabled", "#9e97d3", "Color for activated elements");

        // Route label settings
        RouteLabelDisabledColorHtml = Config.Bind("Palette", "RouteLabelDisabledColor", "#320e0e",
            "Color for disabled route label text");

        RouteLabelDisabledOpacityHtml = Config.Bind("Palette", "RouteLabelDisabledOpacity", 50,
            new ConfigDescription("Opacity for disabled route label text",
                new AcceptableValueRange<int>(0, 100)));

        RouteLabelEnabledColorHtml = Config.Bind("Palette", "RouteLabelEnabledColor", "#bbff28",
            "Color for enabled route label text");

        RouteLabelEnabledOpacityHtml = Config.Bind("Palette", "RouteLabelEnabledOpacity", 100,
            new ConfigDescription("Opacity for enabled route label text",
                new AcceptableValueRange<int>(0, 100)));

        // General settings
        FeaturesMenuToggleKey = Config.Bind("General", "FeaturesMenuToggleKey",
            KeyCode.RightShift, "Key to toggle the features menu");
    }

    private void InitializeHarmony()
    {
        _harmony = new Harmony(Info.Metadata.GUID);
        _harmony.PatchAll();
    }

    private void CreateRequiredDirectories()
    {
        Directory.CreateDirectory(Constants.Paths.ConfigDir);
        Directory.CreateDirectory(Constants.Paths.RoutesConfigPath);
        Directory.CreateDirectory(Constants.Paths.SettingsConfigPath);
        Directory.CreateDirectory(Constants.Paths.SpeedrunStatsDir);
    }

    private void InitializeUI()
    {
        var featuresMenuObject = new GameObject(Constants.UI.MenuObjectName);
        DontDestroyOnLoad(featuresMenuObject);
        featuresMenuObject.AddComponent<FeaturesMenu>();
    }

    private void InitializeFeatures()
    {
        var miscCategoryPos = new Vector2(-250, 0);
        var visualCategoryPos = new Vector2(0, 0);
        var pathCategoryPos = new Vector2(250, 0);

        // Visual features
        RegisterFeature(visualCategoryPos, "Visual", new DebugInfo());
        RegisterFeature(visualCategoryPos, "Visual", new CustomFog());
        RegisterFeature(visualCategoryPos, "Visual", new CustomHandhold());
        RegisterFeature(visualCategoryPos, "Visual", new ShowItemInfo());
        RegisterFeature(visualCategoryPos, "Visual", new BuffsDisplay());

        // Path features
        RegisterFeature(pathCategoryPos, "Path", new RoutePlayer());
        RegisterFeature(pathCategoryPos, "Path", new RouteRecorder());

        // Misc features
        RegisterFeature(miscCategoryPos, "Misc", new FreeBuying());
        RegisterFeature(miscCategoryPos, "Misc", new SpeedrunStats());
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<FeatureSettingChangedEvent>(e =>
        {
            Debounce.Run(
                CoroutineRunner.Instance,
                e.Setting.Name,
                2.5f,
                () => Files.SaveSettingToConfig(e.Feature.Name, e.Setting.Name, e.Setting.GetValue())
            );
        });

        EventBus.Subscribe<GameStartEvent>(_ => FeaturesMenu.EnsureHisToolsMenuInitialized());

        EventBus.Subscribe<FeatureSettingsMenuToggleEvent>(e =>
        {
            if (SettingsPanelController.Instance.TryGet(out var value))
                value.HandleSettingsToggle(e.Feature);
        });
    }

    private void RegisterFeature(Vector2 categoryPosition, string categoryName, IFeature feature)
    {
        if (feature == null)
        {
            Logger.LogWarning("Attempted to register a null feature");
            return;
        }

        try
        {
            FeatureFactory.Register(feature.Name, () => feature);
            FeatureFactory.Create(feature.Name);
            FeaturesMenu.AssignFeatureToCategory(feature, categoryName, categoryPosition);
            Utils.Logger.Debug($"Registered feature: {feature.Name} in category: {categoryName}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to register feature {feature.Name}: {ex}");
        }
    }

    private void ExtractBuiltinStuff()
    {
        try
        {
            var builtinsDir = Path.GetDirectoryName(Info.Location);
            if (string.IsNullOrEmpty(builtinsDir))
            {
                Utils.Logger.Warn("Could not determine plugin directory for builtin assets");
                return;
            }

            ExtractBuiltinZips(builtinsDir, Constants.Paths.ConfigDir);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during builtin assets extraction: {ex}");
        }
    }

    private static void ExtractBuiltinZips(string sourceDir, string targetDir)
    {
        if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
        {
            Utils.Logger.Warn($"Source directory does not exist or is inaccessible: {sourceDir}");
            return;
        }

        if (Files.EnsureDirectory(targetDir).IsNone)
        {
            Utils.Logger.Error($"targetDir '{targetDir}' is not a directory path");
            return;
        }

        if (Files.GetFiles(sourceDir, "*.zip").TryGet(out var zipFiles))
        {
            if (zipFiles.Length == 0)
            {
                Utils.Logger.Debug("No builtin zip files found to extract");
                return;
            }

            Utils.Logger.Info($"Found {zipFiles.Length} builtin zip files to extract");

            foreach (var zip in zipFiles)
            {
                ExtractZipFile(zip, targetDir);
            }
        }
        else
        {
            Utils.Logger.Error($"No builtin zip files found in '{sourceDir}'");
        }
    }

    private static void ExtractZipFile(string zipPath, string targetDir)
    {
        var folderName = Path.GetFileNameWithoutExtension(zipPath);
        var destPath = Path.Combine(targetDir, folderName);

        try
        {
            Utils.Logger.Info($"Extracting '{Path.GetFileName(zipPath)}' to '{destPath}'...");

            Directory.CreateDirectory(destPath);

            ZipFile.ExtractToDirectory(zipPath, destPath, true);

            File.Delete(zipPath);

            Utils.Logger.Info($"Successfully extracted to '{destPath}'");
        }
        catch (UnauthorizedAccessException ex)
        {
            Utils.Logger.Error($"Access denied when processing '{zipPath}': {ex.Message}");
        }
        catch (IOException ex)
        {
            Utils.Logger.Error($"I/O error processing '{zipPath}': {ex.Message}");
        }
        catch (Exception ex)
        {
            Utils.Logger.Error($"Failed to extract '{zipPath}': {ex}");
        }
    }
}