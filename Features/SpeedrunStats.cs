using System;
using System.IO;
using System.Linq;
using HisTools.Features.Controllers;
using HisTools.Prefabs;
using HisTools.Utils;
using HisTools.Utils.SpeedrunFeature;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Text = HisTools.Utils.Text;

namespace HisTools.Features;

internal struct LevelState
{
    public M_Level Instance;
    public TimeSpan NowTime;
    public TimeSpan AvgTime;
    public TimeSpan BestTime;
    public TimeSpan ElapsedTime;

    public string DisplayName => Instance ? Text.CompactLevelName(Instance.levelName) : "Unknown Level";
}

public class SpeedrunStats : FeatureBase
{
    private Canvas _statsCanvas;

    private TextMeshProUGUI _prevText;
    private TextMeshProUGUI _currText;

    private Transform _playerTransform;

    private LevelState _prevLevel;
    private LevelState _currLevel;

    private readonly JArray _history = [];

    private readonly FloatSliderSetting _levelsPosition;
    private readonly BoolSetting _showOnlyInPause;
    private readonly BoolSetting _showPredicted;

    private static string Format(TimeSpan t) => t.ToString(@"mm\:ss\:ff");

    public SpeedrunStats() : base("SpeedrunStats", "Various info about level completion speed")
    {
        EventBus.Subscribe<GameStartEvent>(OnGameStart);
        EventBus.Subscribe<EnterLevelEvent>(OnEnterLevel);
        EventBus.Subscribe<LevelChangedEvent>(OnLevelChanged);

        _showOnlyInPause = AddSetting(new BoolSetting(this, "ShowOnlyInPause", "...", false));
        _showPredicted = AddSetting(new BoolSetting(this, "PredictElapsedTime", "...", true));
        _levelsPosition = AddSetting(new FloatSliderSetting(this, "Position", "...", 3f, 1f, 6f, 1f, 0));
    }

    public override void OnEnable()
    {
        EventBus.Subscribe<WorldUpdateEvent>(OnWorldUpdate);
    }

    public override void OnDisable()
    {
        EventBus.Unsubscribe<WorldUpdateEvent>(OnWorldUpdate);

        if (_statsCanvas) Object.Destroy(_statsCanvas.gameObject);
    }

    private void EnsurePlayer()
    {
        if (_playerTransform) return;
        if (Player.GetTransform().TryGet(out var value)) _playerTransform = value;
    }

    private void EnsureUI()
    {
        if (_statsCanvas && _prevText) return;
        if (!PrefabDatabase.Instance.GetObject("histools/UI_Speedrun", false).TryGet(out var prefab)) return;

        var go = Object.Instantiate(prefab);

        _statsCanvas = go.GetComponent<Canvas>();

        var texts = _statsCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);

        _prevText = texts[0];
        _currText = texts[1];

