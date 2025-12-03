using UnityEngine;

namespace HisTools.Utils;

public static class Anchor
{
    public static void SetAnchor(RectTransform rt, int pos)
    {
        Vector2 pivot;

        var anchor = pos switch
        {
            1 => // Top-Left
                pivot = new Vector2(0, 1),
            2 => // Top-Center
                pivot = new Vector2(0.5f, 1),
            3 => // Top-Right
                pivot = new Vector2(1, 1),
            4 => // Bottom-Left
                pivot = new Vector2(0, 0),
            5 => // Bottom-Center
                pivot = new Vector2(0.5f, 0),
            6 => // Bottom-Right
                pivot = new Vector2(1, 0),
            _ => pivot = new Vector2(0.5f, 0.5f)
        };

        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
    }
}
