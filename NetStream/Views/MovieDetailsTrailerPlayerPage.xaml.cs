using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
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
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for MovieDetailsTrailerPlayerPage.xaml
    /// </summary>
    public partial class MovieDetailsTrailerPlayerPage : Page
    {
        private LibVLCSharp.Shared.MediaPlayer mediaPlayer;
        private LibVLC libVlc;
        private string videoPath;
        public MovieDetailsTrailerPlayerPage(string videoPath)
        {
            InitializeComponent();
            this.videoPath = videoPath;
        }

        private void TrailerPlayer_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TrailerPlayer.MediaPlayer = mediaPlayer;
                using (var media = new Media(libVlc, new Uri(videoPath)))
                    TrailerPlayer.MediaPlayer.Play(media);
                TrailerPlayer.MediaPlayer.EnableKeyInput = false;
                TrailerPlayer.MediaPlayer.EnableMouseInput = false;

                TrailerPlayer.MediaPlayer.Stopped += MediaPlayerOnStopped;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MediaPlayerOnStopped(object? sender, EventArgs e)
        {
            
        }

        private void MovieDetailsTrailerPlayerPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                libVlc = new LibVLC();
                mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(libVlc);
                Unloaded += OnUnloaded;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            mediaPlayer.Dispose();
            libVlc.Dispose();
        }
    }
}
