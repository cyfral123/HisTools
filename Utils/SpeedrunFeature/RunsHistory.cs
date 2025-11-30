using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace HisTools.Utils.SpeedrunFeature;

public class RunsHistory
{
    private class RunSegment(string from, string to, TimeSpan elapsed)
    {
        public TimeSpan Elapsed { get; } = elapsed;
    }

    public static IEnumerator LoadSegmentsAndCompute(string folderPath, string targetLevel,
        Action<TimeSpan, TimeSpan> onFinished)
    {
        var levelSegments = new List<RunSegment>();

        var files = Directory.GetFiles(folderPath, "*.json");
        foreach (var file in files)
        {
            try
            {
                var text = File.ReadAllText(file);
                var root = JArray.Parse(text);

                foreach (var obj in root)
                {
                    var from = (string)obj["from"];
                    var to = (string)obj["to"];
                    var elapsedStr = (string)obj["elapsed"];

                    if (TimeSpan.TryParseExact(elapsedStr, @"hh\:mm\:ss\:ff", null, out var elapsed))
                    {
                        if (from == targetLevel)
                            levelSegments.Add(new RunSegment(from, to, elapsed));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to read JSON {file}: {ex}");
            }

            yield return null;
        }

        if (levelSegments.Count > 0)
        {
            var best = levelSegments.Min(s => s.Elapsed);
            var avg = TimeSpan.FromMilliseconds(levelSegments.Average(s => s.Elapsed.TotalMilliseconds));
            onFinished?.Invoke(best, avg);
        }
        else
        {
            onFinished?.Invoke(TimeSpan.Zero, TimeSpan.Zero);
        }
    }
}