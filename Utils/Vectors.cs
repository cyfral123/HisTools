using UnityEngine;

namespace Utils;

public static class Vectors
{
    public static Vector3 ConvertPointToAbsolute(Vector3 localPoint)
    {
        var levelTransform = CL_EventManager.currentLevel?.transform;
        if (levelTransform != null)
            return levelTransform.TransformPoint(localPoint);
        else
            return localPoint;
    }

    public static Vector3 ConvertPointToLocal(Vector3 worldPoint)
    {
        var levelTransform = CL_EventManager.currentLevel?.transform;
        if (levelTransform != null)
            return levelTransform.InverseTransformPoint(worldPoint);
        else
            return worldPoint;
    }
}