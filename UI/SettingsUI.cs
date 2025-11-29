using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public static class SettingsUI
{

    public static void DrawSettingsUI(IFeature feature, RectTransform parent, int maxPerColumn = 5)
    {
        foreach (Transform child in parent)
            GameObject.Destroy(child.gameObject);

        var row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlHeight = true;
        hl.childControlWidth = false;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        var labelGO = new GameObject("HisTools_SettingsTitleLabel", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);

        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = $"{feature.Name} Settings:";
        label.fontSize = 16;
        label.fontWeight = FontWeight.Bold;
        label.color = Utils.Palette.FromHtml(Plugin.AccentHtml.Value);

        var buttonGO = new GameObject("HisTools_SettingsResetButton", typeof(RectTransform));
        buttonGO.transform.SetParent(row.transform, false);

        var image = buttonGO.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 1);

        var resetButton = buttonGO.AddComponent<Button>();
        resetButton.onClick.AddListener(() =>
        {
            foreach (var setting in feature.Settings)
                setting.ResetToDefault();

            EventBus.Publish(new SettingsPanelShouldRefreshEvent());
        });

        var btnTextGO = new GameObject("HisTools_SettingsResetText", typeof(RectTransform));
        btnTextGO.transform.SetParent(buttonGO.transform, false);

        var btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnText.text = "Reset";
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 16;

        // Layout
        var btnLayout = buttonGO.AddComponent<LayoutElement>();
        btnLayout.preferredHeight = 20f;
        btnLayout.preferredWidth = 60f;

        var columnsGO = new GameObject("HisTools_Columns");
        columnsGO.transform.SetParent(parent, false);

        var hLayout = columnsGO.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 50f;
        hLayout.childAlignment = TextAnchor.UpperLeft;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = true;

        int count = 0;
        RectTransform currentColumn = null;

        float duration = 0.4f;
        float delayPerItem = 0.025f;

        foreach (var setting in feature.Settings)
        {
            if (count % maxPerColumn == 0)
            {
                var colGO = new GameObject($"HisTools_Column_{count / maxPerColumn + 1}");
                colGO.transform.SetParent(columnsGO.transform, false);
                currentColumn = colGO.AddComponent<RectTransform>();
                var vLayout = colGO.AddComponent<VerticalLayoutGroup>();
                vLayout.childForceExpandWidth = false;
                vLayout.childForceExpandHeight = false;
                vLayout.childControlHeight = true;
                vLayout.spacing = 10f;
            }

            GameObject go = null;

            switch (setting)
            {
                case BoolSetting boolSetting:
                    go = CreateSwitch(currentColumn, boolSetting);
                    break;
                case FloatSliderSetting floatSetting:
                    go = CreateSlider(currentColumn, floatSetting);
                    break;
                case ColorSetting colorSetting:
                    go = CreateColorPicker(currentColumn, colorSetting);
                    break;
                default:
                    Utils.Logger.Warn($"Unsupported setting type: {setting.GetType().Name}");
                    break;
            }

            if (go != null)
            {
                go.transform.localScale = Vector3.zero;
                go.transform.DOScale(Vector3.one, duration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(count * delayPerItem);
            }

            count++;
        }

    }

    private static GameObject CreateSwitch(Transform parent, BoolSetting setting)
    {
        var rootGO = new GameObject($"HisTool_Switch_{setting.Name}");
        rootGO.transform.SetParent(parent, false);
        var layout = rootGO.AddComponent<LayoutElement>();
        layout.minHeight = 35f;
        layout.preferredWidth = 250f;

        var trackGO = new GameObject("HisTools_Track");
        trackGO.transform.SetParent(rootGO.transform, false);
        var trackRect = trackGO.AddComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0, 0.5f);
        trackRect.anchorMax = new Vector2(0, 0.5f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.sizeDelta = new Vector2(50, 25);
        trackRect.anchoredPosition = new Vector2(25, 0);

        var trackImg = trackGO.AddComponent<Image>();
        trackImg.color = setting.Value ? Utils.Palette.HtmlColorDark(Plugin.EnabledHtml.Value) : Utils.Palette.FromHtml(Plugin.BackgroundHtml.Value);
        trackImg.raycastTarget = true;
        trackImg.type = Image.Type.Sliced;

        var toggle = trackGO.AddComponent<Toggle>();
        toggle.transition = Selectable.Transition.None;
        toggle.targetGraphic = trackImg;
        var navigation = toggle.navigation;
        navigation.mode = Navigation.Mode.None;
        toggle.navigation = navigation;

        var knobGO = new GameObject("HisTools_Knob");
        knobGO.transform.SetParent(trackGO.transform, false);
        var knobRect = knobGO.AddComponent<RectTransform>();
        knobRect.anchorMin = new Vector2(0, 0.5f);
        knobRect.anchorMax = new Vector2(0, 0.5f);
        knobRect.pivot = new Vector2(0.5f, 0.5f);
        knobRect.sizeDelta = new Vector2(20, 20);
        knobRect.anchoredPosition = setting.Value ? new Vector2(37.5f, 0) : new Vector2(12.5f, 0);

        var knobImg = knobGO.AddComponent<Image>();
        knobImg.color = setting.Value ? Utils.Palette.FromHtml(Plugin.EnabledHtml.Value) : new Color(0.3f, 0.3f, 0.3f, 1f);
        knobImg.raycastTarget = false;

        var labelGO = new GameObject("HisTools_Label");
        labelGO.transform.SetParent(rootGO.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(70, 0);
        labelRect.offsetMax = new Vector2(-5, 0);

        var text = labelGO.AddComponent<TextMeshProUGUI>();
        text.text = setting.Name;
        text.color = Color.gray;
        text.fontSize = 16f;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;

        toggle.onValueChanged.AddListener(isOn =>
        {
            float targetX = isOn ? 37.5f : 12.5f;
            knobRect.DOAnchorPosX(targetX, 0.15f).SetEase(Ease.OutQuad);

            Color targetTrackColor = isOn ? Utils.Palette.HtmlColorDark(Plugin.EnabledHtml.Value) : Utils.Palette.FromHtml(Plugin.BackgroundHtml.Value);
            Color targetKnobColor = isOn ? Utils.Palette.FromHtml(Plugin.EnabledHtml.Value) : new Color(0.3f, 0.3f, 0.3f, 1f);
            trackImg.DOColor(targetTrackColor, 0.15f);
            knobImg.DOColor(targetKnobColor, 0.15f);

            if (setting.Value != isOn)
                setting.Value = isOn;
        });

        toggle.SetIsOnWithoutNotify(setting.Value);

        return rootGO;
    }

    private static GameObject CreateSlider(Transform parent, FloatSliderSetting setting)
    {
        var rootGO = new GameObject($"HisTools_Slider_{setting.Name}");
        rootGO.transform.SetParent(parent, false);
        var rect = rootGO.AddComponent<RectTransform>();
        var layout = rootGO.AddComponent<LayoutElement>();
        layout.minHeight = 35f;
        layout.preferredWidth = 250f;

        var labelGO = new GameObject("HisTools_Label");
        labelGO.transform.SetParent(rootGO.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0, 10);

        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = $"{setting.Name} - <color=white>{setting.Value}</color>";
        labelText.color = Color.gray;
        labelText.fontSize = 16;
        labelText.alignment = TextAlignmentOptions.TopLeft;
        labelText.raycastTarget = false;

        var sliderGO = new GameObject("HisTools_Slider");
        sliderGO.transform.SetParent(rootGO.transform, false);
        var sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0);
        sliderRect.anchorMax = new Vector2(1, 0);
        sliderRect.pivot = new Vector2(0.5f, 0);
        sliderRect.anchoredPosition = Vector2.zero;
        sliderRect.sizeDelta = new Vector2(0, 5);

        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = setting.Min;
        slider.maxValue = setting.Max;
        slider.value = setting.Value;
        slider.wholeNumbers = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };

        var bgGO = new GameObject("HisTools_Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Utils.Palette.FromHtml(Plugin.BackgroundHtml.Value);

        slider.targetGraphic = bgImg;

        var fillGO = new GameObject("HisTools_Fill");
        fillGO.transform.SetParent(sliderGO.transform, false);
        var fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = Utils.Palette.FromHtml(Plugin.EnabledHtml.Value);
        slider.fillRect = fillRect;

        var handleGO = new GameObject("HisTools_Handle");
        handleGO.transform.SetParent(sliderGO.transform, false);
        var handleRect = handleGO.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(12, 12);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Utils.Palette.HtmlColorDark(Plugin.EnabledHtml.Value);
        slider.handleRect = handleRect;

        slider.onValueChanged.AddListener(v =>
        {
            if (setting.Step > 0f)
                v = Mathf.Round(v / setting.Step) * setting.Step;
            v = (float)Math.Round(v, setting.Decimals);
            labelText.text = $"{setting.Name} - <color=white>{v}</color>";
            if (setting.Value != v)
                setting.Value = v;
            slider.SetValueWithoutNotify(setting.Value);
        });

        return rootGO;
    }

    private static GameObject CreateColorPicker(Transform parent, ColorSetting setting)
    {
        var rootGO = new GameObject($"HisTools_ColorTextPicker_{setting.Name}");
        rootGO.transform.SetParent(parent, false);
        var rect = rootGO.AddComponent<RectTransform>();
        var layout = rootGO.AddComponent<LayoutElement>();
        layout.minHeight = 40f;
        layout.preferredWidth = 150f;

        var labelGO = new GameObject("HisTools_Label");
        labelGO.transform.SetParent(rootGO.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0, 20);

        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = setting.Name;
        labelText.fontSize = 16;
        labelText.color = Color.gray;
        labelText.alignment = TextAlignmentOptions.TopLeft;
        labelText.raycastTarget = false;

        var inputGO = new GameObject("HisTools_InputField");
        inputGO.transform.SetParent(rootGO.transform, false);
        var inputRect = inputGO.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 0);
        inputRect.anchorMax = new Vector2(1, 0);
        inputRect.pivot = new Vector2(0.5f, 0);
        inputRect.anchoredPosition = Vector2.zero;
        inputRect.sizeDelta = new Vector2(0, 20);

        var input = inputGO.AddComponent<TMP_InputField>();
        input.text = "#" + ColorUtility.ToHtmlStringRGBA(setting.Value);
        var navigation = input.navigation;
        navigation.mode = Navigation.Mode.None;
        input.navigation = navigation;

        var bgGO = new GameObject("HisTools_Background");
        bgGO.transform.SetParent(inputGO.transform, false);
        var bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Utils.Palette.FromHtml(Plugin.BackgroundHtml.Value);

        input.targetGraphic = bgImg;

        var textGO = new GameObject("HisTools_Text");
        textGO.transform.SetParent(inputGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5, 0);
        textRect.offsetMax = new Vector2(-5, 0);

        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = input.text;
        text.fontSize = 16;
        text.color = setting.Value;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;

        input.textComponent = text;

        input.onValueChanged.AddListener(str =>
        {
            if (ColorUtility.TryParseHtmlString(str, out var c))
                text.color = c;
            if (setting.Value != c)
                setting.Value = c;
        });

        input.onEndEdit.AddListener(str =>
        {
            if (!str.StartsWith("#"))
                str = "#" + str;

            if (ColorUtility.TryParseHtmlString(str, out var c))
            {
                input.text = "#" + ColorUtility.ToHtmlStringRGBA(c);
                text.color = c;
                if (setting.Value != c)
                    setting.Value = c;
            }
            else
            {
                input.text = "#" + ColorUtility.ToHtmlStringRGBA(setting.Value);
            }
        });

        return rootGO;
    }
}
