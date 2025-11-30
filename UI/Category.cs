using HisTools.Features.Controllers;
using HisTools.UI.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace HisTools.UI;

public class MyCategory : ICategory
{
    public string Name { get; }
    public Transform LayoutTransform => LayoutGO.transform;

    private VerticalLayoutGroup Layout { get; }
    private GameObject LayoutGO => Layout.gameObject;

    public MyCategory(GameObject parent, string name, Vector2 initialPosition)
    {
        Name = name;

        var categoryGO = new GameObject(name);
        categoryGO.transform.SetParent(parent.transform, false);
        categoryGO.AddComponent<LayoutElement>();

        var rectCategory = categoryGO.GetComponent<RectTransform>();
        rectCategory.anchorMin = new Vector2(0.5f, 0.5f);
        rectCategory.anchorMax = new Vector2(0.5f, 0.5f);
        rectCategory.pivot = new Vector2(0.5f, 1f);
        rectCategory.sizeDelta = new Vector2(200, 0);
        rectCategory.anchoredPosition = initialPosition;

        var rootGroup = categoryGO.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;

        // header
        var headerGO = new GameObject($"HisTools_Header_{name}");
        headerGO.transform.SetParent(categoryGO.transform, false);

        var rect = headerGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(0, 25);

        var image = headerGO.AddComponent<Image>();
        image.color = Utils.Palette.HtmlColorLight(Plugin.AccentHtml.Value, 1.1f);
        image.raycastTarget = true;

        headerGO.transform.AddMyText(name, TMPro.TextAlignmentOptions.Center, 16f, Color.white);

        // buttons container
        var buttonsContainerGO = new GameObject("HisTools_buttonsContainer");
        buttonsContainerGO.transform.SetParent(categoryGO.transform, false);
        var rectContainer = buttonsContainerGO.AddComponent<RectTransform>();
        rectContainer.anchorMin = Vector2.zero;
        rectContainer.anchorMax = Vector2.one;
        rectContainer.offsetMin = Vector2.zero;
        rectContainer.offsetMax = Vector2.zero;
        rectContainer.pivot = new Vector2(0.5f, 0.5f);

        Layout = buttonsContainerGO.AddComponent<VerticalLayoutGroup>();
        Layout.padding = new RectOffset(2, 2, 2, 2);
        Layout.childAlignment = TextAnchor.UpperCenter;
        Layout.childControlHeight = true;
        Layout.childForceExpandHeight = false;
        Layout.childControlWidth = true;
        Layout.childForceExpandWidth = true;

        var buttonsCanvasGroup = buttonsContainerGO.AddComponent<CanvasGroup>();
        buttonsCanvasGroup.alpha = 1f;

        var controller = headerGO.AddComponent<CategoryController>();
        controller.categoryRect = rectCategory;
        controller.buttonsContainer = buttonsContainerGO;
        controller.layoutGroup = Layout;
        controller.buttonsCanvasGroup = buttonsCanvasGroup;
    }
}
