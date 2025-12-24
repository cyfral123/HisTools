using HarmonyLib;
using HisTools.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HisTools.Patches;

public static class CL_GameManagerPatch
{
    [HarmonyPatch(typeof(CL_GameManager), "Start")]
    public static class CL_GameManager_Start_Patch
    {
        public static void Postfix()
        {
            EventBus.Publish(new GameStartEvent());
            var pauseLayout = GameObject.Find("GameManager/Canvas/Pause/Pause Menu/Pause Buttons/Pause Layout");
            if (pauseLayout == null)
            {
                Utils.Logger.Error("Pause layout not found");
                return;
            }

            var resumeButton = pauseLayout?.transform.Find("Settings");
            if (resumeButton == null)
            {
                Utils.Logger.Error("Resume button not found");
                return;
            }


            var hisToolsButton = new GameObject("testHisToolsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            hisToolsButton.transform.SetParent(pauseLayout.transform, false);

            hisToolsButton.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 40);
            hisToolsButton.GetComponent<Image>().color = Color.gray;

            hisToolsButton.gameObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                var prefab = Prefabs.PrefabDatabase.Instance.GetObject("histools/RouteBrowser", false);
                if (prefab == null)
                {
                    Utils.Logger.Error("RouteBrowser prefab not found");
                    return;
                }

                var instance = Object.Instantiate(prefab);
                instance.SetActive(true);
                var canvasGroup = instance.gameObject.GetComponent<CanvasGroup>();
                canvasGroup.gameObject.AddComponent<EscCloseCanvasGroup>().group = canvasGroup;
            });
        }
    }
}