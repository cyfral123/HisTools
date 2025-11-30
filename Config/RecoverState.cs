using HisTools.Features.Controllers;
using HisTools.Utils;
using Newtonsoft.Json.Linq;

namespace HisTools.Config;

public static class RecoverState
{
    public static void FeaturesState(string filePath)
    {
        var json = Files.LoadOrRepairJson(filePath);

        foreach (var kvp in json)
        {
            var feature = FeatureRegistry.GetByName(kvp.Key);
            if (kvp.Value != null && (feature == null || !kvp.Value.Type.Equals(JTokenType.Boolean)))
            {
                Logger.Warn($"RecoverState: Feature invalid '{kvp.Key}'");
                continue;
            }

            Logger.Debug($"RecoverState: Feature '{kvp.Key}' -> {kvp.Value}");

            EventBus.Publish(new FeatureToggleEvent(feature, (bool)kvp.Value));
        }
    }
}