using HarmonyLib;

namespace HisTools.Patches;

public static class WorldPatch
{
    [HarmonyPatch(typeof(WorldLoader), "Update")]
    public static class WorldLoader_Update_Patch
    {
        public static void Postfix(WorldLoader __instance)
        {
            EventBus.Publish(new WorldUpdateEvent(__instance));
        }
    }

    [HarmonyPatch(typeof(CL_EventManager), "EnterLevel")]
    public static class CL_EventManager_EnterLevel_Patch
    {
        public static void Postfix(M_Level level)
        {
            EventBus.Publish(new EnterLevelEvent(level));
        }
    }
}