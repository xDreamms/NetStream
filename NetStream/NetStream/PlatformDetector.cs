using System;

namespace NetStream;
public enum Platform
{
    Windows,
    Linux,
    Mac,
    iOS,
    Android,
    Web
}
public static class PlatformDetector
{
    public static Platform GetPlatform()
    {
        if (OperatingSystem.IsBrowser())
            return Platform.Web;
        if (OperatingSystem.IsWindows())
            return Platform.Windows;
        if (OperatingSystem.IsLinux())
            return Platform.Linux;
        if (OperatingSystem.IsMacOS())
            return Platform.Mac;
        if (OperatingSystem.IsIOS())
            return Platform.iOS;
        if (OperatingSystem.IsAndroid())
            return Platform.Android;

        throw new NotSupportedException("Unknown platform");
    }

    public static bool IsMobile()
    {
        return GetPlatform() == Platform.iOS || GetPlatform() == Platform.Android;
    }
    
    public static bool IsDesktop()
    {
        return GetPlatform() == Platform.Windows || GetPlatform() == Platform.Linux || GetPlatform() == Platform.Mac;
    }
    
    public static bool IsWeb()
    {
        return GetPlatform() == Platform.Web;
    }

}