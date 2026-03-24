using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using LibVLCSharp.Shared;
using Serilog;
using Material.Icons.Avalonia;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using AvaloniaWebView;
using LibVLCSharp.Avalonia;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using Avalonia.Platform;
using System.Web;
using Avalonia.Controls.Shapes;
using Avalonia.Threading;
using ExCSS;
using NetStream.Controls;
using NetStream.Services;
using TMDbLib.Objects.Movies;
#if BROWSER
// WebView import for WASM
using Avalonia.Browser;
#endif
using NetStream.Extensions;
using Projektanker.Icons.Avalonia;
using Color = Avalonia.Media.Color;
using Path = System.IO.Path;
using Point = Avalonia.Point;

namespace NetStream.Views
{
    public partial class MovieDetailsPage : UserControl, IDisposable
    {
        public static MovieDetailsPage Instance;
        public static Movie currentMedia =null;
        private Movie selectedMovie;
        private string trailerLink = "";
        public LibVLC libVlc;
        private LibVLCSharp.Shared.MediaPlayer mediaPlayer;
        private VideoView videoView;
        private bool isTrailerPlaying = false;
        private TorrentsPage _cachedTorrentsPage;
        
        private List<ListBoxItem> SimilarMoviesVisibleItems = new List<ListBoxItem>();
        private List<ListBoxItem> CastVisibleItems = new List<ListBoxItem>();
        
        public MovieDetailsPage()
        {
            InitializeComponent();
        }

        private SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        private async Task InitializeVideoPlayer()
        {
            try
            {
                await _initLock.WaitAsync();
                try
                {
                    Log.Information("InitializeVideoPlayer started in MovieDetailsPage");
                    if (PlatformDetector.GetPlatform() == Platform.Windows || PlatformDetector.GetPlatform() == Platform.Linux
                        || PlatformDetector.GetPlatform() == Platform.Mac)
                    {
                        // LibVLCSharp için gerekli başlatma
                        if (!unloaded)
                        {
                            // Önceki mediaPlayer ve libVlc varsa temizle
                            await CleanupVideoPlayer();
                            
                            Log.Information("Creating new LibVLC and MediaPlayer instances");
                            
                            libVlc = new LibVLC();
                            mediaPlayer = new MediaPlayer(libVlc);
                            
                            if (TrailerPlayer != null)
                            {
                                TrailerPlayer.MediaPlayer = mediaPlayer;
                                mediaPlayer.EnableKeyInput = false;
                                mediaPlayer.EnableMouseInput = false;
                                
                                // Varsayılan görünürlük ayarları
                                TrailerPlayer.IsVisible = false;
                                if (MainMovieTvShow != null)
                                {
                                    MainMovieTvShow.IsVisible = true;
                                }
                            }
                            Log.Information("Video Player initialized successfully");
                        }
                        else
                        {
                            Log.Information("InitializeVideoPlayer skipped because page is unloaded");
                        }
                    }
                }
                finally
                {
                    _initLock.Release();
                }
            }
            catch (Exception ex)
            {
               Log.Error(ex, "Video player initialization error");
            }
        }
        
