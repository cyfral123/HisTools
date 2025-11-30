using System;
using System.Collections.Generic;

namespace HisTools.Features.Controllers;

public class FeatureFactory : IFeatureFactory
{
    private readonly Dictionary<string, Func<IFeature>> _registry = [];

    public void Register(string name, Func<IFeature> creator)
    {
        _registry[name] = creator;
    }

    public IFeature Create(string featureName)
    {
        if (!_registry.TryGetValue(featureName, out var creator))
            throw new Exception($"Feature {featureName} is not registered");

        var feature = creator();
        FeatureRegistry.Register(feature);
        return feature;
    }

    public IEnumerable<string> GetAllFeatureNames() => _registry.Keys;
}