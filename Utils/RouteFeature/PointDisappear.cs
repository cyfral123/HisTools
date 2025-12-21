using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace HisTools.Utils.RouteFeature;

public class PointDisappear : MonoBehaviour
{
    public float duration = 0.2f;
    public Ease ease = Ease.InCubic;

    public void PlayAndDestroy()
    {
        var cg = GetComponent<CanvasGroup>();
        var le = GetComponent<LayoutElement>();

        var startWidth = le.preferredWidth > 0 ? le.preferredWidth : ((RectTransform)transform).rect.width;

        le.preferredWidth = startWidth;

        var seq = DOTween.Sequence();

        seq.Join(transform.DOScale(Vector3.zero, duration).SetEase(ease));

        seq.Join(cg.DOFade(0f, duration).SetEase(ease));

        seq.Join(
            DOTween.To(
                () => le.preferredWidth,
                x => le.preferredWidth = x,
                0f,
                duration
            ).SetEase(ease)
        );

        seq.OnComplete(() => Destroy(gameObject));
    }
}