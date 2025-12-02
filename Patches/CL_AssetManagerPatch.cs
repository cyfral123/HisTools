using HarmonyLib;
using HisTools.Prefabs;
using UnityEngine;

namespace HisTools.Patches;

public static class CL_AssetManagerPatch
{
    [HarmonyPatch(typeof(CL_AssetManager), "InitializeAssetManager")]
    public static class CL_AssetManager_InitializeAssetManager_Patch
    {
        private static void Postfix()
        {
            if (PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "Feature_DebugInfo").IsNone)
            {
                Utils.Logger.Error("Cant load asset for debugInfo");
            }

            if (PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "SphereMarker").IsNone)
            {
                Utils.Logger.Error("Cant load asset for sphereMarker");
            }
            
            if (PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "InfoLabel").IsNone)
            {
                Utils.Logger.Error("Cant load asset for InfoLabel");
            }
        }
    }
}