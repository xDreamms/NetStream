using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Serilog;
using TinifyAPI;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for MoviesPage.xaml
    /// </summary>
    public partial class MoviesPage : Page, IDisposable
    {
        public MoviesPage()
        {
            InitializeComponent();
            this.DataContext = this;
            MainMovieTvShow.DataContext = Service.MainMovieee;
            OnLoad();
        }

        private async void OnLoad()
        {
            SetVisibility();
            Animate();
            await LoadMoviesInChunksOptimized();
        }

        private void Animate()
        {
            try
            {
                DoubleAnimation opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(TimeSpan.FromSeconds(0.3))
                };

                DoubleAnimation translateAnimation = new DoubleAnimation
                {
                    From = 50,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.75)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                MainMovieStackPanel.BeginAnimation(OpacityProperty, opacityAnimation);
                MainMovieStackPanelTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimation);
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        public async Task LoadMoviesInChunksOptimized()
        {
            try
            {
                List<string> categories = new List<string>()
                {
                    "popular",
                    "topRated",
                    "upComing",
                    "nowPlaying"
                };
                foreach (var category in categories)
                {
                    await LoadMoviesChunk(category, 1);
                    await Task.Delay(200);
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
           
        }

        private async Task LoadMoviesChunk(string category, int page)
        {
            try
            {
                var movieList = await GetMoviesChunk(page, category);

                if (movieList != null && movieList.Results.Any())
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var movie in movieList.Results)
                        {
                            Movie mov = new Movie()
                            {
                                Poster = (Service.client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                                Id = movie.Id,
                                Name = movie.Title,
                                Rating = movie.VoteAverage,
                                ShowType = ShowType.Movie,
                            };
                            AddMovieToCollection(category, mov);
                        }

                        switch (category)
                        {
                            case "popular":
                                PopularMoviesDisplay.ItemsSource = Service.PopularMovies;
                                break;
                            case "topRated":
                                TopRatedMoviesDisplay.ItemsSource = Service.TopRatedMovies;
                                break;
                            case "upComing":
                                UpComingMoviesDisplay.ItemsSource = Service.UpComingMovies;
                                break;
                            case "nowPlaying":
                                NowPlayingMoviesDisplay.ItemsSource = Service.NowPlayingMovies;
                                break;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void AddMovieToCollection(string category, Movie movie)
        {
            switch (category)
            {
                case "popular":
                    if (!Service.PopularMovies.Any(x => x.Id == movie.Id)) Service.PopularMovies.Add(movie);
                    break;
                case "topRated":
                    if (!Service.TopRatedMovies.Any(x => x.Id == movie.Id)) Service.TopRatedMovies.Add(movie);
                    break;
                case "upComing":
                    if (!Service.UpComingMovies.Any(x => x.Id == movie.Id)) Service.UpComingMovies.Add(movie);
                    break;
                case "nowPlaying":
                    if (!Service.NowPlayingMovies.Any(x => x.Id == movie.Id)) Service.NowPlayingMovies.Add(movie);
                    break;
            }
        }

        public async Task<TMDbLib.Objects.General.SearchContainer<TMDbLib.Objects.Search.SearchMovie>> GetMoviesChunk(int page, string category)
        {
            switch (category)
            {
                case "popular":
                    return await Service.client.GetMoviePopularListAsync(Service.language, page);
                case "topRated":
                    return await Service.client.GetMovieTopRatedListAsync(Service.language, page);
                case "upComing":
                    return await Service.client.GetMovieUpcomingListAsync(Service.language, page);
                case "nowPlaying":
                    return await Service.client.GetMovieNowPlayingListAsync(Service.language, page);
                default:
                    return null;
            }
        }


        public FastObservableCollection<Movie> PopularMovies
        {
            get { return Service.PopularMovies; }
        }

        public FastObservableCollection<Movie> TopRatedMovies
        {
            get { return Service.TopRatedMovies; }
        }

        public FastObservableCollection<Movie> UpcomingMovies
        {
            get { return Service.UpComingMovies; }
        }

        public FastObservableCollection<Movie> NowPlayingMovies
        {
            get { return Service.NowPlayingMovies; }
        }

        private async void Home_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void SetVisibility()
        {
            if (Service.MainMovieee != null)
            {
                if (String.IsNullOrWhiteSpace(Service.MainMovieee.Overview))
                {
                    OverviewBox.Visibility = Visibility.Collapsed;
                }
            }
        }


        private void PopularMoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Movie selectedMov = (Movie)PopularMoviesDisplay.SelectedItem;
            if (selectedMov != null)
            {
                MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                this.NavigationService.Navigate(movieDetailsPage);
                PopularMoviesDisplay.UnselectAll();
            }
        }


        private void ExploreMorePopularMovies_OnMouseEnter(object sender, MouseEventArgs e)
        {
            var textblock = sender as TextBlock;
            textblock.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
        }

        private void ExploreMorePopularMovies_OnMouseLeave(object sender, MouseEventArgs e)
        {
            var textblock = sender as TextBlock;
            textblock.Foreground = new SolidColorBrush(Color.FromArgb(255, 130, 130, 130));
        }
        private void ExploreMorePopularMovies_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var textBlock = sender as TextBlock;

                if (textBlock.Name == "exploreMoreMovieNowPlaying")
                {
                    this.NavigationService.Navigate(new ExploreMorePage(NowPlayingMovies, ExploreMore.Movie,
                        ExploreMoreList.NowPlaying, Application.Current.Resources["NowPlayingMoviesString"].ToString()));
                }
                else if (textBlock.Name == "exploreMoreMovieUpcoming")
                {
                    this.NavigationService.Navigate(new ExploreMorePage(UpcomingMovies, ExploreMore.Movie,
                        ExploreMoreList.UpComing, Application.Current.Resources["UpcomingMoviesString"].ToString()));
                }
                else if (textBlock.Name == "exploreMoreTopRatedMovies")
                {
                    this.NavigationService.Navigate(new ExploreMorePage(TopRatedMovies, ExploreMore.Movie,
                        ExploreMoreList.TopRated, Application.Current.Resources["TopRatedMoviesString"].ToString()));
                }
                else if (textBlock.Name == "exploreMorePopularMovies")
                {
                    this.NavigationService.Navigate(new ExploreMorePage(PopularMovies, ExploreMore.Movie,
                        ExploreMoreList.Popular, Application.Current.Resources["PopularMoviesString"].ToString()));
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public void Dispose()
        {
            this.DataContext = null;
            this.MainMovieTvShow.DataContext = null;
            Service.PopularMovies.Clear();
            Service.TopRatedMovies.Clear();
            Service.UpComingMovies.Clear();
            Service.NowPlayingMovies.Clear();
            Service.PopularTvShows.Clear();
            Service.TopRatedTvShows.Clear();
            Service.AiringTodayTvShows.Clear();
            PopularMoviesVisibleItems.Clear();
            TopRatedMoviesVisibleItems.Clear();
            NowPlayingMoviesVisibleItems.Clear();
            UpComingMoviesVisibleItems.Clear();
        }

        private void TopRatedMoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)TopRatedMoviesDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    this.NavigationService.Navigate(movieDetailsPage);
                    TopRatedMoviesDisplay.UnselectAll();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void UpcomingMoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)UpComingMoviesDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    this.NavigationService.Navigate(movieDetailsPage);
                    UpComingMoviesDisplay.UnselectAll();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void NowPlayingMoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)NowPlayingMoviesDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    this.NavigationService.Navigate(movieDetailsPage);
                    NowPlayingMoviesDisplay.UnselectAll();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MainMovieTvShow_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MainMovie mainMovie = MainMovieTvShow.DataContext as MainMovie;
                if (mainMovie != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(mainMovie.Id, mainMovie.ShowType);
                    this.NavigationService.Navigate(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void PopularMoviesScrollRightButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToRight(PopularMoviesDisplay,Service.PopularMovies);
        }

        private void PopularMoviesScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToLeft(PopularMoviesDisplay, Service.PopularMovies);
        }

        private void TopRatedMoviesScrollRightButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToRight(TopRatedMoviesDisplay,Service.TopRatedMovies);
        }

        private void TopRatedMoviesScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToLeft(TopRatedMoviesDisplay,Service.TopRatedMovies);
        }

        private void UpComingMoviesScrollRightButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToRight(UpComingMoviesDisplay,Service.UpComingMovies);
        }

        private void UpComingMoviesScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToLeft(UpComingMoviesDisplay,Service.UpComingMovies);
        }

        private void NowPlayingMoviesScrollRightButton_OnPreviewMouseLeftButtonDown(object sender,
            MouseButtonEventArgs e)
        {
            ScrollToRight(NowPlayingMoviesDisplay,Service.NowPlayingMovies);
        }

        private void NowPlayingMoviesScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender,
            MouseButtonEventArgs e)
        {
            ScrollToLeft(NowPlayingMoviesDisplay, Service.NowPlayingMovies);
        }

        private void PopularMoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Task.Delay(100);
            SetLastVisibleItemLowerOpacity(PopularMoviesDisplay, PopularMoviesDisplay);
        }

        private void TopRatedMoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Task.Delay(100);
            SetLastVisibleItemLowerOpacity(TopRatedMoviesDisplay, TopRatedMoviesDisplay);
        }

        private void UpComingMoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Task.Delay(100);
            SetLastVisibleItemLowerOpacity(UpComingMoviesDisplay, UpComingMoviesDisplay);
        }

        private void NowPlayingMoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Task.Delay(100);
            SetLastVisibleItemLowerOpacity(NowPlayingMoviesDisplay, NowPlayingMoviesDisplay);
        }

        private void ScrollToRight(ListBox listBox,FastObservableCollection<Movie> movies)
        {
            try
            {
                Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                if (border != null)
                {
                    ScrollViewer scrollViewer = border.Child as ScrollViewer;
                    if (scrollViewer != null)
                    {
                        var currentScrollWidth = (((movies.Count - 5) - scrollViewer.ScrollableWidth) * 1) + 5.0;
                        var currentScrollPos = scrollViewer.ContentHorizontalOffset;
                        if (scrollViewer.ScrollableWidth - currentScrollPos >= currentScrollWidth)
                        {
                            currentScrollPos += currentScrollWidth;
                            scrollViewer.ScrollToHorizontalOffset(currentScrollPos);
                        }
                        else
                        {
                            currentScrollPos = scrollViewer.ScrollableWidth;
                            scrollViewer.ScrollToHorizontalOffset(currentScrollPos);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ScrollToLeft(ListBox listBox,FastObservableCollection<Movie> movies)
        {
            try
            {
                Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                if (border != null)
                {
                    ScrollViewer scrollViewer = border.Child as ScrollViewer;
                    if (scrollViewer != null)
                    {
                        var currentScrollWidth = (((movies.Count - 5) - scrollViewer.ScrollableWidth) * 1) + 5.0;
                        var currentScrollPos = scrollViewer.ContentHorizontalOffset;
                        if (currentScrollPos >= currentScrollWidth)
                        {
                            currentScrollPos -= currentScrollWidth;
                            scrollViewer.ScrollToHorizontalOffset(currentScrollPos);
                        }
                        else
                        {
                            currentScrollPos = 0;
                            scrollViewer.ScrollToHorizontalOffset(currentScrollPos);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        private static bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (element == null || !element.IsVisible)
                return false;

            Rect bounds =
                element.TransformToAncestor(container)
                    .TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            var rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }

        List<ListBoxItem> PopularMoviesVisibleItems = new List<ListBoxItem>();
        List<ListBoxItem> TopRatedMoviesVisibleItems = new List<ListBoxItem>();
        List<ListBoxItem> UpComingMoviesVisibleItems = new List<ListBoxItem>();
        List<ListBoxItem> NowPlayingMoviesVisibleItems = new List<ListBoxItem>();

        private void SetLastVisibleItemLowerOpacity(ListBox listBox, FrameworkElement parentToTestVisibility)
        {
            try
            {
                if (listBox == PopularMoviesDisplay)
                {
                    foreach (var visibleItem in PopularMoviesVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }

                    PopularMoviesVisibleItems.Clear();
                    foreach (Movie item in PopularMoviesDisplay.Items)
                    {
                        var container = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem(item);
                        if (IsUserVisible(container, parentToTestVisibility))
                        {
                            PopularMoviesVisibleItems.Add(container);
                        }
                    }

                    Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                    if (border != null)
                    {
                        ScrollViewer scrollViewer = border.Child as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            if (PopularMoviesVisibleItems.Count > 0 &&
                                scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                PopularMoviesVisibleItems.Last().Opacity = 0.5;
                            }
                        }
                    }
                }
                else if (listBox == TopRatedMoviesDisplay)
                {
                    foreach (var visibleItem in TopRatedMoviesVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }

                    TopRatedMoviesVisibleItems.Clear();
                    foreach (Movie item in TopRatedMoviesDisplay.Items)
                    {
                        var container = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem(item);
                        if (IsUserVisible(container, parentToTestVisibility))
                        {
                            TopRatedMoviesVisibleItems.Add(container);
                        }
                    }

                    Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                    if (border != null)
                    {
                        ScrollViewer scrollViewer = border.Child as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            if (TopRatedMoviesVisibleItems.Count > 0 &&
                                scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                TopRatedMoviesVisibleItems.Last().Opacity = 0.5;
                            }
                        }
                    }
                }
                else if (listBox == UpComingMoviesDisplay)
                {
                    foreach (var visibleItem in UpComingMoviesVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }

                    UpComingMoviesVisibleItems.Clear();
                    foreach (Movie item in UpComingMoviesDisplay.Items)
                    {
                        var container = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem(item);
                        if (IsUserVisible(container, parentToTestVisibility))
                        {
                            UpComingMoviesVisibleItems.Add(container);
                        }
                    }

                    Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                    if (border != null)
                    {
                        ScrollViewer scrollViewer = border.Child as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            if (UpComingMoviesVisibleItems.Count > 0 &&
                                scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                UpComingMoviesVisibleItems.Last().Opacity = 0.5;
                            }
                        }
                    }
                }
                else if (listBox == NowPlayingMoviesDisplay)
                {
                    foreach (var visibleItem in NowPlayingMoviesVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }

                    NowPlayingMoviesVisibleItems.Clear();
                    foreach (Movie item in NowPlayingMoviesDisplay.Items)
                    {
                        var container = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem(item);
                        if (IsUserVisible(container, parentToTestVisibility))
                        {
                            NowPlayingMoviesVisibleItems.Add(container);
                        }
                    }

                    Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                    if (border != null)
                    {
                        ScrollViewer scrollViewer = border.Child as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            if (NowPlayingMoviesVisibleItems.Count > 0 &&
                                scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                NowPlayingMoviesVisibleItems.Last().Opacity = 0.5;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MoviesPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetLastVisibleItemLowerOpacity(PopularMoviesDisplay, PopularMoviesDisplay);
            SetLastVisibleItemLowerOpacity(TopRatedMoviesDisplay, TopRatedMoviesDisplay);
            SetLastVisibleItemLowerOpacity(UpComingMoviesDisplay, UpComingMoviesDisplay);
            SetLastVisibleItemLowerOpacity(NowPlayingMoviesDisplay, NowPlayingMoviesDisplay);
        }

        private void WatchTrailerButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void WatchButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
        }
    }
}
