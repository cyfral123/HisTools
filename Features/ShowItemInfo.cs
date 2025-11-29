using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

public class ShowItemInfo : FeatureBase
{
    private GameObject _itemInfoPrefab = null;
    private Transform _playerTransform = null;

    public ShowItemInfo() : base("ShowItemInfo", "Show spawn chances")
    {
        AddSetting(new BoolSetting(this, "Color from palette", "Prefer color from accent palette", true));
        AddSetting(new BoolSetting(this, "Item name", "Show item text label", true));
        AddSetting(new BoolSetting(this, "Spawn chance", "Show item spawn chance", true));
        AddSetting(new FloatSliderSetting(this, "Label size", "...", 0.5f, 0.1f, 2f, 0.05f, 2));
        AddSetting(new ColorSetting(this, "Label color", "...", Color.gray));
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

    private void EnsurePrefabs()
    {
        EnsurePlayer();
        if (_itemInfoPrefab == null)
        {
            _itemInfoPrefab = new GameObject($"HisTools_ItemInfo_Prefab");
            var tmp = _itemInfoPrefab.AddComponent<TextMeshPro>();
            tmp.text = "ItemInfo";
            tmp.fontSize = GetSetting<FloatSliderSetting>("Label size").Value;
            tmp.color = GetSetting<ColorSetting>("Label color").Value;
            tmp.alignment = TextAlignmentOptions.Center;
            var look = tmp.AddComponent<LookAtPlayer>();
            look.player = _playerTransform;
            _itemInfoPrefab.SetActive(false);
        }
    }

    public override void OnEnable()
    {
        EventBus.Subscribe<EnterLevelEvent>(OnEnterLevel);
    }

    public override void OnDisable()
    {
        EventBus.Unsubscribe<EnterLevelEvent>(OnEnterLevel);
    }

    private void OnEnterLevel(EnterLevelEvent e)
    {
        EnsurePrefabs();

        var entities = e.Level.GetComponentsInChildren<GameEntity>();
        if (entities != null)
        {
            foreach (var entity in entities)
            {
                var spawnchance = entity.GetComponent<UT_SpawnChance>();
                if (spawnchance != null && spawnchance.spawnSettings != null)
                {
                    var label = GameObject.Instantiate(_itemInfoPrefab, entity.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    var tmp = label.GetComponent<TextMeshPro>();
                    var finalText = new StringBuilder();

                    if (GetSetting<BoolSetting>("Color from palette").Value)
                        tmp.color = Palette.FromHtml(Plugin.AccentHtml.Value);
                    if (GetSetting<BoolSetting>("Item name").Value)
                        finalText.Append(entity.name).Append(" - ");
                    if (GetSetting<BoolSetting>("Spawn chance").Value)
                        finalText.Append(spawnchance.spawnSettings.GetEffectiveSpawnChance() * 100f).Append("%");
                    
                    tmp.fontSize = GetSetting<FloatSliderSetting>("Label size").Value;
                    tmp.text = finalText.ToString();
                    label.SetActive(true);
                }
            }
        }
    }
}