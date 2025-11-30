using UnityEngine;

namespace HisTools.Utils;

public class CoroutineRunner : MonoBehaviour
{
    public static CoroutineRunner Instance
    {
        get
        {
            if (!field)
            {
                var go = new GameObject("HisTools_CoroutineRunner");
                field = go.AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(go);
            }
            Logger.Debug("CoroutineRunner called");
            return field;
        }
    }
}