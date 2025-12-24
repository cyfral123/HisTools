using System;
using UnityEngine;

namespace HisTools.Utils;

public class EscCloseCanvasGroup : MonoBehaviour
{
    public CanvasGroup group;
    public Action OnHide;
    
    private void Update()
    {
        if (!group.interactable) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Logger.Debug("EscCloseCanvasGroup Hide");
            Hide();
            OnHide?.Invoke();
        }
    }

    private void Hide()
    {
        group.alpha = 0;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
