using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Google.Protobuf.Compiler;
using LibVLCSharp.Shared;
using Serilog;
using Newtonsoft.Json;
using NETCore.Encrypt;
using NetStream.Controls;
using NetStream;
using Path = System.IO.Path;
using NetStream.Services;

namespace NetStream.Views
{
    public partial class PlayerWindow : UserControl, IDisposable
    {
        public bool shouldListen = true;
        private LibVLC libVlc;
        public MediaPlayer mediaPlayer;
        private bool isFullScreen = false;
        private bool isFullScreenToggleInProgress = false;
        private int movieId;
        public string path;
        private ShowType showType;
        private int seasonNumber;
        private int episodeNumber;
        private string movieName;
        private FileInfo fileInfo;
        public static List<Subtitle> subtitles = new List<Subtitle>();
        private int imdbId;
        private Subtitle currentSubtitle;
        public static List<ThumbnailCache> thumbnailCaches = new List<ThumbnailCache>();
        private Item torrent;
        private int fileIndex;
        private string poster;
        private bool isCompleted;
        private bool _isVolumePopupOpen = false;
        private bool _isMouseOverVolumePopup = false;
        private bool _isMouseOverButtonMute = false;
        private System.Timers.Timer _volumePopupTimer;
        
        // Mouse inactivity timer for auto-hiding controls
        private System.Timers.Timer _mouseInactivityTimer;
        private DateTime _lastMouseMoveTime;
        private const int MOUSE_INACTIVITY_TIMEOUT = 2000; // 2 seconds
        
        // Torrent download tracking
        private DispatcherTimer _torrentProgressTimer;
        private double _torrentProgress = 0;
        
        const uint ES_CONTINUOUS = 0x80000000;
        const uint ES_DISPLAY_REQUIRED = 0x00000002;
        const uint ES_SYSTEM_REQUIRED = 0x00000001;
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SetThreadExecutionState([In] uint esFlags);
        
        private bool shouldCollapseControlPanel;
        private bool _disposed = false;
        private IDisposable _boundsSubscription;
        private Media _currentMedia; // Track media for disposal
        
        // Static cached icon bitmaps to prevent repeated allocation
        private static readonly Lazy<Bitmap> _playIcon = new Lazy<Bitmap>(() => new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/play_transparent_266x220.png"))));
        private static readonly Lazy<Bitmap> _pauseIcon = new Lazy<Bitmap>(() => new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/pause_transparent_266x220.png"))));
        private static readonly Lazy<Bitmap> _muteIcon = new Lazy<Bitmap>(() => new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/mute_transparent_266x220.png"))));
        private static readonly Lazy<Bitmap> _volume1Icon = new Lazy<Bitmap>(() => new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/volume1_transparent_266x220.png"))));
        private static readonly Lazy<Bitmap> _volume2Icon = new Lazy<Bitmap>(() => new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/volume2_transparent_266x220.png"))));
        private static readonly HashSet<string> SupportedSubtitleExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".srt",
            ".sub",
            ".ass",
            ".ssa",
            ".vtt"
        };
        private static readonly string[] GeneratedOwnSubtitleSuffixes =
        {
            "_SENKRON.srt",
            "_synced.srt",
            "_TURKCE_CEVIRI.srt"
        };
        private bool _isSubtitlePickerOpen;

        public PlayerWindow()
        {
            InitializeComponent();
            shouldCollapseControlPanel = true;
            _volumePopupTimer = new System.Timers.Timer(300);
            _volumePopupTimer.Elapsed += VolumePopupTimerOnElapsed;
            _volumePopupTimer.AutoReset = false;
        }
        
        private double CalculateValueFromLinear(double width)
        {
            const double x1 = 1607;
            const double y1 = -672;
            const double slope = -160.0 / 313.0; // ≈ -0.511

            double result = slope * (width - x1) + y1;
            return Math.Round(result);
        }

        
        private double CalculateScaledValue(double width, double minValue, double maxValue)
        {
            const double minWidth = 320;
            const double maxWidth = 3840;
            
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);
            
            return Math.Round(scaledValue);
        }

        public PlayerWindow(int movieId, string movieName, ShowType showType, int seasonNumber, int episodeNumber,
            FileInfo fileInfo, bool isCompleted, int imdbId, Item torrent, int fileIndex, string poster)
        {
            InitializeComponent();
            shouldCollapseControlPanel = true;
            ApplyResponsiveLayout(MainWindow.Instance.screenWidth);
            SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
            this.movieId = movieId;
            // Her zaman TorrentStream ile aç - direkt dosya oynatmada süre/thumbnail sorunları var
            isCompleted = false;

            MainWindow.Instance.SizeChanged += MainWindowOnSizeChanged;
            this.isCompleted = false;
            this.showType = showType;
            this.seasonNumber = seasonNumber;
            this.episodeNumber = episodeNumber;
            this.movieName = movieName;
            this.imdbId = imdbId;
            this.fileInfo = fileInfo;
            this.torrent = torrent;
            this.fileIndex = fileIndex;
            this.poster = poster;
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() => { if (MovieNameTextCenter != null) MovieNameTextCenter.Text = GetShowName(); });
            Console.WriteLine($"Loaded Player Window for: {GetShowName()}");
            GetThumbnailCaches();
            
            _volumePopupTimer = new System.Timers.Timer(300);
            _volumePopupTimer.Elapsed += VolumePopupTimerOnElapsed;
            _volumePopupTimer.AutoReset = false;
            
            if (showType == ShowType.Movie)
            {
                if (ButtonEpisodeList != null) ButtonEpisodeList.IsVisible = false;
                if (ButtonNextEpisode != null) ButtonNextEpisode.IsVisible = false;
            }
            
            // Initialize mouse inactivity timer
            _mouseInactivityTimer = new System.Timers.Timer(MOUSE_INACTIVITY_TIMEOUT);
            _mouseInactivityTimer.Elapsed += MouseInactivityTimerOnElapsed;
            _mouseInactivityTimer.AutoReset = false;
            _lastMouseMoveTime = DateTime.Now;
            
            // Initialize torrent progress timer (only if torrent is active)
            if (!isCompleted && torrent != null)
            {
                _torrentProgressTimer = new DispatcherTimer(DispatcherPriority.Background);
                _torrentProgressTimer.Interval = new TimeSpan(0, 0, 1); // Update every second
                _torrentProgressTimer.Tick += TorrentProgressTimerOnTick;
                _torrentProgressTimer.Start();
            }
            
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 750); // 500ms -> 750ms for better low-end PC performance
            dispatcherTimer.Tick += DispatcherTimerOnTick;
            dispatcherTimer.Start();
        }

        public void ApplyResponsiveLayout(double width)
        {
            var iconSize = CalculateScaledValue(width, 24,70);
            
            var iconSizeBack = CalculateScaledValue(width, 20,50);
            BackImage.FontSize = iconSizeBack;
            PlayButton.Width = iconSize; PlayButton.Height = iconSize;
            MuteButton.Width = iconSize; MuteButton.Height = iconSize;
            SubtitleIcon.Width = iconSize; SubtitleIcon.Height = iconSize;
            FullScreenIcon.Width = iconSize; FullScreenIcon.Height = iconSize;
            RewindIcon.Width = iconSize; RewindIcon.Height = iconSize;
            ForwardIcon.Width = iconSize; ForwardIcon.Height = iconSize;
            SpeedIcon.Width = iconSize; SpeedIcon.Height = iconSize;
            EpisodesIcon.Width = iconSize; EpisodesIcon.Height = iconSize;
            NextEpisodesIcon.Width = iconSize; NextEpisodesIcon.Height = iconSize;
            
            var textSize = CalculateScaledValue(width, 18,30);

            if(MovieNameTextCenter != null) MovieNameTextCenter.FontSize = textSize;
            VolumeText.FontSize =textSize;
            LoadingTextBlock.FontSize =textSize;
            TxtCurrentTime.FontSize = textSize;
            TxtDuration.FontSize =textSize;
            var episodeTransform = this.FindControl<Avalonia.Controls.LayoutTransformControl>("EpisodePopupTransform");
            if (episodeTransform != null)
            {
                var scaleFactor = width / 1920.0;
                if (scaleFactor < 0.6) scaleFactor = 0.6;
                if (scaleFactor > 1.5) scaleFactor = 1.5;
                episodeTransform.LayoutTransform = new Avalonia.Media.ScaleTransform(scaleFactor, scaleFactor);
            }
            
            var speedTransform = this.FindControl<Avalonia.Controls.LayoutTransformControl>("SpeedPopupTransform");
            if (speedTransform != null)
            {
                var scaleFactor = width / 1920.0;
                if (scaleFactor < 0.6) scaleFactor = 0.6;
                if (scaleFactor > 1.5) scaleFactor = 1.5;
                speedTransform.LayoutTransform = new Avalonia.Media.ScaleTransform(scaleFactor, scaleFactor);
            }
            
            var audioTransform = this.FindControl<Avalonia.Controls.LayoutTransformControl>("AudioSubtitlePopupTransform");
            if (audioTransform != null)
            {
                var scaleFactor = width / 1920.0;
                if (scaleFactor < 0.6) scaleFactor = 0.6;
                if (scaleFactor > 1.5) scaleFactor = 1.5;
                audioTransform.LayoutTransform = new Avalonia.Media.ScaleTransform(scaleFactor, scaleFactor);
            }
            
            var nextEpisodeTransform = this.FindControl<Avalonia.Controls.LayoutTransformControl>("NextEpisodePopupTransform");
            if (nextEpisodeTransform != null)
            {
                var scaleFactor = width / 1920.0;
                if (scaleFactor < 0.6) scaleFactor = 0.6;
                if (scaleFactor > 1.5) scaleFactor = 1.5;
                nextEpisodeTransform.LayoutTransform = new Avalonia.Media.ScaleTransform(scaleFactor, scaleFactor);
            }
            
            var subtitleShiftTransform = this.FindControl<Avalonia.Controls.LayoutTransformControl>("SubtitleShiftPopupTransform");
            if (subtitleShiftTransform != null)
            {
                var scaleFactor = width / 1920.0;
                if (scaleFactor < 0.6) scaleFactor = 0.6;
                if (scaleFactor > 1.5) scaleFactor = 1.5;
                subtitleShiftTransform.LayoutTransform = new Avalonia.Media.ScaleTransform(scaleFactor, scaleFactor);
            }
        }

        private void MainWindowOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.width);
        }

        private DownloadsFilesPage downloadsFilesPage = null;

        public PlayerWindow(int movieId, string movieName, ShowType showType, int seasonNumber, int episodeNumber,
            FileInfo fileInfo, bool isCompleted, int imdbId, Item torrent, int fileIndex, string poster,
            DownloadsFilesPage downloadsFilesPage)
        {
            InitializeComponent();
            shouldCollapseControlPanel = true;
            ApplyResponsiveLayout(MainWindow.Instance.screenWidth);
            SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
            this.movieId = movieId;
            // Her zaman TorrentStream ile aç
            isCompleted = false;
            MainWindow.Instance.SizeChanged += MainWindowOnSizeChanged;
            this.isCompleted = false;
            this.showType = showType;
            this.seasonNumber = seasonNumber;
            this.episodeNumber = episodeNumber;
            this.movieName = movieName;
            this.imdbId = imdbId;
            this.fileInfo = fileInfo;
            this.torrent = torrent;
            this.fileIndex = fileIndex;
            this.poster = poster;
            this.downloadsFilesPage = downloadsFilesPage;
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() => { if (MovieNameTextCenter != null) MovieNameTextCenter.Text = GetShowName(); });
            GetThumbnailCaches();
            Console.WriteLine($"Loaded Player Window for: {GetShowName()}");
            
            _volumePopupTimer = new System.Timers.Timer(300);
            _volumePopupTimer.Elapsed += VolumePopupTimerOnElapsed;
            _volumePopupTimer.AutoReset = false;

            if (showType == ShowType.Movie)
            {
                if (ButtonEpisodeList != null) ButtonEpisodeList.IsVisible = false;
                if (ButtonNextEpisode != null) ButtonNextEpisode.IsVisible = false;
            }

            // Initialize mouse inactivity timer
            _mouseInactivityTimer = new System.Timers.Timer(MOUSE_INACTIVITY_TIMEOUT);
            _mouseInactivityTimer.Elapsed += MouseInactivityTimerOnElapsed;
            _mouseInactivityTimer.AutoReset = false;
            _lastMouseMoveTime = DateTime.Now;
            
