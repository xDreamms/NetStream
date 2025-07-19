using NetStream.Views;
using System.Configuration;
using System.Data;
using System.IO;
using System.Runtime;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Serilog;
using NetStream.Language;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Controls;
using IWshRuntimeLibrary;
using Newtonsoft.Json;
using File = System.IO.File;
using System.Windows.Documents;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            //Libtorrent.client.Clear();
            //Essentials.KillJackett();
            //var youtubePath = AppSettingsManager.appSettings.YoutubeVideoPath;
            //if (Directory.Exists(youtubePath))
            //{
            //    var directoryInfo = new DirectoryInfo(youtubePath);
            //    foreach (var file in directoryInfo.GetFiles())
            //    {
            //        if (file.FullName != MovieDetailsPage.currentMediaFile)
            //        {
            //            file.Delete();
            //        }
            //    }
            //}
        }



        private SplashScreenWindow splashWindow;
        public async void App_OnStartup(object sender, StartupEventArgs e)
        {
            string data = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            ProfileOptimization.SetProfileRoot(data);
            ProfileOptimization.StartProfile("Startup.Profile");

            LogManager.Initialize();
            Log.Information("Application was started.");

            if (Application.ResourceAssembly == null)
            {
                Application.ResourceAssembly = Assembly.GetExecutingAssembly();
            }

            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var resources = Application.Current.Resources;
           

            dictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/HandyControl;component/Themes/skindark.xaml")
            });

            dictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/HandyControl;component/Themes/Theme.xaml")
            });

            dictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml")
            });

            dictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml")
            });

            AddBrushIfMissing(resources, "MaterialDesign.Brush.Primary.Light", Colors.LightBlue);
            AddBrushIfMissing(resources, "MaterialDesign.Brush.Primary.Light.Foreground", Colors.Black);
            AddBrushIfMissing(resources, "MaterialDesign.Brush.Primary", (Color)ColorConverter.ConvertFromString("#673AB7"));  // DeepPurple
            AddBrushIfMissing(resources, "MaterialDesign.Brush.Primary.Foreground", Colors.White);
            AddBrushIfMissing(resources, "MaterialDesign.Brush.Primary.Dark", (Color)ColorConverter.ConvertFromString("#512D6D"));  // Darker Purple
            AddBrushIfMissing(resources, "MaterialDesign.Brush.Primary.Dark.Foreground", Colors.White);

            AddBrushIfMissing(resources, "MaterialDesign.Brush.Secondary.Light", Colors.LightGreen);
            AddBrushIfMissing(resources, "MaterialDesign.Brush.Secondary.Light.Foreground", Colors.Black);
            AddBrushIfMissing(resources, "MaterialDesign.Brush.Secondary", (Color)ColorConverter.ConvertFromString("#3D7E05"));  // Lime
            AddBrushIfMissing(resources, "MaterialDesign.Brush.Secondary.Foreground", Colors.White);
            AddBrushIfMissing(resources, "MaterialDesign.Brush.Secondary.Dark", (Color)ColorConverter.ConvertFromString("#A4B42B"));  // Darker Lime
            AddBrushIfMissing(resources, "MaterialDesign.Brush.Secondary.Dark.Foreground", Colors.White);

            var fontUri = new Uri("pack://application:,,,/NetStream;component/Fonts/");

            resources.Add("Gotham-Light", new Style(typeof(TextBlock))
            {
                Setters = {
                    new Setter(
                        TextElement.FontFamilyProperty,
                        new FontFamily(fontUri, "./#Gotham Light")
                    )
                }
            });

            resources.Add("Gotham-Extra-Light", new Style(typeof(TextBlock))
            {
                Setters = {
                    new Setter(
                        TextElement.FontFamilyProperty,
                        new FontFamily(fontUri, "./#Gotham ExtraLight")
                    )
                }
            });
            resources.Add("Gotham-Medium", new Style(typeof(TextBlock))
            {
                Setters = {
                    new Setter(
                        TextElement.FontFamilyProperty,
                        new FontFamily(fontUri, "./#Gotham Medium")
                    )
                }
            });
            resources.Add("Gotham-Thin", new Style(typeof(TextBlock))
            {
                Setters = {
                    new Setter(
                        TextElement.FontFamilyProperty,
                        new FontFamily(fontUri, "./#Gotham Thin")
                    )
                }
            });
            resources.Add("Gotham", new Style(typeof(TextBlock))
            {
                Setters = {
                    new Setter(
                        TextElement.FontFamilyProperty,
                        new FontFamily(fontUri, "./#Gotham")
                    )
                }
            });

          

            // Color ve SolidColorBrush kaynakları
            var colorDefault = new Color { A = 255, R = 229, G = 9, B = 20 };
            resources.Add("ColorDefault", colorDefault);
            resources.Add("BrushDefault", new SolidColorBrush(colorDefault));

            FileHelper.CreateImportantDirectories();

            //CreateShortCut();

            var currentProcess = Process.GetCurrentProcess();
            var existingProcesses = Process.GetProcessesByName(currentProcess.ProcessName);

            if (existingProcesses.Length > 1)
            {
                Application.Current.Shutdown();
                return; 
            }

            if (!(await Updater.IsLatestVersion()))
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "NetStreamUpdater.full.exe",
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = true,  // Yönetici yetkisi için true olmalı
                    Verb = "runas"           // Yönetici olarak çalıştır
                };
                Process.Start(processStartInfo);
                Application.Current.Shutdown();
            }
            else
            {
                // AppDomain için global hata yakalayıcı
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    LogException(args.ExceptionObject as Exception);
                    args = new UnhandledExceptionEventArgs(null, false);
                };

                // Dispatcher (UI) için global hata yakalayıcı
                DispatcherUnhandledException += (sender, args) =>
                {
                    LogException(args.Exception);
                    args.Handled = true; // Hatanın fırlatılmasını durdur
                };

                // Task bazlı hatalar için
                TaskScheduler.UnobservedTaskException += (sender, args) =>
                {
                    LogException(args.Exception);
                    args.SetObserved(); // Hatanın "görülmemiş" olarak işaretlenmesini engelle
                };

                SetDefaultValues();
                SetPrimaryColor();
                LanguageManager.SwitchLanguage();
                splashWindow = new SplashScreenWindow();
                splashWindow.Show();

                FirestoreManager.Initialize();
                PaymentManager.Initialize();
                
               
                Load();

                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.MainWindow = mainWindow;
                Log.Information("Opened Main Window");
                splashWindow.Close();


                //if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.FireStoreEmail) &&
                //    !String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.FireStorePassword)
                //    && !AppSettingsManager.appSettings.SignedOut)
                //{
                //    Log.Information("Logging in");
                //    Login();
                //}
                //else
                //{
                //    if (AppSettingsManager.appSettings.SignedOut)
                //    {
                //        new LoginWindow(PageType.SignIn).Show();
                //        splashWindow.Close();
                //    }
                //    else
                //    {
                //        Log.Information("Login credentials was empty. Opening SingUp page...");
                //        new LoginWindow(PageType.SignUp).Show();
                //        splashWindow.Close();
                //    }
                //}
            }
        }
        private void AddBrushIfMissing(ResourceDictionary resources, string key, Color color)
        {
            if (!resources.Contains(key))
            {
                resources[key] = new SolidColorBrush(color);
            }
        }
        //private void CreateShortCut()
        //{
           
        //    string currentDirectory = Environment.CurrentDirectory;
        //    string corePath = Path.Combine(currentDirectory, "NetStream.Core.exe");
        //    if (File.Exists(corePath))
        //    {
        //        File.Delete(corePath);
        //        string exePath = Path.Combine(currentDirectory, "NetStream.exe");

        //        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        //        // Kısayol dosyasının hedef yolu
        //        string shortcutPath = Path.Combine(desktopPath, "NetStream.lnk");

        //        if (File.Exists(shortcutPath))
        //        {
        //            File.Delete(shortcutPath);
        //        }

        //        try
        //        {
        //            var wshShell = new WshShell();
        //            var shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutPath);

        //            shortcut.TargetPath = exePath;
        //            shortcut.WorkingDirectory = currentDirectory;
        //            shortcut.Description = "NetStream";
        //            shortcut.Save();
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error($"{ex.Message}");
        //        }
        //    }

            
        //}

        private void EncryptSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);

            ConfigurationSection section = config.GetSection("userSettings/NetStream.Properties.Settings");

            if (section != null && !section.SectionInformation.IsProtected)
            {
                try
                {
                    section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
                    section.SectionInformation.ForceSave = true; 
                    config.Save(ConfigurationSaveMode.Full);
                }
                catch (Exception ex)
                {
                    Log.Error($"Şifreleme sırasında bir hata oluştu: {ex.Message}");
                }
            }
            else if (section == null)
            {
                Log.Error("NetStream.Properties.Settings bölümü bulunamadı.");
            }
            else
            {
                Log.Error("NetStream.Properties.Settings zaten şifreli.");
            }
        }

        private void LogException(Exception ex)
        {
            //Essentials.KillJackett();
            if (ex != null)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public static void SetPrimaryColor()
        {
            PaletteHelper paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            if (AppSettingsManager.appSettings.PrimaryColorRed != null)
            {
                Color current = new Color();
                current.A = (byte)AppSettingsManager.appSettings.PrimaryColorAlpha;
                current.R = (byte)AppSettingsManager.appSettings.PrimaryColorRed;
                current.G = (byte)AppSettingsManager.appSettings.PrimaryColorGreen;
                current.B = (byte)AppSettingsManager.appSettings.PrimaryColorBlue;
                App.Current.Resources["ColorDefault"] = current;
                theme.SetPrimaryColor(current);
            }
            else
            {
                var defaultColor = (Color)App.Current.Resources["ColorDefault"];
                theme.SetPrimaryColor(defaultColor);
                AppSettingsManager.appSettings.PrimaryColorAlpha = (double)defaultColor.A;
                AppSettingsManager.appSettings.PrimaryColorRed = (double)defaultColor.R;
                AppSettingsManager.appSettings.PrimaryColorGreen = (double)defaultColor.G;
                AppSettingsManager.appSettings.PrimaryColorBlue = (double)defaultColor.B;
                AppSettingsManager.SaveAppSettings();
            }
            paletteHelper.SetTheme(theme);
            Log.Information("Set theme color");
        }

        private async void Load()
        {
            try
            {
                var destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett\\Indexers");
                if (!FileHelper.IsFirstStart && Directory.Exists(destinationFolder) && Essentials.IsJackettInstalled())
                {
                    await Essentials.RunJacketAsync();
                    JackettService.Init();
                    Init();
                }
                else
                {
                    Essentials.InstallJackett();
                    await Essentials.RunJacketAsync();
                    JackettService.Init();
                    Init();
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private static void SetDefaultValues()
        {
            SetLanguage();

            if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiUrl))
            {
                AppSettingsManager.appSettings.JacketApiUrl = "http://127.0.0.1:9117/";
                AppSettingsManager.SaveAppSettings();
            }

            if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiKey))
            {
                AppSettingsManager.appSettings.JacketApiKey = "htxi2hsv7gp3b245n5mcf8kz7vwf5sp4";
                AppSettingsManager.SaveAppSettings();
            }

            Log.Information("Set default values");
        }

        public static List<string> SupportedProgramLanguages = new List<string>()
        {
            "en",
            "tr",
            "it",
            "af",
            "ar",
            "az",
            "be",
            "bg",
            "cn",
            "cs",
            "da",
            "de",
            "el",
            "es",
            "et",
            "fa",
            "fi",
            "fr",
            "ga",
            "hi",
            "hr",
            "hu",
            "hy",
            //"id",
            "ja",
            "ka",
            "kk",
            //"ko",
            "ky",
            "la",
            "mg",
            "mk",
            //"nl",
            "no",
            //"pl",
            "pt",
            "ro",
            "ru",
            "sk",
            "sl",
            "sq",
            "sr",
            "sv",
            "tk",
            "ug",
            "uk",
            "uz",
            "vi"
        };

        public static bool isClosing = true;

        private static async void SetLanguage()
        {
            var languages = await Service.InitLanguages();
            if (languages != null && languages.Count > 0)
            {
                CultureInfo ci = CultureInfo.InstalledUICulture;
                var match = languages.FirstOrDefault(x => x.Iso_639_1 == ci.TwoLetterISOLanguageName);
                var englishName = match != null ? match.EnglishName : "English";
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
                if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.ProgramLanguage))
                {
                    if (SupportedProgramLanguages.Any(x => x == match.Iso_639_1))
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
                if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.SubtitleLanguage))
                {
                    if (SubtitleHandler.LanguagesToOpenSubtitles.Select(x => x.Key).Any(z => z == match.Iso_639_1))
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
        }

        public static async void Init()
        {
            var tasks = new List<Task>()
            {
                Service.client.GetConfigAsync(),
                Libtorrent.Initialize(),
                SubtitleHandler.Init(),
                LoadAccountPage()
            };
            await Task.WhenAll(tasks);
        }

        public static async Task LoadAccountPage()
        {
            if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.TmdbUsername) &&
                !String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.TmdbPassword))
            {

                var loginResult = await Service.Login(AppSettingsManager.appSettings.TmdbUsername,
                    AppSettingsManager.appSettings.TmdbPassword);
                if (loginResult)
                {
                    AccountPage.GetAccountPageInstance = new AccountPage();
                    Log.Information("Logged in to TMDB account");
                }
            }
            else
            {
                Log.Error("There is no TMDB account please log in from profile page");
            }
        }

        private async void Login()
        {
            var result = await FirestoreManager.SignIn(AppSettingsManager.appSettings.FireStoreEmail,AppSettingsManager.appSettings.FireStorePassword);
            if (result != null)
            {
                if (result.Success)
                {
                    var isUserRegistered = await FirestoreManager.IsUserRegistered(AppSettingsManager.appSettings.FireStoreEmail);
                    if (!isUserRegistered)
                    {
                        await FirestoreManager.Register(new SubPlan() { PlanName = "lifetime" });
                    }
                    var loginResult = await FirestoreManager.IsValidLogin();
                    if (loginResult.Success)
                    {
                        var mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.MainWindow = mainWindow;
                        Log.Information("Opened Main Window");
                        splashWindow.Close();
                    }
                    else
                    {
                        if (loginResult.ErrorType == ErrorType.Expired)
                        {
                            SubPlansPage subPlansPage = new SubPlansPage(true,false);
                            subPlansPage.Show();
                            new CustomMessageBox(loginResult.ErrorMessage, MessageType.Error, MessageButtons.Ok).ShowDialog();
                            splashWindow.Close();
                        }
                        else if (loginResult.ErrorType == ErrorType.Hwid)
                        {
                            new ChangeHwidWindow().Show();
                            splashWindow.Close();
                        }
                        else if(loginResult.ErrorType == ErrorType.UserNotFound)
                        {
                            new SubPlansPage(true,true).Show();
                            splashWindow.Close();
                        }
                    }
                }
                else
                {
                    new LoginWindow(PageType.SignIn).Show();
                    splashWindow.Close();
                    new CustomMessageBox("Connection failed.", MessageType.Error, MessageButtons.Ok).ShowDialog();
                    Log.Information("Couldnt login opened login page");
                }
            }
        }
    }

}
