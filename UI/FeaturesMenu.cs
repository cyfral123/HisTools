using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class FeaturesMenu : MonoBehaviour
{
    private static bool _isAnimating = false;
    private const float AnimationCooldown = 0.25f;

    public static GameObject FeaturesMenuGO { get; private set; }
    public static GameObject CategoriesContainerGO { get; private set; }
    public static GameObject SettingsGO { get; private set; }
    public static CanvasGroup CanvasGroup { get; private set; }
    public static MenuAnimator MenuAnimator { get; private set; }
    public static Canvas TooltipCanvas { get; private set; }
    public static Canvas Canvas { get; private set; }
    public static bool IsMenuVisible { get; private set; }

    private class CategoryEntry
    {
        public Vector2 Position;
        public MyCategory Category;
    }

    private static readonly Dictionary<string, CategoryEntry> CategoryEntries = new();
    private static readonly Dictionary<string, string> FeatureCategoryMap = new();

    private void Awake()
    {
        BuildMenuHierarchy();
    }

    private static void BuildMenuHierarchy()
    {
        if (FeaturesMenuGO != null && CanvasGroup != null && MenuAnimator != null)
            return;

        FeaturesMenuGO = new GameObject("HisTools_HisToolsFeaturesMenu");
        DontDestroyOnLoad(FeaturesMenuGO);

        Canvas = FeaturesMenuGO.GetComponent<Canvas>() ?? FeaturesMenuGO.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Canvas.sortingOrder = 9999;

        if (FeaturesMenuGO.GetComponent<CanvasScaler>() == null)
            FeaturesMenuGO.AddComponent<CanvasScaler>();

        if (FeaturesMenuGO.GetComponent<GraphicRaycaster>() == null)
            FeaturesMenuGO.AddComponent<GraphicRaycaster>();

        CanvasGroup = FeaturesMenuGO.GetComponent<CanvasGroup>() ?? FeaturesMenuGO.AddComponent<CanvasGroup>();
        CanvasGroup.alpha = 0f;
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;

        var image = FeaturesMenuGO.GetComponent<Image>() ?? FeaturesMenuGO.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 1f);
        image.raycastTarget = false;

        if (SettingsGO == null)
        {
            SettingsGO = new GameObject("HisTools_SettingsPanelController");
            SettingsGO.AddComponent<SettingsPanelController>();
            SettingsGO.transform.SetParent(FeaturesMenuGO.transform, false);
        }

        if (CategoriesContainerGO == null)
        {
            CategoriesContainerGO = new GameObject("HisTools_CategoriesContainer");
            CategoriesContainerGO.transform.SetParent(FeaturesMenuGO.transform, false);
            var rect = CategoriesContainerGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(0, 0);
            rect.offsetMax = new Vector2(0, 0);
        }

        if (CategoriesContainerGO.GetComponent<CanvasGroup>() == null)
            CategoriesContainerGO.AddComponent<CanvasGroup>();

        var categoriesAnim = CategoriesContainerGO.GetComponent<CategoriesAnimator>() ??
                             CategoriesContainerGO.AddComponent<CategoriesAnimator>();

        MenuAnimator = FeaturesMenuGO.GetComponent<MenuAnimator>() ?? FeaturesMenuGO.AddComponent<MenuAnimator>();
        MenuAnimator.canvasGroup = CanvasGroup;
        MenuAnimator.categoryAnim = categoriesAnim;
    }

    public static void ShowMenu()
    {
        if (IsMenuVisible) return;
        if (MenuAnimator == null) return;

        MenuAnimator.Show();
        IsMenuVisible = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void HideMenu()
    {
        if (!IsMenuVisible) return;
        if (MenuAnimator == null) return;

        MenuAnimator.Hide();
        IsMenuVisible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Camera lock/unlock handling in Patches/PlayerPatch.cs
    // Cursor handling in Patches/InputManagerPatch.cs
    public static void ToggleMenu()
    {
        EnsureHisToolsMenuInitialized();
        if (MenuAnimator == null || _isAnimating) return;

        _isAnimating = true;

        if (IsMenuVisible)
            HideMenu();
        else
            ShowMenu();

        EventBus.Publish(new MenuVisibleChangedEvent(IsMenuVisible));
        CoroutineRunner.Instance.StartCoroutine(ResetAnimating());
    }

    private static IEnumerator ResetAnimating()
    {
        yield return new WaitForSeconds(AnimationCooldown);
        _isAnimating = false;
    }

    public static void EnsureHisToolsMenuInitialized()
    {
        BuildMenuHierarchy();

        RebuildCategoriesAndButtonsIfNeeded();

        if (MenuAnimator != null && MenuAnimator)
            return;

        if (CanvasGroup == null && FeaturesMenuGO != null)
        {
            CanvasGroup = FeaturesMenuGO.GetComponent<CanvasGroup>();
            if (CanvasGroup == null)
            {
                CanvasGroup = FeaturesMenuGO.AddComponent<CanvasGroup>();
                CanvasGroup.alpha = 0f;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }
        }

        if (CategoriesContainerGO == null && FeaturesMenuGO != null)
        {
            CategoriesContainerGO = FeaturesMenuGO.transform.Find("CategoriesContainer")?.gameObject;
        }

        var categoriesAnimator = CategoriesContainerGO != null
            ? CategoriesContainerGO.GetComponent<CategoriesAnimator>()
            : null;

        if (CanvasGroup == null)
        {
            Utils.Logger.Error("FeaturesMenu.EnsureAnimatorInitialized: CanvasGroup missing");
            return;
        }

        MenuAnimator = FeaturesMenuGO.GetComponent<MenuAnimator>();

        if (MenuAnimator == null)
        {
            MenuAnimator = FeaturesMenuGO.AddComponent<MenuAnimator>();
        }

        MenuAnimator.canvasGroup = CanvasGroup;
        MenuAnimator.categoryAnim = categoriesAnimator ?? CategoriesContainerGO?.AddComponent<CategoriesAnimator>();
    }

    private static void RebuildCategoriesAndButtonsIfNeeded()
    {
        if (CategoriesContainerGO == null) return;

        if (CategoriesContainerGO.transform.childCount > 0) return;

        if (CategoryEntries.Count == 0)
        {
            Utils.Logger.Warn("FeaturesMenu: Cannot rebuild categories because no entries recorded");
            return;
        }

        Utils.Logger.Info("FeaturesMenu: Rebuilding categories and buttons");

        foreach (Transform child in CategoriesContainerGO.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        foreach (var kvp in CategoryEntries)
        {
            var category = new MyCategory(CategoriesContainerGO, kvp.Key, kvp.Value.Position);
            kvp.Value.Category = category;
        }

        foreach (var feature in FeatureRegistry.GetAll())
        {
            if (FeatureCategoryMap.TryGetValue(feature.Name, out var categoryName) &&
                CategoryEntries.TryGetValue(categoryName, out var entry) &&
                entry.Category != null)
            {
                feature.SetCategory(entry.Category);
            }
        }

        var buttonFactory = new UIButtonFactory();
        buttonFactory.CreateAllButtons(FeatureRegistry.GetAll());

        try
        {
            RecoverState.FeaturesState(Plugin.FeaturesStateConfigFilePath);
        }
        catch (System.Exception ex)
        {
            Utils.Logger.Error($"FeaturesMenu: Failed to recover feature state after rebuild: {ex.Message}");
        }
    }

    public static MyCategory GetOrCreateCategory(string categoryName, Vector2 initialPosition)
    {
        BuildMenuHierarchy();

        if (CategoryEntries.TryGetValue(categoryName, out var entry))
        {
            if (entry.Category != null && entry.Category.LayoutTransform != null)
            {
                return entry.Category;
            }
        }

        var category = new MyCategory(CategoriesContainerGO, categoryName, initialPosition);
        CategoryEntries[categoryName] = new CategoryEntry
        {
            Position = initialPosition,
            Category = category
        };

        return category;
    }

    public static void AssignFeatureToCategory(IFeature feature, string categoryName, Vector2 initialPosition)
    {
        if (feature == null) return;

        var category = GetOrCreateCategory(categoryName, initialPosition);
        feature.SetCategory(category);
        FeatureCategoryMap[feature.Name] = categoryName;
    }
}