        public async Task CleanupVideoPlayer()
        {
            try
            {
                Log.Information("CleanupVideoPlayer started in MovieDetailsPage");
                isTrailerPlaying = false;
                
                MediaPlayer tempMediaPlayer = mediaPlayer;
                LibVLC tempLibVlc = libVlc;
                
                // Prevent concurrent cleanups or usage by nulling the fields immediately
                mediaPlayer = null;
                libVlc = null;

                if (tempMediaPlayer != null)
                {
                    // Stop ve event removal UI thread'de
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            if (TrailerPlayer?.MediaPlayer != null)
                            {
                                TrailerPlayer.MediaPlayer = null;
                            }
                            
                            if (tempMediaPlayer.IsPlaying)
                            {
                                tempMediaPlayer.Stop();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error stopping player");
                        }
                    });

                    // Dispose background thread'de
                    await Task.Run(() =>
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(50);
                            tempMediaPlayer.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error disposing media player");
                        }
                    });
                }
                
                if (tempLibVlc != null)
                {
                     await Task.Run(() =>
                    {
                        try
                        {
                            tempLibVlc.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error disposing libvlc");
                        }
                    });
                }
                
                // Görünürlük ayarlarını sıfırla
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (TrailerPlayer != null)
                    {
                        TrailerPlayer.IsVisible = false;
                    }
                    
                    if (MainMovieTvShow != null)
                    {
                        MainMovieTvShow.IsVisible = true;
                    }
                });
                
                // Bellek temizliği
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Log.Information("CleanupVideoPlayer completed");
            }
            catch (Exception ex)
            {
               Log.Error(ex, "Error cleaning up video player");
            }
        }

        public MovieDetailsPage(Movie selectedMovie)
        {
            InitializeComponent();
            Instance = this;
            this.selectedMovie = selectedMovie;
            this.DataContext = this;
            //Service.Similars.Clear();
            //SetVisibilityWatchButton(selectedMovie.Id);
            
            if (selectedMovie.ShowType == ShowType.Movie)
            {
                MovieDetailsNavigation.Content = new MovieDetailsOverViewPage(selectedMovie);
            }
            else if (selectedMovie.ShowType == ShowType.TvShow)
            {
                MovieDetailsNavigation.Content =(new TvShowDetailsOverViewPage(selectedMovie));
            }
            
            if (selectedMovie.ShowType == ShowType.TvShow)
            {
                // MainMovieTvShow kontrolüne ulaşıp Duration elemanlarını gizle
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow != null)
                {
                    var durationElipse = mainMovieTvShow.FindControl<Ellipse>("DurationElipse");
                    var durationViewBox = mainMovieTvShow.FindControl<Viewbox>("DurationViewBox");
                    
                    if (durationElipse != null) durationElipse.IsVisible = false;
                    if (durationViewBox != null) durationViewBox.IsVisible = false;
                }
            }
            currentMedia = selectedMovie;
            LoadThis();

        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            TrailerPlayer.Height = (int)e.height * 0.944;
            
           
            
            try
            {
                // Get current width
                double width = e.width;
                
                if (width != previousWidth)
                {
                    previousWidth = width;
                    
                    // Determine screen size category for responsive design
                    isSmallScreen = width <= SMALL_SCREEN_THRESHOLD;
                    isExtraSmallScreen = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                    
                    // Apply responsive layout based on screen size
                    ApplyResponsiveLayout(width);
                    
                    // Ana poster görüntüsünün boyutunu ayarla
                    AdjustMainMoviePosterSize(width);
                    
                    // WrapPanel elemanlarının boyutunu ayarla
                    AdjustWrapPanelMovieDetailsSize(width);
                    
                    // Etkileşimli öğelerin boyutlarını ayarla
                    AdjustInteractiveElements(width);
                    
                    // Scroll butonlarının konumunu ayarla
                    AdjustScrollButtonsPosition(width);
                    
                    // Dinamik film öğelerinin stillerini ayarla
                    UpdateLayoutBasedOnSize(width);
                    
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error in Home_OnSizeChanged: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
               Console.WriteLine(errorMessage);
            }
        }

        private async Task GetMainMovieDetail()
        {
            MainMovieTvShow.DataContext = await Service.GetMainMovieDetail( selectedMovie);
        }

        private async Task GetTrailerLink()
        {
            trailerLink = await Service.GetTrailerLink(selectedMovie.ShowType,selectedMovie.Id);
        }

        // Film ID'si ile başlatma
        public MovieDetailsPage(int movieId, ShowType showType)
        {
            InitializeComponent();
            Instance = this;
            Movie mo = new Movie()
            {
                Id = movieId,
                ShowType = showType
            };
            this.selectedMovie = mo;
            this.DataContext = this;
           // Service.Similars.Clear();
           // SetVisibilityWatchButton(movie_id);
           if (selectedMovie.ShowType == ShowType.Movie)
           {
               MovieDetailsNavigation.Content =(new MovieDetailsOverViewPage(selectedMovie));
           }
           else if (selectedMovie.ShowType == ShowType.TvShow)
           {
               MovieDetailsNavigation.Content =(new TvShowDetailsOverViewPage(selectedMovie));
           }

           if (selectedMovie.ShowType == ShowType.TvShow)
           {
               // MainMovieTvShow kontrolüne ulaşıp Duration elemanlarını gizle
               var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
               if (mainMovieTvShow != null)
               {
                   var durationElipse = mainMovieTvShow.FindControl<Ellipse>("DurationElipse");
                   var durationViewBox = mainMovieTvShow.FindControl<Viewbox>("DurationViewBox");
                   
                   if (durationElipse != null) durationElipse.IsVisible = false;
                   if (durationViewBox != null) durationViewBox.IsVisible = false;
               }
           }
           currentMedia = selectedMovie;
           LoadThis();
        }
        
        private void Home_OnLoaded(object sender, RoutedEventArgs e)
        {
            unloaded = false;
            
            // Sayfaya geri dönüldüğünde video oynatıcısını yeniden başlat
            if (TrailerPlayer != null && !string.IsNullOrWhiteSpace(videoPath) 
                && File.Exists(videoPath) && new FileInfo(videoPath).Length > 0)
            {
                // Önce görünürlük ayarlarını sıfırla
                TrailerPlayer.IsVisible = false;
                MainMovieTvShow.IsVisible = true;
                
                // Video oynatıcıyı yeniden başlat
                // We don't await here because this is an event handler, but we use Dispatcher.Post to ensure order
                Dispatcher.UIThread.Post(async () => 
                {
                    await InitializeVideoPlayer();
                    
                    await Task.Delay(500); // Kısa bir gecikme ekleyerek UI'ın güncellemesini bekle
                    if (!unloaded)
                    {
                        await PlayTrailer();
                    }
                });
            }
        }

        private void LoadThis()
        {
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
            Load();
            SetVisibility();
            
            TrailerPlayer.Height = (int)MainView.Instance.Bounds.Height * 0.944;
            // Update UI for different sizes
            
            try
            {
                // Get current width
                double width = MainView.Instance.Bounds.Width;
                
                if (width != previousWidth)
                {
                    previousWidth = width;
                    
                    // Determine screen size category for responsive design
                    isSmallScreen = width <= SMALL_SCREEN_THRESHOLD;
                    isExtraSmallScreen = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                    
                    // Apply responsive layout based on screen size
                    ApplyResponsiveLayout(width);
                    
                    // Ana poster görüntüsünün boyutunu ayarla
                    AdjustMainMoviePosterSize(width);
                    
                    // WrapPanel elemanlarının boyutunu ayarla
                    AdjustWrapPanelMovieDetailsSize(width);
                    
                    // Etkileşimli öğelerin boyutlarını ayarla
                    AdjustInteractiveElements(width);
                    
                    // Scroll butonlarının konumunu ayarla
                    AdjustScrollButtonsPosition(width);
                    
                    // Dinamik film öğelerinin stillerini ayarla
                    UpdateLayoutBasedOnSize(width);
                    
                    // Process items for responsive display
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error in Home_OnSizeChanged: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }
        
        private void SetVisibility()
        {
            // MainMovieTvShow kontrolüne eriş
            var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
            if (mainMovieTvShow == null) return;
            
            // OverviewTextBlock kontrolü artık MainMovieTvShow içinde
            var overviewTextBlock = mainMovieTvShow.FindControl<TextBlock>("OverviewTextBlock");
            if (overviewTextBlock == null) return;
            
            if (this.DataContext is MainMovie mainMovie)
            {
                if (string.IsNullOrWhiteSpace(mainMovie.Overview))
                {
                    overviewTextBlock.IsVisible = false;
                }
            }
            
            // ButtonWatch ve ButtonWatchNow kontrolleri
            var buttonWatch = mainMovieTvShow.FindControl<Button>("ButtonWatch");
            var buttonWatchNow = mainMovieTvShow.FindControl<Button>("ButtonWatchNow");
            
            // Eğer film için fragmentler varsa, Watch ve Watch Now butonlarını göster
            if (selectedMovie != null /*&& selectedMovie.Fragments != null && selectedMovie.Fragments.Count > 0*/)
            {
                if (buttonWatch != null)
                    buttonWatch.IsVisible = true;
                    
                if (buttonWatchNow != null)
                    buttonWatchNow.IsVisible = true;
            }
            else
            {
                if (buttonWatch != null)
                    buttonWatch.IsVisible = false;
                    
                if (buttonWatchNow != null)
                    buttonWatchNow.IsVisible = false;
            }
        }
        public ObservableCollection<Cast> Casts
        {
            get
            {
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    return Service.MovieCasts;
                }
                else
                {
                    return Service.TvShowCasts;
                }
            }
        }
        
        public ObservableCollection<Movie> Similars
        {
            get
            {
                return Service.Similars;
            }
        }
        
     
         private async void Load()
 {
    
    

     var tasks = new List<Task>()
     {
         GetMainMovieDetail(),
         GetTrailerLink(),
         Service.GetCredits(selectedMovie),
         Service.GetSimilars( selectedMovie)
     };

     await Task.WhenAll(tasks);
     if (unloaded) return; // Sayfa unload edildiyse geri dön
     
     SetVisibility();
     SetStateAccountIcons();

     if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
         RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
         RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
     {
         videoPath = System.IO.Path.Combine(AppSettingsManager.appSettings.YoutubeVideoPath, selectedMovie.Id + ".mp4");
         Console.WriteLine("Video path: " +videoPath);
         Console.WriteLine("trailer link: " + trailerLink);
         if (!String.IsNullOrWhiteSpace(trailerLink))
         {
             try
             {
                 var videoDownloaded = await PrepareVideoAsync();
                 
                 if (videoDownloaded && !unloaded)
                 {
                     await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                     {
                         if (!unloaded)
                         {
                             MainMovieTvShow.IsVisible = false;
                             TrailerPlayer.IsVisible = true;
                             await InitializeVideoPlayer();
                         }
                     });
                     
                     if (!unloaded)
                     {
                         await PlayTrailer();
                     }
                 }
             }
             catch (Exception exception)
             {
                 var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Console.WriteLine(errorMessage);
             }
         }
     }
 }

 private async Task<bool> PrepareVideoAsync()
 {
     if (string.IsNullOrWhiteSpace(videoPath) || string.IsNullOrWhiteSpace(trailerLink) || unloaded)
         return false;
     
     if (!File.Exists(videoPath) || new FileInfo(videoPath).Length == 0)
     {
         if (!unloaded)
         {
             await RunFFMpegAsync();
         }
     }
     
     return File.Exists(videoPath) && new FileInfo(videoPath).Length > 0 && !unloaded;
 }

 public string videoPath;
 async Task RunFFMpegAsync()
 {
     try
     {
         var ytdl = new YoutubeDL();

         ytdl.YoutubeDLPath = Path.Combine(Environment.CurrentDirectory, "yt-dlp.exe");
         ytdl.FFmpegPath = Environment.CurrentDirectory + "\\ffmpeg\\bin\\ffmpeg.exe";
         var options = new OptionSet
         {
             Format = "bestvideo+bestaudio/best",
             MergeOutputFormat = DownloadMergeFormat.Mp4,
             Output = videoPath
         };

         var res = await ytdl.RunVideoDownload(
             trailerLink,
             overrideOptions: options
         );

     }
     catch (Exception e)
     {
        Console.WriteLine(e.Message);
     }
 }

         private async Task PlayTrailer()
        {
            try
            {
                if (unloaded) return; // Eğer sayfa unload edilmişse video oynatmayı deneme
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || 
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || 
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Önceki oynatma durumunu sıfırla
                    isTrailerPlaying = false;
                    
                    if (!isTrailerPlaying && !unloaded)
                    {
                        if (File.Exists(videoPath))
                        {
                            // VideoView hazır değilse video oynatmayı deneme
                            if (TrailerPlayer == null || TrailerPlayer.MediaPlayer == null)
                            {
                                // Video oynatıcı hazır değilse yeniden başlatmayı dene
                                await InitializeVideoPlayer();
                                
                                if (TrailerPlayer == null || TrailerPlayer.MediaPlayer == null)
                                {
                                    Log.Warning("TrailerPlayer is not ready");
                                    return;
                                }
                            }
                            
                            // Önce oynatıcıyı durdur (eğer hala çalışıyorsa)
                            if (mediaPlayer != null && mediaPlayer.IsPlaying)
                            {
                                mediaPlayer.Stop();
                            }
                            
                            // TrailerPlayer'ı UI üzerinde hazırla
                            await Dispatcher.UIThread.InvokeAsync(() => 
                            {
                                TrailerPlayer.IsVisible = true;
                                
                                // MainMovieTvShow kontrolünü gizle
                                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                                if (mainMovieTvShow != null)
                                {
                                    mainMovieTvShow.IsVisible = false;
                                }
                            });
                            
                            // Videoyu oynat
                            await Task.Delay(100); // UI'nin güncellenmesi için küçük bir bekleme süresi
                            
                            using (var media = new Media(libVlc, videoPath, FromType.FromPath))
                            {
                                // Video özelliklerini ayarla
                                media.AddOption(":video-filter=adjust");
                                media.AddOption(":contrast=1.1");
                                
                                // Medya dosyasını oynat
                                mediaPlayer.Play(media);
                                isTrailerPlaying = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               Console.WriteLine($"Error playing trailer: {ex.Message}");
            }
        }

       
        
        private string ExtractYouTubeVideoId(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return string.Empty;
                    
                // YouTube URL'den video ID'sini çıkar
                if (url.Contains("youtu.be/"))
                {
                    // https://youtu.be/VIDEO_ID formatı
                    return url.Split(new[] { "youtu.be/" }, StringSplitOptions.None)[1].Split('?')[0];
                }
                else if (url.Contains("youtube.com/watch"))
                {
                    // https://www.youtube.com/watch?v=VIDEO_ID formatı
                    Uri uri = new Uri(url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    return query["v"];
                }
                else if (url.Contains("youtube.com/embed/"))
                {
                    // https://www.youtube.com/embed/VIDEO_ID formatı
                    return url.Split(new[] { "youtube.com/embed/" }, StringSplitOptions.None)[1].Split('?')[0];
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async void WatchTrailerButton_OnClick(object? sender, RoutedEventArgs e)
        {
            await PlayTrailer();
        }
        
        private double previousWidth = 0;
        private bool isSmallScreen = false;
        private bool isExtraSmallScreen = false;

        // Threshold for small screen detection
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
       
        
         
        
           private void AdjustScrollButtonsPosition(double width)
        {
            try
            {
                // Ekran genişliğini maksimum 3840px ile sınırla
                double clampedWidth = Math.Min(width, 3840);
                
                // Ekran boyutuna göre buton boyutlarını ölçekle
                double buttonSize = CalculateScaledValue(clampedWidth, 40, 60);
                double iconSize = CalculateScaledValue(clampedWidth, 40, 60);
                double buttonHeight = CalculateCardHeight(clampedWidth); // Kart yüksekliğiyle aynı olsun
                
               
                
                // Sağ butonları ayarla
              
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AdjustScrollButtonsPosition Exception: {ex.Message}");
            }
        }
        
        private void AdjustWrapPanelMovieDetailsSize(double width)
        {
            try
            {
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow == null) return;
                
                var wrapPanelMovieDetails = mainMovieTvShow.FindControl<WrapPanel>("WrapPanelMovieDetails");
                if (wrapPanelMovieDetails == null) return;
                
                // Ekran genişliği sınırları
                const double minWidth = 320;   // En küçük ekran genişliği (piksel)
                const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
                const double minHeightValue = 16; // En küçük yükseklik değeri
                const double maxHeightValue = 36; // En büyük yükseklik değeri
                
                // Ekran genişliğini sınırla
                double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
                
                // Doğrusal ölçeklendirme ile maxHeight değerini hesapla
                double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
                double dynamicHeightValue = minHeightValue + scale * (maxHeightValue - minHeightValue);
                
                // Değeri yuvarla
                dynamicHeightValue = Math.Round(dynamicHeightValue);
                
                // Döngüsel yeniden boyutlandırmaları önlemek için değişikliği takip et
                bool madeChanges = false;
                
                // WrapPanel içindeki tüm Viewbox'ları bul ve boyutlarını ayarla
                foreach (var child in wrapPanelMovieDetails.Children)
                {
                    if (child is Viewbox viewbox)
                    {
                        // Sadece değer değiştiyse güncelle
                        if (Math.Abs(viewbox.MaxHeight - dynamicHeightValue) > 0.1)
                        {
                            viewbox.MaxHeight = dynamicHeightValue;
                            madeChanges = true;
                        }
                    }
                    else if (child is Ellipse ellipse)
                    {
                        // Ellipse'lerin boyutunu oran olarak hesapla (yüksekliğin %20'si)
                        double ellipseSize = dynamicHeightValue * 0.2;
                        
                        // Sadece değer değiştiyse güncelle
                        if (Math.Abs(ellipse.Width - ellipseSize) > 0.1 || 
                           Math.Abs(ellipse.Height - ellipseSize) > 0.1)
                        {
                            ellipse.Width = ellipseSize;
                            ellipse.Height = ellipseSize;
                            madeChanges = true;
                        }
                    }
                }
                
                // Özel elementlerin boyutlarını ve kenar boşluklarını ayarla
                AdjustInteractiveElements(clampedWidth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AdjustWrapPanelMovieDetailsSize Exception: {ex.Message}");
            }
        }
        
        private void AdjustInteractiveElements(double width)
        {
            try
            {
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow == null) return;
                
                var watchTrailerButton = mainMovieTvShow.FindControl<Button>("WatchTrailerButton");
                
                // İkon boyutlarını hesapla
                double iconSize = CalculateScaledValue(width, 16, 30);
                
                // Buton yazı tipi boyutunu hesapla
                double buttonFontSize = CalculateScaledValue(width, 12, 20);
                
                // Margin değerlerini hesapla
                double marginSize = CalculateScaledValue(width, 5, 10);
                
                // Butonları güncelle
                if (watchTrailerButton != null)
                {
                    watchTrailerButton.FontSize = buttonFontSize;
                    watchTrailerButton.Margin = new Thickness(0);
                }
                
                var ButtonWatch = mainMovieTvShow.FindControl<Button>("ButtonWatch");
                if (ButtonWatch != null)
                {
                    ButtonWatch.FontSize = buttonFontSize;
                    // Butonlar arası mesafe ekle
                    ButtonWatch.Margin = new Thickness(marginSize, 0, 0, 0);
                }
                
                // İkonları güncelle - BtnSetRating, FavoritesIconBlock, WatchListIconBlock
                
                double iconSize2 = CalculateScaledValue(width, 18, 33);
                var btnSetRating = mainMovieTvShow.FindControl<Icon>("BtnSetRating");
                if (btnSetRating != null)
                {
                    btnSetRating.FontSize = iconSize2;
                    btnSetRating.Margin = new Thickness(0, 0, marginSize, 0);
                }
                
                var favoritesIconBlock = mainMovieTvShow.FindControl<Icon>("FavoritesIconBlock");
                if (favoritesIconBlock != null)
                {
                    favoritesIconBlock.FontSize = iconSize;
                    favoritesIconBlock.Margin = new Thickness(0, 0, marginSize, 0);
                }
                
                var watchListIconBlock = mainMovieTvShow.FindControl<Icon>("WatchListIconBlock");
                if (watchListIconBlock != null)
                {
                    watchListIconBlock.FontSize = iconSize;
                    watchListIconBlock.Margin = new Thickness(0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AdjustInteractiveElements Exception: {ex.Message}");
            }
        }
        
        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                // Önce MainMovieTvShow kontrolüne eriş
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow == null) return;
                
                // Önce ScrollViewer pozisyonlarını sıfırla
                
                // Use our new comprehensive layout update method
                UpdateLayoutBasedOnSize(width);
                
                // Post-update adjustments specific to screen size
                if (width <= SMALL_SCREEN_THRESHOLD)
                {
                    // Mobile layout specific adjustments
                    
                    // Mobil mod için ListBox'ları ve scroll butonlarını ayarla
                    if (CastDisplay != null)
                    {
                        CastDisplay.Margin = new Thickness(0, 0, 10, 0);
                    }
                    
                    // if (SimilarMoviesDisplay != null)
                    // {
                    //     SimilarMoviesDisplay.Margin = new Thickness(0, 0, 10, 70);
                    // }
                }
                else
                {
                    // Desktop layout specific adjustments
                    
                    // Masaüstü mod için ListBox'ları ve scroll butonlarını ayarla
                    if (CastDisplay != null)
                    {
                        CastDisplay.Margin = new Thickness(0, 0, 0, 0);
                    }
                    
                    // if (SimilarMoviesDisplay != null)
                    // {
                    //     SimilarMoviesDisplay.Margin = new Thickness(0, 0, 0, 70);
                    // }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyResponsiveLayout Exception: {ex.Message}");
            }
        }
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
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
        
        // Kart genişliği için ölçekleme
        private double CalculateCardWidth(double width)
        {
            return CalculateScaledValue(width, 140, 400);
        }
        
        // Kart yüksekliği için ölçekleme
        private double CalculateCardHeight(double width)
        {
            return CalculateScaledValue(width, 210, 600);
        }

        private bool isTorrentTabBelow = false;
        private bool isCommentTabBelow = false;
         private void AdjustMenuLayout(double width)
        {
            try
            {
                if (MenuGrid == null) return;
                
                // Menü elemanları
                
                // MenuGrid'in mevcut durumunu sıfırla
                ResetMenuLayout();
                
                // Ekrana ne kadar sığacağını hesaplayalım
                double availableWidth = width * 0.8; // Ekran genişliğinin %80'ini kullan (menü için)
                double totalMenuWidth = CalculateMenuWidth();
              
                if(totalMenuWidth > width *0.9)
                {
                    AdjustMenuToMultipleRowsComment();
                    isCommentTabBelow = true;
                    isTorrentTabBelow = true;
                }
                else if (totalMenuWidth > availableWidth)
                {
                    // İki satırlık düzen oluştur
                    AdjustMenuToMultipleRows();
                    isCommentTabBelow = false;
                    isTorrentTabBelow = true;
                }
                else
                {
                    isCommentTabBelow = false;
                    isTorrentTabBelow = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AdjustMenuLayout Exception: {ex.Message}");
            }
        }
        
        private void ResetMenuLayout()
        {
            try
            {
                // Menü elemanları
                var menuItems = new[] { MenuOverview, MenuVideo, MenuPhotos, MenuComments, MenuTorrents };
                
                // MenuGrid'in RowDefinitions kısmını temizle
                MenuGrid.RowDefinitions.Clear();
                MenuGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                // Tüm menü öğelerini ilk satıra yerleştir
                foreach (var item in menuItems)
                {
                    if (item == null) continue;
                    
                    // Orijinal sütun konumunu ayarla
                    Grid.SetRow(item, 0);
                    if (item == MenuTorrents)
                    {
                        Grid.SetColumn(item, 8);
                    }
                    if (item == MenuComments)
                    {
                        Grid.SetColumn(item, 6);
                    }
                    // Görünürlüğünü ayarla
                    item.IsVisible = true;
                }
                
              
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ResetMenuLayout Exception: {ex.Message}");
            }
        }
        
        private double CalculateMenuWidth()
        {
            double totalWidth = 0;
            
            // Menü elemanları
            var menuItems = new[] { MenuOverview, MenuVideo, MenuPhotos, MenuComments, MenuTorrents };
            
            // Sütun genişlikleri
            var separatorWidths = MenuGrid.ColumnDefinitions
                .Where((_, i) => i % 2 == 1) // Tek indeksli sütunlar (ayırıcılar)
                .Select(cd => cd.ActualWidth) // Absolute genişlik değeri veya varsayılan 20
                .ToArray();
            
            // Menü elemanlarının genişliklerini topla
            for (int i = 0; i < menuItems.Length; i++)
            {
                var item = menuItems[i];
                if (item == null) continue;
                
                // Menü elemanının genişliği
                totalWidth += item.DesiredSize.Width;
                
                // Eğer son eleman değilse, ayırıcı genişliğini ekle
                if (i < menuItems.Length - 1 && i < separatorWidths.Length)
                {
                    totalWidth += separatorWidths[i];
                }
            }
            
            return totalWidth;
        }
        
        private void AdjustMenuToMultipleRows()
        {
            try
            {
                // İki satır için RowDefinitions oluştur
                MenuGrid.RowDefinitions.Clear();
                MenuGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                MenuGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // Satırlar arası boşluk
                MenuGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                // İlk satırda 4 eleman, ikinci satırda 1 eleman olacak
                Grid.SetRow(MenuOverview, 0);
                Grid.SetColumn(MenuOverview, 0);
                
                Grid.SetRow(MenuVideo, 0);
                Grid.SetColumn(MenuVideo, 2);
                
                Grid.SetRow(MenuPhotos, 0);
                Grid.SetColumn(MenuPhotos, 4);
                
                Grid.SetRow(MenuComments, 0);
                Grid.SetColumn(MenuComments, 6);
                
                // Torrents ikinci satıra
                Grid.SetRow(MenuTorrents, 2);
                Grid.SetColumn(MenuTorrents, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AdjustMenuToMultipleRows Exception: {ex.Message}");
            }
        }
        
        private void AdjustMenuToMultipleRowsComment()
        {
            try
            {
                // İki satır için RowDefinitions oluştur
                MenuGrid.RowDefinitions.Clear();
                MenuGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                MenuGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // Satırlar arası boşluk
                MenuGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                // İlk satırda 4 eleman, ikinci satırda 1 eleman olacak
                Grid.SetRow(MenuOverview, 0);
                Grid.SetColumn(MenuOverview, 0);
                
                Grid.SetRow(MenuVideo, 0);
                Grid.SetColumn(MenuVideo, 2);
                
                Grid.SetRow(MenuPhotos, 0);
                Grid.SetColumn(MenuPhotos, 4);
                
                Grid.SetRow(MenuComments, 2);
                Grid.SetColumn(MenuComments, 0);
                
                // Torrents ikinci satıra
                Grid.SetRow(MenuTorrents, 2);
                Grid.SetColumn(MenuTorrents, 2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AdjustMenuToMultipleRows Exception: {ex.Message}");
            }
        }
        private void UpdateLayoutBasedOnSize(double width)
        {
            try
            {
                // MainMovieTvShow kontrolüne eriş
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow == null) return;
                
                // OverviewTextBlock ve MainMovieTitle kontrollerine erişim
                var overviewTextBlock = mainMovieTvShow.FindControl<TextBlock>("OverviewTextBlock");
                var mainMovieTitle = mainMovieTvShow.FindControl<TextBlock>("MainMovieTitle");
                
                // Ekran genişliğini maksimum 3840px ile sınırla
                double clampedWidth = Math.Min(width, 3840);
                
                bool isSmallScreen = clampedWidth <= SMALL_SCREEN_THRESHOLD;
                bool isExtraSmallScreen = clampedWidth <= EXTRA_SMALL_SCREEN_THRESHOLD;
                
                // Record screen state changes
                bool screenSizeChanged = this.isSmallScreen != isSmallScreen;
                
                this.isSmallScreen = isSmallScreen;
                this.isExtraSmallScreen = isExtraSmallScreen;
                
                // Ana başlık boyutları için ölçeklendirme değerleri
                double headerFontSize = CalculateTextSize(clampedWidth, 18, 36);
                
                // WrapPanel'daki öğelerin boyutları için ölçeklendirme
                double viewboxMaxHeight = CalculateScaledValue(clampedWidth, 16, 40);
                double ellipseSize = viewboxMaxHeight * 0.2;
                
                double tabFontSize = CalculateTextSize(clampedWidth, 14, 30);
                MenuTorrents.FontSize = tabFontSize;
                MenuOverview.FontSize = tabFontSize;
                MenuVideo.FontSize = tabFontSize;
                MenuPhotos.FontSize = tabFontSize;
                MenuComments.FontSize = tabFontSize;
                MenuBottomLine.Height = CalculateScaledValue(clampedWidth, 2, 5);

                var columnnWidth = CalculateScaledValue(clampedWidth, 20, 119);
                MenuGrid.ColumnDefinitions[1].Width = new GridLength(columnnWidth);
                MenuGrid.ColumnDefinitions[3].Width = new GridLength(columnnWidth);
                MenuGrid.ColumnDefinitions[5].Width = new GridLength(columnnWidth);
                MenuGrid.ColumnDefinitions[7].Width = new GridLength(columnnWidth);
                
                AdjustMenuLayout(width);
                
                // Başlıkları boyutlandır ve marjinleri ayarla
                var headerMargin = CalculateScaledValue(clampedWidth, 20, 100);
                
                // Configure section titles - using the correct element names from XAML
                if (TextBlockCast != null)
                {
                    TextBlockCast.FontSize = headerFontSize;
                }
                
                if (TextBlockSimilar != null)
                {
                    TextBlockSimilar.FontSize = headerFontSize;
                }
                
                // Configure main movie description
                if (overviewTextBlock != null)
                {
                    double overviewFontSize = CalculateTextSize(clampedWidth, 12, 27);
                    overviewTextBlock.FontSize = overviewFontSize;
                    
                    // Ekran boyutuna göre maksimum genişliği ayarla
                    double maxWidth = CalculateScaledValue(clampedWidth, clampedWidth - 40, 1200);
                    overviewTextBlock.MaxWidth = clampedWidth <= SMALL_SCREEN_THRESHOLD ? clampedWidth - 40 : maxWidth;
                }
                
                // Configure main movie title
                if (mainMovieTitle != null)
                {
                    // Doğrudan ekran genişliğine göre ölçeklendirme yapalım, converter'a güvenmek yerine
                    var titleFontSize = CalculateFontSizeForTitle(clampedWidth);
                    mainMovieTitle.FontSize = titleFontSize;
                }
                
                // Configure main movie info
                if (mainMovieTvShow != null)
                {
                    if (screenSizeChanged || Math.Abs(clampedWidth - previousWidth) > 50)
                    {
                        double mainMovieMargin = CalculateScaledValue(clampedWidth, 10, 100);
                        
                        if (isSmallScreen)
                        {
                            mainMovieTvShow.Margin = new Thickness(10);
                        }
                        else
                        {
                            mainMovieTvShow.Margin = new Thickness(mainMovieMargin, 20, mainMovieMargin, 20);
                        }
                    }
                }
                
                // Scroll butonlarını ayarla
                AdjustScrollButtonsPosition(clampedWidth);
                
                // // TvShowDisplay margin'lerini ayarla
                // if (SimilarMoviesDisplay != null)
                // {
                //     if (clampedWidth <= SMALL_SCREEN_THRESHOLD)
                //     {
                //         // Mobil görünümde daha fazla alt boşluk (navigasyon barı için)
                //         SimilarMoviesDisplay.Margin = new Thickness(0, 0, 10, 100);
                //     }
                //     else
                //     {
                //         // Desktop görünümde de yeterli alt boşluk
                //         double margin = CalculateScaledValue(clampedWidth, 40, 80);
                //         SimilarMoviesDisplay.Margin = new Thickness(0, 0, margin, 70);
                //     }
                // }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateLayoutBasedOnSize Exception: {ex.Message}");
            }
        }
       
        private double CalculateFontSizeForTitle(double width)
        {
            // Doğrusal ölçeklendirme formülü: y = mx + b
            // Ekran boyutu arttıkça font boyutu da artacak şekilde
            const double minWidth = 320;   // En küçük ekran genişliği (piksel)
            const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
            const double minFontSize = 18; // En küçük font boyutu
            const double maxFontSize = 90; // En büyük font boyutu
            
            // Ekran genişliğini sınırla
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Doğrusal ölçeklendirme formülü
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double fontSize = minFontSize + scale * (maxFontSize - minFontSize);
            
            // Font boyutunu yuvarla
            return Math.Round(fontSize);
        }
        private void AdjustMainMoviePosterSize(double width)
        {
            try
            {
                // MainMovieTvShow kontrolüne eriş
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow == null) return;
                
                // PosterImage kontrolü artık MainMovieTvShow içinde
                var posterImage = mainMovieTvShow.FindControl<AsyncImageControl2>("PosterImage");
                if (posterImage == null) return;
                
                // Minimum ve maksimum yükseklik değerleri
                const double minWidth = 320;   // En küçük ekran genişliği
                const double maxWidth = 3840;  // En büyük ekran genişliği
                const double minHeight = 300;  // Minimum poster yüksekliği
                const double maxHeight = 700;  // Maksimum poster yüksekliği
                
                // Ekran genişliğini sınırla
                double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
                
                // Doğrusal ölçeklendirme ile poster yüksekliğini hesapla
                double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
                double dynamicHeight = minHeight + scale * (maxHeight - minHeight);
                
                // Değeri yuvarla
                dynamicHeight = Math.Round(dynamicHeight);
                
                // Poster yüksekliğini güncelle
                if (Math.Abs(posterImage.Height - dynamicHeight) > 0.1)
                {
                    posterImage.Height = dynamicHeight;
                    
                    // Ana filmin minimum yüksekliğini de ayarla
                    mainMovieTvShow.MinHeight = dynamicHeight;
                }
            }
            catch (Exception ex)
            {
               Console.WriteLine($"Error in AdjustMainMoviePosterSize: {ex.Message}");
            }
        }
        
       
        
        private async void ScrollViewer_OnScrollChanged(object sender, Avalonia.Controls.ScrollChangedEventArgs e)
        {
            try
            {
                var scrollViewer = sender as ScrollViewer;
                if(scrollViewer == null) return;
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.InvalidateMeasure();
                    this.InvalidateArrange();
                });
                    if (TorrentsPage != null && TorrentsPage.isItemLoadingFinished)
                    {
                        if (scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height - 900)
                        {
                            if (!TorrentsPage.sortByComboboxSelectionChanged)
                            {
                                if (TorrentsPage.selectedMovie.ShowType == ShowType.Movie && TorrentsPage.loadingMovieTorrentsFinished)
                                {
                                    await TorrentsPage.LoadMovieTorrents();
                                }
                                else if (TorrentsPage.selectedMovie.ShowType == ShowType.TvShow &&
                                         TorrentsPage.loadingtvTorrentsFinished)
                                {
                                    await TorrentsPage.LoadTvShows();
                                }
                            }
                            else
                            {
                                if (TorrentsPage.isSortedCollectionLoadingFinished)
                                {
                                    await TorrentsPage.LoadMoreTorrentCollection();
                                }   
                            }
                        }
                    }
                
                
                // Video görünürlük kontrolü
                var sv = sender as ScrollViewer;
                if (sv != null)
                {
                    try
                    {
                        // Şu anki görünür alan (viewport) bilgisi
                        var svViewportBounds = new Rect(sv.Offset.X, sv.Offset.Y, sv.Viewport.Width, sv.Viewport.Height);
                        
                        // Görünürlük eşik değeri (viewport yüksekliğinin yüzdesi olarak)
                        double visibilityThreshold = sv.Viewport.Height * 0.3;
                        
                        // TrailerPlayer kontrolü
                        if (TrailerPlayer != null && TrailerPlayer.Bounds.Height > 0)
                        {
                            // TrailerPlayer'ın yerini belirle
                            var offset = TrailerPlayer.TranslatePoint(new Point(0, 0), sv) ?? new Point(0, 0);
                            
                            // Görünürlük kontrolü için daha dinamik bir Rect hesapla
                            var trPlayerRect = new Rect(
                                offset.X, 
                                offset.Y, 
                                TrailerPlayer.Bounds.Width, 
                                TrailerPlayer.Bounds.Height);
                            
                            // Görünür alanda ne kadar yer kapladığını hesapla
                            var intersection = trPlayerRect.Intersect(svViewportBounds);
                            double visiblePercentage = intersection.Height / TrailerPlayer.Bounds.Height;
                            Console.WriteLine("Trailer Player visible percentege: " +visiblePercentage);
                            // Yeterince görünür değilse videoyu durdur
                            if (visiblePercentage < 0.8) // %40'dan az görünüyorsa
                            {
                                if (mediaPlayer != null && mediaPlayer.IsPlaying)
                                {
                                    mediaPlayer.Pause();
                                    
                                    // UI güncelleme işlemi
                                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        TrailerPlayer.IsVisible = false;
                                        MainMovieTvShow.IsVisible = true;
                                    });
                                }
                            }
                        }

                        // MainMovieTvShow kontrolü
                        if (MainMovieTvShow != null && MainMovieTvShow.Bounds.Height > 0)
                        {
                            // MainMovieTvShow'un yerini belirle
                            var offset = MainMovieTvShow.TranslatePoint(new Point(0, 0), sv) ?? new Point(0, 0);
                            
                            // Görünürlük kontrolü için daha dinamik bir Rect hesapla
                            var mainMovieRect = new Rect(
                                offset.X, 
                                offset.Y, 
                                MainMovieTvShow.Bounds.Width, 
                                MainMovieTvShow.Bounds.Height);
                            
                            // Viewport'un üst kısmında mı kontrol et
                            bool isInTopArea = offset.Y >= -visibilityThreshold && offset.Y <= visibilityThreshold;
                            
                            // Görünür alanda ne kadar yer kapladığını hesapla
                            var intersection = mainMovieRect.Intersect(svViewportBounds);
                            double visiblePercentage = intersection.Height / MainMovieTvShow.Bounds.Height;
                            Console.WriteLine("MainMovieTvShow visible percentege: " +visiblePercentage);
                            // Yeterince görünür ve üst bölgede ise videoyu başlat
                            if (visiblePercentage > 0.8 && isInTopArea) // %50'den fazla görünüyorsa ve üst bölgede ise
                            {
                                if (mediaPlayer != null && mediaPlayer.State != VLCState.Playing && 
                                    mediaPlayer.State != VLCState.Ended && mediaPlayer.State != VLCState.Error)
                                {
                                    mediaPlayer.Play();
                                    
                                    // UI güncelleme işlemi
                                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        TrailerPlayer.IsVisible = true;
                                        MainMovieTvShow.IsVisible = false;
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                       Console.WriteLine($"Video görünürlük kontrolünde hata: {ex.Message}");
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
               Console.WriteLine(errorMessage);
            }
        }
        
     
        public void WatchButton_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            // Handle watch button click
        }
        
        public async void WatchNowButton_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                // Stop and dispose trailer if playing
                await CleanupVideoPlayer();
                
                if (selectedMovie == null)
                {
                    Debug.WriteLine("WatchNow: No movie selected");
                    return;
                }

                //Debug.WriteLine($"WatchNow: Starting for {selectedMovie.Title}");

                // Search for torrents
                List<Item> torrents = null;
                
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    // Get movie details first
                    var movie = await Service.client.GetMovieAsync(selectedMovie.Id, "en", null, MovieMethods.Images);
                    if (string.IsNullOrEmpty(movie.ImdbId))
                    {
                        Debug.WriteLine("WatchNow: No IMDb ID found for movie");
                        // TODO: Show notification to user
                        return;
                    }
                    
                    // Search for torrents using IMDb ID
                    torrents = await JackettService.GetMovieTorrentsImdb(movie.ImdbId);
                   torrents.AddRange( await JackettService.GetMovieTorrentsName(movie.OriginalTitle,movie.ReleaseDate.HasValue ? movie.ReleaseDate.Value.Year : 0));
                }
                else
                {
                    // For TV shows, we need season and episode info
                    Debug.WriteLine("WatchNow: TV show support coming soon");
                    // TODO: Implement TV show support
                    return;
                }

                if (torrents == null || !torrents.Any())
                {
                    Debug.WriteLine("WatchNow: No torrents found");
                    // TODO: Show notification to user
                    return;
                }

                Debug.WriteLine($"WatchNow: Found {torrents.Count} torrents");

                // Use TorrentSelectionService to select the best torrent with user's preferred quality
                var selectionService = new TorrentSelectionService();
                var preferredQuality = AppSettingsManager.appSettings.WatchNowDefaultQuality;
                var bestTorrent = await selectionService.SelectBestTorrentAsync(torrents, preferredQuality);

                if (bestTorrent == null)
                {
                    Debug.WriteLine("WatchNow: No suitable torrent found");
                    // TODO: Show notification to user
                    return;
                }

                Debug.WriteLine($"WatchNow: Selected torrent: {bestTorrent.Title}");
                Debug.WriteLine($"WatchNow: Seeds: {bestTorrent.Seeders}, Size: {bestTorrent.Size} bytes");

                // Fill in missing torrent information
                bestTorrent.MovieId = selectedMovie.Id;
                bestTorrent.MovieName = selectedMovie.Name;
                bestTorrent.ShowType = selectedMovie.ShowType;
                bestTorrent.SeasonNumber = 0;
                bestTorrent.EpisodeNumber = 0;
                
                var movie2 = await Service.client.GetMovieAsync(selectedMovie.Id);
                if (!string.IsNullOrEmpty(movie2.ImdbId))
                {
                    bestTorrent.ImdbId = Int32.Parse(new String(movie2.ImdbId.Where(Char.IsDigit).ToArray()));
                }
                
                // Set poster image
                var images = await Service.client.GetMovieImagesAsync(selectedMovie.Id);
                if (images?.Backdrops?.Any() == true)
                {
                    var rnd = new Random();
                    int randomNumber = rnd.Next(0, images.Backdrops.Count - 1);
                    var image = Service.client.GetImageUrl(
                        Service.client.Config.Images.BackdropSizes[Service.client.Config.Images.BackdropSizes.Count - 4],
                        images.Backdrops[randomNumber].FilePath).AbsoluteUri;
                    bestTorrent.Poster = image;
                }

                // Start the torrent download with sequential mode
                string torrentLink = bestTorrent.Link;
                if (string.IsNullOrEmpty(torrentLink) && bestTorrent.Enclosure != null && !string.IsNullOrEmpty(bestTorrent.Enclosure.Url))
                {
                    torrentLink = bestTorrent.Enclosure.Url;
                    bestTorrent.Link = torrentLink;
                }
                
                if (string.IsNullOrEmpty(torrentLink))
                {
                    Debug.WriteLine("WatchNow: No valid torrent link");
                    return;
                }

                // Download torrent file or use magnet
                string torrentPath = "";
                if (!torrentLink.ToLower().Contains("magnet"))
                {
                    try
                    {
                        torrentPath = System.IO.Path.Combine(AppSettingsManager.appSettings.TorrentsPath, bestTorrent.Title + ".torrent");
                        if (!File.Exists(torrentPath))
                        {
                            using (var httpClient = new System.Net.Http.HttpClient())
                            {
                                using (var stream = await httpClient.GetStreamAsync(torrentLink))
                                {
                                    using (var fileStream = new FileStream(torrentPath, FileMode.CreateNew))
                                    {
                                        await stream.CopyToAsync(fileStream);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"WatchNow: Error downloading torrent file: {ex.Message}");
                        torrentPath = "";
                    }
                }

                // Add torrent to libtorrent
                string hash = !string.IsNullOrWhiteSpace(torrentPath)
                    ? await Libtorrent.AddTorrentFromFile(torrentPath)
                    : await Libtorrent.AddTorrentFromMagnet(torrentLink);

                if (string.IsNullOrWhiteSpace(hash))
                {
                    Debug.WriteLine("WatchNow: Failed to add torrent");
                    return;
                }

                bestTorrent.Hash = hash;
                await Libtorrent.DownloadFileSequentially(hash);

                Debug.WriteLine($"WatchNow: Torrent added with hash: {hash}");

                // Wait a moment for torrent info to be available
                await Task.Delay(2000);

                // Get files from torrent
                var files = await Libtorrent.GetFiles(hash);
                if (files == null || !files.Any(f => f.IsMediaFile))
                {
                    Debug.WriteLine("WatchNow: No media files found in torrent");
                    await Libtorrent.Delete(hash);
                    return;
                }

                // Find the largest media file (main movie)
                var mainFile = files.Where(f => f.IsMediaFile).OrderByDescending(f => f.Size).FirstOrDefault();
                if (mainFile == null)
                {
                    Debug.WriteLine("WatchNow: Could not find main movie file");
                    await Libtorrent.Delete(hash);
                    return;
                }

                Debug.WriteLine($"WatchNow: Found main file: {mainFile.Name}");

                // Set this file to maximum priority
                await Libtorrent.ChangeMovieCollectionFilePriorityToMaximal(bestTorrent, mainFile.Index);

                // Open player window directly
                var playerWindow = new PlayerWindow(
                    bestTorrent.MovieId,
                    bestTorrent.MovieName,
                    bestTorrent.ShowType,
                    0, // season
                    0, // episode
                    new FileInfo(mainFile.FullPath),
                    false, // not completed yet
                    bestTorrent.ImdbId,
                    bestTorrent,
                    mainFile.Index,
                    bestTorrent.Poster,
                    null);

                // Handle cleanup when player closes
                playerWindow.Unloaded += async (o, args) =>
                {
                    try
                    {
                        Debug.WriteLine($"WatchNow: Player closed, deleting torrent {hash}");
                        
                        // Try to delete through Libtorrent first
                        await Libtorrent.Delete(hash);
                        
                        // Give it a moment to delete
                        await Task.Delay(500);
                        
                        // Manually delete files if they still exist
                        try
                        {
                            string moviesPath = AppSettingsManager.appSettings.MoviesPath;
                            string resumePath = Path.Combine(AppSettingsManager.appSettings.TorrentsPath, "Resume");
                            
                            // Delete resume files
                            if (Directory.Exists(resumePath))
                            {
                                var resumeFiles = Directory.GetFiles(resumePath, $"*{hash}*", SearchOption.AllDirectories);
                                foreach (var file in resumeFiles)
                                {
                                    try
                                    {
                                        File.Delete(file);
                                        Debug.WriteLine($"WatchNow: Manually deleted resume file: {file}");
                                    }
                                    catch (Exception deleteEx)
                                    {
                                        Debug.WriteLine($"WatchNow: Could not delete resume file {file}: {deleteEx.Message}");
                                    }
                                }
                            }
                            
                            // Delete movie files
                            if (Directory.Exists(moviesPath) && mainFile != null)
                            {
                                var fileDir = Path.GetDirectoryName(mainFile.FullPath);
                                if (!string.IsNullOrEmpty(fileDir) && Directory.Exists(fileDir))
                                {
                                    try
                                    {
                                        Directory.Delete(fileDir, true);
                                        Debug.WriteLine($"WatchNow: Manually deleted movie directory: {fileDir}");
                                    }
                                    catch (Exception deleteDirEx)
                                    {
                                        Debug.WriteLine($"WatchNow: Could not delete directory {fileDir}: {deleteDirEx.Message}");
                                        
                                        // Try to delete individual files
                                        try
                                        {
                                            var files = Directory.GetFiles(fileDir);
                                            foreach (var file in files)
                                            {
                                                try
                                                {
                                                    File.Delete(file);
                                                    Debug.WriteLine($"WatchNow: Manually deleted file: {file}");
                                                }
                                                catch (Exception fileEx)
                                                {
                                                    Debug.WriteLine($"WatchNow: Could not delete file {file}: {fileEx.Message}");
                                                }
                                            }
                                        }
                                        catch (Exception filesEx)
                                        {
                                            Debug.WriteLine($"WatchNow: Could not enumerate files: {filesEx.Message}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception cleanupEx)
                        {
                            Debug.WriteLine($"WatchNow: Manual cleanup error: {cleanupEx.Message}");
                        }
                        
                        Debug.WriteLine("WatchNow: Cleanup completed");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"WatchNow: Cleanup error: {ex.Message}");
                    }
                };

                MainWindow.Instance.SetContent(playerWindow);
                Debug.WriteLine("WatchNow: Player opened successfully");
            }
            catch (Exception exception)
            {
                var errorMessage = $"WatchNow Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Debug.WriteLine(errorMessage);
                Log.Error(errorMessage);
            }
        }
        
        public void BtnSetRating_OnMouseEnter(object? sender, PointerEventArgs e)
        {
            if(rating.HasValue) return;
            
            var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
            if (mainMovieTvShow == null) return;
            
            var btnSetRating = mainMovieTvShow.FindControl<Icon>("BtnSetRating");
            if (btnSetRating == null) return;
            
            btnSetRating.Value = "fa-solid fa-star";
            btnSetRating.Foreground = new SolidColorBrush(Color.Parse("#E50914"));
        }
        
        public void BtnSetRating_OnMouseLeave(object? sender, PointerEventArgs e)
        {
            if (rating.HasValue) return;
            
            var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
            if (mainMovieTvShow == null) return;
            
            var btnSetRating = mainMovieTvShow.FindControl<Icon>("BtnSetRating");
            if (btnSetRating == null) return;
            
            btnSetRating.Value = "fa-light fa-star";
            btnSetRating.Foreground = new SolidColorBrush(Color.Parse("#808080"));
        }
        
        public void BtnSetRating_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            DialogHost.IsOpen = true;
        }
        
        public void FavoritesIconBlock_OnMouseEnter(object? sender, PointerEventArgs e)
        {
            if (favorite) return;
            
            var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
            if (mainMovieTvShow == null) return;
            
            var favoritesIconBlock = mainMovieTvShow.FindControl<Icon>("FavoritesIconBlock");
            if (favoritesIconBlock == null) return;
            
            favoritesIconBlock.Value = "fa-solid fa-heart";
            favoritesIconBlock.Foreground = new SolidColorBrush(Color.Parse("#E50914"));
        }
        
        public void FavoritesIconBlock_OnMouseLeave(object? sender, PointerEventArgs e)
        {
            if (favorite) return;
            
            var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
            if (mainMovieTvShow == null) return;
            
            var favoritesIconBlock = mainMovieTvShow.FindControl<Icon>("FavoritesIconBlock");
            if (favoritesIconBlock == null) return;
            
            favoritesIconBlock.Value = "fa-light fa-heart";
            favoritesIconBlock.Foreground = new SolidColorBrush(Color.Parse("#808080"));
        }
        
        public async void FavoritesIconBlock_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow == null) return;
                
                var favoritesIconBlock = mainMovieTvShow.FindControl<Icon>("FavoritesIconBlock");
                if (favoritesIconBlock == null) return;
                
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    if (favorite)
                    {
                        await Service.client.AccountChangeFavoriteStatusAsync(TMDbLib.Objects.General.MediaType.Movie,
                            selectedMovie.Id, false);
                        favorite = false;
                        favoritesIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                        favoritesIconBlock.Value = "fa-light fa-heart";
                        await NotificationService.Instance.ShowNotification(mainMovieTvShow.MainMovieTitle.Text + " " +ResourceProvider.GetString("RemoveFavoritesNotify"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),false);
                        //Growl.Success(new GrowlInfo(){Message = MainMovieName.Text + " " +ResourceProvider.GetString("RemoveFavoritesNotify"],StaysOpen = false,WaitTime = 4});
                    }
                    else
                    {
                        await Service.client.AccountChangeFavoriteStatusAsync(TMDbLib.Objects.General.MediaType.Movie,
                            selectedMovie.Id, true);
                        favorite = true;
                        favoritesIconBlock.Foreground =
                            new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                        favoritesIconBlock.Value = "fa-solid fa-heart";
                        await NotificationService.Instance.ShowNotification(mainMovieTvShow.MainMovieTitle.Text + " " +ResourceProvider.GetString("AddFavoritesNotify"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),false);
                        //Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " +ResourceProvider.GetString("AddFavoritesNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                }
                else
                {
                    if (favorite)
                    {
                        await Service.client.AccountChangeFavoriteStatusAsync(TMDbLib.Objects.General.MediaType.Tv,
                            selectedMovie.Id, false);
                        favorite = false;
                        favoritesIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                        favoritesIconBlock.Value = "fa-light fa-heart";
                        await NotificationService.Instance.ShowNotification(mainMovieTvShow.MainMovieTitle.Text + " " +ResourceProvider.GetString("RemoveFavoritesNotify"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),false);
                        //Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " +ResourceProvider.GetString("RemoveFavoritesNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                    else
                    {
                        await Service.client.AccountChangeFavoriteStatusAsync(TMDbLib.Objects.General.MediaType.Tv,
                            selectedMovie.Id, true);
                        favorite = true;
                        favoritesIconBlock.Foreground =
                            new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                        favoritesIconBlock.Value = "fa-solid fa-heart";
                        await NotificationService.Instance.ShowNotification(mainMovieTvShow.MainMovieTitle.Text + " " +ResourceProvider.GetString("AddFavoritesNotify"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),false);
                        //Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " +ResourceProvider.GetString("AddFavoritesNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
               Console.WriteLine(errorMessage);
            }
        }
        
        public void WatchListIconBlock_OnMouseEnter(object? sender, PointerEventArgs e)
        {
            if(watchList) return;
            
            var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
            if (mainMovieTvShow == null) return;
            
            var watchListIconBlock = mainMovieTvShow.FindControl<Icon>("WatchListIconBlock");
            if (watchListIconBlock == null) return;
            
            watchListIconBlock.Value = "fa-solid fa-bookmark";
            watchListIconBlock.Foreground = new SolidColorBrush(Color.Parse("#E50914"));
        }
        
        public void WatchListIconBlock_OnMouseLeave(object? sender, PointerEventArgs e)
        {
            if(watchList) return;
            
            var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
            if (mainMovieTvShow == null) return;
            
            var watchListIconBlock = mainMovieTvShow.FindControl<Icon>("WatchListIconBlock");
            if (watchListIconBlock == null) return;
            
            watchListIconBlock.Value = "fa-light fa-bookmark";
            watchListIconBlock.Foreground = new SolidColorBrush(Color.Parse("#808080"));
        }
        
        public async void WatchListIconBlock_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow == null) return;
                
                var watchListIconBlock = mainMovieTvShow.FindControl<Icon>("WatchListIconBlock");
                if (watchListIconBlock == null) return;
                
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    if (watchList)
                    {
                        await Service.client.AccountChangeWatchlistStatusAsync(TMDbLib.Objects.General.MediaType.Movie, selectedMovie.Id,
                            false);
                        watchList = false;
                        watchListIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                        watchListIconBlock.Value = "fa-light fa-bookmark";
                        await NotificationService.Instance.ShowNotification(mainMovieTvShow.MainMovieTitle.Text + " " +ResourceProvider.GetString("RemoveWatchListNotify"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),false);
                        // Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " +ResourceProvider.GetString("RemoveWatchListNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                    else
                    {
                        await Service.client.AccountChangeWatchlistStatusAsync(TMDbLib.Objects.General.MediaType.Movie, selectedMovie.Id, true);
                        watchList = true;
                        watchListIconBlock.Foreground =
                            new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                        watchListIconBlock.Value = "fa-solid fa-bookmark";
                        await NotificationService.Instance.ShowNotification(mainMovieTvShow.MainMovieTitle.Text + " " +ResourceProvider.GetString("AddWatchListNotify"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),false);
                        //Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " +ResourceProvider.GetString("AddWatchListNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                }
                else
                {
                    if (watchList)
                    {
                        await Service.client.AccountChangeWatchlistStatusAsync(TMDbLib.Objects.General.MediaType.Tv, selectedMovie.Id, false);
                        watchList = false;
                        watchListIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                        watchListIconBlock.Value = "fa-light fa-bookmark";
                        await NotificationService.Instance.ShowNotification(mainMovieTvShow.MainMovieTitle.Text + " " +ResourceProvider.GetString("RemoveWatchListNotify"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),false);
                        //Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " +ResourceProvider.GetString("RemoveWatchListNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                    else
                    {
                        await Service.client.AccountChangeWatchlistStatusAsync(TMDbLib.Objects.General.MediaType.Tv, selectedMovie.Id, true);
                        watchList = true;
                        watchListIconBlock.Foreground =
                            new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                        watchListIconBlock.Value = "fa-solid fa-bookmark";
                        await NotificationService.Instance.ShowNotification(mainMovieTvShow.MainMovieTitle.Text + " " +ResourceProvider.GetString("AddWatchListNotify"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),false);
                        //Growl.Success(new GrowlInfo() { Message = , StaysOpen = false, WaitTime = 4 });
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
               Console.WriteLine(errorMessage);
            }
        }

        private ClickedTabType ClickedTabType = ClickedTabType.Overview;
        private async void MenuOverview_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                ClickedTabType = ClickedTabType.Overview;
                Service.PhotoDetailsBackdrop.Clear();
                Service.PhotoDetailsPoster.Clear();
                Service.VideoDetails.Clear();
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                        this.MenuBottomLine.SetValue(Grid.ColumnProperty, 0);
                        this.MenuBottomLine.SetValue(Grid.RowProperty, 0);
                        MenuBottomLine.Width = MenuOverview.Width;
                        MenuOverview.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        MenuVideo.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuPhotos.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuTorrents.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuComments.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    });
              
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    MovieDetailsNavigation.Content =(new MovieDetailsOverViewPage(selectedMovie));
                }
                else if (selectedMovie.ShowType == ShowType.TvShow)
                {
                    MovieDetailsNavigation.Content =(new TvShowDetailsOverViewPage(selectedMovie));
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
               Console.WriteLine(errorMessage);
            }
        }
        
        private async void MenuVideo_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                ClickedTabType = ClickedTabType.Video;
                Service.PhotoDetailsBackdrop.Clear();
                Service.PhotoDetailsPoster.Clear();
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                        this.MenuBottomLine.SetValue(Grid.ColumnProperty, 2);
                        this.MenuBottomLine.SetValue(Grid.RowProperty, 0);
                        MenuBottomLine.Width = MenuVideo.Width;
                        MenuOverview.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuVideo.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        MenuPhotos.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuTorrents.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuComments.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    });

                MovieDetailsNavigation.Content =(new MovieDetailsVideoPage(selectedMovie,this));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
               Console.WriteLine(errorMessage);
            }
        }
        
        private async void MenuPhotos_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                ClickedTabType = ClickedTabType.Photo;
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                        this.MenuBottomLine.SetValue(Grid.ColumnProperty, 4);
                        this.MenuBottomLine.SetValue(Grid.RowProperty, 0);
                        MenuBottomLine.Width = MenuPhotos.Width;
                        MenuOverview.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuVideo.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuPhotos.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        MenuTorrents.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuComments.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    });
                MovieDetailsNavigation.Content = (new MovieDetailsPhotosPage(selectedMovie));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
               Console.WriteLine(errorMessage);
            }
        }
        
        private async void MenuComments_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                ClickedTabType = ClickedTabType.Comment;
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                        this.MenuBottomLine.SetValue(Grid.ColumnProperty, isCommentTabBelow ? 0 : 6);
                        this.MenuBottomLine.SetValue(Grid.RowProperty, isCommentTabBelow ? 2 : 0);
                        MenuBottomLine.Width = MenuComments.Width;
                        MenuOverview.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuVideo.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuPhotos.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuTorrents.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuComments.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    });
                MovieDetailsNavigation.Content =(new CommentPage(selectedMovie));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
               Console.WriteLine(errorMessage);
            }
        }
        public static TorrentsPage TorrentsPage;

        private async void MenuTorrents_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                ClickedTabType = ClickedTabType.Torrent;
                Service.PhotoDetailsBackdrop.Clear();
                Service.PhotoDetailsPoster.Clear();
                Service.VideoDetails.Clear();
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                        this.MenuBottomLine.SetValue(Grid.ColumnProperty, isTorrentTabBelow ? 2 :8);
                        this.MenuBottomLine.SetValue(Grid.RowProperty, isTorrentTabBelow ? 2 : 0);
                        MenuBottomLine.Width = MenuTorrents.Width;
                        MenuOverview.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuVideo.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuPhotos.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuTorrents.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        MenuComments.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    });
                if (selectedMovie.ShowType == ShowType.Movie)
        {
            if (_cachedTorrentsPage == null)
            {
                _cachedTorrentsPage = new TorrentsPage(selectedMovie);
            }
            MovieDetailsNavigation.Content = _cachedTorrentsPage;
        }
                else
                {
                    MovieDetailsNavigation.Content=new EpisodesPage(selectedMovie);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
               Console.WriteLine(errorMessage);
            }
        }
        
        
      
        
      
    
        
      
     
        
         private bool favorite = false;
 private bool watchList = false;
 private double? rating;
 private async void SetStateAccountIcons()
 {
     try
     {
         // MainMovieTvShow kontrolüne eriş
         var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
         if (mainMovieTvShow == null) return;
         
         // Kontrollar artık MainMovieTvShow içinde
        // var favoritesElipse = mainMovieTvShow.FindControl<Ellipse>("FavoritesElipse");
         var favoritesIconBlock = mainMovieTvShow.FindControl<Icon>("FavoritesIconBlock");
        // var watchListElipse = mainMovieTvShow.FindControl<Ellipse>("WatchListElipse");
         var watchListIconBlock = mainMovieTvShow.FindControl<Icon>("WatchListIconBlock");
         var btnSetRating = mainMovieTvShow.FindControl<Icon>("BtnSetRating");
        // var ratingElipse = mainMovieTvShow.FindControl<Ellipse>("RatingElipse");
         
         if (Service.client.ActiveAccount != null && !String.IsNullOrWhiteSpace(Service.client.SessionId))
         {
             // if (favoritesElipse != null) favoritesElipse.IsVisible = true;
             // if (favoritesIconBlock != null) favoritesIconBlock.IsVisible = true;
             // if (watchListElipse != null) watchListElipse.IsVisible = true;
             // if (watchListIconBlock != null) watchListIconBlock.IsVisible = true;

             var mainMovie = mainMovieTvShow.DataContext as MainMovie;
             if (mainMovie != null)
             {
                 if (mainMovie.IsFavorite)
                 {
                     if (favoritesIconBlock != null)
                     {
                         favoritesIconBlock.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                         favoritesIconBlock.Value = "fa-solid fa-heart";
                     }
                     favorite = true;
                 }
                 else
                 {
                     if (favoritesIconBlock != null)
                     {
                         favoritesIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                         favoritesIconBlock.Value = "fa-light fa-heart";
                     }
                     favorite = false;
                 }

                 if (mainMovie.IsInWatchlist)
                 {
                     if (watchListIconBlock != null)
                     {
                         watchListIconBlock.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                         watchListIconBlock.Value = "fa-solid fa-bookmark";
                     }
                     watchList = true;
                 }
                 else
                 {
                     if (watchListIconBlock != null)
                     {
                         watchListIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                         watchListIconBlock.Value = "fa-light fa-bookmark";
                     }
                     watchList = false;
                 }

                 if (mainMovie.MyRating.HasValue)
                 {
                     if (btnSetRating != null)
                     {
                         btnSetRating.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                         btnSetRating.Value = "fa-solid fa-star";
                     }
                     rating = mainMovie.MyRating;
                     TextBlockDialogText.Text = ResourceProvider.GetString("YourVote").ToString() + " " + rating;
                     BtnConfirm.Content = ResourceProvider.GetString("Rerate").ToString();
                     SetRating.Rating = mainMovie.MyRating.Value;
                     BtnRemoveRating.IsVisible = true;
                 }
                 else
                 {
                     if (btnSetRating != null)
                     {
                         btnSetRating.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                         btnSetRating.Value = "fa-light fa-star";
                     }
                     rating = null;
                     TextBlockDialogText.Text = ResourceProvider.GetString("WhatIsYourRating").ToString();
                     BtnConfirm.Content = ResourceProvider.GetString("Confirm").ToString();
                     SetRating.Rating = 0;
                     BtnRemoveRating.IsVisible = false;
                 }
             }
         
         }
     }
     catch (Exception e)
     {
         var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
        Console.WriteLine(errorMessage);
     }
 }

        public async void Dispose()
        {
            try
            {
                unloaded = true;
                
                // Video player'ı temizle
                await CleanupVideoPlayer();
                
                // Event handler'ları unsubscribe et
                if (MainView.Instance != null)
                {
                    MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
                }
                
                // Diğer kaynakları temizle
                this.DataContext = null;
                
                // MainMovieTvShow kontrolünü bul ve DataContext'ini temizle
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow != null)
                {
                    mainMovieTvShow.DataContext = null;
                }
                
                // Collection'ları temizle
                Service.MovieCasts?.Clear();
                Service.TvShowCasts?.Clear();
                Service.Similars?.Clear();
                Service.VideoDetails?.Clear();
                
                // Clear static instance
                Instance = null;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
               Console.WriteLine(errorMessage);
            }
        }

        private bool unloaded = false;
        private async void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            unloaded = true;
            
            // Stop and dispose trailer to prevent background audio
            await CleanupVideoPlayer();
        }

        private void BtnCloseDialog_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs e)
        {
            DialogHost.IsOpen = false;
        }

        private async void BtnConfirm_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs e)
        {
            try
            {
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow == null) return;
                
                var btnSetRating = mainMovieTvShow.FindControl<Icon>("BtnSetRating");
                
                if (await Service.Vote(selectedMovie.Id, selectedMovie.ShowType, SetRating.Rating))
                {
                    DialogHost.IsOpen = false;
                    
                    if (btnSetRating != null)
                    {
                        btnSetRating.Foreground = new SolidColorBrush((Color)Application.Current.Resources["ColorDefault"]);
                        btnSetRating.Value = "fa-solid fa-star";
                    }
                    
                    rating = SetRating.Rating;
                    TextBlockDialogText.Text = ResourceProvider.GetString("YourVote") + " " + rating;
                    BtnConfirm.Content = ResourceProvider.GetString("Rerate");
                    BtnRemoveRating.IsVisible = true;
                    await NotificationService.Instance.ShowNotification(ResourceProvider.GetString("VoteNotify"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),false);
                    // Growl.Success(new GrowlInfo() { Message =ResourceProvider.GetString("VoteNotify"].ToString(), StaysOpen = false, WaitTime = 4 });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private async void BtnRemoveRating_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs e)
        {
            try
            {
                var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
                if (mainMovieTvShow == null) return;
                
                var btnSetRating = mainMovieTvShow.FindControl<Icon>("BtnSetRating");
                
                if (await Service.RemoveVote(selectedMovie.Id, selectedMovie.ShowType))
                {
                    DialogHost.IsOpen = false;
                    
                    if (btnSetRating != null)
                    {
                        btnSetRating.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                        btnSetRating.Value = "fa-light fa-star";
                    }
                    
                    rating = null;
                    TextBlockDialogText.Text = ResourceProvider.GetString("WhatIsYourRating").ToString();
                    BtnConfirm.Content = ResourceProvider.GetString("Confirm").ToString();
                    SetRating.Rating = 0;
                    BtnRemoveRating.IsVisible = false;
                    await NotificationService.Instance.ShowNotification(ResourceProvider.GetString("RemoveVoteNotify"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),false);

                   // Growl.Success(new GrowlInfo() { Message =ResourceProvider.GetString("RemoveVoteNotify").ToString(), StaysOpen = false, WaitTime = 4 });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void MenuTorrents_OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (selectedMovie.ShowType == ShowType.TvShow)
            {
                MenuTorrents.Text =ResourceProvider.GetString("EpisodesString").ToString();
            }
        }
    }

    public enum ClickedTabType
    {
        Overview,
        Video,
        Photo,
        Comment,
        Torrent
    }
} 