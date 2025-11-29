using Newtonsoft.Json.Linq;
using System.IO;
using Utils;

public static class RecoverState
{
    public static void FeaturesState(string filePath)
    {
        var json = Files.LoadOrRepairJson(filePath);

        foreach (var kvp in json)
        {
            var feature = FeatureRegistry.GetByName(kvp.Key);
            if (feature == null || !kvp.Value.Type.Equals(JTokenType.Boolean))
            {
                Logger.Warn($"RecoverState: Feature invalid '{kvp.Key}'");
                continue;
            }

            Logger.Debug($"RecoverState: Feature '{kvp.Key}' -> {kvp.Value}");

            EventBus.Publish(new FeatureToggleEvent(feature, (bool)kvp.Value));
        }
    }
}
