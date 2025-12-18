using System;
using System.Linq;
using HisTools.Features.Controllers;
using HisTools.Prefabs;
using UnityEngine;
using UnityEngine.UI;

namespace HisTools.UI;

public class SettingsButton : MonoBehaviour
{
    private RectTransform _settingsRect;
    private Button _settingsButton;
    public IFeature Feature;

    private void Awake()
    {
        var settingsImg = gameObject.AddComponent<Image>();
        var sprite = PrefabDatabase.Instance.GetTexture("histools/Wrench");
        if (!sprite)
        {
            Utils.Logger.Error("PrefabDatabase: Texture 'histools/Wrench' not found");
            return;
        }

        
        settingsImg.sprite =
            Sprite.Create(sprite, new Rect(0, 0, sprite.width, sprite.height), new Vector2(0.5f, 0.5f));
        settingsImg.color = Color.gray;

        _settingsRect = gameObject.GetComponent<RectTransform>();
        _settingsRect.anchorMin = new Vector2(1, 0.5f);
        _settingsRect.anchorMax = new Vector2(1, 0.5f);
        _settingsRect.pivot = new Vector2(1, 0.5f);
        _settingsRect.anchoredPosition = new Vector2(-2f, -1f);
        _settingsRect.sizeDelta = new Vector2(25f, 25f);

        _settingsButton = gameObject.AddComponent<Button>();
        var navigation = _settingsButton.navigation;
        navigation.mode = Navigation.Mode.None;
        _settingsButton.navigation = navigation;
        _settingsButton.targetGraphic = settingsImg;

        var colors = _settingsButton.colors;
        colors.normalColor = Color.gray;
        colors.highlightedColor = Color.white;

        colors.pressedColor = Utils.Palette.FromHtml(Plugin.EnabledHtml.Value);
        colors.disabledColor = Color.black;
        colors.colorMultiplier = 1f;
        _settingsButton.colors = colors;

        _settingsButton.transition = Selectable.Transition.ColorTint;
        _settingsButton.onClick.AddListener(() => { EventBus.Publish(new FeatureSettingsMenuToggleEvent(Feature)); });
    }
}