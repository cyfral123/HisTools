using System.Collections.Generic;

namespace HisTools.Features.Controllers;

public interface IFeature
{
    ICategory Category { get; set; }

    string Name { get; }
    string Description { get; }
    bool Enabled { get; set; }

    IReadOnlyList<IFeatureSetting> Settings { get; }

    void OnEnable();
    void OnDisable();
    void SetCategory(ICategory category);

    T GetSetting<T>(string name) where T : class, IFeatureSetting;
}