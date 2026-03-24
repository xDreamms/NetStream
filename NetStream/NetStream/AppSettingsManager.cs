using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using NETCore.Encrypt;
using NetStream;
using NetStream.Views;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

public static class AppSettingsManager
{
    public static AppSettings appSettings = new AppSettings();
    private const string SettingsKey = "netstream_settings";
    
    // Default path for desktop if not set explicitly
    private static string _defaultAppSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "NetStream", 
        "appSettings.json");
    
    // Property with default value
    public static string AppSettingsPath 
    { 
        get => _appSettingsPath ?? _defaultAppSettingsPath; 
        set => _appSettingsPath = value; 
    }
    private static string _appSettingsPath;

    static AppSettingsManager()
    {
        // No auto-loading in constructor - let the application control when to load
    }

    public static void ApplyEnvironmentOverrides()
    {
        NetStreamEnvironment.Load();

        var jacketApiUrl = NetStreamEnvironment.GetString("NETSTREAM_JACKET_API_URL");
        if (!string.IsNullOrWhiteSpace(jacketApiUrl))
        {
            appSettings.JacketApiUrl = jacketApiUrl;
        }

        var jacketApiKey = NetStreamEnvironment.GetString("NETSTREAM_JACKET_API_KEY");
        if (!string.IsNullOrWhiteSpace(jacketApiKey))
        {
            appSettings.JacketApiKey = jacketApiKey;
        }

        var openSubtitlesApiKey = NetStreamEnvironment.GetString("NETSTREAM_OPENSUBTITLES_API_KEY");
        if (!string.IsNullOrWhiteSpace(openSubtitlesApiKey))
        {
            appSettings.OpenSubtitlesApiKey = openSubtitlesApiKey;
        }
    }

    // For backward compatibility with older code
    public static void GetAppSettings()
    {
        LoadAppSettings();
    }

    public static void LoadAppSettings()
    {
        try
        {
            if (IsMobileOrWeb())
            {
                try
                {
                    var settingsJson = GetPreference(SettingsKey, "{}");
                    Console.WriteLine($"Mobile: Settings JSON retrieved, length: {settingsJson?.Length ?? 0}");
                    
                    if (!string.IsNullOrEmpty(settingsJson) && settingsJson != "{}")
                    {
                        try
                        {
                            var loadedSettings = JsonSerializer.Deserialize<AppSettings>(settingsJson);
                            if (loadedSettings != null)
                            {
                                appSettings = loadedSettings;
                                Console.WriteLine("Settings successfully loaded from preferences");
                                return;
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            Console.WriteLine($"Error deserializing settings JSON: {jsonEx.Message}");
                            // Continue to default settings
                        }
                    }
                    
                    Console.WriteLine("No valid settings found in preferences, initializing defaults");
                    InitializeDefaultSettings();
                }
                catch (Exception mobileEx)
                {
                    Console.WriteLine($"Mobile-specific error loading settings: {mobileEx.Message}");
                    InitializeDefaultSettings();
                }
            }
            else
            {
                if (File.Exists(AppSettingsPath))
                {
                    try
                    {
                        var encrypted = File.ReadAllText(AppSettingsPath);
                        if (!String.IsNullOrWhiteSpace(encrypted))
                        {
                            var contents = EncryptProvider.AESDecrypt(encrypted, Encryptor.Key, Encryptor.IV);
                            var loadedSettings = JsonConvert.DeserializeObject<AppSettings>(contents);
                            if (loadedSettings != null)
                            {
                                appSettings = loadedSettings;
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error decrypting settings: {ex.Message}");
                    }
                }
                
                Console.WriteLine("Settings file not found or invalid, initializing defaults");
                InitializeDefaultSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical error loading app settings: {ex.Message}");
            // Ensure we always have valid settings by initializing defaults in case of any error
            try
            {
                InitializeDefaultSettings();
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"Even fallback settings initialization failed: {fallbackEx.Message}");
                // Last resort: manually create minimal settings to prevent null reference exceptions
                appSettings = new AppSettings();
            }
        }
    }

    public static async Task SaveAppSettingsAsync()
    {
        try
        {
            Console.WriteLine("Starting to save app settings...");

            if (IsMobileOrWeb())
            {
                // Save to preferences on mobile/web
                string settingsJson = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"Serialized settings JSON length: {settingsJson?.Length ?? 0}");
                
                // Adım adım kaydetme işlemini yapalım
                await SetPreferenceAsync(SettingsKey, settingsJson);
                Console.WriteLine("Settings saved to preferences asynchronously");
                
                // Doğrulama yapalım
                var verifyJson = GetPreference(SettingsKey, "");
                Console.WriteLine($"Verification - Saved settings size: {verifyJson?.Length ?? 0} chars");
                
                if (string.IsNullOrEmpty(verifyJson) || verifyJson.Length < 10)
                {
                    // İlk denemede kaydedilemedi, tekrar deneyelim
                    Console.WriteLine("First save attempt failed, retrying...");
                    await Task.Delay(100); // Kısa bir bekleme
                    await SetPreferenceAsync(SettingsKey, settingsJson);
                    
                    // Tekrar doğrulama
                    verifyJson = GetPreference(SettingsKey, "");
                    Console.WriteLine($"Second verification - Saved settings size: {verifyJson?.Length ?? 0} chars");
                }
            }
            else
            {
                // Make sure directory exists
                string directory = Path.GetDirectoryName(AppSettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Save to file for desktop
                File.WriteAllText(AppSettingsPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(appSettings), Encryptor.Key, Encryptor.IV));
                Console.WriteLine("Settings saved to encrypted file");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving app settings: {ex.Message}");
        }
    }

    // Geriye dönük uyumluluk için senkron metodu koruyalım
    public static void SaveAppSettings()
    {
        try
        {
            if (IsMobileOrWeb())
            {
                var task = Task.Run(async () => await SaveAppSettingsAsync());
                task.Wait();
            }
            else
            {
                string directory = Path.GetDirectoryName(AppSettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(AppSettingsPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(appSettings), Encryptor.Key, Encryptor.IV));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in sync SaveAppSettings: {ex.Message}");
        }
    }

    // Cross-platform method to initialize default settings for all platforms
    public static void InitializeDefaultSettings()
    {
        appSettings = new AppSettings
        {
            // General settings
            TmdbResultLanguage = "",
            IsoTmdbResultLanguage = "",
            ProgramLanguage = "",
            IsoProgramLanguage = "",
            SubtitleLanguage = "",
            IsoSubtitleLanguage = "",
            
            // Theme settings
            PrimaryColorAlpha = 255,
            PrimaryColorRed = 229,
            PrimaryColorGreen = 9,
            PrimaryColorBlue = 20,
            
            // API settings
            JacketApiUrl = "",
            JacketApiKey = "",
            
            // User settings
            FireStoreEmail = "",
            FireStorePassword = "",
            TmdbUsername = "",
            TmdbPassword = "",
            FireStoreDisplayName = "",
            FireStoreProfilePhotoName = "",
            OpenSubtitlesApiKey = "",
            SignedOut = false,
            Verified = false,
            
            // File paths - less relevant for mobile but included for consistency
            TorrentsPath = "",
            MoviesPath = "",
            SubtitlesPath = "",
            VolumeCachePath = "",
            YoutubeVideoPath = "",
            downloadingTorrentsJson = "",
            SubtitleInfoPath = "",
            ThumbnailCachesPath = "",
            IndexersPath = "",
            
            // Player settings
            PlayerSettingAutoSync = true,
            PlayerSettingShowThumbnail = true,
            AutoSyncSubtitles = true,
            ShowThumbnails = true,
            
            // Watch Now settings
            WatchNowDefaultQuality = "1080p",
            
        };
        
        SaveAppSettings();
    }

    private static bool IsMobileOrWeb()
    {
        return PlatformDetector.IsMobile() || PlatformDetector.IsWeb();
    }

    // Platform-specific preference storage methods
    private static string GetPreference(string key, string defaultValue)
    {
        try
        {
#if ANDROID
            try
            {
                // Android implementation using SharedPreferences
                var context = Android.App.Application.Context;
                if (context == null)
                {
                    Console.WriteLine("Android Context is null, cannot access preferences");
                    return defaultValue;
                }
                
                var prefs = context.GetSharedPreferences("NetStreamPrefs", Android.Content.FileCreationMode.Private);
                if (prefs == null)
                {
                    Console.WriteLine("Failed to get SharedPreferences");
                    return defaultValue;
                }
                
                var value = prefs.GetString(key, defaultValue);
                Console.WriteLine($"Android: GetPreference {key}, value length: {value?.Length ?? 0}");
                return value ?? defaultValue;
            }
            catch (Exception androidEx)
            {
                Console.WriteLine($"Android GetPreference error: {androidEx.Message}");
                return defaultValue;
            }
#elif IOS
            // iOS implementation using NSUserDefaults
            var defaults = Foundation.NSUserDefaults.StandardUserDefaults;
            var value = defaults.StringForKey(key);
            return value ?? defaultValue;
#else
            // Fallback for web or other platforms - using local storage or memory
            // For this example, we'll use a simple in-memory approach
            return _inMemoryPrefs.TryGetValue(key, out var value) ? value : defaultValue;
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetPreference: {ex.Message}");
            return defaultValue;
        }
    }

    // Asenkron SharedPreferences kaydetme
    private static async Task SetPreferenceAsync(string key, string value)
    {
        try
        {
#if ANDROID
            await Task.Run(() => {
                try
                {
                    // Android implementation using SharedPreferences
                    var context = Android.App.Application.Context;
                    if (context == null)
                    {
                        Console.WriteLine("Android Context is null, cannot set preferences");
                        return;
                    }
                    
                    var prefs = context.GetSharedPreferences("NetStreamPrefs", Android.Content.FileCreationMode.Private);
                    if (prefs == null)
                    {
                        Console.WriteLine("Failed to get SharedPreferences for writing");
                        return;
                    }
                    
                    var editor = prefs.Edit();
                    if (editor == null)
                    {
                        Console.WriteLine("SharedPreferences editor is null");
                        return;
                    }
                    
                    // Büyük veri setleri için daha güvenilir olduğu için Apply() kullanıyoruz
                    editor.PutString(key, value);
                    editor.Apply();
                    
                    // Apply() asenkron olduğu için kısa bir süre bekleyelim
                    Thread.Sleep(200);
                    
                    // İşlemin başarılı olduğundan emin olmak için değeri okumayı deneyelim
                    var verifyValue = prefs.GetString(key, null);
                    if (verifyValue != null)
                    {
                        Console.WriteLine($"Android: SetPreferenceAsync {key}, value applied successfully, length: {verifyValue.Length}");
                    }
                    else
                    {
                        // Apply başarısız oldu, Commit() ile tekrar deneyelim
                        Console.WriteLine("Apply failed, trying Commit...");
                        editor = prefs.Edit();
                        editor.PutString(key, value);
                        var committed = editor.Commit();
                        Console.WriteLine($"Android: SetPreferenceAsync fallback commit {key}, committed: {committed}");
                    }
                }
                catch (Exception androidEx)
                {
                    Console.WriteLine($"Android SetPreferenceAsync error: {androidEx.Message}");
                }
            });
#elif IOS
            await Task.Run(() => {
                // iOS implementation using NSUserDefaults
                var defaults = Foundation.NSUserDefaults.StandardUserDefaults;
                defaults.SetString(value, key);
                defaults.Synchronize();
            });
#else
            // Fallback for web or other platforms
            await Task.Run(() => {
                _inMemoryPrefs[key] = value;
            });
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SetPreferenceAsync: {ex.Message}");
        }
    }

    // Simple in-memory storage for platforms without native preference storage
    private static readonly Dictionary<string, string> _inMemoryPrefs = new Dictionary<string, string>();
}
