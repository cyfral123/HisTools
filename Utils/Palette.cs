using UnityEngine;

namespace HisTools.Utils
{
    public static class Palette
    {
        public static string RGBAToHex(Color color)
        {
            int ri = Mathf.RoundToInt(color.r * 255f);
            int gi = Mathf.RoundToInt(color.g * 255f);
            int bi = Mathf.RoundToInt(color.b * 255f);
            int ai = Mathf.RoundToInt(color.a * 255f);

            return $"#{ri:X2}{gi:X2}{bi:X2}{ai:X2}";
        }

        public static Color FromHtml(string htmlColor)
        {
            if (!ColorUtility.TryParseHtmlString(htmlColor, out Color color)) color = Color.white;
            return new Color(color.r, color.g, color.b, color.a);
        }

        public static Color HtmlWithForceAlpha(string htmlColor, float alpha)
        {
            if (!ColorUtility.TryParseHtmlString(htmlColor, out Color color)) color = Color.white;
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static Color HtmlColorDark(string htmlColor, float factor = 0.5f, float alpha = 1f)
        {
            if (!ColorUtility.TryParseHtmlString(htmlColor, out Color color)) color = Color.white;
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a * alpha);
        }

        public static string HtmlTransparent(string html, float opacity)
        {
            int a = Mathf.Clamp(Mathf.RoundToInt(opacity * 255f), 0, 255);

            if (html.Length == 7)
                return html + a.ToString("X2");

            if (html.Length == 9)
                return html.Substring(0, 7) + a.ToString("X2");

            return html;
        }

        public static Color HtmlColorLight(string htmlColor, float factor = 1.4f)
        {
            if (!ColorUtility.TryParseHtmlString(htmlColor, out Color color)) color = Color.white;
            return new Color(
                Mathf.Clamp01(color.r * factor),
                Mathf.Clamp01(color.g * factor),
                Mathf.Clamp01(color.b * factor),
                color.a
            );
        }
    }
}
