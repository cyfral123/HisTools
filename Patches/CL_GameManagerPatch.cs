using HarmonyLib;

public static class CL_GameManagerPatch
{
    [HarmonyPatch(typeof(CL_GameManager), "Start")]
    public static class CL_GameManager_Start_Patch
    {
        public static void Postfix()
        {
            EventBus.Publish(new GameStartEvent());
        }
    }
}