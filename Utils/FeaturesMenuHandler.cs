using System;
using HisTools.UI;
using UnityEngine;

namespace HisTools.Utils;

public class FeaturesMenuHandler : MonoBehaviour
{
    public Action OnToggle;
    
    private void Update()
    {
        if (Input.GetKeyDown(Plugin.FeaturesMenuToggleKey.Value) && !CL_GameManager.isDead() &&
            !CL_GameManager.gMan.isPaused)
        {
            OnToggle?.Invoke();
        }
    }
}
