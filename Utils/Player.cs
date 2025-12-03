using UnityEngine;

namespace HisTools.Utils;

public class Player
{
    public static Option<GameObject> GetObject()
    {
        var isFound = GameObject.Find("CL_Player");
        return isFound ? Option<GameObject>.Some(isFound) : Option<GameObject>.None();
    }
    
    public static Option<Transform> GetTransform()
    {
        if (GetObject().TryGet(out var value))
        {
            return Option<Transform>.Some(value.transform);
        }
        
        return Option<Transform>.None();
    }
}