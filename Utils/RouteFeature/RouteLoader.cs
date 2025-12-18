using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HisTools.Utils.RouteFeature.BackwardCompatibility;
using Newtonsoft.Json;

namespace HisTools.Utils.RouteFeature;

public static class RouteLoader
{
    public static RouteData LoadRoutes(string filePath)
    {
        var routeData = new RouteData
        {
            points = new List<Vec3Dto>(),
            jumpIndices = new List<int>(),
            notes = new List<RouteNoteDto>()
        };

        if (!File.Exists(filePath))
        {
            Logger.Error($"Route file not found: {filePath}");
            return routeData;
        }
        
        if (!filePath.EndsWith(".json"))
        {
            Logger.Error($"Route file '{filePath}' is not a JSON-file");
            return routeData;
        }
        
        var jsonText = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            Logger.Warn($"JSON-file is empty: {filePath}");
            return routeData;
        }

        var version = RouteOldSupport.DetectVersion(jsonText);
        if (version == RouteJsonVersion.V1)
        {
            Logger.Warn($"Route file '{filePath}' is in old (V1) format, converting to new (V2) format...");
            Files.BackupFile(filePath, "OLD_V1_format");
            
            var converted = RouteOldSupport.ConvertV1ToV2(jsonText);
            
            jsonText = JsonConvert.SerializeObject(
                converted,
                Formatting.Indented
            );

            Files.SaveJsonToFile(filePath, jsonText);
        }


        List<RouteData> routeDataList;
        try
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            routeDataList = JsonConvert.DeserializeObject<List<RouteData>>(jsonText, settings);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Error parsing JSON: {ex.Message}");
            return routeData;
        }


        if (routeDataList == null || routeDataList.Count == 0)
        {
            Logger.Debug($"No routes in file: {filePath}");
            return routeData;
        }

        var changed = false;

        foreach (var rd in routeDataList
                     .Where(rd => rd.info != null)
                     .Where(rd => string.IsNullOrWhiteSpace(rd.info.uid)))
        {
            rd.info.uid = Files.GenerateUid();
            changed = true;
        }

        if (changed)
        {
            var json = JsonConvert.SerializeObject(routeDataList, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        var first = routeDataList.First();
        routeData.info = first.info;

        foreach (var rd in routeDataList.Where(rd => rd.points != null))
        {
            var startIndex = routeData.points.Count;

            foreach (var p in rd.points)
            {
                routeData.points.Add(p);
            }

            if (rd.jumpIndices != null)
            {
                foreach (var jumpIndex in rd.jumpIndices)
                {
                    routeData.jumpIndices.Add(startIndex + jumpIndex);
                }
            }

            if (rd.notes != null)
            {
                foreach (var n in rd.notes)
                {
                    routeData.notes.Add(new RouteNoteDto
                    {
                        position = n.position,
                        text = n.text
                    });
                }
            }
        }


        return routeData;
    }
}