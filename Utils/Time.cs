using System;

namespace HisTools.Utils;

public static class Time
{
    public static bool AlmostEqual(TimeSpan a, TimeSpan b, TimeSpan tolerance)
    {
        return (a - b).Duration() <= tolerance;
    }

}