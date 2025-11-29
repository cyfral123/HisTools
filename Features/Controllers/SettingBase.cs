public abstract class SettingBase<T>(IFeature feature, string name, string description, T defaultValue) : IFeatureSetting<T>
{
    public IFeature Feature { get; } = feature;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public virtual T Value { get; set; } = defaultValue;
    public T DefaultValue { get; } = defaultValue;

    public void ResetToDefault()
    {
        if (Value.Equals(DefaultValue)) return;
        
        this.Value = DefaultValue;
        Utils.Logger.Info($"Reset '{Name}' to default value: {DefaultValue}");
    }
    public object GetValue() => Value;
    public void SetValue(object val) => Value = (T)val;
}