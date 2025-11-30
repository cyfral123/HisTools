using UnityEngine;

namespace HisTools.Features.Controllers;

public class BoolSetting(IFeature feature, string name, string description, bool defaultValue)
    : SettingBase<bool>(feature, name, description, defaultValue)
{
    public override bool Value
    {
        get => base.Value;
        set
        {
            base.Value = value;
            EventBus.Publish(new FeatureSettingChangedEvent(this, Feature));
        }
    }
}

public class FloatSliderSetting(
    IFeature feature,
    string name,
    string description,
    float defaultValue,
    float min,
    float max,
    float step,
    int decimals) : SettingBase<float>(feature, name, description, defaultValue)
{
    public float Min { get; } = min;
    public float Max { get; } = max;
    public float Step { get; } = step;
    public int Decimals { get; } = decimals;

    public override float Value
    {
        get => base.Value;
        set
        {
            base.Value = Mathf.Clamp(value, Min, Max);
            EventBus.Publish(new FeatureSettingChangedEvent(this, Feature));
        }
    }
}

public class ColorSetting(IFeature feature, string name, string description, Color defaultValue)
    : SettingBase<Color>(feature, name, description, defaultValue)
{
    public override Color Value
    {
        get => base.Value;
        set
        {
            base.Value = value;
            EventBus.Publish(new FeatureSettingChangedEvent(this, Feature));
        }
    }
}