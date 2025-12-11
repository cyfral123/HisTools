using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HisTools.Utils;

public static class Files
{
    public static string GenerateUid() => Guid.NewGuid().ToString();

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

        var jsonFiles = Directory.GetFiles(routesDir, "*.json", SearchOption.AllDirectories);

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

    public static bool? GetRouteStateFromConfig(string routeUid)
    {
        try
        {
            var filePath = Constants.Paths.RoutesStateConfigFilePath;

            var json = LoadOrRepairJson(filePath);

            if (json[routeUid] != null)
            {
                return json[routeUid].ToObject<bool>();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load route state '{routeUid}': {ex.Message}");
        }

        return null;
    }

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
            BackupCorrupt(path);
            return new JObject();
        }

        try
        {
            return JObject.Parse(text);
        }
        catch (JsonReaderException ex)
        {
            Logger.Error($"JSON corrupted '{path}' Repair attempt error: {ex.Message}");
            BackupCorrupt(path);

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
            BackupCorrupt(path);
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

    private static void BackupCorrupt(string path)
    {
        try
        {
            var backup = path + ".corrupt_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(path, backup, overwrite: true);
            Logger.Warn($"Backup saved: {backup}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save corrupt backup: {ex.Message}");
        }
    }

    private static void SaveJsonToFile(string path, JObject json)
    {
        SaveToFile(path, json.ToString(Formatting.Indented));
    }

    public static void SaveJsonToFile(string path, JArray json)
    {
        SaveToFile(path, json.ToString(Formatting.Indented));
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

    public static Option<DirectoryInfo> EnsureDirectory(string path)
    {
        if (Directory.Exists(path))
            return Option<DirectoryInfo>.Some(new DirectoryInfo(path));

        try
        {
            var dirInfo = Directory.CreateDirectory(path);
            return Option<DirectoryInfo>.Some(dirInfo);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create directory '{path}': {ex.Message}");
            return Option<DirectoryInfo>.None();
        }
    }

    public static Option<string[]> GetFiles(string path, string searchPattern = null)
    {
        try
        {
            var files = searchPattern == null ? Directory.GetFiles(path) : Directory.GetFiles(path, searchPattern);

            return Option<string[]>.Some(files);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to get files from path '{path}': {ex.Message}");
            return Option<string[]>.None();
        }
    }
}