using UnityEngine;

public class CustomHandhold : FeatureBase
{
    // Patches/FXManagerPatch.cs
    public CustomHandhold() : base("CustomHandhold", "Edit handlond effects and colors")
    {
        AddSetting(new BoolSetting(this, "Custom shimmer color", "...", true));
        AddSetting(new ColorSetting(this, "Shimmer color", "...", Color.white));
    }

    public override void OnEnable() => FXManager.UpdateHandholdMaterialSettings();
    public override void OnDisable() => FXManager.UpdateHandholdMaterialSettings();
    public override void OnSettingChanged(string _, IFeatureSetting __)
    {
        FXManager.UpdateHandholdMaterialSettings();
    }
}