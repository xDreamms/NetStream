using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops.Signatures;
using Vlc.DotNet.Wpf;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using NetStream.Properties;
using NetStream.Views;
using Serilog;
using Path = System.IO.Path;
using Windows.Media.Playback;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using OpenCvSharp;
using Tesseract;
using System.Globalization;
using MaterialDesignThemes.Wpf.Converters;
using System.Reflection;
using TinifyAPI;
using System.Windows.Media.Animation;
using Castle.Components.DictionaryAdapter;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Rect = OpenCvSharp.Rect;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using NETCore.Encrypt;
using Image = System.Windows.Controls.Image;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for PlayerWindow.xaml
    /// </summary>
    public partial class PlayerWindow : System.Windows.Window
    {
        private LibVLC libVlc;
        private LibVLCSharp.Shared.MediaPlayer mediaPlayer;

        const uint ES_CONTINUOUS = 0x80000000;
        const uint ES_DISPLAY_REQUIRED = 0x00000002;
        const uint ES_SYSTEM_REQUIRED = 0x00000001;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SetThreadExecutionState([In] uint esFlags);

        private bool isFullScrenn = false;
        private int movieId;
        private string path;
        private ShowType showType;
        private int seasonNumber;
        private int episodeNumber;
        private string movieName;
        private List<PlayerCache> playerCaches;
        private FileInfo fileInfo;
        public static List<Subtitle> subtitles = new List<Subtitle>();
        private int imdbId;
        private Subtitle currentSubtitle;
        public static List<ThumbnailCache> thumbnailCaches = new List<ThumbnailCache>();

        private Item torrent;
        private int fileIndex;
        private string poster;
        private bool isCompleted;
       
        public PlayerWindow(int movieId, string movieName, ShowType showType, int seasonNumber, int episodeNumber, FileInfo fileInfo, bool isCompleted, int imdbId,Item torrent,int fileIndex,string poster)
        {
            InitializeComponent();
            SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
            this.Title = movieName;
            this.movieId = movieId;
            if (isCompleted)
            {
                path = fileInfo.FullName;
            }
            this.isCompleted = isCompleted;
            this.showType = showType;
            this.seasonNumber = seasonNumber;
            this.episodeNumber = episodeNumber;
            this.movieName = movieName;
            this.imdbId = imdbId;
            this.fileInfo = fileInfo;
            this.torrent = torrent;
            this.fileIndex = fileIndex;
            this.poster = poster;
            Dispatcher.InvokeAsync(() =>
            {
                MovieNameText.Text = GetShowName();
            });
            Log.Information($"Loaded Player Window for: {GetShowName()}");
            WindowsManager.OpenedWindows.Add(this);
            GetThumbnailCaches();
            GetPlayerCaches();
        }

        private DownloadsFilesPage downloadsFilesPage = null;
        public PlayerWindow(int movieId, string movieName, ShowType showType, int seasonNumber, int episodeNumber, FileInfo fileInfo, bool isCompleted, int imdbId, Item torrent, int fileIndex, string poster,DownloadsFilesPage downloadsFilesPage)
        {
            InitializeComponent();
            SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
            this.Title = movieName;
            this.movieId = movieId;
            if (isCompleted)
            {
                path = fileInfo.FullName;
            }
            this.isCompleted = isCompleted;
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
            Dispatcher.InvokeAsync(() =>
            {
                MovieNameText.Text = GetShowName();
            });
            GetThumbnailCaches();
            Log.Information($"Loaded Player Window for: {GetShowName()}");
            WindowsManager.OpenedWindows.Add(this);
            GetPlayerCaches();
        }

        private async void GetPlayerCaches()
        {
            try
            {
                var js = File.ReadAllText(AppSettingsManager.appSettings.PlayerCachePath);
                if (!String.IsNullOrWhiteSpace(js))
                {
                    playerCaches = JsonConvert.DeserializeObject<List<PlayerCache>>(EncryptProvider.AESDecrypt(js,Encryptor.Key,Encryptor.IV));
                }
                else
                {
                    if (FirestoreManager.WatchHistories.Count > 0 && (playerCaches == null || playerCaches.Count == 0))
                    {
                        var result = await FirestoreManager.GetWatchHistory();
                        if (result.Success)
                        {
                            playerCaches = new List<PlayerCache>();
                            foreach (var resultWatchHistory in result.WatchHistories)
                            {
                                PlayerCache playerCache = new PlayerCache
                                {
                                    MovieId = resultWatchHistory.Id,
                                    ShowType = resultWatchHistory.ShowType,
                                    LastPosition = (float)resultWatchHistory.Progress,
                                    SeasonNumber = resultWatchHistory.SeasonNumber,
                                    EpisodeNumber = resultWatchHistory.EpisodeNumber,
                                    DeletedTorrent = resultWatchHistory.DeletedTorrent
                                };
                                playerCaches.Add(playerCache);
                            }

                            await File.WriteAllTextAsync(AppSettingsManager.appSettings.PlayerCachePath,
                                EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(playerCaches), Encryptor.Key, Encryptor.IV));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private Dictionary<int, BitmapImage> thumbnailCache = new Dictionary<int, BitmapImage>();
        private const int MaxCacheSize = 10; // Maksimum thumbnail sayısı

        private void ShowThumbnailPreview(int currentSecond, System.Windows.Controls.Image ThumbnailPreviewImage)
        {
            try
            {
                // Cache'de mevcut bir thumbnail var mı kontrol et
                if (!thumbnailCache.TryGetValue(currentSecond, out BitmapImage bitmap))
                {
                    string thumbnailPath = Path.Combine(_thumbnailsFolder, $"thumb_{currentSecond}.jpg");

                    if (File.Exists(thumbnailPath))
                    {
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(thumbnailPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // Kaynakları serbest bırakmak için
                        bitmap.EndInit();

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
                Log.Error(errorMessage);
            }
        }
        private void AddToCache(int key, BitmapImage bitmap)
        {
            // Eğer cache doluysa en eski öğeyi kaldır
            try
            {
                if (thumbnailCache.Count >= MaxCacheSize)
                {
                    var firstKey = thumbnailCache.Keys.First();
                    thumbnailCache[firstKey].StreamSource?.Dispose();
                    thumbnailCache.Remove(firstKey);
                }

                thumbnailCache[key] = bitmap;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
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


        private async void GetSubtitles()
        {
            try
            {
                // Dosyayı asenkron olarak oku
                string s = File.ReadAllText(AppSettingsManager.appSettings.SubtitleInfoPath);

                if (!string.IsNullOrWhiteSpace(s))
                {
                    // Şifreyi çöz
                    string decryptedData = EncryptProvider.AESDecrypt(s,Encryptor.Key,Encryptor.IV);

                    // Decrypted veriyi deserialize et
                    if (!string.IsNullOrWhiteSpace(decryptedData))
                    {
                        subtitles = JsonConvert.DeserializeObject<List<Subtitle>>(decryptedData);
                        Log.Information("Loaded subtitles.");
                    }
                    else
                    {
                        Log.Error("Decrypted data is empty or invalid.");
                    }
                }
                else
                {
                    Log.Error("Subtitle file is empty or invalid.");
                }
            }
            catch (FormatException fe)
            {
                Log.Error("Error on Subtitle decryption: The input is not a valid Base-64 string. " + fe.Message);
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public static async void GetThumbnailCaches()
        {
            try
            {
                var s =  File.ReadAllText(AppSettingsManager.appSettings.ThumbnailCachesPath);
                if (!String.IsNullOrWhiteSpace(s))
                {
                    thumbnailCaches = JsonConvert.DeserializeObject<List<ThumbnailCache>>(s);
                    Log.Information("Loaded thumbnail caches.");
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

    
        private void PlayerWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Core.Initialize();
                libVlc = new LibVLC();
                mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(libVlc);
                Player.Loaded+= PlayerOnLoaded;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void OnUnloaded(object sender, RoutedEventArgs e)
        {
            
        }

        public static async Task<long> GetVideoDurationMillisecondsAsync(string videoPath)
        {
            // ffprobe komut satırı argümanları:
            // -v error -> Sadece hata mesajlarını gösterir.
            // -show_entries format=duration -> Sadece duration bilgisini döndürür.
            // -of default=noprint_wrappers=1:nokey=1 -> Çıktıda anahtar isimlerini bastırır, yalnızca değeri verir.
            try
            {
                string arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"";
                string ffmpegPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin", "ffprobe.exe");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,   // ffprobe.exe'nin tam yolu
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,     // Konsol penceresi açılmasın diye
                    CreateNoWindow = true        // Konsol penceresi açılmasın diye
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
                    if (double.TryParse(output, NumberStyles.Float, CultureInfo.InvariantCulture, out double durationSeconds))
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
                Log.Error(errorMessage);
                return 0;
            }
        }


        bool success = false;
        private PlayerCache currentPlayerCache;
        private TorrentStream torrentStream;
        private async void PlayerOnLoaded(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                ProgressGrid.Visibility = Visibility.Visible;
                LoadingTextBlock.Visibility = Visibility.Visible;
                ProgressBarToPlay.IsIndeterminate = true;
                ProgressBarToPlay.Visibility = Visibility.Visible;
                NextEpisodeStackPanel.Visibility = Visibility.Collapsed;
                PanelControlVideo.Visibility = Visibility.Collapsed;

                //while (!(await Libtorrent.IsPiecesReadyFile(torrent.Hash, fileIndex)))
                //{
                //    //wait
                //}

                
            }


            Player.MediaPlayer = mediaPlayer;
            Player.MouseMove -= Player_OnMouseMove;
            Player.MouseMove += Player_OnMouseMove;

            //Get Saved Volume
            double volumeValue;
            double.TryParse(await File.ReadAllTextAsync(AppSettingsManager.appSettings.VolumeCachePath), out volumeValue);
            VolumeSlider.Value = volumeValue;

            string subtitlePath = await GetSubtitle();
            

            while (!success)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(subtitlePath))
                    {
                        Log.Information("Subtitle couldnt be found");

                        //foreach (var srt in fileInfo.Directory.GetFiles())
                        //{
                        //    var extension = Path.GetExtension(srt.FullName).ToLower();
                        //    if (extension == ".srt" || extension == ".vtt" || extension == ".sub")
                        //    {
                        //        srt.Delete();
                        //    }
                        //}
                        if(closed) break;
                        torrentStream = await TorrentStream.Create(torrent, fileIndex);
                        var media = new Media(libVlc,
                            new StreamMediaInput(torrentStream));

                        await media.Parse(MediaParseOptions.ParseNetwork | MediaParseOptions.FetchLocal);
                        media.ParsedChanged +=async (sender, e) =>
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
                        Log.Information("Playing media file without subtitles.");
                        
                    }
                    else
                    {
                        //foreach (var srt in fileInfo.Directory.GetFiles())
                        //{
                        //    var extension = Path.GetExtension(srt.FullName).ToLower();
                        //    if (extension == ".srt" || extension == ".vtt" || extension == ".sub")
                        //    {
                        //        srt.Delete();
                        //    }
                        //}
                        if (closed) break;
                        torrentStream = await TorrentStream.Create(torrent, fileIndex);
                        var media = new Media(libVlc,
                            new StreamMediaInput(torrentStream),
                            new[] { "sub-file=" + subtitlePath });

                        await media.Parse(MediaParseOptions.ParseNetwork | MediaParseOptions.FetchLocal);
                        media.ParsedChanged +=async (sender, e) =>
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
                        Log.Information("Playing media file with subtitle");
                        
                    }

                    if (closed) break;
                    Player.MediaPlayer.EnableKeyInput = false;
                    Player.MediaPlayer.EnableMouseInput = false;

                    if (closed) break;
                    if (currentPlayerCache != null)
                    {
                        Player.MediaPlayer.Position = currentPlayerCache.LastPosition;
                    }
                    else if (subtitleClickedTime.HasValue)
                    {
                        Player.MediaPlayer.Position = subtitleClickedTime.Value;
                    }
                    else
                    {

                        if (playerCaches != null)
                        {
                            if (playerCaches.Any(x =>
                                    x.MovieId == movieId && x.ShowType == showType &&
                                    x.EpisodeNumber == episodeNumber && x.SeasonNumber == seasonNumber))
                            {
                                currentPlayerCache = playerCaches.FirstOrDefault(x => x.MovieId == movieId &&
                                                                                   x.ShowType == showType &&
                                                                                   x.EpisodeNumber == episodeNumber &&
                                                                                   x.SeasonNumber == seasonNumber);

                                Player.MediaPlayer.Position =
                                    currentPlayerCache.DeletedTorrent || currentPlayerCache.LastPosition == 1
                                        ? 0
                                        : (float)currentPlayerCache.LastPosition;
                                if (currentPlayerCache.DeletedTorrent)
                                {
                                    await FirestoreManager.EditWatchHistory(new EditWatchHistoryRequest
                                    {
                                        Email = AppSettingsManager.appSettings.FireStoreEmail,
                                        Id = movieId,
                                        ShowType = showType,
                                        SeasonNumber = seasonNumber,
                                        EpisodeNumber = episodeNumber,
                                        NewProgress = currentPlayerCache.LastPosition,
                                        DeletedTorrent = false,
                                        Hash = torrent.Hash
                                    });
                                    currentPlayerCache.DeletedTorrent = false;
                                    File.WriteAllText(AppSettingsManager.appSettings.PlayerCachePath,
                                        EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(playerCaches), Encryptor.Key, Encryptor.IV));
                                }
                            }
                            else
                            {
                                var x = new PlayerCache
                                {
                                    MovieId = movieId,
                                    ShowType = showType,
                                    LastPosition = 0,
                                    SeasonNumber = seasonNumber,
                                    EpisodeNumber = episodeNumber,
                                    DeletedTorrent = false
                                };
                                playerCaches.Add(x);
                                File.WriteAllText(AppSettingsManager.appSettings.PlayerCachePath,
                                    EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(playerCaches), Encryptor.Key, Encryptor.IV));
                            }
                        }
                        else
                        {
                            playerCaches = new List<PlayerCache>();
                            var x = new PlayerCache
                            {
                                MovieId = movieId,
                                ShowType = showType,
                                LastPosition = 0,
                                SeasonNumber = seasonNumber,
                                EpisodeNumber = episodeNumber,
                                DeletedTorrent = false
                            };
                            playerCaches.Add(x);
                            File.WriteAllText(AppSettingsManager.appSettings.PlayerCachePath,
                                EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(playerCaches), Encryptor.Key, Encryptor.IV));
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
                    if (String.IsNullOrWhiteSpace(path) && await Libtorrent.GetTorrentState(torrent) != TorrentState.Downloading)
                    {
                        await Libtorrent.Resume(torrent);
                    }
                    if (closed) break;
                    success = true;
                    if (closed) break;
                    if (!isCompleted && AppSettingsManager.appSettings.PlayerSettingShowThumbnail)
                    {
                        checkIsCompletedTimer = new System.Timers.Timer(10000); 
                        checkIsCompletedTimer.Elapsed += CheckIsCompletedTimerOnElapsed;
                        checkIsCompletedTimer.AutoReset = true;
                        checkIsCompletedTimer.Enabled = true;
                    }
                }
                catch (Exception exception)
                {
                    var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                    Log.Error(errorMessage);
                    if (exception.Message.ToLower().Contains("the process cannot access the file"))
                    {
                        success = false;
                        if (String.IsNullOrWhiteSpace(path) && await Libtorrent.GetTorrentState(torrent) == TorrentState.Downloading)
                        {
                            await Libtorrent.Pause(torrent);
                        }
                    }
                    else
                    {
                        success = true;
                    }
                }
            }
        }

        //private void OnMediaStarted()
        //{
        //    Task.Delay(1000).ContinueWith(t =>
        //    {
        //        if (!lengthUpdated)
        //        {
        //            StartLengthPolling();
        //        }
        //    }, TaskScheduler.FromCurrentSynchronizationContext());
        //}

        //private void StartLengthPolling()
        //{
        //    // Eğer zaten güncellendiyse polling başlatmaya gerek yok
        //    if (lengthUpdated)
        //        return;

        //    lengthPollTimer = new DispatcherTimer();
        //    lengthPollTimer.Interval = TimeSpan.FromMilliseconds(500);
        //    lengthPollTimer.Tick += async (s, e) =>
        //    {
        //        try
        //        {
        //            if (closed)
        //            {
        //                lengthPollTimer.Stop();
        //                return;
        //            }
        //            var media = mediaPlayer.Media;
        //            if (media == null)
        //                return;

        //            var status =  await media.Parse(MediaParseOptions.ParseNetwork | MediaParseOptions.FetchLocal);
        //            media.ParsedChanged += (sender, e) =>
        //            {
        //                if (e.ParsedStatus == MediaParsedStatus.Done)
        //                {
        //                    long durationMilliseconds = media.Duration;
        //                    Console.WriteLine(durationMilliseconds);
        //                    if (durationMilliseconds > 0)
        //                    {
        //                        UpdateDuration(durationMilliseconds);
        //                        lengthUpdated = true;
        //                        lengthPollTimer.Stop();
        //                    }
        //                }
        //            };
                   
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Diagnostics.Debug.WriteLine("Length polling hata: " + ex.Message);
        //        }
        //    };
        //    lengthPollTimer.Start();
        //}

        private void MediaPlayerOnPaused(object? sender, EventArgs e)
        {
            SetThreadExecutionState(ES_CONTINUOUS);
            setThread = false;
        }

        
        private async Task<string> GetSubtitle()
        {
            GetSubtitles();
            try
            {
                if (subtitles.Any(x =>
                        x.MovieId == movieId && x.EpisodeNumber == episodeNumber &&
                        x.SeasonNumber == this.seasonNumber && !String.IsNullOrWhiteSpace(x.Fullpath)
                        && x.Language == AppSettingsManager.appSettings.IsoSubtitleLanguage))
                {
                    var subtitle = subtitles.FirstOrDefault(x =>
                        x.MovieId == movieId && x.EpisodeNumber == episodeNumber &&
                        x.SeasonNumber == this.seasonNumber &&
                        x.Language == AppSettingsManager.appSettings.IsoSubtitleLanguage);

                    if (subtitle.Synchronized)
                    {
                        currentSubtitle = subtitle;
                        Log.Information("Subtitle has been found and sycnhronized. Path: " + subtitle.Fullpath);
                        return subtitle.Fullpath;
                    }
                    else
                    {
                        if (!String.IsNullOrWhiteSpace(path) && AppSettingsManager.appSettings.PlayerSettingAutoSync)
                        {
                            Log.Information(
                                "Subtitle has been found for completed video file. But it is not synchronized.");
                            Log.Information("Started Synchronizing process");
                            var subtitlePathh = subtitle.Fullpath;
                            string moviesFolder = fileInfo.Directory.FullName;
                            string movieName = Path.GetFileName(path);
                            string newPath = "";
                            if (movieName.HasSpecialChar())
                            {
                                string fileName = Path.GetFileNameWithoutExtension(path);
                                newPath = Path.Combine(fileInfo.Directory.FullName,
                                    fileName.MakeStringWithoutSpecialChar()) + ".mp4";
                                File.Copy(path, newPath, true);
                                movieName = Path.GetFileName(newPath);
                            }

                            if (await SubtitleHandler.SyncSubtitlesAsync(Path.Combine(moviesFolder, movieName), subtitlePathh,
                                    subtitlePathh, this))
                            {
                                subtitle.Synchronized = true;
                                if (!String.IsNullOrWhiteSpace(newPath))
                                {
                                    Log.Information("File deleted");
                                    File.Delete(newPath);
                                }

                                File.WriteAllText(NetStream.AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));
                            }
                            currentSubtitle = subtitle;
                            Log.Information("Finished Synchronizing.");
                            return subtitle.Fullpath;
                        }
                        else
                        {
                            currentSubtitle = subtitle;
                            Log.Information(
                                "Subtitle has been found for not completed video file. It cant be synchronized");
                            Log.Information("Set subtitle path");
                            return subtitle.Fullpath;
                        }
                    }
                }
                else
                {
                    Log.Information("Subtitle hasnt been found.");
                    if (String.IsNullOrWhiteSpace(path))
                    {
                        Log.Information("Started Search subtitle for Not completed video file");
                        if (showType == ShowType.Movie)
                        {
                            Subtitle subtitle = await SubtitleHandler.GetSubtitlesByTMDbId(movieName, movieId,
                                new List<string>() { AppSettingsManager.appSettings.IsoSubtitleLanguage });

                            if (subtitle != null && !String.IsNullOrWhiteSpace(subtitle.Fullpath))
                            {
                                Log.Information("[OpenSubtitles.com] Movie subtitle found. Subtitle path: " + subtitle.Fullpath);
                                subtitle.MovieId = movieId;
                                subtitle.EpisodeNumber = episodeNumber;
                                subtitle.SeasonNumber = seasonNumber;
                                subtitle.Name = Path.GetFileName(subtitle.Fullpath);
                                subtitles.Add(subtitle);
                                File.WriteAllText(NetStream.AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));
                                currentSubtitle = subtitle;
                                return subtitle.Fullpath;
                            }
                            else
                            {
                                var mov = await Service.client.GetMovieAsync(movieId);
                                int year = mov.ReleaseDate.HasValue ? mov.ReleaseDate.Value.Year : 0;

                                var newSubtitle = await SubtitleHandler.SelectAndDownloadSubtitle(showType, movieName, movieId,
                                    year, seasonNumber, episodeNumber, AppSettingsManager.appSettings.IsoSubtitleLanguage,
                                    "tt" + imdbId);

                                if (newSubtitle != null && !String.IsNullOrWhiteSpace(newSubtitle.Fullpath))
                                {
                                    Log.Information("[OpenSubtitles.Org] Movie subtitle found. Subtitle path: " + newSubtitle.Fullpath);
                                    newSubtitle.MovieId = movieId;
                                    newSubtitle.EpisodeNumber = episodeNumber;
                                    newSubtitle.SeasonNumber = seasonNumber;
                                    newSubtitle.Name = Path.GetFileName(newSubtitle.Fullpath);
                                    subtitles.Add(newSubtitle);
                                    File.WriteAllText(NetStream.AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));
                                    currentSubtitle = newSubtitle;
                                    return newSubtitle.Fullpath;
                                }
                            }
                        }
                        else
                        {
                            Subtitle subtitle = await SubtitleHandler.GetSubtitlesByNameAndEpisode(movieName, seasonNumber,
                                episodeNumber, new List<string>() { AppSettingsManager.appSettings.IsoSubtitleLanguage },
                                imdbId);
                            if (subtitle != null && !String.IsNullOrWhiteSpace(subtitle.Fullpath))
                            {
                                Log.Information("TvShow subtitle found. Subtitle path: " + subtitle.Fullpath);
                                subtitle.MovieId = movieId;
                                subtitle.EpisodeNumber = episodeNumber;
                                subtitle.SeasonNumber = seasonNumber;
                                subtitle.Name = Path.GetFileName(subtitle.Fullpath);
                                subtitles.Add(subtitle);
                                File.WriteAllText(NetStream.AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));
                                currentSubtitle = subtitle;
                                return subtitle.Fullpath;
                            }
                            else
                            {
                                var mov = await Service.client.GetTvShowAsync(movieId);
                                int year = mov.FirstAirDate.Value.Year;

                                var newSubtitle = await SubtitleHandler.SelectAndDownloadSubtitle(showType, movieName, movieId,
                                    year, seasonNumber, episodeNumber, AppSettingsManager.appSettings.IsoSubtitleLanguage,
                                    "tt" + imdbId);


                                if (newSubtitle != null && !String.IsNullOrWhiteSpace(newSubtitle.Fullpath))
                                {
                                    Log.Information("TvShow subtitle found. Subtitle path: " + newSubtitle.Fullpath);
                                    newSubtitle.MovieId = movieId;
                                    newSubtitle.EpisodeNumber = episodeNumber;
                                    newSubtitle.SeasonNumber = seasonNumber;
                                    newSubtitle.Name = Path.GetFileName(newSubtitle.Fullpath);
                                    subtitles.Add(newSubtitle);
                                    File.WriteAllText(NetStream.AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));
                                    currentSubtitle = newSubtitle;
                                    return newSubtitle.Fullpath;
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.Information("Started Search subtitle for completed video file");
                        Subtitle subtitle = await SubtitleHandler.GetSubtitlesByHash(path,
                            new List<string>() { AppSettingsManager.appSettings.IsoSubtitleLanguage }, movieName);

                        if (subtitle != null && !String.IsNullOrWhiteSpace(subtitle.Fullpath))
                        {
                            Log.Information("Subtitle has been found. Subtitle path: " + subtitle.Fullpath);
                            subtitle.MovieId = movieId;
                            subtitle.EpisodeNumber = episodeNumber;
                            subtitle.SeasonNumber = seasonNumber;
                            subtitle.Name = Path.GetFileName(subtitle.Fullpath);
                            subtitle.HashDownload = true;

                            if (AppSettingsManager.appSettings.PlayerSettingAutoSync)
                            {
                                Log.Information("Started Synchronizing");

                                var subtitlePathh = subtitle.Fullpath;
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

                                if (await SubtitleHandler.SyncSubtitlesAsync(Path.Combine(moviesFolder, movieName), subtitlePathh,
                                        subtitlePathh, this))
                                {
                                    subtitle.Synchronized = true;
                                    if (!String.IsNullOrWhiteSpace(newPath))
                                    {
                                        File.Delete(newPath);
                                        Log.Information("File deleted");
                                    }
                                }
                                else
                                {
                                    subtitle.Synchronized = false;
                                }
                            }
                           

                            subtitles.Add(subtitle);
                            File.WriteAllText(NetStream.AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));

                            currentSubtitle = subtitle;
                            Log.Information("Finished Synchronizing");
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
                                    Log.Information("Movie subtitle found. Subtitle path: " + newSubtitle.Fullpath);
                                    newSubtitle.MovieId = movieId;
                                    newSubtitle.EpisodeNumber = episodeNumber;
                                    newSubtitle.SeasonNumber = seasonNumber;
                                    newSubtitle.Name = Path.GetFileName(newSubtitle.Fullpath);

                                    if (AppSettingsManager.appSettings.PlayerSettingAutoSync)
                                    {
                                        Log.Information("Started Synchronizing");

                                        var subtitlePathh = newSubtitle.Fullpath;
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

                                        if (await SubtitleHandler.SyncSubtitlesAsync(Path.Combine(moviesFolder, movieName), subtitlePathh,
                                                subtitlePathh, this))
                                        {
                                            newSubtitle.Synchronized = true;
                                            if (!String.IsNullOrWhiteSpace(newPath))
                                            {
                                                File.Delete(newPath);
                                                Log.Information("File deleted");
                                            }
                                        }
                                        else
                                        {
                                            newSubtitle.Synchronized = false;
                                        }
                                    }
                                    
                                    subtitles.Add(newSubtitle);
                                    File.WriteAllText(NetStream.AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));

                                    currentSubtitle = newSubtitle;
                                    Log.Information("Finished Synchronizing");
                                    return newSubtitle.Fullpath;
                                }
                                else
                                {
                                    var mov = await Service.client.GetMovieAsync(movieId);
                                    int year = mov.ReleaseDate.HasValue ? mov.ReleaseDate.Value.Year : 0;

                                    var newSubtitle2 = await SubtitleHandler.SelectAndDownloadSubtitle(showType, movieName, movieId,
                                        year, seasonNumber, episodeNumber, AppSettingsManager.appSettings.IsoSubtitleLanguage,
                                        "tt" + imdbId);

                                    if (newSubtitle2 != null && !String.IsNullOrWhiteSpace(newSubtitle2.Fullpath))
                                    {
                                        Log.Information("Movie subtitle found. Subtitle path: " + newSubtitle2.Fullpath);
                                        newSubtitle2.MovieId = movieId;
                                        newSubtitle2.EpisodeNumber = episodeNumber;
                                        newSubtitle2.SeasonNumber = seasonNumber;
                                        newSubtitle2.Name = Path.GetFileName(newSubtitle2.Fullpath);

                                        if (AppSettingsManager.appSettings.PlayerSettingAutoSync)
                                        {
                                            Log.Information("Started Synchronizing");

                                            var subtitlePathh = newSubtitle2.Fullpath;
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

                                            if (await SubtitleHandler.SyncSubtitlesAsync(Path.Combine(moviesFolder, movieName), subtitlePathh,
                                                    subtitlePathh, this))
                                            {
                                                newSubtitle2.Synchronized = true;
                                                if (!String.IsNullOrWhiteSpace(newPath))
                                                {
                                                    File.Delete(newPath);
                                                    Log.Information("File deleted");
                                                }
                                            }
                                            else
                                            {
                                                newSubtitle2.Synchronized = false;
                                            }
                                        }

                                        subtitles.Add(newSubtitle2);
                                        File.WriteAllText(NetStream.AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));

                                        currentSubtitle = newSubtitle2;
                                        Log.Information("Finished Synchronizing");
                                        return newSubtitle2.Fullpath;
                                    }
                                }
                            }
                            else
                            {
                                var newsubtitle3 = await SubtitleHandler.GetSubtitlesByNameAndEpisode(movieName, seasonNumber,
                                    episodeNumber, new List<string>() { AppSettingsManager.appSettings.IsoSubtitleLanguage },
                                    imdbId);
                                if (newsubtitle3 != null && !String.IsNullOrWhiteSpace(newsubtitle3.Fullpath))
                                {
                                    Log.Information("TvShow subtitle found. Subtitle path: " + newsubtitle3.Fullpath);
                                    newsubtitle3.MovieId = movieId;
                                    newsubtitle3.EpisodeNumber = episodeNumber;
                                    newsubtitle3.SeasonNumber = seasonNumber;
                                    newsubtitle3.Name = Path.GetFileName(newsubtitle3.Fullpath);

                                    if (AppSettingsManager.appSettings.PlayerSettingAutoSync)
                                    {
                                        Log.Information("Started Synchronizing");

                                        var subtitlePathh = newsubtitle3.Fullpath;
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
                                            newsubtitle3.Synchronized = true;
                                            if (!String.IsNullOrWhiteSpace(newPath))
                                            {
                                                File.Delete(newPath);
                                                Log.Information("File deleted");
                                            }
                                        }
                                        else
                                        {
                                            newsubtitle3.Synchronized = false;
                                        }
                                    }

                                    subtitles.Add(newsubtitle3);
                                    File.WriteAllText(NetStream.AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));

                                    currentSubtitle = newsubtitle3;
                                    Log.Information("Finished Synchronizing");
                                    return newsubtitle3.Fullpath;
                                }
                                else
                                {
                                    var mov = await Service.client.GetTvShowAsync(movieId);
                                    int year = mov.FirstAirDate.Value.Year;
                                    
                                    var newsubtitle4 = await SubtitleHandler.SelectAndDownloadSubtitle(showType, movieName, movieId,
                                        year, seasonNumber, episodeNumber, AppSettingsManager.appSettings.IsoSubtitleLanguage,
                                        "tt" + imdbId);


                                    if (newsubtitle4 != null && !String.IsNullOrWhiteSpace(newsubtitle4.Fullpath))
                                    {
                                        Log.Information("TvShow subtitle found. Subtitle path: " + newsubtitle4.Fullpath);
                                        newsubtitle4.MovieId = movieId;
                                        newsubtitle4.EpisodeNumber = episodeNumber;
                                        newsubtitle4.SeasonNumber = seasonNumber;
                                        newsubtitle4.Name = Path.GetFileName(newsubtitle4.Fullpath);

                                        if (AppSettingsManager.appSettings.PlayerSettingAutoSync)
                                        {
                                            Log.Information("Started Synchronizing");

                                            var subtitlePathh = newsubtitle4.Fullpath;
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
                                                newsubtitle4.Synchronized = true;
                                                if (!String.IsNullOrWhiteSpace(newPath))
                                                {
                                                    File.Delete(newPath);
                                                    Log.Information("File deleted");
                                                }
                                            }
                                            else
                                            {
                                                newsubtitle4.Synchronized = false;
                                            }
                                        }

                                        subtitles.Add(newsubtitle4);
                                        File.WriteAllText(NetStream.AppSettingsManager.appSettings.SubtitleInfoPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(PlayerWindow.subtitles), Encryptor.Key, Encryptor.IV));

                                        currentSubtitle = newsubtitle4;
                                        Log.Information("Finished Synchronizing");
                                        return newsubtitle4.Fullpath;
                                    }
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

            return "";
        }

        private CancellationTokenSource cts = new CancellationTokenSource();
        private async void CheckIsCompletedTimerOnElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if(checkIsCompletedTimer != null)
                    checkIsCompletedTimer.Stop();

                var torrentFiles = await Libtorrent.GetFiles(torrent);
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

                            if (!Directory.Exists(_thumbnailsFolder) ||
                                Directory.GetFiles(_thumbnailsFolder, "*.jpg").Length < durationInSeconds)
                            {
                                if(!Directory.Exists(_thumbnailsFolder))
                                    Directory.CreateDirectory(_thumbnailsFolder);
                                await GenerateThumbnailsAsync(fullPath, _thumbnailsFolder,cts.Token);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
            finally
            {
                if (!isCompleted && !startedGetThumbnails && checkIsCompletedTimer != null)
                {
                    checkIsCompletedTimer.Start();
                }
            }
        }

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
                Log.Error(errorMessage);
            }
        }

        //private bool resumed = false;
        bool addedToHistory = false;
        private bool setThread = true;
        private void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            if (!setThread)
            {
                SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
                setThread = true;
            }
        }
        bool isEndReached = false;
        private async void MediaPlayerOnEndReached(object? sender, EventArgs e)
        {
            try
            {
                if(DurationSlider.Value < 0.90) return;
                isEndReached = true;
                if (downloadsFilesPage == null)
                {
                    if (Player.MediaPlayer != null)
                    {
                        await this.Dispatcher.InvokeAsync(() =>
                        {
                            Player.MediaPlayer.Stop();
                            Player.MediaPlayer.Position = 0;  // Videoyu baştan başlat
                            Player.MediaPlayer.Play();
                        });
                    }
                }
                else
                {
                    await this.Dispatcher.InvokeAsync(async () =>
                    {
                        lastPos = 1;
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
                        }
                    });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        private void MediaPlayerOnBuffering(object? sender, MediaPlayerBufferingEventArgs e)
        {
            
        }

        private void MediaPlayerOnStopped(object? sender, EventArgs e)
        {
            
        }

        private async void SaveCurrentPlayerPosition()
        {
            try
            {
                if (playerCaches == null || playerCaches.Count == 0)
                {
                    playerCaches = new List<PlayerCache>();

                    PlayerCache playerCache = new PlayerCache();
                    playerCache.MovieId = movieId;
                    playerCache.LastPosition = lastPos;
                    playerCache.SeasonNumber = this.seasonNumber;
                    playerCache.EpisodeNumber = this.episodeNumber;
                    playerCache.ShowType = showType;
                    playerCache.DeletedTorrent = false;
                    
                    playerCaches.Add(playerCache);
                    File.WriteAllText(AppSettingsManager.appSettings.PlayerCachePath,EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(playerCaches),Encryptor.Key,Encryptor.IV));
                }
                else
                {
                    if (playerCaches.Any(x =>
                            x.MovieId == movieId && x.ShowType == showType && x.SeasonNumber == seasonNumber &&
                            x.EpisodeNumber == episodeNumber))
                    {
                        var currentCache = playerCaches.FirstOrDefault(x =>
                            x.MovieId == movieId && x.ShowType == showType && x.SeasonNumber == seasonNumber &&
                            x.EpisodeNumber == episodeNumber);

                        currentCache.LastPosition = lastPos;
                        File.WriteAllText(AppSettingsManager.appSettings.PlayerCachePath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(playerCaches), Encryptor.Key, Encryptor.IV));
                    }
                    else
                    {
                        PlayerCache playerCache = new PlayerCache();

                        playerCache.MovieId = movieId;
                        playerCache.LastPosition = lastPos;
                        playerCache.SeasonNumber = 0;
                        playerCache.EpisodeNumber = 0;
                        playerCache.ShowType = showType;
                        playerCache.DeletedTorrent = false;

                        playerCaches.Add(playerCache);
                        File.WriteAllText(AppSettingsManager.appSettings.PlayerCachePath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(playerCaches), Encryptor.Key, Encryptor.IV));
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private async void MediaPlayerOnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
        {
            try
            {
                if (myPositionChanging || isSliderUpdating)
                {
                    return;
                }

                await this.Dispatcher.Invoke(async () =>
                {
                    DurationSlider.Value = e.Position;
                    lastPos = e.Position;
                });
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private DateTime _lastSnapshotTime = DateTime.MinValue;
        string snapshotPath = "snapshot.jpg";
        private async void MediaPlayerOnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            try
            {
               await this.Dispatcher.InvokeAsync(async() =>
                {
                    TxtCurrentTime.Text = TimeSpan.FromMilliseconds(e.Time).ToString().Substring(0, 8) + "/";

                    //if (downloadsFilesPage != null)
                    //{
                    //    var currentEpisode = downloadsFilesPage.FilesDisplay.SelectedItem as EpisodeFile;
                    //    if (currentEpisode != null && currentEpisode.FileIndex != downloadsFilesPage.episodeFiles.Last().FileIndex)
                    //    {
                    //        long currentTime = e.Time; // ms

                    //        if (currentTime > duration - twentyMinutesInMilliseconds)
                    //        {
                    //            if (!endOfTheVideo && !isMessageBoxShown)
                    //            {
                    //                if ((DateTime.Now - _lastSnapshotTime).TotalSeconds > 1
                    //                    && GetVisiblePercentage(this) >= 90)
                    //                {
                    //                    await Task.Run(() => TakeSnapshotAsync(snapshotPath));
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                });
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private Queue<bool> motionQueue = new Queue<bool>();  
        private byte[] _previousFrameBuffer = null; 

        private bool AnalyzeMotion(string snapshotPath)
        {
            try
            {
                if (_previousFrameBuffer == null)
                {
                    _previousFrameBuffer = File.ReadAllBytes(snapshotPath); 
                    return false; 
                }
                byte[] currentFrameBuffer = File.ReadAllBytes(snapshotPath); 

                using var currentMat = new Mat((int)Height, (int)Width, MatType.CV_8UC4);
                Marshal.Copy(currentFrameBuffer, 0, currentMat.Data, currentFrameBuffer.Length);

                using var previousMat = new Mat((int)Height, (int)Width, MatType.CV_8UC4);
                Marshal.Copy(_previousFrameBuffer, 0, previousMat.Data, _previousFrameBuffer.Length);

                using var diffMat = new Mat();
                Cv2.Absdiff(currentMat, previousMat, diffMat);

                double diffSum = Cv2.Sum(diffMat).Val0;
                _previousFrameBuffer = currentFrameBuffer; 

                return diffSum / (Width * Height * 255 * 3) < 0.02;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
                return false;
            }
        }

        private int ExtractSecondFromFilename(string snapshotPath)
        {
            string filename = Path.GetFileNameWithoutExtension(snapshotPath);

            var match = Regex.Match(filename, @"\d+"); 

            if (match.Success)
            {
                if (int.TryParse(match.Value, out int second))
                {
                    return second;
                }
            }
            return -1;  
        }

        //private int startSecond = 0;
        //private int AnalyzeMotionFromThumbnails(IEnumerable<string> snapshotPaths)
        //{
        //    foreach (var x in snapshotPaths)
        //    {
        //        int currentSecond = ExtractSecondFromFilename(x);

        //        if (_previousFrameBuffer == null)
        //        {
        //            _previousFrameBuffer = File.ReadAllBytes(x);
        //            startSecond = currentSecond;
        //            continue; 
        //        }

        //        byte[] currentFrameBuffer = File.ReadAllBytes(x);

        //        using var currentMat = new Mat((int)Height, (int)Width, MatType.CV_8UC4);
        //        Marshal.Copy(currentFrameBuffer, 0, currentMat.Data, currentFrameBuffer.Length);

        //        using var previousMat = new Mat((int)Height, (int)Width, MatType.CV_8UC4);
        //        Marshal.Copy(_previousFrameBuffer, 0, previousMat.Data, _previousFrameBuffer.Length);

        //        using var diffMat = new Mat();
        //        Cv2.Absdiff(currentMat, previousMat, diffMat);

        //        double diffSum = Cv2.Sum(diffMat).Val0;
        //        _previousFrameBuffer = currentFrameBuffer;

        //        bool motionNotDetected = diffSum / (Width * Height * 255 * 3) < 0.02; 

        //        if (!motionNotDetected)
        //        {
        //            _previousFrameBuffer = null;
        //        }

        //        motionQueue.Enqueue(motionNotDetected);

        //        if (motionQueue.Count > 5) motionQueue.Dequeue();

        //        if (motionQueue.Count == 5 && motionQueue.All(result => result == true)) 
        //        {
        //            return startSecond;  
        //        }
        //    }

        //    return -1;  
        //}

        bool isCreditsShown = false;

        //private List<string> CreditsTexts = new List<string>()
        //{
        //    "directed by",
        //    "produced by",
        //    "written by",
        //    "created by"
        //};

        private async Task AnalyzeSnapshot(string snapshotPath)
        {
            try
            {
                if (!isMessageBoxShown && !endOfTheVideo)
                {
                    motionQueue.Enqueue(AnalyzeMotion(snapshotPath));

                    if (motionQueue.Count > 5) motionQueue.Dequeue();

                    if (!isCreditsShown)
                    {
                        var detectedText = PerformOcr(snapshotPath).Replace(" ","");
                        if (detectedText.Length>3)
                        {
                            isCreditsShown = true;
                        }
                    }

                    if (motionQueue.Count == 5 && motionQueue.All(result => result) && isCreditsShown)
                    {
                        isMessageBoxShown = true;
                        endOfTheVideo = true;
                        ProgressGrid.Visibility = Visibility.Visible;
                        LoadingTextBlock.Visibility = Visibility.Collapsed;
                        ProgressBarToPlay.Visibility = Visibility.Collapsed;
                        NextEpisodeStackPanel.Visibility = Visibility.Visible;
                        NextEpisodeButton.IsChecked = true;
                        timerNextEpisode = new DispatcherTimer();
                        timerNextEpisode.Interval = TimeSpan.FromMilliseconds(30);  
                        timerNextEpisode.Tick += TimerNextEpisodeOnTick;
                        timerNextEpisode.Start();
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task TakeSnapshotAsync(string snapshotPath)
        {
            try
            {
                await this.Dispatcher.InvokeAsync(async () =>
                {
                    var location = Player.PointToScreen(new System.Windows.Point(0, 0));  // WPF Point
                    System.Drawing.Point drawingLocation = new System.Drawing.Point((int)location.X, (int)location.Y);
                    System.Drawing.Size size = new System.Drawing.Size((int)Player.ActualWidth, (int)Player.ActualHeight);

                    using (Bitmap bitmap = new Bitmap(size.Width, size.Height))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(drawingLocation, System.Drawing.Point.Empty, size);
                        }

                        bitmap.Save(snapshotPath, System.Drawing.Imaging.ImageFormat.Png);
                        _lastSnapshotTime = DateTime.Now;
                        await AnalyzeSnapshot(snapshotPath);
                    }
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private string PerformOcr(string imagePath)
        {
            // Görüntüyü yükle ve işlemek için bir Mat oluştur
            try
            {
                using (var originalImage = Cv2.ImRead(imagePath))
                {
                    if (originalImage.Empty())
                    {
                        throw new FileNotFoundException($"Görüntü dosyası bulunamadı: {imagePath}");
                    }

                    // Görüntünün ortasında bir bölge seç
                    Rect centerRegion = new Rect(
                        originalImage.Width / 4, // Sol üst köşe x
                        originalImage.Height / 4, // Sol üst köşe y
                        originalImage.Width / 2, // Bölgenin genişliği
                        originalImage.Height / 2 // Bölgenin yüksekliği
                    );

                    using (var croppedImage = new Mat(originalImage, centerRegion))
                    {
                        // Görüntüyü gri tonlamaya çevir ve eşik uygula
                        using (var grayImage = new Mat())
                        {
                            Cv2.CvtColor(croppedImage, grayImage, ColorConversionCodes.BGR2GRAY);

                            // Adaptive Threshold veya Binary Threshold uygula
                            using (var thresholdImage = new Mat())
                            {
                                Cv2.Threshold(grayImage, thresholdImage, 128, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                                // Geçici dosya yolu oluştur
                                string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");

                                // Görüntünün geçerli olup olmadığını kontrol et
                                if (thresholdImage.Empty())
                                {
                                    throw new InvalidOperationException("İşlenen görüntü boş.");
                                }

                                // İşlenen görüntüyü geçici bir PNG dosyasına kaydet
                                bool writeSuccess = Cv2.ImWrite(tempFilePath, thresholdImage);
                                if (!writeSuccess)
                                {
                                    throw new InvalidOperationException($"Görüntü geçici dosyaya kaydedilemedi: {tempFilePath}");
                                }

                                // Dosya boyutunu kontrol et
                                if (new FileInfo(tempFilePath).Length == 0)
                                {
                                    throw new InvalidOperationException("Geçici dosya boş: " + tempFilePath);
                                }

                                try
                                {
                                    // Tesseract ile OCR işlemini gerçekleştir
                                    using (var pix = Pix.LoadFromFile(tempFilePath))
                                    {
                                        using (var ocrEngine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                                        {
                                            using (var page = ocrEngine.Process(pix))
                                            {
                                                return page.GetText();
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    // Geçici dosyayı sil
                                    if (File.Exists(tempFilePath))
                                    {
                                        File.Delete(tempFilePath);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
                return "";
            }
        }


        private bool endOfTheVideo = false;
        private bool isMessageBoxShown = false;
        private DispatcherTimer timerNextEpisode;

        private double progressNextEpisode = 0;

        private async void TimerNextEpisodeOnTick(object? sender, EventArgs e)
        {
            try
            {
                if (NextEpisodeButton.Progress < 100)
                {
                    progressNextEpisode += 1; 

                    this.Dispatcher.Invoke(
                        () =>
                        {
                            NextEpisodeButton.Progress = progressNextEpisode;
                            NextEpisodeButton.InvalidateVisual();
                        });
                }
                else
                {
                    timerNextEpisode.Stop();
                    timerNextEpisode.Tick -= TimerNextEpisodeOnTick;

                    if (!clickedWatchCredits)
                    {
                        SaveCurrentPlayerPosition();
                        var nextFile =
                            downloadsFilesPage.episodeFiles.IndexOf(
                                downloadsFilesPage.episodeFiles.FirstOrDefault(x => x.FileIndex == fileIndex)) + 1;
                        downloadsFilesPage.FilesDisplay.SelectedItem = downloadsFilesPage.episodeFiles[nextFile];
                        Close();
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

     
        private double GetVisiblePercentage(System.Windows.Window window)
        {
            // Ekranın çalışma alanı (taskbar'ı hariç)
            try
            {
                var screenBounds = SystemParameters.WorkArea;

                // Pencerenin ekran sınırları
                double windowLeft = window.Left;
                double windowTop = window.Top;
                double windowRight = window.Left + window.Width;
                double windowBottom = window.Top + window.Height;

                // Görünür olan pencere kısmını hesapla
                double visibleWidth = Math.Min(windowRight, screenBounds.Right) - Math.Max(windowLeft, screenBounds.Left);
                double visibleHeight = Math.Min(windowBottom, screenBounds.Bottom) - Math.Max(windowTop, screenBounds.Top);

                // Eğer pencere ekranın içinde değilse, görünür kısım negatif olur.
                if (visibleWidth < 0 || visibleHeight < 0)
                    return 0; // Pencere ekranın dışında

                // Görünür kısmın yüzdesini hesapla
                double totalWindowArea = window.Width * window.Height;
                double visibleArea = visibleWidth * visibleHeight;

                // Yüzdeyi hesapla
                double visiblePercentage = (visibleArea / totalWindowArea) * 100;

                return visiblePercentage;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
                return 0;
            }
        }

        private long twentyMinutesInMilliseconds;
        private long duration;
        private double durationInSeconds;
        private Process ffmpegProcess;
        public async Task GenerateThumbnailsAsync(string videoPath, string outputFolder, CancellationToken cancellationToken)
        {
            if(ffmpegProcess != null) return;
            string ffmpegPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin", "ffmpeg.exe");
            string arguments = $"-i \"{videoPath}\" -vf \"fps=1/1,scale=320:180\" \"{outputFolder}\\thumb_%d.jpg\"";

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ffmpegProcess = new Process { StartInfo = processStartInfo };

            ffmpegProcess.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Log.Information("Output: " + e.Data);
                }
            };

            ffmpegProcess.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Log.Error("Error: " + e.Data);
                }
            };

            try
            {
                ffmpegProcess.Start();

                ffmpegProcess.BeginOutputReadLine();
                ffmpegProcess.BeginErrorReadLine();

                // Cancellation token kullanımı
                _ = Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(100);
                    }

                    if (!ffmpegProcess.HasExited)
                    {
                        await ffmpegProcess.StandardInput.WriteLineAsync("q");
                    }
                }, cancellationToken);

                await Task.Run(() => ffmpegProcess.WaitForExit(), cancellationToken);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
            finally
            {
                ffmpegProcess?.Dispose();
            }
        }

        private System.Timers.Timer checkIsCompletedTimer;
        private async void MediaPlayerOnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
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

                if (isCompleted && AppSettingsManager.appSettings.PlayerSettingShowThumbnail)
                {
                    string fullPath = fileInfo.FullName;
                    string directoryPath = Path.GetDirectoryName(fullPath);
                   
                    var files = await Libtorrent.GetFiles(torrent);
                    if (files.Any())
                    {
                        var currentMediaFile = files.FirstOrDefault(x=> x.Index == fileIndex);
                        if (currentMediaFile != null)
                        {
                            _thumbnailsFolder = Path.Combine(directoryPath, "thumbnails" , 
                                Path.GetFileNameWithoutExtension(currentMediaFile.Name));
                            AddNewThumbnailCache(_thumbnailsFolder);
                            if (!Directory.Exists(_thumbnailsFolder) || 
                                Directory.GetFiles(_thumbnailsFolder, "*.jpg").Length < durationInSeconds)
                            {
                                if(!Directory.Exists(_thumbnailsFolder))
                                    Directory.CreateDirectory(_thumbnailsFolder);
                                await GenerateThumbnailsAsync(fullPath, _thumbnailsFolder, cts.Token);
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
        private void UpdateDuration(long length)
        {
            // UI thread üzerinde güncelleme yapılıyor.
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    // Süreyi TimeSpan'a çevirip, hh:mm:ss formatında görüntüle (ilk 8 karakter)
                    TxtDuration.Text = TimeSpan.FromMilliseconds(length).ToString().Substring(0, 8);
                    if (string.IsNullOrWhiteSpace(path) && ProgressGrid.Visibility == Visibility.Visible)
                    {
                        ProgressGrid.Visibility = Visibility.Collapsed;
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
                Log.Error(errorMessage);
            }
        }

        private bool startedGetThumbnails = false;
        
        private float lastPos;
        private bool myPositionChanging;
       

        private float seconds10 = 0;

       
        private async void PlayerWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer == null) return;
                if (e.Key == Key.Space)
                {
                    if (Player.MediaPlayer.IsPlaying)
                    {
                        Player.MediaPlayer.Pause();
                        PlayButton.Source = (BitmapImage)FindResource("PlayButtonImage");
                    }
                    else
                    {
                        Player.MediaPlayer.Pause();
                        PlayButton.Source = (BitmapImage)FindResource("PauseButtonImage");
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
                        double newTimeInSeconds = DurationSlider.Value * durationInSeconds + seconds10 * durationInSeconds;

                        var isDownloadedPiece = pieceTimeRanges.Any(range => newTimeInSeconds >= range.start.TotalSeconds
                                                                             && newTimeInSeconds <= range.end.TotalSeconds);

                        if (isDownloadedPiece)
                        {
                            Player.MediaPlayer.Position = Player.MediaPlayer.Position + seconds10;
                        }
                    }

                    if (e.Key == Key.Left)
                    {
                        double newTimeInSeconds = DurationSlider.Value * durationInSeconds - seconds10 * durationInSeconds;

                        var isDownloadedPiece = pieceTimeRanges.Any(range => newTimeInSeconds >= range.start.TotalSeconds
                                                                             && newTimeInSeconds <= range.end.TotalSeconds);

                        if (isDownloadedPiece)
                        {
                            Player.MediaPlayer.Position = Player.MediaPlayer.Position - seconds10;
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

        private void PlayerWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            
        }

        private void PlayerWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        private bool _isMouseMoveHandled = false;
        private async void Player_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseMoveHandled) return;
            _isMouseMoveHandled = true;
            try
            {
                ButtonBack.Visibility = Visibility.Visible;
                MovieNameText.Visibility = Visibility.Visible;
                Cursor = Cursors.Arrow;
                PanelControlVideo.Visibility = Visibility.Visible;

                await Task.Delay(TimeSpan.FromSeconds(2));

                if (shouldCollapseControlPanel)
                {
                    PanelControlVideo.Visibility = Visibility.Collapsed;
                    ButtonBack.Visibility = Visibility.Collapsed;
                    MovieNameText.Visibility = Visibility.Collapsed;
                    Cursor = Cursors.None;
                }
               
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
            finally
            {
                _isMouseMoveHandled = false; 
            }
        }


        private void Player_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (VolumeSlider.Value <= 200 && VolumeSlider.Value >= 0)
                {
                    if (e.Delta > 0)
                    {

                        VolumeSlider.Value += 10;
                    }
                    else
                    {

                        VolumeSlider.Value -= 10;
                    }

                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void Player_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if(Player.MediaPlayer == null) return;
                if (Player.MediaPlayer.IsPlaying) 
                {
                    Player.MediaPlayer.Pause();
                    PlayButton.Source = (BitmapImage)FindResource("PlayButtonImage");
                }
                else
                {
                    Player.MediaPlayer.Pause();
                    PlayButton.Source = (BitmapImage)FindResource("PauseButtonImage");
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        public void FullScreen()
        {
            try
            {
                this.WindowState = WindowState.Maximized;
                isFullScrenn = true;
                ButtonBack.Visibility = Visibility.Visible;
                MovieNameText.Visibility = Visibility.Visible;
                //Taskbar.Gizle();
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public void NormalScreen()
        {
            try
            {
                this.WindowState = WindowState.Normal;
                isFullScrenn = false;
                //Taskbar.Goster();
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void Player_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (this.WindowState == WindowState.Normal)
                {
                    FullScreen();
                }
                else
                {
                    NormalScreen();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ButtonBack_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
       
        private async void DurationSlider_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer == null) return;
                if (DurationSlider.Value <= 1 && DurationSlider.Value >= 0)
                {
                    if (isCompleted)
                    {
                        if (e.Delta > 0)
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
                        if (e.Delta > 0)
                        {
                            double newTimeInSeconds = (DurationSlider.Value + seconds10) * durationInSeconds * durationInSeconds;

                            var isDownloadedPiece = pieceTimeRanges.Any(range => newTimeInSeconds >= range.start.TotalSeconds
                                && newTimeInSeconds <= range.end.TotalSeconds);

                            if (isDownloadedPiece)
                            {
                                Player.MediaPlayer.Position = Player.MediaPlayer.Position + seconds10;
                            }
                        }
                        else
                        {
                            double newTimeInSeconds = DurationSlider.Value * durationInSeconds - seconds10 * durationInSeconds;

                            var isDownloadedPiece = pieceTimeRanges.Any(range => newTimeInSeconds >= range.start.TotalSeconds
                                && newTimeInSeconds <= range.end.TotalSeconds);

                            if (isDownloadedPiece)
                            {
                                Player.MediaPlayer.Position = Player.MediaPlayer.Position - seconds10;
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
        private void DurationSlider_OnDragStarted(object sender, DragStartedEventArgs e)
        {
            myPositionChanging = true;
        }

        private async void DurationSlider_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer == null) return;
                myPositionChanging = false;
                if (isCompleted)
                {
                    Player.MediaPlayer.Position = (float)(sender as System.Windows.Controls.Slider).Value;
                }
                else
                {
                    double newTimeInSeconds = (sender as System.Windows.Controls.Slider).Value * durationInSeconds;

                    var isDownloadedPiece = pieceTimeRanges.Any(range => newTimeInSeconds >= range.start.TotalSeconds
                                                                         && newTimeInSeconds <= range.end.TotalSeconds);

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
                        Player.MediaPlayer.Position = (float)(sender as System.Windows.Controls.Slider).Value;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private bool isSliderUpdating = false;
        private double previousSliderValue = 0;
        private Canvas canvas = null;
        private bool startedUpdatingSelectionRanges = false;
        private List<(TimeSpan start, TimeSpan end)> pieceTimeRanges = new List<(TimeSpan start, TimeSpan end)> ();
        private async void DurationSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (canvas == null)
                    canvas = FindDescendantByName<Canvas>(DurationSlider, "SelectionRangeCanvas");
                if (isCompleted)
                {
                    previousSliderValue = e.NewValue;
                    pieceTimeRanges = new EditableList<(TimeSpan start, TimeSpan end)>()
                    {
                        (TimeSpan.FromSeconds(DurationSlider.Value * durationInSeconds), TimeSpan.FromSeconds(durationInSeconds))
                    };
                    UpdateSelectionRanges(canvas);
                }
                else
                {
                    if (!startedUpdatingSelectionRanges)
                    {
                        pieceTimeRanges = await Libtorrent.GetAvailaibleSeconds(torrent.Hash, (int)durationInSeconds, fileIndex);
                        if (canvas != null)
                        {
                            startedUpdatingSelectionRanges = true;
                            UpdateSelectionRanges(canvas);
                        }
                    }

                    if (isSliderUpdating || myPositionChanging || torrent == null) return;

                    double newTimeInSeconds = e.NewValue * durationInSeconds;



                    var isDownloadedPiece = pieceTimeRanges.Any(range => newTimeInSeconds >= range.start.TotalSeconds
                                                                         && newTimeInSeconds <= range.end.TotalSeconds);

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
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonPlay_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer == null) return;
                if (Player.MediaPlayer.IsPlaying)
                {
                    Player.MediaPlayer.Pause();
                    PlayButton.Source = (BitmapImage)FindResource("PlayButtonImage");
                }
                else
                {
                    Player.MediaPlayer.Pause();
                    PlayButton.Source = (BitmapImage)FindResource("PauseButtonImage");
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonMute_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (VolumeSlider.Value == 0)
                {
                    VolumeSlider.Value = saveVolume;
                    if (saveVolume >= 100)
                    {
                        MuteButton.Source = (BitmapImage)FindResource("AudioButtonImage");

                    }
                    else
                    {
                        MuteButton.Source = (BitmapImage)FindResource("SpeakerButtonImage");
                    }
                }
                else
                {
                    saveVolume = Convert.ToInt32(VolumeSlider.Value);
                    VolumeSlider.Value = 0;
                    MuteButton.Source = (BitmapImage)FindResource("MuteButtonImage");
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private int saveVolume = 50;
        private void MuteButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private bool isVolumeSliderValueChangedHandled = false;
        private async void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
          
            try
            {
                if (Player != null)
                {
                    File.WriteAllText(AppSettingsManager.appSettings.VolumeCachePath, VolumeSlider.Value.ToString());
                    
                    VolumeText.Text = Application.Current.Resources["VolumeString"] + ": " + VolumeSlider.Value;
                    if(Player != null && Player.MediaPlayer != null)
                        Player.MediaPlayer.Volume = Convert.ToInt32(VolumeSlider.Value);
                    if (VolumeSlider.Value == 0)
                    {
                        MuteButton.Source = (BitmapImage)FindResource("MuteButtonImage");

                    }
                    else if (VolumeSlider.Value > 0 && VolumeSlider.Value <= 100)
                    {
                        MuteButton.Source = (BitmapImage)FindResource("SpeakerButtonImage");

                    }
                    else if (VolumeSlider.Value > 100 && VolumeSlider.Value <= 200)
                    {
                        MuteButton.Source = (BitmapImage)FindResource("AudioButtonImage");

                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }

            if (isVolumeSliderValueChangedHandled) return;
            isVolumeSliderValueChangedHandled = true;
            VolumeText.Visibility = Visibility.Visible;
            await Task.Delay(TimeSpan.FromSeconds(2));
            VolumeText.Visibility = Visibility.Collapsed;
            isVolumeSliderValueChangedHandled = false;
        }


        private void ButtonFullScreen_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                FullScreen();
            }
            else
            {
                NormalScreen();
            }
        }

        private void VolumeSlider_OnLoaded(object sender, RoutedEventArgs e)
        {
            VolumeSlider.ValueChanged += VolumeSlider_OnValueChanged;
        }

        private float? subtitleClickedTime = null;
        private async void ButtonSubtitle_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Player.MediaPlayer == null) return;
            try
            {
                if (currentSubtitle != null)
                {
                    if(currentPlayerCache != null)
                        currentPlayerCache.LastPosition = lastPos;
                    else
                    {
                        subtitleClickedTime = lastPos;
                    }
                    if (Player.MediaPlayer.IsPlaying)
                    {
                        Player.MediaPlayer.Pause();
                        PlayButton.Source =
                            (BitmapImage)FindResource("PlayButtonImage");
                    }

                    int year;
                    if (showType == ShowType.Movie)
                    {
                        var mov =await Service.client.GetMovieAsync(movieId);
                        year = mov.ReleaseDate.Value.Year;
                    }
                    else
                    {
                        var mov = await Service.client.GetTvShowAsync(movieId);
                        year = mov.FirstAirDate.Value.Year;
                    }

                    SubtitleManagerWindow subtitleManager = 
                         new SubtitleManagerWindow(currentSubtitle, path, movieName, imdbId,movieId,showType,year,seasonNumber,episodeNumber);
                    subtitleManager.Owner = this;
                    subtitleManager.ShowDialog();
                    subtitleManager.Unloaded += delegate(object o, RoutedEventArgs args)
                    {
                        if (subtitleManager.disabledSubtitle)
                        {
                            Player.MediaPlayer.SetSpu(-1);
                        }
                        else
                        {
                            if (subtitleManager.changedSubtitle)
                            {
                                mediaPlayer.Stop();
                                success = false;
                                PlayerOnLoaded(sender, e);
                                Player.MediaPlayer.SetSpu(0);
                            }
                        }
                        
                        ButtonBack.Visibility = Visibility.Visible;
                        MovieNameText.Visibility = Visibility.Visible;
                        Cursor = Cursors.Arrow;
                        PanelControlVideo.Visibility = Visibility.Visible;
                        Player.MediaPlayer.Play();
                        PlayButton.Source =
                            (BitmapImage)FindResource("PauseButtonImage");
                    };
                }
                else
                {
                    if (currentPlayerCache != null)
                        currentPlayerCache.LastPosition = lastPos;
                    else
                    {
                        subtitleClickedTime = lastPos;
                    }
                    if (Player.MediaPlayer.IsPlaying)
                    {
                        Player.MediaPlayer.Pause();
                        PlayButton.Source =
                            (BitmapImage)FindResource("PlayButtonImage");
                    }

                    int year;
                    if (showType == ShowType.Movie)
                    {
                        var mov = await Service.client.GetMovieAsync(movieId);
                        year = mov.ReleaseDate.Value.Year;
                    }
                    else
                    {
                        var mov = await Service.client.GetTvShowAsync(movieId);
                        year = mov.FirstAirDate.Value.Year;
                    }
                    SubtitleManagerWindow subtitleManager = new SubtitleManagerWindow(movieId,path,movieName,imdbId,showType,year,seasonNumber,episodeNumber);
                    subtitleManager.Owner = this;
                    subtitleManager.ShowDialog();
                    subtitleManager.Unloaded += delegate (object o, RoutedEventArgs args)
                    {
                        if (subtitleManager.disabledSubtitle)
                        {
                            Player.MediaPlayer.SetSpu(-1);
                        }
                        else
                        {
                            if (subtitleManager.changedSubtitle)
                            {
                                mediaPlayer.Stop();
                                success = false;
                                PlayerOnLoaded(sender, e);
                                Player.MediaPlayer.SetSpu(0);
                            }
                        }

                        ButtonBack.Visibility = Visibility.Visible;
                        MovieNameText.Visibility = Visibility.Visible;
                        Cursor = Cursors.Arrow;
                        PanelControlVideo.Visibility = Visibility.Visible;
                        Player.MediaPlayer.Play();
                        PlayButton.Source =
                            (BitmapImage)FindResource("PauseButtonImage");
                    };
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void DurationSlider_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                double sliderWidth = DurationSlider.ActualWidth;
                System.Windows.Point mousePosition = e.GetPosition(DurationSlider);
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

                        var isDownloadedPiece = pieceTimeRanges.Any(range => newTimeInSeconds >= range.start.TotalSeconds
                                                                             && newTimeInSeconds <= range.end.TotalSeconds);

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
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void NextEpisodeButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SaveCurrentPlayerPosition();
                await this.Dispatcher.InvokeAsync(async () =>
                {
                    var nextFile =
                        downloadsFilesPage.episodeFiles.IndexOf(
                            downloadsFilesPage.episodeFiles.FirstOrDefault(x => x.FileIndex == fileIndex)) + 1;
                    downloadsFilesPage.FilesDisplay.SelectedItem = downloadsFilesPage.episodeFiles[nextFile];
                    Close();
                });
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private bool clickedWatchCredits = false;
        private void BtnWatchCredits_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            clickedWatchCredits = true;
            timerNextEpisode.Stop();
            timerNextEpisode.Tick -= TimerNextEpisodeOnTick;
            ProgressGrid.Visibility = Visibility.Collapsed;
            NextEpisodeStackPanel.Visibility = Visibility.Collapsed;
        }
        public bool closed = false;
        private async void PlayerWindow_OnClosed(object? sender, EventArgs e)
        {
            try
            {
                Player.Loaded -= PlayerOnLoaded;
                Player.MouseMove -= Player_OnMouseMove;
                closed = true;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    mediaPlayer.Stop();
                    mediaPlayer.Dispose();
                    libVlc.Dispose();
                });
                if(torrentStream != null)
                    torrentStream.Dispose();
                cts.Cancel();
                if (checkIsCompletedTimer != null)
                {
                    checkIsCompletedTimer.Dispose();
                    checkIsCompletedTimer = null;
                }
                foreach (var bitmapImage in thumbnailCache)
                {
                    bitmapImage.Value.StreamSource?.Dispose();
                    thumbnailCache.Remove(bitmapImage.Key);
                }
                SaveCurrentPlayerPosition();
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


                WindowsManager.OpenedWindows.Remove(this);
                var itemToRemove =
                    DownloadsPageQ.GetDownloadsPageInstance.openedTorrents.FirstOrDefault(x => x == torrent.Link);
                DownloadsPageQ.GetDownloadsPageInstance.openedTorrents.Remove(itemToRemove);

                if (timerNextEpisode != null)
                {
                    timerNextEpisode.Stop();
                    timerNextEpisode.Tick -= TimerNextEpisodeOnTick;
                }

                if (DownloadsPageQ.GetDownloadsPageInstance.openedTorrents.Count < 2)
                {
                    SetThreadExecutionState(ES_CONTINUOUS);
                }

                Log.Information($"Unloaded Player window for: {GetShowName()}");
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private List<System.Windows.Shapes.Rectangle> existingRectangles = new List<System.Windows.Shapes.Rectangle>();

        private void UpdateSelectionRanges(Canvas canvas)
        {
            try
            {
                int count = 1;

                if (existingRectangles.Count > 0)
                {
                    foreach (var rectangle in existingRectangles)
                    {
                        rectangle.MouseEnter -= (sender, e) => StartAnimation(rectangle, 2);
                        rectangle.MouseLeave -= (sender, e) => StartAnimation(rectangle, 1);
                    }
                }

                canvas.MouseEnter -= (sender, e) => StartAnimation(canvas, 2);
                canvas.MouseLeave -= (sender, e) => StartAnimation(canvas, 1);

                canvas.Children.Clear();
                existingRectangles.Clear();

                double canvasWidth = canvas.ActualWidth;

                pieceTimeRanges = MergeOverlappingRanges(pieceTimeRanges);
            
                foreach (var range in pieceTimeRanges)
                {
                    var start = range.start.TotalSeconds / durationInSeconds;
                    var end = range.end.TotalSeconds /durationInSeconds;

                    if (start <= previousSliderValue) start = previousSliderValue;
                    if(end <= previousSliderValue) end = previousSliderValue;

                    var rectangle = new Rectangle
                    {
                        Fill = new SolidColorBrush(Color.FromRgb(203, 203, 203)),
                        Height = 5,
                        Width = (end - start) * canvasWidth,
                        Visibility = Visibility.Visible,
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        RenderTransform = new ScaleTransform(1, 1),
                        Name = $"PART_SelectionRange_{count}"
                    };

                    Canvas.SetLeft(rectangle, start * canvasWidth);
                    count++;

                    canvas.Children.Add(rectangle);
                    existingRectangles.Add(rectangle);

                    rectangle.MouseEnter += (sender, e) => StartAnimation(rectangle, 2);
                    rectangle.MouseLeave += (sender, e) => StartAnimation(rectangle, 1);

                    canvas.RenderTransformOrigin = new Point(0.5, 0.5);

                    canvas.MouseEnter += (sender, e) => StartAnimation(canvas, 2);
                    canvas.MouseLeave += (sender, e) => StartAnimation(canvas, 1);
                }
                startedUpdatingSelectionRanges = false;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private List<(TimeSpan start, TimeSpan end)> MergeOverlappingRanges(List<(TimeSpan start, TimeSpan end)> ranges)
        {
            // Aralıkları başlama zamanına göre sırala
            var sortedRanges = ranges.OrderBy(r => r.start).ToList();
            var mergedRanges = new List<(TimeSpan start, TimeSpan end)>();

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
                        if (lastRange.end >= range.start)
                        {
                            // Eğer aralıklar örtüşüyor veya bitiş ve başlangıç noktaları arasında küçük bir fark varsa birleştir
                            mergedRanges[mergedRanges.Count - 1] = (
                                lastRange.start,
                                TimeSpan.FromTicks(Math.Max(lastRange.end.Ticks, range.end.Ticks))
                            );
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
                Log.Error(errorMessage);
            }

            return mergedRanges;
        }
        private void StartAnimation(Canvas x, double scaleY)
        {
            try
            {
                var animation = new DoubleAnimation
                {
                    To = scaleY,
                    Duration = new Duration(TimeSpan.FromSeconds(0.2))
                };

                Storyboard.SetTarget(animation, x);
                Storyboard.SetTargetProperty(animation, new PropertyPath("RenderTransform.ScaleY"));

                var storyboard = new Storyboard();
                storyboard.Children.Add(animation);
                storyboard.Begin();
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private void StartAnimation(System.Windows.Shapes.Rectangle rectangle, double scaleY)
        {
            try
            {
                var animation = new DoubleAnimation
                {
                    To = scaleY,
                    Duration = new Duration(TimeSpan.FromSeconds(0.2))
                };

                Storyboard.SetTarget(animation, rectangle);
                Storyboard.SetTargetProperty(animation, new PropertyPath("RenderTransform.ScaleY"));

                var storyboard = new Storyboard();
                storyboard.Children.Add(animation);
                storyboard.Begin();
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private void DurationSlider_OnLoaded(object sender, RoutedEventArgs e)
        {
          
            //Track boyutu

            //System.Windows.Shapes.Rectangle rectangle = DurationSlider.Template.FindName("TrackRectangle", DurationSlider) as System.Windows.Shapes.Rectangle;
            //if (rectangle != null)
            //{
            //    rectangle.Height = 30;
            //}

            ////activeTrack

            //Border activeTrack = DurationSlider.Template.FindName("activeTrack", DurationSlider) as Border;
            //if (activeTrack != null)
            //{
            //    activeTrack.Height = 30;
            //}



            //thumb boyutu  PART_Track
            //Track partTrack = DurationSlider.Template.FindName("PART_Track", DurationSlider) as Track;
            //if (partTrack != null)
            //{
            //    partTrack.Height = 100;
            //}

            //thumbGrid

            //Grid thumb = FindDescendantByName<Grid>(DurationSlider, "thumbGrid");
            //if (thumb != null)
            //{
            //    thumb.Height = 50;
            //}
        }

        public T FindDescendantByName<T>(DependencyObject obj, string objname) where T : DependencyObject
        {
            try
            {
                string controlneve = "";

                Type tyype = obj.GetType();
                if (tyype.GetProperty("Name") != null)
                {
                    PropertyInfo prop = tyype.GetProperty("Name");
                    controlneve = (string)prop.GetValue((object)obj, null);
                }
                else
                {
                    return null;
                }

                if (obj is T && objname.ToString().ToLower() == controlneve.ToString().ToLower())
                {
                    return obj as T;
                }

                // Check for children
                int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
                if (childrenCount < 1)
                    return null;

                // First check all the children
                for (int i = 0; i <= childrenCount - 1; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    if (child is T && objname.ToString().ToLower() == controlneve.ToString().ToLower())
                    {
                        return child as T;
                    }
                }

                // Then check the childrens children
                for (int i = 0; i <= childrenCount - 1; i++)
                {
                    string checkobjname = objname;
                    DependencyObject child = FindDescendantByName<T>(VisualTreeHelper.GetChild(obj, i), objname);
                    if (child != null && child is T && objname.ToString().ToLower() == checkobjname.ToString().ToLower())
                    {
                        return child as T;
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }

            return null;
        }
        private string _thumbnailsFolder = "";
        private void DurationSlider_OnMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var slider = sender as Slider;
                if (slider == null) return;
                var popup = (Popup)slider.Template.FindName("HoverPopup", slider);
                var timeText = (TextBlock)slider.Template.FindName("TimeText", slider);
          
                var thumbnail = (Image)slider.Template.FindName("Thumbnail", slider);
            
                var position = e.GetPosition(slider);

                var hoverLine = (Grid)slider.Template.FindName("HoverLine", slider);
                if (hoverLine != null && popup != null && thumbnail != null)
                {
                    hoverLine.Opacity = 1;
                    hoverLine.SetValue(Canvas.LeftProperty, position.X - hoverLine.Width / 2);

                    popup.IsOpen = true;
                    popup.HorizontalOffset = position.X - popup.ActualWidth / 2 + (!isCompleted || !AppSettingsManager.appSettings.PlayerSettingShowThumbnail ? - 23 : -150);
                    popup.VerticalOffset = -15;

                    double sliderWidth = slider.ActualWidth;
                    double relativePosition = position.X / sliderWidth;
                    relativePosition = Math.Max(0, Math.Min(1, relativePosition));
                    double totalDuration = durationInSeconds;
                    double currentSecond = relativePosition * totalDuration;
                    int hours = (int)(currentSecond / 3600); 
                    int minutes = (int)((currentSecond % 3600) / 60); 
                    int seconds = (int)(currentSecond % 60); 

                    string timeString;

                    if (hours > 0)
                    {
                        timeString = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
                        if(!isCompleted)
                            popup.HorizontalOffset = position.X - popup.ActualWidth / 2 - 35;
                    }
                    else
                    {
                        timeString = $"{minutes:D2}:{seconds:D2}";
                    }

                    timeText.Text = timeString;
                    if (isCompleted && AppSettingsManager.appSettings.PlayerSettingShowThumbnail)
                    {
                        thumbnail.Visibility = Visibility.Visible;
                        ShowThumbnailPreview((int)currentSecond, thumbnail);
                    }
                    else
                    {
                        thumbnail.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void DurationSlider_OnMouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                var slider = sender as Slider;
                if (slider == null)
                    return;

                var popup = (Popup)slider.Template.FindName("HoverPopup", slider);
                if (popup != null)
                {
                    popup.IsOpen = false; 
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private bool shouldCollapseControlPanel = true;
        private void PanelControlVideo_OnMouseEnter(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = false;
        }

        private void PanelControlVideo_OnMouseLeave(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = true;
        }
        //private bool _doClose = false;
        private async void PlayerWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            //if (!_doClose)
            //{
            //    e.Cancel = true;
            //    this.Hide();
            //    await ClosingTasks();
            //}
        }

        private async Task ClosingTasks()
        {
            
        }
    }

    internal class BooleanAllConverter : IMultiValueConverter
    {
        public static readonly BooleanAllConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => values.OfType<bool>().All(b => b);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public sealed class InvertBooleanConverter : BooleanConverter<bool>
    {
        public static readonly InvertBooleanConverter Instance = new();

        public InvertBooleanConverter()
            : base(false, true)
        {
        }
    }

    internal class SliderToolTipConverter : IMultiValueConverter
    {
        public static readonly SliderToolTipConverter Instance = new();
        public object? Convert(object?[]? values, Type? targetType, object? parameter, CultureInfo? culture)
        {
            if (values?.Length >= 2 && values[1] is string format && !string.IsNullOrEmpty(format))
            {
                try
                {
                    return string.Format(culture, format, values[0]);
                }
                catch (FormatException) { }
            }
            if (values?.Length >= 1 && targetType is not null)
            {
                return System.Convert.ChangeType(values[0], targetType, culture);
            }
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    [ValueConversion(typeof(double), typeof(double), ParameterType = typeof(System.Windows.Controls.Orientation))]
    internal class SliderValueLabelPositionConverter : IValueConverter
    {
        public static readonly SliderValueLabelPositionConverter Instance = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is System.Windows.Controls.Orientation orientation && value is double width)
            {
                const double halfGripWidth = 9.0;
                const double margin = 4.0;
                return orientation switch
                {
                    System.Windows.Controls.Orientation.Horizontal => (-width * 0.5) + halfGripWidth,
                    System.Windows.Controls.Orientation.Vertical => -width - margin,
                    _ => throw new ArgumentOutOfRangeException(nameof(parameter))
                };
            }

            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class FontSizeConverter : IValueConverter
    {
        public double BaseWidth { get; set; } = 1607; // Başlangıç genişliği
        public double BaseFontSize { get; set; } // Başlangıç font boyutu (30 veya 34)

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double currentWidth)
            {
                // Oran ile yeni FontSize hesaplama
                return BaseFontSize * (currentWidth / BaseWidth);
            }
            return BaseFontSize;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
