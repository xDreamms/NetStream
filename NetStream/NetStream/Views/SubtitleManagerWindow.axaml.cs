using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NETCore.Encrypt;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Path = System.IO.Path;

namespace NetStream.Views
{
    public partial class SubtitleManagerWindow : UserControl
    {
        public Subtitle currentSubtitle;
        private string path;
        private string movieName;
        public bool changedSubtitle = false;
        private int imdbId;
        
        public SubtitleManagerWindow()
        {
            InitializeComponent();
        }
        PlayerWindow playerWindow;
        public SubtitleManagerWindow(PlayerWindow playerWindow,Subtitle currentSubitle, string path, string movieName, int imdbId, int showId, ShowType showType, int year, int seasonNumber, int episodeNumber)
        {
            InitializeComponent();
            this.playerWindow = playerWindow;
            this.currentSubtitle = currentSubitle;
            this.path = path;
            this.movieName = movieName;
            this.imdbId = imdbId;
            this.showId = showId;
            this.showType = showType;
            this.year = year;
            this.seasonNumber = seasonNumber;
            this.episodeNumber = episodeNumber;
            LoadSubtitleLanguages();
            Log.Information($"Opened Subtitle Manager Window Movie Name: {movieName} Path:{path}");
        }

        public SubtitleManagerWindow(PlayerWindow playerWindow,int movieId, string path, string movieName, int imdbId, ShowType showType, int year, int seasonNumber, int episodeNumber)
        {
            InitializeComponent();
            this.playerWindow = playerWindow;
            this.path = path;
            this.movieName = movieName;
            this.imdbId = imdbId;
            this.showId = movieId;
            this.showType = showType;
            this.year = year;
            this.seasonNumber = seasonNumber;
            this.episodeNumber = episodeNumber;
            LoadSubtitleLanguages();
            Log.Information($"Opened Subtitle Manager Window Movie Name: {movieName} Path:{path}");
        }

        private async void LoadSubtitleLanguages()
        {
            try
            {
                var subtitleLanguages = SubtitleHandler.LanguagesToOpenSubtitles.Select(x => x.Key);
                var subLanguages = Service.Languages.Where(x => subtitleLanguages.Any(z => z == x.Iso_639_1))
                    .Select(m => m.EnglishName).OrderBy(x => x).ToList();
                subLanguages.Insert(0, "Disabled");
                
                var comboBoxSubtitleLanguage = this.FindControl<ComboBox>("ComboBoxSubtitleLanguage");
                comboBoxSubtitleLanguage.ItemsSource = subLanguages;
                
                if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.SubtitleLanguage))
                {
                    comboBoxSubtitleLanguage.SelectedItem = AppSettingsManager.appSettings.SubtitleLanguage;
                }
                else
                {
                    comboBoxSubtitleLanguage.SelectedItem = "English";
                    AppSettingsManager.appSettings.SubtitleLanguage = "English";
                    AppSettingsManager.appSettings.IsoSubtitleLanguage = await GetLanguage("English");
                    AppSettingsManager.SaveAppSettings();
                }

                comboBoxSubtitleLanguage.SelectionChanged += ComboBoxSubtitleLanguageOnSelectionChanged;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ComboBoxSubtitleLanguageOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var comboBoxSubtitleLanguage = this.FindControl<ComboBox>("ComboBoxSubtitleLanguage");
                var menuGrid = this.FindControl<Grid>("MenuGrid");
                