        var group = _statsCanvas.GetComponentInChildren<VerticalLayoutGroup>(true);
        Anchor.SetAnchor(group.GetComponent<RectTransform>(), (int)_levelsPosition.Value);
    }

    private bool ShouldUpdate()
    {
        return CL_EventManager.currentLevel ? _currLevel.Instance : false;
    }

    private TimeSpan PredictElapsedTime(M_Level level, TimeSpan currentElapsed)
    {
        var levelHeight = level.GetLength();
        var traveledHeight = Vectors.ConvertPointToLocal(_playerTransform.position).y;
        var currentSpeed = traveledHeight / currentElapsed.TotalSeconds;

        var remainingHeight = levelHeight - traveledHeight;
        var remainingTime = remainingHeight / currentSpeed;

        return TimeSpan.FromSeconds(remainingTime + currentElapsed.TotalSeconds);
    }

    private void OnWorldUpdate(WorldUpdateEvent e)
    {
        if (!ShouldUpdate()) return;
        EnsurePlayer();
        EnsureUI();

        if (_showOnlyInPause.Value && !CL_GameManager.gMan.isPaused)
            _statsCanvas.gameObject.SetActive(false);
        else
            _statsCanvas.gameObject.SetActive(true);

        var bg = Palette.HtmlTransparent(Plugin.BackgroundHtml.Value, 0.5f);

        var prevNowColor = CalculateColor(_prevLevel.ElapsedTime, _prevLevel.AvgTime);

        _prevText.text =
            $"<color=grey>{_prevLevel.DisplayName} - " +
            $"<mark={bg}><b><color={prevNowColor}>{Format(_prevLevel.ElapsedTime)}</color></b></mark> " +
            $"<color=#808080>(Avg: <b>{Format(_prevLevel.AvgTime)}</b> | Best: <b>{Format(_prevLevel.BestTime)}</b>)</color>";

        var currentElapsed = TimeSpan.FromSeconds(CL_GameManager.gMan.GetGameTime()) - _currLevel.NowTime;

        var predictedElapsed = PredictElapsedTime(_currLevel.Instance, currentElapsed);
        var calculatedElapsedTime = _showPredicted.Value ? predictedElapsed : currentElapsed;

        var currNowColor = CalculateColor(currentElapsed, _currLevel.AvgTime);

        _currText.text =
            $"<color=grey>{_currLevel.DisplayName} - " +
            $"<mark={bg}><b><color={currNowColor}>{Format(calculatedElapsedTime)}</color></b></mark> " +
            $"<color=#808080>(Avg: <b>{Format(_currLevel.AvgTime)}</b> | Best: <b>{Format(_currLevel.BestTime)}</b>)</color>" +
            $"</color>";
    }

    private static string CalculateColor(TimeSpan elapsedTime, TimeSpan avgTime)
    {
        const string muted = "#808080";
        const string cheated = "#ffffff";
        var attention = Plugin.EnabledHtml.Value;

        if (Cheats.Detected) return cheated;

        // If the player is within 20% of the average time, highlight the current time
        if (avgTime.TotalSeconds > 0)
        {
            var deviation = Math.Abs(elapsedTime.TotalSeconds - avgTime.TotalSeconds);
            var percent = deviation / avgTime.TotalSeconds;

            if (percent < 0.2 || elapsedTime.TotalSeconds < avgTime.TotalSeconds) return attention;
        }

        return muted;
    }

    private void OnEnterLevel(EnterLevelEvent e)
    {
        var timeNow = TimeSpan.FromSeconds(CL_GameManager.gMan.GetGameTime());
        var newLevel = e.Level;

        if (!_currLevel.Instance)
        {
            _currLevel = new LevelState
            {
                Instance = newLevel,
                NowTime = timeNow,
                ElapsedTime = TimeSpan.Zero
            };

            CoroutineRunner.Instance.StartCoroutine(
                RunsHistory.LoadSegmentsAndCompute(
                    Constants.Paths.SpeedrunStatsDir,
                    _currLevel.Instance.levelName,
                    (best, avg) =>
                    {
                        _currLevel.BestTime = best;
                        _currLevel.AvgTime = avg;
                    }
                )
            );

            _prevLevel = _currLevel;
            return;
        }

        if (newLevel == _currLevel.Instance)
        {
            _currLevel.NowTime = timeNow;
            return;
        }

        _prevLevel = _currLevel;
        _prevLevel.ElapsedTime = timeNow - _prevLevel.NowTime;

        _currLevel = new LevelState
        {
            Instance = newLevel,
            NowTime = timeNow,
            ElapsedTime = TimeSpan.Zero
        };

        CoroutineRunner.Instance.StartCoroutine(
            RunsHistory.LoadSegmentsAndCompute(
                Constants.Paths.SpeedrunStatsDir,
                _prevLevel.Instance.levelName,
                (best, avg) =>
                {
                    _prevLevel.BestTime = best;
                    _prevLevel.AvgTime = avg;
                }
            )
        );

        CoroutineRunner.Instance.StartCoroutine(
            RunsHistory.LoadSegmentsAndCompute(
                Constants.Paths.SpeedrunStatsDir,
                _currLevel.Instance.levelName,
                (best, avg) =>
                {
                    _currLevel.BestTime = best;
                    _currLevel.AvgTime = avg;
                }
            )
        );

        EventBus.Publish(new LevelChangedEvent(_prevLevel.Instance, _currLevel.Instance, _prevLevel.NowTime,
            _currLevel.NowTime));
    }

    private void OnLevelChanged(LevelChangedEvent e)
    {
        if (Cheats.Detected)
        {
            Utils.Logger.Debug("SpeedrunStats: Level changed while cheating, ignoring");
            return;
        }

        var block = new JObject
        {
            ["from"] = e.LastLevel.levelName,
            ["to"] = e.CurrentLevel.levelName,
            ["start"] = Format(e.LastTimeSpan),
            ["end"] = Format(e.CurrentTimeSpan),
            ["elapsed"] = Format((e.CurrentTimeSpan - e.LastTimeSpan))
        };
        _history.Add(block);
    }

    private void OnGameStart(GameStartEvent e)
    {
        Utils.Logger.Debug("SpeedrunStats: Game start");
        StartHistory(savePrevious: true);
    }

    private void StartHistory(bool savePrevious)
    {
        if (_history.Count > 3 && savePrevious)
        {
            SaveHistory();
        }

        _history.RemoveAll();

        if (!WorldLoader.instance)
            return;

        var runInfo = new JObject
        {
            ["runStart"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["runSeed"] = WorldLoader.instance.seed
        };
        _history.Add(runInfo);
    }

    private void SaveHistory(int maxFiles = 500)
    {
        const string baseFileName = "unnamed_run";
        var folderPath = Constants.Paths.SpeedrunStatsDir;

        var files = Directory.GetFiles(folderPath, $"{baseFileName}*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        var maxCounter = 0;
        foreach (var file in files)
        {
            if (file == baseFileName)
            {
                maxCounter = Math.Max(maxCounter, 1);
            }
            else if (file.StartsWith(baseFileName + "_"))
            {
                var suffix = file.Substring(baseFileName.Length + 1);
                if (int.TryParse(suffix, out var n))
                {
                    maxCounter = Math.Max(maxCounter, n);
                }
            }
        }

        var filePath = Path.Combine(folderPath,
            maxCounter == 0 ? $"{baseFileName}.json" : $"{baseFileName}_{(maxCounter + 1):D2}.json");

        Utils.Logger.Debug($"SpeedrunStats: saving history to {filePath}");
        Files.SaveJsonToFile(filePath, _history);

        var allFiles = Directory.GetFiles(folderPath, $"{baseFileName}*.json")
            .OrderBy(File.GetCreationTime)
            .ToList();

        while (allFiles.Count > maxFiles)
        {
            try
            {
                File.Delete(allFiles[0]);
                allFiles.RemoveAt(0);
            }
            catch (Exception ex)
            {
                Utils.Logger.Error($"SpeedrunStats: failed to delete file {allFiles[0]}: {ex.Message}");
            }
        }
    }
}