using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;
using Unity.VisualScripting;
using System;

public class SettingsPanelController : MonoBehaviour
{
    public static SettingsPanelController Instance { get; private set; }
    private static IFeature s_lastFeature;

    private static RectTransform s_panelRect;
    private static VerticalLayoutGroup s_layoutGroup;

    private const int _itemHeight = 70;
    private const int _itemCount = 3;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Canvas parentCanvas = UI.FeaturesMenu.Canvas;
        if (parentCanvas == null)
        {
            Utils.Logger.Error("Manager canvas not found!");
            return;
        }

        var panelGO = new GameObject("HisTools_SettingsPanel");
        panelGO.transform.SetParent(parentCanvas.transform, false);

        s_panelRect = panelGO.AddComponent<RectTransform>();
        s_panelRect.anchorMin = new Vector2(0, 0);
        s_panelRect.anchorMax = new Vector2(1, 0);
        s_panelRect.pivot = new Vector2(0.5f, 0);
        s_panelRect.sizeDelta = new Vector2(0, _itemCount * _itemHeight);
        s_panelRect.anchoredPosition = Vector2.zero;

        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);
        img.raycastTarget = false;

        s_layoutGroup = panelGO.AddComponent<VerticalLayoutGroup>();
        s_layoutGroup.childForceExpandWidth = false;
        s_layoutGroup.childForceExpandHeight = false;
        s_layoutGroup.childControlHeight = true;
        s_layoutGroup.spacing = 5f;
        s_layoutGroup.padding = new RectOffset(5, 5, 5, 5);

        panelGO.SetActive(false);

        // refresh settings panel after setting change
        EventBus.Subscribe<SettingsPanelShouldRefreshEvent>(_ =>
        {
            if (s_lastFeature == null)
                return;

            HideSettings();
            HandleSettingsToggle(s_lastFeature, true);
        });
    }

    public void HandleSettingsToggle(IFeature currentfeature, bool force = false)
    {
        Utils.Logger.Debug($"HandleSettingsToggle: called for '{currentfeature.Name}' ({currentfeature.Settings.Count} settings)");

        if (currentfeature == null)
        {
            Utils.Logger.Debug("HandleSettingsToggle: Tried to show settings for null feature");
            s_lastFeature = null;
            HideSettings();
            return;
        }

        if (s_lastFeature == currentfeature && !force)
        {
            s_lastFeature = null;
            HideSettings();
            return;
        }

        s_panelRect.gameObject.SetActive(true);
        CanvasGroup canvasGroup = s_panelRect.GetOrAddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.5f);

        foreach (Transform child in s_panelRect)
            Destroy(child.gameObject);

        s_lastFeature = currentfeature;

        SettingsUI.DrawSettingsUI(currentfeature, s_panelRect, _itemCount);
    }

    public void HideSettings()
    {
        s_panelRect.gameObject.SetActive(false);
    }

}
