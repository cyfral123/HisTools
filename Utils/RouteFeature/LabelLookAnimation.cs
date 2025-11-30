using DG.Tweening;
using TMPro;
using UnityEngine;

namespace HisTools.Utils.RouteFeature;

public class LabelLookAnimation : MonoBehaviour
{
    public float maxDistance = 3f;
    public float tweenDuration = 0.3f;
    public float scaleFactor = 1.6f;
    public float boundsExpansion = 0.5f;
    public Color targetColor = Color.white;

    public Collider sharedCollider;

    private TextMeshPro _tmp;
    private Transform _cam;
    private Color _baseColor;
    private Vector3 _baseScale;
    private Tween _currentTween;
    private bool _isActive;
    private readonly Shader _shader = Shader.Find("Unlit/Color");
    private GameObject _backgroundGo;

    private void Awake()
    {
        _tmp = GetComponentInChildren<TextMeshPro>();
        _cam = Camera.main?.transform;

        _baseColor = _tmp.color;
        _baseScale = _tmp.transform.localScale;

        CreateBackground();
        _backgroundGo.SetActive(false);
    }

    private void CreateBackground()
    {
        _backgroundGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _backgroundGo.name = "TextBackground";
        _backgroundGo.transform.SetParent(_tmp.transform, false);
        _backgroundGo.transform.localPosition = new Vector3(0, 0, 0.01f);

        var mat = new Material(_shader)
        {
            color = Palette.FromHtml(Plugin.BackgroundHtml.Value)
        };
        _backgroundGo.GetComponent<MeshRenderer>().material = mat;

        Destroy(_backgroundGo.GetComponent<Collider>());

        UpdateBackgroundSize();
    }

    private void UpdateBackgroundSize()
    {
        var bounds = _tmp.bounds;
        _backgroundGo.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);
    }

    private void Update()
    {
        if (!_cam || !_tmp)
            return;

        var hitThis = false;
        var ray = new Ray(_cam.position, _cam.forward);

        var bounds = _tmp.GetComponent<Renderer>().bounds;
        bounds.Expand(boundsExpansion);

        if (bounds.IntersectRay(ray, out var distance))
        {
            if (distance <= maxDistance)
                hitThis = true;
        }

        switch (hitThis)
        {
            case true when !_isActive:
                Activate();
                break;
            case false when _isActive:
                Deactivate();
                break;
        }
    }

    private void Activate()
    {
        if (!this || !transform)
        {
            return;
        }

        _isActive = true;
        _currentTween?.Kill();

        _backgroundGo.SetActive(true);

        _currentTween = DOTween.Sequence()
            .Join(_tmp.DOColor(targetColor, tweenDuration))
            .Join(_tmp.transform.DOScale(_baseScale * scaleFactor, tweenDuration).SetEase(Ease.OutBack));
    }

    private void Deactivate()
    {
        if (!this || !transform)
        {
            return;
        }

        _isActive = false;
        _currentTween?.Kill();

        _backgroundGo.SetActive(false);

        _currentTween = DOTween.Sequence()
            .Join(_tmp.DOColor(_baseColor, tweenDuration))
            .Join(_tmp.transform.DOScale(_baseScale, tweenDuration).SetEase(Ease.InOutSine));
    }
}