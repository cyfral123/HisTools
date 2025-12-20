using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using HarmonyLib;
using HisTools.Features.Controllers;
using HisTools.Prefabs;
using HisTools.UI.Controllers;
using HisTools.Utils;
using HisTools.Utils.RouteFeature;
using LibBSP;
using Newtonsoft.Json;
using Steamworks;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HisTools.Features;

public class RouteRecorder : FeatureBase
{
    private const int MinPoints = 10;
    private const string JumpButton = "Jump";
    private bool _stopRequested;

    private readonly List<Vector3> _points = [];
    private readonly HashSet<int> _jumpIndices = [];
    private readonly List<Note> _notes = [];

    private GameObject _previewRoot;
    private LineRenderer _previewLine;
    private GameObject _previewMarker;

    private Transform _playerTransform;
    private TextMeshPro _notePrefab;

    private GameObject _uiGuide;
    private PopupController _addNotePopup;
    private PopupController _saveRecordPopup;


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
        if (!player) return;
        _playerTransform = player.transform;
        Cleanup();
        _previewRoot = new GameObject("HisTools_PreviewRoot");

        // Marker
        var marker = PrefabDatabase.Instance.GetObject("histools/SphereMarker", false);
        if (marker)
        {
            _previewMarker = Object.Instantiate(marker, _previewRoot.transform);
            _previewMarker.AddComponent<MarkerActivator>();
            _previewMarker.GetComponent<Renderer>().material.color = Color.cyan;
        }

        // Note
        var notePrefab = PrefabDatabase.Instance.GetObject("histools/InfoLabel", false);
        if (notePrefab)
        {
            var noteGo = Object.Instantiate(notePrefab);
            var tmp = noteGo.GetComponent<TextMeshPro>();
            tmp.fontSize = 3;

            var look = noteGo.AddComponent<LookAtPlayer>();
            look.player = _playerTransform;

            _notePrefab = tmp;
        }

        // Line
        var lineObj = new GameObject("HisTools_RecordedPath");
        _previewLine = lineObj.AddComponent<LineRenderer>();
        _previewLine.positionCount = 0;
        _previewLine.material = new Material(Shader.Find("Sprites/Default"));
        _previewLine.widthMultiplier = GetSetting<FloatSliderSetting>("Preview line width").Value;
        _previewLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _previewLine.receiveShadows = false;

