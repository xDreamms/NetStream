using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Serilog;
using TMDbLib.Client;
using TMDbLib.Objects.Discover;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;

namespace NetStream.Views
{
    public partial class ExploreMorePage : UserControl, IDisposable
    {
        public static ExploreMorePage Instance;
        private int page = 2;
        private ExploreMore exploreMore;
        private ExploreMoreList exploreMoreList;
        private ObservableCollection<Movie> movies = new ObservableCollection<Movie>();
        private string language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;

        // Properties
        public ObservableCollection<Movie> Movies => movies;

        public ExploreMorePage(ObservableCollection<Movie> movies, ExploreMore exploreMore, ExploreMoreList exploreMoreList, string header)
        {
            try
            {
                InitializeComponent();
                Instance = this;
                this.exploreMore = exploreMore;
                this.exploreMoreList = exploreMoreList;
                this.TextBlockCollectionName.Text = header;
                
                // Mevcut filmleri saklayalım
                foreach (var movie in movies)
                {
                    this.movies.Add(movie);
                }

                MainView.Instance.SizeChanged += InstanceOnSizeChanged;
                VerticalMoviesControl.ItemsSource = this.movies;
                VerticalMoviesControl.ScrollReachedBottom += VerticalMoviesControl_ScrollReachedBottom;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public ExploreMorePage()
        {
            InitializeComponent();
        }

        private void NavigateToPage(UserControl page)
        {
            var mainView = this.FindAncestorOfType<MainView>();
            if (mainView != null)
            {
                mainView.SetContent(page);
            }
        }

        private async Task GetMorePageForPopularMovies()
        {
            try
            {
                var popularList = await Service.client.GetMoviePopularListAsync(language, page);
           
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var popularMovie in popularList.Results)
                    {
                        if (!String.IsNullOrWhiteSpace(popularMovie.PosterPath))
                        {
                            Movie mov = new Movie()
                            {
                                Poster = Service.client.GetImageUrl("w500", popularMovie.PosterPath).AbsoluteUri,
                                Name = popularMovie.Title,
                                Id = popularMovie.Id,
                                Rating = popularMovie.VoteAverage,
                                ShowType = ShowType.Movie
                            };
                        
                            if (movies.Any(x => x.Id == popularMovie.Id))
                            {
                                // Skip if already exists
                            }
                            else
                            {
                                movies.Add(mov);
                            }
                        }
                    }
                });
                
                page++;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task GetMorePageForTopRatedMovies()
        {
            try
            {
                var topRatedList = await Service.client.GetMovieTopRatedListAsync(language, page);
           
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var topRatedMovie in topRatedList.Results)
                    {
                        if (!String.IsNullOrWhiteSpace(topRatedMovie.PosterPath))
                        {
                            Movie mov = new Movie()
                            {
                                Poster = Service.client.GetImageUrl("w500", topRatedMovie.PosterPath).AbsoluteUri,
                                Name = topRatedMovie.Title,
                                Id = topRatedMovie.Id,
                                Rating = topRatedMovie.VoteAverage,
                                ShowType = ShowType.Movie
                            };
                            if (movies.Any(x => x.Id == topRatedMovie.Id))
                            {
                                // Skip if already exists
                            }
                            else
                            {
                                movies.Add(mov);
                            }
                        }
                    }
                });

                page++;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task GetMorePageForUpComingMovies()
        {
            try
            {
                var upcomingList = await Service.client.GetMovieUpcomingListAsync(language, page);
           
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var upcomingMovie in upcomingList.Results)
                    {
                        if (!String.IsNullOrWhiteSpace(upcomingMovie.PosterPath))
                        {
                            Movie mov = new Movie()
                            {
                                Poster = Service.client.GetImageUrl("w500", upcomingMovie.PosterPath).AbsoluteUri,
                                Name = upcomingMovie.Title,
                                Id = upcomingMovie.Id,
                                Rating = upcomingMovie.VoteAverage,
                                ShowType = ShowType.Movie
                            };

                            if (movies.Any(x => x.Id == upcomingMovie.Id))
                            {
                                // Skip if already exists
                            }
                            else
                            {
                                movies.Add(mov);
                            }
                        }
                    }
                });

