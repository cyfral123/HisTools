using System;
using System.Collections.Generic;

public class FeatureFactory : IFeatureFactory
{
    private readonly Dictionary<string, Func<IFeature>> _registry = [];

    public FeatureFactory()
    {

    }
    
    public void Register(string name, Func<IFeature> creator)
    {
        _registry[name] = creator;
    }

    public IFeature Create(string featureName)
    {
        if (_registry.TryGetValue(featureName, out var creator))
        {
            var feature = creator();
            FeatureRegistry.Register(feature);
            return feature;
        }

        throw new Exception($"Feature {featureName} is not registered");
    }

    public void CreateAll()
    {
        foreach (var name in _registry.Keys)
        {
            Create(name);
        }
    }

    public IEnumerable<string> GetAllFeatureNames() => _registry.Keys;
}