using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HisTools.Features.Controllers;
using HisTools.Prefabs;
using HisTools.Utils;
using HisTools.Utils.RouteFeature;
using LibBSP;
using Newtonsoft.Json;
using Steamworks;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HisTools.Features;

public class RouteRecorder : FeatureBase
{
    private readonly List<Vector3> _points = [];
    private readonly HashSet<int> _jumpIndices = [];
    private readonly List<Note> _notes = [];
    private readonly List<GameObject> _jumpMarkers = [];

    private LineRenderer _lineRenderer;
    private GameObject _markerPrefab;
    private Transform _playerTransform;

    private GameObject _uiGuide;

    private const string JumpButton = "Jump";

    public RouteRecorder() : base("RouteRecorder", "Record route for current level and save to json")
    {
        AddSettings();
    }

    private void AddSettings()
    {
        AddSetting(new FloatSliderSetting(this, "Record quality", "How much points have to be recorded", 3.3f, 0.5f,
            4.0f, 0.1f, 1));
        AddSetting(new FloatSliderSetting(this, "Preview line width", "Size of preview trail from player", 0.15f, 0.05f,
            0.3f, 0.05f, 2));
        AddSetting(new FloatSliderSetting(this, "Preview markers size", "Size of jump points markers", 0.3f, 0.05f,
            0.4f, 0.05f, 2));
        AddSetting(new BoolSetting(this, "Show preview while recording", "...", true));
        AddSetting(new BoolSetting(this, "Auto stop", "Stop recording automatically on level end", true));
        AddSetting(new FloatSliderSetting(this, "Auto stop distance", "Distance to level exit to stop recording", 5.5f,
            1f, 15f, 0.1f, 1));
    }

    public override void OnEnable()
    {
        var player = ENT_Player.GetPlayer();
        if (!player)
            return;

        _playerTransform = player.transform;

        if (!_markerPrefab)
        {
            var marker = PrefabDatabase.Instance.GetObject("histools/SphereMarker", false);
            if (marker)
            {
                _markerPrefab = Object.Instantiate(marker);
                _markerPrefab.AddComponent<MarkerActivator>();
                _markerPrefab.GetComponent<Renderer>().material.color = Color.cyan;
            }
        }

        _points.Clear();
        _jumpIndices.Clear();
        _jumpMarkers.Clear();
        _notes.Clear();

        var lineObj = new GameObject("HisTools_RecordedPath");
        _lineRenderer = lineObj.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 0;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.widthMultiplier =
            GetSetting<FloatSliderSetting>("Preview line width").Value;
        _lineRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.green, 0f),
                new GradientColorKey(Color.red, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        _lineRenderer.colorGradient = gradient;

        EventBus.Subscribe<PlayerLateUpdateEvent>(OnPlayerLateUpdate);

        // UI guide
        var guide = PrefabDatabase.Instance.GetObject("histools/UI_RouteRecorder", true);
        if (guide)
        {
            _uiGuide = Object.Instantiate(guide, _playerTransform, true);
        }
    }

    // private void UploadRoute(string json)
    // {
    //     _ = UploadRouteAsync(json);
    // }
    //
    // private async Task UploadRouteAsync(string json)
    // {
    //     var (ok, ownerToken, error) = await Http.RouteApiClient.UploadRouteAsync(json);
    //     if (ok)
    //     {
    //         Utils.Logger.Info($"RecordPath: Route uploaded successfully, owner token: {ownerToken}");
    //     }
    //     else
    //     {
    //         Utils.Logger.Error($"RecordPath: Failed to upload route: {error}");
    //     }
    // }

