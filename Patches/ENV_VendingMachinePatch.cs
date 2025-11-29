using System;
using HarmonyLib;

public static class ENV_VendingMachinePatch
{
    [HarmonyPatch(typeof(ENV_VendingMachine), "Buy", [typeof(int), typeof(bool), typeof(bool)])]
    public static class ENV_VendingMachine_Buy_Patch
    {
        static void Prefix(ref int i, ref bool force, ref bool free)
        {
            try
            {
                var freeBuying = FeatureRegistry.GetByType<FreeBuying>();
                if (freeBuying.Enabled)
                {
                    if (freeBuying.GetSetting<BoolSetting>("Items").Value)
                        free = true;
                }
            }
            catch (Exception ex)
            {
                Utils.Logger.Error($"ENV_VendingMachine_Buy_Patch: Prefix: {ex.Message}");
            }
        }
    }
}