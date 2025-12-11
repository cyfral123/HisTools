using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HisTools.Features.Controllers;
using HisTools.Prefabs;
using HisTools.Utils;
using HisTools.Utils.RouteFeature;
using TMPro;
using UnityEngine;
using CoroutineRunner = HisTools.Utils.CoroutineRunner;

namespace HisTools.Features;

public class RoutePlayer : FeatureBase
{
    public static readonly Dictionary<string, RouteInstance> ActiveRoutes = [];
    private readonly HashSet<GameObject> _activatedMarkers = [];

    private Transform _infoLabelsContainer;
    private Transform _playerTransform;

    private GameObject _routeNameLabelPrefab;
    private GameObject _routeDescriptionLabelPrefab;
    private TextMeshPro _notePrefab;
    private LineRenderer _linePrefab;
    private GameObject _markerPrefab;
    private readonly Material _defaultMaterial;
    private bool _isLoading;

    public RoutePlayer() : base("RoutePlayer", "Show recorded routes for levels")
    {
        _defaultMaterial = new Material(Shader.Find("Sprites/Default"));

        AddSettings();
    }

    private void AddSettings()
    {
        AddSetting(new FloatSliderSetting(this, "Path progress threshold",
            "Distance ahead along the path to consider as progress",
            70f, 30f, 200f, 1f, 0));
        AddSetting(new FloatSliderSetting(this, "JumpMarkers trigger distance",
            "Distance to trigger markers",
            7f, 0f, 10f, 0.1f, 1));
        AddSetting(new FloatSliderSetting(this, "JumpMarkers size",
            "Size of markers",
            0.25f, 0f, 0.8f, 0.05f, 2));
        AddSetting(new FloatSliderSetting(this, "Fade distance",
            "Distance to pathline to start fading",
            8f, 0f, 20f, 1f, 0));
        AddSetting(new FloatSliderSetting(this, "Default opacity",
            "Opacity of path by default",
            0.4f, 0f, 1f, 0.01f, 2));
        AddSetting(new FloatSliderSetting(this, "Faded opacity",
            "Opacity of path when faded",
            0.2f, 0f, 1f, 0.01f, 2));
        AddSetting(new FloatSliderSetting(this, "Line quality",
            "Mesh smoothing quality",
            8f, 5f, 30f, 1f, 0));

        AddSetting(new BoolSetting(this, "Show route names", "Display route names", true));
        AddSetting(new BoolSetting(this, "Show route authors", "Display route authors", true));
        AddSetting(new BoolSetting(this, "Show route descriptions", "Display route descriptions", true));
        AddSetting(new BoolSetting(this, "Use route preferred colors", "Use preferred route colors", true));

        AddSetting(new ColorSetting(this, "Completed color", "Color of completed route",
            Palette.FromHtml(Plugin.BackgroundHtml.Value)));
        AddSetting(new ColorSetting(this, "Remaining color", "Color of remaining route",
            Palette.FromHtml(Plugin.AccentHtml.Value)));
        AddSetting(new ColorSetting(this, "Text color", "Color of text labels",
            Palette.FromHtml(Plugin.EnabledHtml.Value)));
    }

    private void EnsurePrefabs()
    {
        if (_routeNameLabelPrefab && _routeDescriptionLabelPrefab && _notePrefab && _linePrefab &&
            _infoLabelsContainer) return;
        Utils.Logger.Debug("Some prefabs are missing, creating them");
        CreatePrefabsIfNeeded();
    }

    private void EnsurePlayer()
    {
        if (_playerTransform) return;

        if (Player.GetTransform().TryGet(out var value))
        {
            _playerTransform = value;
        }
    }

