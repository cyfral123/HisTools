using UnityEngine;

namespace HisTools.Utils;

public static class Vectors
{
    public static Vector3 ConvertPointToAbsolute(Vector3 localPoint)
    {
        return CL_EventManager.currentLevel?.transform.TransformPoint(localPoint) ?? localPoint;
    }

    public static Vector3 ConvertPointToLocal(Vector3 worldPoint)
    {
        return CL_EventManager.currentLevel?.transform.InverseTransformPoint(worldPoint) ?? worldPoint;
    }
}
