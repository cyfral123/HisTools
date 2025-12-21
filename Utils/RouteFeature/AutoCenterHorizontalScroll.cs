using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace HisTools.Utils.RouteFeature;

public class AutoCenterHorizontalScroll : MonoBehaviour
{
    private ScrollRect _scroll;
    private RectTransform _viewport;
    private RectTransform _content;

    private void Awake()
    {
        _scroll = GetComponentInParent<ScrollRect>();
        _viewport = _scroll.viewport;
        _content = _scroll.content;
    }

    private void OnTransformChildrenChanged()
    {
        StartCoroutine(CenterCoroutine());
    }


    private IEnumerator CenterCoroutine()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (_content.childCount == 0)
            yield break;

        var last = _content.GetChild(_content.childCount - 1) as RectTransform;

        var contentWidth = _content.rect.width;
        var viewportWidth = _viewport.rect.width;

        if (contentWidth <= viewportWidth || !last)
            yield break;

        var elementCenter =
            last.anchoredPosition.x +
            last.rect.width * (1f - last.pivot.x);

        var target =
            (elementCenter - viewportWidth * 0.5f) /
            (contentWidth - viewportWidth);

        _scroll.horizontalNormalizedPosition = Mathf.Clamp01(target);
    }
}