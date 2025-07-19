using CountryFlags;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NETCore.Encrypt;
using Newtonsoft.Json;
using Serilog;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Controls.Button;
using Path = System.IO.Path;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for SubtitleManagerWindow.xaml
    /// </summary>
    public partial class SubtitleManagerWindow : HandyControl.Controls.Window
    {
      
        public Subtitle currentSubtitle;
        private string path;
        private string movieName;
        public bool changedSubtitle = false;
        private int imdbId;
        public SubtitleManagerWindow(Subtitle currentSubitle,string path,string movieName,int imdbId,int showId, ShowType showType, int year, int seasonNumber, int episodeNumber)
        {
            InitializeComponent();
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

        public SubtitleManagerWindow(int movieId,string path, string movieName, int imdbId, ShowType showType, int year, int seasonNumber, int episodeNumber)
        {
            InitializeComponent();
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
                ComboBoxSubtitleLanguage.ItemsSource = subLanguages;
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


                ComboBoxSubtitleLanguage.SelectionChanged += ComboBoxSubtitleLanguageOnSelectionChanged;
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
                if (ComboBoxSubtitleLanguage.SelectedItem == null) return;
                var selectedItem = ComboBoxSubtitleLanguage.SelectedItem.ToString();
                if (selectedItem == "Disabled")
                {
                    MenuGrid.Visibility = Visibility.Collapsed;
                    SubtitlesDisplay.Visibility = Visibility.Collapsed;
                    disabledSubtitle = true;
                    AppSettingsManager.appSettings.SubtitleLanguage = "Disabled";
                    AppSettingsManager.appSettings.IsoSubtitleLanguage = "Disabled";
                    AppSettingsManager.SaveAppSettings();;
                }
                else
                {

                    MenuGrid.Visibility = Visibility.Visible;
                    SubtitlesDisplay.Visibility = Visibility.Visible;
                    AppSettingsManager.appSettings.SubtitleLanguage = ComboBoxSubtitleLanguage.SelectedItem.ToString();
                    AppSettingsManager.appSettings.IsoSubtitleLanguage =
                        await GetLanguage(ComboBoxSubtitleLanguage.SelectedItem.ToString());
                    AppSettingsManager.SaveAppSettings();;
                    FastObservableCollection<Subtitle> subtitles;
                    if (subtitleType == SubtitleType.Com)
                    {
                        subtitles = await SubtitleHandler.SearchSubtitle(showId, seasonNumber, episodeNumber,
                            AppSettingsManager.appSettings.IsoSubtitleLanguage
                            , path, movieName, imdbId);
                    }
                    else
                    {
                        subtitles = await SubtitleHandler.GetSubtitles(showType, movieName, showId, year,
                            seasonNumber, episodeNumber, AppSettingsManager.appSettings.IsoSubtitleLanguage, "tt" + imdbId,false);
                    }
                    SubtitlesDisplay.Dispatcher.Invoke(() =>
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
                    SubtitlesDisplay.Visibility = Visibility.Collapsed;
                    MenuGrid.Visibility = Visibility.Collapsed;
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
                Log.Error(errorMessage);
            }
        }


        private void SubtitleManagerWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
           
        }

        private void SubtitleManagerWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (e.ChangedButton == MouseButton.Left)
            //    this.DragMove();
        }

        private async void SubtitlesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedSubtitle = SubtitlesDisplay.SelectedItem as Subtitle;

                if (selectedSubtitle != null)
                {
                    if (subtitleType == SubtitleType.Com)
                    {
                        if (currentSubtitle == null || (currentSubtitle != null  && currentSubtitle.SubtitleId != selectedSubtitle.SubtitleId))
                        {
                            var subtitlePath = await SubtitleHandler.DownloadSubtitle(selectedSubtitle.FileId.Value, selectedSubtitle.FileName);
                            if(PlayerWindow.subtitles.Any(x=> x.MovieId == selectedSubtitle.MovieId
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

                                File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles),Encryptor.Key,Encryptor.IV));
                                Close();
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
                                    ImdbId = "tt"+imdbId,
                                    DownloadUrl = null
                                };
                                changedSubtitle = true;
                                PlayerWindow.subtitles.Add(subtitle);
                                File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));
                                Close();
                            }
                        }
                        else
                        {
                            Close();
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
                                    Close();
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
                                    Close();
                                }
                            }
                        }
                        else
                        {
                            Close();
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

        private void SubtitlesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }


        private void StackPanelSubtitle_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if(currentSubtitle == null) return;
                var stackpanel = sender as StackPanel;
                if (stackpanel.DataContext is Subtitle)
                {
                    var subtitle = stackpanel.DataContext as Subtitle;
                    if (subtitle.SubtitleId == currentSubtitle.SubtitleId)
                    {
                        stackpanel.Background = new SolidColorBrush(Color.FromArgb(255, 30, 219, 98));
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
                if(currentSubtitle == null) return;
                var button = sender as Button;
                if (button.DataContext is Subtitle)
                {
                    var subtitle = button.DataContext as Subtitle;
                    if (subtitle.SubtitleId == currentSubtitle.SubtitleId)
                    {
                        button.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void UIElement_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

                                Close();
                            }
                            else
                            {
                                Close();
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

                                    Close();
                                }
                            }
                            else
                            {
                                Close();
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

        private async void OpenSubtitlesOrgMenu_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MenuLine.SetValue(Grid.ColumnProperty,3);
                OpenSubtitlesOrgMenu.Foreground = new SolidColorBrush(Colors.White);
                OpenSubtitlesComMenu.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));

                var subtitles = await SubtitleHandler.GetSubtitles(showType, movieName, showId, year,
                    seasonNumber, episodeNumber, AppSettingsManager.appSettings.IsoSubtitleLanguage, "tt" + imdbId,false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SubtitlesDisplay.ItemsSource = subtitles;
                });
                subtitleType = SubtitleType.Org;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void OpenSubtitlesComMenu_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MenuLine.SetValue(Grid.ColumnProperty, 1);
                OpenSubtitlesComMenu.Foreground = new SolidColorBrush(Colors.White);
                OpenSubtitlesOrgMenu.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                if (currentSubtitle != null)
                {
                    SubtitlesDisplay.ItemsSource = await SubtitleHandler.SearchSubtitle(currentSubtitle.MovieId, seasonNumber, episodeNumber, AppSettingsManager.appSettings.IsoSubtitleLanguage, path, movieName, imdbId);
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
                Log.Error(errorMessage);
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

        private void SubtitleManagerWindow_OnClosed(object? sender, EventArgs e)
        {
            ComboBoxSubtitleLanguage.SelectionChanged -= ComboBoxSubtitleLanguageOnSelectionChanged;
        }
    }

    public enum SubtitleType
    {
        Com,
        Org
    }
}
