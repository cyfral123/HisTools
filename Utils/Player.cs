using UnityEngine;

namespace HisTools.Utils;

public class Player
{
    public static Option<GameObject> GetObject()
    {
        var isFound = GameObject.Find("CL_Player");
        
        if (isFound == null)
        {
            Logger.Error("Player object not found");
            return Option<GameObject>.None();
        }

        return Option<GameObject>.Some(isFound);
    }

    public static Option<Transform> GetTransform()
    {
        if (GetObject().TryGet(out var value))
        {
            return Option.Some(value.transform);
        }
        
        Logger.Error("Player transform not found");
        return Option<Transform>.None();
    }

    public static Option<ENT_Player> GetPlayer()
    {
        var nullable = ENT_Player.GetPlayer();
        
        if (nullable == null)
        {
            Logger.Error("Player not found");
            return Option<ENT_Player>.None();
        }
        
        return Option<ENT_Player>.Some(nullable);
    }
}