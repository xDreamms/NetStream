using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HandyControl.Controls;
using MaterialDesignThemes.Wpf;
using NetStream.Language;
using HandyControl.Data;
using System.Diagnostics;
using System.IO;
using HandyControl.Tools;
using Newtonsoft.Json;
using DynamicData;
using MessageBox = System.Windows.MessageBox;
using TinifyAPI;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using Path = System.IO.Path;
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page,IDisposable
    {
        private List<string> languages;
        public static SettingsPage GetSettingsPageInstance;
        private CurrentProcess currentProcess;
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
                    AppSettingsManager.SaveAppSettings();;
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
                    AppSettingsManager.SaveAppSettings();;
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
                    AppSettingsManager.SaveAppSettings();;
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
                    AppSettingsManager.SaveAppSettings();;
                }

                //if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.QBittorrrentUrl))
                //{
                //    TextBoxqBittorrentApiUrl.Text = AppSettingsManager.appSettings.QBittorrrentUrl;
                //}
                if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.OpenSubtitlesApiKey))
                {
                    TextBoxopenSubtitlesApiKey.Text = AppSettingsManager.appSettings.OpenSubtitlesApiKey;
                }


                TextBoxSendSocketBufferSize.Text = SessionSettings.send_socket_buffer_size.ToString();
                TextBoxRecvSocketBufferSize.Text = SessionSettings.recv_socket_buffer_size.ToString();
                TextBoxConnectionsLimit.Text = SessionSettings.connections_limit.ToString();
                TextBoxUnchokeSlotsLimit.Text = SessionSettings.UnchokeSlotsLimit.ToString();
                TextBoxMaxPeerlistSize.Text = SessionSettings.MaxPeerListSize.ToString();
                TextBoxHalfOpenLimit.Text = SessionSettings.half_open_limit.ToString();
                TextBoxActiveDownloads.Text = SessionSettings.active_downloads.ToString();
                TextBoxCacheSize.Text = SessionSettings.CacheSize.ToString();
                TextBoxReadCacheLineSize.Text = SessionSettings.read_cache_line_size.ToString();
                CheckBoxPreferRc4.IsChecked = SessionSettings.prefer_rc4;
                CheckBoxEnableDht.IsChecked = SessionSettings.enable_dht;
                CheckBoxEnableLsd.IsChecked = SessionSettings.enable_lsd;
                CheckBoxEnableNatpmp.IsChecked = SessionSettings.enable_natpmp;
                CheckBoxEnableUpnp.IsChecked = SessionSettings.enable_upnp;


                TextBoxSendSocketBufferSize.TextChanged += TextBoxSendSocketBufferSizeOnTextChanged;
                TextBoxRecvSocketBufferSize.TextChanged += TextBoxRecvSocketBufferSizeOnTextChanged;
                TextBoxConnectionsLimit.TextChanged += TextBoxConnectionsLimitOnTextChanged;
                TextBoxUnchokeSlotsLimit.TextChanged += TextBoxUnchokeSlotsLimitOnTextChanged;
                TextBoxMaxPeerlistSize.TextChanged+= TextBoxMaxPeerlistSizeOnTextChanged;
                TextBoxHalfOpenLimit.TextChanged+= TextBoxHalfOpenLimitOnTextChanged;
                TextBoxActiveDownloads.TextChanged+= TextBoxActiveDownloadsOnTextChanged;
                TextBoxCacheSize.TextChanged+= TextBoxCacheSizeOnTextChanged;
                TextBoxReadCacheLineSize.TextChanged+= TextBoxReadCacheLineSizeOnTextChanged;
                CheckBoxPreferRc4.Checked += CheckBoxPreferRc4OnChecked;
                CheckBoxPreferRc4.Unchecked += CheckBoxPreferRc4OnUnchecked;
                CheckBoxEnableDht.Checked+= CheckBoxEnableDhtOnChecked;
                CheckBoxEnableDht.Unchecked+= CheckBoxEnableDhtOnUnchecked;
                CheckBoxEnableLsd.Checked+= CheckBoxEnableLsdOnChecked;
                CheckBoxEnableLsd.Unchecked+= CheckBoxEnableLsdOnUnchecked;
                CheckBoxEnableNatpmp.Checked+= CheckBoxEnableNatpmpOnChecked;
                CheckBoxEnableNatpmp.Unchecked+= CheckBoxEnableNatpmpOnUnchecked;
                CheckBoxEnableUpnp.Checked+= CheckBoxEnableUpnpOnChecked;
                CheckBoxEnableUpnp.Unchecked+= CheckBoxEnableUpnpOnUnchecked;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CheckBoxEnableUpnpOnUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionSettings.enable_upnp = false;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CheckBoxEnableUpnpOnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionSettings.enable_upnp = true;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CheckBoxEnableNatpmpOnUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionSettings.enable_natpmp = false;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CheckBoxEnableNatpmpOnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionSettings.enable_natpmp = true;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CheckBoxEnableLsdOnUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionSettings.enable_lsd = false;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CheckBoxEnableLsdOnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionSettings.enable_lsd = true;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CheckBoxEnableDhtOnUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionSettings.enable_dht = false;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CheckBoxEnableDhtOnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionSettings.enable_dht = true;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CheckBoxPreferRc4OnUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionSettings.prefer_rc4 = false;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CheckBoxPreferRc4OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionSettings.prefer_rc4 = true;
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

        private void TextBoxReadCacheLineSizeOnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxReadCacheLineSize.Text, out result))
                {
                    SessionSettings.read_cache_line_size = result;
                }
                else
                {
                    TextBoxReadCacheLineSize.Text = SessionSettings.read_cache_line_size.ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxCacheSizeOnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxCacheSize.Text, out result))
                {
                    SessionSettings.CacheSize = result;
                }
                else
                {
                    TextBoxCacheSize.Text = SessionSettings.CacheSize.ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxActiveDownloadsOnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxActiveDownloads.Text, out result))
                {
                    SessionSettings.active_downloads = result;
                }
                else
                {
                    TextBoxActiveDownloads.Text = SessionSettings.active_downloads.ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }

        }

        private void TextBoxHalfOpenLimitOnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxHalfOpenLimit.Text, out result))
                {
                    SessionSettings.half_open_limit = result;
                }
                else
                {
                    TextBoxHalfOpenLimit.Text = SessionSettings.half_open_limit.ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxMaxPeerlistSizeOnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxMaxPeerlistSize.Text, out result))
                {
                    SessionSettings.MaxPeerListSize = result;
                }
                else
                {
                    TextBoxMaxPeerlistSize.Text = SessionSettings.MaxPeerListSize.ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxUnchokeSlotsLimitOnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxUnchokeSlotsLimit.Text, out result))
                {
                    SessionSettings.UnchokeSlotsLimit = result;
                }
                else
                {
                    TextBoxUnchokeSlotsLimit.Text = SessionSettings.UnchokeSlotsLimit.ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxConnectionsLimitOnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxConnectionsLimit.Text, out result))
                {
                    SessionSettings.ConnectionLimit = result;
                }
                else
                {
                    TextBoxConnectionsLimit.Text = SessionSettings.ConnectionLimit.ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxRecvSocketBufferSizeOnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxRecvSocketBufferSize.Text, out result))
                {
                    SessionSettings.recv_socket_buffer_size = result;
                }
                else
                {
                    TextBoxRecvSocketBufferSize.Text = SessionSettings.recv_socket_buffer_size.ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxSendSocketBufferSizeOnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int result;
                if (TryExtractWholeNumber(TextBoxSendSocketBufferSize.Text, out result))
                {
                    SessionSettings.send_socket_buffer_size = result;
                }
                else
                {
                    TextBoxSendSocketBufferSize.Text = SessionSettings.send_socket_buffer_size.ToString();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void FillComboBoxes()
        {
            try
            {
                languages = await Service.GetLanguages();
                ComboBoxTmdbResultLanguage.ItemsSource = languages;
                ComboBoxProgramLanguage.ItemsSource = Service.Languages.Where(x=> App.SupportedProgramLanguages.Any(z=> z == x.Iso_639_1))
                    .Select(m=> m.EnglishName).OrderBy(x=> x);

                var subtitleLanguages = SubtitleHandler.LanguagesToOpenSubtitles.Select(x => x.Key);
                var subLanguages = Service.Languages.Where(x => subtitleLanguages.Any(z => z == x.Iso_639_1))
                    .Select(m => m.EnglishName).OrderBy(x => x).ToList();
                subLanguages.Insert(0, "Disabled");
                ComboBoxSubtitleLanguage.ItemsSource = subLanguages;

                var diskIoModes = new List<string>
                {
                    "Enable OS Cache" ,
                    "Disable OS Cache" ,
                    "Write Through" 
                };
                ComboBoxDiskIOWriteMode.ItemsSource = diskIoModes;
                ComboBoxDiskIOReadMode.ItemsSource = diskIoModes;
            
                switch ((DiskIOMode)SessionSettings.disk_io_write_mode)
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

                switch ((DiskIOMode)SessionSettings.disk_io_read_mode)
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

                switch ((EncryptionLevel)SessionSettings.allowed_enc_level)
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

                switch ((EncryptionPolicy)SessionSettings.out_enc_policy)
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

                switch ((EncryptionPolicy)SessionSettings.in_enc_policy)
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


                ConfigHelper.Instance.SetLang("en");
                bool success = false;

                while (!success)
                {
                    try
                    {
                        var indexers = await JackettService.GetIndexersAsync();
                        var indexerList = indexers.Select(x => x.Title).OrderBy(x => x).ToList();
                        ComboBoxJacketIndexers.ItemsSource = indexerList;

                        if (JackettService.SelectedIndexers != null && JackettService.SelectedIndexers.Count > 0)
                        {
                            foreach (var item in ComboBoxJacketIndexers.Items)
                            {
                                if (JackettService.SelectedIndexers.Any(x => x.Title == item.ToString()))
                                {
                                    if (!ComboBoxJacketIndexers.SelectedItems.Contains(item))
                                    {
                                        ComboBoxJacketIndexers.SelectedItems.Add(item);
                                    }
                                }
                            }
                        }
                        else
                        {
                            JackettService.SelectedIndexers = JsonConvert.DeserializeObject<List<Indexer>>(
                                File.ReadAllText(AppSettingsManager.appSettings.IndexersPath));

                            foreach (var item in ComboBoxJacketIndexers.Items)
                            {
                                if (JackettService.SelectedIndexers.Any(x => x.Title == item.ToString()))
                                {
                                    if (!ComboBoxJacketIndexers.SelectedItems.Contains(item))
                                    {
                                        ComboBoxJacketIndexers.SelectedItems.Add(item);
                                    }
                                }
                            }
                        }
                        success = true;
                    }
                    catch (Exception e)
                    {
                        success = false;
                    }
                }

                ComboBoxJacketIndexers.SelectionChanged += ComboBoxJacketIndexersOnSelectionChanged;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ComboBoxIncomingTrafficEncryptionOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (ComboBoxIncomingTrafficEncryption.SelectedItem.ToString())
                {
                    case "Enforce Encryption":
                        SessionSettings.in_enc_policy = (int)EncryptionPolicy.PeForced;
                        break;
                    case "Enable Encryption":
                        SessionSettings.in_enc_policy = (int)EncryptionPolicy.PeEnabled;
                        break;
                    case "Disable Encryption":
                        SessionSettings.in_enc_policy = (int)EncryptionPolicy.PeDisabled;
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ComboBoxOutgoingTrafficEncryptionOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (ComboBoxOutgoingTrafficEncryption.SelectedItem.ToString())
                {
                    case "Enforce Encryption":
                        SessionSettings.out_enc_policy = (int)EncryptionPolicy.PeForced;
                        break;
                    case "Enable Encryption":
                        SessionSettings.out_enc_policy = (int)EncryptionPolicy.PeEnabled;
                        break;
                    case "Disable Encryption":
                        SessionSettings.out_enc_policy = (int)EncryptionPolicy.PeDisabled;
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ComboBoxEncryptionLevelOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (ComboBoxEncryptionLevel.SelectedItem.ToString())
                {
                    case "RC4 and Plain Text (Recommended)":
                        SessionSettings.allowed_enc_level = (int)EncryptionLevel.PeBoth;
                        break;
                    case "Just RC4":
                        SessionSettings.allowed_enc_level = (int)EncryptionLevel.PeRc4;
                        break;
                    case "No Encryption (Risky!)":
                        SessionSettings.allowed_enc_level = (int)EncryptionLevel.PePlaintext;
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ComboBoxDiskIOReadModeOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (ComboBoxDiskIOReadMode.SelectedItem.ToString())
                {
                    case "Enable OS Cache":
                        SessionSettings.disk_io_read_mode = (int)DiskIOMode.EnableOsCache;
                        break;
                    case "Disable OS Cache":
                        SessionSettings.disk_io_read_mode = (int)DiskIOMode.DisableOsCache;
                        break;
                    case "Write Through":
                        SessionSettings.disk_io_read_mode = (int)DiskIOMode.WriteThrough;
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ComboBoxDiskIOWriteModeOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (ComboBoxDiskIOWriteMode.SelectedItem.ToString())
                {
                    case "Enable OS Cache":
                        SessionSettings.disk_io_write_mode = (int)DiskIOMode.EnableOsCache;
                        break;
                    case "Disable OS Cache":
                        SessionSettings.disk_io_write_mode = (int)DiskIOMode.DisableOsCache;
                        break;
                    case "Write Through":
                        SessionSettings.disk_io_write_mode = (int)DiskIOMode.WriteThrough;
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ComboBoxJacketIndexersOnSelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void ButtonOpenFolder_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var folderDialog = new OpenFolderDialog();

                if (folderDialog.ShowDialog() == true)
                {
                    var folderName = folderDialog.FolderName;
                    SavePathFolderTextBox.Text = folderName;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ComboBoxTmdbResultLanguage_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private async void ComboBoxProgramLanguage_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
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

        

        private async void ComboBoxSubtitleLanguage_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void TextBoxJackettApiUrl_OnTextChanged(object sender, TextChangedEventArgs e)
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

        private void TextBoxJackettApiKey_OnTextChanged(object sender, TextChangedEventArgs e)
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

        private async void SavePathFolderTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
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

        private void PickerControlBase_OnColorChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                Color currentColor = new Color();
                currentColor.A = (byte)PrimaryColorPicker.Color.A;
                currentColor.R = (byte)PrimaryColorPicker.Color.RGB_R;
                currentColor.G = (byte)PrimaryColorPicker.Color.RGB_G;
                currentColor.B = (byte)PrimaryColorPicker.Color.RGB_B;
                App.Current.Resources["ColorDefault"] = currentColor;
                App.Current.Resources["BrushDefault"] = new SolidColorBrush(currentColor);
                SetPrimaryColor(currentColor);

                AppSettingsManager.appSettings.PrimaryColorAlpha = currentColor.A;
                AppSettingsManager.appSettings.PrimaryColorRed = currentColor.R;
                AppSettingsManager.appSettings.PrimaryColorGreen = currentColor.G;
                AppSettingsManager.appSettings.PrimaryColorBlue = currentColor.B;
                AppSettingsManager.SaveAppSettings();;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void SetPrimaryColor(Color color)
        {
            try
            {
                PaletteHelper paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                theme.SetPrimaryColor(color);
                paletteHelper.SetTheme(theme);
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        //private void TextBoxqBittorrentApiUrl_OnTextChanged(object sender, TextChangedEventArgs e)
        //{
        //    AppSettingsManager.appSettings.QBittorrrentUrl = TextBoxqBittorrentApiUrl.Text;
        //    AppSettingsManager.SaveAppSettings();;
        //}


        private async void TextBoxopenSubtitlesApiKey_OnTextChanged(object sender, TextChangedEventArgs e)
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

        private void BtnCloseDialog_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                TextBlockDialogText.Text = "";
                if (currentProcess == CurrentProcess.ChangePassword)
                {
                    TextBoxCurrentPassword.Password = "";
                    TextBoxNewPassword.Password = "";
                    TextBoxNewPasswordAgain.Password = "";
                    ProgressReport.Visibility = Visibility.Collapsed;
                    StackPanelError.Visibility = Visibility.Collapsed;
                    StackPanelChangePassword.Visibility = Visibility.Visible;
                    StackPanelChangeUsername.Visibility = Visibility.Collapsed;
                }
                else if (currentProcess == CurrentProcess.ChangeUsername)
                {
                    TextBoxNewUsername.Text = "";
                    ProgressReport.Visibility = Visibility.Collapsed;
                    StackPanelError.Visibility = Visibility.Collapsed;
                    StackPanelChangePassword.Visibility = Visibility.Collapsed;
                    StackPanelChangeUsername.Visibility = Visibility.Visible;
                }
                else
                {
                    DialogHost.IsOpen = false;
                    TextBoxCurrentPassword.Password = "";
                    TextBoxNewPassword.Password = "";
                    TextBoxNewPasswordAgain.Password = "";
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
           
        }

        private void CancelButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DialogHost.IsOpen = false;
            currentProcess = CurrentProcess.None;
        }

        private async void ChangePasswordButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                currentProcess = CurrentProcess.ChangePassword;
                var result = await FirestoreManager.ChangePassword( TextBoxCurrentPassword.Password,
                    TextBoxNewPassword.Password,
                    TextBoxNewPasswordAgain.Password
                );
                if (result.Success)
                {
                    Growl.Success(new GrowlInfo() { Message = App.Current.Resources["PasswordChangeMessage"].ToString(), WaitTime = 4, StaysOpen = false });
                    DialogHost.IsOpen = false;
                    AppSettingsManager.appSettings.FireStorePassword = TextBoxNewPassword.Password;
                    AppSettingsManager.SaveAppSettings();;
                    TextBoxCurrentPassword.Password = "";
                    TextBoxNewPassword.Password = "";
                    TextBoxNewPasswordAgain.Password = "";
                    currentProcess = CurrentProcess.None;
                }
                else
                {
                    TextBlockDialogText.Text = result.ErrorMessage;
                    ProgressReport.Visibility = Visibility.Collapsed;
                    StackPanelError.Visibility = Visibility.Visible;
                    StackPanelChangePassword.Visibility = Visibility.Collapsed;
                    StackPanelChangeUsername.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CancelButtonChangeUsername_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DialogHost.IsOpen = false;
        }

        private async void ChangeUsernameButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                currentProcess = CurrentProcess.ChangeUsername;
                ProgressReport.Visibility = Visibility.Visible;
                StackPanelError.Visibility = Visibility.Collapsed;
                StackPanelChangePassword.Visibility = Visibility.Collapsed;
                StackPanelChangeUsername.Visibility = Visibility.Collapsed;
                ProgressInfo.Text = App.Current.Resources["Changing"].ToString();
                var result = await FirestoreManager.ChangeUsername( TextBoxNewUsername.Text);
                if (result.Success)
                {
                    Growl.Success(new GrowlInfo() { Message = App.Current.Resources["UsernameChangeMessage"].ToString(), WaitTime = 4, StaysOpen = false });
                    DialogHost.IsOpen = false;
                    mainAccountPage.TextBlockUsername.Text = TextBoxNewUsername.Text;
                    AppSettingsManager.appSettings.FireStoreDisplayName = TextBoxNewUsername.Text;
                    AppSettingsManager.SaveAppSettings();;
                    TextBoxNewUsername.Text = "";
                    currentProcess = CurrentProcess.None;
                }
                else
                {
                    TextBlockDialogText.Text = result.ErrorMessage;
                    ProgressReport.Visibility = Visibility.Collapsed;
                    StackPanelError.Visibility = Visibility.Visible;
                    StackPanelChangePassword.Visibility = Visibility.Collapsed;
                    StackPanelChangeUsername.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private MainAccountPage mainAccountPage;
        private void SettingsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            mainAccountPage = new MainAccountPage(this);
            AccountNav.Navigate(mainAccountPage);
        }

        private void BtnDiscord_OnMouseEnter(object sender, MouseEventArgs e)
        {
            
        }

        private void BtnDiscord_OnMouseLeave(object sender, MouseEventArgs e)
        {
            
        }

        private void BtnDiscord_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://discord.gg/SF3P59xGyW") { UseShellExecute = true });
        }

        private void PlayerSettingShowThumbnails_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.PlayerSettingShowThumbnail = true;
                AppSettingsManager.SaveAppSettings();;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void PlayerSettingShowThumbnails_OnUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.PlayerSettingShowThumbnail = false;
                AppSettingsManager.SaveAppSettings();;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void PlayerSettingAutoSync_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.PlayerSettingAutoSync = true;
                AppSettingsManager.SaveAppSettings();;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void PlayerSettingAutoSync_OnUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                AppSettingsManager.appSettings.PlayerSettingAutoSync = false;
                AppSettingsManager.SaveAppSettings();;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void PlayerSettingAutoSync_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PlayerSettingAutoSync.IsChecked = AppSettingsManager.appSettings.PlayerSettingAutoSync;
                PlayerSettingAutoSync.Checked += PlayerSettingAutoSync_OnChecked;
                PlayerSettingAutoSync.Unchecked += PlayerSettingAutoSync_OnUnchecked;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void PlayerSettingShowThumbnails_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PlayerSettingShowThumbnails.IsChecked = AppSettingsManager.appSettings.PlayerSettingShowThumbnail;
                PlayerSettingShowThumbnails.Checked += PlayerSettingShowThumbnails_OnChecked;
                PlayerSettingShowThumbnails.Unchecked += PlayerSettingShowThumbnails_OnUnchecked;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }

        }

        private void SettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void OpenSubtitlesGetApiKey_OnMouseEnter(object sender, MouseEventArgs e)
        {
            OpenSubtitlesGetApiKey.TextDecorations = System.Windows.TextDecorations.Underline;
        }

        private void OpenSubtitlesGetApiKey_OnMouseLeave(object sender, MouseEventArgs e)
        {
            OpenSubtitlesGetApiKey.TextDecorations = null;
        }

        private void OpenSubtitlesGetApiKey_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.opensubtitles.com/") { UseShellExecute = true });
        }

        public void Dispose()
        {
            PlayerSettingAutoSync.Checked -= PlayerSettingAutoSync_OnChecked;
            PlayerSettingAutoSync.Unchecked -= PlayerSettingAutoSync_OnUnchecked;


            PlayerSettingShowThumbnails.Checked -= PlayerSettingShowThumbnails_OnChecked;
            PlayerSettingShowThumbnails.Unchecked -= PlayerSettingShowThumbnails_OnUnchecked;

            ComboBoxJacketIndexers.SelectionChanged -= ComboBoxJacketIndexersOnSelectionChanged;
        }

       
    }

    public enum CurrentProcess
    {
        ChangePassword,
        ChangeUsername,
        None
    }

    public enum DiskIOMode 
    {
        EnableOsCache = 0,
        DisableOsCache = 2,
        WriteThrough = 3,
    }

    public enum EncryptionLevel 
    {
        PePlaintext = 1,
        PeRc4 = 2,
        PeBoth = 3
    }

    public enum EncryptionPolicy 
    {
        PeForced = 0,
        PeEnabled = 1,
        PeDisabled = 2
    }

    
}