            // Initialize torrent progress timer (only if torrent is active)
            if (!isCompleted && torrent != null)
            {
                _torrentProgressTimer = new DispatcherTimer(DispatcherPriority.Background);
                _torrentProgressTimer.Interval = new TimeSpan(0, 0, 1); // Update every second
                _torrentProgressTimer.Tick += TorrentProgressTimerOnTick;
                _torrentProgressTimer.Start();
            }

            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 750); // 500ms -> 750ms for better low-end PC performance
            dispatcherTimer.Tick += DispatcherTimerOnTick;
            dispatcherTimer.Start();
            
        }

        private bool isInScaleProcess = false;
        private void DispatcherTimerOnTick(object? sender, EventArgs e)
        {
            // FIXAT: Sadece hover durumu değişti VE slider üzerinde değilse scale down yap
            bool isPointerOverSlider = DurationSlider?.IsPointerOver ?? false;
            if (isUp && !isPointerOverSlider && !isInScaleProcess)
            {
                isUp = false;
                rangeCanvas?.ScaleDown();
                DurationSlider.Classes.Remove("ScaledUp");
            }

            // Next Episode Logic
            if (Player?.MediaPlayer != null && Player.MediaPlayer.IsPlaying && duration > 0)
            {
                // Check if we are near the end (e.g., last 60 seconds or 98%)
                // Using 98% as a heuristic for credits if duration is long enough
                bool isNearEnd = false;
                if (duration > 300000) // > 5 minutes
                {
                    isNearEnd = Player.MediaPlayer.Position >= 0.98f;
                }
                else
                {
                    isNearEnd = Player.MediaPlayer.Position >= 0.95f;
                }

                if (isNearEnd && !NextEpisodeStackPanel.IsVisible)
                {
                     Dispatcher.UIThread.InvokeAsync(() =>
                     {
                         ProgressGrid.IsVisible = true;
                         LoadingTextBlock.IsVisible = false;
                         ProgressBarToPlay.IsVisible = false;
                         NextEpisodeStackPanel.IsVisible = true;
                     });
                }
                else if (!isNearEnd && NextEpisodeStackPanel.IsVisible)
                {
                     Dispatcher.UIThread.InvokeAsync(() =>
                     {
                         NextEpisodeStackPanel.IsVisible = false;
                         if (!LoadingTextBlock.IsVisible) // If not loading
                         {
                             ProgressGrid.IsVisible = false;
                         }
                     });
                }
            }
        }

        private DispatcherTimer dispatcherTimer;
        
        private Dictionary<int, Bitmap> thumbnailCache = new Dictionary<int, Bitmap>();
        private Queue<int> thumbnailHistory = new Queue<int>();
        private const int MaxCacheSize = 25; // Maksimum thumbnail sayısı
        
        private void ClearThumbnailCache()
        {
            try
            {
                foreach (var bitmap in thumbnailCache.Values)
                {
                    bitmap?.Dispose();
                }
                thumbnailCache.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Thumbnail cache temizlenirken hata: {ex.Message}");
            }
        }
        private CancellationTokenSource _thumbnailCts;
        private async void ShowThumbnailPreview(int currentSecond, Image ThumbnailPreviewImage)
        {
            try
            {
                if (fileInfo == null || string.IsNullOrWhiteSpace(fileInfo.FullName))
                    return;

                if (string.IsNullOrWhiteSpace(_thumbnailsFolder))
                {
                    string directoryPath = Path.GetDirectoryName(fileInfo.FullName);
                    _thumbnailsFolder = Path.Combine(directoryPath, "thumbnails", Path.GetFileNameWithoutExtension(fileInfo.Name));
                    if (!Directory.Exists(_thumbnailsFolder)) Directory.CreateDirectory(_thumbnailsFolder);
                }

                // Cache'de mevcut bir thumbnail var mı kontrol et
                if (!thumbnailCache.TryGetValue(currentSecond, out Bitmap bitmap))
                {
                    string thumbnailPath = Path.Combine(_thumbnailsFolder, $"thumb_{currentSecond}.jpg");

                    if (File.Exists(thumbnailPath))
                    {
                        bitmap = new Bitmap(thumbnailPath);
                        AddToCache(currentSecond, bitmap);
                    }
                }

                // Thumbnail'i göster
                if (bitmap != null)
                {
                    ThumbnailPreviewImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private async Task GenerateThumbnailsAsync(string videoPath, string outputFolder)
        {
            try
            {
                string ffmpegPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin", "ffmpeg.exe");
                // Yöntem 2 pre-generate hızlı extraction
                string arguments = $"-skip_frame nokey -i \"{videoPath}\" -vf \"fps=1/1,scale=160:90\" -preset ultrafast -threads 0 \"{outputFolder}\\thumb_%d.jpg\"";

                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                ffmpegProcess = new Process { StartInfo = processStartInfo };
                ffmpegProcess.Start();

                var tcs = new TaskCompletionSource<bool>();
                
                _thumbnailCts?.Cancel();
                _thumbnailCts = new CancellationTokenSource();

                using (_thumbnailCts.Token.Register(() => 
                {
                    tcs.TrySetCanceled();
                    try { if (!ffmpegProcess.HasExited) ffmpegProcess.Kill(); } catch { }
                }))
                {
                    _ = Task.Run(() => 
                    {
                        ffmpegProcess.WaitForExit();
                        tcs.TrySetResult(true);
                    });

                    await tcs.Task;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Thumbnail generation error: {ex.Message}");
            }
        }
        
        private void AddToCache(int key, Bitmap bitmap)
        {
            try
            {
                if (!thumbnailCache.ContainsKey(key))
                {
                    thumbnailHistory.Enqueue(key);
                }

                thumbnailCache[key] = bitmap;

                while (thumbnailHistory.Count > MaxCacheSize)
                {
                    var oldestKey = thumbnailHistory.Dequeue();
                    if (thumbnailCache.TryGetValue(oldestKey, out var oldBmp))
                    {
                        thumbnailCache.Remove(oldestKey);
                        
                        // Eğer şu an gösterilen resimse Dispose etme
                        if (ThumbnailPreviewImage != null && ThumbnailPreviewImage.Source == oldBmp)
                        {
                            // Atla, bunu silmeyeceğiz
                        }
                        else
                        {
                            oldBmp?.Dispose();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error AddToCache: {e.Message}");
            }
        }

        public static void GetThumbnailCaches()
        {
            try
            {
                var s = File.ReadAllText(AppSettingsManager.appSettings.ThumbnailCachesPath);
                if (!String.IsNullOrWhiteSpace(s))
                {
                    thumbnailCaches = JsonConvert.DeserializeObject<List<ThumbnailCache>>(s);
                    Console.WriteLine("Loaded thumbnail caches.");
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private string GetShowName()
        {
            if (showType == ShowType.TvShow)
            {
                return movieName + " S" + seasonNumber + "E" + episodeNumber;
            }

            return movieName;
        }


        public static async void GetSubtitles()
        {
            try
            {
                // Dosyayı asenkron olarak oku
                string s = File.ReadAllText(AppSettingsManager.appSettings.SubtitleInfoPath);

                if (!string.IsNullOrWhiteSpace(s))
                {
                    // Şifreyi çöz
                    string decryptedData = EncryptProvider.AESDecrypt(s, Encryptor.Key, Encryptor.IV);

                    // Decrypted veriyi deserialize et
                    if (!string.IsNullOrWhiteSpace(decryptedData))
                    {
                        subtitles = JsonConvert.DeserializeObject<List<Subtitle>>(decryptedData);
                        Console.WriteLine("Loaded subtitles.");
                    }
                    else
                    {
                        Console.WriteLine("Decrypted data is empty or invalid.");
                    }
                }
                else
                {
                    Console.WriteLine("Subtitle file is empty or invalid.");
                }
            }
            catch (FormatException fe)
            {
                Console.WriteLine("Error on Subtitle decryption: The input is not a valid Base-64 string. " + fe.Message);
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private WatchHistory currentPlayerCache;

        #region Event Handlers

        private void PlayerWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        // Mevcut kodun başına Focus çağrısı ekle
        this.Focus();
        
        // Thumbnail önizleme kontrollerini başlangıçta gizle
        if (ThumbnailPreviewPopup != null)
        {
            ThumbnailPreviewPopup.IsOpen = false;
        }

        Avalonia.Input.DragDrop.SetAllowDrop(this, true);
        this.AddHandler(Avalonia.Input.DragDrop.DropEvent, Drop);
        
        try
        {
            Log.Information("PlayerWindow_OnLoaded: Initializing LibVLC and MediaPlayer");
            libVlc = new LibVLC();
            mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(libVlc);
            Player.Loaded += PlayerOnLoaded;
            Log.Information("PlayerWindow_OnLoaded: Initialization successful");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PlayerWindow_OnLoaded: Error initializing video player");
        }
    }

    private async void Drop(object? sender, Avalonia.Input.DragEventArgs e)
    {
        if (e.Data.Contains(Avalonia.Input.DataFormats.Files))
        {
            var files = e.Data.GetFiles()?.ToList();
            if (files != null && files.Count > 0)
            {
                var file = files.FirstOrDefault();
                if (file != null && IsSupportedSubtitleFile(file.Path.LocalPath))
                {
                    await LoadExternalSubtitleAsync(file.Path.LocalPath);
                }
            }
        }
    }

    private async void AddManualSubtitle_Click(object? sender, RoutedEventArgs e)
    {
        AudioSubtitlePopup.IsOpen = false;

        string result = await PickSubtitleFileAsync("Altyazi Sec");
        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        await LoadExternalSubtitleAsync(result);
    }

    private static bool IsSupportedSubtitleFile(string subtitlePath)
    {
        if (string.IsNullOrWhiteSpace(subtitlePath))
        {
            return false;
        }

        string extension = Path.GetExtension(subtitlePath);
        return !string.IsNullOrWhiteSpace(extension) && SupportedSubtitleExtensions.Contains(extension);
    }

    private void SaveSubtitleRegistry()
    {
        try
        {
            File.WriteAllText(
                AppSettingsManager.appSettings.SubtitleInfoPath,
                EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(subtitles), Encryptor.Key, Encryptor.IV));
        }
        catch
        {
        }
    }

    private static bool IsDisabledSubtitleLanguage(string? language)
    {
        return string.IsNullOrWhiteSpace(language) ||
               string.Equals(language, "Disabled", StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateSubtitleLanguagePreference(string? isoLanguage)
    {
        if (string.IsNullOrWhiteSpace(isoLanguage))
        {
            return;
        }

        if (string.Equals(isoLanguage, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            AppSettingsManager.appSettings.SubtitleLanguage = "Disabled";
            AppSettingsManager.appSettings.IsoSubtitleLanguage = "Disabled";
            AppSettingsManager.SaveAppSettings();
            return;
        }

        AppSettingsManager.appSettings.IsoSubtitleLanguage = isoLanguage;
        AppSettingsManager.appSettings.SubtitleLanguage =
            Service.Languages.FirstOrDefault(x =>
                string.Equals(x.Iso_639_1, isoLanguage, StringComparison.OrdinalIgnoreCase))?.EnglishName
            ?? isoLanguage;
        AppSettingsManager.SaveAppSettings();
    }

    private bool IsCurrentSubtitle(Subtitle? subtitle)
    {
        if (subtitle == null || currentSubtitle == null)
        {
            return false;
        }

        bool currentIsOwnSubtitle = IsOwnSubtitleEntry(currentSubtitle);
        bool comparedSubtitleIsOwn = IsOwnSubtitleEntry(subtitle);
        if (currentIsOwnSubtitle || comparedSubtitleIsOwn)
        {
            return SubtitlePathsMatch(currentSubtitle, subtitle);
        }

        if (currentSubtitle.SubtitleId.HasValue &&
            subtitle.SubtitleId.HasValue &&
            currentSubtitle.SubtitleId.Value == subtitle.SubtitleId.Value)
        {
            return true;
        }

        return SubtitlePathsMatch(currentSubtitle, subtitle);
    }

    private static bool IsOwnSubtitleEntry(Subtitle? subtitle)
    {
        if (subtitle == null)
        {
            return false;
        }

        if (subtitle.CustomIsUserAdded)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(subtitle.Fullpath))
        {
            return false;
        }

        return GeneratedOwnSubtitleSuffixes.Any(suffix =>
            subtitle.Fullpath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool SubtitlePathsMatch(Subtitle? firstSubtitle, Subtitle? secondSubtitle)
    {
        if (firstSubtitle == null ||
            secondSubtitle == null ||
            string.IsNullOrWhiteSpace(firstSubtitle.Fullpath) ||
            string.IsNullOrWhiteSpace(secondSubtitle.Fullpath))
        {
            return false;
        }

        return string.Equals(
            GetNormalizedSubtitlePath(firstSubtitle.Fullpath),
            GetNormalizedSubtitlePath(secondSubtitle.Fullpath),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetNormalizedSubtitlePath(string subtitlePath)
    {
        try
        {
            return Path.GetFullPath(subtitlePath);
        }
        catch
        {
            return subtitlePath;
        }
    }

    private async Task<string?> PickSubtitleFileAsync(string title)
    {
        if (_isSubtitlePickerOpen)
        {
            return null;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null)
        {
            return null;
        }

        _isSubtitlePickerOpen = true;
        bool previousShouldListen = shouldListen;
        bool previousShouldListenWindowEvents = shouldListenWindowEvents;

        try
        {
            shouldListen = false;
            shouldListenWindowEvents = false;

            await Task.Yield();

            IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Subtitle Files")
                    {
                        Patterns = new[] { "*.srt", "*.sub", "*.ass", "*.ssa", "*.vtt" },
                        MimeTypes = new[] { "text/plain", "application/x-subrip", "text/vtt" }
                    }
                }
            });

            return files != null && files.Count > 0 ? files[0].Path.LocalPath : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Subtitle picker error: {ex.Message}");
            return null;
        }
        finally
        {
            shouldListen = previousShouldListen;
            shouldListenWindowEvents = previousShouldListenWindowEvents;
            _isSubtitlePickerOpen = false;
        }
    }

    private async Task LoadExternalSubtitleAsync(string subtitlePath)
    {
        if (string.IsNullOrWhiteSpace(subtitlePath))
        {
            return;
        }

        string finalPath = Path.GetFullPath(subtitlePath);
        if (!File.Exists(finalPath) || !IsSupportedSubtitleFile(finalPath))
        {
            return;
        }

        string language = AppSettingsManager.appSettings.IsoSubtitleLanguage;
        if (string.IsNullOrWhiteSpace(language) || string.Equals(language, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            language = currentSubtitle?.Language ?? "und";
        }

        var existingSubtitle = subtitles.LastOrDefault(x =>
            string.Equals(x.Fullpath, finalPath, StringComparison.OrdinalIgnoreCase) &&
            x.MovieId == movieId &&
            x.EpisodeNumber == episodeNumber &&
            x.SeasonNumber == seasonNumber);

        if (existingSubtitle == null)
        {
            existingSubtitle = new Subtitle
            {
                Fullpath = finalPath,
                Name = Path.GetFileName(finalPath),
                Language = language,
                Synchronized = false,
                HashDownload = true,
                MovieId = movieId,
                EpisodeNumber = episodeNumber,
                SeasonNumber = seasonNumber,
                CustomIsUserAdded = true
            };
            subtitles.Add(existingSubtitle);
        }
        else
        {
            existingSubtitle.Name = Path.GetFileName(finalPath);
            existingSubtitle.Fullpath = finalPath;
            existingSubtitle.Language = language;
            existingSubtitle.CustomIsUserAdded = true;
        }

        currentSubtitle = existingSubtitle;
        SaveSubtitleRegistry();
        AudioSubtitlePopup.IsOpen = false;

        if (mediaPlayer != null)
        {
            subtitleClickedTime = mediaPlayer.Position;
            mediaPlayer.Stop();
        }

        success = false;
        await Task.Delay(100);
        PlayerOnLoaded(this, new RoutedEventArgs());
    }

        public bool success = false;
        private TorrentStream torrentStream;
        public bool closed = false;

        public static async Task<long> GetVideoDurationMillisecondsAsync(string videoPath)
        {
            // ffprobe komut satırı argümanları:
            // -v error -> Sadece hata mesajlarını gösterir.
            // -show_entries format=duration -> Sadece duration bilgisini döndürür.
            // -of default=noprint_wrappers=1:nokey=1 -> Çıktıda anahtar isimlerini bastırır, yalnızca değeri verir.
            try
            {
                string arguments =
                    $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"";
                string ffmpegPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin", "ffprobe.exe");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath, // ffprobe.exe'nin tam yolu
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false, // Konsol penceresi açılmasın diye
                    CreateNoWindow = true // Konsol penceresi açılmasın diye
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();

                    // Asenkron olarak çıktıyı oku
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string errorOutput = await process.StandardError.ReadToEndAsync();

                    // Process'in tamamlanmasını bekle (örn. .NET 5+ ile WaitForExitAsync kullanılabilir)
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrWhiteSpace(errorOutput))
                    {
                        Console.WriteLine("ffprobe hatası: " + errorOutput);
                        return 0;
                    }

                    // ffprobe genellikle süre bilgisini saniye cinsinden ondalıklı bir sayı olarak verir.
                    // Bu değeri milisaniyeye çevirmek için 1000 ile çarpıyoruz.
                    if (double.TryParse(output, NumberStyles.Float, CultureInfo.InvariantCulture,
                            out double durationSeconds))
                    {
                        long durationMilliseconds = (long)(durationSeconds * 1000);
                        return durationMilliseconds;
                    }
                    else
                    {
                        Console.WriteLine("Video süresi parse edilemedi. Çıktı: " + output);
                        return 0;
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Console.WriteLine(errorMessage);
                return 0;
            }
        }

        private float? subtitleClickedTime = null;

        public async void PlayerOnLoaded(object? sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                ProgressGrid.IsVisible = true;
                LoadingTextBlock.IsVisible = true;
                ProgressBarToPlay.IsIndeterminate = true;
                ProgressBarToPlay.IsVisible = true;
                NextEpisodeStackPanel.IsVisible = false;
                PanelControlVideo.IsVisible = false;
            }

            Player.MediaPlayer = mediaPlayer;


            double volumeValue;
            double.TryParse(await File.ReadAllTextAsync(AppSettingsManager.appSettings.VolumeCachePath),
                out volumeValue);
            VolumeSlider.Value = volumeValue;

            string subtitlePath = await GetSubtitle();

            // Dispose old TorrentStream before creating a new one to avoid file lock conflicts
            if (torrentStream != null)
            {
                try
                {
                    torrentStream.Dispose();
                    torrentStream = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing old TorrentStream: {ex.Message}");
                }
            }

            // Give VLC time to release resources after Stop()
            await Task.Delay(200);

            while (!success)
            {
                try
                {
                    // Completed torrents: play directly from disk file (no TorrentStream needed)
                    if (isCompleted && !String.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        Console.WriteLine($"Playing completed torrent directly from disk: {path}");
                        if (closed) break;

                        // Delete subtitle files in the video's folder so VLC doesn't auto-load them
                        try
                        {
                            var videoDir = Path.GetDirectoryName(path);
                            string selectedSubtitlePath = string.IsNullOrWhiteSpace(subtitlePath)
                                ? string.Empty
                                : Path.GetFullPath(subtitlePath);
                            if (!string.IsNullOrEmpty(videoDir))
                            {
                                var subtitleExtensions = new[] { "*.srt", "*.sub", "*.ass", "*.ssa", "*.vtt", "*.idx" };
                                foreach (var ext in subtitleExtensions)
                                {
                                    foreach (var subFile in Directory.GetFiles(videoDir, ext))
                                    {
                                        try
                                        {
                                            if (!string.IsNullOrWhiteSpace(selectedSubtitlePath) &&
                                                string.Equals(Path.GetFullPath(subFile), selectedSubtitlePath, StringComparison.OrdinalIgnoreCase))
                                            {
                                                continue;
                                            }

                                            File.Delete(subFile);
                                            Console.WriteLine($"Deleted subtitle file from video folder: {subFile}");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Could not delete subtitle file {subFile}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error cleaning subtitle files: {ex.Message}");
                        }

                        Media media;
                        if (!String.IsNullOrWhiteSpace(subtitlePath))
                        {
                            media = new Media(libVlc, path, FromType.FromPath,
                                new[] { "sub-file=" + subtitlePath, "no-sub-autodetect-file" });
                            Console.WriteLine("Playing completed file with subtitle: " + subtitlePath);
                        }
                        else
                        {
                            media = new Media(libVlc, path, FromType.FromPath,
                                new[] { "no-sub-autodetect-file" });
                            Console.WriteLine("Playing completed file without subtitles");
                        }

                        await media.Parse(MediaParseOptions.ParseNetwork | MediaParseOptions.FetchLocal);
                        media.ParsedChanged += async (sender, e) =>
                        {
                            if (e.ParsedStatus == MediaParsedStatus.Done)
                            {
                                long durationMilliseconds = media.Duration;
                                if (durationMilliseconds > 0)
                                {
                                    UpdateDuration(durationMilliseconds);
                                }
                                else
                                {
                                    var duration = await GetVideoDurationMillisecondsAsync(fileInfo.FullName);
                                    if (duration > 0)
                                    {
                                        UpdateDuration(duration);
                                    }
                                }
                            }
                        };
                        Player.MediaPlayer.Play(media);
                    }
                    else if (String.IsNullOrWhiteSpace(subtitlePath))
                    {
                        Console.WriteLine("Subtitle couldnt be found");
                        if (closed) break;
                        torrentStream = await TorrentStream.Create(torrent, fileIndex);
                        var media = new Media(libVlc,
                            new StreamMediaInput(torrentStream));

                        await media.Parse(MediaParseOptions.ParseNetwork | MediaParseOptions.FetchLocal);
                        media.ParsedChanged += async (sender, e) =>
                        {
                            if (e.ParsedStatus == MediaParsedStatus.Done)
                            {
                                long durationMilliseconds = media.Duration;
                                if (durationMilliseconds > 0)
                                {
                                    UpdateDuration(durationMilliseconds);
                                }
                                else
                                {
                                    var duration = await GetVideoDurationMillisecondsAsync(fileInfo.FullName);
                                    if (duration > 0)
                                    {
                                        UpdateDuration(duration);
                                    }
                                }
                            }
                        };
                        Player.MediaPlayer.Play(media);
                        Console.WriteLine("Playing media file without subtitles.");
                    }
                    else
                    {
                        if (closed) break;
                        torrentStream = await TorrentStream.Create(torrent, fileIndex);
                        var media = new Media(libVlc,
                            new StreamMediaInput(torrentStream),
                            new[] { "sub-file=" + subtitlePath });

                        await media.Parse(MediaParseOptions.ParseNetwork | MediaParseOptions.FetchLocal);
                        media.ParsedChanged += async (sender, e) =>
                        {
                            if (e.ParsedStatus == MediaParsedStatus.Done)
                            {
                                long durationMilliseconds = media.Duration;
                                if (durationMilliseconds > 0)
                                {
                                    UpdateDuration(durationMilliseconds);
                                }
                                else
                                {
                                    var duration = await GetVideoDurationMillisecondsAsync(fileInfo.FullName);
                                    if (duration > 0)
                                    {
                                        UpdateDuration(duration);
                                    }
                                }
                            }
                        };
                        Player.MediaPlayer.Play(media);
                        Console.WriteLine("Playing media file with subtitle");
                    }

                    if (closed) break;
                    Player.MediaPlayer.EnableKeyInput = false;
                    Player.MediaPlayer.EnableMouseInput = false;

                    if (closed) break;
                    // if (currentPlayerCache != null)
                    // {
                    //     Player.MediaPlayer.Position = (float) currentPlayerCache.Progress;
                    // }
                     if (subtitleClickedTime.HasValue)
                    {
                        Player.MediaPlayer.Position = subtitleClickedTime.Value;
                        subtitleClickedTime = null;
                    }
                    else
                    {
                        var watchHistoryResult =
                            await FirestoreManager.FindWatchHistory(movieId, showType, seasonNumber, episodeNumber);
                        if (watchHistoryResult != null && watchHistoryResult.Success &&
                            watchHistoryResult.WatchHistory != null)
                        {
                            currentPlayerCache = watchHistoryResult.WatchHistory;

                            Player.MediaPlayer.Position =
                                currentPlayerCache.DeletedTorrent || currentPlayerCache.Progress >= 1
                                    ? 0
                                    : (float)currentPlayerCache.Progress;

                            if (currentPlayerCache.DeletedTorrent)
                            {
                                await FirestoreManager.EditWatchHistory(new EditWatchHistoryRequest
                                {
                                    Email = AppSettingsManager.appSettings.FireStoreEmail,
                                    Id = movieId,
                                    ShowType = showType,
                                    SeasonNumber = seasonNumber,
                                    EpisodeNumber = episodeNumber,
                                    NewProgress = currentPlayerCache.Progress,
                                    DeletedTorrent = false,
                                    Hash = torrent.Hash
                                });
                                currentPlayerCache.DeletedTorrent = false;
                            }

                        }

                    }

                    if (closed) break;
                    Player.MediaPlayer.LengthChanged += MediaPlayerOnLengthChanged;
                    Player.MediaPlayer.TimeChanged += MediaPlayerOnTimeChanged;
                    Player.MediaPlayer.Volume = Convert.ToInt32(VolumeSlider.Value);
                    Player.MediaPlayer.PositionChanged += MediaPlayerOnPositionChanged;
                    Player.MediaPlayer.Stopped += MediaPlayerOnStopped;
                    Player.MediaPlayer.Buffering += MediaPlayerOnBuffering;
                    Player.MediaPlayer.EndReached += MediaPlayerOnEndReached;
                    Player.MediaPlayer.Playing += MediaPlayer_Playing;
                    Player.MediaPlayer.Paused += MediaPlayerOnPaused;

                    if (closed) break;
                    if (String.IsNullOrWhiteSpace(path) &&
                        await Libtorrent.GetTorrentState(torrent.Hash) != TorrentState.Downloading)
                    {
                        await Libtorrent.Resume(torrent.Hash);
                    }

                    if (closed) break;
                    success = true;
                    if (closed) break;
                    if (AppSettingsManager.appSettings.PlayerSettingShowThumbnail)
                    {
                        _ = Task.Run((async () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    var torrentFiles = await Libtorrent.GetFiles(torrent.Hash);
                                    if (torrentFiles != null && torrentFiles.Count > 0)
                                    {
                                        var currentFile = torrentFiles.FirstOrDefault(x => x.Index == fileIndex);
                                        if (currentFile != null)
                                        {
                                            isCompleted = currentFile.IsCompleted;

                                            if (isCompleted && !startedGetThumbnails)
                                            {
                                                startedGetThumbnails = true;

                                                string fullPath = fileInfo.FullName;
                                                string directoryPath = Path.GetDirectoryName(fullPath);

                                                _thumbnailsFolder = Path.Combine(directoryPath, "thumbnails",
                                                    Path.GetFileNameWithoutExtension(currentFile.Name));

                                                AddNewThumbnailCache(_thumbnailsFolder);

                                                if (!Directory.Exists(_thumbnailsFolder))
                                                    Directory.CreateDirectory(_thumbnailsFolder);
                                                
                                                _ = GenerateThumbnailsAsync(fullPath, _thumbnailsFolder);

                                                break;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                    await Task.Delay(1000);
                                }
                                catch (Exception ex)
                                {
                                    var errorMessage =
                                        $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                                    Console.WriteLine(errorMessage);
                                }
                            }
                        }));
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage =
                        $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                    Console.WriteLine(errorMessage);
                    if (ex.Message.ToLower().Contains("the process cannot access the file"))
                    {
                        success = false;
                        await Task.Delay(500); // Wait before retrying to avoid tight loop
                        if (String.IsNullOrWhiteSpace(path) &&
                            await Libtorrent.GetTorrentState(torrent.Hash) == TorrentState.Downloading)
                        {
                            await Libtorrent.Pause(torrent.Hash);
                        }
                    }
                    else
                    {
                        success = true;
                    }
                }
            }
        }

        private bool startedGetThumbnails = false;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private string _thumbnailsFolder = "";
        


        private async void AddNewThumbnailCache(string folderPath)
        {
            try
            {
                if (!thumbnailCaches.Any(x => x.FolderPath == folderPath))
                {
                    thumbnailCaches.Add(new ThumbnailCache() { Hash = torrent.Hash, FolderPath = folderPath });
                    var js = JsonConvert.SerializeObject(thumbnailCaches);
                    await File.WriteAllTextAsync(AppSettingsManager.appSettings.ThumbnailCachesPath, js);
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private float seconds10 = 0;

        private long twentyMinutesInMilliseconds;
        private long duration;
        private double durationInSeconds;
        private Process ffmpegProcess;

        private void UpdateDuration(long length)
        {
            // UI thread üzerinde güncelleme yapılıyor.
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    // Süreyi TimeSpan'a çevirip, hh:mm:ss formatında görüntüle (ilk 8 karakter)
                    TxtDuration.Text = TimeSpan.FromMilliseconds(length).ToString().Substring(0, 8);
                    if (string.IsNullOrWhiteSpace(path) && ProgressGrid.IsVisible == true)
                    {
                        ProgressGrid.IsVisible = false;
                    }
                });

                // Örneğin, global değişkenler güncelleniyor:
                seconds10 = (float)10000 / length;
                duration = length;
                durationInSeconds = length / 1000;
                twentyMinutesInMilliseconds = 20 * 60 * 1000;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private async Task<string> GetSubtitle()
        {
            string selectedLanguage = AppSettingsManager.appSettings.IsoSubtitleLanguage;
            if (IsDisabledSubtitleLanguage(selectedLanguage))
            {
                currentSubtitle = null;
                return string.Empty;
            }

            if (currentSubtitle != null &&
                !string.IsNullOrWhiteSpace(currentSubtitle.Fullpath) &&
                File.Exists(currentSubtitle.Fullpath) &&
                currentSubtitle.MovieId == movieId &&
                currentSubtitle.EpisodeNumber == episodeNumber &&
                currentSubtitle.SeasonNumber == seasonNumber &&
                string.Equals(currentSubtitle.Language, selectedLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return currentSubtitle.Fullpath;
            }

            GetSubtitles();
            try
            {
                if (subtitles.Any(x =>
                        x.MovieId == movieId && x.EpisodeNumber == episodeNumber &&
                        x.SeasonNumber == this.seasonNumber && !String.IsNullOrWhiteSpace(x.Fullpath)
                        && x.Language == selectedLanguage))
                {
                    var subtitle = subtitles.LastOrDefault(x =>
                        x.MovieId == movieId && x.EpisodeNumber == episodeNumber &&
                        x.SeasonNumber == this.seasonNumber && !String.IsNullOrWhiteSpace(x.Fullpath) &&
                        x.Language == selectedLanguage);

                    currentSubtitle = subtitle;
                    return subtitle.Fullpath;
                   
                }
                else
                {
                    Console.WriteLine("Subtitle hasnt been found.");
                    if (String.IsNullOrWhiteSpace(path))
                    {
                        Console.WriteLine("Started Search subtitle for Not completed video file");
                        if (showType == ShowType.Movie)
                        {
                            Subtitle subtitle = await SubtitleHandler.GetSubtitlesByTMDbId(movieName, movieId,
                                new List<string>() { AppSettingsManager.appSettings.IsoSubtitleLanguage });
                    
                            if (subtitle != null && !String.IsNullOrWhiteSpace(subtitle.Fullpath))
                            {
                                Console.WriteLine("[OpenSubtitles.com] Movie subtitle found. Subtitle path: " +
                                                subtitle.Fullpath);
                                subtitle.MovieId = movieId;
                                subtitle.EpisodeNumber = episodeNumber;
                                subtitle.SeasonNumber = seasonNumber;
                                subtitle.Name = Path.GetFileName(subtitle.Fullpath);
                                subtitles.Add(subtitle);
                                File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                                    EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(subtitles),
                                        Encryptor.Key, Encryptor.IV));
                                currentSubtitle = subtitle;
                                return subtitle.Fullpath;
                            }
                            else
                            {
                                var mov = await Service.client.GetMovieAsync(movieId);
                                int year = mov.ReleaseDate.HasValue ? mov.ReleaseDate.Value.Year : 0;
                    
                                var newSubtitle = await SubtitleHandler.SelectAndDownloadSubtitle(showType, movieName,
                                    movieId,
                                    year, seasonNumber, episodeNumber,
                                    AppSettingsManager.appSettings.IsoSubtitleLanguage,
                                    "tt" + imdbId);
                    
                                if (newSubtitle != null && !String.IsNullOrWhiteSpace(newSubtitle.Fullpath))
                                {
                                    Console.WriteLine("[OpenSubtitles.Org] Movie subtitle found. Subtitle path: " +
                                                    newSubtitle.Fullpath);
                                    newSubtitle.MovieId = movieId;
                                    newSubtitle.EpisodeNumber = episodeNumber;
                                    newSubtitle.SeasonNumber = seasonNumber;
                                    newSubtitle.Name = Path.GetFileName(newSubtitle.Fullpath);
                                    subtitles.Add(newSubtitle);
                                    File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                                        EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(subtitles),
                                            Encryptor.Key, Encryptor.IV));
                                    currentSubtitle = newSubtitle;
                                    return newSubtitle.Fullpath;
                                }
                            }
                        }
                        else
                        {
                            Subtitle subtitle = await SubtitleHandler.GetSubtitlesByNameAndEpisode(movieName,
                                seasonNumber,
                                episodeNumber,
                                new List<string>() { AppSettingsManager.appSettings.IsoSubtitleLanguage },
                                imdbId);
                            if (subtitle != null && !String.IsNullOrWhiteSpace(subtitle.Fullpath))
                            {
                                Console.WriteLine("TvShow subtitle found. Subtitle path: " + subtitle.Fullpath);
                                subtitle.MovieId = movieId;
                                subtitle.EpisodeNumber = episodeNumber;
                                subtitle.SeasonNumber = seasonNumber;
                                subtitle.Name = Path.GetFileName(subtitle.Fullpath);
                                subtitles.Add(subtitle);
                                File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                                    EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(subtitles),
                                        Encryptor.Key, Encryptor.IV));
                                currentSubtitle = subtitle;
                                return subtitle.Fullpath;
                            }
                            else
                            {
                                var mov = await Service.client.GetTvShowAsync(movieId);
                                int year = mov.FirstAirDate.Value.Year;
                    
                                var newSubtitle = await SubtitleHandler.SelectAndDownloadSubtitle(showType, movieName,
                                    movieId,
                                    year, seasonNumber, episodeNumber,
                                    AppSettingsManager.appSettings.IsoSubtitleLanguage,
                                    "tt" + imdbId);
                    
                    
                                if (newSubtitle != null && !String.IsNullOrWhiteSpace(newSubtitle.Fullpath))
                                {
                                    Console.WriteLine("TvShow subtitle found. Subtitle path: " + newSubtitle.Fullpath);
                                    newSubtitle.MovieId = movieId;
                                    newSubtitle.EpisodeNumber = episodeNumber;
                                    newSubtitle.SeasonNumber = seasonNumber;
                                    newSubtitle.Name = Path.GetFileName(newSubtitle.Fullpath);
                                    subtitles.Add(newSubtitle);
                                    File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                                        EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(subtitles),
                                            Encryptor.Key, Encryptor.IV));
                                    currentSubtitle = newSubtitle;
                                    return newSubtitle.Fullpath;
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Started Search subtitle for completed video file");
                        Subtitle subtitle = await SubtitleHandler.GetSubtitlesByHash(path,
                            new List<string>() { AppSettingsManager.appSettings.IsoSubtitleLanguage }, movieName);
                    
                        if (subtitle != null && !String.IsNullOrWhiteSpace(subtitle.Fullpath))
                        {
                            Console.WriteLine("Subtitle has been found. Subtitle path: " + subtitle.Fullpath);
                            subtitle.MovieId = movieId;
                            subtitle.EpisodeNumber = episodeNumber;
                            subtitle.SeasonNumber = seasonNumber;
                            subtitle.Name = Path.GetFileName(subtitle.Fullpath);
                            subtitle.HashDownload = true;
                    
                            subtitles.Add(subtitle);
                            File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                                EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(subtitles),
                                    Encryptor.Key, Encryptor.IV));
                    
                            currentSubtitle = subtitle;
                            return subtitle.Fullpath;
                        }
                        else
                        {
                            if (showType == ShowType.Movie)
                            {
                                var newSubtitle = await SubtitleHandler.GetSubtitlesByTMDbId(movieName, movieId,
                                    new List<string>() { AppSettingsManager.appSettings.IsoSubtitleLanguage });
                    
                                if (newSubtitle != null && !String.IsNullOrWhiteSpace(newSubtitle.Fullpath))
                                {
                                    Console.WriteLine("Movie subtitle found. Subtitle path: " + newSubtitle.Fullpath);
                                    newSubtitle.MovieId = movieId;
                                    newSubtitle.EpisodeNumber = episodeNumber;
                                    newSubtitle.SeasonNumber = seasonNumber;
                                    newSubtitle.Name = Path.GetFileName(newSubtitle.Fullpath);
                                    
                    
                                    subtitles.Add(newSubtitle);
                                    File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                                        EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(subtitles),
                                            Encryptor.Key, Encryptor.IV));
                    
                                    currentSubtitle = newSubtitle;
                                    return newSubtitle.Fullpath;
                                }
                                else
                                {
                                    var mov = await Service.client.GetMovieAsync(movieId);
                                    int year = mov.ReleaseDate.HasValue ? mov.ReleaseDate.Value.Year : 0;
                    
                                    var newSubtitle2 = await SubtitleHandler.SelectAndDownloadSubtitle(showType,
                                        movieName, movieId,
                                        year, seasonNumber, episodeNumber,
                                        AppSettingsManager.appSettings.IsoSubtitleLanguage,
                                        "tt" + imdbId);
                    
                                    if (newSubtitle2 != null && !String.IsNullOrWhiteSpace(newSubtitle2.Fullpath))
                                    {
                                        Console.WriteLine("Movie subtitle found. Subtitle path: " +
                                                        newSubtitle2.Fullpath);
                                        newSubtitle2.MovieId = movieId;
                                        newSubtitle2.EpisodeNumber = episodeNumber;
                                        newSubtitle2.SeasonNumber = seasonNumber;
                                        newSubtitle2.Name = Path.GetFileName(newSubtitle2.Fullpath);
                    
                                        subtitles.Add(newSubtitle2);
                                        File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                                            EncryptProvider.AESEncrypt(
                                                JsonConvert.SerializeObject(subtitles), Encryptor.Key,
                                                Encryptor.IV));
                    
                                        currentSubtitle = newSubtitle2;
                                        return newSubtitle2.Fullpath;
                                    }
                                }
                            }
                            else
                            {
                                var newsubtitle3 = await SubtitleHandler.GetSubtitlesByNameAndEpisode(movieName,
                                    seasonNumber,
                                    episodeNumber,
                                    new List<string>() { AppSettingsManager.appSettings.IsoSubtitleLanguage },
                                    imdbId);
                                if (newsubtitle3 != null && !String.IsNullOrWhiteSpace(newsubtitle3.Fullpath))
                                {
                                    Console.WriteLine("TvShow subtitle found. Subtitle path: " + newsubtitle3.Fullpath);
                                    newsubtitle3.MovieId = movieId;
                                    newsubtitle3.EpisodeNumber = episodeNumber;
                                    newsubtitle3.SeasonNumber = seasonNumber;
                                    newsubtitle3.Name = Path.GetFileName(newsubtitle3.Fullpath);
                    
                                    subtitles.Add(newsubtitle3);
                                    File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                                        EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(subtitles),
                                            Encryptor.Key, Encryptor.IV));
                    
                                    currentSubtitle = newsubtitle3;
                                    return newsubtitle3.Fullpath;
                                }
                                else
                                {
                                    var mov = await Service.client.GetTvShowAsync(movieId);
                                    int year = mov.FirstAirDate.Value.Year;
                    
                                    var newsubtitle4 = await SubtitleHandler.SelectAndDownloadSubtitle(showType,
                                        movieName, movieId,
                                        year, seasonNumber, episodeNumber,
                                        AppSettingsManager.appSettings.IsoSubtitleLanguage,
                                        "tt" + imdbId);
                    
                    
                                    if (newsubtitle4 != null && !String.IsNullOrWhiteSpace(newsubtitle4.Fullpath))
                                    {
                                        Console.WriteLine(
                                            "TvShow subtitle found. Subtitle path: " + newsubtitle4.Fullpath);
                                        newsubtitle4.MovieId = movieId;
                                        newsubtitle4.EpisodeNumber = episodeNumber;
                                        newsubtitle4.SeasonNumber = seasonNumber;
                                        newsubtitle4.Name = Path.GetFileName(newsubtitle4.Fullpath);
                                        
                                        subtitles.Add(newsubtitle4);
                                        File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                                            EncryptProvider.AESEncrypt(
                                                JsonConvert.SerializeObject(subtitles), Encryptor.Key,
                                                Encryptor.IV));
                                        currentSubtitle = newsubtitle4;
                                        return newsubtitle4.Fullpath;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }

            return "";
        }

        private async void Player_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!shouldListen) return;
            if (_isMouseMoveHandled || isFullScreenToggleInProgress)
            {
                return; // Tam ekran geçişi sırasında mouse hareket olaylarını işleme
            }

            _isMouseMoveHandled = true;
            try
            {
                // Show controls and cursor
                ButtonBack.IsVisible = true;

                PanelControlVideo.IsVisible = true;
                this.Cursor = Cursor.Default;
                
                // FIXAT: Torrent aktifse progress panelini de göster
                if (!isCompleted && TorrentProgressPanel != null && torrent != null)
                {
                    TorrentProgressPanel.IsVisible = true;
                }
                
                // Update last mouse move time
                _lastMouseMoveTime = DateTime.Now;
                
                // Restart the mouse inactivity timer
                if (_mouseInactivityTimer != null)
                {
                    _mouseInactivityTimer.Stop();
                    _mouseInactivityTimer.Start();
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
            finally
            {
                _isMouseMoveHandled = false;
            }
        }

        private void SetupUI()
        {
            // Initialize UI elements like sliders, buttons, etc.
            var volumeSlider = this.FindControl<Slider>("VolumeSlider");
            if (volumeSlider != null)
            {
                volumeSlider.Value = 50; // Default volume
                if (mediaPlayer != null)
                {
                    mediaPlayer.Volume = 50;
                }
            }

            // // Load saved player position if available
            // if (playerCaches != null && playerCaches.Any())
            // {
            //     var cache = playerCaches.FirstOrDefault(x =>
            //         x.MovieId == movieId && x.ShowType == showType &&
            //         x.SeasonNumber == seasonNumber && x.EpisodeNumber == episodeNumber);
            //
            //     if (cache != null && mediaPlayer != null)
            //     {
            //         // We'll seek after media is loaded in the MediaPlayerOnLengthChanged
            //         currentPlayerCache = cache;
            //     }
            // }
        }

        

        #region MediaPlayer Events

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            if (!setThread)
            {
                SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
                setThread = true;
            }
        }

        private bool setThread = true;

        private void MediaPlayerOnPaused(object sender, EventArgs e)
        {
            SetThreadExecutionState(ES_CONTINUOUS);
            setThread = false;
        }

        bool isEndReached = false;
        private float lastPos;

        private async void MediaPlayerOnEndReached(object sender, EventArgs e)
        {
            try
            {
                if (DurationSlider.Value < 0.90) return;
                isEndReached = true;
                if (downloadsFilesPage == null)
                {
                    if (Player.MediaPlayer != null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Player.MediaPlayer.Stop();
                            Player.MediaPlayer.Position = 0;  // Videoyu baştan başlat
                            Player.MediaPlayer.Play();
                        });
                    }
                }
                else
                {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    /*lastPos = 1;
                    var currentEpisode = downloadsFilesPage.FilesDisplay.SelectedItem as EpisodeFile;
                    if (currentEpisode != null &&
                        currentEpisode.FileIndex != downloadsFilesPage.episodeFiles.Last().FileIndex)
                    {
                        var nextFile =
                            downloadsFilesPage.episodeFiles.IndexOf(
                                downloadsFilesPage.episodeFiles.FirstOrDefault(x => x.FileIndex == fileIndex)) + 1;
                        downloadsFilesPage.FilesDisplay.SelectedItem = downloadsFilesPage.episodeFiles[nextFile];
                        Close();
                    }
                    else
                    {
                        Close();
                    }*/
                }); 
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void MediaPlayerOnBuffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            // Handle buffering events
        }

        private void MediaPlayerOnStopped(object sender, EventArgs e)
        {
            Console.WriteLine("Media playback stopped");
        }
        private bool isSliderUpdating = false;
        private DateTime _lastPositionUpdateTime = DateTime.MinValue;
        private const int PositionUpdateThrottleMs = 100; // 100ms throttle - CPU kullanımını azaltmak için

        private async void MediaPlayerOnPositionChanged(object sender, MediaPlayerPositionChangedEventArgs e)
        {
            try
            {
                if (myPositionChanging || isSliderUpdating)
                {
                    return;
                }

                // Throttle - çok sık güncelleme yapmayı önle
                var now = DateTime.UtcNow;
                if ((now - _lastPositionUpdateTime).TotalMilliseconds < PositionUpdateThrottleMs)
                {
                    return;
                }
                _lastPositionUpdateTime = now;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        DurationSlider.Value = e.Position;
                        lastPos = e.Position;
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private DateTime _lastTimeUpdateTime = DateTime.MinValue;
        private const int TimeUpdateThrottleMs = 250; // 250ms throttle - CPU kullanımını azaltmak için

        private void MediaPlayerOnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            try
            {
                // Throttle - çok sık güncelleme yapmayı önle
                var now = DateTime.UtcNow;
                if ((now - _lastTimeUpdateTime).TotalMilliseconds < TimeUpdateThrottleMs)
                {
                    return;
                }
                _lastTimeUpdateTime = now;

                // Update time display
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        TxtCurrentTime.Text = TimeSpan.FromMilliseconds(e.Time).ToString().Substring(0, 8) + "/ ";
                        var remainMs = Math.Max(0, duration - e.Time);
                        TxtDuration.Text = TimeSpan.FromMilliseconds(remainMs).ToString().Substring(0, 8);
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TimeChanged error: {ex.Message}");
            }
        }

        private async void MediaPlayerOnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            try
            {
                if (e.Length > 0)
                {
                    UpdateDuration(e.Length);
                }
                //this.Dispatcher.Invoke(() =>
                //{
                //    TxtDuration.Text = TimeSpan.FromMilliseconds(e.Length).ToString().Substring(0, 8);
                //    if (String.IsNullOrWhiteSpace(path) && ProgressGrid.Visibility == Visibility.Visible)
                //    {
                //        ProgressGrid.Visibility = Visibility.Collapsed;
                //    }
                //});
                //seconds10 = (float)10000 / e.Length;
                //duration = e.Length;
                //durationInSeconds = e.Length / 1000;
                //twentyMinutesInMilliseconds = 20 * 60 * 1000;

                Console.WriteLine("On legnth changed");
                if (isCompleted && AppSettingsManager.appSettings.PlayerSettingShowThumbnail)
                {
                    Console.WriteLine("1");
                    string fullPath = fileInfo.FullName;
                    string directoryPath = Path.GetDirectoryName(fullPath);

                    var files = await Libtorrent.GetFiles(torrent.Hash);
                    if (files.Any())
                    {
                        var currentMediaFile = files.FirstOrDefault(x=> x.Index == fileIndex);
                        if (currentMediaFile != null)
                        {
                            _thumbnailsFolder = Path.Combine(directoryPath, "thumbnails" ,
                                Path.GetFileNameWithoutExtension(currentMediaFile.Name));
                            AddNewThumbnailCache(_thumbnailsFolder);
                            if (!Directory.Exists(_thumbnailsFolder))
                                Directory.CreateDirectory(_thumbnailsFolder);

                            int currentCount = Directory.GetFiles(_thumbnailsFolder, "*.jpg").Length;
                            if (durationInSeconds > 0 && currentCount < (durationInSeconds * 0.9))
                            {
                                _ = GenerateThumbnailsAsync(fullPath, _thumbnailsFolder);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        #endregion

        #region UI Event Handlers

        private void Player_OnMouseWheel(object sender, PointerWheelEventArgs e)
        {
            try
            {
                if(!shouldListenWindowEvents) return;
                if (!shouldListen) return;
                if (VolumeSlider.Value <= 200 && VolumeSlider.Value >= 0)
                {
                    if (e.Delta.Y > 0)
                    {
                        VolumeSlider.Value += 10;
                    }
                    else
                    {
                        VolumeSlider.Value -= 10;
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void Player_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            if(!shouldListenWindowEvents) return;
            if (AudioSubtitlePopup != null) AudioSubtitlePopup.IsOpen = false;
            if (mediaPlayer == null || !shouldListen || !shouldListenClickOnPlayer) return;

            if (mediaPlayer.IsPlaying)
            {
                mediaPlayer.Pause();
                PlayButton.Source = new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/play_transparent_266x220.png")));
            }
            else
            {
                mediaPlayer.Play();
                PlayButton.Source = new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/pause_transparent_266x220.png")));
            }
        }

        private void Player_OnMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if(!shouldListenWindowEvents) return;
                if (!shouldListen) return;
                // Eğer tam ekran geçişi zaten devam ediyorsa, birden fazla işlemi önlemek için durdur
                if (isFullScreenToggleInProgress)
                    return;

                isFullScreenToggleInProgress = true;

                // Tam ekran/normal ekran geçişi
                if (!isFullScreen)
                {
                    FullScreen();
                }
                else
                {
                    NormalScreen();
                }

                // Diğer olayların bu olayı etkilememesi için
                e.Handled = true;

                // 500ms sonra korumayı kaldır (yeni çift tıklamalara izin ver)
                Task.Delay(500).ContinueWith(_ => { isFullScreenToggleInProgress = false; });
            }
            catch (Exception ex)
            {
                isFullScreenToggleInProgress = false;
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private bool myPositionChanging = false;
        private async void DurationSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                if (!shouldListen) return;
                if (isCompleted)
                {
                    previousSliderValue = e.NewValue;
                    pieceTimeRanges = new List<TimeRange>()
                    {
                            new TimeRange()
                            {
                                Start = TimeSpan.FromSeconds(DurationSlider.Value * durationInSeconds), 
                                End =  (TimeSpan.FromSeconds(durationInSeconds))
                            }
                       
                    };
                    UpdateSelectionRanges();
                }
                else
                {
                    if (isSliderUpdating || myPositionChanging || torrent == null) return;

                    double newTimeInSeconds = e.NewValue * durationInSeconds;



                    var isDownloadedPiece = pieceTimeRanges.Any(range => newTimeInSeconds >= range.Start.TotalSeconds
                                                                         && newTimeInSeconds <= range.End.TotalSeconds);

                    if (!isDownloadedPiece)
                    {
                        isSliderUpdating = true;
                        DurationSlider.Value = previousSliderValue;
                        isSliderUpdating = false;
                    }
                    else
                    {
                        previousSliderValue = e.NewValue;
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }
        
         // private List<Rectangle> existingRectangles = new List<Rectangle>();

 private void UpdateSelectionRanges()
 {
     try
     {
         // RangeCanvas null ise (henüz oluşturulmamışsa) çıkalım
         if (rangeCanvas == null)
             return;

         rangeCanvas.Width = DurationSlider.Bounds.Width;

         // TimeRange'leri RangeCanvas için uygun formata dönüştürelim
         pieceTimeRanges = MergeOverlappingRanges(pieceTimeRanges);
         List<(double Start, double End)> regions = new List<(double Start, double End)>();

         foreach (var range in pieceTimeRanges)
         {
             double start = range.Start.TotalSeconds / durationInSeconds;
             double end = range.End.TotalSeconds / durationInSeconds;

             // [0, 1] aralığına clamp et
             start = Math.Max(0, Math.Min(1, start));
             end = Math.Max(0, Math.Min(1, end));

             // Bu aralık tamamen mevcut konumun gerisindeyse atla
             if (end <= previousSliderValue) continue;

             // Başlangıç mevcut konumun gerisindeyse, başlangıcı mevcut konuma ayarla
             if (start < previousSliderValue) start = previousSliderValue;

             // Geçersiz aralık oluştuysa atla
             if (start >= end) continue;

             regions.Add((start, end));
         }

         // RangeCanvas'a bölgeleri ekleyelim
         rangeCanvas.SetRegions(regions);
     }
     catch (Exception e)
     {
         var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
         Console.WriteLine(errorMessage);
     }
 }
 
 private List<TimeRange> MergeOverlappingRanges(List<TimeRange> ranges)
 {
     // Aralıkları başlama zamanına göre sırala
     var sortedRanges = ranges.OrderBy(r => r.Start).ToList();
     var mergedRanges = new List<TimeRange>();

     try
     {
         foreach (var range in sortedRanges)
         {
             if (mergedRanges.Count == 0)
             {
                 // İlk aralığı doğrudan ekle
                 mergedRanges.Add(range);
             }
             else
             {
                 var lastRange = mergedRanges.Last();
                 if (lastRange.End >= range.Start)
                 {
                     // Eğer aralıklar örtüşüyor veya bitiş ve başlangıç noktaları arasında küçük bir fark varsa birleştir
                     mergedRanges[mergedRanges.Count - 1] = new TimeRange()
                     {
                         Start = lastRange.Start,
                            End = TimeSpan.FromTicks(Math.Max(lastRange.End.Ticks, range.End.Ticks))
                     };
                 }
                 else
                 {
                     // Örtüşme yoksa yeni bir aralık ekle
                     mergedRanges.Add(range);
                 }
             }
         }
     }
     catch (Exception e)
     {
         var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
         Console.WriteLine(errorMessage);
     }

     return mergedRanges;
 }

        private void DurationSlider_OnMouseWheel(object sender, PointerWheelEventArgs e)
        {
            try
            {
                if (!shouldListen) return;
                if (Player.MediaPlayer == null) return;
                if (DurationSlider.Value <= 1 && DurationSlider.Value >= 0)
                {
                    if (isCompleted)
                    {
                        if (e.Delta.Y > 0)
                        {
                            Player.MediaPlayer.Position = Player.MediaPlayer.Position + seconds10;
                        }
                        else
                        {
                            Player.MediaPlayer.Position = Player.MediaPlayer.Position - seconds10;
                        }
                    }
                    else
                    {
                        if (e.Delta.Y > 0)
                        {
                            double newTimeInSeconds = (DurationSlider.Value + seconds10) * durationInSeconds *
                                                      durationInSeconds;

                            var isDownloadedPiece = pieceTimeRanges.Any(range =>
                                newTimeInSeconds >= range.Start.TotalSeconds
                                && newTimeInSeconds <= range.End.TotalSeconds);

                            if (isDownloadedPiece)
                            {
                                Player.MediaPlayer.Position = Player.MediaPlayer.Position + seconds10;
                            }
                        }
                        else
                        {
                            double newTimeInSeconds =
                                DurationSlider.Value * durationInSeconds - seconds10 * durationInSeconds;

                            var isDownloadedPiece = pieceTimeRanges.Any(range =>
                                newTimeInSeconds >= range.Start.TotalSeconds
                                && newTimeInSeconds <= range.End.TotalSeconds);

                            if (isDownloadedPiece)
                            {
                                Player.MediaPlayer.Position = Player.MediaPlayer.Position - seconds10;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
            
        }

     

        private void DurationSlider_OnMouseMove(object sender, PointerEventArgs e)
        {
            if (!shouldListen) return;
            
            try 
            {
                // Fare pozisyonunu al
                var pointerPosition = e.GetPosition(DurationSlider);
                var sliderWidth = DurationSlider.Bounds.Width;
                
                // Slider üzerindeki pozisyona göre saniyeyi hesapla
                var relativePosition = pointerPosition.X / sliderWidth;
                if (relativePosition < 0) relativePosition = 0;
                if (relativePosition > 1) relativePosition = 1;
                
                var currentTimeInSeconds = (int)(relativePosition * durationInSeconds);
                
                // Zaman bilgisini göster
                var timeSpan = TimeSpan.FromSeconds(currentTimeInSeconds);
                ThumbnailTimeText.Text = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                
                // Popup'ı göster ve konumlandır
                if (ThumbnailPreviewPopup != null)
                {
                    ThumbnailPreviewPopup.IsOpen = true;
                    ThumbnailPreviewPopup.HorizontalOffset = pointerPosition.X - (sliderWidth / 2);
                }
                
                // Şu anki saniyeye göre thumbnail'i göster
                ShowThumbnailPreview(currentTimeInSeconds, ThumbnailPreviewImage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void ButtonPlay_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            if (!shouldListen) return;
            if (mediaPlayer == null) return; // Keep original null check for mediaPlayer

            if (mediaPlayer.IsPlaying)
            {
                mediaPlayer.Pause(); // Keep original functionality
                PlayButton.Source = _playIcon.Value;
            }
            else
            {
                mediaPlayer.Play(); // Keep original functionality
                PlayButton.Source = _pauseIcon.Value;
            }
        }

        private int saveVolume = 50;

        private void ButtonMute_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (!shouldListen) return;
                if (VolumeSlider.Value == 0)
                {
                    VolumeSlider.Value = saveVolume;
                    if (saveVolume >= 100)
                    {
                        MuteButton.Source = _volume2Icon.Value;
                    }
                    else
                    {
                        MuteButton.Source = _volume1Icon.Value;
                    }
                }
                else
                {
                    saveVolume = Convert.ToInt32(VolumeSlider.Value);
                    MuteButton.Source = _muteIcon.Value;
                    VolumeSlider.Value = 0;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void VolumeSlider_OnLoaded(object sender, RoutedEventArgs e)
        {
            // VolumeSlider.ValueChanged += VolumeSlider_OnValueChanged;
        }

        private async void ButtonBack_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (!shouldListen) return;
                shouldListen = false; // Prevent multiple clicks
                await Close(); // Properly cleanup before changing content
                MainWindow.Instance.ShowTitleBar(); 
                MainWindow.Instance.SetContent(MainView.Instance);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
                // Ensure UI is reset even if cleanup fails
                try
                {
                    MainWindow.Instance.ShowTitleBar();
                    MainWindow.Instance.SetContent(MainView.Instance);
                }
                catch { }
            }
        }

        private static readonly object _cleanupLock = new object();
        private bool _isCleaningUp = false;

        private async Task CleanupVideoPlayer()
        {
            // Prevent concurrent cleanup
            lock (_cleanupLock)
            {
                if (_isCleaningUp) return;
                _isCleaningUp = true;
            }

            try
            {
                Log.Information("CleanupVideoPlayer: Starting cleanup");
                
                // CRITICAL FIX: Detach from UI immediately to prevent AccessViolation during Dispose
                // This ensures VideoView doesn't try to access the MediaPlayer while we are disposing it
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (Player != null)
                    {
                        Player.MediaPlayer = null;
                    }
                });

                // 1. Önce MediaPlayer'ı durdur (UI thread'de güvenli)
                MediaPlayer tempMediaPlayer = null;
                LibVLC tempLibVlc = null;
                
                lock (_cleanupLock)
                {
                    tempMediaPlayer = mediaPlayer;
                    tempLibVlc = libVlc;
                }

                // 2. MediaPlayer'ı durdur ve event handler'ları kaldır
                if (tempMediaPlayer != null)
                {
                    try
                    {
                        // Stop playback immediately
                        if (tempMediaPlayer.IsPlaying)
                        {
                            tempMediaPlayer.Stop();
                        }
                        
                        // Remove event handlers
                        tempMediaPlayer.Playing -= MediaPlayer_Playing;
                        tempMediaPlayer.Paused -= MediaPlayerOnPaused;
                        tempMediaPlayer.EndReached -= MediaPlayerOnEndReached;
                        tempMediaPlayer.PositionChanged -= MediaPlayerOnPositionChanged;
                        tempMediaPlayer.LengthChanged -= MediaPlayerOnLengthChanged;
                        tempMediaPlayer.Buffering -= MediaPlayerOnBuffering;
                      //  tempMediaPlayer.EncounteredError -= MediaPlayerOnEncounteredError;
                        
                        // Dispose in background to avoid UI freeze
                        await Task.Run(() =>
                        {
                            try
                            {
                                tempMediaPlayer.Dispose();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "CleanupVideoPlayer: Error disposing MediaPlayer");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "CleanupVideoPlayer: Error stopping MediaPlayer");
                    }
                    
                    lock (_cleanupLock)
                    {
                        mediaPlayer = null;
                    }
                }

                // 3. LibVLC'yi temizle
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
                            Log.Error(ex, "CleanupVideoPlayer: Error disposing LibVLC");
                        }
                    });

                    lock (_cleanupLock)
                    {
                        libVlc = null;
                    }
                }

                // 4. GC'yi zorlayarak bellek temizliğini hızlandır
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Log.Information("CleanupVideoPlayer: Cleanup completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CleanupVideoPlayer: General error");
            }
            finally
            {
                lock (_cleanupLock)
                {
                    _isCleaningUp = false;
                }
            }
        }


        private async Task Close()
        {
            if (_disposed) return;
            _disposed = true;
            
            try
            {
                // Önce taskbar'ı göster (crash olsa bile taskbar görünür kalmalı)
                try
                {
                    if (isFullScreen)
                        Taskbar.Show();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Taskbar restore error: {ex.Message}");
                }
                
                try { _syncProcess?.Kill(); } catch { }
                try { _translateCts?.Cancel(); } catch { }

                if (AudioSubtitlePopup != null && AudioSubtitlePopup.IsOpen)
                {
                    AudioSubtitlePopup.IsOpen = false;
                }
                
                if (VolumePopup != null && VolumePopup.IsOpen)
                {
                    VolumePopup.IsOpen = false;
                }
                
                if (_volumePopupTimer != null)
                {
                    _volumePopupTimer.Stop();
                    _volumePopupTimer.Elapsed -= VolumePopupTimerOnElapsed;
                    _volumePopupTimer.Dispose();
                    _volumePopupTimer = null;
                }
                
                // Dispose mouse inactivity timer
                if (_mouseInactivityTimer != null)
                {
                    _mouseInactivityTimer.Stop();
                    _mouseInactivityTimer.Elapsed -= MouseInactivityTimerOnElapsed;
                    _mouseInactivityTimer.Dispose();
                    _mouseInactivityTimer = null;
                }
                
                // Dispose torrent progress timer
                if (_torrentProgressTimer != null)
                {
                    _torrentProgressTimer.Stop();
                    _torrentProgressTimer.Tick -= TorrentProgressTimerOnTick;
                    _torrentProgressTimer = null;
                }
                
                // Dispose thumbnail CancellationTokenSource
                if (_thumbnailCts != null)
                {
                    _thumbnailCts.Cancel();
                    _thumbnailCts.Dispose();
                    _thumbnailCts = null;
                }
                
                // Dispose bounds subscription
                _boundsSubscription?.Dispose();
                _boundsSubscription = null;
               
                Player.Loaded -= PlayerOnLoaded;
                closed = true;
                
                // Event handler'ları unsubscribe et
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.SizeChanged -= MainWindowOnSizeChanged;
                }
                
                // DispatcherTimer'ı durdur ve dispose et
                if (dispatcherTimer != null)
                {
                    dispatcherTimer.Stop();
                    dispatcherTimer.Tick -= DispatcherTimerOnTick;
                    dispatcherTimer = null;
                }
                
                // Thumbnail cache'i temizle
                ClearThumbnailCache();
                
                // DurationSlider event handler'ları kaldır
                if (DurationSlider != null)
                {
                    DurationSlider.PointerEntered -= DurationSlider_PointerEntered;
                    DurationSlider.PointerExited -= DurationSlider_PointerExited;
                }
                
                // Dispose current media
                if (_currentMedia != null)
                {
                    try { _currentMedia.Dispose(); } catch { }
                    _currentMedia = null;
                }
                
                if (torrentStream != null)
                {
                    torrentStream.Dispose();
                    torrentStream = null;
                }
                
                // Cleanup işlemini background thread'de yap ama await et
                await CleanupVideoPlayer();
               
                cts?.Cancel();
                cts?.Dispose();
                cts = null;
                
                // Bellek temizliği
                GC.Collect();
                GC.WaitForPendingFinalizers();
            
                var time = await FirestoreManager.GetNistTime();
                if (currentPlayerCache != null)
                {
                    await FirestoreManager.EditWatchHistory(new EditWatchHistoryRequest()
                    {
                        Email = AppSettingsManager.appSettings.FireStoreEmail,
                        EpisodeNumber = episodeNumber,
                        Id = movieId,
                        LastWatchDate = time.ToUniversalTime(),
                        NewProgress = isEndReached ? 1 : lastPos,
                        SeasonNumber = seasonNumber,
                        ShowType = showType,
                        DeletedTorrent = false,
                        Hash = torrent.Hash
                    });
                }
                else
                {
                    var addshowToWatchHistoryRequest = new AddShowToWatchHistoryRequest()
                    {
                        Email = AppSettingsManager.appSettings.FireStoreEmail,
                        EpisodeNumber = episodeNumber,
                        Id = movieId,
                        LastWatchDate = time.ToUniversalTime(),
                        Name = movieName,
                        Poster = poster,
                        Progress = isEndReached ? 1 : lastPos,
                        SeasonNumber = seasonNumber,
                        ShowType = showType,
                        DeletedTorrent = false,
                        TorrentHash = torrent.Hash
                    };
                    await FirestoreManager.AddShowToWatchHistory(addshowToWatchHistoryRequest);
                }
                SetThreadExecutionState(ES_CONTINUOUS);
                Console.WriteLine($"Unloaded Player window for: {GetShowName()}");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
                
                // Crash durumunda da taskbar'ı göster
                try
                {
                    Taskbar.Show();
                    MainWindow.Instance.ShowTitleBar();
                }
                catch { }
            }
        }

        private void ButtonFullScreen_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            if (!shouldListen) return;
            if (MainWindow.Instance.WindowState == WindowState.Normal)
            {
                FullScreen();
            }
            else
            {
                NormalScreen();
            }
        }

        private bool _isAudioSubtitlePopupHovered = false;
        private bool _isAudioSubtitleButtonHovered = false;
        
        private Process _syncProcess;
        private CancellationTokenSource _translateCts;


        private async void AudioSubtitlePopup_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
            _isAudioSubtitleButtonHovered = true;
            _isAudioSubtitlePopupHovered = true;
            
            if (!AudioSubtitlePopup.IsOpen)
            {
               await LoadAudioSubtitleData();
               AudioSubtitlePopup.IsOpen = true;
               if (DurationSlider != null) DurationSlider.IsVisible = false;
            }
        }

        private async void AudioSubtitlePopup_OnPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
            _isAudioSubtitlePopupHovered = false;
            _isAudioSubtitleButtonHovered = false;
            await Task.Delay(200);
            if (!_isAudioSubtitlePopupHovered && !_isAudioSubtitleButtonHovered)
            {
                AudioSubtitlePopup.IsOpen = false;
                if (DurationSlider != null && !VolumePopup.IsOpen && !PlaybackSpeedPopup.IsOpen && !SubtitleShiftPopup.IsOpen) DurationSlider.IsVisible = true;
            }
        }

        private bool _isSubtitleShiftPopupHovered = false;
        private bool _isSubtitleShiftButtonHovered = false;

        private void SubtitleShiftPopup_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
            _isSubtitleShiftButtonHovered = true;
            _isSubtitleShiftPopupHovered = true;
            
            if (!SubtitleShiftPopup.IsOpen)
            {
               SubtitleShiftPopup.IsOpen = true;
               if (DurationSlider != null) DurationSlider.IsVisible = false;
            }
        }

        private async void SubtitleShiftPopup_OnPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
            _isSubtitleShiftPopupHovered = false;
            _isSubtitleShiftButtonHovered = false;
            await Task.Delay(200);
            if (!_isSubtitleShiftPopupHovered && !_isSubtitleShiftButtonHovered)
            {
                SubtitleShiftPopup.IsOpen = false;
                if (DurationSlider != null && !VolumePopup.IsOpen && !PlaybackSpeedPopup.IsOpen && !AudioSubtitlePopup.IsOpen) DurationSlider.IsVisible = true;
            }
        }

        private void ButtonSubtitleShift_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            SubtitleShiftPopup_OnPointerEntered(sender, e);
        }

        private void ButtonSubtitleShift_OnPointerExited(object? sender, PointerEventArgs e)
        {
            SubtitleShiftPopup_OnPointerExited(sender, e);
        }

        private long _currentSubtitleDelayMs = 0;

        private void SubtitleShiftPlus_Pressed(object? sender, RoutedEventArgs e)
        {
            if (int.TryParse(SubtitleShiftTextBox.Text, out int amount))
            {
                _currentSubtitleDelayMs += amount;
                ApplySubtitleDelay();
            }
        }

        private void SubtitleShiftMinus_Pressed(object? sender, RoutedEventArgs e)
        {
            if (int.TryParse(SubtitleShiftTextBox.Text, out int amount))
            {
                _currentSubtitleDelayMs -= amount;
                ApplySubtitleDelay();
            }
        }

        private void ApplySubtitleDelay()
        {
            if (Player.MediaPlayer != null)
            {
                // In VLC, SpuDelay is in microseconds mapping => 1 ms = 1000 microsecond
                if (Player.MediaPlayer.SetSpuDelay(_currentSubtitleDelayMs * 1000))
                {
                    Console.WriteLine("SPU delay set: " + _currentSubtitleDelayMs);
                } 
                
                // Show notification to user
                string prefix = _currentSubtitleDelayMs > 0 ? "+" : "";
                NotificationService.Instance.ShowNotification("Altyazı Gecikmesi", $"{prefix}{_currentSubtitleDelayMs} ms", TimeSpan.FromSeconds(2), true);
            }
        }

        private void SaveShiftedSubtitle_Pressed(object? sender, RoutedEventArgs e)
        {
            if (_currentSubtitleDelayMs == 0) return;
            ShiftSubtitlePermanently((int)_currentSubtitleDelayMs);
            _currentSubtitleDelayMs = 0; // reset after save
            
            if (Player.MediaPlayer != null)
            {
                Player.MediaPlayer.SetSpuDelay(0);
            }
        }

        private async void ShiftSubtitlePermanently(int amountMs)
        {
            if (currentSubtitle == null || string.IsNullOrEmpty(currentSubtitle.Fullpath) || !File.Exists(currentSubtitle.Fullpath))
                return;

            try
            {
                var parser = new SubtitlesParser.Classes.Parsers.SrtParser();
                List<SubtitlesParser.Classes.SubtitleItem> items;
                using (var stream = File.OpenRead(currentSubtitle.Fullpath))
                {
                    items = parser.ParseStream(stream, System.Text.Encoding.UTF8);
                }

                foreach (var item in items)
                {
                    item.StartTime += amountMs;
                    item.EndTime += amountMs;
                    
                    if (item.StartTime < 0) item.StartTime = 0;
                    if (item.EndTime < item.StartTime) item.EndTime = item.StartTime + 1000; // fallback
                }

                var tempPath = currentSubtitle.Fullpath + ".tmp";
                using (var stream = File.Create(tempPath))
                using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        writer.WriteLine(i + 1);
                        var start = TimeSpan.FromMilliseconds(item.StartTime);
                        var end = TimeSpan.FromMilliseconds(item.EndTime);
                        writer.WriteLine($"{start:hh\\:mm\\:ss\\,fff} --> {end:hh\\:mm\\:ss\\,fff}");
                        foreach (var line in item.Lines)
                        {
                            writer.WriteLine(line);
                        }
                        writer.WriteLine();
                    }
                }

                File.Delete(currentSubtitle.Fullpath);
                File.Move(tempPath, currentSubtitle.Fullpath);

                SubtitleShiftPopup.IsOpen = false;

                if (mediaPlayer != null)
                {
                    subtitleClickedTime = mediaPlayer.Position;
                    mediaPlayer.Stop();
                }
                success = false;
                
                NotificationService.Instance.ShowNotification("NetStream", $"Altyazıya {amountMs}ms kaydedildi.", TimeSpan.FromSeconds(4), true);

                await Task.Delay(100);
                PlayerOnLoaded(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error shifting subtitle: {ex.Message}");
            }
        }

        private async Task LoadAudioSubtitleData()
        {
             try
             {
                 // Audio Tracks
                 var audioTracks = new List<SubtitleTrackItem>();
                 if (Player?.MediaPlayer != null)
                 {
                     var tracks = Player.MediaPlayer.AudioTrackDescription;
                     var currentTrackToken = Player.MediaPlayer.AudioTrack;
                     foreach(var t in tracks)
                     {
                         bool isSelected = t.Id == currentTrackToken;
                         audioTracks.Add(new SubtitleTrackItem { Id = t.Id, Name = t.Name, IsSelected = isSelected, TextColor = isSelected ? "White" : "#BBBBBB" });
                     }
                 }
                 AudioTracksItemsControl.ItemsSource = audioTracks;

                 // Subtitle Languages
                 var subtitleLanguages = SubtitleHandler.LanguagesToOpenSubtitles.Select(x => x.Key);
                 var subLanguagesNames = Service.Languages.Where(x => subtitleLanguages.Any(z => z == x.Iso_639_1)).Select(m => m.EnglishName).OrderBy(x => x).ToList();
                 subLanguagesNames.Insert(0, "Disabled");
                 
                 var subtitleLangs = new List<SubtitleTrackItem>();
                 string selectedLang = AppSettingsManager.appSettings.SubtitleLanguage ?? "English";
                 foreach(var sl in subLanguagesNames)
                 {
                     bool isSelected = sl == selectedLang;
                     string displayName = sl == "Disabled" ? "Kapalı" : sl;
                     subtitleLangs.Add(new SubtitleTrackItem { Name = sl, IsSelected = isSelected, TextColor = isSelected ? "White" : "#BBBBBB", Details = displayName });
                 }
                 SubtitleLangsItemsControl.ItemsSource = subtitleLangs;

                 // Search for Found Subtitles
                 if (selectedLang != "Disabled")
                 {
                      int yr = 0;
                      if (showType == ShowType.Movie)
                      {
                          var movie = await Service.client.GetMovieAsync(movieId);
                          yr = movie.ReleaseDate?.Year ?? 0;
                      }
                      else
                      {
                          var tvShow = await Service.client.GetTvShowAsync(movieId);
                          yr = tvShow.FirstAirDate?.Year ?? 0;
                      }
                      
                      ObservableCollection<Subtitle> localSubtitles;
                      
                      localSubtitles = await SubtitleHandler.SearchSubtitle(movieId, seasonNumber, episodeNumber,
                          AppSettingsManager.appSettings.IsoSubtitleLanguage, path, movieName, imdbId);
                      
                      var foundSubtitles = new List<SubtitleTrackItem>();
                      foreach(var sub in localSubtitles)
                      {
                          bool isCurrent = IsCurrentSubtitle(sub);
                          foundSubtitles.Add(new SubtitleTrackItem {
                               Id = sub.SubtitleId ?? 0,
                               Name = sub.Name,
                               Details = $"Downloads: {sub.DownloadCount ?? "0"} • Votes: {sub.Votes ?? "0"}",
                               HasDetails = true,
                               IsSelected = isCurrent,
                           TextColor = isCurrent ? "White" : "#BBBBBB",
                           Tag = sub
                          });
                      }
                      FoundSubtitlesItemsControl.ItemsSource = foundSubtitles;
                      
                      // Auto scroll to selected element in Found Subtitles
                      Dispatcher.UIThread.InvokeAsync(async () => {
                          await Task.Delay(50);
                          var selectedFoundObj = foundSubtitles.FirstOrDefault(x => x.IsSelected);
                          if(selectedFoundObj != null) {
                              var idx = foundSubtitles.IndexOf(selectedFoundObj);
                              if(FoundSubtitlesScrollViewer != null) FoundSubtitlesScrollViewer.Offset = new Avalonia.Vector(0, idx * 30);
                          }
                      });
                 }
                 else
                 {
                      FoundSubtitlesItemsControl.ItemsSource = null;
                 }
                 
                 // Kendi Altyazılarım (Own Subtitles)
                 var ownSubtitlesList = new List<SubtitleTrackItem>();
                 if (subtitles != null)
                 {
                     var generatedSubs = subtitles
                         .Where(x =>
                             x.MovieId == movieId &&
                             x.EpisodeNumber == episodeNumber &&
                             x.SeasonNumber == seasonNumber &&
                             !string.IsNullOrEmpty(x.Fullpath) &&
                             IsOwnSubtitleEntry(x))
                         .GroupBy(x => GetNormalizedSubtitlePath(x.Fullpath), StringComparer.OrdinalIgnoreCase)
                         .Select(x => x.Last())
                         .ToList();
                     foreach(var sub in generatedSubs)
                     {
                          bool isCurrent = IsCurrentSubtitle(sub);
                         ownSubtitlesList.Add(new SubtitleTrackItem {
                             Id = sub.SubtitleId ?? 0,
                             Name = Path.GetFileName(sub.Fullpath),
                             Details = sub.Language ?? "tr",
                             HasDetails = true,
                             IsSelected = isCurrent,
                             TextColor = isCurrent ? "White" : "#BBBBBB",
                             Tag = sub
                         });
                     }
                 }
                 OwnSubtitlesItemsControl.ItemsSource = ownSubtitlesList;
                 
                 // Auto scroll for audio list
                 Dispatcher.UIThread.InvokeAsync(async () => {
                     await Task.Delay(50);
                     var selectedAudio = audioTracks.FirstOrDefault(x => x.IsSelected);
                     if(selectedAudio != null) {
                         var idx = audioTracks.IndexOf(selectedAudio);
                         if(AudioScrollViewer != null) AudioScrollViewer.Offset = new Avalonia.Vector(0, idx * 30);
                     }
                     
                     var selectedSubLang = subtitleLangs.FirstOrDefault(x => x.IsSelected);
                     if(selectedSubLang != null) {
                         var idx = subtitleLangs.IndexOf(selectedSubLang);
                         if(SubtitleLangsScrollViewer != null) SubtitleLangsScrollViewer.Offset = new Avalonia.Vector(0, idx * 30);
                     }
                 });
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"LoadAudioSubtitleData error: {ex.Message}");
             }
        }
        
        private async void OwnSubtitleItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control control && control.DataContext is SubtitleTrackItem subItem)
            {
                var selectedSubtitle = subItem.Tag as Subtitle;
                if (selectedSubtitle != null)
                {
                    selectedSubtitle.SubtitleId = null;
                    selectedSubtitle.FileId = null;
                    selectedSubtitle.FileName = null;
                    currentSubtitle = selectedSubtitle;
                    UpdateSubtitleLanguagePreference(selectedSubtitle.Language);
                    SaveSubtitleRegistry();
                    AudioSubtitlePopup.IsOpen = false;

                    if (mediaPlayer != null)
                    {
                        subtitleClickedTime = mediaPlayer.Position;
                        mediaPlayer.Stop();
                    }
                    success = false;
                    
                    await Task.Delay(100);
                    PlayerOnLoaded(this, new RoutedEventArgs());
                }
            }
        }
        
        private async void AudioTrackItem_PointerPressed(object? sender, PointerPressedEventArgs e)

        {
            if (sender is Control control && control.DataContext is SubtitleTrackItem track)
            {
                if (Player?.MediaPlayer != null)
                {
                    Player.MediaPlayer.SetAudioTrack(track.Id);
                    await LoadAudioSubtitleData();
                }
            }
        }
        
        private async void SubtitleLangItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control control && control.DataContext is SubtitleTrackItem lang)
            {
                string? selectedIsoLanguage;
                if (lang.Name == "Disabled")
                {
                    selectedIsoLanguage = "Disabled";
                }
                else
                {
                    selectedIsoLanguage = Service.Languages.FirstOrDefault(x => x.EnglishName == lang.Name)?.Iso_639_1;
                    if (string.IsNullOrWhiteSpace(selectedIsoLanguage))
                    {
                        return;
                    }
                }

                UpdateSubtitleLanguagePreference(selectedIsoLanguage);

                if (string.Equals(selectedIsoLanguage, "Disabled", StringComparison.OrdinalIgnoreCase))
                {
                    currentSubtitle = null;
                    if (Player?.MediaPlayer != null)
                    {
                        Player.MediaPlayer.SetSpu(-1);
                    }
                }

                await LoadAudioSubtitleData();
            }
        }
        
        private async void FoundSubtitleItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control control && control.DataContext is SubtitleTrackItem subItem)
            {
                var selectedSubtitle = subItem.Tag as Subtitle;
                if (selectedSubtitle != null)
                {
                    if (currentSubtitle == null || currentSubtitle.SubtitleId != selectedSubtitle.SubtitleId)
                    {
                        try
                        {
                            if (!selectedSubtitle.FileId.HasValue || string.IsNullOrWhiteSpace(selectedSubtitle.FileName))
                            {
                                return;
                            }

                            var subtitlePath = await SubtitleHandler.DownloadSubtitleWithoutSync(selectedSubtitle.FileId.Value, selectedSubtitle.FileName);
                             
                            var activeSubtitle = PlayerWindow.subtitles.LastOrDefault(subtitle =>
                                subtitle.MovieId == selectedSubtitle.MovieId &&
                                subtitle.EpisodeNumber == selectedSubtitle.EpisodeNumber &&
                                subtitle.SeasonNumber == selectedSubtitle.SeasonNumber &&
                                !IsOwnSubtitleEntry(subtitle) &&
                                subtitle.SubtitleId == selectedSubtitle.SubtitleId);

                            if (activeSubtitle == null)
                            {
                                activeSubtitle = PlayerWindow.subtitles.LastOrDefault(subtitle =>
                                    subtitle.MovieId == selectedSubtitle.MovieId &&
                                    subtitle.EpisodeNumber == selectedSubtitle.EpisodeNumber &&
                                    subtitle.SeasonNumber == selectedSubtitle.SeasonNumber &&
                                    !IsOwnSubtitleEntry(subtitle) &&
                                    string.Equals(subtitle.Language, selectedSubtitle.Language, StringComparison.OrdinalIgnoreCase));
                            }

                            if (activeSubtitle == null)
                            {
                                selectedSubtitle.Name = Path.GetFileName(subtitlePath);
                                selectedSubtitle.Fullpath = subtitlePath;
                                selectedSubtitle.HashDownload = true;
                                selectedSubtitle.Synchronized = true;
                                selectedSubtitle.CustomIsUserAdded = false;
                                PlayerWindow.subtitles.Add(selectedSubtitle);
                                activeSubtitle = selectedSubtitle;
                            }
                            else
                            {
                                activeSubtitle.Name = Path.GetFileName(subtitlePath);
                                activeSubtitle.Fullpath = subtitlePath;
                                activeSubtitle.HashDownload = true;
                                activeSubtitle.Synchronized = true;
                                activeSubtitle.SubtitleId = selectedSubtitle.SubtitleId;
                                activeSubtitle.FileId = selectedSubtitle.FileId;
                                activeSubtitle.FileName = selectedSubtitle.FileName;
                                activeSubtitle.Language = selectedSubtitle.Language;
                                activeSubtitle.CustomIsUserAdded = false;
                            }

                            currentSubtitle = activeSubtitle;
                            UpdateSubtitleLanguagePreference(activeSubtitle.Language);
                            SaveSubtitleRegistry();
                             
                            AudioSubtitlePopup.IsOpen = false;

                            if (mediaPlayer != null)
                            {
                                subtitleClickedTime = mediaPlayer.Position;
                                mediaPlayer.Stop();
                            }
                            success = false;
                            
                            // To prevent crash, wait a tiny bit
                            await Task.Delay(100);
                            PlayerOnLoaded(this, new RoutedEventArgs());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Subtitle download error: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void PanelControlVideo_OnMouseEnter(object sender, PointerEventArgs e)
        {
            if (!shouldListen) return;
            shouldCollapseControlPanel = false;
            shouldListenClickOnPlayer = false;
        }

        private void PanelControlVideo_OnMouseLeave(object sender, PointerEventArgs e)
        {
            shouldCollapseControlPanel = true;
            shouldListenClickOnPlayer = true;
        }

        private async void NextEpisodeButton_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (downloadsFilesPage != null)
                {
                     var currentEpisode = downloadsFilesPage.episodeFiles.FirstOrDefault(x => x.FileIndex == fileIndex);
                     if (currentEpisode != null)
                     {
                         var currentIndex = downloadsFilesPage.episodeFiles.IndexOf(currentEpisode);
                         if (currentIndex < downloadsFilesPage.episodeFiles.Count - 1)
                         {
                             var nextEpisode = downloadsFilesPage.episodeFiles[currentIndex + 1];
                             
                             // Close current player
                             await Close();
                             
                             // Open next episode
                             var playerWindow = new PlayerWindow(movieId, movieName, showType, nextEpisode.SeasonNumber, nextEpisode.EpisodeNumber,
                                 new FileInfo(nextEpisode.FilePath), nextEpisode.IsCompleted, showType == ShowType.TvShow ? imdbId : -1, torrent, nextEpisode.FileIndex, poster, downloadsFilesPage);
                             
                             MainWindow.Instance.SetContent(playerWindow);

                             if (showType == ShowType.TvShow)
                             {
                                 await Libtorrent.ChangeEpisodeFileToMaximalPriority(torrent.Hash, nextEpisode.SeasonNumber, nextEpisode.EpisodeNumber);
                             }
                             else
                             {
                                 if (!nextEpisode.IsCompleted)
                                     await Libtorrent.ChangeMovieCollectionFilePriorityToMaximal(torrent, nextEpisode.FileIndex);
                             }
                         }
                     }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing episode: {ex.Message}");
            }
        }

        private void BtnWatchCredits_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            // Continue watching the current episode credits
            NextEpisodeStackPanel.IsVisible = false;
        }

        private List<TimeRange> pieceTimeRanges = new List<TimeRange>();

        private void PlayerWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            
            if (mediaPlayer == null || !shouldListen) return;

            try
            {
                if (Player.MediaPlayer == null) return;
                if (e.Key == Key.Space)
                {
                    if (Player.MediaPlayer.IsPlaying)
                    {
                        Player.MediaPlayer.Pause();
                        PlayButton.Source = new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/play_transparent_266x220.png")));
                    }
                    else
                    {
                        Player.MediaPlayer.Pause();
                        PlayButton.Source = new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/pause_transparent_266x220.png")));
                    }
                }

                if (isCompleted)
                {
                    if (e.Key == Key.Right)
                    {
                        Player.MediaPlayer.Position = Player.MediaPlayer.Position + seconds10;
                    }

                    if (e.Key == Key.Left)
                    {
                        Player.MediaPlayer.Position = Player.MediaPlayer.Position - seconds10;
                    }
                }
                else
                {
                    if (e.Key == Key.Right)
                    {
                        double newTimeInSeconds =
                            DurationSlider.Value * durationInSeconds + seconds10 * durationInSeconds;

                        var isDownloadedPiece = pieceTimeRanges.Any(range =>
                            newTimeInSeconds >= range.Start.TotalSeconds
                            && newTimeInSeconds <= range.End.TotalSeconds);

                        if (isDownloadedPiece)
                        {
                            Player.MediaPlayer.Position = Player.MediaPlayer.Position + seconds10;
                        }
                    }

                    if (e.Key == Key.Left)
                    {
                        double newTimeInSeconds =
                            DurationSlider.Value * durationInSeconds - seconds10 * durationInSeconds;

                        var isDownloadedPiece = pieceTimeRanges.Any(range =>
                            newTimeInSeconds >= range.Start.TotalSeconds
                            && newTimeInSeconds <= range.End.TotalSeconds);

                        if (isDownloadedPiece)
                        {
                            Player.MediaPlayer.Position = Player.MediaPlayer.Position - seconds10;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        #endregion

        #endregion

        #region Helper Methods

        public void FullScreen()
        {
            try
            {
                if (isFullScreen) return; // Zaten tam ekrandaysa çık

                MainWindow.Instance.WindowState = WindowState.FullScreen; // True fullscreen
                MainWindow.Instance.HideTitleBar(); // Title bar'ı gizle
                Taskbar.Hide(); // Hide taskbar
                isFullScreen = true;
                ButtonBack.IsVisible = true;

                Console.WriteLine("Entered full screen mode");
                
                FullScreenIcon.Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/crop_transparent_266x220.png")));
                
                // FIXAT: Tam ekran geçişinde selection ranges'i güncelle
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        if (rangeCanvas != null && pieceTimeRanges != null && pieceTimeRanges.Count > 0)
                        {
                            UpdateSelectionRanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating selection ranges in fullscreen: {ex.Message}");
                    }
                }, DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        public void NormalScreen()
        {
            try
            {
                if (!isFullScreen) return; // Zaten normal ekrandaysa çık

                MainWindow.Instance.WindowState = WindowState.Normal;
                MainWindow.Instance.ShowTitleBar(); // Title bar'ı göster
                Taskbar.Show(); // Show taskbar
                isFullScreen = false;
                Console.WriteLine("Exited full screen mode");
                
                FullScreenIcon.Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/fullscreen_transparent_266x220.png")));
                
                // FIXAT: Normal ekrana dönüşte selection ranges'i güncelle
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        if (rangeCanvas != null && pieceTimeRanges != null && pieceTimeRanges.Count > 0)
                        {
                            UpdateSelectionRanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating selection ranges in normal screen: {ex.Message}");
                    }
                }, DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }


        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            try
            {
                if (_volumePopupTimer != null)
                {
                    _volumePopupTimer.Stop();
                    _volumePopupTimer.Elapsed -= VolumePopupTimerOnElapsed;
                    _volumePopupTimer.Dispose();
                    _volumePopupTimer = null;
                }
                
                // Dispose mouse inactivity timer
                if (_mouseInactivityTimer != null)
                {
                    _mouseInactivityTimer.Stop();
                    _mouseInactivityTimer.Elapsed -= MouseInactivityTimerOnElapsed;
                    _mouseInactivityTimer.Dispose();
                    _mouseInactivityTimer = null;
                }
                
                // Dispose torrent progress timer
                if (_torrentProgressTimer != null)
                {
                    _torrentProgressTimer.Stop();
                    _torrentProgressTimer.Tick -= TorrentProgressTimerOnTick;
                    _torrentProgressTimer = null;
                }
                
                // Dispose thumbnail CancellationTokenSource
                if (_thumbnailCts != null)
                {
                    _thumbnailCts.Cancel();
                    _thumbnailCts.Dispose();
                    _thumbnailCts = null;
                }
                
                // Dispose bounds subscription
                _boundsSubscription?.Dispose();
                _boundsSubscription = null;
                
                // Dispose CancellationTokenSource
                cts?.Cancel();
                cts?.Dispose();
                cts = null;
                
                // Dispose current media
                if (_currentMedia != null)
                {
                    try { _currentMedia.Dispose(); } catch { }
                    _currentMedia = null;
                }
                
                // Dispose torrent stream
                if (torrentStream != null)
                {
                    torrentStream.Dispose();
                    torrentStream = null;
                }
                
                // Event handler'ları unsubscribe et
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.SizeChanged -= MainWindowOnSizeChanged;
                }
                
                // DispatcherTimer'ı durdur ve dispose et
                if (dispatcherTimer != null)
                {
                    dispatcherTimer.Stop();
                    dispatcherTimer.Tick -= DispatcherTimerOnTick;
                    dispatcherTimer = null;
                }
                
                // Thumbnail cache'i temizle
                ClearThumbnailCache();
                
                // DurationSlider event handler'ları kaldır
                if (DurationSlider != null)
                {
                    DurationSlider.PointerEntered -= DurationSlider_PointerEntered;
                    DurationSlider.PointerExited -= DurationSlider_PointerExited;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dispose error: {ex.Message}");
            }
        }

        #endregion

        private bool _isMouseMoveHandled = false;

        private async void Player_OnMouseMove(object? sender, PointerEventArgs e)
        {
        }

        private bool isVolumeSliderValueChangedHandled = false;

        private async void VolumeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                if (Player != null)
                {
                    File.WriteAllText(AppSettingsManager.appSettings.VolumeCachePath, VolumeSlider.Value.ToString());

                    VolumeText.Text = ResourceProvider.GetString("VolumeString") + ": " + VolumeSlider.Value;
                    if (Player != null && Player.MediaPlayer != null)
                    {
                        Player.MediaPlayer.Volume = Convert.ToInt32(VolumeSlider.Value);
                    }

                    
                    if (VolumeSlider.Value == 0)
                    {
                        MuteButton.Source = _muteIcon.Value;
                    }
                    else if (VolumeSlider.Value > 0 && VolumeSlider.Value <= 100)
                    {
                        MuteButton.Source = _volume1Icon.Value;
                    }
                    else if (VolumeSlider.Value > 100 && VolumeSlider.Value <= 200)
                    {
                        MuteButton.Source = _volume2Icon.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }

            if (isVolumeSliderValueChangedHandled) return;
            isVolumeSliderValueChangedHandled = true;
            VolumeText.IsVisible = true;
            await Task.Delay(TimeSpan.FromSeconds(2));
            VolumeText.IsVisible = false;
            isVolumeSliderValueChangedHandled = false;
        }

       


        // Canvas değişkenini RangeCanvas olarak değiştirelim
        private RangeCanvas rangeCanvas;

        private void DurationSlider_OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            Panel rootPanel = e.NameScope.Find<Panel>("PART_RootPanel");
            if (rootPanel != null)
            {
                // RangeCanvas'ı doğrudan rootPanel'e ekleyelim
                rangeCanvas = new RangeCanvas
                {
                    BackgroundColor = new SolidColorBrush(Colors.Transparent),
                    RegionColor = new SolidColorBrush(Color.FromRgb(203, 203, 203)),
                    Height = 5,
                    Width = DurationSlider.Bounds.Width,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    IsVisible = true,
                    IsHitTestVisible = false, // FIXAT: RangeCanvas pointer olaylarını engellememeli
                };
                
                // RangeCanvas'ı doğrudan rootPanel'e ekleyelim
                rootPanel.Children.Insert(1, rangeCanvas);
                
                // DurationSlider'a PointerEntered ve PointerExited olaylarını ekle
                DurationSlider.PointerEntered += DurationSlider_PointerEntered;
                DurationSlider.PointerExited += DurationSlider_PointerExited;
                
                // FIXAT: Bounds değiştiğinde RangeCanvas'ın width'ini güncelle
                _boundsSubscription = DurationSlider.GetObservable(BoundsProperty).Subscribe(bounds =>
                {
                    try
                    {
                        if (rangeCanvas != null && bounds.Width > 0)
                        {
                            rangeCanvas.Width = bounds.Width;
                            
                            // Selection ranges'i de güncelle
                            if (pieceTimeRanges != null && pieceTimeRanges.Count > 0)
                            {
                                UpdateSelectionRanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating RangeCanvas width: {ex.Message}");
                    }
                });
            }
        }

        private bool isPointerOnDurationSlider = false;
        
        bool shouldListenWindowEvents = true;
        
        private void DurationSlider_PointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
            if(isInScaleProcess) return;
            isInScaleProcess = true;
            isPointerOnDurationSlider = true;
            
            // FIXAT: Sadece hover durumunda scale up
            if (!isUp)
            {
                rangeCanvas?.ScaleUp();
                DurationSlider.Classes.Add("ScaledUp");
                isUp = true;
            }
            isInScaleProcess = false;
        }

        private bool isUp = false;
        private void DurationSlider_PointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
            if(isInScaleProcess) return;
            isInScaleProcess = true;
            isPointerOnDurationSlider = false;
            
            // FIXAT: Sadece scale up durumundaysa scale down yap
            if (isUp)
            {
                rangeCanvas?.ScaleDown();
                DurationSlider.Classes.Remove("ScaledUp");
                isUp = false;
            }
            ThumbnailPreviewPopup.IsOpen = false;
            isInScaleProcess = false;
        }

        private void DurationSlider_OnDragStarted(object? sender, VectorEventArgs e)
        {
            myPositionChanging = true;
        }
        private double previousSliderValue = 0;
        private void DurationSlider_OnDragCompleted(object? sender, VectorEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer == null) return;
                myPositionChanging = false;
                if (isCompleted)
                {
                    Player.MediaPlayer.Position = (float)(sender as Slider).Value;
                }
                else
                {
                    double newTimeInSeconds = (sender as Slider).Value * durationInSeconds;

                    var isDownloadedPiece = pieceTimeRanges.Any(range => newTimeInSeconds >= range.Start.TotalSeconds
                                                                         && newTimeInSeconds <= range.End.TotalSeconds);

                    if (!isDownloadedPiece)
                    {
                        isSliderUpdating = true;
                        //DurationSlider.Value = previousSliderValue;
                        Player.MediaPlayer.Position = (float)previousSliderValue;
                        isSliderUpdating = false;
                    }
                    else
                    {
                        //previousSliderValue = (sender as System.Windows.Controls.Slider).Value;
                        Player.MediaPlayer.Position = (float)(sender as Slider).Value;
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private bool shouldListenClickOnPlayer = true;
        private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenClickOnPlayer = false;
        }

        private void InputElement_OnPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenClickOnPlayer = true;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            Close();
        }

        private void ButtonRewind_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            Player.MediaPlayer.Position = Player.MediaPlayer.Position - seconds10;
        }

        private void ButtonForward_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            Player.MediaPlayer.Position = Player.MediaPlayer.Position + seconds10;
        }

        private void VolumePopupTimerOnElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!_isMouseOverVolumePopup && !_isMouseOverButtonMute)
                    {
                        VolumePopup.IsOpen = false;
                        _isVolumePopupOpen = false;
                        // DurationSlider'ı tekrar görünür yap
                        DurationSlider.IsVisible = true;
                    }
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void ButtonMute_OnPointerEntered(object sender, PointerEventArgs e)
        {
            try
            {
                _isMouseOverButtonMute = true;
                if (!_isVolumePopupOpen)
                {
                    // VolumePopup'ı aç
                    VolumePopup.IsOpen = true;
                    _isVolumePopupOpen = true;
                    // DurationSlider'ı gizle
                    DurationSlider.IsVisible = false;
                }
                
                // Timer'ı durdur
                if (_volumePopupTimer.Enabled)
                {
                    _volumePopupTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void ButtonMute_OnPointerExited(object sender, PointerEventArgs e)
        {
            try
            {
                _isMouseOverButtonMute = false;
                // Timer'ı başlat
                _volumePopupTimer.Start();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void VolumePopup_OnPointerEntered(object sender, PointerEventArgs e)
        {
            try
            {
                Console.WriteLine("popup entered");
                _isMouseOverVolumePopup = true;
                // Fare VolumePopup üzerine geldiğinde timer'ı durdur
                if (_volumePopupTimer.Enabled)
                {
                    _volumePopupTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void VolumePopup_OnPointerExited(object sender, PointerEventArgs e)
        {
            try
            {
                Console.WriteLine("popup exited");
                _isMouseOverVolumePopup = false;
                // Timer'ı başlat
                _volumePopupTimer.Start();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }
        
        private void DurationSlider_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            try
            {
                double sliderWidth = DurationSlider.Bounds.Width;
                var mousePosition = e.GetPosition(DurationSlider);
                double relativePosition = mousePosition.X / sliderWidth;

                if (Player.MediaPlayer != null)
                {
                    if (isCompleted)
                    {
                        Player.MediaPlayer.Position = (float)Math.Clamp(relativePosition, 0.0, 1.0);
                    }
                    else
                    {
                        double newTimeInSeconds = relativePosition * durationInSeconds;

                        var isDownloadedPiece = pieceTimeRanges.Any(range => newTimeInSeconds >= range.Start.TotalSeconds
                                                                             && newTimeInSeconds <= range.End.TotalSeconds);

                        if (!isDownloadedPiece)
                        {
                            isSliderUpdating = true;
                            Player.MediaPlayer.Position = (float)Math.Clamp(previousSliderValue, 0.0, 1.0);
                            isSliderUpdating = false;
                        }
                        else
                        {
                            Player.MediaPlayer.Position = (float)Math.Clamp(relativePosition, 0.0, 1.0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

    

        private void ButttonPlay_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
        }

        private void ButttonPlay_OnPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
        }

        private void ButtonRewindPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
        }

        private void ButtonRewindPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
        }

        private void ButtonForward_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
        }

        private void ButtonForward_OnPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
        }

        private void ButtonSubtitle_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
        }

        private void ButtonSubtitle_OnPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
        }

        private void ButtonFullScreen_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
        }

        private void ButtonFullScreen_OnPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
        }

        private bool _isPlaybackSpeedPopupHovered = false;
        private bool _isPlaybackSpeedButtonHovered = false;

        private void ButtonSync_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
            _isPlaybackSpeedButtonHovered = true;
            PlaybackSpeedPopup.IsOpen = true;
            if (DurationSlider != null) DurationSlider.IsVisible = false;
        }

        private async void ButtonSync_OnPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
            _isPlaybackSpeedButtonHovered = false;
            await Task.Delay(200);
            if (!_isPlaybackSpeedPopupHovered && !_isPlaybackSpeedButtonHovered)
            {
                PlaybackSpeedPopup.IsOpen = false;
                if (DurationSlider != null && !VolumePopup.IsOpen && !AudioSubtitlePopup.IsOpen) DurationSlider.IsVisible = true;
            }
        }
        
        private void PlaybackSpeedPopup_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            _isPlaybackSpeedPopupHovered = true;
        }

        private async void PlaybackSpeedPopup_OnPointerExited(object? sender, PointerEventArgs e)
        {
            _isPlaybackSpeedPopupHovered = false;
            await Task.Delay(200);
            if (!_isPlaybackSpeedPopupHovered && !_isPlaybackSpeedButtonHovered)
            {
                PlaybackSpeedPopup.IsOpen = false;
                if (DurationSlider != null && !VolumePopup.IsOpen && !AudioSubtitlePopup.IsOpen) DurationSlider.IsVisible = true;
            }
        }
        

        
        private void Speed05_Pressed(object? sender, PointerPressedEventArgs e) { SetPlaybackSpeed(0.5); }
        private void Speed075_Pressed(object? sender, PointerPressedEventArgs e) { SetPlaybackSpeed(0.75); }
        private void Speed10_Pressed(object? sender, PointerPressedEventArgs e) { SetPlaybackSpeed(1.0); }
        private void Speed125_Pressed(object? sender, PointerPressedEventArgs e) { SetPlaybackSpeed(1.25); }
        private void Speed15_Pressed(object? sender, PointerPressedEventArgs e) { SetPlaybackSpeed(1.5); }

        private void SetPlaybackSpeed(double val)
        {
            if (Player?.MediaPlayer != null)
            {
                Player.MediaPlayer.SetRate((float)val);
                
                Thumb05.Opacity = val == 0.5 ? 1.0 : 0.0;
                Thumb075.Opacity = val == 0.75 ? 1.0 : 0.0;
                Thumb10.Opacity = val == 1.0 ? 1.0 : 0.0;
                Thumb125.Opacity = val == 1.25 ? 1.0 : 0.0;
                Thumb15.Opacity = val == 1.5 ? 1.0 : 0.0;
                
                Text05.Foreground = val == 0.5 ? Avalonia.Media.Brushes.White : Avalonia.Media.Brushes.LightGray;
                Text05.FontWeight = val == 0.5 ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;

                Text075.Foreground = val == 0.75 ? Avalonia.Media.Brushes.White : Avalonia.Media.Brushes.LightGray;
                Text075.FontWeight = val == 0.75 ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;

                Text10.Foreground = val == 1.0 ? Avalonia.Media.Brushes.White : Avalonia.Media.Brushes.LightGray;
                Text10.FontWeight = val == 1.0 ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;

                Text125.Foreground = val == 1.25 ? Avalonia.Media.Brushes.White : Avalonia.Media.Brushes.LightGray;
                Text125.FontWeight = val == 1.25 ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;

                Text15.Foreground = val == 1.5 ? Avalonia.Media.Brushes.White : Avalonia.Media.Brushes.LightGray;
                Text15.FontWeight = val == 1.5 ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
            }
        }

        private async void ButtonSync_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if(currentSubtitle == null) return;
            Console.WriteLine("Started Synchronizing");

            var subtitlePathh = currentSubtitle.Fullpath;
            string moviesFolder = fileInfo.Directory.FullName;
            string movieName = Path.GetFileName(path);

            string newPath = "";
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (movieName.HasSpecialChar())
            {
                newPath = Path.Combine(fileInfo.Directory.FullName,
                    fileName.MakeStringWithoutSpecialChar()) + ".mp4";
                File.Copy(path, newPath, true);
                movieName = Path.GetFileName(newPath);
            }

            if (await SubtitleHandler.SyncSubtitlesAsync(
                    Path.Combine(moviesFolder, movieName), subtitlePathh,
                    subtitlePathh, this))
            {
                currentSubtitle.Synchronized = true;
                if (!String.IsNullOrWhiteSpace(newPath))
                {
                    File.Delete(newPath);
                    Console.WriteLine("File deleted");
                }
            }
            else
            {
                currentSubtitle.Synchronized = false;
            }


            File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                EncryptProvider.AESEncrypt(
                    JsonConvert.SerializeObject(subtitles), Encryptor.Key,
                    Encryptor.IV));

            Console.WriteLine("Finished Synchronizing");
        }
        
        /// <summary>
        /// Handler for mouse inactivity timer - hides controls after 2 seconds of inactivity
        /// </summary>
        private void MouseInactivityTimerOnElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    bool isAnyPopupOpen = (VolumePopup != null && VolumePopup.IsOpen) || 
                                          (AudioSubtitlePopup != null && AudioSubtitlePopup.IsOpen) || 
                                          (PlaybackSpeedPopup != null && PlaybackSpeedPopup.IsOpen) || 
                                          (EpisodeListPopup != null && EpisodeListPopup.IsOpen) || 
                                          (ThumbnailPreviewPopup != null && ThumbnailPreviewPopup.IsOpen);

                    if (shouldCollapseControlPanel && !isFullScreenToggleInProgress && !isAnyPopupOpen)
                    {
                        PanelControlVideo.IsVisible = false;
                        ButtonBack.IsVisible = false;

                        
                        // FIXAT: Torrent progress panelini de gizle
                        if (TorrentProgressPanel != null)
                        {
                            TorrentProgressPanel.IsVisible = false;
                        }
                        
                        // Also hide cursor
                        if(shouldListen)
                            this.Cursor = new Cursor(StandardCursorType.None);
                    }
                    else if (!shouldCollapseControlPanel)
                    {
                        // If we shouldn't collapse, ensure controls are visible (recovery)
                        PanelControlVideo.IsVisible = true;
                        ButtonBack.IsVisible = true;

                        this.Cursor = Cursor.Default;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mouse inactivity timer error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handler for torrent progress timer - updates download progress and speed
        /// </summary>
        private async void TorrentProgressTimerOnTick(object sender, EventArgs e)
        {
            try
            {
                if (torrent == null || isCompleted || string.IsNullOrEmpty(torrent.Hash))
                {
                    _torrentProgressTimer?.Stop();
                    return;
                }
                
                // Get torrent status from libtorrent
                var status = await Libtorrent.GetStatus(torrent.Hash);
                if (status != null)
                {
                    // Calculate streaming readiness progress (first N pieces for playback)
                    // We need only the initial pieces to start streaming, not the entire torrent
                    double streamingReadyProgress = 0;
                    
                    if (status.Pieces != null && status.Pieces.Any())
                    {
                        var piecesArray = status.Pieces.ToArray();
                        
                        // Get file piece range for the current file
                        var filePieceRange = await Libtorrent.GetFilePieceRange(torrent.Hash, fileIndex);
                        
                        if (filePieceRange.StartPieceIndex >= 0 && filePieceRange.EndPieceIndex >= filePieceRange.StartPieceIndex)
                        {
                            int startPiece = filePieceRange.StartPieceIndex;
                            int endPiece = filePieceRange.EndPieceIndex;
                            int fileTotalPieces = endPiece - startPiece + 1;
                            
                            // Calculate how many initial pieces we need for streaming (roughly 5% of file or minimum 10 pieces)
                            int piecesNeededForStreaming = Math.Max(10, (int)(fileTotalPieces * 0.05)); 
                            piecesNeededForStreaming = Math.Min(piecesNeededForStreaming, fileTotalPieces); // don't exceed total
                            
                            // Count how many of the first N pieces of the FILE are downloaded
                            int downloadedInitialPieces = 0;
                            for (int i = 0; i < piecesNeededForStreaming; i++)
                            {
                                int pieceIndex = startPiece + i;
                                if (pieceIndex < piecesArray.Length && piecesArray[pieceIndex])
                                {
                                    downloadedInitialPieces++;
                                }
                            }
                            
                            // Calculate progress percentage for streaming readiness
                            streamingReadyProgress = (double)downloadedInitialPieces / piecesNeededForStreaming * 100.0;
                        }
                        else
                        {
                             // Fallback if file range not found
                             streamingReadyProgress = status.Progress * 100;
                        }
                    }
                    else
                    {
                        // Fallback to total progress if pieces info not available
                        streamingReadyProgress = status.Progress * 100;
                    }
                    
                    _torrentProgress = streamingReadyProgress;

                    // Selection range'leri güncelle (indirilen parçaları slider'da göster)
                    if (rangeCanvas != null && durationInSeconds > 0)
                    {
                        pieceTimeRanges = await Libtorrent.GetAvailaibleSeconds(torrent.Hash, (int)durationInSeconds, fileIndex);
                    }

                    // Update UI with progress and speed info
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            // Selection range'leri UI thread'de güncelle
                            if (rangeCanvas != null && pieceTimeRanges != null && pieceTimeRanges.Count > 0)
                            {
                                Console.WriteLine(pieceTimeRanges.Select(x=> x.End).First().ToString());
                                UpdateSelectionRanges();
                            }

                            // Update loading screen if visible
                            if (ProgressGrid != null && ProgressGrid.IsVisible)
                            {
                                // Update progress text to show percentage and speed
                                if (LoadingTextBlock != null)
                                {
                                    LoadingTextBlock.Text = $"Buffering: {_torrentProgress:F1}% - {status.DownloadSpeedString}";
                                }
                                
                                // Update progress bar value and disable indeterminate mode
                                if (this.FindControl<ProgressBar>("ProgressBarToPlay") is ProgressBar progressBar)
                                {
                                    progressBar.IsIndeterminate = false;
                                    progressBar.Value = _torrentProgress;
                                    
                                    // When initial pieces are ready (100%), hide progress and start playback
                                    if (_torrentProgress >= 100)
                                    {
                                        ProgressGrid.IsVisible = false;
                                        _torrentProgressTimer?.Stop();
                                        
                                        // Start playback if not already playing
                                        if (Player?.MediaPlayer != null && !Player.MediaPlayer.IsPlaying)
                                        {
                                            Player.MediaPlayer.Play();
                                            PlayButton.Source = new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/pause_transparent_266x220.png")));
                                        }
                                    }
                                }
                            }
                            else if (!isCompleted) // Update header display when not loading
                            {
                                // FIXAT: Sadece kontroller görünürken torrent progress'i göster
                                bool shouldShowProgress = PanelControlVideo != null && PanelControlVideo.IsVisible;
                                
                                // Show torrent progress panel in header
                                if (TorrentProgressPanel != null)
                                {
                                    TorrentProgressPanel.IsVisible = shouldShowProgress;
                                    
                                    if (shouldShowProgress)
                                    {
                                        // Update download speed
                                        if (TorrentSpeedText != null)
                                        {
                                            TorrentSpeedText.Text = status.DownloadSpeedString;
                                        }
                                        
                                        // Update overall torrent progress
                                        if (TorrentProgressText != null)
                                        {
                                            TorrentProgressText.Text = $"{(status.Progress * 100):F1}%";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Hide torrent progress panel when completed
                                if (TorrentProgressPanel != null)
                                {
                                    TorrentProgressPanel.IsVisible = false;
                                }
                            }
                        }
                        catch (Exception uiEx)
                        {
                            Console.WriteLine($"Torrent progress UI update error: {uiEx.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Torrent progress timer error: {ex.Message}");
            }
        }
        private void ButtonDubbing_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is Panel panel)
            {
                panel.RenderTransform = new ScaleTransform(1.2, 1.2);
            }
        }

        private void ButtonDubbing_OnPointerExited(object? sender, PointerEventArgs e)
        {
            if (sender is Panel panel)
            {
                panel.RenderTransform = new ScaleTransform(1, 1);
            }
        }

        private void ButtonDubbing_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
               // Not implemented for now

                // Pause the video while dubbing manager is open
                if (Player.MediaPlayer.IsPlaying)
                {
                    Player.MediaPlayer.Pause();
                    PlayButton.Source = new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/play_transparent_266x220.png")));
                }
                
                // Hide controls that might overlap or distract
                ButtonBack.IsVisible = false;

                PanelControlVideo.IsVisible = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening Dubbing Manager: {ex.Message}");
            }
        }
        private void ButtonNextEpisode_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
        }

        private void ButtonNextEpisode_OnPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
        }

        private void ButtonEpisodeList_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            
        }

        private void ButtonEpisodeList_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            
        }

        private bool _isEpisodeListPopupHovered = false;
        private bool _isEpisodeListButtonHovered = false;
        private int _currentViewedSeason = 1;

        private async void ButtonEpisodeList_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = false;
            _isEpisodeListButtonHovered = true;

            if (showType == ShowType.TvShow)
            {
                EpisodeListPopup.IsOpen = true;
                _currentViewedSeason = seasonNumber;
                await LoadSeasonAndEpisodes(_currentViewedSeason);
            }
        }

        private async void ButtonEpisodeList_OnPointerExited(object? sender, PointerEventArgs e)
        {
            shouldListenWindowEvents = true;
            _isEpisodeListButtonHovered = false;
            await Task.Delay(200);
            if (!_isEpisodeListPopupHovered && !_isEpisodeListButtonHovered)
                EpisodeListPopup.IsOpen = false;
        }

        private void EpisodeListPopup_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            _isEpisodeListPopupHovered = true;
        }

        private async void EpisodeListPopup_OnPointerExited(object? sender, PointerEventArgs e)
        {
            _isEpisodeListPopupHovered = false;
            await Task.Delay(200);
            if (!_isEpisodeListPopupHovered && !_isEpisodeListButtonHovered)
                EpisodeListPopup.IsOpen = false;
        }

        private void EpisodeListSeason_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            e.Handled = true;
            if (SeasonListScrollView.IsVisible)
            {
                SeasonListScrollView.IsVisible = false;
                EpisodeListScrollView.IsVisible = true;
                EpisodeListBackButton.IsVisible = true;
            }
            else
            {
                SeasonListScrollView.IsVisible = true;
                EpisodeListScrollView.IsVisible = false;
                EpisodeListBackButton.IsVisible = false;
            }
        }

        private async void SeasonItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control control && control.DataContext is SeasonItem season)
            {
                _currentViewedSeason = season.SeasonNumber;
                await LoadSeasonAndEpisodes(_currentViewedSeason);
            }
        }

        private void EpisodeItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control control && control.DataContext is EpisodeItemUI ep)
            {
                ep.IsExpanded = !ep.IsExpanded;
            }
        }

        private async void EpisodeImage_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control control && control.DataContext is EpisodeItemUI ep)
            {
                if (downloadsFilesPage != null)
                {
                    var fileToPlay = downloadsFilesPage.episodeFiles.FirstOrDefault(x => x.SeasonNumber == _currentViewedSeason && x.EpisodeNumber == ep.EpisodeNumber);
                    if (fileToPlay != null)
                    {
                        var playerWindow = new PlayerWindow(movieId, movieName, showType, _currentViewedSeason, ep.EpisodeNumber,
                            new FileInfo(fileToPlay.FilePath), fileToPlay.IsCompleted, imdbId, torrent, fileToPlay.FileIndex, poster, downloadsFilesPage);
                        MainWindow.Instance.SetContent(playerWindow);
                        
                        if (!fileToPlay.IsCompleted)
                        {
                            await Libtorrent.ChangeEpisodeFileToMaximalPriority(torrent.Hash, _currentViewedSeason, ep.EpisodeNumber);
                        }
                        await Close();
                    }
                }
            }
        }

        private async Task LoadSeasonAndEpisodes(int targetSeasonNumber)
        {
            try
            {
                EpisodeListSeasonText.Text = targetSeasonNumber + ". Sezon";
                
                var tvShow = await Service.client.GetTvShowAsync(movieId, TMDbLib.Objects.TvShows.TvShowMethods.Undefined, Service.language);
                var seasonsList = new List<SeasonItem>();
                foreach(var s in tvShow.Seasons)
                {
                    if (s.SeasonNumber > 0)
                    {
                       seasonsList.Add(new SeasonItem {
                           SeasonNumber = s.SeasonNumber,
                           Name = s.SeasonNumber + ". Sezon",
                           IsSelected = (s.SeasonNumber == targetSeasonNumber)
                       });
                    }
                }
                SeasonItemsControl.ItemsSource = seasonsList;
                
                var seasonDetail = await Service.client.GetTvSeasonAsync(movieId, targetSeasonNumber, TMDbLib.Objects.TvShows.TvSeasonMethods.Undefined, Service.language);
                
                var eps = new List<EpisodeItemUI>();
                foreach(var ep in seasonDetail.Episodes)
                {
                    double progress = 0;
                    var historyItem = FirestoreManager.WatchHistories.FirstOrDefault(x => x.ShowType == ShowType.TvShow && x.Id == movieId && x.SeasonNumber == targetSeasonNumber && x.EpisodeNumber == ep.EpisodeNumber);
                    if (historyItem != null)
                    {
                        progress = historyItem.Progress;
                    }
                    double progressPixelWidth = progress * 80.0;
                    if (progressPixelWidth > 80) progressPixelWidth = 80;
                    if (progressPixelWidth < 0) progressPixelWidth = 0;

                    bool isCurrent = (targetSeasonNumber == this.seasonNumber && ep.EpisodeNumber == this.episodeNumber);
                    
                    eps.Add(new EpisodeItemUI {
                        EpisodeNumber = ep.EpisodeNumber,
                        Name = string.IsNullOrEmpty(ep.Name) ? $"Bölüm {ep.EpisodeNumber}" : ep.Name,
                        Overview = ep.Overview,
                        HasOverview = !string.IsNullOrEmpty(ep.Overview),
                        IsExpanded = isCurrent,
                        BackgroundColor = isCurrent ? "#222222" : "Transparent",
                        ImageUrl = !string.IsNullOrEmpty(ep.StillPath) ? Service.client.GetImageUrl("w500", ep.StillPath).AbsoluteUri : null,
                        ProgressPixelWidth = progressPixelWidth
                    });
                }
                EpisodeItemsControl.ItemsSource = eps;

                EpisodeListScrollView.IsVisible = true;
                SeasonListScrollView.IsVisible = false;
                EpisodeListBackButton.IsVisible = true;
            }
            catch(Exception ex)
            { 
                Console.WriteLine(ex); 
            }
        }

        private bool _isNextEpisodeHoverPopupOpen = false;
        private bool _isNextEpisodeButtonHovered = false;

        private async void ButtonNextEpisode_OnPointerEntered(object? sender, PointerPressedEventArgs e)
        {
            if (showType == ShowType.Movie) return;
            
            _isNextEpisodeButtonHovered = true;
            shouldListenWindowEvents = false;

            if (!NextEpisodeHoverPopup.IsOpen)
            {
                try
                {
                    var tvShow = await Service.client.GetTvShowAsync(movieId, TMDbLib.Objects.TvShows.TvShowMethods.Undefined, Service.language);
                    var seasonDetail = await Service.client.GetTvSeasonAsync(movieId, this.seasonNumber, TMDbLib.Objects.TvShows.TvSeasonMethods.Undefined, Service.language);
                    
                    int targetSeason = this.seasonNumber;
                    int targetEpisode = this.episodeNumber + 1;
                    
                    // Check if current episode is the last in the season
                    if (targetEpisode > seasonDetail.Episodes.Count)
                    {
                        targetSeason++;
                        targetEpisode = 1;
                    }

                    // Check if next season exists
                    var nextSeasonInfo = tvShow.Seasons.FirstOrDefault(s => s.SeasonNumber == targetSeason);
                    if (nextSeasonInfo != null)
                    {
                        var nextSeasonDetail = await Service.client.GetTvSeasonAsync(movieId, targetSeason, TMDbLib.Objects.TvShows.TvSeasonMethods.Undefined, Service.language);
                        var nextEpInfo = nextSeasonDetail.Episodes.FirstOrDefault(e => e.EpisodeNumber == targetEpisode);
                        
                        if (nextEpInfo != null)
                        {
                            NextEpTitleOverlay.Text = nextEpInfo.Name;
                            NextEpDescOverlay.Text = nextEpInfo.Overview;
                            if (!string.IsNullOrEmpty(nextEpInfo.StillPath))
                            {
                                NextEpImageOverlay.Source = Service.client.GetImageUrl("w500", nextEpInfo.StillPath).AbsoluteUri;
                            }
                            NextEpisodeHoverPopup.IsOpen = true;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error fetching next episode: " + ex.Message);
                }
            }
        }

        private async void ButtonNextEpisode_OnPointerExited(object? sender, PointerPressedEventArgs e)
        {
            _isNextEpisodeButtonHovered = false;
            await Task.Delay(200);
            if (!_isNextEpisodeHoverPopupOpen && !_isNextEpisodeButtonHovered && NextEpisodeHoverPopup != null)
            {
                NextEpisodeHoverPopup.IsOpen = false;
                shouldListenWindowEvents = true;
            }
        }

        private void NextEpisodeHoverPopup_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            _isNextEpisodeHoverPopupOpen = true;
            shouldListenWindowEvents = false;
        }

        private async void NextEpisodeHoverPopup_OnPointerExited(object? sender, PointerEventArgs e)
        {
            _isNextEpisodeHoverPopupOpen = false;
            await Task.Delay(200);
            if (!_isNextEpisodeHoverPopupOpen && !_isNextEpisodeButtonHovered && NextEpisodeHoverPopup != null)
            {
                NextEpisodeHoverPopup.IsOpen = false;
                shouldListenWindowEvents = true;
            }
        }
        // --- SUBTITLE SYNC & TRANSLATE IMPLEMENTATION ---
        
        private void ButtonSyncSubtitle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            shouldListen = false;
            SubtitleSyncPopup.IsOpen = true;
            if (currentSubtitle != null && !string.IsNullOrEmpty(currentSubtitle.Fullpath) && File.Exists(currentSubtitle.Fullpath)) 
            {
                InputSubtitlePath.Text = currentSubtitle.Fullpath;
                SourceSubtitleToTranslatePath.Text = currentSubtitle.Fullpath;
            }
            else 
            {
                InputSubtitlePath.Text = "";
                SourceSubtitleToTranslatePath.Text = "";
                SyncLogText.Text = "Lütfen önce oynatıcıdan düzeltilecek veya senkronize edilecek bir altyazı seçin.\n";
            }

            if (TargetLangComboBox.Items.Count == 0)
            {
                var languages = new Dictionary<string, string>
                {
                    { "tr", "Turkish" },
                    { "en", "English" },
                    { "de", "German" },
                    { "es", "Spanish" },
                    { "fr", "French" },
                    { "it", "Italian" },
                    { "pt", "Portuguese" },
                    { "ru", "Russian" },
                    { "ja", "Japanese" },
                    { "ko", "Korean" },
                    { "zh", "Chinese" }
                };

                foreach (var kvp in languages)
                {
                    TargetLangComboBox.Items.Add(new ComboBoxItem { Content = kvp.Value, Tag = kvp.Key });
                }

                var defaultLang = AppSettingsManager.appSettings.IsoSubtitleLanguage ?? "tr";
                for (int i = 0; i < TargetLangComboBox.Items.Count; i++)
                {
                    if (((ComboBoxItem)TargetLangComboBox.Items[i]).Tag.ToString() == defaultLang)
                    {
                        TargetLangComboBox.SelectedIndex = i;
                        break;
                    }
                }
                if (TargetLangComboBox.SelectedIndex == -1 && TargetLangComboBox.Items.Count > 0)
                {
                    TargetLangComboBox.SelectedIndex = 0;
                }
            }
        }
        
        private void CloseSubtitleSyncGrid_Click(object? sender, RoutedEventArgs e)
        {
            shouldListen = true;
            SubtitleSyncPopup.IsOpen = false;
        }

        private void SubtitleSyncGridInner_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            e.Handled = true;
        }

        private async void SelectRefSubtitle_Click(object? sender, RoutedEventArgs e)
        {
             string? result = await PickSubtitleFileAsync("Referans Altyazi Sec");
             if (!string.IsNullOrWhiteSpace(result))
             {
                 RefSubtitlePath.Text = result;
             }
        }
        
        private async void SelectSourceTranslateSubtitle_Click(object? sender, RoutedEventArgs e)
        {
             string? result = await PickSubtitleFileAsync("Cevrilecek Altyaziyi Sec");
             if (!string.IsNullOrWhiteSpace(result))
             {
                 SourceSubtitleToTranslatePath.Text = result;
             }
        }
        
        private async void StartSync_Click(object? sender, RoutedEventArgs e)
        {
            string inputSub = InputSubtitlePath.Text;
            if (string.IsNullOrEmpty(inputSub) || !File.Exists(inputSub)) {
                SyncLogText.Text += "Hatalı bir giriş altyazısı.\n";
                return;
            }
            
            bool useVideo = RefTypeComboBox.SelectedIndex == 0;
            
            string refPath = useVideo ? this.path : RefSubtitlePath.Text;
            if (useVideo)
            {
                if (!String.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    refPath = path;
                }
                else if (!String.IsNullOrWhiteSpace(TorrentStream.FilePath) && File.Exists(TorrentStream.FilePath))
                {
                    refPath = TorrentStream.FilePath;
                }
            }
            else{
                refPath = RefSubtitlePath.Text;
                }
           
            if (string.IsNullOrEmpty(refPath) || !File.Exists(refPath)) {
                SyncLogText.Text += "Referans dosya bulunamadı. Lütfen kontrol edin.\n";
                return;
            }
            
            string outputSub = Path.Combine(Path.GetDirectoryName(inputSub), Path.GetFileNameWithoutExtension(inputSub) + "_SENKRON.srt");
            
            string ffsubsyncExe =  Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffsubsync_wrapper.exe"); // Or whatever if it exists
            
            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "bin", "ffmpeg.exe");
            
            string argumanlar = $"\"{refPath}\" -i \"{inputSub}\" -o \"{outputSub}\" --ffmpeg-path \"{ffmpegPath}\"";
            
            StartSyncButton.IsEnabled = false;
            SubtitleSyncPopup.IsLightDismissEnabled = false;
            SyncProgressBar.IsIndeterminate = true;
            SyncLogText.Text = "Senkronizasyon başlıyor...\nLütfen işlem bitene kadar bekleyin.\n";
            
            await Task.Run(() => {
                try {
                    Process process = new Process();
                    process.StartInfo.FileName = ffsubsyncExe;
                    process.StartInfo.Arguments = argumanlar;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                    process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
                    
                    process.ErrorDataReceived += (s, args) => {
                        if (!string.IsNullOrEmpty(args.Data)) {
                            Dispatcher.UIThread.InvokeAsync(() => {
                                SyncLogText.Text += args.Data + "\n";
                                SyncLogScrollViewer.ScrollToEnd();
                                
                                // Parse progress
                                var match = System.Text.RegularExpressions.Regex.Match(args.Data, @"(\d+)%");
                                if (match.Success && double.TryParse(match.Groups[1].Value, out double progressData))
                                {
                                    double finalProgress = progressData;
                                    SyncProgressBar.IsIndeterminate = false;
                                    SyncProgressBar.Value = finalProgress;
                                }
                            });
                        }
                    };
                    process.OutputDataReceived += (s, args) => {
                        if (!string.IsNullOrEmpty(args.Data)) {
                            Dispatcher.UIThread.InvokeAsync(() => {
                                SyncLogText.Text += args.Data + "\n";
                                SyncLogScrollViewer.ScrollToEnd();
                            });
                        }
                    };
                    
                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.WaitForExit();
                    
                    Dispatcher.UIThread.InvokeAsync(() => {
                        if (process.ExitCode == 0) {
                            SyncLogText.Text += "\n✅ Senkronizasyon Başarılı!\nÇıktı: " + outputSub + "\n";
                            
                            if (File.Exists(outputSub)) {
                                 SyncLogText.Text += "Yeni altyazı videoya eklendi!\n";
                                 
                                 Subtitle newSyncSub = new Subtitle {
                                     Fullpath = outputSub,
                                     Name = Path.GetFileName(outputSub),
                                     Language = AppSettingsManager.appSettings.IsoSubtitleLanguage ?? "tr",
                                     Synchronized = true,
                                     HashDownload = true,
                                     MovieId = movieId,
                                     EpisodeNumber = episodeNumber,
                                     SeasonNumber = seasonNumber
                                 };
                                 PlayerWindow.subtitles.Add(newSyncSub);
                                 currentSubtitle = newSyncSub;
                                 
                                 // Save to subtitleinfo file
                                 try {
                                     File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath,
                                         EncryptProvider.AESEncrypt(
                                             JsonConvert.SerializeObject(subtitles), Encryptor.Key,
                                             Encryptor.IV));
                                 } catch { }

                                 if (mediaPlayer != null)
                                 {
                                     subtitleClickedTime = mediaPlayer.Position;
                                     mediaPlayer.Stop();
                                 }
                                 success = false;
                                 Task.Delay(100).ContinueWith(_ => {
                                     Dispatcher.UIThread.InvokeAsync(() => {
                                         PlayerOnLoaded(this, new RoutedEventArgs());
                                     });
                                 });
                            }
                        } else {
                            SyncLogText.Text += "\n❌ Senkronizasyon Başarısız Oldu!\n";
                        }
                    });
                } catch(Exception ex) {
                   Dispatcher.UIThread.InvokeAsync(() => {
                       SyncLogText.Text += "\n❌ Hata: " + ex.Message + "\n";
                   });
                }
            });
            
            StartSyncButton.IsEnabled = true;
            SyncProgressBar.IsIndeterminate = false;
            SubtitleSyncPopup.IsLightDismissEnabled = true;
            
            // Notification
            NotificationService.Instance.ShowNotification("NetStream", "Altyazı Senkronizasyonu Tamamlandı.", TimeSpan.FromSeconds(4), true);
        }

        private async void StartTranslate_Click(object? sender, RoutedEventArgs e)
        {
            string sourcePath = SourceSubtitleToTranslatePath.Text;
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) {
                TranslateLogText.Text = "Lütfen geçerli bir altyazı seçin.\n";
                return;
            }
            
            StartTranslateButton.IsEnabled = false;
            SubtitleSyncPopup.IsLightDismissEnabled = false;
            TranslateProgressBar.Value = 0;
            TranslateLogText.Text = "Çeviri başlatılıyor...\n";
            
            string targetLang = AppSettingsManager.appSettings.IsoSubtitleLanguage ?? "tr";
            if (TargetLangComboBox.SelectedItem is ComboBoxItem selectedLangItem && selectedLangItem.Tag != null)
            {
                targetLang = selectedLangItem.Tag.ToString();
            }

            string method = "Google Translate (Free)";
            if (TranslateMethodComboBox.SelectedItem is ComboBoxItem selectedMethodItem && selectedMethodItem.Content != null)
            {
                method = selectedMethodItem.Content.ToString();
            }

            string apiKey = TranslateApiKeyTextBox.Text?.Trim() ?? "";

            if (!method.Contains("Google") && string.IsNullOrEmpty(apiKey))
            {
                TranslateLogText.Text += "Hata: Seçilen çeviri yöntemi için API Anahtarı girmeniz gereklidir.\n";
                StartTranslateButton.IsEnabled = true;
                SubtitleSyncPopup.IsLightDismissEnabled = true;
                return;
            }

            string outputFileName = Path.GetFileNameWithoutExtension(sourcePath) + $"_{targetLang.ToUpperInvariant()}_CEVIRI.srt";
            string outputPath = Path.Combine(Path.GetDirectoryName(sourcePath), outputFileName);
            
            var translatorService = new NetStream.Services.SrtTranslatorService();
            translatorService.Method = method;
            translatorService.ApiKey = apiKey;

            translatorService.StatusChanged += (s, msg) => {
                Dispatcher.UIThread.InvokeAsync(() => {
                    TranslateLogText.Text += msg + "\n";
                    TranslateLogScrollViewer.ScrollToEnd();
                });
            };
            
            translatorService.ProgressChanged += (s, prog) => {
                Dispatcher.UIThread.InvokeAsync(() => {
                    TranslateProgressBar.Value = prog;
                });
            };
            
            _translateCts = new CancellationTokenSource();
            string resultPath = await translatorService.TranslateSrtFileAsync(sourcePath, targetLang, outputPath, _translateCts.Token);
            
            if (!string.IsNullOrEmpty(resultPath) && File.Exists(resultPath)) {
                TranslateLogText.Text += "\n✅ Çeviri başarıyla tamamlandı!\n";
                TranslateLogText.Text += "Çevrilen altyazı oynatıcıya eklendi!\n";
                 
                 Subtitle newTranslateSub = new Subtitle {
                     Fullpath = resultPath,
                     Name = Path.GetFileName(resultPath),
                     Language = targetLang,
                     Synchronized = true,
                     MovieId = movieId,
                     EpisodeNumber = episodeNumber,
                     SeasonNumber = seasonNumber
                 };
                 PlayerWindow.subtitles.Add(newTranslateSub);
                 currentSubtitle = newTranslateSub;
                 
                 try {
                     File.WriteAllText(AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(subtitles), Encryptor.Key, Encryptor.IV));
                 } catch { }
                 
                 if (mediaPlayer != null)
                 {
                     subtitleClickedTime = mediaPlayer.Position;
                     mediaPlayer.Stop();
                 }
                 success = false;
                 await Task.Delay(100);
                 PlayerOnLoaded(this, new RoutedEventArgs());
                 
            } else {
                TranslateLogText.Text += "\n❌ Çeviri başarısız/iptal edildi.\n";
            }
            
            StartTranslateButton.IsEnabled = true;
            SubtitleSyncPopup.IsLightDismissEnabled = true;

            NotificationService.Instance.ShowNotification("NetStream", "Altyazı Çevirisi Tamamlandı.", TimeSpan.FromSeconds(4), true);
        }
    }

    public class SubtitleTrackItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public string Details { get; set; }
        public bool HasDetails { get; set; }
        public string TextColor { get; set; }
        public object Tag { get; set; }
    }

    public class SeasonItem
    {
        public int SeasonNumber { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }

    public class EpisodeItemUI : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int EpisodeNumber { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public bool HasOverview { get; set; }
        private bool _isExpanded;
        public bool IsExpanded 
        { 
            get => _isExpanded; 
            set 
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                }
            }
        }
        public string BackgroundColor { get; set; }
        public string ImageUrl { get; set; }
        public double ProgressPixelWidth { get; set; }
    }
} 
