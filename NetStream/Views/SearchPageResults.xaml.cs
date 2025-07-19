using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
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
using NetStream.Annotations;
using Serilog;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for SearchPageResults.xaml
    /// </summary>
    ///

    public partial class SearchPageResults : Page , INotifyPropertyChanged,IDisposable
    {
        public string searchKey = "";
        private SearchType searchType;

        private int searchMoviesPage = 1;
        private int searchTvShowsPage = 1;
        private int searchPersonPage = 1;

        public int TotalResultMovies = 0;
        public int TotalResultTvShows = 0;
        public int TotalResultPersons = 0;

        public SearchPageResults()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public string SearchKey {
            get
            {
                return "Search result for: "+searchKey;
            } 
            set
            {
                searchKey = value;
                NotifyPropertyChanged("SearchKey");
            }
        }




        private void MoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)SearchedMoviesTvShowsDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    HomePage.NavigationService.Navigate(movieDetailsPage);
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
                    switch (searchType)
                    {
                        case SearchType.Movie:
                            searchMoviesPage++;
                            await Service.GetMorePagesSearchedMovies(searchKey,  searchMoviesPage);
                            break;
                        case SearchType.TvShow:
                            searchTvShowsPage++;
                            await Service.GetMorePagesSearchedTvShows(searchKey,  searchTvShowsPage);
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        DebounceHelper debounceHelper = new DebounceHelper();

        private async void FrameworkElement_OnTargetUpdated(object? sender, DataTransferEventArgs e)
        {
            //await Task.Delay(200);
            try
            {
                await debounceHelper.DebouncedAction(async () =>
                {
                    switch (searchType)
                    {
                        case SearchType.Movie:
                            Service.SearchedTvShowsResult.Clear();
                            Service.SearchedCastsResult.Clear();
                            SearchedMoviesTvShowsDisplay.ItemsSource = Service.SearchedMoviesResult;
                            await Service.GetSearchedMovies(searchKey, 1, this);
                            break;
                        case SearchType.TvShow:
                            Service.SearchedMoviesResult.Clear();
                            Service.SearchedCastsResult.Clear();
                            SearchedMoviesTvShowsDisplay.ItemsSource = Service.SearchedTvShowsResult;
                            await Service.GetSearchedTvShows(searchKey, 1, this);
                            break;
                        case SearchType.Person:
                            Service.SearchedMoviesResult.Clear();
                            Service.SearchedTvShowsResult.Clear();
                            SearchedCastsDisplay.ItemsSource = Service.SearchedCastsResult;
                            await Service.GetSearchedCasts(searchKey, 1, this);
                            break;
                    }
                }, 300); // 300 ms bekle
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
           
        }


        private void SearchPageResults_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }


        private void SearchedCastsDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Cast selectedCast = (Cast)SearchedCastsDisplay.SelectedItem;
                if (selectedCast != null)
                {
                    CastPage castPage = new CastPage(selectedCast);
                    HomePage.NavigationService.Navigate(castPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void SearchedCastsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (e.VerticalOffset == e.ExtentHeight - e.ViewportHeight)
                {
                    searchPersonPage++;
                    await Service.GetMorePagesSearchedCasts(searchKey,  searchPersonPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MoviesTab_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SearchedMoviesTvShowsDisplay.ItemsSource = Service.SearchedMoviesResult;
                if (Service.SearchedMoviesResult.Count == 0)
                {
                    await Service.GetSearchedMovies(searchKey,  1, this);
                }
                searchType = SearchType.Movie;
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.ItemCountText.Text = TotalResultMovies + " items";
                        SearchedMoviesTvShowsDisplay.Visibility = Visibility.Visible;
                        SearchedCastsDisplay.Visibility = Visibility.Collapsed;
                        this.MenuLine.SetValue(Grid.ColumnProperty, 0);
                        MoviesTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        TvShowsTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        PersonsTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TvShowsTab_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SearchedMoviesTvShowsDisplay.ItemsSource = Service.SearchedTvShowsResult;
                if (Service.SearchedTvShowsResult.Count == 0)
                {
                    await Service.GetSearchedTvShows(searchKey,  1,this);
                }
                searchType = SearchType.TvShow;
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.ItemCountText.Text = TotalResultTvShows + " items";
                        SearchedMoviesTvShowsDisplay.Visibility = Visibility.Visible;
                        SearchedCastsDisplay.Visibility = Visibility.Collapsed;
                        this.MenuLine.SetValue(Grid.ColumnProperty, 1);
                        MoviesTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        TvShowsTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        PersonsTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void PersonsTab_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SearchedCastsDisplay.ItemsSource = Service.SearchedCastsResult;
                if (Service.SearchedCastsResult.Count == 0)
                {
                    await Service.GetSearchedCasts(searchKey,  1, this);
                }
                searchType = SearchType.Person;

                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.ItemCountText.Text = TotalResultPersons + " items";
                        SearchedMoviesTvShowsDisplay.Visibility = Visibility.Collapsed;
                        SearchedCastsDisplay.Visibility = Visibility.Visible;
                        this.MenuLine.SetValue(Grid.ColumnProperty, 2);
                        MoviesTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        TvShowsTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        PersonsTab.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    }));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void SearchPageResults_OnUnloaded(object sender, RoutedEventArgs e)
        {
        }

        public void Dispose()
        {
            Service.SearchedMoviesResult.Clear();
            Service.SearchedTvShowsResult.Clear();
            Service.SearchedCastsResult.Clear();
        }
    }

    public enum SearchType
    {
        Movie,
        TvShow,
        Person
    }
    public class DebounceHelper
    {
        private CancellationTokenSource _debounceCancellationTokenSource;

        public async Task DebouncedAction(Func<Task> action, int delayMilliseconds)
        {
            _debounceCancellationTokenSource?.Cancel();  // Önceki görevi iptal et
            _debounceCancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Bekleyip sonra çağır
                await Task.Delay(delayMilliseconds, _debounceCancellationTokenSource.Token);
                await action(); // Gerçek eylemi gerçekleştir
            }
            catch (TaskCanceledException)
            {
                // Eğer işlem iptal edilirse burada herhangi bir şey yapmayabiliriz.
            }
        }
    }
}
