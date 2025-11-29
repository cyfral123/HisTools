using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

[RequireComponent(typeof(Toggle), typeof(RectTransform), typeof(Button))]
public class SettingsButton : MonoBehaviour
{

    private Toggle _toggle;
    private RectTransform _settingsRect;
    private Button _settingsButton;
    public IFeature Feature;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        var navigation = _toggle.navigation;
        navigation.mode = Navigation.Mode.None;
        _toggle.navigation = navigation;

        _settingsRect = GetComponent<RectTransform>();
        _settingsRect.anchorMin = new Vector2(1, 0.5f);
        _settingsRect.anchorMax = new Vector2(1, 0.5f);
        _settingsRect.pivot = new Vector2(1, 0.5f);
        // -1f because shadow
        _settingsRect.anchoredPosition = new Vector2(0f, -1f);
        _settingsRect.sizeDelta = new Vector2(25f, 25f);

        var settingsImg = gameObject.AddComponent<Image>();

        var wrenchIcon = LoadWrenchIcon();
        settingsImg.sprite = wrenchIcon;
        settingsImg.color = Color.gray;

        _settingsButton = GetComponent<Button>();
        _settingsButton.navigation = navigation;

        var colors = _settingsButton.colors;
        colors.normalColor = Color.gray;
        colors.highlightedColor = Color.white;
        
        colors.pressedColor = Utils.Palette.FromHtml(Plugin.EnabledHtml.Value);
        colors.disabledColor = Color.black;
        colors.colorMultiplier = 1f;
        _settingsButton.colors = colors;

        _settingsButton.transition = Selectable.Transition.ColorTint;
        _settingsButton.onClick.AddListener(() =>
        {
            EventBus.Publish(new FeatureSettingsMenuToggleEvent(Feature));
        });
    }

    private static Sprite LoadWrenchIcon()
    {
        Sprite wrenchIcon = null;

        try
        {
            wrenchIcon = CL_AssetManager.GetSpriteAsset("Wrench");
        }
        catch (Exception ex)
        {
            Utils.Logger.Warn($"Failed to load wrench icon from CL_AssetManager: {ex.Message}");
        }

        if (wrenchIcon != null)
        {
            wrenchIcon.texture.filterMode = FilterMode.Bilinear;
            return wrenchIcon;
        }

        return CreateFallbackSprite();
    }

    private static Sprite CreateFallbackSprite()
    {
        const int size = 24;
        var texture = new Texture2D(size, size);

        var pixels = Enumerable.Repeat(Color.gray, size * size).ToArray();
        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}