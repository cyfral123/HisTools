using UnityEngine;

namespace HisTools.Utils;

public class Player
{
    public static Option<GameObject> GetObject()
    {
        var isFound = GameObject.Find("CL_Player");
        return Option.FromNullable(isFound);
    }

    public static Option<Transform> GetTransform()
    {
        if (GetObject().TryGet(out var value))
        {
            return Option.Some(value.transform);
        }

        return Option<Transform>.None();
    }

    public static Option<ENT_Player> GetPlayer()
    {
        var nullable = ENT_Player.GetPlayer();
        return Option.FromNullable(nullable);
    }
}