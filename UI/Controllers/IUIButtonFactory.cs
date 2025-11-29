using System.Collections.Generic;
using UI;

public interface IUIButtonFactory
{
    FeatureButton CreateButton(IFeature feature);
    void CreateAllButtons(IEnumerable<IFeature> features);
}