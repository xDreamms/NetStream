using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
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
using NetStream.Properties;
using Serilog;
using TMDbLib.Objects.Movies;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops.Signatures;
using Vlc.DotNet.Wpf;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using Path = System.IO.Path;
using NetStream.Views;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using YoutubeDLSharp.Options;
using YoutubeDLSharp;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for TrailerPlayPage.xaml
    /// </summary>
    public partial class TrailerPlayPage : Window
    {
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SetThreadExecutionState([In] uint esFlags);

        private LibVLC libVlc;
        private LibVLCSharp.Shared.MediaPlayer mediaPlayer;

        private bool isFullScrenn = false;
        private VideoDetail videoDetail;
        
        public TrailerPlayPage(VideoDetail videoDetail)
        {
            InitializeComponent();
            SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
            this.videoDetail = videoDetail;
            videoPath = System.IO.Path.Combine(AppSettingsManager.appSettings.YoutubeVideoPath, videoDetail.Name +".mp4");
            WindowsManager.OpenedWindows.Add(this);
        }

        private void TrailerPlayPage_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void TrailerPlayPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            
        }
        private bool _isMouseMoveHandled = false;
        private bool shouldCollapseControlPanel = true;
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

        private void Player_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

        private void ButtonBack_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

        private void DurationSlider_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(Player.MediaPlayer == null) return;
            if (DurationSlider.Value <= 1 && DurationSlider.Value >= 0)
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
        }


        private void DurationSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }

        private void ButtonPlay_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Player.MediaPlayer.IsPlaying)
            {
                Player.MediaPlayer.Pause();
                PlayButton.Source = new BitmapImage(new Uri("pack://application:,,,/NetStream;component/Play_50px.png"));
            }
            else
            {
                Player.MediaPlayer.Pause();
                PlayButton.Source = new BitmapImage(new Uri("pack://application:,,,/NetStream;component/Pause_50px.png"));
            }
        }
        private int saveVolume = 50;
        private void ButtonMute_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

        private void MuteButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
        }
        private async void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (Player != null)
                {
                    File.WriteAllText(AppSettingsManager.appSettings.VolumeCachePath, VolumeSlider.Value.ToString());

                    VolumeText.Text = Application.Current.Resources["VolumeString"] + ": " + VolumeSlider.Value;
                    try
                    {
                        if (Player != null && Player?.MediaPlayer != null)
                            Player.MediaPlayer.Volume = Convert.ToInt32(VolumeSlider.Value);
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception.Message);
                    }
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

            VolumeText.Visibility = Visibility.Visible;
            await Task.Delay(TimeSpan.FromSeconds(2));
            VolumeText.Visibility = Visibility.Collapsed;
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

        async Task DownloadToPipeStreamAsync(string pipeName, IStreamInfo mediaStreamInfo)
        {
            using (var pipe = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                try
                {
                    await pipe.WaitForConnectionAsync(); //We need to wait until someone connects to our pipeline
                    await Service.YoutubeClient.Videos.Streams.CopyToAsync(mediaStreamInfo, pipe); //Download media stream to pipeline
                }
                finally
                {
                    if (pipe.IsConnected)
                        pipe.Disconnect();
                }
            }
        }
        public string videoPath;
        bool success = false;
         string GenerateSixLetterGuid()
        {
            const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"; // Harf havuzu
            string guid = Guid.NewGuid().ToString("N"); // GUID oluştur ve düz format al
            var onlyLetters = guid.Where(c => char.IsLetter(c)).ToArray(); // Sadece harfleri al
            var random = new Random();

            // Rastgele 6 harf seç
            return new string(Enumerable.Range(0, 6).Select(_ => onlyLetters[random.Next(onlyLetters.Length)]).ToArray());
        }
        async Task RunFFMpegAsync()
        {
            try
            {
                //var a = GenerateSixLetterGuid();
                //string FFmpegPath = Environment.CurrentDirectory + "ffmpeg\\bin\\ffmpeg.exe";
                //string videoPipeName = "ffvideo"+ a;
                //string audioPipeName = "ffaudio2"+a;

                //var streamManifest = await Service.YoutubeClient.Videos.Streams.GetManifestAsync(videoDetail.VideoLink);
                //var streamInfoVideo = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
                //var streamInfoAudio = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                //var processStartInfo = new ProcessStartInfo
                //{
                //    FileName = Path.GetFullPath(FFmpegPath),
                //    Arguments = $@"-i \\.\pipe\{videoPipeName} -i \\.\pipe\{audioPipeName} -c:v copy -c:a aac -movflags frag_keyframe+empty_moov -f mp4 -",
                //    UseShellExecute = false,
                //    RedirectStandardOutput = true,
                //    CreateNoWindow = true
                //};
                //using (var process = Process.Start(processStartInfo))
                //{
                //    using (var fileStream = new FileStream(videoPath, FileMode.Create))
                //    {
                //        //I don't await tasks directly because they all need to be parallel. Otherwise = deadlock
                //        var audioDownloadTask = DownloadToPipeStreamAsync(audioPipeName, streamInfoAudio);
                //        var videoDownloadTask = DownloadToPipeStreamAsync(videoPipeName, streamInfoVideo);
                //        var outputTask = process.StandardOutput.BaseStream.CopyToAsync(fileStream);
                //        //Wait for all tasks to finnish
                //        await Task.WhenAll(audioDownloadTask, videoDownloadTask, outputTask);
                //        process.StandardOutput.Close();
                //    }
                //    process.WaitForExit();
                //}
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
                    videoDetail.VideoLink,
                    overrideOptions: options);
                success = true;
            }
            catch (Exception e)
            {
                success = false;
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
       
        private async void TrailerPlayPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MovieDetailsPage.current != null)
                {
                    MovieDetailsPage.current.Id = 0;
                }

                libVlc = new LibVLC();
                mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(libVlc);
                Player.Loaded += PlayerOnLoaded;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void PlayerOnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressGrid.Visibility = Visibility.Visible;
                LoadingTextBlock.Visibility = Visibility.Visible;
                ProgressBarToPlay.IsIndeterminate = true;
                ProgressBarToPlay.Visibility = Visibility.Visible;
                PanelControlVideo.Visibility = Visibility.Collapsed;

                await RunFFMpegAsync();

                ProgressGrid.Visibility = Visibility.Collapsed;

                if (success)
                {
                    Player.MediaPlayer = mediaPlayer;
                    Player.MouseMove += Player_OnMouseMove;

                    double volumeValue;
                    double.TryParse(await File.ReadAllTextAsync(AppSettingsManager.appSettings.VolumeCachePath), out volumeValue);
                    VolumeSlider.Value = volumeValue;

                    using (var media = new Media(libVlc, new Uri(videoPath), ":file-caching=1000"))
                    {
                        Player.MediaPlayer.Play(media);
                    }

                    Player.MediaPlayer.EnableKeyInput = false;
                    Player.MediaPlayer.EnableMouseInput = false;

                    Player.MediaPlayer.LengthChanged += MediaPlayerOnLengthChanged;
                    Player.MediaPlayer.TimeChanged += MediaPlayerOnTimeChanged;
                    Player.MediaPlayer.Volume = Convert.ToInt32(VolumeSlider.Value);
                    Player.MediaPlayer.PositionChanged += MediaPlayerOnPositionChanged;
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
        private float lastPos;
        private bool myPositionChanging;
       

        private async void MediaPlayerOnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            try
            {
                await this.Dispatcher.InvokeAsync(async () =>
                {
                    TxtCurrentTime.Text = TimeSpan.FromMilliseconds(e.Time).ToString().Substring(0, 8) + "/";
                });
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private float seconds10 = 0;
        long durationInSeconds = 0;
        private void MediaPlayerOnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    TxtDuration.Text = TimeSpan.FromMilliseconds(e.Length).ToString().Substring(0, 8);
                });
                durationInSeconds = e.Length / 1000;
                seconds10 = (float)10000 / e.Length;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TrailerPlayPage_OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
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

                if (e.Key == Key.Right)
                {
                    Player.MediaPlayer.Position = Player.MediaPlayer.Position + seconds10;
                }

                if (e.Key == Key.Left)
                {
                    Player.MediaPlayer.Position = Player.MediaPlayer.Position - seconds10;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TrailerPlayPage_OnClosed(object? sender, EventArgs e)
        {
            try
            {
                Player.Loaded -= PlayerOnLoaded;
                Player.MouseMove -= Player_OnMouseMove;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    mediaPlayer.Stop();
                    mediaPlayer.Dispose();
                    libVlc.Dispose();
                });
                
                WindowsManager.OpenedWindows.Remove(this);
                
                if (DownloadsPageQ.GetDownloadsPageInstance.openedTorrents.Count < 2)
                {
                    SetThreadExecutionState(ES_CONTINUOUS);
                }
                //Taskbar.Goster();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void DurationSlider_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void DurationSlider_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer != null && !myPositionChanging)
                {
                    myPositionChanging = true;

                    // Yeni pozisyonu ayarla
                    float targetPosition = (float)DurationSlider.Value;

                    // Görevi arka planda çalıştır, UI iş parçacığına güvenerek pozisyonu ayarla
                    Task.Run(() =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Player.MediaPlayer.Position = targetPosition; // Pozisyonu ayarla
                        });
                    }).ContinueWith(_ =>
                    {
                        myPositionChanging = false; // İşlem tamamlandığında bayrağı sıfırla
                    }, TaskScheduler.FromCurrentSynchronizationContext());
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

        private void DurationSlider_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer == null) return;

                // Sürükleme tamamlandığında pozisyonu ayarla
                float targetPosition = (float)DurationSlider.Value;

                // Görevi arka planda çalıştır, UI iş parçacığına güvenerek pozisyonu ayarla
                Task.Run(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        Player.MediaPlayer.Position = targetPosition; // Pozisyonu ayarla
                    });
                }).ContinueWith(_ =>
                {
                    myPositionChanging = false; // İşlem tamamlandığında bayrağı sıfırla
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MediaPlayerOnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
        {
            try
            {
                // Kullanıcı sürükleme yapmıyorsa slider'ı güncelle
                if (!myPositionChanging)
                {
                    this.Dispatcher.InvokeAsync(() =>
                    {
                        DurationSlider.Value = (double)e.Position;
                    });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void VolumeSlider_OnLoaded(object sender, RoutedEventArgs e)
        {
            VolumeSlider.ValueChanged += VolumeSlider_OnValueChanged;
        }

        private void DurationSlider_OnMouseMove(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = false;
        }

        private void DurationSlider_OnMouseLeave(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = true;
        }

        private void ButtonBack_OnMouseEnter(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = false;
        }

        private void ButtonBack_OnMouseLeave(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = true;
        }

        private void UIElement_OnMouseEnter(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = false;
        }

        private void UIElement_OnMouseLeave(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = true;
        }

        private void StackPanelAudio_OnMouseEnter(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = false;
        }

        private void StackPanelAudio_OnMouseLeave(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = true;
        }

        private void StackPanelFullScreen_OnMouseEnter(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = false;
        }

        private void StackPanelFullScreen_OnMouseLeave(object sender, MouseEventArgs e)
        {
            shouldCollapseControlPanel = true;
        }

       
    }
}