                if (comboBoxSubtitleLanguage.SelectedItem == null) return;
                var selectedItem = comboBoxSubtitleLanguage.SelectedItem.ToString();
                if (selectedItem == "Disabled")
                {
                    menuGrid.IsVisible = false;
                    SubtitlesDisplay.IsVisible = false;
                    disabledSubtitle = true;
                    AppSettingsManager.appSettings.SubtitleLanguage = "Disabled";
                    AppSettingsManager.appSettings.IsoSubtitleLanguage = "Disabled";
                    AppSettingsManager.SaveAppSettings();
                }
                else
                {
                    menuGrid.IsVisible = true;
                    SubtitlesDisplay.IsVisible = true;
                    AppSettingsManager.appSettings.SubtitleLanguage = comboBoxSubtitleLanguage.SelectedItem.ToString();
                    AppSettingsManager.appSettings.IsoSubtitleLanguage =
                        await GetLanguage(comboBoxSubtitleLanguage.SelectedItem.ToString());
                    AppSettingsManager.SaveAppSettings();
                    
                    ObservableCollection<Subtitle> subtitles;
                    if (subtitleType == SubtitleType.Com)
                    {
                        subtitles = await SubtitleHandler.SearchSubtitle(showId, seasonNumber, episodeNumber,
                            AppSettingsManager.appSettings.IsoSubtitleLanguage
                            , path, movieName, imdbId);
                    }
                    else
                    {
                        subtitles = await SubtitleHandler.GetSubtitles(showType, movieName, showId, year,
                            seasonNumber, episodeNumber, AppSettingsManager.appSettings.IsoSubtitleLanguage, "tt" + imdbId, false);
                    }
                    
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SubtitlesDisplay.ItemsSource = null;
                        SubtitlesDisplay.ItemsSource = subtitles;
                    });

                    changedSubtitle = true;
                    disabledSubtitle = false;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private int showId;
        ShowType showType;
        private int year;
        private int seasonNumber;
        private int episodeNumber;
        private string language;

        private SubtitleType subtitleType = SubtitleType.Com;

