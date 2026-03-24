using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;

namespace NetStream.Language
{
    public class LanguageManager
    {
        private static ResourceInclude _currentLanguageResource;
        
        // Track if we've initialized language
        private static bool _initialized = false;

        public static async void SwitchLanguage()
        {
            if (Application.Current == null)
                return;

            // For mobile platforms, ensure settings are reloaded from shared preferences
            if (Application.Current.ApplicationLifetime is ISingleViewApplicationLifetime)
            {
                // Reload app settings in case they were changed by another component
                Console.WriteLine("Mobile platform detected, explicitly reloading settings");
                AppSettingsManager.LoadAppSettings();
                AppSettingsManager.ApplyEnvironmentOverrides();
            }

            string languageCode;
            Uri resourceUri;

            // Add debug logging

            if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.IsoProgramLanguage))
            {
                if (SupportedProgramLanguages.Contains(AppSettingsManager.appSettings.IsoProgramLanguage))
                {
                    languageCode = AppSettingsManager.appSettings.IsoProgramLanguage;
                    resourceUri = new Uri($"avares://NetStream/Language/StringResources.{languageCode}.xaml");
                }
                else
                {
                    languageCode = "en";
                    resourceUri = new Uri("avares://NetStream/Language/StringResources.en.xaml");
                }
            }
            else
            {
                // If no language is set, try to detect system language
                await Service.InitLanguages();
                CultureInfo ci = CultureInfo.InstalledUICulture;
                
                var match = Service.Languages.FirstOrDefault(x => x.Iso_639_1 == ci.TwoLetterISOLanguageName);
                if (match != null && SupportedProgramLanguages.Contains(match.Iso_639_1))
                {
                    languageCode = match.Iso_639_1;
                    resourceUri = new Uri($"avares://NetStream/Language/StringResources.{languageCode}.xaml");
                    AppSettingsManager.appSettings.IsoProgramLanguage = languageCode;
                    AppSettingsManager.appSettings.ProgramLanguage = match.EnglishName ?? languageCode;
                    AppSettingsManager.SaveAppSettings();
                }
                else
                {
                    languageCode = "en";
                    resourceUri = new Uri("avares://NetStream/Language/StringResources.en.xaml");
                    AppSettingsManager.appSettings.IsoProgramLanguage = "en";
                    AppSettingsManager.appSettings.ProgramLanguage = "English";
                    AppSettingsManager.SaveAppSettings();
                }
            }

            try
            {
                
                // Önceki dil kaynaklarını kaldır
                if (_currentLanguageResource != null && Application.Current.Resources.MergedDictionaries.Contains(_currentLanguageResource))
                {
                    Application.Current.Resources.MergedDictionaries.Remove(_currentLanguageResource);
                }

                // Yeni dil kaynaklarını ekle
                _currentLanguageResource = new ResourceInclude(new Uri("avares://NetStream/Language"));
                
                try
                {
                    // Try to load the requested language
                    _currentLanguageResource.Source = resourceUri;
                    Application.Current.Resources.MergedDictionaries.Add(_currentLanguageResource);
                    _initialized = true;
                }
                catch (Exception ex)
                {
                    
                    // Fallback to English if the requested language resource fails to load
                    if (languageCode != "en")
                    {
                        _currentLanguageResource.Source = new Uri("avares://NetStream/Language/StringResources.en.xaml");
                        Application.Current.Resources.MergedDictionaries.Add(_currentLanguageResource);
                        AppSettingsManager.appSettings.IsoProgramLanguage = "en";
                        AppSettingsManager.appSettings.ProgramLanguage = "English";
                        AppSettingsManager.SaveAppSettings();
                        _initialized = true;
                    }
                    else
                    {
                        // If even English fails, rethrow the exception
                        throw;
                    }
                }
                
                // Update UI based on application lifecycle
                UpdateUiForCurrentLifetime();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to switch language: {ex.Message}");
            }
        }

        private static void UpdateUiForCurrentLifetime()
        {
            // Handle different application lifetimes
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null)
                {
                    // Trigger UI update for desktop
                    desktop.MainWindow.InvalidateVisual();
                }
            }
            else if (Application.Current.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                // Trigger UI update for mobile/web
                if (singleView.MainView != null)
                {
                    singleView.MainView.InvalidateVisual();
                }
            }
        }
        
        // Check if we need to initialize language (called from App.axaml.cs)
        public static bool NeedsInitialization()
        {
            return !_initialized;
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
    }
}
