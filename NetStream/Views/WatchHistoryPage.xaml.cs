using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Windows.Threading;
using NetStream.Annotations;
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for WatchHistoryPage.xaml
    /// </summary>
    public partial class WatchHistoryPage : Page
    {
        public static WatchHistoryPage GetWatchHistoryPageInstance = null;
        public WatchHistoryPage()
        {
            InitializeComponent();
            
            currentPage = 1;
            OnLoad();
        }

        private int currentPage;
        private async void OnLoad()
        {
            var time = await FirestoreManager.GetNistTime();
            currentTime = time.ToUniversalTime();
            WatchHistoryDisplay.ItemsSource = FirestoreManager.WatchHistories;
            await FirestoreManager.GetPaginatedWatchHistory(currentPage);
        }
        private int selectedIndex = -1;
        private void WatchHistoryDisplay_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;

                for (int i = 0; i < FirestoreManager.WatchHistories.Count; i++)
                {
                    var lbi = WatchHistoryDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private static bool IsMouseOverTarget(Visual target, Point point)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(target);
            return bounds.Contains(point);
        }

        private async void WatchHistoryDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedShow = WatchHistoryDisplay.SelectedItem as WatchHistory;
                if (selectedShow != null)
                {
                    var currentTorrent =
                        DownloadsPageQ.torrents.FirstOrDefault(x => x.Hash != null && x.Hash.ToLower() == selectedShow.TorrentHash.ToLower());
                    if (currentTorrent != null)
                    {
                        var files = await Libtorrent.GetFiles(currentTorrent.Hash);
                        var mediaFileCount = files.Count(x => x.IsMediaFile);
                        if (mediaFileCount == 1)
                        {
                            var mediaFile = files.FirstOrDefault(x => x.IsMediaFile);
                            var playerWindow = new PlayerWindow(currentTorrent.MovieId, currentTorrent.MovieName,
                                currentTorrent.ShowType,
                                currentTorrent.SeasonNumber, currentTorrent.EpisodeNumber,
                                new FileInfo(mediaFile.FullPath), currentTorrent.IsCompleted,
                                currentTorrent.ImdbId, currentTorrent, mediaFile.Index, currentTorrent.Poster);
                            playerWindow.Show();
                            playerWindow.Unloaded+= async delegate(object o, RoutedEventArgs args)
                            {
                                var time = await FirestoreManager.GetNistTime();
                                currentTime = time.ToUniversalTime();
                            };
                            DownloadsPageQ.GetDownloadsPageInstance.openedTorrents.Add(currentTorrent.Link);
                        }
                        else if (mediaFileCount > 1)
                        {
                            if (currentTorrent.ShowType == ShowType.TvShow)
                            {
                                DownloadsFilesPage downloadsFilesPage = new DownloadsFilesPage(currentTorrent.MovieId,
                                    currentTorrent.MovieName, currentTorrent.ShowType, currentTorrent.ImdbId,
                                    currentTorrent);

                                if (this.NavigationService != null)
                                {
                                    downloadsFilesPage.OnLoadedCompleted += delegate
                                    {
                                        var selectedEpisode = downloadsFilesPage.episodeFiles.FirstOrDefault(x =>
                                            x.SeasonNumber == selectedShow.SeasonNumber &&
                                            x.EpisodeNumber == selectedShow.EpisodeNumber);
                                        if (selectedEpisode != null)
                                        {
                                            downloadsFilesPage.FilesDisplay.SelectedItem = selectedEpisode;
                                        }
                                    };
                                    this.NavigationService.Navigate(downloadsFilesPage);
                                }
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


        private async void WatchHistoryDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (e.VerticalOffset == e.ExtentHeight - e.ViewportHeight)
                {
                    if (currentPage + 1 <= FirestoreManager.TotalWatchHistoryPages)
                    {
                        currentPage++;
                        await FirestoreManager.GetPaginatedWatchHistory(currentPage);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void GotoDetailsMenuItemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedShow = WatchHistoryDisplay.Items[selectedIndex] as WatchHistory;

                if (selectedShow != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedShow.Id,
                        selectedShow.ShowType);
                    HomePage.GetHomePageInstance.HomePageNavigation.Navigate(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }

        }
        DispatcherTimer timer;
        public static DateTime currentTime;
        private async void WatchHistoryPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer.Tick -= TimerOnTick;
                }

                var time = await FirestoreManager.GetNistTime();
                currentTime = await Task.Run(() => time.ToUniversalTime());
                await Task.Run(() => UpdateUI());
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += TimerOnTick;
                timer.Start();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void UpdateUI()
        {
            try
            {
                var time = await FirestoreManager.GetNistTime();
                currentTime = await Task.Run(() => time.ToUniversalTime());
                foreach (var wh in FirestoreManager.WatchHistories)
                {
                    wh.OnPropertyChanged("RelativeDate");
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TimerOnTick(object? sender, EventArgs e)
        {
            try
            {
                var x = await FirestoreManager.GetNistTime();
                var time = await Task.Run(() => x.ToUniversalTime());

                Dispatcher.Invoke(() =>
                {
                    currentTime = time;
                    foreach (var wh in FirestoreManager.WatchHistories)
                    {
                        wh.OnPropertyChanged("RelativeDate");
                    }
                });
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void WatchHistoryPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            timer.Tick -= TimerOnTick;
        }
    }

    public class WatchHistory : INotifyPropertyChanged
    {
        private int id;

        public int Id
        {
            get { return id; }
            set
            {
                id = value;
                OnPropertyChanged("Id");
            }
        }

        private ShowType showType;

        public ShowType ShowType
        {
            get { return showType; }
            set
            {
                showType = value;
                OnPropertyChanged("ShowType");
            }
        }

        private string name;

        public string Name
        {
            get { return name ; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        private int seasonNumber;

        public int SeasonNumber
        {
            get { return seasonNumber; }
            set
            {
                seasonNumber = value;
                OnPropertyChanged("SeasonNumber");
            }
        }


        private int episodeNumber;

        public int EpisodeNumber
        {
            get { return episodeNumber; }
            set
            {
                episodeNumber = value;
                OnPropertyChanged("EpisodeNumber");
            }
        }

        private string poster;

        public string Poster
        {
            get { return poster; }
            set
            {
                poster = value;
                OnPropertyChanged("Poster");
            }
        }

        private DateTime lastWatchDate;

        public DateTime LastWatchDate
        {
            get { return lastWatchDate; }
            set
            {
                lastWatchDate = value;
                OnPropertyChanged("LastWatchDate");
            }
        }

        private string relativeDate;
        public string RelativeDate
        {
            get
            {
                return App.Current.Resources["LastViewedDateString"]+ " " + FirestoreManager.GetRelativeDate(LastWatchDate,WatchHistoryPage.currentTime);
            }
            set
            {
                relativeDate = value;
                OnPropertyChanged("RelativeDate");
            }
        }

        private double progress;

        public double Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged("Progress");
            }
        }

        private string progressText;
        public string ProgressText
        {
            get { return App.Current.Resources["ViewingRateString"] + " %" + (progress * 100).ToString("0.00"); }
            set
            {
                progressText = value;
                OnPropertyChanged("ProgressText");
            }
        }

        public bool DeletedTorrent { get; set; }
        public string TorrentHash { get; set; }


        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
