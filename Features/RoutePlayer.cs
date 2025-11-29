using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Utils;
using Unity.VisualScripting;
using HisTools.Routes;
using System.Collections;
using System.Linq;

public class RoutePlayer : FeatureBase
{
    public static readonly Dictionary<string, RouteInstance> ActiveRoutes = [];
    private readonly HashSet<GameObject> _activatedMarkers = [];

    private Transform _infoLabelsContainer;
    private Transform _playerTransform;

    private GameObject _markerPrefab;
    private GameObject _infoLabelPrefab;
    private GameObject _notePrefab;
    private GameObject _linePrefab;

    private readonly Material _defaultMaterial;
    private bool _isLoading = false;

    public RoutePlayer() : base("RoutePlayer", "Show recorded routes for levels")
    {
        _defaultMaterial = new Material(Shader.Find("Sprites/Default"));

        AddSettings();
    }

    private void AddSettings()
    {
        AddSetting(new FloatSliderSetting(this, "Path progress threshold", "Distance ahead along the path to consider as progress", 70f, 30f, 200f, 1f, 0));
        AddSetting(new FloatSliderSetting(this, "JumpMarkers trigger distance", "Distance to trigger markers", 7f, 0f, 10f, 0.1f, 1));
        AddSetting(new FloatSliderSetting(this, "JumpMarkers size", "Size of markers", 0.15f, 0f, 0.8f, 0.05f, 2));
        AddSetting(new FloatSliderSetting(this, "Fade distance", "Distance to pathline to start fading", 8f, 0f, 20f, 1f, 0));
        AddSetting(new FloatSliderSetting(this, "Default opacity", "Opacity of path by default", 0.4f, 0f, 1f, 0.01f, 2));
        AddSetting(new FloatSliderSetting(this, "Faded opacity", "Opacity of path when faded", 0.2f, 0f, 1f, 0.01f, 2));
        AddSetting(new FloatSliderSetting(this, "Line quality", "Mesh smoothing quality", 8f, 5f, 30f, 1f, 0));

        AddSetting(new BoolSetting(this, "Show route names", "Display route names", true));
        AddSetting(new BoolSetting(this, "Show route authors", "Display route authors", true));
        AddSetting(new BoolSetting(this, "Show route descriptions", "Display route descriptions", true));
        AddSetting(new BoolSetting(this, "Use route preferred colors", "Use preferred route colors", true));

        AddSetting(new ColorSetting(this, "Completed color", "Color of completed route", Palette.FromHtml(Plugin.BackgroundHtml.Value)));
        AddSetting(new ColorSetting(this, "Remaining color", "Color of remaining route", Palette.FromHtml(Plugin.AccentHtml.Value)));
        AddSetting(new ColorSetting(this, "Text color", "Color of text labels", Palette.FromHtml(Plugin.EnabledHtml.Value)));
    }

    private void EnsurePrefabs()
    {
        if (_markerPrefab == null || _infoLabelPrefab == null || _notePrefab == null || _linePrefab == null || _infoLabelsContainer == null)
        {
            Utils.Logger.Debug("Some prefabs are missing, creating them");
            CreatePrefabsIfNeeded();
        }
        else return;
    }

    private void EnsurePlayer()
    {
        if (_playerTransform == null)
        {
            var playerObj = GameObject.Find("CL_Player");
            if (playerObj == null)
            {
                Utils.Logger.Error("RoutePlayer: Player object not found");
            }

            _playerTransform = playerObj.transform;
        }
    }

