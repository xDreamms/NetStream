using System;
using System.Threading.Tasks;
using Avalonia;
using NetStream;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace NetStream.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        try
        {
            // Update checking is now handled inside App.axaml.cs (InitializeDesktopApp)
        }
        catch
        {
        }

        // AOT derlemesi için TMDbLib türlerinin metadata'sını koru
        NativeAOTWorkarounds.KeepTMDbLibTypes();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>();
        
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    }
        
}