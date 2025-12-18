using HisTools.Utils.RouteFeature;
using UnityEngine;

namespace HisTools.Utils;

public static class Vectors
{
    public static Vector3 ConvertPointToAbsolute(Vector3 localPoint)
    {
        return CL_EventManager.currentLevel?.transform.TransformPoint(localPoint) ?? localPoint;
    }
    
    public static Vector3 ConvertPointToAbsolute(Vec3Dto localPoint)
    {
        var vecLocalPoint = new Vector3(localPoint.x, localPoint.y, localPoint.z);
        return CL_EventManager.currentLevel?.transform.TransformPoint(vecLocalPoint) ?? vecLocalPoint;
    }
    
    public static Vector3 ConvertPointToLocal(Vector3 worldPoint)
    {
        return CL_EventManager.currentLevel?.transform.InverseTransformPoint(worldPoint) ?? worldPoint;
    }
    
    public static Vector3 ConvertPointToLocal(Vec3Dto worldPoint)
    {
        var vecWorldPoint = new Vector3(worldPoint.x, worldPoint.y, worldPoint.z);
        return CL_EventManager.currentLevel?.transform.InverseTransformPoint(vecWorldPoint) ?? vecWorldPoint;
    }
}
