using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using Serilog;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using Material.Icons.Avalonia;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Avalonia.Animation;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using NetStream.Controls;
using NetStream.Navigation;
using Projektanker.Icons.Avalonia;
using TMDbLib.Objects.Discover;

namespace NetStream.Views
{
    
    public partial class Home : UserControl, IDisposable
    {
        private bool isSmallScreen = false;
        private bool isExtraSmallScreen = false;

        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;

        public static Home Instance;

        public Home()
        {
            InitializeComponent();
            Instance = this;
            this.DataContext = this;
            OnLoad();
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
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
              
                
                Service.PopularMovies.Clear();
                Service.PopularTvShows.Clear();
                
                await LoadMoviesInChunksOptimized();
            
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MoviesControl.ItemsSource = Service.PopularMovies;
                    TvShowsControl.ItemsSource = Service.PopularTvShows;
                });
                
                ResponsiveLayout(MainView.Instance.screenWidth);
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

                TvShowsControl.Margin = new Thickness(0, 0, 0, MainScroll.Extent.Height * 0.030);
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
                // Filmler için veri yükleme
                if (category == "popularMovie")
                {
                    var movieList = await GetMoviesChunk(page, category) as SearchContainer<SearchMovie>;

                    if (movieList != null && movieList.Results.Any())
                    {
                        // UI thread'de çalıştığımızdan emin olalım
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            // Önce mevcut koleksiyonu temizleyelim
                            Service.PopularMovies.Clear();
                            
                            // Sonra yeni filmleri ekleyelim
                            foreach (var movie in movieList.Results)
                            {
                                string posterUrl = movie.PosterPath != null 
                                    ? Service.client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri 
                                    : null;
                                
                                if (string.IsNullOrEmpty(posterUrl))
                                {
                                    continue; // Poster yoksa eklemiyoruz
                                }
                                
                                Movie mov = new Movie()
                                {
                                    Poster = posterUrl,
                                    Id = movie.Id,
                                    Name = movie.Title,
                                    Rating = movie.VoteAverage,
                                    ShowType = ShowType.Movie,
                                    //RatingNumber = "(" + movie.VoteCount + ")"
                                };
                                
                                Service.PopularMovies.Add(mov);
                            }
                            
                        });
                    }
                    else
                    {
                        Log.Warning($"Filmler yüklenemedi veya boş döndü! movieList null mu: {movieList == null}");
                    }
                }
                // TV Dizileri için veri yükleme
                else if (category == "popularTv")
                {
                     await GetMoviesChunk(page, category);
                }
            }
            catch (System.Exception ex)
            {
                var errorMessage = $"Error loading {category}: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        

        public async Task<object> GetMoviesChunk(int page, string category)
        {
            switch (category)
            {
                case "popularMovie":
                    return await Service.client.GetMoviePopularListAsync(Service.language, page);
                case "popularTv":
                    await GetPopularTvShows(page,Service.language);
                    return null;
                default:
                    return null;
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
                
                /*
                 *
                 * 
Belgesel 99
Çocuklar 10762
Haber 10763
Gerçeklik 10764
Pembe Dizi 10766
Talk 10767
Sava� & Politik 10768
                 */
                
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

        
        private void Home_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ResponsiveLayout(e.width);
        }

        void ResponsiveLayout(double width)
        {
            try
            {
                isSmallScreen = width <= SMALL_SCREEN_THRESHOLD;
                isExtraSmallScreen = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                
                ApplyResponsiveLayout(width);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error in Home_OnSizeChanged: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                UpdateLayoutBasedOnSize(width);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyResponsiveLayout Exception: {ex.Message}");
            }
        }

        private void ExploreMoreMoviesHome_OnMouseEnter(object sender, PointerEventArgs e)
        {
            exploreMoreMoviesHome.Foreground = new SolidColorBrush(App.Current.Resources["ColorDefault"] as Color? ?? Colors.Blue);
        }

        private void ExploreMoreMoviesHome_OnMouseLeave(object sender, PointerEventArgs e)
        {
            exploreMoreMoviesHome.Foreground = new SolidColorBrush(Color.Parse("#828282"));
        }

        private void ExploreMoreTvShowsHome_OnMouseEnter(object sender, PointerEventArgs e)
        {
            exploreMoreTvShowsHome.Foreground = new SolidColorBrush(App.Current.Resources["ColorDefault"] as Color? ?? Colors.Blue);
        }

        private void ExploreMoreTvShowsHome_OnMouseLeave(object sender, PointerEventArgs e)
        {
            exploreMoreTvShowsHome.Foreground = new SolidColorBrush(Color.Parse("#828282"));
        }

        private void ExploreMoreTvShowsHome_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                // Avalonia'da navigation farklı şekilde yapılır
                var exploreMorePage = new ExploreMorePage(Service.PopularTvShows, ExploreMore.TvShow,
                    ExploreMoreList.Popular, ResourceProvider.GetString("PopularTvShowsString"));
                
                var mainView = this.FindAncestorOfType<MainView>();
                if (mainView != null)
                {
                    mainView.SetContent(exploreMorePage);
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ExploreMoreMoviesHome_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                var exploreMorePage = new ExploreMorePage(Service.PopularMovies, ExploreMore.Movie,
                    ExploreMoreList.Popular, ResourceProvider.GetString("PopularMoviesString"));
                
                var mainView = this.FindAncestorOfType<MainView>();
                if (mainView != null)
                {
                    mainView.SetContent(exploreMorePage);
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
            foreach (var visualDescendant in MoviesControl.ItemsDisplay.GetVisualDescendants())
            {
                if (visualDescendant is AsyncImageControl asyncImageControl)
                {
                    asyncImageControl.Dispose();
                }
            }
            foreach (var visualDescendant in TvShowsControl.ItemsDisplay.GetVisualDescendants())
            {
                if (visualDescendant is AsyncImageControl asyncImageControl)
                {
                    asyncImageControl.Dispose();
                }
            }
         
            
            this.DataContext = null;
            this.MainMovieTvShow.DataContext = null;
            
            MoviesControl.ItemsSource = null;
            TvShowsControl.ItemsSource = null;
            
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
        
        private void UpdateLayoutBasedOnSize(double width)
        {
            try
            {
                double clampedWidth = Math.Min(width, 3840);
                double commonLeftMargin = isSmallScreen ? 20 : CalculateScaledValue(clampedWidth, 40, 80); 
                double headerFontSize = CalculateTextSize(clampedWidth, 14, 28);
                
                if (PopularMoviesTitle != null)
                {
                    PopularMoviesTitle.FontSize = headerFontSize;
                    
                    if (PopularMoviesHeaderGrid != null)
                    {
                        PopularMoviesHeaderGrid.Margin = new Thickness(commonLeftMargin, 20, 20, 0);
                    }
                }
                
                if (PopularTvShowsTitle != null)
                {
                    PopularTvShowsTitle.FontSize = headerFontSize;
                    
                    if (PopularTvShowsHeaderGrid != null)
                    {
                        PopularTvShowsHeaderGrid.Margin = new Thickness(commonLeftMargin, 20, 20, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateLayoutBasedOnSize Exception: {ex.Message}");
            }
        }

        // Yardımcı metotlar
        private double CalculateScaledValue(double width, double minValue, double maxValue)
        {
            const double minWidth = 320;
            const double maxWidth = 3840;
            
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);
            
            return Math.Round(scaledValue);
        }

        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }

        private void StackPanelMain_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Gönderen kontrol bir StackPanel veya olay argümanlarından DataContext'i alabiliriz
            var movie = e.Source is StackPanel stackPanel ? 
                stackPanel.DataContext as Movie : 
                sender as Movie;
                
            if (movie != null)
            {
                var movieDetailsPage = new MovieDetailsPage(movie);
                NavigationService.Instance.Navigate(movieDetailsPage);
            }
        }

        private void TvStackPanelMain_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Gönderen kontrol bir StackPanel veya olay argümanlarından DataContext'i alabiliriz
            var movie = e.Source is StackPanel stackPanel ? 
                stackPanel.DataContext as Movie : 
                sender as Movie;
                
            if (movie != null)
            {
                var movieDetailsPage = new MovieDetailsPage(movie);
                NavigationService.Instance.Navigate(movieDetailsPage);
            }
        }

        private void MainScroll_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
        }
    }
} 