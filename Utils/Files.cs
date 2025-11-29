using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Utils;

public class Files
{
    public static string GenerateUid() => Guid.NewGuid().ToString();

    public static void EnsureAllRoutesHaveUID()
    {
        string routesDir = Path.Combine(Plugin.RoutesConfigPath);

        if (!Directory.Exists(routesDir))
        {
            Logger.Warn($"Routes directory not found: {routesDir}");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(routesDir, "*.json", SearchOption.AllDirectories);

        Logger.Debug($"Checking '{jsonFiles.Length}' route file(s) for missing UID...");

        foreach (string file in jsonFiles)
        {
            try
            {
                string text = File.ReadAllText(file);
                JArray arr = JArray.Parse(text);

                bool changed = false;

                foreach (var obj in arr)
                {
                    if (obj["uid"] == null)
                    {
                        string newUid = GenerateUid();
                        obj["uid"] = newUid;
                        changed = true;
                    }
                }

                if (changed)
                {
                    string newJson = JsonConvert.SerializeObject(arr, Formatting.Indented);
                    File.WriteAllText(file, newJson);
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Failed to process JSON '{file}': {ex.Message}");
            }
        }
    }

    public static IEnumerator GetRouteFilesByTargetLevel(string targetLevel, Action<List<string>> callback)
    {
        var result = new List<string>();

        if (string.IsNullOrEmpty(targetLevel))
        {
            Logger.Debug("Target level is null or empty");
            yield break;
        }

        string routesDir = Path.Combine(Plugin.RoutesConfigPath);
        if (!Directory.Exists(routesDir))
        {
            Logger.Debug($"Routes directory not found: {routesDir}");
            yield break;
        }

        var jsonFiles = Directory.GetFiles(routesDir, "*.json", SearchOption.AllDirectories);

        foreach (var file in jsonFiles)
        {
            string json = File.ReadAllText(file);
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
            string filePath = Plugin.RoutesStateConfigFilePath;

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
            string filePath = Plugin.RoutesStateConfigFilePath;

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
            string filePath = Path.Combine(Plugin.SettingsConfigPath, $"{featureName}.json");

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
            string filePath = Path.Combine(Plugin.SettingsConfigPath, $"{featureName}.json");

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
            string filePath = Plugin.FeaturesStateConfigFilePath;

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

        string text = File.ReadAllText(path);

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

            JObject repaired = TryRepairJson(text);
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
            string fixedJson = broken
                .Trim()
                .TrimEnd(',', ' ', '\n', '\r', '\t');

            int open = fixedJson.Count(c => c == '{');
            int close = fixedJson.Count(c => c == '}');

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
            string backup = path + ".corrupt_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(path, backup, overwrite: true);
            Logger.Warn($"Backup saved: {backup}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save corrupt backup: {ex.Message}");
        }
    }

    public static void SaveJsonToFile(string path, JObject json)
    {
        SaveToFile(path, json.ToString(Formatting.Indented));
    }

    public static void SaveJsonToFile(string path, JArray json)
    {
        SaveToFile(path, json.ToString(Formatting.Indented));
    }

    public static void SaveToFile(string path, string content)
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

}