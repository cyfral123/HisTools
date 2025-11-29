using UnityEngine;
using TMPro;

public static class UIExtensions
{
    public static TextMeshProUGUI AddMyText(this Transform parent, string content, TextAlignmentOptions aligment, float fontsize, Color color, float leftPadding = 0f)
    {
        var textGO = new GameObject("HisTools_Text");
        textGO.transform.SetParent(parent.transform, false);

        var rect = textGO.GetComponent<RectTransform>() ?? textGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(leftPadding, 0);
        rect.offsetMax = new Vector2(0, 0);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.alignment = aligment;
        tmp.fontSize = fontsize;
        tmp.fontWeight = FontWeight.Regular;
        tmp.color = color;

        return tmp;
    }
}
