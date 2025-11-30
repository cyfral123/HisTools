using System.Collections.Generic;
using System.Linq;

namespace HisTools.Features.Controllers;

public static class FeatureRegistry
{
    private static readonly List<IFeature> _features = [];

    public static void Register(IFeature feature) => _features.Add(feature);
    public static IEnumerable<IFeature> GetAll() => _features;

    public static IFeature GetByName(string name) => _features.FirstOrDefault(f => f.Name == name);

    public static IFeature GetByType<T>() where T : IFeature => _features.First(f => f is T);
}