using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using Serilog;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using NetStream;
using NetStream.Controls;
using Path = System.IO.Path;

namespace NetStream.Views
{
    public partial class TrailerPlayPage : UserControl, IDisposable
    {
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SetThreadExecutionState(uint esFlags);

        private LibVLC libVlc;
        private MediaPlayer mediaPlayer;

        private bool isFullScreen = false;
        private bool isFullScreenToggleInProgress = false;
        private VideoDetail videoDetail;
        private bool shouldCollapseControlPanel = true;

        // Mouse inactivity timer for auto-hiding controls
        private System.Timers.Timer _mouseInactivityTimer;
        private DateTime _lastMouseMoveTime;
        private const int MOUSE_INACTIVITY_TIMEOUT = 2000; // 2 seconds

        // Volume popup
        private bool _isVolumePopupOpen = false;
        private bool _isMouseOverVolumePopup = false;
        private bool _isMouseOverButtonMute = false;
        private System.Timers.Timer _volumePopupTimer;

        // RangeCanvas for selection ranges (always full for trailer)
        private RangeCanvas rangeCanvas;

        public string videoPath;
        private bool success = false;
        private bool closed = false;

        private float seconds10 = 0;
        private long durationInSeconds = 0;
        private float lastPos;
        private bool myPositionChanging;
        private int saveVolume = 50;

        // Cleanup lock
        private readonly object _cleanupLock = new object();
        private bool _isCleaningUp = false;

        // Throttle
        private DateTime _lastPositionUpdateTime = DateTime.MinValue;
        private const int PositionUpdateThrottleMs = 100;
        private DateTime _lastTimeUpdateTime = DateTime.MinValue;
        private const int TimeUpdateThrottleMs = 250;

        // Scale
        private bool isUp = false;
        private bool isInScaleProcess = false;

        private DispatcherTimer dispatcherTimer;

        public TrailerPlayPage()
        {
            InitializeComponent();
            shouldCollapseControlPanel = true;
            _volumePopupTimer = new System.Timers.Timer(300);
            _volumePopupTimer.Elapsed += VolumePopupTimerOnElapsed;
            _volumePopupTimer.AutoReset = false;
        }

        public TrailerPlayPage(VideoDetail videoDetail)
        {
            InitializeComponent();
            shouldCollapseControlPanel = true;
            SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
            this.videoDetail = videoDetail;
            videoPath = Path.Combine(AppSettingsManager.appSettings.YoutubeVideoPath, videoDetail.Name + ".mp4");
            Console.WriteLine(videoPath);

            MainWindow.Instance.SizeChanged += MainWindowOnSizeChanged;
            ApplyResponsiveLayout(MainWindow.Instance.screenWidth);
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() => { MovieNameText.Text = videoDetail.Name; });

            _volumePopupTimer = new System.Timers.Timer(300);
            _volumePopupTimer.Elapsed += VolumePopupTimerOnElapsed;
            _volumePopupTimer.AutoReset = false;

