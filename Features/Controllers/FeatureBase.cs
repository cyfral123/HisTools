using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HisTools.Features.Controllers;

public abstract class FeatureBase : IFeature
{
    public ICategory Category { get; set; }
    public string Name { get; }
    public string Description { get; }
    public bool Enabled { get; set; }

    private readonly List<IFeatureSetting> _settings = [];
    public IReadOnlyList<IFeatureSetting> Settings => _settings;

    protected FeatureBase(string name, string description)
    {
        Name = name;
        Description = description;

        EventBus.Subscribe<FeatureToggleEvent>(e =>
        {
            if (e.Feature != this) return;
            Utils.Logger.Debug($"FeatureBase: changing '{e.Feature.Name}' -> {e.Enabled}");

            if (e.Enabled) Enable();
            else Disable();
            Utils.Files.SaveFeatureStateToConfig(Name, Enabled);
        });

        EventBus.Subscribe<FeatureSettingChangedEvent>(e =>
        {
            if (e.Feature.Name != Name) return;
            OnSettingChanged(e.Feature.Name, e.Setting);
        });
    }

    public virtual void OnEnable()
    {
    }

    public virtual void OnDisable()
    {
    }

    protected virtual void OnSettingChanged(string _, IFeatureSetting __)
    {
    }


    private void Enable()
    {
        if (Enabled) return;
        Enabled = true;
        OnEnable();
    }

    private void Disable()
    {
        if (!Enabled) return;
        Enabled = false;
        OnDisable();
    }

    public void SetCategory(ICategory category)
    {
        Category = category;
    }

    public T GetSetting<T>(string name) where T : class, IFeatureSetting
    {
        var result = _settings.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.CurrentCultureIgnoreCase));
        if (result != null) return result as T;
        Utils.Logger.Warn($"Feature {Name} does not have setting {name}");
        return null;

    }

    protected T AddSetting<T>(T setting) where T : IFeatureSetting
    {
        try
        {
            var loadedObj = Utils.Files.GetSettingFromConfig(
                Name,
                setting.Name,
                setting.GetValue()
            );

            var finalValue = ConvertToSettingType(loadedObj, setting.GetValue().GetType());

            setting.SetValue(finalValue);
        }
        catch (Exception ex)
        {
            Utils.Logger.Error($"Failed to load setting '{setting.Name}': {ex.Message}\n\n");
        }

        _settings.Add(setting);
        return setting;
    }

    private static object ConvertToSettingType(object value, Type targetType)
    {
        if (value == null) return null;

        if (targetType == typeof(float))
        {
            return value switch
            {
                double d => (float)d,
                int i => i,
                long l => l,
                _ => Convert.ToSingle(value)
            };
        }

        if (targetType == typeof(int))
        {
            return value switch
            {
                long l => l,
                double d => d,
                _ => Convert.ToInt32(value)
            };
        }

        if (targetType == typeof(Color))
        {
            return value switch
            {
                string html when ColorUtility.TryParseHtmlString(html, out var color)
                    => color,

                Color rgba
                    => rgba,

                _ => Color.white
            };
        }

        return Convert.ChangeType(value, targetType);
    }
}