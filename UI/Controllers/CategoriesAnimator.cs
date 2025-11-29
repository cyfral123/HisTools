using System;
using DG.Tweening;
using UnityEngine;

public class CategoriesAnimator : MonoBehaviour
{
    private CanvasGroup[] _groups;
    private RectTransform[] _rects;
    private readonly float _duration = 0.25f;
    private readonly float _delay = 0.02f;
    private Vector2[] _originalPositions;

    public void Refresh()
    {
        _groups = GetComponentsInChildren<CanvasGroup>(true);
        _rects = new RectTransform[_groups.Length];
        _originalPositions = new Vector2[_groups.Length];

        for (int i = 0; i < _groups.Length; i++)
        {
            var g = _groups[i];
            var rect = g.transform as RectTransform;
            _rects[i] = rect;

            _originalPositions[i] = rect.anchoredPosition;

            g.alpha = 0f;

            rect.anchoredPosition += new Vector2(0, 20f);
        }
    }


    public void PlayShowAnimation()
    {
        if (_groups == null || _groups.Length == 0)
            Refresh();

        float d = 0f;

        for (int i = 0; i < _groups.Length; i++)
        {
            var g = _groups[i];
            var rect = _rects[i];

            g.DOFade(1f, _duration).SetDelay(d);

            rect.DOAnchorPos(_originalPositions[i], _duration)
                .SetDelay(d)
                .SetEase(Ease.OutCubic);

            d += _delay;
        }
    }
}
