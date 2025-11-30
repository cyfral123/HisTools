using System.Collections.Generic;
using HisTools.Features.Controllers;

namespace HisTools.UI.Controllers;

public interface IUIButtonFactory
{
    FeatureButton CreateButton(IFeature feature);
    void CreateAllButtons(IEnumerable<IFeature> features);
}