            // Initialize mouse inactivity timer
            _mouseInactivityTimer = new System.Timers.Timer(MOUSE_INACTIVITY_TIMEOUT);
            _mouseInactivityTimer.Elapsed += MouseInactivityTimerOnElapsed;
            _mouseInactivityTimer.AutoReset = false;
            _lastMouseMoveTime = DateTime.Now;

            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 750);
            dispatcherTimer.Tick += DispatcherTimerOnTick;
            dispatcherTimer.Start();
        }

        #region Responsive Layout

        private double CalculateScaledValue(double width, double minValue, double maxValue)
        {
            const double minWidth = 320;
            const double maxWidth = 3840;

            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));

            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);

            return Math.Round(scaledValue);
        }

        public void ApplyResponsiveLayout(double width)
        {
            var iconSize = CalculateScaledValue(width, 24, 70);
            var iconSizeBack = CalculateScaledValue(width, 20, 50);

            BackImage.FontSize = iconSizeBack;
            PlayButton.FontSize = iconSize;
            MuteButton.FontSize = iconSize;
            FullScreenIcon.FontSize = iconSize;
            RewindIcon.FontSize = iconSize;
            ForwardIcon.FontSize = iconSize;

            var textSize = CalculateScaledValue(width, 18, 30);
            MovieNameText.FontSize = textSize;
            VolumeText.FontSize = textSize;
            LoadingTextBlock.FontSize = textSize;
            TxtCurrentTime.FontSize = textSize;
            TxtDuration.FontSize = textSize;
        }

        private void MainWindowOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.width);
        }

        #endregion

        #region Timer

        private void DispatcherTimerOnTick(object? sender, EventArgs e)
        {
            bool isPointerOverSlider = DurationSlider?.IsPointerOver ?? false;
            if (isUp && !isPointerOverSlider && !isInScaleProcess)
            {
                isUp = false;
                rangeCanvas?.ScaleDown();
                DurationSlider.Classes.Remove("ScaledUp");
            }
        }

        #endregion

        #region Event Handlers - Loading

        private void TrailerPlayPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Focus();

            try
            {
                Log.Information("TrailerPlayPage_OnLoaded: Initializing LibVLC and MediaPlayer");
                libVlc = new LibVLC();
                mediaPlayer = new MediaPlayer(libVlc);
                Player.Loaded += PlayerOnLoaded;
                Log.Information("TrailerPlayPage_OnLoaded: Initialization successful");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "TrailerPlayPage_OnLoaded: Error initializing video player");
            }
        }

        private async void PlayerOnLoaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                ProgressGrid.IsVisible = true;
                LoadingTextBlock.IsVisible = true;
                ProgressBarToPlay.IsIndeterminate = true;
                ProgressBarToPlay.IsVisible = true;
                PanelControlVideo.IsVisible = false;

                // RunFFMpegAsync'i background thread'de calistir - UI donmasini onle
                await Task.Run(async () =>
                {
                    await RunFFMpegAsync();
                });

                // UI guncellemelerini UI thread'de yap
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressGrid.IsVisible = false;

                    if (success)
                    {
                        try
                        {
                            Player.MediaPlayer = mediaPlayer;

                            double volumeValue;
                            double.TryParse(File.ReadAllText(AppSettingsManager.appSettings.VolumeCachePath), out volumeValue);
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

                            // Selection ranges: trailer always fully downloaded, set full range
                            SetFullSelectionRange();
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error setting up player: {ex.Message}");
                            ProgressGrid.IsVisible = false;
                        }
                    }
                    else
                    {
                        ProgressGrid.IsVisible = false;
                    }
                });
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressGrid.IsVisible = false;
                });
            }
        }

        #endregion

        #region YouTube Download

        private async Task RunFFMpegAsync()
        {
            try
            {
                var ytdl = new YoutubeDL();

                ytdl.YoutubeDLPath = Path.Combine(Environment.CurrentDirectory, "yt-dlp.exe");
                ytdl.FFmpegPath = Environment.CurrentDirectory + "\\ffmpeg\\bin\\ffmpeg.exe";
                Console.WriteLine(ytdl.YoutubeDLPath);
                Console.WriteLine(ytdl.FFmpegPath);
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
                Console.WriteLine(errorMessage);
                Log.Error(errorMessage);
            }
        }

        #endregion

        #region Selection Ranges

        private void SetFullSelectionRange()
        {
            try
            {
                if (rangeCanvas == null) return;

                // Trailer always fully downloaded - fill from current position to end
                rangeCanvas.Width = DurationSlider.Bounds.Width;
                var currentPos = Player?.MediaPlayer?.Position ?? 0f;
                UpdateSelectionRange(currentPos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting full selection range: {ex.Message}");
            }
        }

        private void UpdateSelectionRange(float position)
        {
            try
            {
                if (rangeCanvas == null) return;

                // Start follows current position, end stays at 1.0 (fully downloaded)
                var regions = new System.Collections.Generic.List<(double start, double end)>
                {
                    (position, 1.0)
                };
                rangeCanvas.SetRegions(regions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating selection range: {ex.Message}");
            }
        }

        #endregion

        #region Media Player Events

        private void MediaPlayerOnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            try
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TxtDuration.Text = TimeSpan.FromMilliseconds(e.Length).ToString().Substring(0, 8);
                    if (ProgressGrid.IsVisible)
                    {
                        ProgressGrid.IsVisible = false;
                    }
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

        private void MediaPlayerOnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            try
            {
                // Throttle
                var now = DateTime.UtcNow;
                if ((now - _lastTimeUpdateTime).TotalMilliseconds < TimeUpdateThrottleMs)
                {
                    return;
                }
                _lastTimeUpdateTime = now;

                Dispatcher.UIThread.InvokeAsync(() =>
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

        private void MediaPlayerOnPositionChanged(object sender, MediaPlayerPositionChangedEventArgs e)
        {
            try
            {
                if (myPositionChanging) return;

                // Throttle
                var now = DateTime.UtcNow;
                if ((now - _lastPositionUpdateTime).TotalMilliseconds < PositionUpdateThrottleMs)
                {
                    return;
                }
                _lastPositionUpdateTime = now;

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        DurationSlider.Value = e.Position;
                        lastPos = e.Position;
                        UpdateSelectionRange(e.Position);
                    }
                    catch { }
                });
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        #endregion

        #region Player Controls

        private void ButtonPlay_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer == null) return;
                if (mediaPlayer.IsPlaying)
                {
                    mediaPlayer.Pause();
                    PlayButton.Value = "fa-regular fa-play";
                }
                else
                {
                    mediaPlayer.Play();
                    PlayButton.Value = "fa-regular fa-pause";
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void Player_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer == null) return;
                if (mediaPlayer.IsPlaying)
                {
                    mediaPlayer.Pause();
                    PlayButton.Value = "fa-regular fa-play";
                }
                else
                {
                    mediaPlayer.Play();
                    PlayButton.Value = "fa-regular fa-pause";
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonRewind_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            if (Player.MediaPlayer != null)
                Player.MediaPlayer.Position = Player.MediaPlayer.Position - seconds10;
        }

        private void ButtonForward_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            if (Player.MediaPlayer != null)
                Player.MediaPlayer.Position = Player.MediaPlayer.Position + seconds10;
        }

        private void ButtonMute_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            if (VolumeSlider.Value == 0)
            {
                VolumeSlider.Value = saveVolume;
            }
            else
            {
                saveVolume = Convert.ToInt32(VolumeSlider.Value);
                VolumeSlider.Value = 0;
            }
        }

        private void ButtonFullScreen_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            if (MainWindow.Instance.WindowState == WindowState.Normal)
            {
                FullScreen();
            }
            else
            {
                NormalScreen();
            }
        }

        #endregion

        #region Fullscreen

        public void FullScreen()
        {
            try
            {
                if (isFullScreen) return;

                MainWindow.Instance.WindowState = WindowState.FullScreen;
                MainWindow.Instance.HideTitleBar();
                Taskbar.Hide();
                isFullScreen = true;
                ButtonBack.IsVisible = true;
                MovieNameText.IsVisible = true;
                Console.WriteLine("Entered full screen mode");
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
                if (!isFullScreen) return;

                MainWindow.Instance.WindowState = WindowState.Normal;
                MainWindow.Instance.ShowTitleBar();
                Taskbar.Show();
                isFullScreen = false;
                Console.WriteLine("Exited full screen mode");
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void Player_OnMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isFullScreenToggleInProgress) return;

                isFullScreenToggleInProgress = true;

                if (!isFullScreen)
                {
                    FullScreen();
                }
                else
                {
                    NormalScreen();
                }

                e.Handled = true;
                Task.Delay(500).ContinueWith(_ => { isFullScreenToggleInProgress = false; });
            }
            catch (Exception exception)
            {
                isFullScreenToggleInProgress = false;
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        #endregion

        #region Mouse & Controls Visibility

        private bool _isMouseMoveHandled = false;

        private void Player_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isMouseMoveHandled || isFullScreenToggleInProgress) return;

            _isMouseMoveHandled = true;
            try
            {
                ButtonBack.IsVisible = true;
                MovieNameText.IsVisible = true;
                PanelControlVideo.IsVisible = true;
                this.Cursor = Cursor.Default;

                _lastMouseMoveTime = DateTime.Now;

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

        private void MouseInactivityTimerOnElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (shouldCollapseControlPanel && !isFullScreenToggleInProgress && !_isVolumePopupOpen)
                    {
                        PanelControlVideo.IsVisible = false;
                        ButtonBack.IsVisible = false;
                        MovieNameText.IsVisible = false;
                        this.Cursor = new Cursor(StandardCursorType.None);
                    }
                    else if (!shouldCollapseControlPanel)
                    {
                        PanelControlVideo.IsVisible = true;
                        ButtonBack.IsVisible = true;
                        MovieNameText.IsVisible = true;
                        this.Cursor = Cursor.Default;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mouse inactivity timer error: {ex.Message}");
            }
        }

        private void PanelControlVideo_OnMouseLeave(object? sender, PointerEventArgs e)
        {
            shouldCollapseControlPanel = true;
        }

        private void PanelControlVideo_OnMouseEnter(object? sender, PointerEventArgs e)
        {
            shouldCollapseControlPanel = false;
        }

        #endregion

        #region Volume Popup

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
                    VolumePopup.IsOpen = true;
                    _isVolumePopupOpen = true;
                    DurationSlider.IsVisible = false;
                }

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
                _volumePopupTimer.Start();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void VolumePopup_OnPointerEntered(object? sender, PointerEventArgs e)
        {
            _isMouseOverVolumePopup = true;
            if (_volumePopupTimer.Enabled)
            {
                _volumePopupTimer.Stop();
            }
        }

        private void VolumePopup_OnPointerExited(object? sender, PointerEventArgs e)
        {
            _isMouseOverVolumePopup = false;
            _volumePopupTimer.Start();
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
                        MuteButton.Value = "fa-regular fa-volume-xmark";
                    }
                    else if (VolumeSlider.Value > 0 && VolumeSlider.Value <= 100)
                    {
                        MuteButton.Value = "fa-regular fa-volume";
                    }
                    else if (VolumeSlider.Value > 100 && VolumeSlider.Value <= 200)
                    {
                        MuteButton.Value = "fa-regular fa-volume-high";
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

        private void VolumeSlider_OnLoaded(object sender, RoutedEventArgs e)
        {
            VolumeSlider.ValueChanged += VolumeSlider_OnValueChanged;
        }

        #endregion

        #region Mouse Wheel

        private void Player_OnMouseWheel(object sender, PointerWheelEventArgs e)
        {
            try
            {
                var delta = e.Delta.Y;

                if (delta > 0)
                {
                    if (VolumeSlider.Value + 5 > 200)
                        VolumeSlider.Value = 200;
                    else
                        VolumeSlider.Value += 5;
                }
                else
                {
                    if (VolumeSlider.Value - 5 < 0)
                        VolumeSlider.Value = 0;
                    else
                        VolumeSlider.Value -= 5;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        #endregion

        #region Duration Slider

        private void DurationSlider_OnMouseWheel(object sender, PointerWheelEventArgs e)
        {
            if (Player.MediaPlayer == null) return;
            if (DurationSlider.Value <= 1 && DurationSlider.Value >= 0)
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
        }

        private void DurationSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
        }

        private void DurationSlider_OnMouseMove(object sender, PointerEventArgs e)
        {
            shouldCollapseControlPanel = false;
        }

        private void DurationSlider_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer == null) return;
                double sliderWidth = DurationSlider.Bounds.Width;
                var mousePosition = e.GetPosition(DurationSlider);
                double relativePosition = mousePosition.X / sliderWidth;
                Player.MediaPlayer.Position = (float)Math.Clamp(relativePosition, 0.0, 1.0);
            }
            catch (Exception exception)
            {
                Log.Error($"Error in DurationSlider_OnPointerReleased: {exception.Message}");
            }
        }

        private void DurationSlider_OnDragStarted(object? sender, VectorEventArgs e)
        {
            myPositionChanging = true;
        }

        private void DurationSlider_OnDragCompleted(object? sender, VectorEventArgs e)
        {
            try
            {
                if (Player.MediaPlayer == null) return;
                myPositionChanging = false;
                // Trailer is always fully downloaded, so no piece range check needed
                Player.MediaPlayer.Position = (float)(sender as Slider).Value;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void DurationSlider_OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            Panel rootPanel = e.NameScope.Find<Panel>("PART_RootPanel");
            if (rootPanel != null)
            {
                // RangeCanvas - always fully filled for trailer
                rangeCanvas = new RangeCanvas
                {
                    BackgroundColor = new SolidColorBrush(Colors.Transparent),
                    RegionColor = new SolidColorBrush(Color.FromRgb(203, 203, 203)),
                    Height = 5,
                    Width = DurationSlider.Bounds.Width,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    IsVisible = true,
                    IsHitTestVisible = false,
                };

                rootPanel.Children.Insert(1, rangeCanvas);

                DurationSlider.PointerEntered += DurationSlider_PointerEntered;
                DurationSlider.PointerExited += DurationSlider_PointerExited;

                DurationSlider.GetObservable(BoundsProperty).Subscribe(bounds =>
                {
                    try
                    {
                        if (rangeCanvas != null && bounds.Width > 0)
                        {
                            rangeCanvas.Width = bounds.Width;
                            SetFullSelectionRange();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating RangeCanvas width: {ex.Message}");
                    }
                });

                // Set initial full range
                SetFullSelectionRange();
            }
        }

        private void DurationSlider_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (isInScaleProcess) return;
            isInScaleProcess = true;

            if (!isUp)
            {
                rangeCanvas?.ScaleUp();
                DurationSlider.Classes.Add("ScaledUp");
                isUp = true;
            }
            isInScaleProcess = false;
        }

        private void DurationSlider_PointerExited(object? sender, PointerEventArgs e)
        {
            if (isInScaleProcess) return;
            isInScaleProcess = true;

            if (isUp)
            {
                rangeCanvas?.ScaleDown();
                DurationSlider.Classes.Remove("ScaledUp");
                isUp = false;
            }
            isInScaleProcess = false;
        }

        #endregion

        #region Keyboard

        private void TrailerPlayPage_OnKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (mediaPlayer == null) return;

            try
            {
                if (Player.MediaPlayer == null) return;

                switch (e.Key)
                {
                    case Key.Space:
                        if (Player.MediaPlayer.IsPlaying)
                        {
                            Player.MediaPlayer.Pause();
                            PlayButton.Value = "fa-regular fa-play";
                        }
                        else
                        {
                            Player.MediaPlayer.Pause();
                            PlayButton.Value = "fa-regular fa-pause";
                        }
                        break;

                    case Key.Right:
                        Player.MediaPlayer.Position = Player.MediaPlayer.Position + seconds10;
                        break;

                    case Key.Left:
                        Player.MediaPlayer.Position = Player.MediaPlayer.Position - seconds10;
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        #endregion

        #region Back / Close

        private async void ButtonBack_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                await Close();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);

                try
                {
                    Taskbar.Show();
                    MainWindow.Instance.ShowTitleBar();
                    MainWindow.Instance.SetContent(MainView.Instance);
                }
                catch { }
            }
        }

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
                Log.Information("TrailerPlayPage CleanupVideoPlayer: Starting cleanup");

                // CRITICAL: Detach from UI immediately to prevent AccessViolation during Dispose
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (Player != null)
                    {
                        Player.MediaPlayer = null;
                    }
                });

                // 1. Get references under lock
                MediaPlayer tempMediaPlayer = null;
                LibVLC tempLibVlc = null;

                lock (_cleanupLock)
                {
                    tempMediaPlayer = mediaPlayer;
                    tempLibVlc = libVlc;
                }

                // 2. Stop and remove event handlers, then dispose MediaPlayer
                if (tempMediaPlayer != null)
                {
                    try
                    {
                        if (tempMediaPlayer.IsPlaying)
                        {
                            tempMediaPlayer.Stop();
                        }

                        tempMediaPlayer.LengthChanged -= MediaPlayerOnLengthChanged;
                        tempMediaPlayer.TimeChanged -= MediaPlayerOnTimeChanged;
                        tempMediaPlayer.PositionChanged -= MediaPlayerOnPositionChanged;

                        // Dispose in background to avoid UI freeze
                        await Task.Run(() =>
                        {
                            try
                            {
                                tempMediaPlayer.Dispose();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "TrailerPlayPage CleanupVideoPlayer: Error disposing MediaPlayer");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "TrailerPlayPage CleanupVideoPlayer: Error stopping MediaPlayer");
                    }

                    lock (_cleanupLock)
                    {
                        mediaPlayer = null;
                    }
                }

                // 3. Dispose LibVLC after MediaPlayer is fully disposed
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
                            Log.Error(ex, "TrailerPlayPage CleanupVideoPlayer: Error disposing LibVLC");
                        }
                    });

                    lock (_cleanupLock)
                    {
                        libVlc = null;
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                Log.Information("TrailerPlayPage CleanupVideoPlayer: Cleanup completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "TrailerPlayPage CleanupVideoPlayer: General error");
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
            try
            {
                try
                {
                    if (isFullScreen)
                        Taskbar.Show();
                }
                catch (Exception ex)
                {
                    Log.Error($"Taskbar restore error: {ex.Message}");
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

                if (_mouseInactivityTimer != null)
                {
                    _mouseInactivityTimer.Stop();
                    _mouseInactivityTimer.Elapsed -= MouseInactivityTimerOnElapsed;
                    _mouseInactivityTimer.Dispose();
                    _mouseInactivityTimer = null;
                }

                Player.Loaded -= PlayerOnLoaded;
                closed = true;

                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.SizeChanged -= MainWindowOnSizeChanged;
                }

                if (dispatcherTimer != null)
                {
                    dispatcherTimer.Stop();
                    dispatcherTimer.Tick -= DispatcherTimerOnTick;
                    dispatcherTimer = null;
                }

                if (DurationSlider != null)
                {
                    DurationSlider.PointerEntered -= DurationSlider_PointerEntered;
                    DurationSlider.PointerExited -= DurationSlider_PointerExited;
                }

                // Cleanup video player BEFORE changing content (while still in visual tree)
                await CleanupVideoPlayer();
                SetThreadExecutionState(ES_CONTINUOUS);

                MainWindow.Instance.ShowTitleBar();
                MainWindow.Instance.SetContent(MainView.Instance);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);

                try
                {
                    Taskbar.Show();
                    MainWindow.Instance.ShowTitleBar();
                }
                catch { }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            // CleanupVideoPlayer is already called in Close() before SetContent,
            // so no need to call Close() again here (would cause double cleanup crash)
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                if (_volumePopupTimer != null)
                {
                    _volumePopupTimer.Elapsed -= VolumePopupTimerOnElapsed;
                    _volumePopupTimer.Dispose();
                    _volumePopupTimer = null;
                }

                if (_mouseInactivityTimer != null)
                {
                    _mouseInactivityTimer.Elapsed -= MouseInactivityTimerOnElapsed;
                    _mouseInactivityTimer.Dispose();
                    _mouseInactivityTimer = null;
                }

                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.SizeChanged -= MainWindowOnSizeChanged;
                }

                if (dispatcherTimer != null)
                {
                    dispatcherTimer.Stop();
                    dispatcherTimer.Tick -= DispatcherTimerOnTick;
                    dispatcherTimer = null;
                }

                if (DurationSlider != null)
                {
                    DurationSlider.PointerEntered -= DurationSlider_PointerEntered;
                    DurationSlider.PointerExited -= DurationSlider_PointerExited;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dispose error: {ex.Message}");
            }
        }

        #endregion
    }
}
