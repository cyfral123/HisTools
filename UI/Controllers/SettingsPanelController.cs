using DG.Tweening;
using HisTools.Features.Controllers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace HisTools.UI.Controllers;

public class SettingsPanelController : MonoBehaviour
{
    public static SettingsPanelController Instance { get; private set; }
    private static IFeature _lastFeature;

    private static RectTransform _panelRect;
    private static VerticalLayoutGroup _layoutGroup;

    private const int ItemHeight = 70;
    private const int ItemCount = 3;

    public void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        var parentCanvas = FeaturesMenu.Canvas;
        if (!parentCanvas)
        {
            Utils.Logger.Error("Manager canvas not found!");
            return;
        }

        var panelGO = new GameObject("HisTools_SettingsPanel");
        panelGO.transform.SetParent(parentCanvas.transform, false);

        _panelRect = panelGO.AddComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0, 0);
        _panelRect.anchorMax = new Vector2(1, 0);
        _panelRect.pivot = new Vector2(0.5f, 0);
        _panelRect.sizeDelta = new Vector2(0, ItemCount * ItemHeight);
        _panelRect.anchoredPosition = Vector2.zero;

        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);
        img.raycastTarget = false;

        _layoutGroup = panelGO.AddComponent<VerticalLayoutGroup>();
        _layoutGroup.childForceExpandWidth = false;
        _layoutGroup.childForceExpandHeight = false;
        _layoutGroup.childControlHeight = true;
        _layoutGroup.spacing = 5f;
        _layoutGroup.padding = new RectOffset(5, 5, 5, 5);

        panelGO.SetActive(false);

        // refresh settings panel after setting change
        EventBus.Subscribe<SettingsPanelShouldRefreshEvent>(_ =>
        {
            if (_lastFeature == null)
                return;

            HideSettings();
            HandleSettingsToggle(_lastFeature, true);
        });
    }

    public void HandleSettingsToggle(IFeature currentFeature, bool force = false)
    {
        Utils.Logger.Debug(
            $"HandleSettingsToggle: called for '{currentFeature.Name}' ({currentFeature.Settings.Count} settings)");

        if (_lastFeature == currentFeature && !force)
        {
            _lastFeature = null;
            HideSettings();
            return;
        }

        _panelRect.gameObject.SetActive(true);
        var canvasGroup = _panelRect.GetOrAddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.5f);

        foreach (Transform child in _panelRect)
            Destroy(child.gameObject);

        _lastFeature = currentFeature;

        SettingsUI.DrawSettingsUI(currentFeature, _panelRect, ItemCount);
    }

    public void HideSettings()
    {
        _panelRect.gameObject.SetActive(false);
    }
}