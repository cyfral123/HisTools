using System;
using UnityEngine;

public readonly struct WorldUpdateEvent(WorldLoader __instance)
{
    public readonly WorldLoader World = __instance;
}

public readonly struct EnterLevelEvent(M_Level level)
{
    public readonly M_Level Level = level;
}

public readonly struct PlayerLateUpdateEvent(ENT_Player __instance, Vector3 velocity)
{
    public readonly ENT_Player Player = __instance;
    public readonly Vector3 Vel = velocity;
}

public readonly struct GameStartEvent() { }

public readonly struct ToggleRouteEvent(string uid, bool show)
{
    public readonly string Uid = uid;
    public readonly bool Show = show;
}

public readonly struct FeatureToggleEvent(IFeature feature, bool enabled)
{
    public readonly IFeature Feature = feature;
    public readonly bool Enabled = enabled;
}

public readonly struct FeatureSettingsMenuToggleEvent(IFeature feature)
{
    public readonly IFeature Feature = feature;
}

public readonly struct MenuVisibleChangedEvent(bool isMenuVisible)
{
    public readonly bool IsMenuVisible = isMenuVisible;
}

public readonly struct FeatureSettingChangedEvent(IFeatureSetting setting, IFeature feature)
{
    public readonly IFeatureSetting Setting = setting;
    public readonly IFeature Feature = feature;
}

public readonly struct EntitySpawnChanceEvent(UT_SpawnChance spawnChance)
{
    public readonly UT_SpawnChance SpawnChance = spawnChance;
}

public readonly struct LevelChangedEvent(M_Level lastLevel, M_Level currentLevel, TimeSpan lastTimeSpan, TimeSpan currentTimeSpan)
{
    public readonly M_Level LastLevel = lastLevel;
    public readonly M_Level CurrentLevel = currentLevel;
    public readonly TimeSpan LastTimeSpan = lastTimeSpan;
    public readonly TimeSpan CurrentTimeSpan = currentTimeSpan;
}

public readonly struct SettingsPanelShouldRefreshEvent() { }