    private void CreatePrefabsIfNeeded()
    {
        if (!_playerTransform)
            return;

        if (!_infoLabelsContainer)
            _infoLabelsContainer = new GameObject("HisTools_InfoLabelsContainer").transform;

        if (!_routeNameLabelPrefab)
        {
            if (PrefabDatabase.Instance.GetObject("histools/InfoLabel", false)
                .TryGet(out var prefab))
            {
                var go = Object.Instantiate(prefab);
                var tmp = go.GetComponent<TextMeshPro>();
                tmp.color = Palette.HtmlWithForceAlpha(Plugin.RouteLabelEnabledColorHtml.Value,
                    Plugin.RouteLabelEnabledOpacityHtml.Value / 100.0f);
                var look = go.AddComponent<LookAtPlayer>();
                look.player = _playerTransform;
                _routeNameLabelPrefab = go;
            }
        }

        if (!_routeDescriptionLabelPrefab)
        {
            if (PrefabDatabase.Instance.GetObject("histools/InfoLabel", false)
                .TryGet(out var prefab))
            {
                var go = Object.Instantiate(prefab);
                var tmp = go.GetComponent<TextMeshPro>();
                tmp.color = Palette.FromHtml(Plugin.BackgroundHtml.Value);
                var look = go.AddComponent<LookAtPlayer>();
                look.player = _playerTransform;
                go.AddComponent<LabelLookAnimation>();

                _routeDescriptionLabelPrefab = go;
            }
        }

        if (!_markerPrefab)
        {
            if (PrefabDatabase.Instance.GetObject("histools/SphereMarker", false)
                .TryGet(out var prefab))
            {
                var go = Object.Instantiate(prefab);
                go.AddComponent<MarkerActivator>();
                go.GetComponent<Renderer>().material.color = GetSetting<ColorSetting>("Remaining color").Value;
                _markerPrefab = go;
            }
        }

        if (!_notePrefab)
        {
            if (PrefabDatabase.Instance.GetObject("histools/InfoLabel", false)
                .TryGet(out var prefab))
            {
                var go = Object.Instantiate(prefab);
                var tmp = go.GetComponent<TextMeshPro>();
                tmp.fontSize = 3;
                tmp.color = GetSetting<ColorSetting>("Text color").Value;
                var look = go.AddComponent<LookAtPlayer>();
                look.player = _playerTransform;
                _notePrefab = tmp;
            }
        }

        if (!_linePrefab)
        {
            var go = new GameObject("HisTools_Line_Prefab");
            go.SetActive(false);

            _linePrefab = go.AddComponent<LineRenderer>();
            _linePrefab.startWidth = 0.1f;
            _linePrefab.endWidth = 0.1f;
            _linePrefab.material = _defaultMaterial;
            _linePrefab.startColor = GetSetting<ColorSetting>("Remaining color").Value;
            _linePrefab.endColor = GetSetting<ColorSetting>("Remaining color").Value;
        }
    }

    public override void OnEnable()
    {
        var level = CL_EventManager.currentLevel;
        if (level)
            DrawRoutes(level);


        EventBus.Subscribe<ToggleRouteEvent>(OnToggleRoute);
        EventBus.Subscribe<WorldUpdateEvent>(OnWorldUpdate);
        EventBus.Subscribe<EnterLevelEvent>(OnEnterLevel);
    }

    public override void OnDisable()
    {
        EventBus.Unsubscribe<ToggleRouteEvent>(OnToggleRoute);
        EventBus.Unsubscribe<WorldUpdateEvent>(OnWorldUpdate);
        EventBus.Unsubscribe<EnterLevelEvent>(OnEnterLevel);

        ClearRoutes();
    }

    private void ClearRoutes()
    {
        foreach (var kvp in ActiveRoutes.Where(kvp => kvp.Value.Root))
        {
            Object.Destroy(kvp.Value.Root);
        }

        if (_infoLabelsContainer)
        {
            foreach (Transform child in _infoLabelsContainer)
            {
                Object.Destroy(child.gameObject);
            }
        }

        _infoLabelsContainer = null;

        ActiveRoutes.Clear();
    }


    private void OnEnterLevel(EnterLevelEvent e)
    {
        if (!e.Level) return;

        DrawRoutes(e.Level);
    }

    private void DrawRoutes(M_Level level)
    {
        ClearRoutes();
        EnsurePlayer();
        EnsurePrefabs();

        CoroutineRunner.Instance.StartCoroutine(ProcessRoutes(level));
    }

    private void CreateGuide(string title, string description, Vector3 position)
    {
        var absolutePos = Vectors.ConvertPointToAbsolute(position);

        var titleObj = Object.Instantiate(_routeNameLabelPrefab, absolutePos,
            Quaternion.identity,
            _infoLabelsContainer);
        var tmp = titleObj.GetComponent<TextMeshPro>();
        tmp.text = title;
        tmp.color = Color.cyan;
        titleObj.SetActive(true);

        var descObj = Object.Instantiate(_routeDescriptionLabelPrefab, absolutePos,
            Quaternion.identity,
            _infoLabelsContainer);
        tmp = descObj.GetComponent<TextMeshPro>();
        tmp.text = description;
        descObj.SetActive(true);

        tmp.ForceMeshUpdate();
        var lines = tmp.textInfo.lineCount;
        if (lines < 1) lines = 1;

        descObj.transform.position -= Vector3.up * (0.15f * lines);
    }

