using System;
using System.Reflection;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;

public static class App_PerkPagePatch
{
    [HarmonyPatch(typeof(App_PerkPage), "PurchaseRefresh")]
    public static class App_PerkPage_Refresh_Patch
    {
        private static readonly FieldInfo _fullfilled;
        private static readonly MethodInfo _generateCards;

        static App_PerkPage_Refresh_Patch()
        {
            _fullfilled = typeof(App_PerkPage).GetField("fullfilled", BindingFlags.NonPublic | BindingFlags.Instance);
            _generateCards = AccessTools.Method(typeof(App_PerkPage), "GenerateCards");
        }

        public static bool Prefix(App_PerkPage __instance)
        {
            try
            {
                var freeBuying = FeatureRegistry.GetByType<FreeBuying>();

                if (freeBuying.Enabled)
                {
                    if (freeBuying.GetSetting<BoolSetting>("Refresh perks").Value)
                    {
                        var fullFilled = (bool)_fullfilled.GetValue(__instance);

                        if (!fullFilled) // && CL_GameManager.roaches >= refreshCost)
                        {
                            // CL_GameManager.AddRoaches(-refreshCost);
                            _generateCards.Invoke(__instance, [true]);
                        }

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Logger.Error($"App_PerkPage_Refresh_Patch: Prefix: {ex.Message}");
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(App_PerkPage_Card), "BuyCard")]
    public static class App_PerkPage_Card_BuyCard_Patch
    {
        public static bool Prefix(App_PerkPage_Card __instance)
        {
            try
            {
                var freeBuying = FeatureRegistry.GetByType<FreeBuying>();

                if (freeBuying.Enabled)
                {
                    if (freeBuying.GetSetting<BoolSetting>("Perks").Value)
                    {
                        __instance.purchasedText.gameObject.SetActive(value: true);
                        __instance.purchasedText.localScale = Vector3.zero;
                        DOTween.Complete(__instance.transform);
                        __instance.purchasedText.DOScale(1f, 0.5f);
                        __instance.transform.DOPunchRotation(new Vector3(0f, 0f, 2f), 0.5f);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Logger.Error($"App_PerkPage_Card_BuyCard_Patch: Prefix: {ex.Message}");
            }

            return true;
        }
    }
}