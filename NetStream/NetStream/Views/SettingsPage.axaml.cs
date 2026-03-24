using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using NetStream.Language;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.VisualTree;
using DialogHostAvalonia;
using DryIoc.ImTools;
using TorznabClient.Models;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl, IDisposable
    {
        private List<string> languages;
        public static SettingsPage GetSettingsPageInstance;
        private CurrentProcess currentProcess;
        
        // Ekran boyutu eşikleri
        private const double SMALL_SCREEN_THRESHOLD = 1100;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 750;
        private const double ULTRA_SMALL_SCREEN_THRESHOLD = 480;
        
        public SettingsPage()
        {
            InitializeComponent();
            FillComboBoxes();
            Initialize();
        }
        
        private async Task<string> GetLanguage(string name)
        {
            return Service.Languages.FirstOrDefault(x => x.EnglishName == name).Iso_639_1;
        }
        
        private async void Initialize()
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.TmdbResultLanguage))
                {
                    ComboBoxTmdbResultLanguage.SelectedItem =
                        AppSettingsManager.appSettings.TmdbResultLanguage;
                }
                else
                {
                    ComboBoxTmdbResultLanguage.SelectedItem = "English";
                    AppSettingsManager.appSettings.TmdbResultLanguage = "English";
                    AppSettingsManager.appSettings.IsoTmdbResultLanguage = await GetLanguage("English");
                    AppSettingsManager.SaveAppSettings();
                }
                if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.ProgramLanguage))
                {
                    ComboBoxProgramLanguage.SelectedItem =
                        AppSettingsManager.appSettings.ProgramLanguage;
                }
                else
                {
                    ComboBoxProgramLanguage.SelectedItem = "English";
                    AppSettingsManager.appSettings.ProgramLanguage = "English";
                    AppSettingsManager.appSettings.IsoProgramLanguage = await GetLanguage("English");
                    AppSettingsManager.SaveAppSettings();
                }
                if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.SubtitleLanguage))
                {
                    ComboBoxSubtitleLanguage.SelectedItem =
                        AppSettingsManager.appSettings.SubtitleLanguage;
                }
                else
                {
                    ComboBoxSubtitleLanguage.SelectedItem = "English";
                    AppSettingsManager.appSettings.SubtitleLanguage = "English";
                    AppSettingsManager.appSettings.IsoSubtitleLanguage = await GetLanguage("English");
                    AppSettingsManager.SaveAppSettings();
                }

                if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiUrl))
                {
                    TextBoxJackettApiUrl.Text = AppSettingsManager.appSettings.JacketApiUrl;
                }
                if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiKey))
                {
                    TextBoxJackettApiKey.Text = AppSettingsManager.appSettings.JacketApiKey;
                }
                if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.MoviesPath))
                {
                    SavePathFolderTextBox.Text = AppSettingsManager.appSettings.MoviesPath;
                }
                else
                {
                    string data = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string name = Assembly.GetExecutingAssembly().GetName().Name;
                    string path = Path.Combine(data, name, "Movies");
                    SavePathFolderTextBox.Text = path;
                    AppSettingsManager.appSettings.MoviesPath = path;
                    AppSettingsManager.SaveAppSettings();
                }

                if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.OpenSubtitlesApiKey))
                {
                    TextBoxopenSubtitlesApiKey.Text = AppSettingsManager.appSettings.OpenSubtitlesApiKey;
                }

                TextBoxSendSocketBufferSize.Text = (await SessionSettings2.GetSendSocketBufferSize()).ToString();
                TextBoxRecvSocketBufferSize.Text = (await SessionSettings2.GetRecvSocketBufferSize()).ToString();
                TextBoxConnectionsLimit.Text = (await SessionSettings2.GetConnectionsLimit()).ToString();
                TextBoxUnchokeSlotsLimit.Text = (await SessionSettings2.GetUnchokeSlotsLimit()).ToString();
                TextBoxMaxPeerlistSize.Text = (await SessionSettings2.GetMaxPeerListSize()).ToString();
                TextBoxHalfOpenLimit.Text = (await SessionSettings2.GetHalfOpenLimit()).ToString();
                TextBoxActiveDownloads.Text = (await SessionSettings2.GetActiveDownloads()).ToString();
                TextBoxCacheSize.Text = (await SessionSettings2.GetCacheSize()).ToString();
                TextBoxReadCacheLineSize.Text = (await SessionSettings2.GetReadCacheLineSize()).ToString();
                CheckBoxPreferRc4.IsChecked = await SessionSettings2.GetPreferRc4();
                CheckBoxEnableDht.IsChecked = await SessionSettings2.GetEnableDht();
                CheckBoxEnableLsd.IsChecked = await SessionSettings2.GetEnableLsd();
                CheckBoxEnableNatpmp.IsChecked = await SessionSettings2.GetEnableNatpmp();
                CheckBoxEnableUpnp.IsChecked =await  SessionSettings2.GetEnableUpnp();

                TextBoxSendSocketBufferSize.TextChanged += TextBoxSendSocketBufferSizeOnTextChanged;
                TextBoxRecvSocketBufferSize.TextChanged += TextBoxRecvSocketBufferSizeOnTextChanged;
                TextBoxConnectionsLimit.TextChanged += TextBoxConnectionsLimitOnTextChanged;
                TextBoxUnchokeSlotsLimit.TextChanged += TextBoxUnchokeSlotsLimitOnTextChanged;
                TextBoxMaxPeerlistSize.TextChanged += TextBoxMaxPeerlistSizeOnTextChanged;
                TextBoxHalfOpenLimit.TextChanged += TextBoxHalfOpenLimitOnTextChanged;
                TextBoxActiveDownloads.TextChanged += TextBoxActiveDownloadsOnTextChanged;
                TextBoxCacheSize.TextChanged += TextBoxCacheSizeOnTextChanged;
                TextBoxReadCacheLineSize.TextChanged += TextBoxReadCacheLineSizeOnTextChanged;
                CheckBoxPreferRc4.IsCheckedChanged += CheckBoxPreferRc4OnIsCheckedChanged;
                CheckBoxEnableDht.IsCheckedChanged += CheckBoxEnableDhtOnChecked;
                CheckBoxEnableLsd.IsCheckedChanged += CheckBoxEnableLsdOnChecked;
                CheckBoxEnableNatpmp.IsCheckedChanged += CheckBoxEnableNatpmpOnChecked;
                CheckBoxEnableUpnp.IsCheckedChanged += CheckBoxEnableUpnpOnChecked;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void CheckBoxPreferRc4OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            try
            {
                await SessionSettings2.SetPreferRc4(CheckBoxPreferRc4.IsChecked.Value);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TextBoxReadCacheLineSizeOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxReadCacheLineSize.Text, out result))
                {
                    await SessionSettings2.SetReadCacheLineSize(result);
                }
                else
                {
                    TextBoxReadCacheLineSize.Text = (await SessionSettings2.GetReadCacheLineSize()).ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TextBoxCacheSizeOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxCacheSize.Text, out result))
                {
                    await SessionSettings2.SetCacheSize(result);
                }
                else
                {
                    TextBoxCacheSize.Text = (await SessionSettings2.GetCacheSize()).ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TextBoxActiveDownloadsOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxActiveDownloads.Text, out result))
                {
                   await SessionSettings2.SetActiveDownloads(result);
                }
                else
                {
                    TextBoxActiveDownloads.Text = (await SessionSettings2.GetActiveDownloads()).ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TextBoxHalfOpenLimitOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxHalfOpenLimit.Text, out result))
                {
                    await SessionSettings2.SetHalfOpenLimit(result);
                }
                else
                {
                    TextBoxHalfOpenLimit.Text = (await SessionSettings2.GetHalfOpenLimit()).ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TextBoxMaxPeerlistSizeOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxMaxPeerlistSize.Text, out result))
                {
                    await SessionSettings2.SetMaxPeerListSize(result);
                }
                else
                {
                    TextBoxMaxPeerlistSize.Text = (await SessionSettings2.GetMaxPeerListSize()).ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TextBoxUnchokeSlotsLimitOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxUnchokeSlotsLimit.Text, out result))
                {
                    await SessionSettings2.SetUnchokeSlotsLimit(result);
                }
                else
                {
                    TextBoxUnchokeSlotsLimit.Text = (await SessionSettings2.GetUnchokeSlotsLimit()).ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TextBoxConnectionsLimitOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxConnectionsLimit.Text, out result))
                {
                    await SessionSettings2.SetConnectionsLimit(result);
                }
                else
                {
                    TextBoxConnectionsLimit.Text = (await SessionSettings2.GetConnectionsLimit()).ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TextBoxRecvSocketBufferSizeOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxRecvSocketBufferSize.Text, out result))
                {
                    await SessionSettings2.SetRecvSocketBufferSize(result);
                }
                else
                {
                    TextBoxRecvSocketBufferSize.Text = (await SessionSettings2.GetRecvSocketBufferSize()).ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TextBoxSendSocketBufferSizeOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxSendSocketBufferSize.Text, out result))
                {
                    await SessionSettings2.SetSendSocketBufferSize(result);
                }
                else
                {
                    TextBoxSendSocketBufferSize.Text = (await SessionSettings2.GetSendSocketBufferSize()).ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        // This method will be filled in later
        private async void FillComboBoxes()
        {
            try
            {
                languages = await Service.GetLanguages();
                ComboBoxTmdbResultLanguage.ItemsSource = languages;
                ComboBoxProgramLanguage.ItemsSource = Service.Languages.Where(x => App.SupportedProgramLanguages.Any(z => z == x.Iso_639_1))
                    .Select(m => m.EnglishName).OrderBy(x => x);

                var subtitleLanguages = SubtitleHandler.LanguagesToOpenSubtitles.Select(x => x.Key);
                var subLanguages = Service.Languages.Where(x => subtitleLanguages.Any(z => z == x.Iso_639_1))
                    .Select(m => m.EnglishName).OrderBy(x => x).ToList();
                subLanguages.Insert(0, "Disabled");
                ComboBoxSubtitleLanguage.ItemsSource = subLanguages;

                var diskIoModes = new List<string>
                {
                    "Enable OS Cache",
                    "Disable OS Cache",
                    "Write Through"
                };
                ComboBoxDiskIOWriteMode.ItemsSource = diskIoModes;
                ComboBoxDiskIOReadMode.ItemsSource = diskIoModes;

                switch (await SessionSettings2.GetDiskIoWriteMode())
                {
                    case DiskIOMode.EnableOsCache:
                        ComboBoxDiskIOWriteMode.SelectedItem = "Enable OS Cache";
                        break;
                    case DiskIOMode.DisableOsCache:
                        ComboBoxDiskIOWriteMode.SelectedItem = "Disable OS Cache";
                        break;
                    case DiskIOMode.WriteThrough:
                        ComboBoxDiskIOWriteMode.SelectedItem = "Write Through";
                        break;
                }

                switch (await SessionSettings2.GetDiskIoReadMode())
                {
                    case DiskIOMode.EnableOsCache:
                        ComboBoxDiskIOReadMode.SelectedItem = "Enable OS Cache";
                        break;
                    case DiskIOMode.DisableOsCache:
                        ComboBoxDiskIOReadMode.SelectedItem = "Disable OS Cache";
                        break;
                    case DiskIOMode.WriteThrough:
                        ComboBoxDiskIOReadMode.SelectedItem = "Write Through";
                        break;
                }

                ComboBoxDiskIOWriteMode.SelectionChanged += ComboBoxDiskIOWriteModeOnSelectionChanged;
                ComboBoxDiskIOReadMode.SelectionChanged += ComboBoxDiskIOReadModeOnSelectionChanged;

                var encryptionLevels = new List<string>
                {
                    "RC4 and Plain Text (Recommended)",
                    "Just RC4",
                    "No Encryption (Risky!)"
                };
                ComboBoxEncryptionLevel.ItemsSource = encryptionLevels;

                switch (await SessionSettings2.GetAllowedEncLevel())
                {
                    case EncryptionLevel.PePlaintext:
                        ComboBoxEncryptionLevel.SelectedItem = "No Encryption (Risky!)";
                        break;
                    case EncryptionLevel.PeRc4:
                        ComboBoxEncryptionLevel.SelectedItem = "Just RC4";
                        break;
                    case EncryptionLevel.PeBoth:
                        ComboBoxEncryptionLevel.SelectedItem = "RC4 and Plain Text (Recommended)";
                        break;
                }

                ComboBoxEncryptionLevel.SelectionChanged += ComboBoxEncryptionLevelOnSelectionChanged;
                
                

                var policyOptions = new List<string>
                {
                    "Enforce Encryption",
                    "Enable Encryption",
                    "Disable Encryption"
                };
                ComboBoxOutgoingTrafficEncryption.ItemsSource = policyOptions;
                ComboBoxIncomingTrafficEncryption.ItemsSource = policyOptions;

                switch (await SessionSettings2.GetOutEncPolicy())
                {
                    case EncryptionPolicy.PeForced:
                        ComboBoxOutgoingTrafficEncryption.SelectedItem = "Enforce Encryption";
                        break;
                    case EncryptionPolicy.PeEnabled:
                        ComboBoxOutgoingTrafficEncryption.SelectedItem = "Enable Encryption";
                        break;
                    case EncryptionPolicy.PeDisabled:
                        ComboBoxOutgoingTrafficEncryption.SelectedItem = "Disable Encryption";
                        break;
                }

                switch (await SessionSettings2.GetInEncPolicy())
                {
                    case EncryptionPolicy.PeForced:
                        ComboBoxIncomingTrafficEncryption.SelectedItem = "Enforce Encryption";
                        break;
                    case EncryptionPolicy.PeEnabled:
                        ComboBoxIncomingTrafficEncryption.SelectedItem = "Enable Encryption";
                        break;
                    case EncryptionPolicy.PeDisabled:
                        ComboBoxIncomingTrafficEncryption.SelectedItem = "Disable Encryption";
                        break;
                }
                ComboBoxOutgoingTrafficEncryption.SelectionChanged += ComboBoxOutgoingTrafficEncryptionOnSelectionChanged;
                ComboBoxIncomingTrafficEncryption.SelectionChanged += ComboBoxIncomingTrafficEncryptionOnSelectionChanged;

                bool success = false;

                while (!success)
                {
                    try
                    {
                        allIndexers = await JackettService.GetIndexersAsync();
                        var indexerList = allIndexers.Select(x => x.Title).OrderBy(x => x).ToList();
                        // Add "All" as the first item
                        indexerList.Insert(0, "All");
                        JacketIndexersCheckBoxDisplay.ItemsSource = indexerList;
                        
                        if (JackettService.SelectedIndexers != null && JackettService.SelectedIndexers.Count > 0)
                        {
                            
                        }
                        else
                        {
                            JackettService.SelectedIndexers = JsonConvert.DeserializeObject<List<Indexer>>(
                                File.ReadAllText(AppSettingsManager.appSettings.IndexersPath));
                        }
                        
                        success = true;
                    }
                    catch (Exception e)
                    {
                        success = false;
                    }
                }

                
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
            
            // Initialize Watch Now quality options
            try
            {
                var watchNowQualities = new List<string>
                {
                    "480p",
                    "720p",
                    "1080p",
                    "2160p"
                };
                ComboBoxWatchNowQuality.ItemsSource = watchNowQualities;
                
                // Set current selection from settings
                if (!string.IsNullOrEmpty(AppSettingsManager.appSettings.WatchNowDefaultQuality))
                {
                    ComboBoxWatchNowQuality.SelectedItem = AppSettingsManager.appSettings.WatchNowDefaultQuality;
                }
                else
                {
                    ComboBoxWatchNowQuality.SelectedItem = "1080p";
                    AppSettingsManager.appSettings.WatchNowDefaultQuality = "1080p";
                    AppSettingsManager.SaveAppSettings();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error initializing Watch Now settings: {ex.Message}");
            }
        }
        

        private async void ComboBoxIncomingTrafficEncryptionOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (ComboBoxIncomingTrafficEncryption.SelectedItem.ToString())
                {
                    case "Enforce Encryption":
                        await SessionSettings2.SetInEncPolicy(EncryptionPolicy.PeForced);
                        break;
                    case "Enable Encryption":
                        await SessionSettings2.SetInEncPolicy(EncryptionPolicy.PeEnabled);
                        break;
                    case "Disable Encryption":
                        await SessionSettings2.SetInEncPolicy(EncryptionPolicy.PeDisabled);
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ComboBoxOutgoingTrafficEncryptionOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (ComboBoxOutgoingTrafficEncryption.SelectedItem.ToString())
                {
                    case "Enforce Encryption":
                        await SessionSettings2.SetOutEncPolicy(EncryptionPolicy.PeForced);
                        break;
                    case "Enable Encryption":
                        await SessionSettings2.SetOutEncPolicy(EncryptionPolicy.PeEnabled);
                        break;
                    case "Disable Encryption":
                        await SessionSettings2.SetOutEncPolicy(EncryptionPolicy.PeDisabled);
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ComboBoxEncryptionLevelOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (ComboBoxEncryptionLevel.SelectedItem.ToString())
                {
                    case "RC4 and Plain Text (Recommended)":
                        await SessionSettings2.SetAllowedEncLevel(EncryptionLevel.PeBoth);
                        break;
                    case "Just RC4":
                        await SessionSettings2.SetAllowedEncLevel(EncryptionLevel.PeRc4);
                        break;
                    case "No Encryption (Risky!)":
                        await  SessionSettings2.SetAllowedEncLevel(EncryptionLevel.PePlaintext);
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ComboBoxDiskIOReadModeOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (ComboBoxDiskIOReadMode.SelectedItem.ToString())
                {
                    case "Enable OS Cache":
                        await SessionSettings2.SetDiskIoReadMode(DiskIOMode.EnableOsCache);
                        break;
                    case "Disable OS Cache":
                        await SessionSettings2.SetDiskIoReadMode(DiskIOMode.DisableOsCache);
                        break;
                    case "Write Through":
                        await SessionSettings2.SetDiskIoReadMode(DiskIOMode.WriteThrough);
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ComboBoxDiskIOWriteModeOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (ComboBoxDiskIOWriteMode.SelectedItem.ToString())
                {
                    case "Enable OS Cache":
                        await SessionSettings2.SetDiskIoWriteMode(DiskIOMode.EnableOsCache);
                        break;
                    case "Disable OS Cache":
                        await SessionSettings2.SetDiskIoWriteMode(DiskIOMode.DisableOsCache);
                        break;
                    case "Write Through":
                        await SessionSettings2.SetDiskIoWriteMode(DiskIOMode.WriteThrough);
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        private async void ComboBoxJacketIndexersOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.RemovedItems.Count > 0)
                {
                    foreach (var eRemovedItem in e.RemovedItems)
                    {
                        if (JackettService.SelectedIndexers.Any(x => x.Title == eRemovedItem.ToString()))
                        {
                            var removedItem =
                                JackettService.SelectedIndexers.FirstOrDefault(x => x.Title == eRemovedItem.ToString());
                            JackettService.SelectedIndexers.Remove(removedItem);
                        }
                    }
                }

                if (e.AddedItems.Count > 0)
                {
                    var allIndexers = await JackettService.GetIndexersAsync();
                    foreach (var eAddedItem in e.AddedItems)
                    {
                        if (!JackettService.SelectedIndexers.Any(x => x.Title == eAddedItem.ToString()))
                        {
                            JackettService.SelectedIndexers.Add(new Indexer
                            {
                                Id = allIndexers.FirstOrDefault(x=> x.Title == eAddedItem.ToString()).Id,
                                Title = eAddedItem.ToString()
                            });
                        }
                    }
                }

                var js = JsonConvert.SerializeObject(JackettService.SelectedIndexers);
                await File.WriteAllTextAsync(AppSettingsManager.appSettings.IndexersPath, js);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public static bool TryExtractWholeNumber(string input, out int number)
        {
            number = 0;

            if (string.IsNullOrEmpty(input))
                return false;

            if (!Regex.IsMatch(input, @"^-?\d+$"))
                return false;

            return int.TryParse(input, out number);
        }

        #region Event Handlers
        

        private async void CheckBoxEnableUpnpOnChecked(object? sender, RoutedEventArgs e)
        {
            try
            {
                await SessionSettings2.SetEnableUpnp(CheckBoxEnableUpnp.IsChecked.Value);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

       

        private async void CheckBoxEnableNatpmpOnChecked(object? sender, RoutedEventArgs e)
        {
            try
            {
                await SessionSettings2.SetEnableNatpmp(CheckBoxEnableNatpmp.IsChecked.Value);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        

        private async void CheckBoxEnableLsdOnChecked(object? sender, RoutedEventArgs e)
        {
            try
            {
                await SessionSettings2.SetEnableLsd(CheckBoxEnableLsd.IsChecked.Value);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

       

        private async void CheckBoxEnableDhtOnChecked(object? sender, RoutedEventArgs e)
        {
            try
            {
                await SessionSettings2.SetEnableDht(CheckBoxEnableDht.IsChecked.Value);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

      
        // More event handlers will be added in subsequent parts

        #endregion
        
        // This will be continued in the next part
        MainAccountPage mainAccountPage;
        private async void SettingsPage_OnLoaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                MainView.Instance.SizeChanged += InstanceOnSizeChanged;
                mainAccountPage = new MainAccountPage(this);
                AccountNav.Content = mainAccountPage;
                
                GetSettingsPageInstance = this;
                
                // Responsive düzeni sayfa yüklendiğinde ayarla
                double width = MainView.Instance.Bounds.Width;
                AdjustUIForScreenSize(width);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            try
            {
                double width = e.width;
                AdjustUIForScreenSize(width);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in Control_OnSizeChanged: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void SettingsPage_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }

        private async void ComboBoxTmdbResultLanguage_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.TmdbResultLanguage = ComboBoxTmdbResultLanguage.SelectedItem.ToString();
                AppSettingsManager.appSettings.IsoTmdbResultLanguage =
                    await GetLanguage(ComboBoxTmdbResultLanguage.SelectedItem.ToString());
                Service.language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;
                AppSettingsManager.SaveAppSettings();;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ComboBoxProgramLanguage_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.ProgramLanguage = ComboBoxProgramLanguage.SelectedItem.ToString();
                AppSettingsManager.appSettings.IsoProgramLanguage =
                    await GetLanguage(ComboBoxProgramLanguage.SelectedItem.ToString());
                AppSettingsManager.SaveAppSettings();;
                LanguageManager.SwitchLanguage();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ComboBoxSubtitleLanguage_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedItem = ComboBoxSubtitleLanguage.SelectedItem.ToString();
                if (selectedItem == "Disabled")
                {
                    AppSettingsManager.appSettings.SubtitleLanguage = "Disabled";
                    AppSettingsManager.appSettings.IsoSubtitleLanguage = "Disabled";
                    AppSettingsManager.SaveAppSettings(); 
                }
                else
                {
                    AppSettingsManager.appSettings.SubtitleLanguage = selectedItem;
                    AppSettingsManager.appSettings.IsoSubtitleLanguage =
                        await GetLanguage(selectedItem);
                    AppSettingsManager.SaveAppSettings(); 
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ComboBoxWatchNowQuality_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ComboBoxWatchNowQuality.SelectedItem != null)
                {
                    AppSettingsManager.appSettings.WatchNowDefaultQuality = ComboBoxWatchNowQuality.SelectedItem.ToString();
                    AppSettingsManager.SaveAppSettings();
                    Console.WriteLine($"Watch Now default quality set to: {AppSettingsManager.appSettings.WatchNowDefaultQuality}");
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxJackettApiUrl_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.JacketApiUrl = TextBoxJackettApiUrl.Text;
                AppSettingsManager.SaveAppSettings();;
                JackettService.Init();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxJackettApiKey_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.JacketApiKey = TextBoxJackettApiKey.Text;
                AppSettingsManager.SaveAppSettings();;
                JackettService.Init();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TextBoxopenSubtitlesApiKey_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.OpenSubtitlesApiKey = TextBoxopenSubtitlesApiKey.Text;
                AppSettingsManager.SaveAppSettings();;
                await SubtitleHandler.Init();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void OpenSubtitlesGetApiKey_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            
        }

        private void OpenSubtitlesGetApiKey_OnPointerExited(object? sender, PointerEventArgs e)
        {
            
        }

        private void OpenSubtitlesGetApiKey_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            
        }

        private void SavePathFolderTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.MoviesPath = SavePathFolderTextBox.Text;
                AppSettingsManager.SaveAppSettings();;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ButtonOpenFolder_OnPointerPressed(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
               
                var topLevel = TopLevel.GetTopLevel(this);
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Downloads Folder",
                    AllowMultiple = false
                });

                if (folders != null && folders.Count > 0)
                {
                    var folder = folders[0];
                    SavePathFolderTextBox.Text = folder.Path.LocalPath;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void PlayerSettingAutoSync_OnLoaded(object? sender, RoutedEventArgs e)
        {
            PlayerSettingAutoSync.IsChecked = AppSettingsManager.appSettings.PlayerSettingAutoSync;
            PlayerSettingAutoSync.IsCheckedChanged += PlayerSettingAutoSyncOnIsCheckedChanged;
        }

        private void PlayerSettingAutoSyncOnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.PlayerSettingAutoSync = PlayerSettingAutoSync.IsChecked.Value;
                AppSettingsManager.SaveAppSettings();;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void PlayerSettingShowThumbnails_OnLoaded(object? sender, RoutedEventArgs e)
        {
            PlayerSettingShowThumbnails.IsChecked = AppSettingsManager.appSettings.PlayerSettingShowThumbnail;
            PlayerSettingShowThumbnails.IsCheckedChanged += PlayerSettingShowThumbnailsOnIsCheckedChanged;
        }

        private void PlayerSettingShowThumbnailsOnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.PlayerSettingShowThumbnail = PlayerSettingShowThumbnails.IsChecked.Value;
                AppSettingsManager.SaveAppSettings();;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnDiscord_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            
        }

        private void BtnDiscord_OnPointerExited(object? sender, PointerEventArgs e)
        {
            
        }

        private void BtnDiscord_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            
        }

        public void Dispose()
        {
            
        }
        
        List<TorznabIndexer> allIndexers = new List<TorznabIndexer>();
        private bool _isLoadingCheckboxes = false;
        private async void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            // Ignore events during initial loading
            if (_isLoadingCheckboxes) return;
            
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                var indexer = checkBox.DataContext as string;
                if (indexer != null)
                {
                    // Handle "All" specially
                    if (indexer == "All")
                    {
                        if (checkBox.IsChecked.Value)
                        {
                            // Select all indexers
                            JackettService.SelectedIndexers.Clear();
                            foreach (var idx in allIndexers)
                            {
                                JackettService.SelectedIndexers.Add(new Indexer
                                {
                                    Id = idx.Id,
                                    Title = idx.Title
                                });
                            }
                        }
                        else
                        {
                            // Deselect all indexers
                            JackettService.SelectedIndexers.Clear();
                        }
                        
                        var js = JsonConvert.SerializeObject(JackettService.SelectedIndexers);
                        await File.WriteAllTextAsync(AppSettingsManager.appSettings.IndexersPath, js);
                        
                        // Refresh the list to update individual checkboxes
                        UpdateIndividualCheckboxes();
                        return;
                    }
                    
                    // Handle individual indexers
                    if (checkBox.IsChecked.Value)
                    {
                        if (!JackettService.SelectedIndexers.Any(x => x.Title == indexer))
                        {
                            JackettService.SelectedIndexers.Add(new Indexer
                            {
                                Id = allIndexers.FirstOrDefault(x=> x.Title == indexer).Id,
                                Title = indexer
                            });
                            
                            var js = JsonConvert.SerializeObject(JackettService.SelectedIndexers);
                            await File.WriteAllTextAsync(AppSettingsManager.appSettings.IndexersPath, js);
                        }
                    }
                    else
                    {
                        if (JackettService.SelectedIndexers.Any(x => x.Title == indexer))
                        {
                            var removedItem =
                                JackettService.SelectedIndexers.FirstOrDefault(x => x.Title == indexer);
                            JackettService.SelectedIndexers.Remove(removedItem);
                            
                            var js = JsonConvert.SerializeObject(JackettService.SelectedIndexers);
                            await File.WriteAllTextAsync(AppSettingsManager.appSettings.IndexersPath, js);
                        }
                    }
                }
            }
        }

        private bool AreAllIndexersSelected()
        {
            if (allIndexers == null || allIndexers.Count == 0) return false;
            return allIndexers.All(indexer => JackettService.SelectedIndexers.Any(x => x.Title == indexer.Title));
        }

        private void UpdateIndividualCheckboxes()
        {
            // Trigger a refresh of the ItemsControl to update checkboxes
            var currentSource = JacketIndexersCheckBoxDisplay.ItemsSource;
            JacketIndexersCheckBoxDisplay.ItemsSource = null;
            JacketIndexersCheckBoxDisplay.ItemsSource = currentSource;
        }

        private void Control_OnLoaded(object? sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                var indexer = checkBox.DataContext as string;
                if (indexer != null)
                {
                    // Set loading flag to prevent events from firing
                    _isLoadingCheckboxes = true;
                    
                    try
                    {
                        // Handle "All" checkbox
                        if (indexer == "All")
                        {
                            // Check if all indexers are selected
                            checkBox.IsChecked = AreAllIndexersSelected();
                        }
                        else
                        {
                            // Handle individual indexers
                            if (JackettService.SelectedIndexers.Any(x => x.Title == indexer))
                            {
                                checkBox.IsChecked = true;
                            }
                        }
                    }
                    finally
                    {
                        // Clear loading flag
                        _isLoadingCheckboxes = false;
                    }
                }
            }
        }

        private void BtnCloseDialog_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs e)
        {
            try
            {
                TextBlockDialogText.Text = "";
                if (currentProcess == CurrentProcess.ChangePassword)
                {
                    TextBoxCurrentPassword.Text = "";
                    TextBoxNewPassword.Text = "";
                    TextBoxNewPasswordAgain.Text = "";
                    ProgressReport.IsVisible = false;
                    StackPanelError.IsVisible = false;
                    StackPanelChangePassword.IsVisible =true;
                    StackPanelChangeUsername.IsVisible = false;
                }
                else if (currentProcess == CurrentProcess.ChangeUsername)
                {
                    TextBoxNewUsername.Text = "";
                    ProgressReport.IsVisible = false;
                    StackPanelError.IsVisible = false;
                    StackPanelChangePassword.IsVisible = false;
                    StackPanelChangeUsername.IsVisible = true;
                }
                else if (currentProcess == CurrentProcess.Tailscale)
                {
                   
                    _ =Task.Run((async () =>
                            {
                                Dispatcher.UIThread.InvokeAsync((() =>
                                {
                                    MarkdownScrollViewer.IsVisible = false;
                                    ProgressReport.IsVisible = true;
                                    StackPanelError.IsVisible = false;
                                    StackPanelChangePassword.IsVisible = false;
                                    StackPanelChangeUsername.IsVisible = false;
                                    TextBlockDialogText.IsVisible = true;
                                    BtnCloseDialog.Content = ResourceProvider.GetString("OkayString");
                                    ProgressInfo.Text = "Installing Tailscale...Do not close the app and click yes to install";
                                }));
                                await TailscaleInstaller.DownloadAndInstallAsync();

                                Dispatcher.UIThread.InvokeAsync((() =>
                                {
                                    MainDialogHost.IsOpen = true;
                                    currentProcess = CurrentProcess.None;
                                    ProgressReport.IsVisible = false;
                                    StackPanelError.IsVisible = true;
                                    StackPanelChangePassword.IsVisible = false;
                                    StackPanelChangeUsername.IsVisible = false;
                                    ProgressInfo.Text = "";
                                    TextBlockDialogText.Text = "Please login to Tailscale to continue...";
                                    BtnCloseDialog.IsVisible = false;
                                }));
                                var ip = TailscaleHelper.GetTailscaleIpFromCli();
                                while (ip == null || String.IsNullOrWhiteSpace(ip))
                                {
                                    ip = TailscaleHelper.GetTailscaleIpFromCli();
                                    await Task.Delay(200);
                                }

                                Dispatcher.UIThread.InvokeAsync((() =>
                                {
                                    TextBlockDialogText.Text = "Success! Your tailscale is installed. Your ip: " + ip;
                                    BtnCloseDialog.IsVisible = true;
                                }));
                            }));
                }
                else
                {
                    MainDialogHost.IsOpen = false;
                    TextBoxCurrentPassword.Text = "";
                    TextBoxNewPassword.Text = "";
                    TextBoxNewPasswordAgain.Text = "";
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CancelButton_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs e)
        {
            MainDialogHost.IsOpen = false;
            currentProcess = CurrentProcess.None;
        }

        private async void ChangePasswordButton_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs e)
        {
            try
            {
                currentProcess = CurrentProcess.ChangePassword;
                var result = await FirestoreManager.ChangePassword( TextBoxCurrentPassword.Text,
                    TextBoxNewPassword.Text,
                    TextBoxNewPasswordAgain.Text
                );
                if (result.Success)
                {
                   // Growl.Success(new GrowlInfo() { Message = App.Current.Resources["PasswordChangeMessage"].ToString(), WaitTime = 4, StaysOpen = false });
                    MainDialogHost.IsOpen = false;
                    AppSettingsManager.appSettings.FireStorePassword = TextBoxNewPassword.Text;
                    AppSettingsManager.SaveAppSettings();;
                    TextBoxCurrentPassword.Text = "";
                    TextBoxNewPassword.Text = "";
                    TextBoxNewPasswordAgain.Text = "";
                    currentProcess = CurrentProcess.None;
                }
                else
                {
                    TextBlockDialogText.Text = result.ErrorMessage;
                    ProgressReport.IsVisible = false;
                    StackPanelError.IsVisible = true;
                    StackPanelChangePassword.IsVisible = false;
                    StackPanelChangeUsername.IsVisible = false;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CancelButtonChangeUsername_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs e)
        {
            MainDialogHost.IsOpen = false;
        }

        private async void ChangeUsernameButton_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs e)
        {
            try
            {
                currentProcess = CurrentProcess.ChangeUsername;
                ProgressReport.IsVisible = true;
                StackPanelError.IsVisible = false;
                StackPanelChangePassword.IsVisible = false;
                StackPanelChangeUsername.IsVisible = false;
                ProgressInfo.Text = App.Current.Resources["Changing"].ToString();
                var result = await FirestoreManager.ChangeUsername( TextBoxNewUsername.Text);
                if (result.Success)
                {
                    //Growl.Success(new GrowlInfo() { Message = App.Current.Resources["UsernameChangeMessage"].ToString(), WaitTime = 4, StaysOpen = false });
                    MainDialogHost.IsOpen = false;
                    //mainAccountPage.TextBlockUsername.Text = TextBoxNewUsername.Text;
                    AppSettingsManager.appSettings.FireStoreDisplayName = TextBoxNewUsername.Text;
                    AppSettingsManager.SaveAppSettings();;
                    TextBoxNewUsername.Text = "";
                    currentProcess = CurrentProcess.None;
                }
                else
                {
                    TextBlockDialogText.Text = result.ErrorMessage;
                    ProgressReport.IsVisible = false;
                    StackPanelError.IsVisible = true;
                    StackPanelChangePassword.IsVisible = false;
                    StackPanelChangeUsername.IsVisible = false;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

     
        
        private void AdjustUIForScreenSize(double width)
        {
            try
            {
                bool isUltraSmallScreen = width <= ULTRA_SMALL_SCREEN_THRESHOLD;
                bool isExtraSmallScreen = width <= EXTRA_SMALL_SCREEN_THRESHOLD && !isUltraSmallScreen;
                bool isSmallScreen = width <= SMALL_SCREEN_THRESHOLD && !isExtraSmallScreen && !isUltraSmallScreen;
                
                // Dil bölümündeki combobox'ların ayarlanması
                AdjustLanguageSectionForScreenSize(width, isUltraSmallScreen, isExtraSmallScreen, isSmallScreen);
                
                // Jackett bölümündeki öğelerin ayarlanması
                AdjustJackettSectionForScreenSize(width, isUltraSmallScreen, isExtraSmallScreen, isSmallScreen);
                
                // Libtorrent bölümündeki öğelerin ayarlanması
                AdjustLibtorrentSectionForScreenSize(width, isUltraSmallScreen, isExtraSmallScreen, isSmallScreen);
                
                // Diğer bölümlerin ayarlanması
                AdjustOtherSectionsForScreenSize(width, isUltraSmallScreen, isExtraSmallScreen, isSmallScreen);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustUIForScreenSize: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void AdjustLanguageSectionForScreenSize(double width, bool isUltraSmallScreen, bool isExtraSmallScreen, bool isSmallScreen)
        {
            try
            {
                // ComboBox genişliklerini ayarla
                double comboBoxWidth = CalculateScaledValue(width, 234, 500);
                ComboBoxTmdbResultLanguage.Width = comboBoxWidth;
                ComboBoxProgramLanguage.Width = comboBoxWidth;
                ComboBoxSubtitleLanguage.Width = comboBoxWidth;
                // Marjin ayarları
                double horizontalMargin = CalculateScaledValue(width, 10, 38);
                
                if (isUltraSmallScreen || width <= 512)
                {
                    // Ultra küçük ekranlarda her panel alt alta gelsin
                    ProgramLanguagePanel.Margin = new Thickness(0, 20, 0, 0);
                    SubtitleLanguagePanel.Margin = new Thickness(0, 20, 0, 0);
                }
                else if (isExtraSmallScreen)
                {
                    // Ekstra küçük ekranlarda ilk ikisi yan yana, üçüncüsü altta olsun
                    ProgramLanguagePanel.Margin = new Thickness(horizontalMargin, 0, 0, 0);
                    SubtitleLanguagePanel.Margin = new Thickness(0, 20, 0, 0);
                }
                else if (width <= 970)
                {
                    ProgramLanguagePanel.Margin = new Thickness(horizontalMargin, 0, 0, 0);
                    SubtitleLanguagePanel.Margin = new Thickness(0, 0, 0, 0);
                }
                else
                {
                    // Normal ekranlarda hepsi yan yana
                    ProgramLanguagePanel.Margin = new Thickness(horizontalMargin, 0, 0, 0);
                    SubtitleLanguagePanel.Margin = new Thickness(horizontalMargin, 0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustLanguageSectionForScreenSize: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void AdjustJackettSectionForScreenSize(double width, bool isUltraSmallScreen, bool isExtraSmallScreen, bool isSmallScreen)
        {
            try
            {
                // TextBox genişliklerini ayarla
                double textBoxWidth = CalculateScaledValue(width, 234, 500);
                TextBoxJackettApiUrl.Width = textBoxWidth;
                TextBoxJackettApiKey.Width = textBoxWidth;
                
                // Marjin ayarları
                double horizontalMargin = CalculateScaledValue(width, 10, 38);
                double scrollViewerHeight = CalculateScaledValue(width, 150, 300);
                
                // ScrollViewer yüksekliğini ayarla
                MoviesDisplayScroll.MaxHeight = scrollViewerHeight;
                
                if (isUltraSmallScreen || width<=530)
                {
                    // Ultra küçük ekranlarda her panel alt alta gelsin
                    JackettApiKeyPanel.Margin = new Thickness(0, 20, 0, 0);
                    JackettIndexersPanel.Margin = new Thickness(0, 20, 0, 0);
                    
                    // Indexers panel genişliğini ayarla
                    JackettIndexersPanel.Width = textBoxWidth;
                }
                else if (isExtraSmallScreen)
                {
                    // Ekstra küçük ekranlarda ilk ikisi yan yana, üçüncüsü altta olsun
                    JackettApiKeyPanel.Margin = new Thickness(horizontalMargin, 0, 0, 0);
                    JackettIndexersPanel.Margin = new Thickness(0, 20, 0, 0);
                    
                    // Indexers panel genişliğini ayarla
                    JackettIndexersPanel.Width = textBoxWidth * 2 + horizontalMargin;
                }
                else if (width <= 877)
                {
                    JackettApiKeyPanel.Margin = new Thickness(horizontalMargin, 0, 0, 0);
                    JackettIndexersPanel.Margin = new Thickness(0, 20, 0, 0);
                    
                    // Indexers panel genişliğini resetle
                    JackettIndexersPanel.Width = double.NaN;
                }
                else
                {
                    // Normal ekranlarda hepsi yan yana
                    JackettApiKeyPanel.Margin = new Thickness(horizontalMargin, 0, 0, 0);
                    JackettIndexersPanel.Margin = new Thickness(horizontalMargin, 0, 0, 0);
                    
                    // Indexers panel genişliğini resetle
                    JackettIndexersPanel.Width = double.NaN;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustJackettSectionForScreenSize: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void AdjustLibtorrentSectionForScreenSize(double width, bool isUltraSmallScreen, bool isExtraSmallScreen, bool isSmallScreen)
        {
            try
            {
                // Tüm TextBox ve ComboBox genişliklerini ayarla
                double controlWidth = CalculateScaledValue(width, 234, 500);
                
                // Sol panel için TextBox'lar
                TextBoxSendSocketBufferSize.Width = controlWidth;
                TextBoxRecvSocketBufferSize.Width = controlWidth;
                TextBoxConnectionsLimit.Width = controlWidth;
                TextBoxUnchokeSlotsLimit.Width = controlWidth;
                TextBoxMaxPeerlistSize.Width = controlWidth;
                TextBoxHalfOpenLimit.Width = controlWidth;
                TextBoxActiveDownloads.Width = controlWidth;
                
                // Orta panel için
                TextBoxCacheSize.Width = controlWidth;
                ComboBoxDiskIOWriteMode.Width = controlWidth;
                ComboBoxDiskIOReadMode.Width = controlWidth;
                TextBoxReadCacheLineSize.Width = controlWidth;
                
                // Sağ panel için
                ComboBoxEncryptionLevel.Width = controlWidth;
                ComboBoxOutgoingTrafficEncryption.Width = controlWidth;
                ComboBoxIncomingTrafficEncryption.Width = controlWidth;
                
                // Font boyutlarını ayarla
                double checkboxFontSize = CalculateScaledValue(width, 14, 18);
                CheckBoxPreferRc4.FontSize = checkboxFontSize;
                CheckBoxEnableDht.FontSize = checkboxFontSize;
                CheckBoxEnableLsd.FontSize = checkboxFontSize;
                CheckBoxEnableNatpmp.FontSize = checkboxFontSize;
                CheckBoxEnableUpnp.FontSize = checkboxFontSize;
                
                // Marjin ayarları
                double horizontalMargin = CalculateScaledValue(width, 15, 40);
                
                if (isUltraSmallScreen)
                {
                    // Ultra küçük ekranlarda tüm paneller alt alta
                    LibtorrentMiddlePanel.Margin = new Thickness(0, 20, 0, 0);
                    LibtorrentRightPanel.Margin = new Thickness(0, 20, 0, 0);
                    LibtorrentCheckBoxPanel.Margin = new Thickness(0, 20, 0, 0);
                }
                else if (isExtraSmallScreen)
                {
                    // Ekstra küçük ekranlarda ilk ikisi yan yana, diğerleri alt alta
                    LibtorrentMiddlePanel.Margin = new Thickness(horizontalMargin, 40, 0, 0);
                    LibtorrentRightPanel.Margin = new Thickness(0, 20, 0, 0);
                    LibtorrentCheckBoxPanel.Margin = new Thickness(0, 20, 0, 0);
                }
                else if (isSmallScreen)
                {
                    // Küçük ekranlarda ilk üçü yan yana, sonuncusu alt alta
                    LibtorrentMiddlePanel.Margin = new Thickness(horizontalMargin, 40, 0, 0);
                    LibtorrentRightPanel.Margin = new Thickness(horizontalMargin, 40, 0, 0);
                    LibtorrentCheckBoxPanel.Margin = new Thickness(0, 20, 0, 0);
                }
                else
                {
                    // Normal ekranlarda hepsi yan yana
                    LibtorrentMiddlePanel.Margin = new Thickness(horizontalMargin, 40, 0, 0);
                    LibtorrentRightPanel.Margin = new Thickness(horizontalMargin, 40, 0, 0);
                    LibtorrentCheckBoxPanel.Margin = new Thickness(horizontalMargin, 40, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustLibtorrentSectionForScreenSize: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void AdjustOtherSectionsForScreenSize(double width, bool isUltraSmallScreen, bool isExtraSmallScreen, bool isSmallScreen)
        {
            try
            {
                // OpenSubtitles bölümü
                if (TextBoxopenSubtitlesApiKey != null)
                {
                    TextBoxopenSubtitlesApiKey.Width = CalculateScaledValue(width, 234, 500);
                }
                
                // Folder Path bölümü
                if (SavePathFolderTextBox != null)
                {
                    SavePathFolderTextBox.Width = CalculateScaledValue(width, 234, 500);
                }
                
                // Başlık fontlarını ayarla
                double headingFontSize = CalculateScaledValue(width, 22, 35);
                // Uygulama içindeki tüm başlıkları bul ve boyutlarını ayarla
                AdjustHeadingFontSizes(headingFontSize);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustOtherSectionsForScreenSize: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void AdjustHeadingFontSizes(double fontSize)
        {
            try
            {
                TextBlockLanguageHeader.FontSize = fontSize;
                JackettHeader.FontSize = fontSize;
                LibtorrentHeader.FontSize = fontSize;
                OpenSubtitlesHeader.FontSize = fontSize;
                ContactUsHeader.FontSize = fontSize;
                SpecialThanksHeader.FontSize = fontSize;
                SavePathHeader.FontSize = fontSize;
                PlayerSettingsHeader.FontSize = fontSize;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustHeadingFontSizes: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private double CalculateScaledValue(double width, double minValue, double maxValue)
        {
            // Ekran genişliği sınırları
            const double minWidth = 320;   // En küçük ekran genişliği (piksel)
            const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
            
            // Ekran genişliğini sınırla
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Doğrusal ölçeklendirme formülü
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);
            
            // Değeri yuvarla
            return Math.Round(scaledValue);
        }
    }
    
    public enum CurrentProcess
    {
        ChangePassword,
        ChangeUsername,
        Tailscale,
        None,
       
    }

  
  
} 