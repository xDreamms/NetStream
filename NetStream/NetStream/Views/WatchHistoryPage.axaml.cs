using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetStream.Annotations;
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for WatchHistoryPage.xaml
    /// </summary>
    public partial class WatchHistoryPage : UserControl
    {
        public static WatchHistoryPage GetWatchHistoryPageInstance = null;
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
        
        public WatchHistoryPage()
        {
            InitializeComponent();
            GetWatchHistoryPageInstance = this;
            currentPage = 1;
            OnLoad();
            // Ekran boyutu değiştiğinde düzeni güncelle
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyDirectStylesToWatchHistoryItems(e.width);
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
        


        private async void WatchHistoryDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                // In Avalonia, we need to check if we've scrolled to the bottom
                var scrollViewer = (ScrollViewer)sender;
                if (Math.Abs(scrollViewer.Offset.Y + scrollViewer.Viewport.Height - scrollViewer.Extent.Height) < 1)
                {
                    if (currentPage + 1 <= FirestoreManager.TotalWatchHistoryPages)
                    {
                        currentPage++;
                        await FirestoreManager.GetPaginatedWatchHistory(currentPage);
                    }
                }
                ApplyDirectStylesToWatchHistoryItems(MainView.Instance.Bounds.Width);
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
                    
                    var mainView = this.FindAncestorOfType<MainView>();
                    if (mainView != null)
                    {
                        mainView.SetContent(movieDetailsPage); 
                    }
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
                
                MainView.Instance.SizeChanged += InstanceOnSizeChanged;
                ApplyDirectStylesToWatchHistoryItems(this.Bounds.Width);
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

                Dispatcher.UIThread.Post(() =>
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
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
            // SizeChanged olayını kaldır
        }

        private async void StackPanelMovie_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                {
                    var stackPanel = sender as StackPanel;
                    if (stackPanel == null) return;
                    var selectedShow = stackPanel.DataContext as WatchHistory;
                    if (selectedShow != null)
                    {
                        var currentTorrent =
                            DownloadsPage.torrents.FirstOrDefault(x =>
                                x.Hash != null && x.Hash.ToLower() == selectedShow.TorrentHash.ToLower());
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
                                MainWindow.Instance.SetContent(playerWindow);
                            }
                            else if (mediaFileCount > 1)
                            {
                                if (currentTorrent.ShowType == ShowType.TvShow)
                                {
                                    /*DownloadsFilesPage downloadsFilesPage = new DownloadsFilesPage(currentTorrent.MovieId,
                                        currentTorrent.MovieName, currentTorrent.ShowType, currentTorrent.ImdbId,
                                        currentTorrent);

                                    var mainView = this.FindAncestorOfType<MainView>();
                                    if (mainView != null)
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
                                        mainView.ContentFrame.Content = downloadsFilesPage;
                                    }*/
                                }
                            }
                        }
                    }
                }
                else
                {
                    e.Handled = true;
                    var stackPanel2 = sender as StackPanel;
                    if (stackPanel2 != null)
                    {
                        var selectedShow2 = stackPanel2.DataContext as WatchHistory;
                        if (selectedShow2 == null) return;
                        selectedIndex = FirestoreManager.WatchHistories.IndexOf(selectedShow2);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        // Ekran boyutu değiştiğinde çağrılır
      
        
        // Ekran genişliğine göre ölçeklendirilmiş değer hesaplar
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
        
        // Kart genişliği hesaplar
        private double CalculateCardWidth(double width)
        {
            return CalculateScaledValue(width, 160, 365);
        }
        
        // Kart yüksekliği hesaplar
        private double CalculateCardHeight(double width)
        {
            return CalculateScaledValue(width, 90, 200);
        }
        
        // Yazı boyutu hesaplar
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }
        
        // İlerleme çubuğu genişliği hesaplar
        private double CalculateProgressBarWidth(double width)
        {
            return CalculateScaledValue(width, 280, 704);
        }
        
        // Ekran boyutuna göre izleme geçmişi öğelerini günceller
        private void ApplyDirectStylesToWatchHistoryItems(double width)
        {
            try
            {
                bool isExtraSmall = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                bool isSmall = width <= SMALL_SCREEN_THRESHOLD && !isExtraSmall;
                
                // Ekran genişliğini maksimum 3840px ile sınırla
                double clampedWidth = Math.Min(width, 3840);
                
                // Öğe boyutlarını hesapla
                double cardWidth = CalculateCardWidth(clampedWidth);
                double cardHeight = CalculateCardHeight(clampedWidth);
                double progressBarWidth = CalculateProgressBarWidth(clampedWidth);
                
                // Font boyutlarını hesapla
                double titleFontSize = CalculateTextSize(clampedWidth, 14, 30);
                double detailsFontSize = CalculateTextSize(clampedWidth, 10, 24);
                
                // Sayfa başlığını güncelle
                var headerTextBlock = this.FindControl<Viewbox>("HeaderViewbox");
                if (headerTextBlock != null)
                {
                    headerTextBlock.Width = clampedWidth * 0.5;
                }
                
                // WatchHistoryDisplay ItemsControl için öğeleri güncelle
                var watchHistoryDisplay = this.FindControl<ItemsControl>("WatchHistoryDisplay");
                if (watchHistoryDisplay == null || watchHistoryDisplay.Items == null) return;
                
                foreach (var item in watchHistoryDisplay.GetVisualDescendants())
                {
                    // Ana StackPanel'i güncelle
                    if (item is StackPanel stackPanel && stackPanel.Name == "StackPanelMovie")
                    {
                        // Küçük ekranlarda dikey düzen, büyük ekranlarda yatay düzen
                        stackPanel.Orientation = isSmall || isExtraSmall ? Orientation.Vertical : Orientation.Horizontal;
                        stackPanel.Margin = new Thickness(0, 
                                                         isExtraSmall ? 10 : (isSmall ? 15 : 20), 
                                                         isExtraSmall ? 10 : (isSmall ? 15 : 20),
                                                         0);
                    }
                    // Poster Border'ını güncelle
                    else if (item is Border border && border.Child is AsyncImageControl)
                    {
                        border.Width = cardWidth;
                        border.Height = cardHeight;
                    }
                    // Bilgi StackPanel'ini güncelle
                    else if (item is StackPanel infoPanel && infoPanel.Children.OfType<TextBlock>().Any())
                    {
                        // Küçük ekranlarda kenar boşluğunu ayarla
                        infoPanel.Margin = isSmall || isExtraSmall ? 
                            new Thickness(0, 10, 0, 0) : 
                            new Thickness(20, 5, 0, 0);
                    }
                    // TextBlock'ları güncelle
                    else if (item is TextBlock textBlock)
                    {
                        // İlk TextBlock başlık, diğerleri detay bilgilerdir
                        if (textBlock.Text == null) continue;
                        
                        if (textBlock.Text == ((WatchHistory)textBlock.DataContext)?.Name)
                        {
                            // Başlık
                            textBlock.FontSize = titleFontSize;
                            textBlock.MaxWidth = isSmall || isExtraSmall ? cardWidth : 350;
                        }
                        else
                        {
                            // Detaylar
                            textBlock.FontSize = detailsFontSize;
                        }
                    }
                    // İlerleme çubuğunu güncelle
                    else if (item is ProgressBar progressBar)
                    {
                        progressBar.Width = isSmall || isExtraSmall ? cardWidth : progressBarWidth;
                        progressBar.Height = CalculateProgressBarHeight(width);
                        // progressBar.Margin = new Thickness(0, 
                        //                                   isExtraSmall ? 10 : (isSmall ? 15 : 20), 
                        //                                   0, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ApplyDirectStylesToWatchHistoryItems hatası: {ex.Message}, {ex.StackTrace}");
            }
        }
        
        private double CalculateProgressBarHeight(double width)
        {
            return CalculateScaledValue(width, 2, 9);
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
                return ResourceProvider.GetString("LastViewedDateString")+ " " + FirestoreManager.GetRelativeDate(LastWatchDate,WatchHistoryPage.currentTime);
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
            get { return ResourceProvider.GetString("ViewingRateString") + " %" + (progress * 100).ToString("0.00"); }
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