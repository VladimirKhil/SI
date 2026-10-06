using Avalonia;

namespace SImulator;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Web buttons server resolves its content (wwwroot2) relative to the current directory
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
