namespace Utils;

public class Text
{
    public static string CompactLevelName(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return levelName;

        int pos = levelName.IndexOf('_');
        if (pos < 0)
            return levelName;

        string trimmed = levelName[(pos + 1)..];

        return trimmed.Replace("_", "");
    }

    public static string ColoredText(string text, string color)
    {
        return $"<color={color}>{text}</color>";
    }

    public static string MarkedText(string text, string color)
    {
        return $"<mark={color}>{text}</mark>";
    }
}