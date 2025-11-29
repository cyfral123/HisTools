namespace Config;

public class Settings
{
    public Settings()
    {
        EventBus.Subscribe<FeatureSettingChangedEvent>(OnFeatureSettingsChanged);
    }

    private void OnFeatureSettingsChanged(FeatureSettingChangedEvent e)
    {
        Debounce.Run(CoroutineRunner.Instance,
            e.Setting.Name,
            2.5f,
            () => Utils.Files.SaveSettingToConfig(e.Feature.Name, e.Setting.Name, e.Setting.GetValue())
        );
    }
}