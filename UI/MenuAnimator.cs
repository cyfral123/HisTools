using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MenuAnimator : MonoBehaviour
{
    public float duration = 0.2f;
    public float maxAlpha = 0.95f;
    public Vector3 startScale = new(0.8f, 0.8f, 0.8f);

    public CanvasGroup canvasGroup;
    public CategoriesAnimator categoryAnim;

    private void Awake()
    {
        if (!canvasGroup)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (!canvasGroup)
        {
            Utils.Logger.Error("MenuAnimator is missing a canvasGroup ref");
            enabled = false;
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.transform.localScale = startScale;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Show()
    {
        Toggle(true);
    }

    public void Hide()
    {
        Toggle(false);
    }

    public void Toggle(bool show)
    {
        if (!this || !transform)
        {
            Utils.Logger.Error("MenuAnimator.Toggle called on a destroyed component");
            return;
        }

        if (!canvasGroup)
        {
            if (UI.FeaturesMenu.CanvasGroup)
            {
                canvasGroup = UI.FeaturesMenu.CanvasGroup;
            }

            if (!canvasGroup)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (!canvasGroup)
            {
                Utils.Logger.Error("MenuAnimator.Toggle called but canvasGroup is null");
                return;
            }
        }

        canvasGroup.DOKill(true);
        canvasGroup.transform.DOKill(true);

        if (show)
        {
            gameObject.SetActive(true);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        var shouldAnimate = show && categoryAnim && categoryAnim.enabled;

        if (shouldAnimate)
        {
            var seq = DOTween.Sequence();
            seq.Join(canvasGroup.DOFade(maxAlpha, duration));
            seq.Join(canvasGroup.transform.DOScale(Vector3.one, duration));

            categoryAnim.Refresh();
            categoryAnim.PlayShowAnimation();
        }
        else
        {
            canvasGroup.alpha = show ? maxAlpha : 0f;
            canvasGroup.transform.localScale = show ? Vector3.one : startScale;
        }

        if (show) return;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        DOVirtual.DelayedCall(duration, () => { gameObject.SetActive(false); });
    }
}