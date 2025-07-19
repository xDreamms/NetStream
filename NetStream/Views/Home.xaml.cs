using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
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
using TinifyAPI;
using Windows.Media.Protection.PlayReady;
using Serilog;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : Page,IDisposable
    {
        public Home()
        {
            InitializeComponent();
            this.DataContext = this;
            OnLoad();
        }
     
        private void MoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)MoviesDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    this.NavigationService.Navigate(movieDetailsPage);
                    MoviesDisplay.UnselectAll();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                Task.Delay(100);
                SetLastVisibleItemLowerOpacity(MoviesDisplay, this);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task GetMainMovieDetails()
        {
            try
            {
                await Service.GetMainMovieDetail();
                MainMovieTvShow.DataContext = Service.MainMovieee;
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void OnLoad()
        {
            try
            {
                await GetMainMovieDetails();
                SetVisibility();
                Animate();
                await LoadMoviesInChunksOptimized();
            
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        SetLastVisibleItemLowerOpacity(MoviesDisplay, MoviesDisplay);
                        SetLastVisibleItemLowerOpacity(TvShowDisplay, TvShowDisplay);
                    }));
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void Animate()
        {
            // Opacity animasyonu (saydamlık)
            try
            {
                DoubleAnimation opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(TimeSpan.FromSeconds(0.3)) // 0.3 saniye
                };

                // Translate animasyonu (yukarıdan kayma)
                DoubleAnimation translateAnimation = new DoubleAnimation
                {
                    From = 50,  // Başlangıç Y değeri
                    To = 0,     // Son Y değeri
                    Duration = new Duration(TimeSpan.FromSeconds(0.75)), // 0.75 saniye
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } // EaseInOut
                };


                // Animasyonları başlat
                MainMovieStackPanel.BeginAnimation(OpacityProperty, opacityAnimation);
                MainMovieStackPanelTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimation);
            }
            catch (System.Exception e)
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
                    "popularMovie",
                    "popularTv"
                };
                foreach (var category in categories)
                {
                    await LoadShowsChunk(category, 1);
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task LoadShowsChunk(string category, int page)
        {
            try
            {
                if (category == "popularMovie")
                {
                    var movieList = await GetMoviesChunk(page, category) as SearchContainer<SearchMovie>;

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
                                case "popularMovie":
                                    MoviesDisplay.ItemsSource = Service.PopularMovies;
                                    break;
                                case "popularTv":
                                    TvShowDisplay.ItemsSource = Service.PopularTvShows;
                                    break;
                            }
                        });
                    }
                }
                else
                {
                    var movieList = await GetMoviesChunk(page, category) as SearchContainer<SearchTv>;

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
                                    Name = movie.Name,
                                    Rating = movie.VoteAverage,
                                    ShowType = ShowType.TvShow,
                                };
                                AddMovieToCollection(category, mov);
                            }

                            switch (category)
                            {
                                case "popularMovie":
                                    MoviesDisplay.ItemsSource = Service.PopularMovies;
                                    break;
                                case "popularTv":
                                    TvShowDisplay.ItemsSource = Service.PopularTvShows;
                                    break;
                            }
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void AddMovieToCollection(string category, Movie movie)
        {
            switch (category)
            {
                case "popularMovie":
                    if (!Service.PopularMovies.Any(x => x.Id == movie.Id)) Service.PopularMovies.Add(movie);
                    break;
                case "popularTv":
                    if (!Service.PopularTvShows.Any(x => x.Id == movie.Id)) Service.PopularTvShows.Add(movie);
                    break;
            }
        }

        public async Task<object> GetMoviesChunk(int page, string category)
        {
            switch (category)
            {
                case "popularMovie":
                    return await Service.client.GetMoviePopularListAsync(Service.language, page);
                case "popularTv":
                    return await Service.client.GetTvShowPopularAsync(page,Service.language);
                default:
                    return null;
            }
        }

        private void SetVisibility()
        {
            try
            {
                if (Service.MainMovieee != null)
                {
                    if (String.IsNullOrWhiteSpace(Service.MainMovieee.Overview))
                    {
                        OverviewBox.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private async void Home_OnLoaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void ExploreMoreMoviesHome_OnMouseEnter(object sender, MouseEventArgs e)
        {
            exploreMoreMoviesHome.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
        }

        private void ExploreMoreMoviesHome_OnMouseLeave(object sender, MouseEventArgs e)
        {
            exploreMoreMoviesHome.Foreground = new SolidColorBrush(Color.FromArgb(255, 130, 130, 130));
        }

        private void ExploreMoreTvShowsHome_OnMouseEnter(object sender, MouseEventArgs e)
        {
            exploreMoreTvShowsHome.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
        }

        private void ExploreMoreTvShowsHome_OnMouseLeave(object sender, MouseEventArgs e)
        {
            exploreMoreTvShowsHome.Foreground = new SolidColorBrush(Color.FromArgb(255, 130, 130, 130));
        }


        private void ExploreMoreTvShowsHome_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (this.NavigationService != null)
                {
                    var exploreMorePage = new ExploreMorePage(Service.PopularTvShows, ExploreMore.TvShow,
                        ExploreMoreList.Popular, Application.Current.Resources["PopularTvShowsString"].ToString());
                    this.NavigationService.Navigate(exploreMorePage);
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        
        private void ExploreMoreMoviesHome_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var exploreMorePage = new ExploreMorePage(Service.PopularMovies, ExploreMore.Movie,
                    ExploreMoreList.Popular, Application.Current.Resources["PopularMoviesString"].ToString());
                this.NavigationService.Navigate(exploreMorePage);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
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

        private void ScrollToLeft(ListBox listBox, FastObservableCollection<Movie> movies)
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


        public void Dispose()
        {
            this.DataContext = null;
            this.MainMovieTvShow.DataContext = null;
            PopularMoviesVisibleItems.Clear();
            PopularTvVisibleItems.Clear();
            Service.PopularMovies.Clear();
            Service.TopRatedMovies.Clear();
            Service.UpComingMovies.Clear();
            Service.NowPlayingMovies.Clear();
            Service.PopularTvShows.Clear();
            Service.TopRatedTvShows.Clear();
            Service.AiringTodayTvShows.Clear();
        }

        private void TvShowDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Task.Delay(100);
            SetLastVisibleItemLowerOpacity(TvShowDisplay, this);
        }

        private void TvShowDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)TvShowDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    this.NavigationService.Navigate(movieDetailsPage);
                    TvShowDisplay.UnselectAll();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public static DependencyObject GetScrollViewer(DependencyObject o)
        {
            // Return the DependencyObject if it is a ScrollViewer
            try
            {
                if (o is ScrollViewer)
                { return o; }
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
                {
                    var child = VisualTreeHelper.GetChild(o, i);
                    var result = GetScrollViewer(child);
                    if (result == null)
                    {
                        continue;
                    }
                    else
                    {
                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
            return null;
        }


        private void MainMovieTvShow_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var main_movie =MainMovieTvShow.DataContext as MainMovie;

                if (main_movie != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(main_movie.Id,main_movie.ShowType);
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
            ScrollToRight(MoviesDisplay,Service.PopularMovies);
        }

        private void PopularMoviesScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToLeft(MoviesDisplay, Service.PopularMovies);
        }

        private static bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (element == null || !element.IsVisible)
                return false;

            Rect bounds =
                element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            var rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }

        List<ListBoxItem> PopularMoviesVisibleItems = new List<ListBoxItem>();
        List<ListBoxItem> PopularTvVisibleItems = new List<ListBoxItem>();
        private void SetLastVisibleItemLowerOpacity(ListBox listBox, FrameworkElement parentToTestVisibility)
        {
            try
            {
                if (listBox == MoviesDisplay)
                {
                    foreach (var visibleItem in PopularMoviesVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }
                    PopularMoviesVisibleItems.Clear();
                    foreach (Movie item in MoviesDisplay.Items)
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
                            if (PopularMoviesVisibleItems.Count > 0 && scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                PopularMoviesVisibleItems.Last().Opacity = 0.5;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var visibleItem in PopularTvVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }
                    PopularTvVisibleItems.Clear();
                    foreach (Movie item in TvShowDisplay.Items)
                    {
                        var container = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem(item);
                        if (IsUserVisible(container, parentToTestVisibility))
                        {
                            PopularTvVisibleItems.Add(container);
                        }
                    }

                    Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                    if (border != null)
                    {
                        ScrollViewer scrollViewer = border.Child as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            if (PopularTvVisibleItems.Count > 0 && scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                PopularTvVisibleItems.Last().Opacity = 0.5;
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

        private void PopularTvScrollRightButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToRight(TvShowDisplay,Service.PopularTvShows);
        }

        private void PopularTvScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToLeft(TvShowDisplay, Service.PopularTvShows);
        }


        private void Home_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetLastVisibleItemLowerOpacity(MoviesDisplay,MoviesDisplay);
            SetLastVisibleItemLowerOpacity(TvShowDisplay, TvShowDisplay);
        }

    }

   
}
