using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace HisTools.Utils.RouteFeature;

public static class RouteLoader
{
    public static RouteSet LoadRoutes(string filePath)
    {
        var routeSet = new RouteSet();

        if (!File.Exists(filePath))
        {
            Logger.Warn($"Route file not found: {filePath}");
            return routeSet;
        }

        var jsonText = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            Logger.Warn($"JSON-file is empty: {filePath}");
            return routeSet;
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
            return routeSet;
        }


        if (routeDataList == null || routeDataList.Count == 0)
        {
            Logger.Debug($"No routes in file: {filePath}");
            return routeSet;
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
        routeSet.info = first.info;

        foreach (var rd in routeDataList.Where(rd => rd.points != null))
        {
            foreach (var p in rd.points)
            {
                routeSet.points.Add(p.ToVector3());
                if (p.jump)
                    routeSet.jumpIndices.Add(routeSet.points.Count - 1);
            }

            if (rd.notes != null)
                routeSet.notes.AddRange(rd.notes);
        }

        return routeSet;
    }
}