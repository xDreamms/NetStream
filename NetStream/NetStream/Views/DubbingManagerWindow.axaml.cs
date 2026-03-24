using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace NetStream.Views
{
    public partial class DubbingManagerWindow : UserControl
    {
        private PlayerWindow _playerWindow;
        private bool _isDubbing = false;

        public DubbingManagerWindow()
        {
            InitializeComponent();
        }

        public DubbingManagerWindow(PlayerWindow playerWindow)
        {
            InitializeComponent();
            _playerWindow = playerWindow;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isDubbing) return;
            _isDubbing = true;
            
            var startButton = this.FindControl<Button>("StartButton");
            var cancelButton = this.FindControl<Button>("CancelButton");
            startButton.IsEnabled = false;
            cancelButton.IsEnabled = false;

            var service = new NetStream.Services.DubbingService();
            service.StatusChanged += (s, status) => 
            {
                Dispatcher.UIThread.InvokeAsync(() => 
                {
                    var statusText = this.FindControl<TextBlock>("StatusText");
                    if (statusText != null) statusText.Text = status;
                });
            };

            service.ProgressChanged += (s, progress) =>
            {
                Dispatcher.UIThread.InvokeAsync(() => 
                {
                    var progressBar = this.FindControl<ProgressBar>("DubbingProgressBar");
                    var progressText = this.FindControl<TextBlock>("ProgressText");
                    if (progressBar != null) progressBar.Value = progress;
                    if (progressText != null) progressText.Text = $"{(int)progress}%";
                });
            };

            // Start the service
            // We use the path from the player window
            string videoPath = _playerWindow.path;
            
            // Check if path is valid
            if (string.IsNullOrEmpty(videoPath) || !System.IO.File.Exists(videoPath))
            {
                 var statusText = this.FindControl<TextBlock>("StatusText");
                 if (statusText != null) statusText.Text = "Error: Video file not found or invalid path.";
                 _isDubbing = false;
                 startButton.IsEnabled = true;
                 cancelButton.IsEnabled = true;
                 return;
            }

            // Stop the player to release file lock
            if (_playerWindow.Player?.MediaPlayer != null)
            {
                _playerWindow.Player.MediaPlayer.Stop();
            }

            await service.StartDubbingAsync(videoPath, "tr"); // Defaulting to Turkish as requested

            _isDubbing = false;
            startButton.IsEnabled = true;
            cancelButton.IsEnabled = true;
            
             // Optionally close after a delay
             await Task.Delay(2000);
             CloseWindow();
        }

        // Removed SimulateDubbingProcess as it is now in DubbingService

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            if (_playerWindow != null)
            {
                // Re-enable player controls
                _playerWindow.ButtonBack.IsVisible = true;
                _playerWindow.PanelControlVideo.IsVisible = true;
                
                if (_playerWindow.Player?.MediaPlayer != null && !_playerWindow.Player.MediaPlayer.IsPlaying)
                {
                    _playerWindow.Player.MediaPlayer.Play();
                    _playerWindow.PlayButton.Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(new Uri("avares://NetStream/Assets/new_icons/pause.png")));
                }
            }
        }
    }
}