                page++;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task GetMorePageForNowPlayingMovies()
        {
            try
            {
                var nowPlayingList = await Service.client.GetMovieNowPlayingListAsync(language, page);
           
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var nowPlayingMovie in nowPlayingList.Results)
                    {
                        if (!String.IsNullOrWhiteSpace(nowPlayingMovie.PosterPath))
                        {
                            Movie mov = new Movie()
                            {
                                Poster = Service.client.GetImageUrl("w500", nowPlayingMovie.PosterPath).AbsoluteUri,
                                Name = nowPlayingMovie.Title,
                                Id = nowPlayingMovie.Id,
                                Rating = nowPlayingMovie.VoteAverage,
                                ShowType = ShowType.Movie
                            };
                            if (movies.Any(x => x.Id == nowPlayingMovie.Id))
                            {
                                // Skip if already exists
                            }
                            else
                            {
                                movies.Add(mov);
                            }
                        }
                    }
                });

                page++;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        public async Task GetPopularTvShows()
        {
            try
            {
                if (!Service.client.HasConfig)
                {
                    await Service.client.GetConfigAsync();
                }
                
                var discoveredTvShows = await Service.client.DiscoverTvShowsAsync()
                    .WhereGenresExclude(new List<int>{10767,10766,10768,10764,10763,10762,99})
                 //   .WithOriginCountry("US")
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
                    
                    if (movies.Any(x => x.Id == discoveredTvShow.Id))
                    {
                        // Skip if already exists
                    }
                    else
                    {
                        movies.Add(tv);
                    }
                    
                }

                page++;

            }
            catch (Exception e)
            {
                Log.Error("Error on discover tv shows: " + e.Message);
            }
        }

       

        private async Task GetMorePageForTopRatedTvShows()
        {
            try
            {
                var topRatedList = await Service.client.GetTvShowTopRatedAsync(page, language);
           
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var topRatedTvShow in topRatedList.Results)
                    {
                        if (!String.IsNullOrWhiteSpace(topRatedTvShow.PosterPath))
                        {
                            Movie mov = new Movie()
                            {
                                Poster = Service.client.GetImageUrl("w500", topRatedTvShow.PosterPath).AbsoluteUri,
                                Name = topRatedTvShow.Name,
                                Id = topRatedTvShow.Id,
                                Rating = topRatedTvShow.VoteAverage,
                                ShowType = ShowType.TvShow
                            };
                            if (movies.Any(x => x.Id == topRatedTvShow.Id))
                            {
                                // Skip if already exists
                            }
                            else
                            {
                                movies.Add(mov);
                            }
                        }
                    }
                });

                page++;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

      
        
        public async Task GetMorePageForAiringTodayTvShows()
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
                    
                    if ( movies.Any(x => x.Id == tv.Id))
                    {
                    }
                    else
                    {
                        movies.Add(tv);
                    }
                    
                }

                page++;

            }
            catch (Exception e)
            {
                Log.Error("Error on discover tv shows: " + e.Message);
            }
        }

        private void ExploreMorePage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyResponsiveLayout(MainView.Instance.screenWidth);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.width);
        }
        
        private double CalculateScaledValue(double width, double minValue, double maxValue)
        {
            const double minWidth = 320;
            const double maxWidth = 3840;
            
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);
            
            return Math.Round(scaledValue);
        }
        
        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                TextBlockCollectionName.FontSize = CalculateScaledValue(width, 16, 32);
                VerticalMoviesControl.ApplyResponsiveLayout(width);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public void Dispose()
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
            VerticalMoviesControl.ScrollReachedBottom -= VerticalMoviesControl_ScrollReachedBottom;
            foreach (var visualDescendant in VerticalMoviesControl.MoviesDisplay.GetVisualDescendants())
            {
                if (visualDescendant is AsyncImageControl asyncImageControl)
                {
                    asyncImageControl.Dispose();
                }
            }
            this.movies.Clear();
            this.movies = null;
            VerticalMoviesControl.ItemsSource = null;
            VerticalMoviesControl.MoviesDisplay.ItemsSource = null;
            this.DataContext = null;
            
            // Bellek temizliği
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private async void VerticalMoviesControl_ScrollReachedBottom(object sender, EventArgs e)
        {
            try
            {
                if (exploreMore == ExploreMore.Movie)
                {
                    switch (exploreMoreList)
                    {
                        case ExploreMoreList.Popular:
                            await GetMorePageForPopularMovies();
                            break;
                        case ExploreMoreList.TopRated:
                            await GetMorePageForTopRatedMovies();
                            break;
                        case ExploreMoreList.UpComing:
                            await GetMorePageForUpComingMovies();
                            break;
                        case ExploreMoreList.NowPlaying:
                            await GetMorePageForNowPlayingMovies();
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (exploreMoreList)
                    {
                        case ExploreMoreList.Popular:
                            await GetPopularTvShows();
                            break;
                        case ExploreMoreList.TopRated:
                            await GetMorePageForTopRatedTvShows();
                            break;
                        case ExploreMoreList.NowPlaying:
                            await GetMorePageForAiringTodayTvShows();
                            break;
                        default:
                            break;
                    }
                }
                
                ApplyResponsiveLayout(MainView.Instance.screenWidth);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            
        }
    }
} 