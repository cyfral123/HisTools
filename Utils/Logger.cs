using BepInEx.Logging;

namespace Utils;

public static class Logger
{
    private static readonly ManualLogSource s_log;

    public static void Info(string msg) => s_log.Log(LogLevel.Info, msg);
    public static void Debug(string msg) => s_log.Log(LogLevel.Debug, msg);
    public static void Warn(string msg) => s_log.Log(LogLevel.Warning, msg);
    public static void Error(string msg) => s_log.Log(LogLevel.Error, msg);

    static Logger()
    {
        s_log = BepInEx.Logging.Logger.CreateLogSource(Plugin.Name);
    }
}
