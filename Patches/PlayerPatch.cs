using System.Reflection;
using HarmonyLib;
using HisTools.UI;
using HisTools.UI.Controllers;
using UnityEngine;

namespace HisTools.Patches;

public static class PlayerPatch
{
    [HarmonyPatch(typeof(ENT_Player), "LateUpdate")]
    public static class PlayerPatch_LateUpdate_Patch
    {
        private static readonly FieldInfo CamSpeedField;
        private static readonly FieldInfo Velocity;

        static PlayerPatch_LateUpdate_Patch()
        {
            CamSpeedField = typeof(ENT_Player).GetField("camSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            Velocity = typeof(ENT_Player).GetField("vel", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void Postfix(ENT_Player __instance)
        {
            if (Input.GetKeyDown(Plugin.FeaturesMenuToggleKey.Value) && !CL_GameManager.isDead() &&
                !CL_GameManager.gMan.isPaused)
            {
                FeaturesMenu.ToggleMenu();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                FeaturesMenu.HideMenu();
            }

            // no __instance.LockCamera() because it makes movement lags
            CamSpeedField.SetValue(__instance, FeaturesMenu.IsMenuVisible || PopupController.IsPopupVisible ? 0f : 1f);

            var vel = (Vector3)Velocity.GetValue(__instance);
            EventBus.Publish(new PlayerLateUpdateEvent(vel));
        }
    }
}