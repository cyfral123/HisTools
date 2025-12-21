using UnityEngine;
using DG.Tweening;

namespace HisTools.Utils.RouteFeature;

public class PointAppear : MonoBehaviour
{
    public float duration = 0.35f;
    public Ease ease = Ease.OutCubic;

    private void Awake()
    {
        transform.localScale = Vector3.zero;

        var cg = GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        transform.DOScale(Vector3.one, duration).SetEase(ease).SetUpdate(true);

        cg.DOFade(1f, duration).SetEase(ease).SetUpdate(true);
    }
}