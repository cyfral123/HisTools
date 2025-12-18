using System;
using System.Collections.Generic;
using System.Linq;
using HisTools.Features.Controllers;
using HisTools.Prefabs;
using HisTools.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HisTools.Features;

internal struct BuffIndicator
{
    public Transform Transform;
    public Image[] Icons;
    public TextMeshProUGUI Value;
    public TextMeshProUGUI Time;
}

public class BuffsDisplay : FeatureBase
{
    private Canvas _canvas;
    private HorizontalLayoutGroup _layout;

    private BuffIndicator? _grub;
    private BuffIndicator? _injector;
    private BuffIndicator? _pills;
    private BuffIndicator? _foodBar;

    private readonly BoolSetting _showOnlyInPause;
    private readonly FloatSliderSetting _buffsPosition;

    public BuffsDisplay() : base("BuffsDisplay", "Display all current buff effects")
    {
        _showOnlyInPause = AddSetting(new BoolSetting(this, "ShowOnlyInPause", "...", false));
        _buffsPosition = AddSetting(new FloatSliderSetting(this, "Position", "...", 2f, 1f, 6f, 1f, 0));
    }

    private BuffIndicator? GetBuffIndicator(string name)
    {
        if (!_layout || !_layout.transform) return null;

        var transform = _layout.transform.Find(name);
        var icons = transform.Find("Icons").GetComponentsInChildren<Image>(true);
        var value = transform.Find("Value").GetComponent<TextMeshProUGUI>();
        var time = transform.Find("Time").GetComponent<TextMeshProUGUI>();

        if (icons.Length == 0 || !value || !time || !transform)
            return null;

        return new BuffIndicator { Transform = transform, Icons = icons, Value = value, Time = time };
    }

    private bool EnsurePrefabs()
    {
        if (_grub.HasValue &&
            _injector.HasValue &&
            _pills.HasValue &&
            _foodBar.HasValue &&
            _layout &&
            _canvas)
            return true;

        var prefab = PrefabDatabase.Instance.GetObject("histools/UI_BuffsDisplay", true);
        if (!prefab)
            return false;

        var go = Object.Instantiate(prefab);

        _canvas = go.GetComponent<Canvas>();
        _layout = _canvas.GetComponentInChildren<HorizontalLayoutGroup>(true);

        if (!_canvas || !_layout)
        {
            Object.Destroy(go);
            return false;
        }

        var grub = GetBuffIndicator("Grub");
        var injector = GetBuffIndicator("Injector");
        var pills = GetBuffIndicator("Pills");
        var foodBar = GetBuffIndicator("FoodBar");

        if (!grub.HasValue ||
            !injector.HasValue ||
            !pills.HasValue ||
            !foodBar.HasValue)
        {
            Object.Destroy(go);
            _canvas = null;
            _layout = null;
            return false;
        }

        _grub = grub;
        _injector = injector;
        _pills = pills;
        _foodBar = foodBar;

        Anchor.SetAnchor(
            _layout.GetComponent<RectTransform>(),
            (int)_buffsPosition.Value
        );

        return true;
    }


    public override void OnEnable()
    {
        EventBus.Subscribe<WorldUpdateEvent>(OnWorldUpdate);
    }

    private static float CalculateBuffSecondsLeft(BuffContainer.Buff buff, float loseRate, bool lose)
    {
        var player = ENT_Player.GetPlayer();
        if (buff == null || buff.maxAmount <= 0f || float.IsNaN(buff.amount) || player == null)
            return 0f;

        if (!lose || loseRate <= 0f)
            return float.PositiveInfinity;

        var buffs = player.curBuffs;

        var timeMultBuff = buffs.GetBuff("buffTimeMult");

        var timeMultiplier = Mathf.Max(1f + timeMultBuff, 0.1f);
        var num = 1f / timeMultiplier;

        var effectiveLoseRate = loseRate * num;

        var buffTime = buff.amount / buff.maxAmount;

        return buffTime / effectiveLoseRate;
    }

    private static string GetFormattedTime(float secondsLeft)
    {
        return float.IsInfinity(secondsLeft) ? "∞" : TimeSpan.FromSeconds(secondsLeft).ToString(@"mm\:ss\:ff");
    }

