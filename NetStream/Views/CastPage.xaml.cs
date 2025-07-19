using ABI.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using TMDbLib.Objects.General;
using TMDbLib.Objects.People;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for CastPage.xaml
    /// </summary>
    public partial class CastPage : Page
    {
        private Cast cast;
        private CastKnownForPage castKnownForPage;
        public CastPage(Cast cast)
        {
            InitializeComponent();
            this.cast = cast;
            castKnownForPage = new CastKnownForPage(cast);
            CastPageNavigation.Navigate(castKnownForPage);
        }

        private ExternalIdsPerson externalIdsPerson;
        private async Task<Cast> GetCastInfo()
        {
            try
            {
                var language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;
                var person = await Service.client.GetPersonAsync(cast.Id, language, PersonMethods.Images |PersonMethods.ExternalIds);

                if (person != null)
                {
                    var Cast = new Cast()
                    {
                        Biography = person.Biography,
                        BirthDate = person.Birthday != null ? person.Birthday.Value.ToShortDateString():"",
                        BirthPlace = person.PlaceOfBirth,
                        Poster = Service.client.GetImageUrl("w500", person.ProfilePath).AbsoluteUri,
                        Id = person.Id,
                        KnownFor = person.KnownForDepartment,
                        Name = person.Name
                    };
                    externalIdsPerson = person.ExternalIds;
                    return Cast;
                }

            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
            return null;
        }

        private async void SetVisibilityForLinks()
        {
            try
            {
                if(externalIdsPerson == null)
                {
                    await Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            BtnFacebook.Visibility = Visibility.Collapsed;
                            BtnInstagram.Visibility = Visibility.Collapsed;
                            BtnImadb.Visibility = Visibility.Collapsed;
                            BtnTwitter.Visibility = Visibility.Collapsed;
                        }));
                }
                else
                {
                    if (String.IsNullOrWhiteSpace(externalIdsPerson.FacebookId))
                    {
                        await Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                BtnFacebook.Visibility = Visibility.Collapsed;
                            }));
                    }
                    if (String.IsNullOrWhiteSpace(externalIdsPerson.InstagramId))
                    {
                        await Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                BtnInstagram.Visibility = Visibility.Collapsed;
                            }));
                    }
                    if (String.IsNullOrWhiteSpace(externalIdsPerson.ImdbId))
                    {
                        await Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                BtnImadb.Visibility = Visibility.Collapsed;
                            }));
                    }
                    if (String.IsNullOrWhiteSpace(externalIdsPerson.TwitterId))
                    {
                        await Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                BtnTwitter.Visibility = Visibility.Collapsed;
                            }));
                    }
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void CastPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            CastPageNavigation.Navigated+= CastPageNavigationOnNavigated;
            CastInfo.DataContext = await GetCastInfo();
            SetVisibilityForLinks();
        }

        private void CastPageNavigationOnNavigated(object sender, NavigationEventArgs e)
        {
            NavigationService.RemoveBackEntry();
        }

        private void GetHomePageInstanceOnMyEvent(object? sender, EventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        private async void KnownForMenuTab_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.MenuLine.SetValue(Grid.ColumnProperty, 0);
                        KnownForMenuTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        CreditsMenuTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        PhotosMenuTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    }));

                castKnownForPage = new CastKnownForPage(cast);
                CastPageNavigation.Navigate(castKnownForPage);
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void CreditsMenuTab_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.MenuLine.SetValue(Grid.ColumnProperty, 2);
                        KnownForMenuTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        CreditsMenuTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        PhotosMenuTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    }));

                CastCreditsPage castCreditsPage = new CastCreditsPage(cast);
                CastPageNavigation.Navigate(castCreditsPage);
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void PhotosMenuTab_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.MenuLine.SetValue(Grid.ColumnProperty, 4);
                        KnownForMenuTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        CreditsMenuTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        PhotosMenuTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    }));

                CastPhotosPage castPhotosPage = new CastPhotosPage(cast);
                CastPageNavigation.Navigate(castPhotosPage);
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnFacebook_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(externalIdsPerson.FacebookId))
                {
                    Process.Start(new ProcessStartInfo("https://www.facebook.com/" + externalIdsPerson.FacebookId) { UseShellExecute = true });
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnInstagram_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(externalIdsPerson.InstagramId))
                {
                    Process.Start(new ProcessStartInfo("https://instagram.com/" + externalIdsPerson.InstagramId) { UseShellExecute = true });
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnImadb_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(externalIdsPerson.ImdbId))
                {
                    Process.Start(new ProcessStartInfo("https://www.imdb.com/name/" + externalIdsPerson.ImdbId) { UseShellExecute = true });
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnTwitter_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(externalIdsPerson.TwitterId))
                {
                    Process.Start(new ProcessStartInfo("https://x.com/" + externalIdsPerson.TwitterId) { UseShellExecute = true });
                }
            }
            catch (System.Exception exception)
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
            catch (System.Exception exception)
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
                        BtnFacebook.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    }));
            }
            catch (System.Exception exception)
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
            catch (System.Exception exception)
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
                        BtnInstagram.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    }));
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnImadb_OnMouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        BtnImadb.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                    }));
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnImadb_OnMouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        BtnImadb.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    }));
            }
            catch (System.Exception exception)
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
            catch (System.Exception exception)
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
                        BtnTwitter.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    }));
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if(!castKnownForPage.isItemLoadingFinished) return;
                if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 50)
                {
                    if (castKnownForPage.MoviesPageIndex <= castKnownForPage.MaxMoviesPage || castKnownForPage.TvShowsPageIndex <= castKnownForPage.MaxTvShowsPage)
                        await castKnownForPage.GetKnownForMovies();
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CastPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
