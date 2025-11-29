using HarmonyLib;
using UnityEngine;
using System.Reflection;
using UI;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;


public static class PlayerPatch
{
    [HarmonyPatch(typeof(ENT_Player), "LateUpdate")]
    public static class PlayerPatch_LateUpdate_Patch
    {
        private static readonly FieldInfo _camSpeedField;
        private static readonly FieldInfo _velocity;

        static PlayerPatch_LateUpdate_Patch()
        {
            _camSpeedField = typeof(ENT_Player).GetField("camSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            _velocity = typeof(ENT_Player).GetField("vel", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        // Patch for enabling cursor while tools menu is opened
        public static void Postfix(ENT_Player __instance)
        {

            if (Input.GetKeyDown(Plugin.FeaturesMenuToggleKey.Value) && !CL_GameManager.isDead() && !CL_GameManager.gMan.isPaused)
            {
                FeaturesMenu.ToggleMenu();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                FeaturesMenu.HideMenu();
            }

            // no __instance.LockCamera() because it makes movement lags
            if (FeaturesMenu.IsMenuVisible)
                _camSpeedField.SetValue(__instance, 0f);
            else
                _camSpeedField.SetValue(__instance, 1f);

            var vel = (Vector3)_velocity.GetValue(__instance);
            EventBus.Publish(new PlayerLateUpdateEvent(__instance, vel));
        }
    }
}
