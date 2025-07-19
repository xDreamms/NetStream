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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Serilog;
using TinifyAPI;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for TvShowsPage.xaml
    /// </summary>
    public partial class TvShowsPage : Page,IDisposable
    {
        public TvShowsPage()
        {
            InitializeComponent();
            this.DataContext = this;
            OnLoad();
        }

        private async void OnLoad()
        {
            try
            {
                await GetMainTvShow();
                SetVisibility();
                Animate();
                await LoadTvShowsInChunksOptimized();
           
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        SetLastVisibleItemLowerOpacity(PopularTvShowsDisplay, PopularTvShowsDisplay);
                        SetLastVisibleItemLowerOpacity(TopRatedTvShowsDisplay, TopRatedTvShowsDisplay);
                        SetLastVisibleItemLowerOpacity(TvShowsAiringTodayDisplay, TvShowsAiringTodayDisplay);
                    }));
            }
            catch (Exception e)
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
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public async Task LoadTvShowsInChunksOptimized()
        {
            List<string> categories = new List<string>()
            {
                "popular",
                "topRated",
                "airingToday",
            };
            foreach (var category in categories)
            {
                await LoadTvShowsChunk(category, 1);
                await Task.Delay(200);
            }
        }

        private async Task LoadTvShowsChunk(string category, int page)
        {
            try
            {
                var movieList = await GetTvShowsChunk(page, category);

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
                            case "popular":
                                PopularTvShowsDisplay.ItemsSource = Service.PopularTvShows;
                                break;
                            case "topRated":
                                TopRatedTvShowsDisplay.ItemsSource = Service.TopRatedTvShows;
                                break;
                            case "airingToday":
                                TvShowsAiringTodayDisplay.ItemsSource = Service.AiringTodayTvShows;
                                break;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Hata kontrolü
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void AddMovieToCollection(string category, Movie movie)
        {
            switch (category)
            {
                case "popular":
                    if (!Service.PopularTvShows.Any(x => x.Id == movie.Id)) Service.PopularTvShows.Add(movie);
                    break;
                case "topRated":
                    if (!Service.TopRatedTvShows.Any(x => x.Id == movie.Id)) Service.TopRatedTvShows.Add(movie);
                    break;
                case "airingToday":
                    if (!Service.AiringTodayTvShows.Any(x => x.Id == movie.Id)) Service.AiringTodayTvShows.Add(movie);
                    break;
            }
        }
       
        public async Task<TMDbLib.Objects.General.SearchContainer<TMDbLib.Objects.Search.SearchTv>> GetTvShowsChunk(int page, string category)
        {
            switch (category)
            {
                case "popular":
                    return await Service.client.GetTvShowPopularAsync(page,Service.language);
                case "topRated":
                    return await Service.client.GetTvShowTopRatedAsync(page,Service.language);
                case "airingToday":
                    return await Service.client.GetTvShowListAsync(TvShowListType.AiringToday, Service.language, page);
                default:
                    return null;
            }
        }

        private async Task GetMainTvShow()
        {
            await Service.GetMainTvShowDetail();
            MainMovieTvShow.DataContext = Service.MainTvshoww;
        }

        private async void Home_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void SetVisibility()
        {
            if (Service.MainTvshoww != null)
            {
                if (String.IsNullOrWhiteSpace(Service.MainTvshoww.Overview))
                {
                    OverviewBox.Visibility = Visibility.Collapsed;
                }
            }
        }


        private void ExploreMorePopularMovies_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var textBlock = sender as TextBlock;

                if (textBlock.Name == "exploreMoreTvShowAiringToday")
                {
                    this.NavigationService.Navigate(new ExploreMorePage(Service.AiringTodayTvShows, ExploreMore.TvShow,
                        ExploreMoreList.NowPlaying, Application.Current.Resources["TvShowsAiringTodayString"].ToString()));
                }
                else if (textBlock.Name == "exploreMoreTopRatedTvShows")
                {
                    this.NavigationService.Navigate(new ExploreMorePage(Service.TopRatedTvShows, ExploreMore.TvShow,
                        ExploreMoreList.TopRated, Application.Current.Resources["TopRatedTvShowsString"].ToString()));
                }
                else if (textBlock.Name == "exploreMorePopularTvShows")
                {
                    this.NavigationService.Navigate(new ExploreMorePage(Service.PopularTvShows, ExploreMore.TvShow,
                        ExploreMoreList.Popular, Application.Current.Resources["PopularTvShowsString"].ToString()));
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
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

        private void PopularMoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)PopularTvShowsDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    this.NavigationService.Navigate(movieDetailsPage);
                    PopularTvShowsDisplay.UnselectAll();
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
            AiringTodayTvVisibleItems.Clear();
            PopularTvVisibleItems.Clear();
            TopRatedTvVisibleItems.Clear();
        }

        private void TopRatedTvShowsDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)TopRatedTvShowsDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    this.NavigationService.Navigate(movieDetailsPage);
                    TopRatedTvShowsDisplay.UnselectAll();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TvShowsAiringTodayDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)TvShowsAiringTodayDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    this.NavigationService.Navigate(movieDetailsPage);
                    TvShowsAiringTodayDisplay.UnselectAll();
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
                var main_movie = MainMovieTvShow.DataContext as MainMovie;

                if (main_movie != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(main_movie.Id, main_movie.ShowType);
                    this.NavigationService.Navigate(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void PopularTvScrollRightButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToRight(PopularTvShowsDisplay,Service.PopularTvShows);
        }

        private void PopularTvScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToLeft(PopularTvShowsDisplay,Service.PopularTvShows);
        }

        private void TopRatedTvScrollRightButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToRight(TopRatedTvShowsDisplay,Service.TopRatedTvShows);
        }

        private void TopRatedTvScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToLeft(TopRatedTvShowsDisplay, Service.TopRatedTvShows);
        }

        private void AiringTodayTvScrollRightButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToRight(TvShowsAiringTodayDisplay,Service.AiringTodayTvShows);
        }

        private void AiringTodayTvScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToLeft(TvShowsAiringTodayDisplay, Service.AiringTodayTvShows);
        }

        private void PopularTvShowsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Task.Delay(100);
            SetLastVisibleItemLowerOpacity(PopularTvShowsDisplay, PopularTvShowsDisplay);
        }

        private void TopRatedTvShowsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Task.Delay(100);
            SetLastVisibleItemLowerOpacity(TopRatedTvShowsDisplay, TopRatedTvShowsDisplay);
        }

        private void TvShowsAiringTodayDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Task.Delay(100);
            SetLastVisibleItemLowerOpacity(TvShowsAiringTodayDisplay, TvShowsAiringTodayDisplay);
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


        private static bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (element == null || !element.IsVisible)
                return false;

            Rect bounds =
                element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            var rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }

       
        List<ListBoxItem> PopularTvVisibleItems = new List<ListBoxItem>();
        List<ListBoxItem> TopRatedTvVisibleItems = new List<ListBoxItem>();
        List<ListBoxItem> AiringTodayTvVisibleItems = new List<ListBoxItem>();

        private void SetLastVisibleItemLowerOpacity(ListBox listBox, FrameworkElement parentToTestVisibility)
        {
            try
            {
                if (listBox == PopularTvShowsDisplay)
                {
                    foreach (var visibleItem in PopularTvVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }

                    PopularTvVisibleItems.Clear();
                    foreach (Movie item in PopularTvShowsDisplay.Items)
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
                            if (PopularTvVisibleItems.Count > 0 &&
                                scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                PopularTvVisibleItems.Last().Opacity = 0.5;
                            }
                        }
                    }
                }
                else if(listBox == TopRatedTvShowsDisplay)
                {
                    foreach (var visibleItem in TopRatedTvVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }

                    TopRatedTvVisibleItems.Clear();
                    foreach (Movie item in TopRatedTvShowsDisplay.Items)
                    {
                        var container = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem(item);
                        if (IsUserVisible(container, parentToTestVisibility))
                        {
                            TopRatedTvVisibleItems.Add(container);
                        }
                    }

                    Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                    if (border != null)
                    {
                        ScrollViewer scrollViewer = border.Child as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            if (TopRatedTvVisibleItems.Count > 0 &&
                                scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                TopRatedTvVisibleItems.Last().Opacity = 0.5;
                            }
                        }
                    }
                }
                else if (listBox == TvShowsAiringTodayDisplay)
                {
                    foreach (var visibleItem in AiringTodayTvVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }

                    AiringTodayTvVisibleItems.Clear();
                    foreach (Movie item in TvShowsAiringTodayDisplay.Items)
                    {
                        var container = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem(item);
                        if (IsUserVisible(container, parentToTestVisibility))
                        {
                            AiringTodayTvVisibleItems.Add(container);
                        }
                    }

                    Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                    if (border != null)
                    {
                        ScrollViewer scrollViewer = border.Child as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            if (AiringTodayTvVisibleItems.Count > 0 &&
                                scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                AiringTodayTvVisibleItems.Last().Opacity = 0.5;
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

        private void TvShowsPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetLastVisibleItemLowerOpacity(PopularTvShowsDisplay, PopularTvShowsDisplay);
            SetLastVisibleItemLowerOpacity(TopRatedTvShowsDisplay, TopRatedTvShowsDisplay);
            SetLastVisibleItemLowerOpacity(TvShowsAiringTodayDisplay, TvShowsAiringTodayDisplay);
        }

      

       
    }
}
