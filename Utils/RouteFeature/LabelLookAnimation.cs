using UnityEngine;
using TMPro;
using DG.Tweening;

public class LabelLookAnimation : MonoBehaviour
{
    public float maxDistance = 3f;
    public float tweenDuration = 0.3f;
    public float scaleFactor = 1.6f;
    public float boundsExpansion = 0.5f;
    public Color targetColor = Color.white;

    public Collider sharedCollider;

    private TextMeshPro tmp;
    private Transform cam;
    private Color baseColor;
    private Vector3 baseScale;
    private Tween currentTween;
    private bool isActive;
    private Shader shader = Shader.Find("Unlit/Color");
    private GameObject backgroundGO;

    private void Awake()
    {
        tmp = GetComponentInChildren<TextMeshPro>();
        cam = Camera.main.transform;

        baseColor = tmp.color;
        baseScale = tmp.transform.localScale;

        CreateBackground();
        backgroundGO.SetActive(false);
    }

    private void CreateBackground()
    {
        backgroundGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backgroundGO.name = "TextBackground";
        backgroundGO.transform.SetParent(tmp.transform, false);
        backgroundGO.transform.localPosition = new Vector3(0, 0, 0.01f);

        var mat = new Material(shader);
        mat.color = Utils.Palette.FromHtml(Plugin.BackgroundHtml.Value);
        backgroundGO.GetComponent<MeshRenderer>().material = mat;

        GameObject.Destroy(backgroundGO.GetComponent<Collider>());

        UpdateBackgroundSize();
    }

    private void UpdateBackgroundSize()
    {
        var bounds = tmp.bounds;
        backgroundGO.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);
    }

    private void Update()
    {
        if (cam == null || tmp == null)
            return;

        bool hitThis = false;
        Ray ray = new Ray(cam.position, cam.forward);

        Bounds bounds = tmp.GetComponent<Renderer>().bounds;
        bounds.Expand(boundsExpansion);

        if (bounds.IntersectRay(ray, out float distance))
        {
            if (distance <= maxDistance)
                hitThis = true;
        }

        if (hitThis && !isActive)
            Activate();
        else if (!hitThis && isActive)
            Deactivate();
    }

    private void Activate()
    {
        if (this == null || transform == null)
        {
            return;
        }

        isActive = true;
        currentTween?.Kill();

        backgroundGO.SetActive(true);

        currentTween = DOTween.Sequence()
            .Join(tmp.DOColor(targetColor, tweenDuration))
            .Join(tmp.transform.DOScale(baseScale * scaleFactor, tweenDuration).SetEase(Ease.OutBack));
    }

    private void Deactivate()
    {
        if (this == null || transform == null)
        {
            return;
        }

        isActive = false;
        currentTween?.Kill();

        backgroundGO.SetActive(false);

        currentTween = DOTween.Sequence()
            .Join(tmp.DOColor(baseColor, tweenDuration))
            .Join(tmp.transform.DOScale(baseScale, tweenDuration).SetEase(Ease.InOutSine));
    }
}
