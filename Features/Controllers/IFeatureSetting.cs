public interface IFeatureSetting
{
    string Name { get; }
    string Description { get; }
    public object GetValue();
    public void SetValue(object val);
    void ResetToDefault();
}

public interface IFeatureSetting<T> : IFeatureSetting
{
    T Value { get; set; }
    T DefaultValue { get; }
}

