using HisTools.Features.Controllers;
using UnityEngine;

namespace HisTools.Features;

public class CustomFog : FeatureBase
{
    // Patches/FXManagerPatch.cs
    public CustomFog() : base("CustomFog", "Edit fog effects and colors")
    {
        AddSetting(new BoolSetting(this, "Color from palette", "...", true));
        AddSetting(new BoolSetting(this, "Fog visible", "...", true));
        AddSetting(new ColorSetting(this, "Fog color", "...", Color.white));
        AddSetting(new FloatSliderSetting(this, "Fog opacity", "...", 0.025f, 0f, 0.25f, 0.001f, 3));
    }
}