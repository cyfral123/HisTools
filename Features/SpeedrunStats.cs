using System;
using System.IO;
using System.Linq;
using HisTools.Features.Controllers;
using HisTools.UI;
using HisTools.Utils;
using HisTools.Utils.SpeedrunFeature;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HisTools.Features;

public class SpeedrunStats : FeatureBase
{
    private Canvas _statsCanvas;
    private TextMeshProUGUI _statsText;

    private Transform _playerTransform;

    private M_Level _lastLevel;
    private M_Level _currentLevel;
    private TimeSpan _lastTimeSpan = TimeSpan.Zero;
    private TimeSpan _currentTimeSpan = TimeSpan.Zero;

    private TimeSpan _bestTime = TimeSpan.Zero;
    private TimeSpan _avgTime = TimeSpan.Zero;

    private readonly JArray _history = [];

    public SpeedrunStats() : base("SpeedrunStats", "Various info about level completion speed")
    {
        EventBus.Subscribe<GameStartEvent>(OnGameStart);
        EventBus.Subscribe<EnterLevelEvent>(OnEnterLevel);
        EventBus.Subscribe<LevelChangedEvent>(OnLevelChanged);
    }

    private void EnsurePlayer()
    {
        if (_playerTransform != null) return;

        var playerObj = GameObject.Find("CL_Player");
        if (playerObj == null)
        {
            Utils.Logger.Error("RoutePlayer: Player object not found");
        }

        _playerTransform = playerObj.transform;
    }

    private void EnsureUI()
    {
        EnsurePlayer();
        if (_statsCanvas != null && _statsText != null)
            return;

        _statsCanvas = new GameObject($"HisTools_{Name}_Canvas").AddComponent<Canvas>();
        _statsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        _statsText = _statsCanvas.transform.AddMyText(
            content: "ShowCompletionTimeElapsed",
            aligment: TextAlignmentOptions.Top,
            fontsize: 16f,
            color: Color.white
        );

        _statsCanvas.gameObject.SetActive(false);
    }

    public override void OnEnable()
    {
        EventBus.Subscribe<WorldUpdateEvent>(OnWorldUpdate);
    }

    public override void OnDisable()
    {
        EventBus.Unsubscribe<WorldUpdateEvent>(OnWorldUpdate);

        if (_statsCanvas != null)
            Object.Destroy(_statsCanvas.gameObject);
    }


    private static string Format(TimeSpan t) => t.ToString(@"mm\:ss\:ff");

    private bool ShouldUpdate()
    {
        if (!CL_EventManager.currentLevel)
            return false;
        if (!_currentLevel && !_lastLevel)
            return false;
        if (_currentTimeSpan == TimeSpan.Zero && _lastTimeSpan == TimeSpan.Zero)
            return false;

        return true;
    }

    private void OnWorldUpdate(WorldUpdateEvent e)
    {
        if (!ShouldUpdate())
            return;

        EnsureUI();
        _statsCanvas.gameObject.SetActive(true);

        var bg = Palette.HtmlTransparent(Plugin.BackgroundHtml.Value, 0.5f);
        var elapsedTime = _currentTimeSpan - _lastTimeSpan;
        var name = Text.CompactLevelName(_lastLevel.levelName);

        var (nowColor, avgColor, bestColor) = GetColors(elapsedTime);

        _statsText.text =
            $"<color=grey>{name}: " +
            $"Start: {Format(_lastTimeSpan)} " +
            $"End: {Format(_currentTimeSpan)}\n" +
            $"Now: <mark={bg}><b><color={nowColor}>{Format(elapsedTime)}</color></b></mark> " +
            $"Avg: <mark={bg}><b><color={avgColor}>{Format(_avgTime)}</color></b></mark> " +
            $"Best: <mark={bg}><b><color={bestColor}>{Format(_bestTime)}</color></b></mark>" +
            $"</color>";
    }

    private (string now, string avg, string best) GetColors(TimeSpan elapsedTime)
    {
        var attention = Plugin.RouteLabelEnabledColorHtml.Value;
        var normal = Plugin.EnabledHtml.Value;
        const string muted = "#808080";
        const string cheated = "#ffffff";

        if (CommandConsole.hasCheated)
            return (cheated, cheated, cheated);

        var nowColor = muted;
        var avgColor = muted;
        var bestColor = muted;

        if (elapsedTime < _bestTime)
        {
            nowColor = attention;
            bestColor = normal;
        }
        else if (elapsedTime < _avgTime)
        {
            nowColor = normal;
        }

        if (_avgTime.TotalSeconds > 0)
        {
            var delta = elapsedTime.TotalSeconds / _avgTime.TotalSeconds;
            if (Utils.Time.AlmostEqual(elapsedTime, _avgTime, TimeSpan.FromSeconds(delta)))
                nowColor = normal;
        }

        if (_bestTime == TimeSpan.Zero || _avgTime == TimeSpan.Zero)
            nowColor = attention;

        return (nowColor, avgColor, bestColor);
    }

    private void OnEnterLevel(EnterLevelEvent e)
    {
        var timeNow = TimeSpan.FromSeconds(CL_GameManager.gMan.GetGameTime());
        var newLevel = e.Level;

        if (_currentLevel == null)
        {
            _currentLevel = newLevel;
            _lastLevel = newLevel;

            _currentTimeSpan = timeNow;
            _lastTimeSpan = timeNow;

            return;
        }

        if (newLevel == _currentLevel)
        {
            _currentTimeSpan = timeNow;
            return;
        }

        _lastLevel = _currentLevel;
        _lastTimeSpan = _currentTimeSpan;

        _currentLevel = newLevel;
        _currentTimeSpan = timeNow;

        EventBus.Publish(new LevelChangedEvent(_lastLevel, _currentLevel, _lastTimeSpan, _currentTimeSpan));

        CoroutineRunner.Instance.StartCoroutine(
            RunsHistory.LoadSegmentsAndCompute(
                Plugin.SpeedrunStatsDir,
                _lastLevel.levelName,
                (best, avg) =>
                {
                    Utils.Logger.Debug($"Level {_lastLevel.levelName}: Best={best}, Avg={avg}");
                    _bestTime = best;
                    _avgTime = avg;
                }
            )
        );
    }

    private void OnLevelChanged(LevelChangedEvent e)
    {
        Utils.Logger.Debug("SpeedrunStats: Level changed");
        if (CommandConsole.hasCheated)
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

        if (WorldLoader.instance == null)
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
        var folderPath = Plugin.SpeedrunStatsDir;

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

        var filePath = Path.Combine(folderPath, maxCounter == 0 ? $"{baseFileName}.json" : $"{baseFileName}_{(maxCounter + 1):D2}.json");

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