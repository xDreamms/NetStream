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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DynamicData;
using Serilog;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for ExploreMorePage.xaml
    /// </summary>
    public partial class ExploreMorePage : Page
    {
        private int page = 2;
        private ExploreMore exploreMore;
        private ExploreMoreList exploreMoreList;
        private FastObservableCollection<Movie> movies = new FastObservableCollection<Movie>();
        private string language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;
        public ExploreMorePage(FastObservableCollection<Movie> movies, ExploreMore exploreMore, ExploreMoreList exploreMoreList,string header)
        {
            InitializeComponent();
            this.exploreMore = exploreMore;
            this.exploreMoreList = exploreMoreList;
            this.TextBlockCollectionName.Text = header;
            this.MoviesDisplay.ItemsSource = this.movies;
            this.movies.AddRange(movies);
        }


        private void MoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)MoviesDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    if (this.NavigationService != null)
                    {
                        this.NavigationService.Navigate(movieDetailsPage);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task GetMorePageForPopularMovies()
        {
            try
            {
                var popularList = await Service.client.GetMoviePopularListAsync(language, page);
           
                foreach (var popularMovie in popularList.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
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
                            }
                            else
                            {
                                iDelayed.Add(mov);
                            }
                        }
                    }
                }
                page++;
            }
            catch (System.Exception e)
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
           
                foreach (var topRatedMovie in topRatedList.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
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
                            }
                            else
                            {
                                iDelayed.Add(mov);
                            }
                        }
                    }
                }

                page++;
            }
            catch (System.Exception e)
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
           
                foreach (var upcomingMovie in upcomingList.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
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
                            }
                            else
                            {
                                iDelayed.Add(mov);
                            }
                        }
                    }
                }

                page++;
            }
            catch (System.Exception e)
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
           
                foreach (var nowPlayingMovie in nowPlayingList.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
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
                            }
                            else
                            {
                                iDelayed.Add(mov);
                            }
                        }
                    }
                }

                page++;
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task GetMorePageForPopularTvShows()
        {
            try
            {

                var popularList = await Service.client.GetTvShowPopularAsync(page, language);
            
                foreach (var popularTvShow in popularList.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
                    {
                        if (!String.IsNullOrWhiteSpace(popularTvShow.PosterPath))
                        {
                            Movie mov = new Movie()
                            {
                                Poster = Service.client.GetImageUrl("w500", popularTvShow.PosterPath).AbsoluteUri,
                                Name = popularTvShow.Name,
                                Id = popularTvShow.Id,
                                Rating = popularTvShow.VoteAverage,
                                ShowType = ShowType.TvShow
                            };
                            if (movies.Any(x => x.Id == popularTvShow.Id))
                            {
                            }
                            else
                            {
                                iDelayed.Add(mov);
                            }
                        }
                    }
                }

                page++;
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task GetMorePageForTopRatedTvShows()
        {
            try
            {
                var topRatedList = await Service.client.GetTvShowTopRatedAsync(page, language);
           
                foreach (var topRatedTvShow in topRatedList.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
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
                            }
                            else
                            {
                                iDelayed.Add(mov);
                            }
                        }
                    }
                }

                page++;
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task GetMorePageForAiringTodayTvShows()
        {

            try
            {
                var airingTodayList = await Service.client.GetTvShowListAsync(TvShowListType.AiringToday, page, language);
            
                foreach (var airingTodayTvShow in airingTodayList.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
                    {
                        if (!String.IsNullOrWhiteSpace(airingTodayTvShow.PosterPath))
                        {
                            Movie mov = new Movie()
                            {
                                Poster = Service.client.GetImageUrl("w500", airingTodayTvShow.PosterPath).AbsoluteUri,
                                Name = airingTodayTvShow.Name,
                                Id = airingTodayTvShow.Id,
                                Rating = airingTodayTvShow.VoteAverage,
                                ShowType = ShowType.TvShow
                            };
                            if (movies.Any(x => x.Id == airingTodayTvShow.Id))
                            {
                            }
                            else
                            {
                                iDelayed.Add(mov);
                            }
                        }
                    }
                }

                page++;
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
            
    }


        private async void MoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (e.VerticalOffset == e.ExtentHeight - e.ViewportHeight)
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
                                await GetMorePageForPopularTvShows();
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

                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ExploreMorePage_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void ExploreMorePage_OnUnloaded(object sender, RoutedEventArgs e)
        {
        }

    }
}
