
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using static FXManager;

public static class FXManagerPatch
{
    // FXManager.UpdateHandholdMaterialSettings have only one call to Color.Lerp, replacing it to MyPatch.CustomLerp
    [HarmonyPatch(typeof(FXManager), "UpdateHandholdMaterialSettings")]
    public static class Patch_HandholdMaterial
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            var lerpMethod = AccessTools.Method(typeof(Color), "Lerp", [typeof(Color), typeof(Color), typeof(float)]);

            var customLerpMethod = AccessTools.Method(typeof(MyPatch), nameof(MyPatch.CustomLerp));

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(lerpMethod))
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, customLerpMethod);
                }
            }

            return codes.AsEnumerable();
        }
    }

    // Patch for fog feature
    [HarmonyPatch(typeof(FXManager), "FXRender")]
    public static class Patch_FXRender
    {
        public static void Postfix()
        {
            var fogFeature = FeatureRegistry.GetByType<CustomFog>();
            if (!fogFeature.Enabled)
                return;

            Vector4 value;
            var fogVisible = fogFeature.GetSetting<BoolSetting>("Fog visible").Value;
            var customFogAlpha = fogFeature.GetSetting<FloatSliderSetting>("Fog opacity").Value;
            if (fogFeature.GetSetting<BoolSetting>("Color from palette").Value)
            {
                var palette = Utils.Palette.FromHtml(Plugin.AccentHtml.Value);
                value = new Vector4(palette.r, palette.g, palette.b, customFogAlpha);
            }
            else
            {
                var customFogColor = fogFeature.GetSetting<ColorSetting>("Fog color").Value;
                value = new Vector4(customFogColor.r, customFogColor.g, customFogColor.b, customFogAlpha);
            }

            if (fogVisible)
            {
                Shader.SetGlobalVector("_FOG", value);
            }
            else
            {
                Shader.SetGlobalVector("_FOG", Vector4.zero);
            }
        }
    }

    // Patch for custom handhold colors
    public static class MyPatch
    {
        public static Color CustomLerp(Color a, Color b, float t)
        {
            var handholdFeature = FeatureRegistry.GetByType<CustomHandhold>();
            var customHandholdShimmerColorEnabled = handholdFeature.GetSetting<BoolSetting>("Custom shimmer color").Value;
            var customHandholdShimmerColor = handholdFeature.GetSetting<ColorSetting>("Shimmer color").Value;

            if (customHandholdShimmerColorEnabled && handholdFeature.Enabled)
            {
                return Color.Lerp(a, customHandholdShimmerColor, t);
            }
            else
            {
                return Color.Lerp(a, b, t);
            }
        }
    }
}