        private async void SubtitleManagerWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AppSettingsManager.appSettings.SubtitleLanguage == "Disabled")
                {
                    SubtitlesDisplay.IsVisible = false;
                    MenuGrid.IsVisible = false;
                }
                else
                {
                    if (SubtitleHandler.remaining.HasValue)
                    {
                        DownloadRemaining.Text = "Remaining: " + SubtitleHandler.remaining.Value;
                    }
                    if (currentSubtitle == null)
                    {
                        SubtitlesDisplay.ItemsSource = await SubtitleHandler.SearchSubtitle(showId, seasonNumber,
                            episodeNumber, AppSettingsManager.appSettings.IsoSubtitleLanguage, path, movieName, imdbId);
                    }
                    else
                    {
                        SubtitlesDisplay.ItemsSource = await SubtitleHandler.SearchSubtitle(currentSubtitle, path, movieName, imdbId);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void SubtitlesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Not implemented in Avalonia yet
        }
        

        private void StackPanelSubtitle_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentSubtitle == null) return;
                var stackpanel = sender as StackPanel;
                if (stackpanel.DataContext is Subtitle)
                {
                    var subtitle = stackpanel.DataContext as Subtitle;
                    if (subtitle.SubtitleId == currentSubtitle.SubtitleId)
                    {
                        stackpanel.Background = new SolidColorBrush(Color.FromRgb(30, 219, 98));
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
             try
             {
                if (currentSubtitle == null) return;
                var button = sender as Button;
                if (button.DataContext is Subtitle)
                {
                    var subtitle = button.DataContext as Subtitle;
                    if (subtitle.SubtitleId == currentSubtitle.SubtitleId)
                    {
                        button.IsVisible = false;
                    }
                }
             }
            catch (Exception exception)
             {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
             }
        }

        private async void UIElement_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var button = sender as Button;
                if (button.DataContext is Subtitle)
                {
                    var selectedSubtitle = button.DataContext as Subtitle;

                    if (selectedSubtitle != null)
                    {
                    if (subtitleType == SubtitleType.Com)
                    {
                        if (currentSubtitle == null || (currentSubtitle != null && currentSubtitle.SubtitleId != selectedSubtitle.SubtitleId))
                        {
                            var subtitlePath = await SubtitleHandler.DownloadSubtitleWithoutSync(selectedSubtitle.FileId.Value, selectedSubtitle.FileName);
                                foreach (var subtitle in PlayerWindow.subtitles)
                                {
                                    if (subtitle.MovieId == selectedSubtitle.MovieId
                                        && subtitle.EpisodeNumber == selectedSubtitle.EpisodeNumber
                                        && subtitle.SeasonNumber == selectedSubtitle.SeasonNumber
                                        && subtitle.Language == selectedSubtitle.Language)
                                    {
                                        subtitle.Name = Path.GetFileName(subtitlePath);
                                        subtitle.Fullpath = subtitlePath;
                                        subtitle.HashDownload = true;
                                        subtitle.Synchronized = true;
                                        subtitle.SubtitleId = selectedSubtitle.SubtitleId;
                                changedSubtitle = true;
                                    }
                            }
                            
                            File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));

                            CloseWindow();
                        }
                        else
                        {
                            CloseWindow();
                        }
                    }
                    else
                    {
                        if (currentSubtitle == null || (currentSubtitle != null && currentSubtitle.SubtitleId != selectedSubtitle.SubtitleId))
                        {
                            var downloadedSubtitle = await SubtitleHandler.DownloadSubtitleWithoutSync(selectedSubtitle);
                            if (downloadedSubtitle != null)
                            {
                                    foreach (var subtitle in PlayerWindow.subtitles)
                                    {
                                        if (subtitle.MovieId == selectedSubtitle.MovieId
                                            && subtitle.EpisodeNumber == selectedSubtitle.EpisodeNumber
                                            && subtitle.SeasonNumber == selectedSubtitle.SeasonNumber
                                            && subtitle.Language == selectedSubtitle.Language)
                                        {
                                            subtitle.Name = Path.GetFileName(downloadedSubtitle.Fullpath);
                                            subtitle.Fullpath = downloadedSubtitle.Fullpath;
                                            subtitle.HashDownload = true;
                                            subtitle.Synchronized = true;
                                            subtitle.SubtitleId = selectedSubtitle.SubtitleId;
                                            subtitle.IsOrg = true;
                                    changedSubtitle = true;
                                        }
                                }
                                
                                File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));

                                CloseWindow();
                            }
                        }
                        else
                        {
                            CloseWindow();
                        }
                    }
                }
            }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MakeSubtitlesMenu_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                var menuLine = this.FindControl<Rectangle>("MenuLine");
                var makeSubtitlesMenu = this.FindControl<TextBlock>("MakeSubtitlesMenu");
                var openSubtitlesComMenu = this.FindControl<TextBlock>("OpenSubtitlesComMenu");
                var makeSubtitlesView = this.FindControl<Grid>("MakeSubtitlesView");
                var subtitlesDisplayScrollViewer = this.FindControl<ScrollViewer>("SubtitlesDisplayScrollViewer");
                var subtitlesDisplay = this.FindControl<ItemsControl>("SubtitlesDisplay");

                Grid.SetColumn(menuLine, 3);
                makeSubtitlesMenu.Foreground = new SolidColorBrush(Colors.White);
                openSubtitlesComMenu.Foreground = new SolidColorBrush(Color.Parse("#414141"));

                makeSubtitlesView.IsVisible = true;
                subtitlesDisplayScrollViewer.IsVisible = false;
                subtitlesDisplay.IsVisible = false;

                subtitleType = SubtitleType.Org; // Using Org enum for this new mode for now

                await GenerateSubtitles();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
                AppendLog(errorMessage);
            }
        }

        private async Task GenerateSubtitles()
        {
            try
            {
                AppendLog("Starting Subtitle Generation...");
                UpdateProgress(0, "Searching for English subtitles...");

                // Step 1: Get target language from ComboBox
                string targetLangName = "English";
                var comboBox = this.FindControl<ComboBox>("ComboBoxSubtitleLanguage");
                if (comboBox.SelectedItem != null)
                {
                    targetLangName = comboBox.SelectedItem.ToString();
                }

                // Get ISO code for target language
                string targetLangIso = await GetLanguage(targetLangName);
                if (string.IsNullOrWhiteSpace(targetLangIso))
                {
                    AppendLog("ERROR: Could not resolve target language ISO code.");
                    return;
                }

                AppendLog($"Target language: {targetLangName} ({targetLangIso})");

                // Step 2: Search for English subtitles on OpenSubtitles
                UpdateProgress(10, "Searching OpenSubtitles for English subtitles...");
                AppendLog("Searching OpenSubtitles.com for English subtitles...");

                Subtitle englishSub = null;

                // Try by TMDB ID first
                if (showId > 0)
                {
                    if (seasonNumber > 0 && episodeNumber > 0)
                    {
                        // TV Show episode
                        englishSub = await SubtitleHandler.GetSubtitlesByNameAndEpisode(
                            movieName, seasonNumber, episodeNumber, new List<string> { "en" }, imdbId);
                    }
                    else
                    {
                        // Movie
                        englishSub = await SubtitleHandler.GetSubtitlesByTMDbId(
                            movieName, showId, new List<string> { "en" });
                    }
                }

                // Fallback: search by file name
                if ((englishSub == null || string.IsNullOrWhiteSpace(englishSub.Fullpath)) && !string.IsNullOrWhiteSpace(path))
                {
                    AppendLog("TMDB search returned no results. Trying by file name...");
                    englishSub = await SubtitleHandler.GetSubtitlesByName(path, new List<string> { "en" });
                }

                // Fallback: try hash-based search
                if ((englishSub == null || string.IsNullOrWhiteSpace(englishSub.Fullpath)) && !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    AppendLog("Name search returned no results. Trying by file hash...");
                    englishSub = await SubtitleHandler.GetSubtitlesByHash(path, new List<string> { "en" }, movieName);
                }

                if (englishSub == null || string.IsNullOrWhiteSpace(englishSub.Fullpath))
                {
                    AppendLog("ERROR: Could not find English subtitles on OpenSubtitles.");
                    UpdateProgress(0, "No English subtitles found");
                    return;
                }

                AppendLog($"English subtitle downloaded: {englishSub.Name}");
                UpdateProgress(30, "English subtitle found. Starting translation...");

                // Step 3: If target language is English, no translation needed
                if (targetLangIso == "en")
                {
                    AppendLog("Target language is English - no translation needed.");
                    UpdateProgress(100, "Done!");
                    AddGeneratedSubtitleToApp(englishSub.Fullpath, targetLangIso);
                    await Task.Delay(1500);
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWindow());
                    return;
                }

                // Step 4: Translate the SRT file from English to target language
                string subtitlesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetStream", "Subtitles");
                if (!Directory.Exists(subtitlesDir)) Directory.CreateDirectory(subtitlesDir);

                string baseName = Path.GetFileNameWithoutExtension(
                    !string.IsNullOrWhiteSpace(path) ? path : movieName);
                string outputFileName = $"{baseName}_{targetLangName.ToUpper()}_TRANSLATED.srt";
                string outputPath = Path.Combine(subtitlesDir, outputFileName);

                var translator = new NetStream.Services.SrtTranslatorService();
                translator.StatusChanged += (s, msg) => AppendLog(msg);
                translator.ProgressChanged += (s, progress) =>
                {
                    // Map translation progress (0-100) to our progress bar (30-95)
                    double mappedProgress = 30 + (progress * 0.65);
                    UpdateProgress(mappedProgress, $"Translating... {progress:F0}%");
                };

                string resultPath = await translator.TranslateSrtFileAsync(
                    englishSub.Fullpath, targetLangIso, outputPath);

                if (!string.IsNullOrWhiteSpace(resultPath) && File.Exists(resultPath))
                {
                    UpdateProgress(100, "Translation Complete!");
                    AppendLog($"Subtitle translated and saved: {outputFileName}");
                    AddGeneratedSubtitleToApp(resultPath, targetLangIso);

                    // Wait briefly so user sees the completion message, then apply subtitle to player
                    await Task.Delay(2000);
                    await Dispatcher.UIThread.InvokeAsync(() => CloseWindow());
                }
                else
                {
                    AppendLog("ERROR: Translation failed.");
                    UpdateProgress(0, "Translation Failed");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"CRITICAL ERROR: {ex.Message}");
                Log.Error($"GenerateSubtitles error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void UpdateProgress(double value, string status)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var progressBar = this.FindControl<ProgressBar>("GenerationProgressBar");
                var statusText = this.FindControl<TextBlock>("ProgressStatusText");
                if (progressBar != null) progressBar.Value = value;
                if (statusText != null) statusText.Text = status;
            });
        }

        private void AppendLog(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var logOutput = this.FindControl<TextBlock>("LogOutput");
                var scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
                if (logOutput != null)
                {
                    logOutput.Text += message + Environment.NewLine;
                    scrollViewer?.ScrollToEnd();
                }
            });
        }

        private void AddGeneratedSubtitleToApp(string filePath, string language)
        {
            try
            {
                // Check if there's already a subtitle for this movie/episode/language
                var existingSub = PlayerWindow.subtitles.FirstOrDefault(x =>
                    x.MovieId == showId &&
                    x.EpisodeNumber == episodeNumber &&
                    x.SeasonNumber == seasonNumber &&
                    x.Language == language);

                if (existingSub != null)
                {
                    // Update existing subtitle entry
                    existingSub.Name = Path.GetFileName(filePath);
                    existingSub.Fullpath = filePath;
                    existingSub.FileName = Path.GetFileName(filePath);
                    existingSub.Name2 = "AI Generated";
                    existingSub.IsOrg = true;
                }
                else
                {
                    // Add new subtitle entry
                    Subtitle subtitle = new Subtitle
                    {
                        MovieId = showId,
                        SeasonNumber = seasonNumber,
                        EpisodeNumber = episodeNumber,
                        Name = Path.GetFileName(filePath),
                        Fullpath = filePath,
                        HashDownload = false,
                        Language = language,
                        Synchronized = false,
                        SubtitleId = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0),
                        Country = null,
                        FileId = null,
                        FileName = Path.GetFileName(filePath),
                        DownloadCount = "0",
                        Votes = "0",
                        Ratings = "0",
                        Name2 = "AI Generated",
                        PublishDate = DateTime.Now,
                        IsOrg = true,
                        ImdbId = "tt" + imdbId,
                        DownloadUrl = null
                    };
                    PlayerWindow.subtitles.Add(subtitle);
                }

                // Save to encrypted subtitle info file
                File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));

                // Flag that subtitle changed so player reloads with new subtitle
                changedSubtitle = true;

                AppendLog("Subtitle saved and will be applied to player.");
            }
            catch (Exception ex)
            {
                AppendLog("Error saving subtitle info: " + ex.Message);
            }
        }

        private async void OpenSubtitlesComMenu_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                SubtitlesDisplay.ItemsSource = null;
                
                var menuLine = this.FindControl<Rectangle>("MenuLine");
                var makeSubtitlesMenu = this.FindControl<TextBlock>("MakeSubtitlesMenu");
                var openSubtitlesComMenu = this.FindControl<TextBlock>("OpenSubtitlesComMenu");
                var makeSubtitlesView = this.FindControl<Grid>("MakeSubtitlesView");
                var subtitlesDisplayScrollViewer = this.FindControl<ScrollViewer>("SubtitlesDisplayScrollViewer");
                var subtitlesDisplay = this.FindControl<ItemsControl>("SubtitlesDisplay");
                
                Grid.SetColumn(menuLine, 1);
                openSubtitlesComMenu.Foreground = new SolidColorBrush(Colors.White);
                makeSubtitlesMenu.Foreground = new SolidColorBrush(Color.Parse("#414141"));
                
                makeSubtitlesView.IsVisible = false;
                subtitlesDisplayScrollViewer.IsVisible = true;
                subtitlesDisplay.IsVisible = true;

                if (currentSubtitle != null)
                { 
                    SubtitlesDisplay.ItemsSource = await SubtitleHandler.SearchSubtitle(currentSubtitle.MovieId, seasonNumber, episodeNumber, AppSettingsManager.appSettings.IsoSubtitleLanguage, "", movieName, imdbId);
                }
                else
                {
                    SubtitlesDisplay.ItemsSource = await SubtitleHandler.SearchSubtitle(showId, seasonNumber, episodeNumber,
                        language, path, movieName, imdbId);
                }
                
                subtitleType = SubtitleType.Com;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }
        
        private async Task<string> GetLanguage(string name)
        {
            var currentLanguage = Service.Languages.FirstOrDefault(x => x.EnglishName == name);
            if (currentLanguage == null)
            {
                return "";
            }
            return currentLanguage.Iso_639_1;
        }

        public bool disabledSubtitle = false;

        // Popup kapatma event'i için
        public event EventHandler Closed;

        private void CloseWindow()
        {
            try
            {
                playerWindow.shouldListen = true;
                // Event'leri temizle
                var comboBoxSubtitleLanguage = this.FindControl<ComboBox>("ComboBoxSubtitleLanguage");
                if (comboBoxSubtitleLanguage != null)
                {
                    comboBoxSubtitleLanguage.SelectionChanged -= ComboBoxSubtitleLanguageOnSelectionChanged;
                }
                
                // Doğrudan PlayerWindow referansını kullanarak popup'ı kapat
                if (playerWindow != null && playerWindow.AudioSubtitlePopup != null)
                {
                    // İşlem tamamlandıktan sonra PlayerWindow'daki metodları çağır
                    if (disabledSubtitle)
                    {
                        if (playerWindow.Player?.MediaPlayer != null)
                            playerWindow.Player.MediaPlayer.SetSpu(-1);
                    }
                    else if (changedSubtitle)
                    {
                        // Yeni alt yazı ile medyayı yeniden başlat
                        if (playerWindow.mediaPlayer != null)
                            playerWindow.mediaPlayer.Stop();
                        playerWindow.success = false;
                        playerWindow.PlayerOnLoaded(this, new RoutedEventArgs());
                    }
                    
                    // Popup'ı kapat
                    playerWindow.AudioSubtitlePopup.IsOpen = false;

                    // Kontrolleri göster
                    playerWindow.ButtonBack.IsVisible = true;
                    playerWindow.PanelControlVideo.IsVisible = true;
                    playerWindow.PlayButton.Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/pause.png")));
                    // Oynatmayı devam ettir - sadece altyazı değişmediyse
                    // changedSubtitle true ise PlayerOnLoaded zaten yeni medyayı oynatacak
                    if (!changedSubtitle && playerWindow.Player?.MediaPlayer != null && !playerWindow.Player.MediaPlayer.IsPlaying)
                    {
                        playerWindow.Player.MediaPlayer.Play();
                       
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void StackPanelSubtitle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var stackPanel = sender as StackPanel;
                if(stackPanel == null) return;
                var selectedSubtitle = stackPanel.DataContext as Subtitle;

                if (selectedSubtitle != null)
                {
                    if (subtitleType == SubtitleType.Com)
                    {
                        if (currentSubtitle == null || (currentSubtitle != null && currentSubtitle.SubtitleId != selectedSubtitle.SubtitleId))
                        {
                            var subtitlePath = await SubtitleHandler.DownloadSubtitle(selectedSubtitle.FileId.Value, selectedSubtitle.FileName);
                            if (PlayerWindow.subtitles.Any(x => x.MovieId == selectedSubtitle.MovieId
                                                              && x.EpisodeNumber == selectedSubtitle.EpisodeNumber
                                                              && x.SeasonNumber == selectedSubtitle.SeasonNumber
                                                              && x.Language == selectedSubtitle.Language))
                            {
                                var currentSub = PlayerWindow.subtitles.FirstOrDefault(x =>
                                    x.MovieId == selectedSubtitle.MovieId
                                    && x.EpisodeNumber == selectedSubtitle.EpisodeNumber
                                    && x.SeasonNumber == selectedSubtitle.SeasonNumber
                                    && x.Language == selectedSubtitle.Language);

                                currentSub.Name = Path.GetFileName(subtitlePath);
                                currentSub.Fullpath = subtitlePath;
                                currentSub.HashDownload = selectedSubtitle.HashDownload;
                                currentSub.Synchronized = false;
                                currentSub.SubtitleId = selectedSubtitle.SubtitleId;
                                changedSubtitle = true;

                                File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));
                                CloseWindow();
                            }
                            else
                            {
                                Subtitle subtitle = new Subtitle
                                {
                                    MovieId = showId,
                                    SeasonNumber = seasonNumber,
                                    EpisodeNumber = episodeNumber,
                                    Name = Path.GetFileName(subtitlePath),
                                    Fullpath = subtitlePath,
                                    HashDownload = selectedSubtitle.HashDownload,
                                    Language = selectedSubtitle.Language,
                                    Synchronized = false,
                                    SubtitleId = selectedSubtitle.SubtitleId,
                                    Country = null,
                                    FileId = selectedSubtitle.FileId,
                                    FileName = selectedSubtitle.FileName,
                                    DownloadCount = null,
                                    Votes = null,
                                    Ratings = null,
                                    Name2 = null,
                                    PublishDate = default,
                                    IsOrg = false,
                                    ImdbId = "tt" + imdbId,
                                    DownloadUrl = null
                                };
                                changedSubtitle = true;
                                PlayerWindow.subtitles.Add(subtitle);
                                File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));
                                CloseWindow();
                            }
                        }
                        else
                        {
                            CloseWindow();
                        }
                    }
                    else
                    {
                        if (currentSubtitle == null || (currentSubtitle != null && currentSubtitle.SubtitleId != selectedSubtitle.SubtitleId))
                        {
                            var downloadedSub = await SubtitleHandler.DownloadSubtitle(selectedSubtitle);
                            if (downloadedSub != null)
                            {
                                if (PlayerWindow.subtitles.Any(x =>
                                        x.MovieId == showId && x.EpisodeNumber == episodeNumber &&
                                        x.SeasonNumber == seasonNumber
                                        && x.Language == selectedSubtitle.Language))
                                {
                                    var currentSub = PlayerWindow.subtitles.FirstOrDefault(x => x.MovieId == showId &&
                                        x.EpisodeNumber == episodeNumber &&
                                        x.SeasonNumber == seasonNumber
                                        && x.Language == selectedSubtitle.Language);

                                    currentSub.Name = Path.GetFileName(downloadedSub.Fullpath);
                                    currentSub.Fullpath = downloadedSub.Fullpath;
                                    currentSub.HashDownload = downloadedSub.HashDownload;
                                    currentSub.Synchronized = downloadedSub.Synchronized;
                                    currentSub.SubtitleId = downloadedSub.SubtitleId;
                                    changedSubtitle = true;

                                    File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));
                                    CloseWindow();
                                }
                                else
                                {
                                    Subtitle subtitle = new Subtitle
                                    {
                                        MovieId = showId,
                                        SeasonNumber = seasonNumber,
                                        EpisodeNumber = episodeNumber,
                                        Name = Path.GetFileName(downloadedSub.Fullpath),
                                        Fullpath = downloadedSub.Fullpath,
                                        HashDownload = false,
                                        Language = downloadedSub.Language,
                                        Synchronized = false,
                                        SubtitleId = downloadedSub.SubtitleId,
                                        Country = null,
                                        FileId = null,
                                        FileName = null,
                                        DownloadCount = null,
                                        Votes = null,
                                        Ratings = null,
                                        Name2 = null,
                                        PublishDate = default,
                                        IsOrg = true,
                                        ImdbId = "tt" + imdbId,
                                        DownloadUrl = downloadedSub.DownloadUrl
                                    };

                                    changedSubtitle = true;
                                    PlayerWindow.subtitles.Add(subtitle);

                                    File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));
                                    CloseWindow();
                                }
                            }
                        }
                        else
                        {
                            CloseWindow();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            playerWindow.shouldListen = true;
        }
    }

    public enum SubtitleType
    {
        Com,
        Org
    }
} 