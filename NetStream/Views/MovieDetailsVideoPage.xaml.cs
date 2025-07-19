using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using System.Windows.Threading;
using Serilog;
using TMDbLib.Client;
using Vlc.DotNet.Wpf;
using YoutubeExplode;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for MovieDetailsVideoPage.xaml
    /// </summary>
    public partial class MovieDetailsVideoPage : Page
    {
        private Movie selectedMovie;
        private MovieDetailsPage movieDetailsPage;
        public MovieDetailsVideoPage(Movie selectedMovie, MovieDetailsPage movieDetailsPage)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
            this.DataContext = this;
            this.movieDetailsPage = movieDetailsPage;
        }

        private async void MovieDetailsVideoPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await Service.GetVideos(selectedMovie,this);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void VideosDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }


        public FastObservableCollection<VideoDetail> VideoDetails
        {
            get
            {
                return Service.VideoDetails;
            }
        }

        

        private async void VideosDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var videoDetail = VideosDisplay.SelectedItem as VideoDetail;
                if (videoDetail != null)
                {
                    movieDetailsPage.sourceProvider?.Dispose();
                    var trailerPage = new TrailerPlayPage(videoDetail);
                    trailerPage.Owner = Application.Current.MainWindow;
                    trailerPage.Show();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MovieDetailsVideoPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Service.VideoDetails.Clear();
            //selectedMovie = null;
        }
    }
}