    public override void OnDisable()
    {
        if (_points.Count <= 5)
        {
            Utils.Logger.Info("RecordPath: No points recorded");
            Cleanup();
            return;
        }

        var folderPath = Path.Combine(Constants.Paths.ConfigDir, "Routes");
        Directory.CreateDirectory(folderPath);

        var authorName = SteamClient.Name ?? "unknownAuthor";
        var levelName = CL_EventManager.currentLevel?.levelName ?? "unknownLevel";

        var filePath = Files.GetNextFilePath(folderPath, $"route_{levelName}_by_{authorName}", "json");

        var resultFileName = Path.GetFileNameWithoutExtension(filePath);

        var quality = GetSetting<FloatSliderSetting>("Record quality");
        var minDistance = Math.Round(math.remap(quality.Min, quality.Max, quality.Max, quality.Min, quality.Value), 2);

        var routeInfo = new RouteInfo
        {
            uid = Files.GenerateUid(),
            name = $"unnamed_{levelName}",
            author = SteamClient.Name,
            description = $"Recorded {DateTime.Now}\nEdit json:\n..\\BepInEx\\HisTools\\Routes\\{resultFileName}.json",
            targetLevel = levelName
        };

        var routeDto = RouteMapper.ToDto(
            _points,
            _jumpIndices,
            _notes,
            routeInfo,
            (float)minDistance);


        var json = JsonConvert.SerializeObject(
            new[] { routeDto },
            Formatting.Indented);

        try
        {
            File.WriteAllText(filePath, json);
            // UploadRoute(json);
            Utils.Logger.Info($"RecordPath: JSON saved to {filePath}");
        }
        catch (Exception ex)
        {
            Utils.Logger.Error($"RecordPath: Failed to write JSON: {ex.Message}");
        }

        Cleanup();
    }

    private void Cleanup()
    {
        if (_lineRenderer)
        {
            Object.Destroy(_lineRenderer.gameObject);
            _lineRenderer = null;
        }

        foreach (var marker in _jumpMarkers.Where(m => m))
            Object.Destroy(marker);

        _jumpMarkers.Clear();
        _points.Clear();
        _notes.Clear();
        _jumpIndices.Clear();

        if (_uiGuide)
            Object.Destroy(_uiGuide);

        EventBus.Unsubscribe<PlayerLateUpdateEvent>(OnPlayerLateUpdate);
    }


    private IEnumerator DelayedStop()
    {
        yield return null;
        EventBus.Publish(new FeatureToggleEvent(this, false));
    }

    private void OnPlayerLateUpdate(PlayerLateUpdateEvent e)
    {
        var level = CL_EventManager.currentLevel;
        if (!_playerTransform || !level) return;
        var playerPos = level.transform.InverseTransformPoint(_playerTransform.position);
        var distanceToStop = GetSetting<FloatSliderSetting>("Auto stop distance").Value;
        var jumped = InputManager.GetButton(JumpButton).Down;
        var quality = GetSetting<FloatSliderSetting>("Record quality");
        // invert slider, because minimum distance is maximum quality
        var minDistance = Math.Round(math.remap(quality.Min, quality.Max, quality.Max, quality.Min, quality.Value), 2);
        if (_points.Count == 0 || Vector3.Distance(_points.Last(), playerPos) >= minDistance || jumped)
        {
            AddPoint(playerPos, jumped);
        }

        if (Input.GetMouseButtonDown(2))
        {
            if (Camera.main)
            {
                var pos = Raycast.GetLookTarget(Camera.main.transform, 100f);
                const string noteText = "YourNote";
                _notes.Add(new Note(pos, noteText));
                Utils.Logger.Info($"RecordPath: Added note at {pos}: {noteText}");
            }
        }

        if (GetSetting<BoolSetting>("Auto stop").Value)
        {
            if (_playerTransform.position.DistanceTo(level.GetLevelExit().position) < distanceToStop)
                CoroutineRunner.Instance.StartCoroutine(DelayedStop());
        }
    }

    private void AddPoint(Vector3 localPos, bool isJumped)
    {
        var index = _points.Count;
        _points.Add(localPos);

        if (isJumped)
        {
            _jumpIndices.Add(index);
            SpawnJumpMarker(localPos);
        }

        UpdateLineRenderer();
    }

    private void SpawnJumpMarker(Vector3 localPos)
    {
        if (!_markerPrefab)
            return;

        var marker = Object.Instantiate(_markerPrefab);

        var levelTransformOpt = CL_EventManager.currentLevel?.transform;
        var worldPos = levelTransformOpt?.TransformPoint(localPos) ?? localPos;

        marker.transform.position = worldPos + Vector3.up * 0.1f;

        marker.transform.localScale = Vector3.one * GetSetting<FloatSliderSetting>("Preview markers size").Value;

        marker.SetActive(true);
        _jumpMarkers.Add(marker);
    }


    private void UpdateLineRenderer()
    {
        if (_lineRenderer == null || _points.Count < 2) return;

        var smoothed = SmoothUtil.Points(_points.ToList(), 3);
        var smoothedWorld = smoothed.Select(p => CL_EventManager.currentLevel.transform.TransformPoint(p)).ToList();

        _lineRenderer.positionCount = smoothedWorld.Count;
        _lineRenderer.SetPositions(smoothedWorld.ToArray());
    }
}