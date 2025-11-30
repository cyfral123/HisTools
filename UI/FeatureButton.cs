using HisTools.Features.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HisTools.UI;

[RequireComponent(typeof(Toggle))]
public class FeatureButton : MonoBehaviour
{
    public Color EnabledColor = Color.green;
    public Color DisabledColor = Color.gray;
    public TextMeshProUGUI TextLabel;

    private Toggle _toggle;
    public float MinHeight = 25f;
    public IFeature Feature;

    private void Start()
    {
        transform.SetParent(Feature.Category.LayoutTransform, false);
        EnabledColor = Utils.Palette.FromHtml(Plugin.EnabledHtml.Value);

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

        TextLabel = transform.AddMyText(Feature.Name, TextAlignmentOptions.Left, 16f, Color.gray, 5f);
        UpdateState(Feature.Enabled);

        _toggle.onValueChanged.AddListener(UpdateState);
    }

    private void UpdateState(bool isOn)
    {
        Utils.Logger.Debug($"FeatureButton: {Feature.Name} -> {isOn}");
        TextLabel.color = isOn ? EnabledColor : DisabledColor;
        _toggle.isOn = isOn;
        EventBus.Publish(new FeatureToggleEvent(Feature, isOn));
    }
}
