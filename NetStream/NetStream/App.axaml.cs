using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using NetStream.ViewModels;
using NetStream.Views;
using Serilog;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using System.Runtime;
using Avalonia.Controls;
using LibVLCSharp.Shared;
using NetStream.Language;
using Material.Styles;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using Microsoft.Extensions.DependencyInjection;
using NetStream.Services;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using SukiUI.Models;
using TMDbLib.Objects.General;

namespace NetStream;

public partial class App : Application
{
    public static INativeMediaPlayerService AppNativeVideoPlayerService { get; set; }
    //public static WebSocketServer WebSocketServer { get; private set; }
   // public static WebSocketClient WebSocketClient { get; private set; }

   //public static VideoServer VideoServer { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public static IServiceProvider Services { get; private set; }


    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            DisableAvaloniaDataAnnotationValidation();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                InitializeDesktopApp();
                // desktop.ShutdownRequested += (sender, e) => {
                //     WebSocketServer?.Stop();
                // };
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Critical framework initialization error: " + ex.Message);
            base.OnFrameworkInitializationCompleted();
        }
    }
    
    

    private async void InitializeDesktopApp()
    {
        LogManager.Initialize();
        Log.Information("Application was started.");
        NetStreamEnvironment.Load();
        Core.Initialize( AppDomain.CurrentDomain.BaseDirectory+"\\libvlc\\win-x64");
        FileHelper.CreateImportantDirectories();
        SetDefaultValues();
        SetPrimaryColor();
        LanguageManager.SwitchLanguage();
        MainWindow mainWindow = new MainWindow();

        /*try
        {
            var filesToDownload = await AutoUpdater.GetUpdatesToDownloadAsync();
            if (filesToDownload != null && filesToDownload.Count > 0)
            {
                var updateControl = new NetStream.Views.AutoUpdateControl();
                mainWindow.SetContent(updateControl);
                mainWindow.Show();

                var progress = new Progress<UpdateProgressInfo>(updateControl.UpdateProgress);
                await AutoUpdater.DownloadAndApplyUpdateAsync(filesToDownload, progress);
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"AutoUpdate failed: {ex.Message}");
        }*/

        mainWindow.SetContent(new SplashScreenWindow());
        mainWindow.Show();
        // WebSocketServer = new WebSocketServer();
        await LoadServices();
        
        FirestoreManager.Initialize();
        PaymentManager.Initialize();
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {

            if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.FireStoreEmail) &&
                !String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.FireStorePassword) &&
                !AppSettingsManager.appSettings.SignedOut)
            {
                Log.Information("Logging in");
                Login();
            }
            else
            {
                HandleLogin();
            }
        });
    }


    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
    

    private async void HandleLogin()
    {
        if (AppSettingsManager.appSettings.SignedOut)
        {
            var loginWindow = new LoginPage(false);
            MainWindow.Instance.SetContent(loginWindow);
        }
        else
        {
            if ( !(await FirestoreManager.IsComputerSignedUpBefore()))
            {
                var signUpPage = new SignUpPage();
                MainWindow.Instance.SetContent(signUpPage);
            }
            else
            {
                var loginPage = new LoginPage(false);
                MainWindow.Instance.SetContent(loginPage);
            }
        }
    }

    private async Task LoadServices()
    {
        try
        {
            var destinationFolder =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Jackett\\Indexers");
            if (!FileHelper.IsFirstStart && Directory.Exists(destinationFolder) && Essentials.IsJackettInstalled())
            {
                await Essentials.RunJacketAsync();
                JackettService.Init();
                await Init();
            }
            else
            {
                Essentials.InstallJackett();
                await Essentials.RunJacketAsync();
                JackettService.Init();
                await Init();
            }
        }
        catch (Exception e)
        {
            var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
            Log.Error(errorMessage);
        }
    }
    
    public static void SetPrimaryColor()
    {
        try
        {
            PaletteHelper paletteHelper = new PaletteHelper();
            ITheme theme = null;
            try
            {
                theme = paletteHelper.GetTheme();
            }
            catch (Exception ex)
            {
                return;
            }

            if (theme != null)
            {
                if (AppSettingsManager.appSettings.PrimaryColorRed != null)
                {
                    Color current = new Color(
                        (byte)AppSettingsManager.appSettings.PrimaryColorAlpha,
                        (byte)AppSettingsManager.appSettings.PrimaryColorRed,
                        (byte)AppSettingsManager.appSettings.PrimaryColorGreen,
                        (byte)AppSettingsManager.appSettings.PrimaryColorBlue);

                    App.Current.Resources["ColorDefault"] = current;
                    theme.SetPrimaryColor(current);
                }
                else
                {
                    var defaultColor = (Color)App.Current.Resources["ColorDefault"];
                    theme.SetPrimaryColor(defaultColor);
                    AppSettingsManager.appSettings.PrimaryColorAlpha = (byte)defaultColor.A;
                    AppSettingsManager.appSettings.PrimaryColorRed = (byte)defaultColor.R;
                    AppSettingsManager.appSettings.PrimaryColorGreen = (byte)defaultColor.G;
                    AppSettingsManager.appSettings.PrimaryColorBlue = (byte)defaultColor.B;
                    AppSettingsManager.SaveAppSettings();
                }

                paletteHelper.SetTheme(theme);
            }
            else
            {
                // Tema oluşturulamadıysa varsayılan renk ayarlamalarını yap
                SetDefaultColor();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }


    private static void SetDefaultColor()
    {
        try
        {
            // Sadece uygulama kaynaklarında renk değişimi yap (tema olmadan)
            var defaultColor = new Color(255, 229, 9, 20); // #FFE50914 - Netflix Red

            App.Current.Resources["ColorDefault"] = defaultColor;
            App.Current.Resources["BrushDefault"] = new SolidColorBrush(defaultColor);

            // Ayarları kaydet
            if (AppSettingsManager.appSettings.PrimaryColorRed == null)
            {
                AppSettingsManager.appSettings.PrimaryColorAlpha = (byte)defaultColor.A;
                AppSettingsManager.appSettings.PrimaryColorRed = (byte)defaultColor.R;
                AppSettingsManager.appSettings.PrimaryColorGreen = (byte)defaultColor.G;
                AppSettingsManager.appSettings.PrimaryColorBlue = (byte)defaultColor.B;
                AppSettingsManager.SaveAppSettings();
            }

            Log.Information("Set default theme color");
        }
        catch (Exception ex)
        {
            Log.Error($"Error setting default color: {ex.Message}");
        }
    }

    public static List<string> SupportedProgramLanguages = new List<string>()
    {
        "en", "tr", "it", "af", "ar", "az", "be", "bg", "cn", "cs", "da", "de", "el", "es", "et", "fa", "fi", "fr",
        "ga", "hi",
        "hr", "hu", "hy", "ja", "ka", "kk", "ky", "la", "mg", "mk", "no", "pt", "ro", "ru", "sk", "sl", "sq", "sr",
        "sv", "tk",
        "ug", "uk", "uz", "vi"
    };

    public static bool isClosing = true;

    private static void SetDefaultValues()
    {
        SetLanguage();
        var jacketApiUrl = NetStreamEnvironment.GetString("NETSTREAM_JACKET_API_URL");
        if (!String.IsNullOrWhiteSpace(jacketApiUrl))
        {
            AppSettingsManager.appSettings.JacketApiUrl = jacketApiUrl;
        }
        else if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiUrl))
        {
            AppSettingsManager.appSettings.JacketApiUrl = "http://127.0.0.1:9117/";
            AppSettingsManager.SaveAppSettings();
        }

        var jacketApiKey = NetStreamEnvironment.GetString("NETSTREAM_JACKET_API_KEY");
        if (!String.IsNullOrWhiteSpace(jacketApiKey))
        {
            AppSettingsManager.appSettings.JacketApiKey = jacketApiKey;
        }
        else if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiKey))
        {
            Log.Warning("NETSTREAM_JACKET_API_KEY is not configured.");
        }
    }

    private static async void SetLanguage()
    {
        var languages = await Service.InitLanguages();
        if (languages != null && languages.Count > 0)
        {
            CultureInfo ci = CultureInfo.InstalledUICulture;
            var match = languages.FirstOrDefault(x => x.Iso_639_1 == ci.TwoLetterISOLanguageName);
            var englishName = match != null ? match.EnglishName : "English";

            // For debugging
          

            // Set TMDB result language
            if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.TmdbResultLanguage))
            {
                AppSettingsManager.appSettings.TmdbResultLanguage = englishName;
                AppSettingsManager.appSettings.IsoTmdbResultLanguage = ci.TwoLetterISOLanguageName;
                Service.language = ci.TwoLetterISOLanguageName;
                AppSettingsManager.SaveAppSettings();
            }
            else
            {
                Service.language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;
            }

            // Set program language
            if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.ProgramLanguage))
            {
                if (match != null && SupportedProgramLanguages.Any(x => x == match.Iso_639_1))
                {
                    AppSettingsManager.appSettings.ProgramLanguage = englishName;
                    AppSettingsManager.appSettings.IsoProgramLanguage = ci.TwoLetterISOLanguageName;
                    AppSettingsManager.SaveAppSettings();
                }
                else
                {
                    AppSettingsManager.appSettings.ProgramLanguage = "English";
                    AppSettingsManager.appSettings.IsoProgramLanguage = "en";
                    AppSettingsManager.SaveAppSettings();
                }
            }
            

            // Set subtitle language
            if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.SubtitleLanguage))
            {
                if (SubtitleHandler.LanguagesToOpenSubtitles.Select(x => x.Key).Any(z => z == match?.Iso_639_1))
                {
                    AppSettingsManager.appSettings.SubtitleLanguage = englishName;
                    AppSettingsManager.appSettings.IsoSubtitleLanguage = ci.TwoLetterISOLanguageName;
                    AppSettingsManager.SaveAppSettings();
                }
                else
                {
                    AppSettingsManager.appSettings.SubtitleLanguage = "Disabled";
                    AppSettingsManager.appSettings.IsoSubtitleLanguage = "Disabled";
                    AppSettingsManager.SaveAppSettings();
                }
            }
        }
        else
        {
            // Set default values if languages can't be loaded
            if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.ProgramLanguage))
            {
                AppSettingsManager.appSettings.ProgramLanguage = "English";
                AppSettingsManager.appSettings.IsoProgramLanguage = "en";
            }

            if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.TmdbResultLanguage))
            {
                AppSettingsManager.appSettings.TmdbResultLanguage = "English";
                AppSettingsManager.appSettings.IsoTmdbResultLanguage = "en";
            }

            if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.SubtitleLanguage))
            {
                AppSettingsManager.appSettings.SubtitleLanguage = "English";
                AppSettingsManager.appSettings.IsoSubtitleLanguage = "en";
            }

            AppSettingsManager.SaveAppSettings();
        }
    }

    public static async Task Init()
    {
        var tasks = new List<Task>()
        {
            Service.client.GetConfigAsync(),
            SubtitleHandler.Init(),
            LoadAccountPage()
        };
        await Task.WhenAll(tasks);
    }

    public static async Task LoadAccountPage()
    {
        // if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.TmdbUsername) &&
        //     !String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.TmdbPassword))
        // {
        //     var loginResult = await Service.Login(AppSettingsManager.appSettings.TmdbUsername,
        //         AppSettingsManager.appSettings.TmdbPassword);
        //     if (loginResult)
        //     {
        //         // Create AccountPage instance and make sure it persists
        //         if (AccountPage.GetAccountPageInstance == null)
        //         {
        //             await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        //             {
        //                 var accountPage = new AccountPage();
        //                 // AccountPage constructor already sets GetAccountPageInstance = this
        //             });
        //         }
        //
        //         Log.Information("Logged in to TMDB account");
        //     }
        // }
        // else
        // {
        //     Log.Error("There is no TMDB account please log in from profile page");
        // }
    }

    private async void Login()
    {
        var result = await FirestoreManager.SignIn(AppSettingsManager.appSettings.FireStoreEmail, AppSettingsManager.appSettings.FireStorePassword);
        if (result != null)
        {
            if (result.Success)
            {
                var isUserRegistered =
                    await FirestoreManager.IsUserRegistered(AppSettingsManager.appSettings.FireStoreEmail);
                if (!isUserRegistered)
                {
                    await FirestoreManager.Register(new SubPlan() { PlanName = "lifetime" });
                }

                var loginResult = await FirestoreManager.IsValidLogin();
                if (loginResult.Success)
                {
                    MainWindow.Instance.SetContent(new MainView());
                }
            }
            else
            {
                var loginWindow = new LoginPage(false);
                MainWindow.Instance.SetContent(loginWindow);
            }
        }
    }
}
