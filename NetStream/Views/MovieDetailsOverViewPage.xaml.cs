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
using TMDbLib.Objects.General;
using TMDbLib.Objects.TvShows;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for MovieDetailsOverViewPage.xaml
    /// </summary>
    public partial class MovieDetailsOverViewPage : Page
    {
        private Movie selectedMovie;
        private MovieDetail movieDetail;
        public MovieDetailsOverViewPage(Movie selectedMovie)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
        }
  
        private async void MovieDetailsOverViewPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            movieDetail = await Service.GetMovieDetails(selectedMovie);
            MovieDetailArea.DataContext = movieDetail;
            SetVisibilityIcons();
            SetVisibilityInfoStrings();
        }

        private void SetVisibilityInfoStrings()
        {
            try
            {
                if (movieDetail != null)
                {
                    if (String.IsNullOrWhiteSpace(movieDetail.Overview))
                    {
                        StoryLineString.Visibility = Visibility.Collapsed;
                        OverviewString.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.ReleaseDate))
                    {
                        ReleasedString.Visibility = Visibility.Collapsed;
                        ReleaseDateText.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Director))
                    {
                        DirectorString.Visibility = Visibility.Collapsed;
                        DirectorText.Visibility = Visibility.Collapsed;
                    }

                    if (movieDetail.Revenue == "0M$")
                    {
                        RevenueString.Visibility = Visibility.Collapsed;
                        RevenueText.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Status))
                    {
                        StatusString.Visibility = Visibility.Collapsed;
                        StatusText.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Production))
                    {
                        ProductionString.Visibility = Visibility.Collapsed;
                        ProductionText.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Runtime))
                    {
                        RuntimeString.Visibility = Visibility.Collapsed;
                        RuntimeText.Visibility = Visibility.Collapsed;
                    }

                    if (movieDetail.Budget == "0M$")
                    {
                        BudgetString.Visibility = Visibility.Collapsed;
                        BudgetText.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Genre))
                    {
                        GenreString.Visibility = Visibility.Collapsed;
                        GenreText.Visibility = Visibility.Collapsed;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Language))
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
                if (movieDetail != null)
                {
                    if (movieDetail.ExternalIdsMovie == null || String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.TwitterId))
                    {
                        BtnTwitter.Visibility = Visibility.Collapsed;
                    }
                    if (movieDetail.ExternalIdsMovie == null || String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.FacebookId))
                    {
                        BtnFacebook.Visibility = Visibility.Collapsed;
                    }
                    if (movieDetail.ExternalIdsMovie == null || String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.InstagramId))
                    {
                        BtnInstagram.Visibility = Visibility.Collapsed;
                    }
                    if (movieDetail.ExternalIdsMovie == null || String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.ImdbId))
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
            try
            {
                if (movieDetail.ExternalIdsMovie != null && !String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.TwitterId))
                {
                    Process.Start(new ProcessStartInfo("https://x.com/" + movieDetail.ExternalIdsMovie.TwitterId) { UseShellExecute = true });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnFacebook_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (movieDetail.ExternalIdsMovie != null && !String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.FacebookId))
                {
                    Process.Start(new ProcessStartInfo("https://www.facebook.com/" + movieDetail.ExternalIdsMovie.FacebookId) { UseShellExecute = true });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnInstagram_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (movieDetail.ExternalIdsMovie != null && !String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.InstagramId))
                {
                    Process.Start(new ProcessStartInfo("https://instagram.com/" + movieDetail.ExternalIdsMovie.InstagramId) { UseShellExecute = true });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

    
        private async void BtnTwitter_OnMouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        BtnTwitter.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnTwitter_OnMouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        BtnTwitter.Foreground = new SolidColorBrush(Colors.White);
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnFacebook_OnMouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        BtnFacebook.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnFacebook_OnMouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        BtnFacebook.Foreground = new SolidColorBrush(Colors.White);
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnInstagram_OnMouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        BtnInstagram.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnInstagram_OnMouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        BtnInstagram.Foreground = new SolidColorBrush(Colors.White);
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        private void BtnImadb_OnMouseLeave(object sender, MouseEventArgs e)
        {
            BtnImadb.Foreground = new SolidColorBrush(Colors.White);
        }

        private void BtnImadb_OnMouseEnter(object sender, MouseEventArgs e)
        {
            BtnImadb.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
        }

        private void BtnImadb_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (movieDetail.ExternalIdsMovie != null && !String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.ImdbId))
                {
                    Process.Start(new ProcessStartInfo("https://www.imdb.com/title/" + movieDetail.ExternalIdsMovie.ImdbId) { UseShellExecute = true });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void DirectorText_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                CastPage castPage = new CastPage(new Cast() { Id = movieDetail.DirectorID });
                HomePage.GetHomePageInstance.HomePageNavigation.Navigate(castPage);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void DirectorText_OnMouseEnter(object sender, MouseEventArgs e)
        {
            DirectorText.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
        }

        private void DirectorText_OnMouseLeave(object sender, MouseEventArgs e)
        {
            DirectorText.Foreground = new SolidColorBrush(Color.FromArgb(255, 185, 185, 185));
        }

        private void MovieDetailsOverViewPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            movieDetail = null;
            //selectedMovie = null;
        }
    }
}
