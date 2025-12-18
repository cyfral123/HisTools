using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HisTools.Utils.RouteFeature.BackwardCompatibility;

public enum RouteJsonVersion
{
    Unknown,
    V1,
    V2
}

public static class RouteOldSupport
{
    public static RouteJsonVersion DetectVersion(string json)
    {
        var token = JToken.Parse(json);

        if (token is JArray arr && arr.Count > 0)
            token = arr[0];

        if (token is not JObject obj)
            return RouteJsonVersion.Unknown;

        if (obj["jumpIndices"] != null)
            return RouteJsonVersion.V2;

        if (obj["points"] is JArray points &&
            points.Any(p => p["jump"] != null))
            return RouteJsonVersion.V1;

        return RouteJsonVersion.Unknown;
    }

    public static List<RouteData> ConvertV1ToV2(string json)
    {
        var token = JToken.Parse(json);

        var arr = token as JArray ?? new JArray(token);

        var result = new List<RouteData>();

        foreach (var t in arr)
        {
            var old = t.ToObject<V1Route>();

            var points = new List<Vec3Dto>();
            var jumpIndices = new List<int>();

            for (int i = 0; i < old.points.Count; i++)
            {
                var p = old.points[i];

                points.Add(new Vec3Dto { x = p.x, y = p.y, z = p.z });

                if (p.jump)
                    jumpIndices.Add(i);
            }

            var notes = old.notes?
                .Select(n => new RouteNoteDto
                {
                    position = new Vec3Dto { x = n.x, y = n.y, z = n.z },
                    text = n.note
                })
                .ToList() ?? [];

            result.Add(new RouteData
            {
                info = new RouteInfo
                {
                    uid = string.IsNullOrWhiteSpace(old.info.uid)
                        ? Files.GenerateUid()
                        : old.info.uid,

                    name = old.info.name,
                    author = old.info.author,
                    description = old.info.description,
                    targetLevel = old.info.targetLevel
                },
                points = points,
                jumpIndices = jumpIndices,
                notes = notes
            });
        }

        return result;
    }
}