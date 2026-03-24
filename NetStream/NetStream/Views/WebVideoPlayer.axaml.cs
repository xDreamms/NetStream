using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.JSInterop;
using NetStream.Annotations;
using NetStream.Services;

namespace NetStream.Views
{
    public partial class WebVideoPlayer : UserControl, INotifyPropertyChanged
    {
        private readonly IPlayerService _playerService;
        private readonly string _videoElementId = "video-player-" + Guid.NewGuid().ToString("N");
        private bool _isPlaying;
        private bool _isMuted;
        private int _volume = 100;
        private double _position;
        private double _duration;
        private bool _areControlsVisible = true;
        private bool _isFullScreen;
        
        public bool AreControlsVisible 
        { 
            get => _areControlsVisible; 
            set 
            { 
                _areControlsVisible = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public double Position 
        { 
            get => _position; 
            set 
            { 
                _position = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(CurrentPosition)); 
            } 
        }
        
        public double Duration 
        { 
            get => _duration; 
            set 
            { 
                _duration = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(TotalDuration)); 
            } 
        }
        
        public int Volume 
        { 
            get => _volume; 
            set 
            { 
                _volume = value; 
                OnPropertyChanged(); 
                SetVolumeAsync(value).ConfigureAwait(false); 
            } 
        }
        
        public string PlayPauseIcon => _isPlaying ? "⏸" : "▶";
        
        public string CurrentPosition => TimeSpan.FromSeconds(_position).ToString(@"hh\:mm\:ss");
        
        public string TotalDuration => TimeSpan.FromSeconds(_duration).ToString(@"hh\:mm\:ss");

        public event PropertyChangedEventHandler PropertyChanged;

        public WebVideoPlayer()
        {
            InitializeComponent();
            
            // Get the player service from DI
            _playerService = App.Services.GetService(typeof(IPlayerService)) as IPlayerService;
            
            if (_playerService == null)
            {
                throw new InvalidOperationException("IPlayerService not registered in DI container");
            }
            
            this.DataContext = this;
            
            // Subscribe to player events
            _playerService.PositionChanged += PlayerService_PositionChanged;
            _playerService.DurationChanged += PlayerService_DurationChanged;
            _playerService.PlaybackStarted += PlayerService_PlaybackStarted;
            _playerService.PlaybackPaused += PlayerService_PlaybackPaused;
            _playerService.PlaybackStopped += PlayerService_PlaybackStopped;
            _playerService.Error += PlayerService_Error;
            
            // Initialize the player when the control is loaded
            this.Loaded += WebVideoPlayer_Loaded;
        }

        private async void WebVideoPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            // Create the video element in JavaScript
            await AddVideoElementAsync();
            
            // Initialize the player service
            await _playerService.Initialize(_videoElementId);
            
            // Set initial volume
            await _playerService.SetVolume(_volume);
        }
        
        private async Task AddVideoElementAsync()
        {
            // Use JS interop to create the video element
            if (_playerService is WebPlayerService webPlayerService)
            {
                var jsRuntime = ((IJSRuntime)webPlayerService.GetType()
                    .GetProperty("JsRuntime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(webPlayerService));
                
                await jsRuntime.InvokeVoidAsync("createVideoElement", _videoElementId, VideoContainer.GetHashCode());
            }
        }

        // Event handlers for player service events
        private void PlayerService_PositionChanged(TimeSpan obj)
        {
            Position = obj.TotalSeconds;
        }

        private void PlayerService_DurationChanged(TimeSpan obj)
        {
            Duration = obj.TotalSeconds;
        }

        private void PlayerService_PlaybackStarted()
        {
            _isPlaying = true;
            OnPropertyChanged(nameof(PlayPauseIcon));
        }

        private void PlayerService_PlaybackPaused()
        {
            _isPlaying = false;
            OnPropertyChanged(nameof(PlayPauseIcon));
        }

        private void PlayerService_PlaybackStopped()
        {
            _isPlaying = false;
            Position = 0;
            OnPropertyChanged(nameof(PlayPauseIcon));
        }

        private void PlayerService_Error(string obj)
        {
            // Handle error
            Console.WriteLine($"Player error: {obj}");
        }

        // Button click handlers
        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                await _playerService.Pause();
            }
            else
            {
                if (Position == 0)
                {
                    // If at the beginning, play the video
                    // (The actual URL should be set from outside)
                }
                else
                {
                    // Resume playback
                    await _playerService.Resume();
                }
            }
        }

        private async void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Go back 10 seconds
            var newPosition = Math.Max(0, Position - 10);
            await _playerService.Seek(TimeSpan.FromSeconds(newPosition));
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Go forward 10 seconds
            var newPosition = Math.Min(Duration, Position + 10);
            await _playerService.Seek(TimeSpan.FromSeconds(newPosition));
        }

        private async void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            _isMuted = !_isMuted;
            await _playerService.SetVolume(_isMuted ? 0 : _volume);
        }

        private async void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            _isFullScreen = !_isFullScreen;
            await _playerService.SetFullScreen(_isFullScreen);
        }

        private async void Slider_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            await _playerService.Seek(TimeSpan.FromSeconds(Position));
        }

        private async Task SetVolumeAsync(int volume)
        {
            await _playerService.SetVolume(volume);
            _isMuted = volume == 0;
        }

        private void VolumeSlider_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Value" && e.NewValue is double newValue)
            {
                Volume = (int)newValue;
            }
        }

        public async Task PlayAsync(string url)
        {
            await _playerService.Play(url);
        }

        public async Task PauseAsync()
        {
            await _playerService.Pause();
        }

        public async Task StopAsync()
        {
            await _playerService.Stop();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 