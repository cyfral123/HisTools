using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

public class RouteStateHandler : MonoBehaviour
{
    public string Uid;
    public float tweenDuration = 0.3f;
    public float scaleFactor = 1.2f;
    public float maxInteractionDistance = 100f;
    public float boundsExpansion = 0.7f;
    public Color hiddenColor = Palette.HtmlWithForceAlpha(Plugin.RouteLabelDisabledColorHtml.Value, Plugin.RouteLabelDisabledOpacityHtml.Value / 100.0f);
    public Color shownColor = Palette.HtmlWithForceAlpha(Plugin.RouteLabelEnabledColorHtml.Value, Plugin.RouteLabelEnabledOpacityHtml.Value / 100.0f);

    private TextMeshPro tmp;
    private Transform cam;
    private Vector3 baseScale;
    private Tween currentTween;
    private bool show = true;

    private void Awake()
    {
        tmp = GetComponentInChildren<TextMeshPro>();
        cam = Camera.main.transform;

        baseScale = tmp.transform.localScale;
        EventBus.Subscribe<ToggleRouteEvent>(OnToggleRouteEvent);
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(2))
            return;


        Ray ray = new Ray(cam.position, cam.forward);

        Bounds bounds = tmp.GetComponent<Renderer>().bounds;
        bounds.Expand(boundsExpansion);

        if (bounds.IntersectRay(ray, out float distance))
        {
            if (distance <= maxInteractionDistance)
                HandleClick();
        }
    }

    private bool IsRouteActive()
    {
        return RoutePlayer.ActiveRoutes
            .First(x => x.Value.Info.uid == Uid).Value.Root.activeSelf;
    }

    private void HandleClick()
    {
        bool isActive = IsRouteActive();

        if (isActive)
        {
            EventBus.Publish(new ToggleRouteEvent(Uid, false));
        }
        else
        {
            EventBus.Publish(new ToggleRouteEvent(Uid, true));
        }


        PlayTween();
    }

    private void OnToggleRouteEvent(ToggleRouteEvent e)
    {
        if (e.Uid != Uid)
            return;
        show = e.Show;
        Utils.Files.SaveRouteStateToConfig(e.Uid, e.Show);
        Utils.Logger.Debug($"Route state saved: active={e.Show}, uid={e.Uid}");
        PlayTween();
    }

    private void PlayTween()
    {
        if (this == null || transform == null)
        {
            return;
        }

        currentTween?.Kill();
        currentTween = DOTween.Sequence()
            .Join(tmp.DOColor(show ? shownColor : hiddenColor, tweenDuration))
            .Join(transform.DOScale(baseScale * scaleFactor, tweenDuration).SetEase(Ease.OutBack))
            .Append(transform.DOScale(baseScale, tweenDuration).SetEase(Ease.InOutSine));
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<ToggleRouteEvent>(OnToggleRouteEvent);
    }
}