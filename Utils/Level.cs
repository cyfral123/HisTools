using UnityEngine;

namespace HisTools.Utils;

public static class Level
{
    public static Option<M_Level> GetCurrent()
    {
        return Option<M_Level>.FromNullable(CL_EventManager.currentLevel);
    }

    public static Option<Transform> GetCurrentTransform()
    {
        return GetCurrent().TryGet(out var value) ? Option<Transform>.Some(value.transform) : Option<Transform>.None();
    }
}