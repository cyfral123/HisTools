using System;
using System.Collections;
using System.Collections.Generic;
using HisTools.Features.Controllers;
using HisTools.Utils;
using UnityEngine;

namespace HisTools.UI.Controllers;

public class UIButtonFactory
{
    public FeatureButton CreateButton(IFeature feature)
    {
        if (feature.Category == null || feature.Category.LayoutTransform == null)
            throw new Exception($"Feature '{feature.Name}' does not have a category assigned");

        bool hasSettings = feature.Settings.Count > 0;

        var button = new GameObject($"FeatureButton_{feature.Name}").AddComponent<FeatureButton>();
        button.Feature = feature;


        if (hasSettings)
        {
            // Waiting for WKAssetsDatabase to take wrench icon for setting button
            CoroutineRunner.Instance.StartCoroutine(AwaitLoad(() =>
            {
                var settingsButton = new GameObject($"SettingsButton_{feature.Name}").AddComponent<SettingsButton>();
                settingsButton.Feature = feature;
                settingsButton.transform.SetParent(button.transform, false);
                Utils.Logger.Debug($"UIButtonFactory: Created FeatureButtonWithSettings for '{feature.Name}'");
            }));
        }
        else
        {
            Utils.Logger.Debug($"UIButtonFactory: Created FeatureButton for '{feature.Name}'");
        }

        return button;
    }

    private static IEnumerator AwaitLoad(Action action)
    {
        yield return new WaitForSeconds(1f);
        action?.Invoke();
    }

    public void CreateAllButtons(IEnumerable<IFeature> features)
    {
        foreach (var feature in features)
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