        lineObj.transform.SetParent(_previewRoot.transform);
        var gradient = new Gradient();
        gradient.SetKeys(
            [new GradientColorKey(Color.green, 0f), new GradientColorKey(Color.red, 1f)],
            [new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)]
        );

        _previewLine.colorGradient = gradient;

        // Guide
        var guide = PrefabDatabase.Instance.GetObject("histools/UI_RouteRecorder", true);
        if (guide) _uiGuide = Object.Instantiate(guide, _playerTransform, true);

        // Note Popup
        var notePopupPrefab = PrefabDatabase.Instance.GetObject("histools/UI_Popup_Input", true);
        if (!notePopupPrefab) return;

        var notePopup = Object.Instantiate(notePopupPrefab);
        _addNotePopup = notePopup.AddComponent<PopupController>();
        _addNotePopup.title.text = "RouteRecorder";
        _addNotePopup.description.text = "You are about to add a new note at the location the camera is pointing at";

        _addNotePopup.applyButton.onClick.AddListener(() =>
        {
            if (!Camera.main) return;

            var pos = Raycast.GetLookTarget(Camera.main.transform, 100f);
            var text = _addNotePopup.inputField!.text;
            AddNote(pos + Vector3.up * 0.5f, text);

            Utils.Logger.Info($"RecordPath: Added note at {pos}: {text}");
            _addNotePopup?.Hide();
        });

        _addNotePopup.cancelButton.onClick.AddListener(() => Utils.Logger.Debug("AddNotePopup CANCEL clicked"));


        // SaveRecord Popup
        if (_saveRecordPopup) Object.Destroy(_saveRecordPopup.gameObject);
        var savePopupPrefab = PrefabDatabase.Instance.GetObject("histools/UI_Popup_SaveRecord", true);
        if (!savePopupPrefab) return;

        var savePopup = Object.Instantiate(savePopupPrefab);
        _saveRecordPopup = savePopup.AddComponent<PopupController>();

        var folderPath = Path.Combine(Constants.Paths.ConfigDir, "Routes");
        Directory.CreateDirectory(folderPath);

        var content = savePopup.transform.Find("Window/Scroll View/Viewport/Content");

        var nameField = content?.Find("RouteName/Input")?
            .GetComponent<TMP_InputField>();

        var authorField = content?.Find("RouteAuthor/Input")?
            .GetComponent<TMP_InputField>();

        var descriptionField = content?.Find("RouteDescription/Input")?
            .GetComponent<TMP_InputField>();

        var levelField = content?.Find("RouteTargetLevel/Input")?
            .GetComponent<TMP_InputField>();
        
        authorField?.text = SteamClient.Name ?? "unknownAuthor";
        levelField?.text = CL_EventManager.currentLevel?.levelName ?? "unknownLevel";
        
        _saveRecordPopup.applyButton.onClick.AddListener(() =>
        {
            var routeInfo = new RouteInfo
            {
                uid = Files.GenerateUid(),
                name = string.IsNullOrWhiteSpace(nameField?.text) ? "unnamed" : nameField.text,
                author = authorField?.text,
                description = descriptionField?.text,
                targetLevel = levelField?.text
            };

            var routeDto = RouteMapper.ToDto(_points, _jumpIndices, _notes, routeInfo, GetMinPointDistance());
            var json = JsonConvert.SerializeObject(new[] { routeDto }, Formatting.Indented);

            try
            {
                var filePath = Files.GetNextFilePath(folderPath, $"{routeInfo.targetLevel}_{routeInfo.author}", "json");
                File.WriteAllText(filePath, json);
                Utils.Logger.Info($"RecordPath: JSON saved to {filePath}");
            }
            catch (Exception ex)
            {
                Utils.Logger.Error($"RecordPath: Failed to write JSON: {ex.Message}");
            }
            
            _saveRecordPopup?.Hide();
            Cleanup();
        });

        _saveRecordPopup.cancelButton.onClick.AddListener(Cleanup);

        EventBus.Subscribe<PlayerLateUpdateEvent>(OnPlayerLateUpdate);
    }

    public override void OnDisable()
    {
        EventBus.Unsubscribe<PlayerLateUpdateEvent>(OnPlayerLateUpdate);
        if (_points.Count < MinPoints)
        {
            Utils.Logger.Info($"RecordPath: Not enough recorded points to save ({_points.Count} <= {MinPoints})");
            Cleanup();
            return;
        }

        _saveRecordPopup.Show();
    }

    private void Cleanup()
    {
        _stopRequested = false;
        if (_addNotePopup) Object.Destroy(_addNotePopup.gameObject);
        if (_previewRoot) Object.Destroy(_previewRoot);

        _points.Clear();
        _notes.Clear();
        _jumpIndices.Clear();

        if (_uiGuide) Object.Destroy(_uiGuide);
    }

    // Disable feature on the next frame to safely exit the current update callback
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
        var jumped = InputManager.GetButton(JumpButton).Down && !CL_GameManager.gMan.lockPlayerInput;
        var minDistance = GetMinPointDistance();

        if (_points.Count == 0 || Vector3.Distance(_points.Last(), playerPos) >= minDistance || jumped)
            AddPointLocal(playerPos, jumped);

        if (Input.GetMouseButtonDown(2)) _addNotePopup?.Show();

        if (GetSetting<BoolSetting>("Auto stop").Value)
        {
            if (!_stopRequested && _playerTransform.position.DistanceTo(level.GetLevelExit().position) < distanceToStop)
            {
                _stopRequested = true;

                CoroutineRunner.Instance.StartCoroutine(DelayedStop());
            }
        }
    }

    private float GetMinPointDistance()
    {
        var q = GetSetting<FloatSliderSetting>("Record quality");
        return (float)Math.Round(math.remap(q.Min, q.Max, q.Max, q.Min, q.Value), 2);
    }

    private void AddNote(Vector3 localPos, string text)
    {
        _notes.Add(new Note(localPos, text));
        SpawnPreviewNoteWorld(localPos, text);
    }

    private void SpawnPreviewNoteWorld(Vector3 localPos, string text)
    {
        TryGetWorldPoint(localPos, out var worldPos);
        var noteLabel = Object.Instantiate(_notePrefab, worldPos, Quaternion.identity, _previewRoot.transform);
        noteLabel.text = text;

        noteLabel.gameObject.SetActive(true);
    }

    private void AddPointLocal(Vector3 localPos, bool isJumped)
    {
        var index = _points.Count;
        _points.Add(localPos);

        if (isJumped)
        {
            _jumpIndices.Add(index);
            SpawnPreviewMarkerWorld(localPos);
        }

        UpdatePreview();
    }

    private static void TryGetWorldPoint(Vector3 localPos, out Vector3 worldPos)
    {
        var levelTransformOpt = CL_EventManager.currentLevel?.transform;
        worldPos = levelTransformOpt?.TransformPoint(localPos) ?? localPos;
    }

    private void SpawnPreviewMarkerWorld(Vector3 localPos)
    {
        if (!_previewMarker)
            return;
        TryGetWorldPoint(localPos, out var worldPos);

        var marker = Object.Instantiate(_previewMarker, worldPos, quaternion.identity, _previewRoot.transform);

        marker.transform.localScale = Vector3.one * GetSetting<FloatSliderSetting>("Preview markers size").Value;
        marker.GetComponent<Renderer>().material.DOFade(0.1f, 6f).SetTarget(marker);
        marker.SetActive(true);
    }

    private void UpdatePreview()
    {
        if (_previewLine == null || _points.Count < 2) return;

        var smoothed = SmoothUtil.Points(_points.ToList(), 3);
        var smoothedWorld = smoothed.Select(p => CL_EventManager.currentLevel.transform.TransformPoint(p)).ToList();

        _previewLine.positionCount = smoothedWorld.Count;
        _previewLine.SetPositions(smoothedWorld.ToArray());
    }
}