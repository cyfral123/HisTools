using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HisTools.Features.Controllers;
using HisTools.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HisTools.UI.Controllers;

public class UIButtonFactory
{
    public static void CreateButton(IFeature feature)
    {
        if (feature.Category == null || feature.Category.LayoutTransform == null)
            throw new Exception($"Feature '{feature.Name}' does not have a category assigned");

        var hasSettings = feature.Settings.Count > 0;

        var button = new GameObject($"FeatureButton_{feature.Name}").AddComponent<FeatureButton>();
        button.Feature = feature;

        if (hasSettings)
        {
            CoroutineRunner.Instance.StartCoroutine(AwaitLoadSettingsButton(feature, button));
        }
        else
        {
            Utils.Logger.Debug($"UIButtonFactory: Created FeatureButton for '{feature.Name}'");
        }
    }

    private static IEnumerator AwaitLoadSettingsButton(IFeature feature, FeatureButton button)
    {
        yield return null;
        var settingsButton = new GameObject($"SettingsButton_{feature.Name}").AddComponent<SettingsButton>();
        settingsButton.transform.SetParent(button.transform, false);
        settingsButton.Feature = feature;
        Utils.Logger.Debug($"UIButtonFactory: Created FeatureButtonWithSettings for '{feature.Name}'");
    }

    public static void CreateAllButtons(IEnumerable<IFeature> features)
    {
        var json = Files.LoadOrRepairJson(Constants.Paths.FeaturesStateConfigFilePath);
        var enumerable = features as IFeature[] ?? features.ToArray();

        foreach (var kvp in json)
        {
            try
            {
                var feature = enumerable.First(f => f.Name == kvp.Key);

                if (kvp.Value != null && (!kvp.Value.Type.Equals(JTokenType.Boolean)))
                {
                    Utils.Logger.Warn($"CreateAllButtons: Cant restore saved state for '{kvp.Key}'");
                    continue;
                }

                EventBus.Publish(new FeatureToggleEvent(feature, (bool)kvp.Value));
            }
            catch (Exception ex)
            {
                Utils.Logger.Warn($"CreateAllButtons: Feature invalid '{kvp.Key}' -> {ex.Message}");
                continue;
            }

            Utils.Logger.Debug($"CreateAllButtons: State restored for '{kvp.Key}' -> {kvp.Value}");
        }

        foreach (var feature in enumerable)
        {
            try
            {
                CreateButton(feature);
            }
            catch (Exception ex)
            {
                Utils.Logger.Error($"UIButtonFactory: Failed to create button for '{feature.Name}': {ex.Message}");
            }
        }
    }
}