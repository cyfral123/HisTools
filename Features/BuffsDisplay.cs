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

public class BuffsDisplay : FeatureBase
{
    private Canvas _canvas;

    private Transform _grub;
    private Transform _injector;
    private Transform _pills;
    private Transform _foodBar;

    private Image[] _grubIcons;
    private Image[] _injectorIcons;
    private Image[] _pillsIcons;
    private Image[] _foodBarIcons;

    private TextMeshProUGUI _grubValue;
    private TextMeshProUGUI _injectorValue;
    private TextMeshProUGUI _pillsValue;
    private TextMeshProUGUI _foodBarValue;

    private TextMeshProUGUI _grubTime;
    private TextMeshProUGUI _injectorTime;
    private TextMeshProUGUI _pillsTime;
    private TextMeshProUGUI _foodBarTime;

    public BuffsDisplay() : base("BuffsDisplay", "Display all current buff effects")
    {
        
    }

    private void EnsurePrefabs()
    {
        if (_grub && _injector && _pills) return;

        if (PrefabDatabase.Instance.GetObject("histools/UI_BuffsDisplay", true)
            .TryGet(out var prefab))
        {
            var go = Object.Instantiate(prefab);
            _canvas = go.GetComponent<Canvas>();

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var buffs = _canvas.GetComponentInChildren<HorizontalLayoutGroup>();

            _grub = buffs.transform.Find("Grub");
            _injector = buffs.transform.Find("Injector");
            _pills = buffs.transform.Find("Pills");
            _foodBar = buffs.transform.Find("FoodBar");

            _grubIcons = _grub.Find("Icons").GetComponentsInChildren<Image>(true);
            _injectorIcons = _injector.Find("Icons").GetComponentsInChildren<Image>(true);
            _pillsIcons = _pills.Find("Icons").GetComponentsInChildren<Image>(true);
            _foodBarIcons = _foodBar.Find("Icons").GetComponentsInChildren<Image>(true);

            _grubValue = _grub.Find("Value").GetComponent<TextMeshProUGUI>();
            _injectorValue = _injector.Find("Value").GetComponent<TextMeshProUGUI>();
            _pillsValue = _pills.Find("Value").GetComponent<TextMeshProUGUI>();
            _foodBarValue = _foodBar.Find("Value").GetComponent<TextMeshProUGUI>();

            _grubTime = _grub.Find("Time").GetComponent<TextMeshProUGUI>();
            _injectorTime = _injector.Find("Time").GetComponent<TextMeshProUGUI>();
            _pillsTime = _pills.Find("Time").GetComponent<TextMeshProUGUI>();
            _foodBarTime = _foodBar.Find("Time").GetComponent<TextMeshProUGUI>();

            _grub.gameObject.SetActive(false);
            _injector.gameObject.SetActive(false);
            _pills.gameObject.SetActive(false);
            _foodBar.gameObject.SetActive(false);
        }
    }

    public override void OnEnable()
    {
        EventBus.Subscribe<WorldUpdateEvent>(OnWorldUpdate);
    }

    private static float CalculateBuffSecondsLeft(BuffContainer.Buff buff, float loseRate, bool lose)
    {
        if (buff == null || buff.maxAmount <= 0f || float.IsNaN(buff.amount))
            return 0f;

        if (!lose || loseRate <= 0f)
            return float.PositiveInfinity;

        var buffs = Player.GetPlayer().Unwrap().curBuffs;
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
        EnsurePrefabs();
        if (!_injector || !_grub || !_pills || !_foodBar) return;

        if (!Player.GetPlayer().TryGet(out var player))
            return;

        var containers = player.curBuffs.currentBuffs;

        var foodBarContainers = SearchBuff(containers, "roided")
            .Where(container => HaveBuff(container, "pilled"))
            .ToList();

        var roidedContainers = SearchBuff(containers, "roided")
            .Where(container => !HaveBuff(container, "pilled"))
            .ToList();

        var pilledContainers = SearchBuff(containers, "pilled")
            .Where(container => !HaveBuff(container, "roided"))
            .ToList();

        var goopedContainers = SearchBuff(containers, "gooped");


        _grub.gameObject.SetActive(goopedContainers.Count > 0);
        _injector.gameObject.SetActive(roidedContainers.Count > 0);
        _foodBar.gameObject.SetActive(foodBarContainers.Count > 0);
        _pills.gameObject.SetActive(pilledContainers.Count > 0);

        if (goopedContainers.Count > 0)
        {
            var container = goopedContainers.First();
            var buff = GetBuffs(container, "gooped").First();
            var secondsLeft = SummaryBuffSecondsLeft(goopedContainers, buff.id);

            _grubValue.text = $"x{goopedContainers.Count}";
            _grubTime.text = GetFormattedTime(secondsLeft);

            UpdateIconsStack(_grubIcons, goopedContainers.Count);
        }

        if (roidedContainers.Count > 0)
        {
            var container = roidedContainers.First();
            var buff = GetBuffs(container, "roided").First();
            var secondsLeft = SummaryBuffSecondsLeft(roidedContainers, buff.id);

            _injectorValue.text = $"x{roidedContainers.Count}";
            _injectorTime.text = GetFormattedTime(secondsLeft);

            UpdateIconsStack(_injectorIcons, roidedContainers.Count);
        }

        if (pilledContainers.Count > 0)
        {
            var container = pilledContainers.First();
            var buff = GetBuffs(container, "pilled").First();
            var secondsLeft = SummaryBuffSecondsLeft(pilledContainers, buff.id);

            _pillsValue.text = $"x{pilledContainers.Count}";
            _pillsTime.text = GetFormattedTime(secondsLeft);

            UpdateIconsStack(_pillsIcons, pilledContainers.Count);
        }

        if (foodBarContainers.Count > 0)
        {
            var container = foodBarContainers.First();
            var buff1 = GetBuffs(container, "roided").First();
            var buff2 = GetBuffs(container, "pilled").First();
            var buff = buff1.amount < buff2.amount ? buff1 : buff2;
            var secondsLeft = SummaryBuffSecondsLeft(foodBarContainers, buff.id);

            _foodBarValue.text = $"x{foodBarContainers.Count}";
            _foodBarTime.text = GetFormattedTime(secondsLeft);

            UpdateIconsStack(_foodBarIcons, foodBarContainers.Count);
        }
    }

    public override void OnDisable()
    {
        Object.Destroy(_canvas.gameObject);
        EventBus.Unsubscribe<WorldUpdateEvent>(OnWorldUpdate);
    }
}