    private void CreatePrefabsIfNeeded()
    {
        if (_playerTransform == null)
            return;

        if (_infoLabelsContainer == null)
            _infoLabelsContainer = new GameObject("HisTools_InfoLabelsContainer").transform;

        if (_markerPrefab == null)
        {
            _markerPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(_markerPrefab.GetComponent<BoxCollider>());
            _markerPrefab.transform.rotation = Quaternion.Euler(45, 45, 0);
            _markerPrefab.GetComponent<Renderer>().material = _defaultMaterial;
            _markerPrefab.AddComponent<MarkerActivator>();
            _markerPrefab.SetActive(false);
        }

        if (_infoLabelPrefab == null)
        {
            _infoLabelPrefab = new GameObject($"HisTools_InfoLabel_Prefab");
            var tmp = _infoLabelPrefab.AddComponent<TextMeshPro>();
            tmp.text = "InfoLabel";
            tmp.fontSize = 1;
            tmp.color = Palette.HtmlWithForceAlpha(Plugin.RouteLabelEnabledColorHtml.Value, Plugin.RouteLabelEnabledOpacityHtml.Value / 100.0f);
            tmp.alignment = TextAlignmentOptions.Center;
            var look = tmp.AddComponent<LookAtPlayer>();
            look.player = _playerTransform;
            _infoLabelPrefab.SetActive(false);
        }

        if (_notePrefab == null)
        {
            _notePrefab = new GameObject($"HisTools_Note_Prefab");
            var tmp = _notePrefab.AddComponent<TextMeshPro>();
            tmp.text = "YourNote";
            tmp.fontSize = 3;
            tmp.color = GetSetting<ColorSetting>("Text color").Value;
            tmp.alignment = TextAlignmentOptions.Center;
            var look = tmp.AddComponent<LookAtPlayer>();
            look.player = _playerTransform;
            _notePrefab.SetActive(false);
        }

        if (_linePrefab == null)
        {
            _linePrefab = new GameObject("HisTools_Line_Prefab");

            var line = _linePrefab.AddComponent<LineRenderer>();
            line.startWidth = 0.1f;
            line.endWidth = 0.1f;
            line.material = _defaultMaterial;
            _linePrefab.SetActive(false);
        }
    }

