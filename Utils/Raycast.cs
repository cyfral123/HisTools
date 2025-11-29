using UnityEngine;

namespace Utils;

public static class Raycast
{
    public static Vector3 GetLookTarget(Transform origin, float maxDistance, float offset = -0.5f)
    {
        var ray = new Ray(origin.position, origin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            return hit.point + origin.forward * offset;
        }

        return origin.position + origin.forward * maxDistance;
    }
}
