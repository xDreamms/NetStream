using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using TMDbLib.Objects.TvShows;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for TvShowDetailsOverViewPage.xaml
    /// </summary>
    public partial class TvShowDetailsOverViewPage : Page
    {
        private Movie selectedMovie;
        private TvShowDetail tvShowDetail;
        public TvShowDetailsOverViewPage(Movie selectedMovie)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
        }

        private async void TvShowDetailsOverViewPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                tvShowDetail = await Service.GetTvShowDetails(selectedMovie);
                MovieDetailArea.DataContext = tvShowDetail;
                SetVisibilityInfoStrings();
                SetVisibilityIcons();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void SetVisibilityInfoStrings()
        {
            try
            {
                if (tvShowDetail != null)
                {
                    if (String.IsNullOrWhiteSpace(tvShowDetail.Overview))
                    {
                        StoryLineString.Visibility = Visibility.Collapsed;
                        OverviewString.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(tvShowDetail.Director))
                    {
                        DirectorString.Visibility = Visibility.Collapsed;
                        DirectorText.Visibility = Visibility.Collapsed;

                        StatusString.Margin = new Thickness(0, 0, 0, 0);
                        StatusText.Margin = new Thickness(0, 0, 0, 0);
                    }

                    if (String.IsNullOrWhiteSpace(tvShowDetail.Status))
                    {
                        StatusString.Visibility = Visibility.Collapsed;
                        StatusText.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(tvShowDetail.Production))
                    {
                        ProductionString.Visibility = Visibility.Collapsed;
                        ProductionText.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(tvShowDetail.Genre))
                    {
                        GenreString.Visibility = Visibility.Collapsed;
                        GenreText.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(tvShowDetail.Language))
                    {
                        LanguageString.Visibility = Visibility.Collapsed;
                        LanguageText.Visibility = Visibility.Collapsed;
                    }

                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void SetVisibilityIcons()
        {
            try
            {
                if (tvShowDetail != null)
                {
                    if (tvShowDetail.ExternalIdsTvShow == null  || String.IsNullOrWhiteSpace(tvShowDetail.ExternalIdsTvShow.TwitterId))
                    {
                        BtnTwitter.Visibility = Visibility.Collapsed;
                    }
                    if (tvShowDetail.ExternalIdsTvShow == null || String.IsNullOrWhiteSpace(tvShowDetail.ExternalIdsTvShow.FacebookId))
                    {
                        BtnFacebook.Visibility = Visibility.Collapsed;
                    }
                    if (tvShowDetail.ExternalIdsTvShow == null || String.IsNullOrWhiteSpace(tvShowDetail.ExternalIdsTvShow.InstagramId))
                    {
                        BtnInstagram.Visibility = Visibility.Collapsed;
                    }
                    if (tvShowDetail.ExternalIdsTvShow == null || String.IsNullOrWhiteSpace(tvShowDetail.ExternalIdsTvShow.ImdbId))
                    {
                        BtnImadb.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnTwitter_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (tvShowDetail.ExternalIdsTvShow != null && !String.IsNullOrWhiteSpace(tvShowDetail.ExternalIdsTvShow.TwitterId))
            {
                Process.Start(new ProcessStartInfo("https://x.com/" + tvShowDetail.ExternalIdsTvShow.TwitterId) { UseShellExecute = true });
            }
        }

        private void BtnFacebook_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (tvShowDetail.ExternalIdsTvShow != null && !String.IsNullOrWhiteSpace(tvShowDetail.ExternalIdsTvShow.FacebookId))
            {
                Process.Start(new ProcessStartInfo("https://www.facebook.com/" + tvShowDetail.ExternalIdsTvShow.FacebookId) { UseShellExecute = true });
            }
        }

        private void BtnInstagram_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (tvShowDetail.ExternalIdsTvShow != null && !String.IsNullOrWhiteSpace(tvShowDetail.ExternalIdsTvShow.InstagramId))
            {
                Process.Start(new ProcessStartInfo("https://instagram.com/" + tvShowDetail.ExternalIdsTvShow.InstagramId) { UseShellExecute = true });
            }
        }

        private void BtnImdb_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (tvShowDetail.ExternalIdsTvShow != null && !String.IsNullOrWhiteSpace(tvShowDetail.ExternalIdsTvShow.ImdbId))
            {
                Process.Start(new ProcessStartInfo("https://www.imdb.com/title/" + tvShowDetail.ExternalIdsTvShow.ImdbId) { UseShellExecute = true });
            }
        }


        private async void BtnTwitter_OnMouseEnter(object sender, MouseEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    BtnTwitter.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                }));
        }

        private async void BtnTwitter_OnMouseLeave(object sender, MouseEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    BtnTwitter.Foreground = new SolidColorBrush(Colors.White);
                }));
        }

        private async void BtnFacebook_OnMouseEnter(object sender, MouseEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    BtnFacebook.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                }));
        }

        private async void BtnFacebook_OnMouseLeave(object sender, MouseEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    BtnFacebook.Foreground = new SolidColorBrush(Colors.White);
                }));
        }

        private async void BtnInstagram_OnMouseEnter(object sender, MouseEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    BtnInstagram.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                }));
        }

        private async void BtnInstagram_OnMouseLeave(object sender, MouseEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    BtnInstagram.Foreground = new SolidColorBrush(Colors.White);
                }));
        }

        private async void BtnImdb_OnMouseEnter(object sender, MouseEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    BtnImadb.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                }));
        }

        private async void BtnImdb_OnMouseLeave(object sender, MouseEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    BtnImadb.Foreground = new SolidColorBrush(Colors.White);
                }));
        }

        private void DirectorText_OnMouseEnter(object sender, MouseEventArgs e)
        {
            DirectorText.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
        }

        private void DirectorText_OnMouseLeave(object sender, MouseEventArgs e)
        {
            DirectorText.Foreground = new SolidColorBrush(Color.FromArgb(255, 185, 185, 185));
        }

        private void DirectorText_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CastPage castPage = new CastPage(new Cast() { Id = tvShowDetail.DirectorId });
            HomePage.GetHomePageInstance.HomePageNavigation.Navigate(castPage);
        }
    }
}
