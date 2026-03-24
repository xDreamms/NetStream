using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetStream.Controls;
using Serilog;
using TMDbLib.Client;
using TMDbLib.Objects.Discover;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for TvShowsPage.xaml
    /// </summary>
    public partial class TvShowsPage : UserControl, IDisposable
    {
        public static TvShowsPage Instance;
        public TvShowsPage()
        {
            InitializeComponent();
            var mainMovieTvShow = this.FindControl<MainMovieTvShow>("MainMovieTvShow");
            if (mainMovieTvShow != null)
            {
                var durationElipse = mainMovieTvShow.FindControl<Ellipse>("DurationElipse");
                var durationViewBox = mainMovieTvShow.FindControl<Viewbox>("DurationViewBox");

                if (durationElipse != null) durationElipse.IsVisible = false;
                if (durationViewBox != null) durationViewBox.IsVisible = false;
            }
            Instance = this;
            this.DataContext = this;
            OnLoad();
        }

        private async void OnLoad()
        {
            try
            {
                await GetMainTvShow();
                await LoadTvShowsInChunksOptimized();
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
            }
            
            AiringTodayTvControl.Margin = new Thickness(0,0,0,MainScroll.Extent.Height * 0.03);
        }

        private async Task LoadTvShowsChunk(string category, int page)
        {
            try
            {
                var movieList = await GetTvShowsChunk(page, category);

                if (movieList != null && movieList.Results.Any())
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (category != "popular" && category != "airingToday")
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
                        }
                      
                        switch (category)
                        {
                            case "popular":
                                PopularTvControl.ItemsSource = Service.PopularTvShows;
                                break;
                            case "topRated":
                                TopRatedTvControl.ItemsSource = Service.TopRatedTvShows;
                                break;
                            case "airingToday":
                                AiringTodayTvControl.ItemsSource = Service.AiringTodayTvShows;
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
                    await GetPopularTvShows(page, Service.language);
                    return null;
                case "topRated":
                    return await Service.client.GetTvShowTopRatedAsync(page, Service.language);
                case "airingToday":
                    await GetAiringTodayTvShows(1,Service.language);
                    return null;
                default:
                    return null;
            }
        }
        
        public async Task GetAiringTodayTvShows(int page,string language)
        {
            try
            {
                Service.AiringTodayTvShows.Clear();
                if (!Service.client.HasConfig)
                {
                    await Service.client.GetConfigAsync();
                }
                
                var today = DateTime.UtcNow;

                var discovered = await Service.client.DiscoverTvShowsAsync()
                    .WhereAirDateIsAfter(today)
                    .WhereAirDateIsBefore(today)
                    .WhereGenresExclude(new List<int>{10767,10766,10768,10764,10763,10762,99})
                    .OrderBy(DiscoverTvShowSortBy.PopularityDesc)
                    .Query(language, page);
                
                
                foreach (var discoveredTvShow in discovered.Results)
                {
                    var tv = new Movie()
                    {
                        Id = discoveredTvShow.Id,
                        Name = discoveredTvShow.Name,
                        Poster = Service.client.GetImageUrl("w500", discoveredTvShow.PosterPath).AbsoluteUri,
                        Rating = discoveredTvShow.VoteAverage,
                        ShowType = ShowType.TvShow
                    };
                    
                    if ( Service.AiringTodayTvShows.Any(x => x.Id == tv.Id))
                    {
                    }
                    else
                    {
                        Service.AiringTodayTvShows.Add(tv);
                    }
                    
                }

            }
            catch (Exception e)
            {
                Log.Error("Error on discover tv shows: " + e.Message);
            }
        }
        
        
        public async Task GetPopularTvShows(int page,string language)
        {
            try
            {
                Service.PopularTvShows.Clear();
                if (!Service.client.HasConfig)
                {
                    await Service.client.GetConfigAsync();
                }
                
                var discoveredTvShows = await Service.client.DiscoverTvShowsAsync()
                    .WhereGenresExclude(new List<int>{10767,10766,10768,10764,10763,10762,99})
                  //  .WithOriginCountry("US")
                    .OrderBy(DiscoverTvShowSortBy.PopularityDesc).Query(language, page);
                
                
                foreach (var discoveredTvShow in discoveredTvShows.Results)
                {
                    var tv = new Movie()
                    {
                        Id = discoveredTvShow.Id,
                        Name = discoveredTvShow.Name,
                        Poster = Service.client.GetImageUrl("w500", discoveredTvShow.PosterPath).AbsoluteUri,
                        Rating = discoveredTvShow.VoteAverage,
                        ShowType = ShowType.TvShow
                    };
                    
                    if ( Service.PopularTvShows.Any(x => x.Id == tv.Id))
                    {
                    }
                    else
                    {
                        Service.PopularTvShows.Add(tv);
                    }
                    
                }

            }
            catch (Exception e)
            {
                Log.Error("Error on discover tv shows: " + e.Message);
            }
        }

        private async Task GetMainTvShow()
        {
            await Service.GetMainTvShowDetail();
            MainMovieTvShow.DataContext = Service.MainTvshoww;
        }

        private void Home_OnLoaded(object sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
            previousWidth = MainView.Instance.screenWidth;
            isSmallScreen = previousWidth <= SMALL_SCREEN_THRESHOLD;
            isExtraSmallScreen = previousWidth <= EXTRA_SMALL_SCREEN_THRESHOLD;
            
            ApplyResponsiveLayout(previousWidth);
            
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            try
            {
                // Get current width
                double width = e.width;
                
                if (width != previousWidth)
                {
                    previousWidth = width;
                    
                    // Determine screen size category for responsive design
                    isSmallScreen = width <= SMALL_SCREEN_THRESHOLD;
                    isExtraSmallScreen = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                    
                    // Apply responsive layout based on screen size
                    ApplyResponsiveLayout(width);
                    
                   
                    
                    // Scroll butonlarının konumunu ayarla
                    
                    // Dinamik film öğelerinin stillerini ayarla
                    UpdateLayoutBasedOnSize(width);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error in Home_OnSizeChanged: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
           
        }

      
        private void ExploreMorePopularMovies_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                var textBlock = sender as TextBlock;

                if (textBlock.Name == "exploreMoreTvShowAiringToday")
                {
                    var exploreMorePage = new ExploreMorePage(Service.AiringTodayTvShows, ExploreMore.TvShow,
                        ExploreMoreList.NowPlaying, ResourceProvider.GetString("TvShowsAiringTodayString").ToString());
                    NavigateToPage(exploreMorePage);
                }
                else if (textBlock.Name == "exploreMoreTopRatedTvShows")
                {
                    var exploreMorePage = new ExploreMorePage(Service.TopRatedTvShows, ExploreMore.TvShow,
                        ExploreMoreList.TopRated, ResourceProvider.GetString("TopRatedTvShowsString").ToString());
                    NavigateToPage(exploreMorePage);
                }
                else if (textBlock.Name == "exploreMorePopularTvShows")
                {
                    var exploreMorePage = new ExploreMorePage(Service.PopularTvShows, ExploreMore.TvShow,
                        ExploreMoreList.Popular, ResourceProvider.GetString("PopularTvShowsString").ToString());
                    NavigateToPage(exploreMorePage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        // Helper method for navigation in Avalonia
        private void NavigateToPage(UserControl page)
        {
            var mainView = this.FindAncestorOfType<MainView>();
            if (mainView != null)
            {
                mainView.SetContent(page);
            }
        }

       
        
        public void Dispose()
        {
            try
            {
                foreach (var visualDescendant in PopularTvControl.ItemsDisplay.GetVisualDescendants())
                {
                    if (visualDescendant is AsyncImageControl asyncImageControl)
                    {
                        asyncImageControl.Dispose();
                    }
                }
                foreach (var visualDescendant in TopRatedTvControl.ItemsDisplay.GetVisualDescendants())
                {
                    if (visualDescendant is AsyncImageControl asyncImageControl)
                    {
                        asyncImageControl.Dispose();
                    }
                }
                foreach (var visualDescendant in AiringTodayTvControl.ItemsDisplay.GetVisualDescendants())
                {
                    if (visualDescendant is AsyncImageControl asyncImageControl)
                    {
                        asyncImageControl.Dispose();
                    }
                }
             
            
                this.DataContext = null;
                this.MainMovieTvShow.DataContext = null;
            
                if (PopularTvControl != null) PopularTvControl.ItemsSource = null;
                if (TopRatedTvControl != null) TopRatedTvControl.ItemsSource = null;
                if (AiringTodayTvControl != null) AiringTodayTvControl.ItemsSource = null;
            
                MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
                
                // Bellek temizliği
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                Log.Error($"Dispose işlemi sırasında hata oluştu: {ex.Message}");
            }
        }
        

        private void MainMovieTvShow_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                var main_movie = MainMovieTvShow.DataContext as MainMovie;

                if (main_movie != null)
                {
                    var movieDetailsPage = new MovieDetailsPage(main_movie.Id, main_movie.ShowType);
                    NavigateToPage(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        

        private double previousWidth = 0;
        private bool isSmallScreen = false;
        private bool isExtraSmallScreen = false;

        // Threshold for small screen detection
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
        
        
        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                
                
                // Use our new comprehensive layout update method
                UpdateLayoutBasedOnSize(width);
                
                // Post-update adjustments specific to screen size
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyResponsiveLayout Exception: {ex.Message}");
            }
        }
        
        private void UpdateLayoutBasedOnSize(double width)
        {
            try
            {
                // Ekran genişliğini maksimum 3840px ile sınırla
                double clampedWidth = Math.Min(width, 3840);
                
                bool isSmallScreen = clampedWidth <= SMALL_SCREEN_THRESHOLD;
                bool isExtraSmallScreen = clampedWidth <= EXTRA_SMALL_SCREEN_THRESHOLD;
                
                // Record screen state changes
                bool screenSizeChanged = this.isSmallScreen != isSmallScreen;
                
                this.isSmallScreen = isSmallScreen;
                this.isExtraSmallScreen = isExtraSmallScreen;
                
                // Ana başlık boyutları için ölçeklendirme değerleri
                double headerFontSize = CalculateTextSize(clampedWidth, 18, 36);
                
                // WrapPanel'daki öğelerin boyutları için ölçeklendirme
                double viewboxMaxHeight = CalculateScaledValue(clampedWidth, 16, 40);
                double ellipseSize = viewboxMaxHeight * 0.2;
                
                // Configure main movie poster visibility and size
               
                
                // Başlıkları boyutlandır ve marjinleri ayarla
                var headerMargin = CalculateScaledValue(clampedWidth, 20, 100);
                
                
                double commonLeftMargin = isSmallScreen ? 20 : CalculateScaledValue(clampedWidth, 40, 80); 
                
              
                if (PopularTvShowsTitle != null)
                {
                    PopularTvShowsTitle.FontSize = headerFontSize;
                    
                    if (PopularTvShowsHeaderGrid != null)
                    {
                        PopularTvShowsHeaderGrid.Margin =  new Thickness(commonLeftMargin, 20, 20, 0);
                    }
                }
                
                if (TopRatedTvShowsTitle != null)
                {
                    TopRatedTvShowsTitle.FontSize = headerFontSize;
                    
                    if (TopRatedTvShowsHeaderGrid != null)
                    {
                        TopRatedTvShowsHeaderGrid.Margin =  new Thickness(commonLeftMargin, 20, 20, 0);
                    }
                }
                
                if (AiringTodayTvShowsTitle != null)
                {
                    AiringTodayTvShowsTitle.FontSize = headerFontSize;
                    
                    if (AiringTodayTvShowsHeaderGrid != null)
                    {
                        AiringTodayTvShowsHeaderGrid.Margin =  new Thickness(commonLeftMargin, 20, 20, 0);
                    }
                }
                
                // Configure main movie info
                if (MainMovieTvShow != null)
                {
                    if (screenSizeChanged || Math.Abs(clampedWidth - previousWidth) > 50)
                    {
                        double mainMovieMargin = CalculateScaledValue(clampedWidth, 10, 100);
                        
                        if (isSmallScreen)
                        {
                            MainMovieTvShow.Margin = new Thickness(10);
                        }
                        else
                        {
                            MainMovieTvShow.Margin = new Thickness(mainMovieMargin, 20, mainMovieMargin, 20);
                        }
                    }
                }
                
              
                
                // WrapPanel'daki elemanların boyutlarını ayarla
              
                // RatingBar yıldız boyutunu güncelle
                var ratingControl = this.FindControl<RatingBar>("RatingControl");
                if (ratingControl != null)
                {
                    double starSize = CalculateScaledValue(clampedWidth, 12, 30);
                    ratingControl.StarSize = starSize;
                }
                
                // Scroll butonlarını ayarla
                
                // ListBox'ların konumlarını düzenle
                
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateLayoutBasedOnSize Exception: {ex.Message}");
            }
        }
        
       
        
        private double CalculateFontSizeForTitle(double width)
        {
            // Doğrusal ölçeklendirme formülü: y = mx + b
            // Ekran boyutu arttıkça font boyutu da artacak şekilde
            const double minWidth = 320;   // En küçük ekran genişliği (piksel)
            const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
            const double minFontSize = 18; // En küçük font boyutu
            const double maxFontSize = 90; // En büyük font boyutu
            
            // Ekran genişliğini sınırla
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Doğrusal ölçeklendirme formülü
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double fontSize = minFontSize + scale * (maxFontSize - minFontSize);
            
            // Font boyutunu yuvarla
            return Math.Round(fontSize);
        }
        private double CalculateScaledValue(double width, double minValue, double maxValue)
        {
            // Ekran genişliği sınırları
            const double minWidth = 320;   // En küçük ekran genişliği (piksel)
            const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
            
            // Ekran genişliğini sınırla
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Doğrusal ölçeklendirme formülü
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);
            
            // Değeri yuvarla
            return Math.Round(scaledValue);
        }

        private double cardWidth;
        private double CalculateCardWidth(double width)
        {
            cardWidth =  CalculateScaledValue(width, 120, 400);
            return cardWidth;
        }
        
        // Kart yüksekliği için ölçekleme
        private double CalculateCardHeight(double width)
        {
            return CalculateScaledValue(width, 210, 600);
        }
        
        // Metin boyutu için ölçekleme
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }

        private void ExploreMorePopularTvHome_OnMouseEnter(object? sender, PointerEventArgs e)
        {
            var textblock = sender as TextBlock;
            textblock.Foreground = new SolidColorBrush(Color.Parse(Application.Current.Resources["ColorDefault"].ToString()));
        }

        private void ExploreMorePopularTvHome_OnMouseLeave(object? sender, PointerEventArgs e)
        {
            var textblock = sender as TextBlock;
            textblock.Foreground = new SolidColorBrush(Color.Parse("#828282"));
        }

       

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
    }
} 