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
using TMDbLib.Objects.Account;
using TMDbLib.Objects.General;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for AccountWatchListPage.xaml
    /// </summary>
    public partial class AccountWatchListPage : Page
    {
        private int pageMovies = 1;
        private int pageTvShow = 1;
        private FavoriteType favoriteType;
        public AccountWatchListPage()
        {
            InitializeComponent();
            pageMovies = 1;
            pageTvShow = 1;
            favoriteType = FavoriteType.Movie;
        }

        private async void LoadWatchListMovies()
        {
            try
            {
                pageMovies = 1;
                favoriteType = FavoriteType.Movie;

                MoviesDisplay.ItemsSource = Service.WatchListMovies;
                TvShowsDisplay.Visibility = Visibility.Collapsed;

                await Service.GetWatchListMovies(1, AccountSortBy.CreatedAt, SortOrder.Descending, true);

                if (Service.WatchListMovies.Count > 0)
                {
                    ViewboxNoMoviesFound.Visibility = Visibility.Collapsed;
                    MoviesDisplay.Visibility = Visibility.Visible;
                }
                else
                {
                    NoMOviesFound.Text = App.Current.Resources["NoMovieFoundString"].ToString();
                    ViewboxNoMoviesFound.Visibility = Visibility.Visible;
                    MoviesDisplay.Visibility = Visibility.Collapsed;
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void LoadWatchListTvShows()
        {
            try
            {
                pageTvShow = 1;
                favoriteType = FavoriteType.TvShow;

                TvShowsDisplay.ItemsSource = Service.WatchListTvShows;
                MoviesDisplay.Visibility = Visibility.Collapsed;

                await Service.GetWatchListTv(1, AccountSortBy.CreatedAt,SortOrder.Descending,true);

                if (Service.WatchListTvShows.Count > 0)
                {
                    ViewboxNoMoviesFound.Visibility = Visibility.Collapsed;
                    TvShowsDisplay.Visibility = Visibility.Visible;
                }
                else
                {
                    NoMOviesFound.Text = App.Current.Resources["NoTvFoundString"].ToString();
                    ViewboxNoMoviesFound.Visibility = Visibility.Visible;
                    TvShowsDisplay.Visibility = Visibility.Collapsed;
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MenuButtonMovie_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LoadWatchListMovies();
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.MenuLine2.SetValue(Grid.ColumnProperty, 0);
                        MenuButtonMovie.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        MenuButtonTvShow.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MenuButtonTvShow_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LoadWatchListTvShows();
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.MenuLine2.SetValue(Grid.ColumnProperty, 2);
                        MenuButtonMovie.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuButtonTvShow.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var movie = MoviesDisplay.SelectedItem as Movie;
                if (movie != null)
                {
                    var movieDetailsPage = new MovieDetailsPage(movie);
                    HomePage.GetHomePageInstance.HomePageNavigation.Navigate(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (e.VerticalOffset == e.ExtentHeight - e.ViewportHeight)
                {
                    if (pageMovies <= Service.MaxWatchListMoviesPage)
                    {
                        pageMovies++;
                        await Service.GetWatchListMovies(pageMovies, AccountSortBy.CreatedAt, SortOrder.Descending,false);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private int selectedMovieIndex = -1;
        private int selectedTvIndex = -1;

        private static bool IsMouseOverTarget(Visual target, Point point)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(target);
            return bounds.Contains(point);
        }
        private void MoviesDisplay_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;

                for (int i = 0; i < MoviesDisplay.Items.Count; i++)
                {
                    var lbi = MoviesDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedMovieIndex = i;
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedMovie = MoviesDisplay.Items[selectedMovieIndex] as Movie;

                if (selectedMovie != null)
                {
                    var result = await Service.client.AccountChangeWatchlistStatusAsync(MediaType.Movie, selectedMovie.Id, false);
                    if (result)
                    {
                        Service.WatchListMovies.Remove(selectedMovie);
                        if (Service.WatchListMovies.Count == 0)
                        {
                            MoviesDisplay.Visibility = Visibility.Collapsed;
                            ViewboxNoMoviesFound.Visibility = Visibility.Visible;
                            NoMOviesFound.Text = App.Current.Resources["NoMovieFoundString"].ToString();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TvShowsDisplay_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;

                for (int i = 0; i < TvShowsDisplay.Items.Count; i++)
                {
                    var lbi = TvShowsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedTvIndex = i;
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TvShowsDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var movie = TvShowsDisplay.SelectedItem as Movie;
                if (movie != null)
                {
                    var movieDetailsPage = new MovieDetailsPage(movie);
                    HomePage.GetHomePageInstance.HomePageNavigation.Navigate(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void RemoveFavoriteTvShow(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedMovie = TvShowsDisplay.Items[selectedTvIndex] as Movie;

                if (selectedMovie != null)
                {
                    var result = await Service.client.AccountChangeWatchlistStatusAsync(MediaType.Tv, selectedMovie.Id, false);
                    if (result)
                    {
                        Service.WatchListTvShows.Remove(selectedMovie);
                        if (Service.WatchListTvShows.Count == 0)
                        {
                            TvShowsDisplay.Visibility = Visibility.Collapsed;
                            ViewboxNoMoviesFound.Visibility = Visibility.Visible;
                            NoMOviesFound.Text = App.Current.Resources["NoTvFoundString"].ToString();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TvShowsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (e.VerticalOffset == e.ExtentHeight - e.ViewportHeight)
                {
                    if (pageTvShow <= Service.MaxWatchListTvPage)
                    {
                        pageTvShow++;
                        await Service.GetWatchListTv(pageTvShow, AccountSortBy.CreatedAt, SortOrder.Descending, false);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void AccountWatchListPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (favoriteType == FavoriteType.Movie)
                {
                    LoadWatchListMovies();
                }
                else
                {
                    LoadWatchListTvShows();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void AccountWatchListPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Clear references to potentially large data
            if (MoviesDisplay != null)
                MoviesDisplay.ItemsSource = null;

            if (TvShowsDisplay != null)
                TvShowsDisplay.ItemsSource = null;
        }
    }
}
