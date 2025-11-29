using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SmoothUtil
{
    // generates smooth curves between points using spline interpolation
    public static List<Vector3> Path(List<Vector3> points, float smoothResolution)
    {
        List<Vector3> smoothed = [];

        if (points.Count < 2)
            return points;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = i == 0 ? points[i] : points[i - 1];
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            Vector3 p3 = i + 2 < points.Count ? points[i + 2] : p2;

            for (int j = 0; j < smoothResolution; j++)
            {
                float t = j / smoothResolution;
                float t2 = t * t;
                float t3 = t2 * t;

                Vector3 position =
                    0.5f * ((2f * p1) +
                    (-p0 + p2) * t +
                    (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                    (-p0 + 3f * p1 - 3f * p2 + p3) * t3);

                smoothed.Add(position);
            }
        }

        smoothed.Add(points.Last());
        return smoothed;
    }

    // smooths positions by averaging each point with its neighbors in a sliding window
    public static List<Vector3> Points(List<Vector3> points, int windowSize)
    {
        List<Vector3> smoothed = [];

        if (points.Count < windowSize)
            return points;

        for (int i = 0; i < points.Count; i++)
        {
            var sum = Vector3.zero;
            int count = 0;
            for (int j = i - windowSize + 1; j <= i; j++)
            {
                if (j >= 0)
                {
                    sum += points[j];
                    count++;
                }
            }
            smoothed.Add(sum / count);
        }
        return smoothed;
    }
}