using System.Collections.Generic;

namespace HisTools.Features.Controllers;

public interface IFeatureFactory
{
    IFeature Create(string featureName);
    IEnumerable<string> GetAllFeatureNames();
}