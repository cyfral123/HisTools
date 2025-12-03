using UnityEngine;

namespace HisTools.Utils;

public static class Vectors
{
    public static Vector3 ConvertPointToAbsolute(Vector3 localPoint)
    {
        return Level.GetCurrentTransform().TryGet(out var levelTransform)
            ? levelTransform.TransformPoint(localPoint)
            : localPoint;
    }

    public static Vector3 ConvertPointToLocal(Vector3 worldPoint)
    {
        return Level.GetCurrentTransform().TryGet(out var levelTransform)
            ? levelTransform.InverseTransformPoint(worldPoint)
            : worldPoint;
    }
}