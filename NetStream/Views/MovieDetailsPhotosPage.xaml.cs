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
using TinifyAPI;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using Windows.Media.Protection.PlayReady;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for MovieDetailsPhotosPage.xaml
    /// </summary>
    public partial class MovieDetailsPhotosPage : Page
    {
        private Movie selectedMovie;
        public MovieDetailsPhotosPage(Movie selectedMovie)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
            this.DataContext = this;
            Service.PhotoDetailsBackdrop.Clear();
            Service.PhotoDetailsPoster.Clear();
            PhotosDisplay.ItemsSource = Service.PhotoDetailsBackdrop;
            PostersDisplay.ItemsSource = Service.PhotoDetailsPoster;
        }


        private async void MovieDetailsPhotosPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            await GetMoviePhotos(selectedMovie,this);
        }

        public async Task GetMoviePhotos(Movie selectedMovie, MovieDetailsPhotosPage movieDetailsPhotosPage)
        {
            try
            {
                if (!Service.client.HasConfig)
                {
                    await Service.client.GetConfigAsync();
                }

                ImagesWithId MovieImages = null;

                // Verileri asenkron yükle
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    MovieImages = await Service.client.GetMovieImagesAsync(selectedMovie.Id);
                }
                else if (selectedMovie.ShowType == ShowType.TvShow)
                {
                    MovieImages = await Service.client.GetTvShowImagesAsync(selectedMovie.Id);
                }

                // UI güncellemelerini Dispatcher ile yap
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        movieDetailsPhotosPage.BackDropsImageCounter.Text = $"{MovieImages.Backdrops.Count} " + App.Current.Resources["ImagesString"];
                        movieDetailsPhotosPage.PosterImageCounter.Text = $"{MovieImages.Posters.Count} " + App.Current.Resources["ImagesString"];
                    }));

                // Arka planda verileri yükle
                var backdropTask = LoadImagesAsync(MovieImages.Backdrops, Service.PhotoDetailsBackdrop);
                var posterTask = LoadImagesAsync(MovieImages.Posters, Service.PhotoDetailsPoster);

                // Hem Backdrops hem de Posters için işlemleri bekle
                await Task.WhenAll(backdropTask, posterTask);
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private static async Task LoadImagesAsync(List<ImageData> images, FastObservableCollection<PhotoDetail> collection)
        {
            try
            {
                foreach (var image in images)
                {
                    var mov = new PhotoDetail();
                    var url = Service.client.GetImageUrl("w500", image.FilePath);
                    mov.Poster = url.AbsoluteUri;

                    // Koleksiyona eklerken UI'yi güncelle
                    await Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            collection.Add(mov);  // Koleksiyona ekle
                        }));

                    // Burada DelayNotifications() kullanabilirsiniz
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private void PhotosDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }

        private void PhotosDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void PostersDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void PostersDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }

        private void MovieDetailsPhotosPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Service.PhotoDetailsBackdrop.Clear();
            Service.PhotoDetailsPoster.Clear();
        }
    }
}
