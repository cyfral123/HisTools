using DG.Tweening;
using HisTools.Features.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HisTools.UI;

[RequireComponent(typeof(Toggle))]
public class FeatureButton : MonoBehaviour
{
    public Color enabledColor = Color.green;
    public Color disabledColor = Color.gray;
    public TextMeshProUGUI textLabel;

    private Toggle _toggle;
    private const float MinHeight = 25f;
    public IFeature Feature;

    private void Start()
    {
        transform.SetParent(Feature.Category.LayoutTransform, false);
        enabledColor = Utils.Palette.FromHtml(Plugin.EnabledHtml.Value);

        _toggle = GetComponent<Toggle>();
        var navigation = _toggle.navigation;
        navigation.mode = Navigation.Mode.None;
        _toggle.navigation = navigation;

        var layoutElement = gameObject.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = MinHeight;
        layoutElement.flexibleWidth = 1f;

        var img = gameObject.AddComponent<Image>();
        img.color = Utils.Palette.FromHtml(Plugin.BackgroundHtml.Value);
        var shadow = gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(2f, -2f);
        shadow.effectDistance = new Vector2(3f, -3f);

        textLabel = transform.AddMyText(Feature.Name, TextAlignmentOptions.Left, 16f, Color.gray, 6f);
        UpdateState(Feature.Enabled);

        _toggle.onValueChanged.AddListener(UpdateState);
    }

    private void UpdateState(bool isOn)
    {
        var targetColor = isOn ? enabledColor : disabledColor;
        var targetScale = isOn ? Vector2.one * 1f : Vector2.one * 0.96f;
        
        textLabel.DOColor(targetColor, 0.3f).SetEase(Ease.InOutCubic);
        textLabel.rectTransform.DOScale(targetScale, 0.3f).SetEase(Ease.OutBack);
        _toggle.isOn = isOn;
        EventBus.Publish(new FeatureToggleEvent(Feature, isOn));
    }
}