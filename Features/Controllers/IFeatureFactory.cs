using System.Collections.Generic;

public interface IFeatureFactory
{
    IFeature Create(string featureName);
    IEnumerable<string> GetAllFeatureNames();
}