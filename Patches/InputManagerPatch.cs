using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

[HarmonyPatch(typeof(InputManager), "UpdateCursorVisibility")]
public static class UpdateCursorVisibility_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var methodIsMenuVisible = AccessTools.PropertyGetter(typeof(UI.FeaturesMenu), "IsMenuVisible");

        for (int i = 0; i < codes.Count - 1; i++)
        {
            if (codes[i].Calls(AccessTools.Method(typeof(CommandConsole), "IsConsoleVisible")))
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, methodIsMenuVisible));
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Or));
                break;
            }
        }
        return codes;
    }
}