    public override void OnEnable()
    {
        var level = CL_EventManager.currentLevel;
        if (level != null)
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

    public void ClearRoutes()
    {
        foreach (var kvp in ActiveRoutes)
        {
            if (kvp.Value.Root != null)
                GameObject.Destroy(kvp.Value.Root);
        }

        if (_infoLabelsContainer != null)
        {
            foreach (Transform child in _infoLabelsContainer)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        _infoLabelsContainer = null;

        ActiveRoutes.Clear();
    }


    public void OnEnterLevel(EnterLevelEvent e)
    {
        if (e.Level == null) return;

        DrawRoutes(e.Level);
    }

    private void DrawRoutes(M_Level level)
    {
        ClearRoutes();
        EnsurePlayer();
        EnsurePrefabs();

        CoroutineRunner.Instance.StartCoroutine(ProcessRoutes(level));
    }

    private IEnumerator ProcessRoutes(M_Level level)
    {

        List<string> filePaths = null;
        yield return CoroutineRunner.Instance.StartCoroutine(Files.GetRouteFilesByTargetLevel(level.levelName, callback => filePaths = callback));

        if (filePaths == null)
        {
            Utils.Logger.Warn("RoutePlayer: No route files found");
            yield break;
        }

        _isLoading = true;
        foreach (var jsonPath in filePaths)
        {
            var routeData = RouteLoader.LoadRoutes(jsonPath);
            CoroutineRunner.Instance.StartCoroutine(BuildRoute(routeData, level.levelName));
            yield return new WaitForEndOfFrame();
        }
        _isLoading = false;
    }

    private IEnumerator BuildRoute(RouteSet routeData, string levelName)
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

        for (int i = 0; i < routeData.points.Count; i++)
        {
            Vector3 localPoint = routeData.points[i];
            Vector3 absolutePos = Vectors.ConvertPointToAbsolute(localPoint);

            absolutePoints.Add(absolutePos);
        }

        // 2) SmoothPath
        absolutePoints = SmoothUtil.Path(absolutePoints, GetSetting<FloatSliderSetting>("Line quality").Value);

        yield return null;

        // 3) LineRenderer
        var line = CreateLine(absolutePoints, routeRoot.transform);
        var lineRenderer = line.GetComponent<LineRenderer>();
        instance.Line = lineRenderer;

        // 4) Info labels
        bool showRouteNames = GetSetting<BoolSetting>("Show route names").Value;
        bool showRouteAuthors = GetSetting<BoolSetting>("Show route authors").Value;
        bool showRouteDescriptions = GetSetting<BoolSetting>("Show route descriptions").Value;

        if (instance.Info != null)
        {
            Vector3 routeEntryPoint = absolutePoints[0];

            string nameAuthorText = instance.Info.name;
            if (showRouteAuthors && !string.IsNullOrEmpty(instance.Info.author))
                nameAuthorText += $" (by {instance.Info.author})";

            string descriptionText = instance.Info.description;

            if (showRouteNames)
            {
                var routeNameAuthor = CreateTextLabel(_infoLabelPrefab, nameAuthorText, Color.clear, routeEntryPoint, _infoLabelsContainer);
                routeNameAuthor.AddComponent<RouteStateHandler>().Uid = instance.Info.uid;
                routeNameAuthor.SetActive(true);
                instance.InfoLabels.Add(routeNameAuthor);
            }

            if (showRouteDescriptions && !string.IsNullOrEmpty(instance.Info.description))
            {
                var color = Palette.FromHtml(Plugin.BackgroundHtml.Value);
                var routeDescription = CreateTextLabel(_infoLabelPrefab, descriptionText, color, routeEntryPoint, _infoLabelsContainer);
                var tmp = routeDescription.GetComponent<TextMeshPro>();

                tmp.ForceMeshUpdate();
                int lines = tmp.textInfo.lineCount;
                if (lines < 1) lines = 1;

                routeDescription.transform.position -= Vector3.up * (0.15f * lines);
                routeDescription.AddComponent<LabelLookAnimation>();
                routeDescription.SetActive(true);
                instance.InfoLabels.Add(routeDescription);
            }
        }

        yield return null;

        // 5) Notes
        Color noteColor = GetSetting<ColorSetting>("Text color").Value;

        foreach (var note in routeData.notes)
        {
            Vector3 localPoint = note.Position;
            Vector3 absolutePos = Vectors.ConvertPointToAbsolute(localPoint);

            var noteLabel = CreateTextLabel(_notePrefab, note.note, noteColor, absolutePos, routeRoot.transform);

            instance.NoteLabels.Add(noteLabel);
        }

        // 6) Jump markers
        float markerSize = GetSetting<FloatSliderSetting>("JumpMarkers size").Value;

        foreach (int index in routeData.jumpIndices)
        {
            if (index < 0 || index >= routeData.points.Count)
                continue;

            Vector3 localPoint = routeData.points[index];
            Vector3 absolutePos = Vectors.ConvertPointToAbsolute(localPoint);

            var jumpMarker = CreateJumpMarker(markerSize, absolutePos, routeRoot.transform);
            instance.JumpMarkers.Add(jumpMarker);
        }

        yield return null;

        ActiveRoutes[instance.Info.uid] = instance;

        var savedState = Files.GetRouteStateFromConfig(instance.Info.uid);
        var routeState = savedState.GetValueOrDefault(true);
        Utils.Logger.Debug($"Restored route '{instance.Info.uid}' state: active={routeState}");
        EventBus.Publish(new ToggleRouteEvent(instance.Info.uid, routeState));


        Utils.Logger.Info(
            $"Loaded route {instance.Info.name}: ({routeData.points.Count} points), ({instance.JumpMarkers.Count} jumps), ({instance.NoteLabels.Count} notes), uid: {instance.Info.uid}"
        );
    }

    private GameObject CreateLine(IReadOnlyList<Vector3> absolutePoints, Transform parent, float startWidth = 0.1f, float endWidth = 0.1f, Material material = null)
    {
        EnsurePrefabs();
        if (_linePrefab == null)
        {
            Utils.Logger.Error("CreateLine: _linePrefab is null");
            return null;
        }

        var line = GameObject.Instantiate(_linePrefab, parent);

        var renderer = line.GetComponent<LineRenderer>();
        renderer.positionCount = absolutePoints.Count;

        if (absolutePoints is Vector3[] arr)
            renderer.SetPositions(arr);
        else
            renderer.SetPositions(absolutePoints.ToArray());

        renderer.startWidth = startWidth;
        renderer.endWidth = endWidth;

        if (material != null)
            renderer.material = material;


        line.SetActive(true);

        return line;
    }

    private GameObject CreateTextLabel(GameObject prefab, string text, Color color, Vector3 position, Transform parent)
    {
        EnsurePrefabs();

        if (prefab == null)
        {
            Utils.Logger.Error("CreateTextLabel: prefab is null");
            return null;
        }

        var label = GameObject.Instantiate(prefab, position, Quaternion.identity, parent);
        var tmp = label.GetComponent<TextMeshPro>();
        tmp.text = text;

        if (color != Color.clear)
            tmp.color = color;

        label.SetActive(true);

        return label;
    }

    private GameObject CreateJumpMarker(float markerSize, Vector3 position, Transform parent)
    {
        EnsurePrefabs();

        var marker = GameObject.Instantiate(_markerPrefab, position, Quaternion.identity, parent);
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * markerSize;

        marker.SetActive(true);

        return marker;
    }

    public void OnWorldUpdate(WorldUpdateEvent e)
    {
        if (ActiveRoutes.Count == 0 || _playerTransform == null || _isLoading)
            return;

        Color remainingColor = GetSetting<ColorSetting>("Remaining color").Value;
        Color completedColor = GetSetting<ColorSetting>("Completed color").Value;
        Color textColor = GetSetting<ColorSetting>("Text color").Value;
        bool useRoutePreferredColors = GetSetting<BoolSetting>("Use route preferred colors").Value;
        float progressThreshold = GetSetting<FloatSliderSetting>("Path progress threshold").Value;
        float fadedAlpha = GetSetting<FloatSliderSetting>("Faded opacity").Value;
        float fadeDistance = GetSetting<FloatSliderSetting>("Fade distance").Value;
        float defaultAlpha = GetSetting<FloatSliderSetting>("Default opacity").Value;
        float triggerDistance = GetSetting<FloatSliderSetting>("JumpMarkers trigger distance").Value;

        foreach (var route in ActiveRoutes.Values)
        {
            if (route.Line == null)
                continue;

            int count = route.Line.positionCount;

            if (route.CachedPositions == null || route.CachedPositions.Length != count)
            {
                route.CachedPositions = new Vector3[count];
                route.Line.GetPositions(route.CachedPositions);
                route.LastClosestIndex = 0;
            }

            var positions = route.CachedPositions;
            Vector3 playerPos = _playerTransform.position;

            int closest = route.LastClosestIndex;
            float bestSq = float.MaxValue;

            int window = 60;
            int start = Mathf.Max(0, closest - window);
            int end = Mathf.Min(count - 1, closest + window);

            for (int i = start; i <= end; i++)
            {
                float distSq = (positions[i] - playerPos).sqrMagnitude;
                if (distSq < bestSq)
                {
                    bestSq = distSq;
                    closest = i;
                }
            }

            route.LastClosestIndex = closest;

            float minDist = Mathf.Sqrt(bestSq);

            float alpha = Mathf.Lerp(fadedAlpha, defaultAlpha, minDist / fadeDistance);
            alpha = Mathf.Clamp(alpha, fadedAlpha, defaultAlpha);

            float baseLookAhead = progressThreshold;
            float lookAheadFactor = Mathf.Clamp01(count / 100f);
            float adaptiveLookAhead = Mathf.Lerp(baseLookAhead * 0.3f, baseLookAhead, lookAheadFactor);

            int lookIndex = Mathf.Min(
                closest + Mathf.RoundToInt(adaptiveLookAhead),
                count - 1
            );

            // progress
            float progress = (float)lookIndex / (count - 1);

            if (progress > route.MaxProgress)
            {
                route.MaxProgress = progress;
                float t = progress;

                if (route.CachedGradient == null)
                    route.CachedGradient = new Gradient();

                float width = 0.15f;
                float t0 = Mathf.Clamp01(t - width / 2f);
                float t1 = Mathf.Clamp01(t + width / 2f);

                Color completed = route.Info != null && useRoutePreferredColors && route.Info.CompletedColor != Color.clear
                    ? route.Info.CompletedColor
                    : completedColor;

                Color remaining = route.Info != null && useRoutePreferredColors && route.Info.RemainingColor != Color.clear
                    ? route.Info.RemainingColor
                    : remainingColor;

                var colorKeys = new GradientColorKey[]
                {
            new(completed, 0f),
            new(completed, t0),
            new(Color.Lerp(completed, remaining, 0.5f), t),
            new(remaining, t1),
            new(remaining, 1f)
                };

                var alphaKeys = new GradientAlphaKey[]
                {
                new(1f, 0f),
                new(1f, 1f)
                };

                Color matCol = route.Line.material.color;
                matCol.a = alpha;
                route.Line.material.color = matCol;

                route.CachedGradient.SetKeys(colorKeys, alphaKeys);
                route.Line.colorGradient = route.CachedGradient;
            }

            // markers
            foreach (var marker in route.JumpMarkers)
            {
                if (marker == null) continue;

                float distSq = (marker.transform.position - playerPos).sqrMagnitude;

                var renderer = marker.GetComponent<Renderer>();
                if (renderer == null) continue;

                bool completed = _activatedMarkers.Contains(marker);

                Color col = completed ? completedColor : remainingColor;
                col.a = alpha;
                renderer.material.color = col;

                if (!completed && distSq < triggerDistance * triggerDistance)
                {
                    _activatedMarkers.Add(marker);
                    marker.GetComponent<MarkerActivator>().ActivateMarker(completedColor);
                }
            }

            // notes
            Color routeTextColor = (route.Info != null && useRoutePreferredColors && route.Info.TextColor != Color.clear)
                ? route.Info.TextColor
                : textColor;

            foreach (var note in route.NoteLabels)
            {
                if (note == null) continue;

                var tmp = note.GetComponent<TextMeshPro>();
                tmp.color = new Color(routeTextColor.r, routeTextColor.g, routeTextColor.b, alpha);
            }
        }

    }

    public void OnToggleRoute(ToggleRouteEvent e)
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
            return;
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
