using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualBasic.Logging;

namespace NetStream.Language
{
    public class LanguageManager
    {
        public static async void SwitchLanguage()
        {
            ResourceDictionary dictionary = new ResourceDictionary();
            if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.IsoProgramLanguage))
            {
                switch (AppSettingsManager.appSettings.IsoProgramLanguage)
                {
                    case string isoLanguage when App.SupportedProgramLanguages.Contains(isoLanguage):
                        dictionary.Source = new Uri($"..\\Language\\StringResources.{isoLanguage}.xaml", UriKind.Relative);
                        Serilog.Log.Information($"Changed language to {isoLanguage.ToUpper()}");
                        break;

                    default:
                        dictionary.Source = new Uri("..\\Language\\StringResources.en.xaml", UriKind.Relative);
                        Serilog.Log.Information("Changed language to English");
                        break;
                }
            }
            else
            {
                await Service.InitLanguages();
                CultureInfo ci = CultureInfo.InstalledUICulture;
                var match = Service.Languages.FirstOrDefault(x => x.Iso_639_1 == ci.TwoLetterISOLanguageName);
                if (match != null && App.SupportedProgramLanguages.Contains(match.Iso_639_1))
                {
                    dictionary.Source = new Uri($"..\\Language\\StringResources.{match.Iso_639_1}.xaml", UriKind.Relative);
                    AppSettingsManager.appSettings.IsoProgramLanguage = match.Iso_639_1;
                    AppSettingsManager.SaveAppSettings();
                    Serilog.Log.Information($"Changed language to {match.Iso_639_1.ToUpper()}");
                }
                else
                {
                    // Varsayılan dil (örneğin, İngilizce)
                    dictionary.Source = new Uri("..\\Language\\StringResources.en.xaml", UriKind.Relative);
                    AppSettingsManager.appSettings.IsoProgramLanguage = "en";
                    AppSettingsManager.SaveAppSettings();
                    Serilog.Log.Information("Changed language to English");
                }
            }
            Application.Current.Resources.MergedDictionaries.Add(dictionary);
        }
    }
}
