using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HisTools.Utils;

/// <summary>
/// Provides utility methods for file operations, JSON handling, and configuration management.
/// </summary>
public static class Files
{
    /// <summary>
    /// Generates a new unique identifier as a string.
    /// </summary>
    /// <returns>A new unique identifier string.</returns>
    public static string GenerateUid() => Guid.NewGuid().ToString();

    /// <summary>
    /// Asynchronously retrieves all route files that match the specified target level.
    /// </summary>
    /// <param name="targetLevel">The target level to search for in route files.</param>
    /// <param name="callback">Action that will be called with the list of matching file paths.</param>
    /// <returns>An IEnumerator for coroutine support.</returns>
    public static IEnumerator GetRouteFilesByTargetLevel(string targetLevel, Action<List<string>> callback)
    {
        var result = new List<string>();

        if (string.IsNullOrEmpty(targetLevel))
        {
            Logger.Debug("Target level is null or empty");
            yield break;
        }

        var routesDir = Path.Combine(Constants.Paths.RoutesPathDir);
        if (!Directory.Exists(routesDir))
        {
            Logger.Debug($"Routes directory not found: {routesDir}");
            yield break;
        }

        var jsonFiles = Directory.GetFiles(routesDir, "*", SearchOption.AllDirectories);

        foreach (var file in jsonFiles)
        {
            var json = File.ReadAllText(file);
            var arr = JArray.Parse(json);

            foreach (var obj in arr)
            {
                try
                {
                    var info = obj["info"];
                    if (info == null) continue;

                    var level = info["targetLevel"]?.ToString();
                    if (level == targetLevel)
                    {
                        result.Add(file);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to process JSON '{file}': {ex.Message}");
                }

                yield return new WaitForEndOfFrame();
            }
        }

        Logger.Debug($"Found '{result.Count}' file(s) for targetLevel: {targetLevel}");
        callback(result);
    }

    /// <summary>
    /// Ensures that built-in routes are properly extracted and available in the route's directory.
    /// Creates the built-in routes directory if it doesn't exist and extracts embedded resources.
    /// </summary>
    public static void EnsureBuiltinRoutes()
    {
        var builtinRoutesDir = Path.Combine(Constants.Paths.RoutesPathDir, "Builtin_histools_routes");
        if (Directory.Exists(builtinRoutesDir))
        {
            Logger.Debug("Builtin routes directory exists");
            return;
        }

        Logger.Debug("Builtin routes does not exist, creating it");

        try
        {
            var result = new Dictionary<string, List<(string FileName, string Content)>>();

            var asm = Assembly.GetExecutingAssembly();
            var asmName = asm.GetName().Name;

            foreach (var res in asm.GetManifestResourceNames().Where(r => r.EndsWith(".json")))
            {
                var firstDot = res.IndexOf('.', asmName.Length + 1);
                var relative = res[(firstDot + 1)..];

                var lastDot = relative.LastIndexOf('.');
                var beforeExt = relative[..lastDot];
                var extension = relative[lastDot..];

                var pathParts = beforeExt.Split('.');
                var folder = Path.Combine(pathParts[..^1]);
                var fileName = pathParts[^1] + extension;

                using var stream = asm.GetManifestResourceStream(res)!;
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                if (!result.TryGetValue(folder, out var list))
                {
                    list = [];
                    result[folder] = list;
                }

                list.Add((fileName, json));
            }

            foreach (var (folder, files) in result)
            {
                var dir = Path.Combine(builtinRoutesDir, folder);
                Directory.CreateDirectory(dir);

                foreach (var (fileName, content) in files)
                {
                    var filePath = Path.Combine(dir, fileName);
                    SaveJsonToFile(filePath, content, true);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create builtin routes: {ex.Message}");
            return;
        }

        Logger.Debug("Builtin routes created");
    }

    /// <summary>
    /// Saves the active state of a route to the configuration.
    /// </summary>
    /// <param name="routeUid">The unique identifier of the route.</param>
    /// <param name="isActive">The active state to save for the route.</param>
    public static void SaveRouteStateToConfig(string routeUid, bool isActive)
    {
        try
        {
            var filePath = Constants.Paths.RoutesStateConfigFilePath;

            var json = LoadOrRepairJson(filePath);

            json[routeUid] = isActive;

            SaveJsonToFile(filePath, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save route state '{routeUid}': {ex.Message}");
        }
    }


    /// <summary>
    /// Tries to get the active state of a route from the configuration.
    /// </summary>
    /// <param name="routeUid">The unique identifier of the route.</param>
    /// <param name="state">The active state of the route if found.</param>
    /// <returns>True if the state was successfully retrieved, false otherwise.</returns>
    public static bool TryGetRouteStateFromConfig(string routeUid, out bool state)
    {
        state = false;

        try
        {
            var json = LoadOrRepairJson(Constants.Paths.RoutesStateConfigFilePath);
            var token = json[routeUid];

            if (token == null)
                return false;

            state = token.ToObject<bool>();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load route state '{routeUid}': {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// Saves a setting value to the configuration for a specific feature.
    /// </summary>
    /// <param name="featureName">The name of the feature.</param>
    /// <param name="settingName">The name of the setting to save.</param>
    /// <param name="value">The value to save. If the value is a Color, it will be converted to HTML string format.</param>
    public static void SaveSettingToConfig(string featureName, string settingName, object value)
    {
        try
        {
            var filePath = Path.Combine(Constants.Paths.SettingsConfigPath, $"{featureName}.json");

            var json = LoadOrRepairJson(filePath);

            if (value is Color c)
                json[settingName] = $"#{ColorUtility.ToHtmlStringRGBA(c)}";
            else
                json[settingName] = JToken.FromObject(value);

            SaveJsonToFile(filePath, json);
            Logger.Debug($"Saved to config: '{featureName}': '{settingName}' -> {value}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save setting: '{featureName}': '{settingName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves a setting value from the configuration for a specific feature.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="featureName">The name of the feature.</param>
    /// <param name="settingName">The name of the setting to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the setting is not found.</param>
    /// <returns>The setting value if found, otherwise the default value.</returns>
    public static T GetSettingFromConfig<T>(string featureName, string settingName, T defaultValue)
    {
        try
        {
            var filePath = Path.Combine(Constants.Paths.SettingsConfigPath, $"{featureName}.json");

            var json = LoadOrRepairJson(filePath);

            if (json[settingName] != null)
            {
                return json[settingName].ToObject<T>();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load setting '{settingName}': {ex.Message}");
        }

        SaveSettingToConfig(featureName, settingName, defaultValue);
        return defaultValue;
    }

    /// <summary>
    /// Saves the enabled/disabled state of a feature to the configuration.
    /// </summary>
    /// <param name="featureName">The name of the feature.</param>
    /// <param name="isEnabled">The enabled state to save for the feature.</param>
    public static void SaveFeatureStateToConfig(string featureName, bool isEnabled)
    {
        try
        {
            var filePath = Constants.Paths.FeaturesStateConfigFilePath;

            var json = LoadOrRepairJson(filePath);

            json[featureName] = isEnabled;

            SaveJsonToFile(filePath, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save feature state '{featureName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a JSON file from the specified path, attempting to repair it if corrupted.
    /// </summary>
    /// <param name="path">The path to the JSON file.</param>
    /// <returns>A JObject containing the parsed JSON, or a new JObject if the file is empty or invalid.</returns>
    public static JObject LoadOrRepairJson(string path)
    {
        if (!File.Exists(path))
        {
            Logger.Debug($"JSON not found, return new object: '{path}'");
            return new JObject();
        }

        var text = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(text))
        {
            Logger.Warn($"JSON empty, regenerating: '{path}'");
            BackupFile(path);
            return new JObject();
        }

        try
        {
            return JObject.Parse(text);
        }
        catch (JsonReaderException ex)
        {
            Logger.Error($"JSON corrupted '{path}' Repair attempt error: {ex.Message}");
            BackupFile(path);

            var repaired = TryRepairJson(text);
            if (repaired != null)
            {
                Logger.Warn("JSON was repaired successfully");
                File.WriteAllText(path, repaired.ToString(Formatting.Indented));
                return repaired;
            }

            Logger.Warn("Repair failed creating new JSON");
            return new JObject();
        }
        catch (Exception ex)
        {
            Logger.Error($"Unexpected error while reading JSON: {ex.Message}");
            BackupFile(path);
            return new JObject();
        }
    }

    private static JObject TryRepairJson(string broken)
    {
        try
        {
            var fixedJson = broken
                .Trim()
                .TrimEnd(',', ' ', '\n', '\r', '\t');

            var open = fixedJson.Count(c => c == '{');
            var close = fixedJson.Count(c => c == '}');

            while (close < open)
            {
                fixedJson += "}";
                close++;
            }

            return JObject.Parse(fixedJson);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to repair JSON: {ex.Message}");
            return null;
        }
    }


    public static void BackupFile(string path, string reason = "")
    {
        try
        {
            var backup = $"{path}.backup_{reason}_{DateTime.Now:yyyyMMdd_HHmmss}";
            File.Copy(path, backup, overwrite: true);
            Logger.Warn($"Backup saved: {backup}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save backup: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves a JToken as a JSON file with indented formatting.
    /// </summary>
    /// <param name="path">The path where to save the JSON file.</param>
    /// <param name="json">The JToken to save.</param>
    public static void SaveJsonToFile(string path, JToken json)
    {
        SaveToFile(path, json.ToString(Formatting.Indented));
    }

    /// <summary>
    /// Saves a JSON string to a file.
    /// </summary>
    /// <param name="path">The path where to save the JSON file.</param>
    /// <param name="json">The JSON string to save.</param>
    /// <param name="ensureExtension">If true, ensures the path ends with ".json".</param>
    public static void SaveJsonToFile(string path, string json, bool ensureExtension = false)
    {
        if (!path.EndsWith(".json") && ensureExtension)
            path += ".json";

        SaveToFile(path, json);
    }

    private static void SaveToFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save file '{path}': {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures that a directory exists at the specified path, creating it if necessary.
    /// </summary>
    public static bool TryEnsureDirectory(string path, out DirectoryInfo directory)
    {
        directory = null;

        try
        {
            if (Directory.Exists(path))
            {
                directory = new DirectoryInfo(path);
                return true;
            }

            directory = Directory.CreateDirectory(path);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create directory '{path}': {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// Retrieves the names of files in the specified directory.
    /// </summary>
    public static bool TryGetFiles(string path, out string[] files, string searchPattern = null)
    {
        files = null;

        try
        {
            files = searchPattern == null
                ? Directory.GetFiles(path)
                : Directory.GetFiles(path, searchPattern);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to get files from path '{path}': {ex.Message}");
            return false;
        }
    }


    public static string GetNextFilePath(string folderPath, string baseFileName, string extension)
    {
        var filePath = Path.Combine(folderPath, $"{baseFileName}.{extension}");
        var counter = 2;

        while (File.Exists(filePath))
        {
            filePath = Path.Combine(
                folderPath,
                $"{baseFileName}_{counter:D2}.{extension}");
            counter++;
        }

        return filePath;
    }
}