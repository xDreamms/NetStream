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
    /// Interaction logic for AccountFavoritesPage.xaml
    /// </summary>
    public partial class AccountFavoritesPage : Page
    {
        private int pageMovies = 1;
        private int pageTvShow = 1;
        private FavoriteType favoriteType;
        public AccountFavoritesPage()
        {
            InitializeComponent();
            pageMovies = 1;
            pageTvShow = 1;
            favoriteType = FavoriteType.Movie;
        }

        private async void LoadFavoriteMovies()
        {
            try
            {
                favoriteType = FavoriteType.Movie;

                MoviesDisplay.ItemsSource = Service.AccountFavoritesMovies;
                TvShowsDisplay.Visibility = Visibility.Collapsed;

                await Service.GetFavoritesMovies(1,AccountSortBy.CreatedAt, SortOrder.Descending);

                if (Service.AccountFavoritesMovies.Count > 0)
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
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void LoadFavoriteTvShows()
        {
            try
            {
                favoriteType = FavoriteType.TvShow;

                TvShowsDisplay.ItemsSource = Service.AccountFavoritesTvShows;
                MoviesDisplay.Visibility = Visibility.Collapsed;

                await Service.GetFavoritesTvShows(1,AccountSortBy.CreatedAt, SortOrder.Descending);
            
                if (Service.AccountFavoritesTvShows.Count > 0)
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
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)MoviesDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
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
                    if (pageMovies <= Service.MaxFavoritesMoviePage)
                    {
                        pageMovies++;
                        await Service.GetFavoritesMoviesMorePages(pageMovies, AccountSortBy.CreatedAt, SortOrder.Descending);
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
                    if (pageTvShow <= Service.MaxFavoritesTvShowPage)
                    {
                        pageTvShow++;
                        await Service.GetFavoritesTvShowsMorePages(pageTvShow, AccountSortBy.CreatedAt, SortOrder.Descending);
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
                Movie selectedTv = (Movie)TvShowsDisplay.SelectedItem;
                if (selectedTv != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedTv);
                    HomePage.GetHomePageInstance.HomePageNavigation.Navigate(movieDetailsPage);
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
            //Movie remove

            try
            {
                var selectedMovie = MoviesDisplay.Items[selectedMovieIndex] as Movie;

                if (selectedMovie != null)
                {
                    var result = await Service.client.AccountChangeFavoriteStatusAsync(MediaType.Movie, selectedMovie.Id, false);
                    if (result)
                    {
                        Service.AccountFavoritesMovies.Remove(selectedMovie);
                        if (Service.AccountFavoritesMovies.Count == 0)
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

        private async void RemoveFavoriteTvShow(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedMovie = TvShowsDisplay.Items[selectedTvIndex] as Movie;

                if (selectedMovie != null)
                {
                    var result = await Service.client.AccountChangeFavoriteStatusAsync(MediaType.Tv, selectedMovie.Id, false);
                    if (result)
                    {
                        Service.AccountFavoritesTvShows.Remove(selectedMovie);
                        if (Service.AccountFavoritesTvShows.Count == 0)
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

        private void AccountFavoritesPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (favoriteType == FavoriteType.Movie)
                {
                    LoadFavoriteMovies();
                }
                else
                {
                    LoadFavoriteTvShows();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
           
        }

        private async void MenuButtonMovie_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LoadFavoriteMovies();
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
                LoadFavoriteTvShows();
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
    }

    public enum FavoriteType
    {
        Movie,
        TvShow
    }
}
