namespace HisTools.Utils;

public static class Text
{
    public static string CompactLevelName(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return levelName;

        var pos = levelName.IndexOf('_');
        if (pos < 0)
            return levelName;

        var trimmed = levelName[(pos + 1)..];

        return trimmed.Replace("_", "");
    }
}