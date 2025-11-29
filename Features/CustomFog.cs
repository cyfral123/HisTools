using UnityEngine;

public class CustomFog : FeatureBase
{
    private readonly BoolSetting _fogVisibleSetting;
    private readonly BoolSetting _fogAutoColorEnabledSetting;
    private readonly ColorSetting _fogColorSetting;
    private readonly FloatSliderSetting _fogAlphaSetting;

    // Patches/FXManagerPatch.cs
    public CustomFog() : base("CustomFog", "Edit fog effects and colors")
    {
        _fogAutoColorEnabledSetting = AddSetting(new BoolSetting(this, "Color from palette", "...", true));
        _fogVisibleSetting = AddSetting(new BoolSetting(this, "Fog visible", "...", true));
        _fogColorSetting = AddSetting(new ColorSetting(this, "Fog color", "...", Color.white));
        _fogAlphaSetting = AddSetting(new FloatSliderSetting(this, "Fog opacity", "...", 0.025f, 0f, 0.25f, 0.001f, 3));
    }
}