using UnityEngine;

public class CustomHandhold : FeatureBase
{
    private readonly BoolSetting _handholdShimmerColorEnabledSetting;
    private readonly ColorSetting _handholdShimmerColor;

    public CustomHandhold() : base("CustomHandhold", "Edit handlond effects and colors")
    {
        _handholdShimmerColorEnabledSetting = AddSetting(new BoolSetting(this, "Custom shimmer color", "...", true));
        _handholdShimmerColor = AddSetting(new ColorSetting(this, "Shimmer color", "...", Color.white));
    }

    public override void OnEnable() => FXManager.UpdateHandholdMaterialSettings();
    public override void OnDisable() => FXManager.UpdateHandholdMaterialSettings();
    public override void OnSettingChanged(string _, IFeatureSetting __)
    {
        FXManager.UpdateHandholdMaterialSettings();
    }
}