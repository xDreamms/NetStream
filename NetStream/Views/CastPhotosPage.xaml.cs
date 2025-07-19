using ABI.System;
using System;
using System.Collections.Generic;
using System.Linq;
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
using TMDbLib.Objects.People;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for CastPhotosPage.xaml
    /// </summary>
    public partial class CastPhotosPage : Page
    {
        private Cast cast;
        public CastPhotosPage(Cast cast)
        {
            InitializeComponent();
            this.cast = cast;
        }

        private void ProfileImagesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private FastObservableCollection<CastImage> castImages = new FastObservableCollection<CastImage>();

        private async Task GetCastPhotos()
        {
            try
            {
                var language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;
                var person = await Service.client.GetPersonAsync(cast.Id, language, PersonMethods.Images);
                if (person != null)
                {
                    var photos = person.Images;
                    await Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            ImageCounter.Text = photos.Profiles.Count + " " + Application.Current.Resources["ImagesString"];
                        }));
                    foreach (var image in photos.Profiles)
                    {
                        castImages.Add(new CastImage()
                        {
                            Poster = (Service.client.GetImageUrl("w500",image.FilePath).AbsoluteUri)
                        });
                    }

                    ProfileImagesDisplay.ItemsSource = castImages;
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        } 

        private void ProfileImagesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }

        private async void CastPhotosPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            await GetCastPhotos();
        }

        private void CastPhotosPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Clear collections
            if (castImages != null)
                castImages.Clear();

            // Clear references to UI elements
            if (ProfileImagesDisplay != null)
                ProfileImagesDisplay.ItemsSource = null;
        }
    }
}