    private static bool HaveBuff(BuffContainer bc, string id)
    {
        return bc.buffs.Any(buff => buff.id == id);
    }

    private static List<BuffContainer.Buff> GetBuffs(BuffContainer bc, string id)
    {
        return bc.buffs.Where(buff => buff.id == id).ToList();
    }

    private static List<BuffContainer> SearchBuff(List<BuffContainer> bcList, string id)
    {
        return bcList.Where(container => HaveBuff(container, id)).ToList();
    }

    private static void UpdateIconsStack(Image[] icons, int count)
    {
        for (var i = 1; i < icons.Length; i++)
            icons[i].gameObject.SetActive(i < count);
    }

    private static float SummaryBuffSecondsLeft(List<BuffContainer> containers, string id)
    {
        var secondsLeft = 0f;
        foreach (var container in containers)
        {
            var buff = GetBuffs(container, id).First();
            secondsLeft += CalculateBuffSecondsLeft(buff, container.loseRate, container.loseOverTime) - secondsLeft;
        }

        return secondsLeft;
    }

    private void OnWorldUpdate(WorldUpdateEvent e)
    {
        if (!EnsurePrefabs())
        {
            Utils.Logger.Error("BuffsDisplay: Failed to ensure prefabs");
            return;
        }

        var player = ENT_Player.GetPlayer();
        if (player == null) return;

        if (_showOnlyInPause.Value && !CL_GameManager.gMan.isPaused)
        {
            _grub?.Transform.gameObject.SetActive(false);
            _injector?.Transform.gameObject.SetActive(false);
            _foodBar?.Transform.gameObject.SetActive(false);
            _pills?.Transform.gameObject.SetActive(false);
            return;
        }

        var containers = player.curBuffs.currentBuffs;

        var foodBarContainers = SearchBuff(containers, "roided")
            .Where(container => HaveBuff(container, "pilled"))
            .ToList();

        var roidedContainers = SearchBuff(containers, "roided")
            .Where(container => !HaveBuff(container, "pilled"))
            .ToList();

        var pilledContainers = SearchBuff(containers, "pilled").Where(container => !HaveBuff(container, "roided"))
            .ToList();
        var goopedContainers = SearchBuff(containers, "gooped");
        
        _grub?.Transform.gameObject.SetActive(goopedContainers.Count > 0);
        _injector?.Transform.gameObject.SetActive(roidedContainers.Count > 0);
        _foodBar?.Transform.gameObject.SetActive(foodBarContainers.Count > 0);
        _pills?.Transform.gameObject.SetActive(pilledContainers.Count > 0);
        
        if (goopedContainers.Count > 0)
            RenderBuff(goopedContainers, _grub.GetValueOrDefault(), "gooped");

        if (roidedContainers.Count > 0)
            RenderBuff(roidedContainers, _injector.GetValueOrDefault(), "roided");

        if (pilledContainers.Count > 0)
            RenderBuff(pilledContainers, _pills.GetValueOrDefault(), "pilled");

        if (foodBarContainers.Count > 0)
            RenderBuff(foodBarContainers, _foodBar.GetValueOrDefault(), "roided", "pilled");
    }

    private void RenderBuff(List<BuffContainer> containers, BuffIndicator obj, string buffId1, string buffId2 = null)
    {
        var container = containers.First();
        BuffContainer.Buff buff;

        if (string.IsNullOrEmpty(buffId2))
        {
            buff = GetBuffs(container, buffId1).First();
        }
        else
        {
            var b1 = GetBuffs(container, buffId1).First();
            var b2 = GetBuffs(container, buffId2).First();

            buff = b1.amount < b2.amount ? b1 : b2;
        }

        var secondsLeft = SummaryBuffSecondsLeft(containers, buff.id);

        obj.Value.text = $"x{containers.Count}";
        obj.Time.text = GetFormattedTime(secondsLeft);

        UpdateIconsStack(obj.Icons, containers.Count);
    }

    public override void OnDisable()
    {
        Object.Destroy(_canvas.gameObject);
        EventBus.Unsubscribe<WorldUpdateEvent>(OnWorldUpdate);
    }
}