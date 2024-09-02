using System.IO;
using log4net.Config;
using log4net;

namespace WechatArticleSearcher.Utils;

public static class Logging
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(App));

    public static void Write(string message, string filePath = @"E:\Home\Temp Files\log.txt")
    {
        if (!File.Exists(filePath)) File.Create(filePath).Close();

        using (var sw = new StreamWriter(filePath, true))
        {
            sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sw.WriteLine(message);
            sw.WriteLine("\n--------------------------------\n");
        }
    }

    public static void Init()
    {
        var log4NetConfig = new FileInfo("log4net.config");
        XmlConfigurator.Configure(log4NetConfig);
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
    }

    private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.ExceptionObject);
    }

    public static void Info(string message)
    {
        Log.Info(message);
    }

    public static void Error(string message)
    {
        Log.Error(message);
    }

    public static void Warn(string message)
    {
        Log.Warn(message);
    }

    public static void Fatal(string message)
    {
        Log.Fatal(message);
    }

    public static void Debug(string message)
    {
        Log.Debug(message);
    }

    public static void Response(string message)
    {
        Log.Info(message);
    }
}