    private void ShowIntroGuides()
    {
        CreateGuide("RoutePlayer Guide",
            "Here you can find various\nguides on how to use RoutePlayer",
            Vector3.up * 1.75f);

        CreateGuide("Activate/Deactivate",
            "You can easily activate/deactivate\nroutes by pressing middle mouse button",
            new Vector3(3.96f, 0.98f * 1.75f, 2.98f));

        var rawRouteDirPath = Path.Combine(Constants.Paths.RoutesPathDir);
        var half = rawRouteDirPath.Length / 2;
        var left = rawRouteDirPath[..half];
        var right = rawRouteDirPath[half..];
        CreateGuide("Where are the routes stored, full path:",
            $"{left}\n{right}",
            new Vector3(-3.96f, 0.98f * 1.75f, 2.98f));
    }

    private IEnumerator ProcessRoutes(M_Level level)
    {
        if (level.levelName == "M1_Intro_01")
        {
            Utils.Logger.Warn("RoutePlayer: Intro level, skipping");
            ShowIntroGuides();
                
            yield break;
        }

        List<string> filePaths = null;
        yield return CoroutineRunner.Instance.StartCoroutine(
            Files.GetRouteFilesByTargetLevel(level.levelName, callback => filePaths = callback));

        if (filePaths == null)
        {
            Utils.Logger.Warn("RoutePlayer: No route files found");
            yield break;
        }

        _isLoading = true;
        foreach (var routeData in filePaths.Select(RouteLoader.LoadRoutes))
        {
            CoroutineRunner.Instance.StartCoroutine(BuildRoute(routeData));
            yield return new WaitForEndOfFrame();
        }

        _isLoading = false;
    }

    private IEnumerator BuildRoute(RouteSet routeData)
    {
        if (routeData == null || routeData.points.Count == 0)
            yield break;

        var routeRoot = new GameObject($"Route_{routeData.info.uid}_{routeData.info.name}");

        var instance = new RouteInstance
        {
            Info = routeData.info,
            Root = routeRoot
        };

        // 1) Convert local points to absolute positions
        var absolutePoints = new List<Vector3>(routeData.points.Count);
        absolutePoints.AddRange(routeData.points.Select(Vectors.ConvertPointToAbsolute));

        yield return new WaitForEndOfFrame();

        // 2) SmoothPath
        absolutePoints = SmoothUtil.Path(absolutePoints, GetSetting<FloatSliderSetting>("Line quality").Value);

        yield return new WaitForEndOfFrame();

        // 3) LineRenderer
        var lineRenderer = CreateLine(absolutePoints, routeRoot.transform);
        instance.Line = lineRenderer;

        yield return new WaitForEndOfFrame();

        // 4) Info labels
        var showRouteNames = GetSetting<BoolSetting>("Show route names").Value;
        var showRouteAuthors = GetSetting<BoolSetting>("Show route authors").Value;
        var showRouteDescriptions = GetSetting<BoolSetting>("Show route descriptions").Value;

        yield return new WaitForEndOfFrame();

        if (instance.Info != null)
        {
            var routeEntryPoint = absolutePoints[0];

            var nameAuthorText = instance.Info.name;
            if (showRouteAuthors && !string.IsNullOrEmpty(instance.Info.author))
                nameAuthorText += $" (by {instance.Info.author})";

            var descriptionText = instance.Info.description;

            if (showRouteNames)
            {
                var routeNameAuthor = Object.Instantiate(_routeNameLabelPrefab, routeEntryPoint, Quaternion.identity,
                    _infoLabelsContainer);
                routeNameAuthor.SetActive(true);


                routeNameAuthor.AddComponent<RouteStateHandler>().Uid = instance.Info.uid;
                var tmp = routeNameAuthor.GetComponent<TextMeshPro>();
                tmp.text = nameAuthorText;
                instance.InfoLabels.Add(routeNameAuthor);
            }

            yield return new WaitForEndOfFrame();

            if (showRouteDescriptions && !string.IsNullOrEmpty(instance.Info.description))
            {
                var routeDescription = Object.Instantiate(_routeDescriptionLabelPrefab, routeEntryPoint,
                    Quaternion.identity,
                    _infoLabelsContainer);
                routeDescription.SetActive(true);

                var tmp = routeDescription.GetComponent<TextMeshPro>();
                tmp.text = descriptionText;

                tmp.ForceMeshUpdate();
                var lines = tmp.textInfo.lineCount;
                if (lines < 1) lines = 1;

                routeDescription.transform.position -= Vector3.up * (0.15f * lines);
                instance.InfoLabels.Add(routeDescription);
            }
        }

        yield return new WaitForEndOfFrame();

        // 5) Notes
        foreach (var note in routeData.notes)
        {
            var localPoint = note.Position;
            var absolutePos = Vectors.ConvertPointToAbsolute(localPoint);

            var noteLabel = Object.Instantiate(_notePrefab, absolutePos, Quaternion.identity,
                routeRoot.transform);
            noteLabel.text = note.note;
            noteLabel.gameObject.SetActive(true);
            instance.NoteLabels.Add(noteLabel.gameObject);
        }

        yield return new WaitForEndOfFrame();

        // 6) Jump markers
        var markerSize = GetSetting<FloatSliderSetting>("JumpMarkers size").Value;

        foreach (var index in routeData.jumpIndices)
        {
            if (index < 0 || index >= routeData.points.Count)
                continue;

            var localPoint = routeData.points[index];
            var absolutePos = Vectors.ConvertPointToAbsolute(localPoint);

            var jumpMarker = Object.Instantiate(_markerPrefab, absolutePos, Quaternion.identity, routeRoot.transform);

            jumpMarker.transform.localScale = Vector3.one * markerSize;
            jumpMarker.SetActive(true);

            instance.JumpMarkers.Add(jumpMarker);
        }

        ActiveRoutes[instance.Info.uid] = instance;

        var savedState = Files.GetRouteStateFromConfig(instance.Info.uid);
        var routeState = savedState.GetValueOrDefault(true);
        Utils.Logger.Debug($"Restored route '{instance.Info.uid}' state: active={routeState}");
        EventBus.Publish(new ToggleRouteEvent(instance.Info.uid, routeState));


        Utils.Logger.Info(
            $"Loaded route {instance.Info.name}: ({routeData.points.Count} points), ({instance.JumpMarkers.Count} jumps), ({instance.NoteLabels.Count} notes), uid: {instance.Info.uid}"
        );
    }

