using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace HisTools.Utils.SpeedrunFeature;

public static class RunsHistory
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

                    if (TimeSpan.TryParseExact(elapsedStr, @"mm\:ss\:ff", null, out var elapsed))
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
            var ms = levelSegments
                .Select(s => s.Elapsed.TotalMilliseconds)
                .OrderBy(x => x)
                .ToList();

            double median;

            var count = ms.Count;
            if (count == 0)
            {
                median = 0;
            }
            else if (count % 2 == 1)
            {
                median = ms[count / 2];
            }
            else
            {
                median = (ms[count / 2 - 1] + ms[count / 2]) / 2.0;
            }

            var medianTime = TimeSpan.FromMilliseconds(median);

            onFinished?.Invoke(best, medianTime);
        }
        else
        {
            onFinished?.Invoke(TimeSpan.Zero, TimeSpan.Zero);
        }
    }


    public static double Median(List<double> xs)
    {
        if (xs == null || xs.Count == 0) throw new ArgumentException();
        xs.Sort();
        var n = xs.Count;
        if (n % 2 == 1) return xs[n / 2];

        return (xs[n / 2 - 1] + xs[n / 2]) / 2.0;
    }
}