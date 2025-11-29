using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class SpeedrunStats : FeatureBase
{
    private Canvas _statsCanvas;
    private TextMeshProUGUI _statsText;

    private Transform _playerTransform;

    private M_Level lastLevel;
    private M_Level currentLevel;
    private TimeSpan lastTimeSpan = TimeSpan.Zero;
    private TimeSpan currentTimeSpan = TimeSpan.Zero;

    private TimeSpan bestTime = TimeSpan.Zero;
    private TimeSpan avgTime = TimeSpan.Zero;

    private JArray history = [];

    public SpeedrunStats() : base("SpeedrunStats", "Various info about level completion speed")
    {
        EventBus.Subscribe<GameStartEvent>(OnGameStart);
        EventBus.Subscribe<EnterLevelEvent>(OnEnterLevel);
        EventBus.Subscribe<LevelChangedEvent>(OnLevelChanged);
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
            GameObject.Destroy(_statsCanvas.gameObject);
    }


    private static string Format(TimeSpan t) => t.ToString("mm\\:ss\\:ff");

    private bool ShouldUpdate()
    {
        if (CL_EventManager.currentLevel == null)
            return false;
        if (currentLevel == null && lastLevel == null)
            return false;
        if (currentTimeSpan == TimeSpan.Zero && lastTimeSpan == TimeSpan.Zero)
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
        var elapsedTime = currentTimeSpan - lastTimeSpan;
        var name = Text.CompactLevelName(lastLevel.levelName);

        var (nowColor, avgColor, bestColor) = GetColors(elapsedTime);

        _statsText.text =
            $"<color=grey>{name}: " +
            $"Start: {Format(lastTimeSpan)} " +
            $"End: {Format(currentTimeSpan)}\n" +
            $"Now: <mark={bg}><b><color={nowColor}>{Format(elapsedTime)}</color></b></mark> " +
            $"Avg: <mark={bg}><b><color={avgColor}>{Format(avgTime)}</color></b></mark> " +
            $"Best: <mark={bg}><b><color={bestColor}>{Format(bestTime)}</color></b></mark>" +
            $"</color>";
    }

    private (string now, string avg, string best) GetColors(TimeSpan elapsedTime)
    {
        string attention = Plugin.RouteLabelEnabledColorHtml.Value;
        string normal = Plugin.EnabledHtml.Value;
        const string muted = "#808080";
        const string cheated = "#ffffff";

        if (CommandConsole.hasCheated)
            return (cheated, cheated, cheated);

        string nowColor = muted;
        string avgColor = muted;
        string bestColor = muted;

        if (elapsedTime < bestTime)
        {
            nowColor = attention;
            bestColor = normal;
        }
        else if (elapsedTime < avgTime)
        {
            nowColor = normal;
        }

        if (avgTime.TotalSeconds > 0)
        {
            double delta = elapsedTime.TotalSeconds / avgTime.TotalSeconds;
            if (Utils.Time.AlmostEqual(elapsedTime, avgTime, TimeSpan.FromSeconds(delta)))
                nowColor = normal;
        }

        if (bestTime == TimeSpan.Zero || avgTime == TimeSpan.Zero)
            nowColor = attention;

        return (nowColor, avgColor, bestColor);
    }

    private void OnEnterLevel(EnterLevelEvent e)
    {
        var timeNow = TimeSpan.FromSeconds(CL_GameManager.gMan.GetGameTime());
        var newLevel = e.Level;

        if (currentLevel == null)
        {
            currentLevel = newLevel;
            lastLevel = newLevel;

            currentTimeSpan = timeNow;
            lastTimeSpan = timeNow;

            return;
        }

        if (newLevel == currentLevel)
        {
            currentTimeSpan = timeNow;
            return;
        }

        lastLevel = currentLevel;
        lastTimeSpan = currentTimeSpan;

        currentLevel = newLevel;
        currentTimeSpan = timeNow;

        EventBus.Publish(new LevelChangedEvent(lastLevel, currentLevel, lastTimeSpan, currentTimeSpan));

        CoroutineRunner.Instance.StartCoroutine(
            RunsHistory.LoadSegmentsAndCompute(
                Plugin.SpeedrunStatsDir,
                lastLevel.levelName,
                (best, avg) =>
                {
                    Utils.Logger.Debug($"Level {lastLevel.levelName}: Best={best}, Avg={avg}");
                    bestTime = best;
                    avgTime = avg;
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

        var block = new JObject();
        block["from"] = e.LastLevel.levelName;
        block["to"] = e.CurrentLevel.levelName;
        block["start"] = e.LastTimeSpan.ToString("hh\\:mm\\:ss\\:ff");
        block["end"] = e.CurrentTimeSpan.ToString("hh\\:mm\\:ss\\:ff");
        block["elapsed"] = (e.CurrentTimeSpan - e.LastTimeSpan).ToString("hh\\:mm\\:ss\\:ff");
        history.Add(block);
    }

    private void OnGameStart(GameStartEvent e)
    {
        Utils.Logger.Debug("SpeedrunStats: Game start");
        StartHistory(savePrevious: true);
    }

    private void StartHistory(bool savePrevious)
    {
        if (history.Count > 3 && savePrevious)
        {
            SaveHistory();
        }

        history.RemoveAll();

        if (WorldLoader.instance == null)
            return;

        var runInfo = new JObject();
        runInfo["runStart"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        runInfo["runSeed"] = WorldLoader.instance.seed;
        history.Add(runInfo);
    }

    private void SaveHistory(int maxFiles = 500)
    {
        var baseFileName = "unnamed_run";
        var folderPath = Plugin.SpeedrunStatsDir;

        var files = Directory.GetFiles(folderPath, $"{baseFileName}*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        int maxCounter = 0;
        foreach (var file in files)
        {
            if (file == baseFileName)
            {
                maxCounter = Math.Max(maxCounter, 1);
            }
            else if (file.StartsWith(baseFileName + "_"))
            {
                var suffix = file.Substring(baseFileName.Length + 1);
                if (int.TryParse(suffix, out int n))
                {
                    maxCounter = Math.Max(maxCounter, n);
                }
            }
        }

        string filePath;
        if (maxCounter == 0)
            filePath = Path.Combine(folderPath, $"{baseFileName}.json");
        else
            filePath = Path.Combine(folderPath, $"{baseFileName}_{(maxCounter + 1):D2}.json");

        Utils.Logger.Debug($"SpeedrunStats: saving history to {filePath}");
        Files.SaveJsonToFile(filePath, history);

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
