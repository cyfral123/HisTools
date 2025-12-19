using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace HisTools.Patches;

[HarmonyPatch(typeof(InputManager), "UpdateCursorVisibility")]
public static class UpdateCursorVisibility_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var code in instructions)
        {
            if (code.Calls(AccessTools.Method(typeof(CommandConsole), nameof(CommandConsole.IsConsoleVisible))))
            {
                yield return new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(typeof(UpdateCursorVisibility_Patch), nameof(IsAnyUIVisible))
                );
            }
            else
            {
                yield return code;
            }
        }
    }

    private static bool IsAnyUIVisible()
    {
        return CommandConsole.IsConsoleVisible()
               || UI.FeaturesMenu.IsMenuVisible
               || UI.Controllers.PopupController.IsPopupVisible;
    }
}