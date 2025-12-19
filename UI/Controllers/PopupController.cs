using DG.Tweening;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HisTools.UI.Controllers;

public class PopupController : MonoBehaviour
{
    public RectTransform panel;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI title;
    public TextMeshProUGUI description;

    [CanBeNull] public TMP_InputField inputField;
    public Button bgButton;
    public Button applyButton;
    public Button cancelButton;
    public static bool IsPopupVisible { get; set; }

    private Tween _tween;

    private void Awake()
    {
        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        if (!panel)
            panel = transform.Find("Window")?.GetComponent<RectTransform>();

        if (!canvasGroup || !panel)
        {
            Debug.LogError("PopupController: references not set", this);
            enabled = false;
            return;
        }

        title = transform.Find("Window/Title")?.GetComponentInChildren<TextMeshProUGUI>();
        description = transform.Find("Window/Content/Description")?.GetComponent<TextMeshProUGUI>();

        bgButton = transform.Find("Background")?.GetComponent<Button>();
        applyButton = transform.Find("Window/Apply")?.GetComponent<Button>();
        cancelButton = transform.Find("Window/Cancel")?.GetComponent<Button>();
        inputField = transform.Find("Window/Content/Input")?.GetComponent<TMP_InputField>();

        bgButton?.onClick.AddListener(Hide);
        cancelButton?.onClick.AddListener(Hide);
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        panel.localScale = Vector3.one * 0.9f;

        GetComponent<Canvas>().sortingOrder = 500;
    }

    public void Show()
    {
        if (!enabled) return;

        gameObject.SetActive(true);
        _tween?.Kill();

        _tween = DOTween.Sequence()
            .Append(canvasGroup.DOFade(1, 0.2f))
            .Join(panel.DOScale(1f, 0.25f).SetEase(Ease.OutBack))
            .OnStart(() => canvasGroup.blocksRaycasts = true);

        IsPopupVisible = true;
        CL_GameManager.gMan.lockPlayerInput = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        if (!enabled) return;


        _tween?.Kill();

        _tween = DOTween.Sequence()
            .Append(canvasGroup.DOFade(0, 0.15f))
            .Join(panel.DOScale(0.9f, 0.15f).SetEase(Ease.InCubic))
            .OnComplete(() =>
            {
                if (!this) return;
                canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            });

        IsPopupVisible = false;
        CL_GameManager.gMan.lockPlayerInput = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        _tween?.Kill();
    }
}