    private LineRenderer CreateLine(IReadOnlyList<Vector3> absolutePoints, Transform parent, float startWidth = 0.1f,
        float endWidth = 0.1f, Material material = null)
    {
        if (!_linePrefab)
        {
            Utils.Logger.Error("CreateLine: _linePrefab is null");
            return null;
        }

        var renderer = Object.Instantiate(_linePrefab, parent);

        renderer.positionCount = absolutePoints.Count;

        if (absolutePoints is Vector3[] arr)
            renderer.SetPositions(arr);
        else
            renderer.SetPositions(absolutePoints.ToArray());

        renderer.startWidth = startWidth;
        renderer.endWidth = endWidth;

        if (material)
            renderer.material = material;


        renderer.gameObject.SetActive(true);

        return renderer;
    }

    private void OnWorldUpdate(WorldUpdateEvent e)
    {
        if (ActiveRoutes.Count == 0 || _playerTransform == null || _isLoading)
            return;

        var remainingColor = GetSetting<ColorSetting>("Remaining color").Value;
        var completedColor = GetSetting<ColorSetting>("Completed color").Value;
        var textColor = GetSetting<ColorSetting>("Text color").Value;
        var useRoutePreferredColors = GetSetting<BoolSetting>("Use route preferred colors").Value;
        var progressThreshold = GetSetting<FloatSliderSetting>("Path progress threshold").Value;
        var fadedAlpha = GetSetting<FloatSliderSetting>("Faded opacity").Value;
        var fadeDistance = GetSetting<FloatSliderSetting>("Fade distance").Value;
        var defaultAlpha = GetSetting<FloatSliderSetting>("Default opacity").Value;
        var triggerDistance = GetSetting<FloatSliderSetting>("JumpMarkers trigger distance").Value;

        foreach (var route in ActiveRoutes.Values)
        {
            if (!route.Line)
                continue;

            var count = route.Line.positionCount;

            if (route.CachedPositions == null || route.CachedPositions.Length != count)
            {
                route.CachedPositions = new Vector3[count];
                route.Line.GetPositions(route.CachedPositions);
                route.LastClosestIndex = 0;
            }

            var positions = route.CachedPositions;
            var playerPos = _playerTransform.position;

            var closest = route.LastClosestIndex;
            var bestSq = float.MaxValue;

            const int window = 60;
            var start = Mathf.Max(0, closest - window);
            var end = Mathf.Min(count - 1, closest + window);

            for (var i = start; i <= end; i++)
            {
                var distSq = (positions[i] - playerPos).sqrMagnitude;
                if (distSq < bestSq)
                {
                    bestSq = distSq;
                    closest = i;
                }
            }

            route.LastClosestIndex = closest;

            var minDist = Mathf.Sqrt(bestSq);

            var alpha = Mathf.Lerp(fadedAlpha, defaultAlpha, minDist / fadeDistance);
            alpha = Mathf.Clamp(alpha, fadedAlpha, defaultAlpha);

            var lookAheadFactor = Mathf.Clamp01(count / 100f);
            var adaptiveLookAhead = Mathf.Lerp(progressThreshold * 0.3f, progressThreshold, lookAheadFactor);

            var lookIndex = Mathf.Min(
                closest + Mathf.RoundToInt(adaptiveLookAhead),
                count - 1
            );

            // progress
            var progress = (float)lookIndex / (count - 1);

            if (progress > route.MaxProgress)
            {
                route.MaxProgress = progress;

                route.CachedGradient ??= new Gradient();

                const float width = 0.15f;
                var t0 = Mathf.Clamp01(progress - width / 2f);
                var t1 = Mathf.Clamp01(progress + width / 2f);

                var completed = route.Info != null && useRoutePreferredColors &&
                                route.Info.CompletedColor != Color.clear
                    ? route.Info.CompletedColor
                    : completedColor;

                var remaining = route.Info != null && useRoutePreferredColors &&
                                route.Info.RemainingColor != Color.clear
                    ? route.Info.RemainingColor
                    : remainingColor;

                var colorKeys = new GradientColorKey[]
                {
                    new(completed, 0f),
                    new(completed, t0),
                    new(Color.Lerp(completed, remaining, 0.5f), progress),
                    new(remaining, t1),
                    new(remaining, 1f)
                };

                var alphaKeys = new GradientAlphaKey[]
                {
                    new(1f, 0f),
                    new(1f, 1f)
                };

                var matCol = route.Line.material.color;
                matCol.a = alpha;
                route.Line.material.color = matCol;

                route.CachedGradient.SetKeys(colorKeys, alphaKeys);
                route.Line.colorGradient = route.CachedGradient;
            }

            // markers
            foreach (var marker in route.JumpMarkers)
            {
                if (!marker) continue;

                var distSq = (marker.transform.position - playerPos).sqrMagnitude;

                var renderer = marker.GetComponent<Renderer>();
                if (!renderer) continue;

                var completed = _activatedMarkers.Contains(marker);

                var col = completed ? completedColor : remainingColor;
                col.a = alpha;
                renderer.material.color = col;

                if (!completed && distSq < triggerDistance * triggerDistance)
                {
                    _activatedMarkers.Add(marker);
                    marker.GetComponent<MarkerActivator>().ActivateMarker(completedColor);
                }
            }

            // notes
            var routeTextColor =
                (route.Info != null && useRoutePreferredColors && route.Info.TextColor != Color.clear)
                    ? route.Info.TextColor
                    : textColor;

            foreach (var note in route.NoteLabels)
            {
                if (!note) continue;

                var tmp = note.GetComponent<TextMeshPro>();
                tmp.color = new Color(routeTextColor.r, routeTextColor.g, routeTextColor.b, alpha);
            }
        }
    }

    private static void OnToggleRoute(ToggleRouteEvent e)
    {
        if (e.Show)
        {
            if (ActiveRoutes.TryGetValue(e.Uid, out var route))
            {
                route.Root.SetActive(true);
                Utils.Logger.Debug($"Activated route: {e.Uid}");
            }
            else
            {
                Utils.Logger.Warn($"Tried to activate route '{e.Uid}', but it was not loaded");
            }
        }
        else
        {
            if (ActiveRoutes.TryGetValue(e.Uid, out var route))
            {
                route.Root.SetActive(false);
                Utils.Logger.Debug($"Deactivated route: {e.Uid}");
            }
            else
            {
                Utils.Logger.Warn($"Tried to deactivate route '{e.Uid}', but it was not loaded");
            }
        }
    }
}