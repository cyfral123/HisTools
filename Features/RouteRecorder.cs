using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly List<PathPoint> _points = [];
    private readonly List<NotePoint> _notes = [];
    private readonly List<GameObject> _jumpMarkers = [];

    private LineRenderer _lineRenderer;
    private GameObject _markerPrefab;
    private Transform _player;

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
        if (Player.GetTransform().TryGet(out var value))
        {
            _player = value;
        }

        if (!_markerPrefab)
        {
            if (PrefabDatabase.Instance.GetObject("histools/SphereMarker", false).TryGet(out var marker))
            {
                _markerPrefab = Object.Instantiate(marker);
                _markerPrefab.AddComponent<MarkerActivator>();
                _markerPrefab.GetComponent<Renderer>().material.color = Color.cyan;
            }
        }

        _points.Clear();
        _jumpMarkers.Clear();
        _notes.Clear();

        var lineObj = new GameObject("HisTools_RecordedPath");
        _lineRenderer = lineObj.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 0;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.widthMultiplier = GetSetting<FloatSliderSetting>("Preview line width").Value;
        _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;

        Gradient gradient = new();
        gradient.SetKeys(
            [
                new GradientColorKey(Color.green, 0f),
                new GradientColorKey(Color.red, 1f)
            ],
            [
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            ]
        );
        _lineRenderer.colorGradient = gradient;

        EventBus.Subscribe<PlayerLateUpdateEvent>(OnPlayerLateUpdate);

        if (PrefabDatabase.Instance.GetObject("histools/UI_RouteRecorder", true).TryGet(out var guide))
        {
            _uiGuide = Object.Instantiate(guide, _player, true);
        }
    }

    public override void OnDisable()
    {
        if (_points.Count > 5)
        {
            var folderPath = Path.Combine(Constants.Paths.ConfigDir, "Routes");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var author = "unknownAuthor";
            if (SteamClient.IsValid)
            {
                author = SteamClient.Name;
            }

            var levelName = CL_EventManager.currentLevel.levelName;
            if (string.IsNullOrEmpty(levelName))
                levelName = "unknownLevel";

            var baseFileName = $"route_{levelName}_by_{author}";
            var filePath = Path.Combine(folderPath, baseFileName + ".json");

            var counter = 2;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(folderPath, $"{baseFileName}_{counter:D2}.json");
                counter++;
            }

            var resultFileName = Path.GetFileNameWithoutExtension(filePath);

            var quality = GetSetting<FloatSliderSetting>("Record quality");
            var pathData = new
            {
                onlyForDebug = new
                {
                    minDistanceBetweenPoints =
                        Math.Round(math.remap(quality.Min, quality.Max, quality.Max, quality.Min, quality.Value), 2),
                },
                info = new
                {
                    // template
                    uid = Files.GenerateUid(),
                    name = $"unnamed_{levelName}",
                    author = SteamClient.Name,
                    description =
                        $"Recorded on {DateTime.Now} \nEdit this route in json:\n..\\BepInEx\\HisTools\\Routes\\{resultFileName}.json",
                    preferredCompleteColor = "#00000000",
                    preferredRemainingColor = "#00000000",
                    preferredNoteColor = "#00000000",
                    targetLevel = levelName
                },
                points = _points.Select(p => new
                {
                    x = Math.Round(p.x, 2),
                    y = Math.Round(p.y, 2),
                    z = Math.Round(p.z, 2),
                    jump = p.jump
                }),
                notes = _notes.Select(n => new
                {
                    x = Math.Round(n.Position.x, 2),
                    y = Math.Round(n.Position.y, 2),
                    z = Math.Round(n.Position.z, 2),
                    note = n.note
                }),
            };

            var json = JsonConvert.SerializeObject(new[] { pathData }, Formatting.Indented);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            try
            {
                File.WriteAllText(filePath, json);
                Utils.Logger.Info($"RecordPath: JSON saved to {filePath}");
            }
            catch (Exception ex)
            {
                Utils.Logger.Error($"RecordPath: Failed to write JSON to file: {ex.Message}");
            }
        }
        else
        {
            Utils.Logger.Info("RecordPath: No points recorded");
        }

        if (_lineRenderer != null)
        {
            Object.Destroy(_lineRenderer.gameObject);
            _lineRenderer = null;
        }

        foreach (var marker in _jumpMarkers.Where(marker => marker))
            Object.Destroy(marker);
        _jumpMarkers.Clear();
        _points.Clear();
        _notes.Clear();
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
        if (!_player || !level) return;
        var playerPos = level.transform.InverseTransformPoint(_player.position);
        var distanceToStop = GetSetting<FloatSliderSetting>("Auto stop distance").Value;
        var jumped = InputManager.GetButton(JumpButton).Down;
        var quality = GetSetting<FloatSliderSetting>("Record quality");
        // invert slider, because minimum distance is maximum quality
        var minDistance = Math.Round(math.remap(quality.Min, quality.Max, quality.Max, quality.Min, quality.Value), 2);
        if (_points.Count == 0 || Vector3.Distance(_points.Last().Position, playerPos) >= minDistance || jumped)
        {
            AddPoint(playerPos, jumped);
        }

        if (Input.GetMouseButtonDown(2))
        {
            if (Camera.main)
            {
                var pos = Raycast.GetLookTarget(Camera.main.transform, 100f);
                const string noteText = "YourNote";
                _notes.Add(new NotePoint(pos, noteText));
                Utils.Logger.Info($"RecordPath: Added note at {pos}: {noteText}");
            }
        }

        if (GetSetting<BoolSetting>("Auto stop").Value)
        {
            if (_player.position.DistanceTo(level.GetLevelExit().position) < distanceToStop)
                CoroutineRunner.Instance.StartCoroutine(DelayedStop());
        }
    }

    private void AddPoint(Vector3 pos, bool jumped)
    {
        var point = new PathPoint(pos, jumped);
        _points.Add(point);

        if (jumped)
        {
            var marker = Object.Instantiate(_markerPrefab);
            marker.SetActive(true);
            var worldPos = CL_EventManager.currentLevel.transform.TransformPoint(pos);
            marker.transform.position = worldPos + Vector3.up * 0.1f;
            marker.transform.localScale = Vector3.one * GetSetting<FloatSliderSetting>("Preview markers size").Value;

            _jumpMarkers.Add(marker);
        }

        UpdateLineRenderer();
    }

    private void UpdateLineRenderer()
    {
        if (_lineRenderer == null || _points.Count < 2) return;

        var smoothed = SmoothUtil.Points(_points.Select(p => p.Position).ToList(), 3);

        var smoothedWorld = smoothed
            .Select(p => CL_EventManager.currentLevel.transform.TransformPoint(p))
            .ToList();

        _lineRenderer.positionCount = smoothedWorld.Count;
        _lineRenderer.SetPositions(smoothedWorld.ToArray());
    }
}