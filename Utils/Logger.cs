using BepInEx.Logging;

namespace HisTools.Utils;

public static class Logger
{
    private static readonly ManualLogSource PluginSource;

    public static void Info(string msg) => PluginSource.Log(LogLevel.Info, msg);
    public static void Debug(string msg) => PluginSource.Log(LogLevel.Debug, msg);
    public static void Warn(string msg) => PluginSource.Log(LogLevel.Warning, msg);
    public static void Error(string msg) => PluginSource.Log(LogLevel.Error, msg);

    static Logger()
    {
        PluginSource = BepInEx.Logging.Logger.CreateLogSource(Constants.PluginName);
    }
}