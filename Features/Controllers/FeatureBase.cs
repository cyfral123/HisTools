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

    protected readonly List<IFeatureSetting> _settings = [];
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

    public virtual void OnEnable() { }
    public virtual void OnDisable() { }
    public virtual void OnSettingChanged(string featureName, IFeatureSetting setting) { }


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
        var result = _settings.FirstOrDefault(s => s.Name.ToLower() == name.ToLower());
        if (result == null)
        {
            Utils.Logger.Warn($"Feature {Name} does not have setting {name}");
            return null;
        }

        return result as T;
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

            object finalValue = ConvertToSettingType(loadedObj, setting.GetValue().GetType());

            setting.SetValue(finalValue);
        }
        catch (Exception ex)
        {
            Utils.Logger.Error($"Failed to load setting '{setting.Name}': {ex.Message}\n\n");
        }

        _settings.Add(setting);
        return setting;
    }

    private object ConvertToSettingType(object value, Type targetType)
    {
        if (value == null) return null;

        if (targetType == typeof(float))
        {
            if (value is double d) return (float)d;
            if (value is int i) return (float)i;
            if (value is long l) return (float)l;
            return Convert.ToSingle(value);
        }
        else if (targetType == typeof(int))
        {
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
            return Convert.ToInt32(value);
        }
        else if (targetType == typeof(Color))
        {
            if (value is string html && ColorUtility.TryParseHtmlString(html, out var color))
            {
                Utils.Logger.Debug($"Converted '{value}' to Color: {color}");
                return color;
            }
            else if (value is Color rgba)
            {
                Utils.Logger.Debug($"Converted '{value}' to Color: {rgba}");
                return rgba;
            }
            Utils.Logger.Debug($"Failed to convert '{value}' to Color");
            return Color.white;
        }

        return Convert.ChangeType(value, targetType);
    }
}