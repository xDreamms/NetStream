using Microsoft.VisualBasic.Devices;
using MovieCollection.OpenSubtitles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NetStream.Properties;
using NetStream.Views;
using Newtonsoft.Json;
using Serilog;
using TMDbLib.Client;
using TMDbLib.Objects.Account;
using TMDbLib.Objects.Authentication;
using TMDbLib.Objects.Changes;
using TMDbLib.Objects.Discover;
using TMDbLib.Objects.Exceptions;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using YoutubeExplode;

namespace NetStream
{
    public enum ImageFormat
    {
        bmp,
        jpeg,
        gif,
        tiff,
        png,
        unknown
    }

    public enum ImageType
    {
        Cast,
        Poster,
        Backdrop
    }
    public class Service
    {
        public static TMDbClient client = new TMDbClient("c5587ca9eca73d0f7cbed1965b505742");
        public static YoutubeClient YoutubeClient = new YoutubeClient();

        public static FastObservableCollection<Movie> SearchedList = new FastObservableCollection<Movie>();

        public static FastObservableCollection<Movie> PopularMovies = new FastObservableCollection<Movie>();
        public static FastObservableCollection<Movie> TopRatedMovies = new FastObservableCollection<Movie>();
        public static FastObservableCollection<Movie> UpComingMovies = new FastObservableCollection<Movie>();
        public static FastObservableCollection<Movie> NowPlayingMovies = new FastObservableCollection<Movie>();

        public static FastObservableCollection<Movie> PopularTvShows = new FastObservableCollection<Movie>();
        public static FastObservableCollection<Movie> TopRatedTvShows = new FastObservableCollection<Movie>();
        public static FastObservableCollection<Movie> AiringTodayTvShows = new FastObservableCollection<Movie>();

        public static FastObservableCollection<Cast> MovieCasts = new FastObservableCollection<Cast>();
        public static FastObservableCollection<Cast> TvShowCasts = new FastObservableCollection<Cast>();


        public static MainMovie MainMovieee;
        public static MainMovie MainTvshoww;

        public static string language;

        public static async Task GetPopularMoviesAsync(int page)
        {
            if (!client.HasConfig)
            {
                await client.GetConfigAsync();
            }
            PopularMovies.Clear();
            var popularMovies = await client.GetMoviePopularListAsync(language, page);
            foreach (var movie in popularMovies.Results)
            {
                using (FastObservableCollection<Movie> iDelayed = PopularMovies.DelayNotifications())
                {
                    Movie mov = new Movie()
                    {
                        Poster = (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                        Id = movie.Id,
                        Name = movie.Title,
                        Rating = movie.VoteAverage,
                        ShowType = ShowType.Movie,
                    };
                    if (PopularMovies.Any(x => x.Id == movie.Id))
                    {
                    }
                    else
                    {
                        iDelayed.Add(mov);
                    }
                }
            }
        }
        public static async Task GetTopRatedMovies( int page)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                TopRatedMovies.Clear();
                var topRatedMovs = await client.GetMovieTopRatedListAsync(language, page);
                foreach (var movie in topRatedMovs.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = TopRatedMovies.DelayNotifications())
                    {
                        Movie mov = new Movie()
                        {
                            Poster = (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                            Id = movie.Id,
                            Name = movie.Title,
                            Rating = movie.VoteAverage,
                            ShowType = ShowType.Movie,
                        };
                        if (TopRatedMovies.Any(x => x.Id == movie.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(mov);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching top rated movies: " + e.Message);
            }
        }
        public static async Task GetUpComingMovies(int page)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                UpComingMovies.Clear();
                var upComingMovs = await client.GetMovieUpcomingListAsync(language, page);
                foreach (var movie in upComingMovs.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = UpComingMovies.DelayNotifications())
                    {
                        Movie mov = new Movie()
                        {
                            Poster = (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                            Id = movie.Id,
                            Name = movie.Title,
                            Rating = movie.VoteAverage,
                            ShowType = ShowType.Movie
                        };
                        if (UpComingMovies.Any(x => x.Id == movie.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(mov);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching upcoming movies: " + e.Message);
            }
        }
        public static async Task GetNowPlayingMovies(int page)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                NowPlayingMovies.Clear();
                var nowPlayingMovs = await client.GetMovieNowPlayingListAsync(language, page);
                foreach (var movie in nowPlayingMovs.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = NowPlayingMovies.DelayNotifications())
                    {
                        Movie mov = new Movie()
                        {
                            Poster = (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                            Id = movie.Id,
                            Name = movie.Title,
                            Rating = movie.VoteAverage,
                            ShowType = ShowType.Movie
                        };
                        if (NowPlayingMovies.Any(x => x.Id == movie.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(mov);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching now playing movies: " + e.Message);
            }
        }

        public static async Task GetPopularTvShowsAsync( int page)
        {
            if (!client.HasConfig)
            {
                await client.GetConfigAsync();
            }

            PopularTvShows.Clear();
            var popularTvShows = await client.GetTvShowPopularAsync(page, language);
            foreach (var tvShow in popularTvShows.Results)
            {
                using (FastObservableCollection<Movie> iDelayed = PopularTvShows.DelayNotifications())
                {
                    Movie mov = new Movie()
                    {
                        Poster = (client.GetImageUrl("w500", tvShow.PosterPath).AbsoluteUri),
                        Id = tvShow.Id,
                        Name = tvShow.Name,
                        Rating = tvShow.VoteAverage,
                        ShowType = ShowType.TvShow
                    };
                    if (PopularTvShows.Any(x => x.Id == tvShow.Id))
                    {
                    }
                    else
                    {
                        iDelayed.Add(mov);
                    }
                }
            }
        }
        public static async Task GetTopRatedTvShows( int page)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                TopRatedTvShows.Clear();
                var topRatedTvShows = await client.GetTvShowTopRatedAsync(page, language);
                foreach (var movie in topRatedTvShows.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = TopRatedTvShows.DelayNotifications())
                    {
                        Movie mov = new Movie()
                        {
                            Poster = (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                            Id = movie.Id,
                            Name = movie.Name,
                            Rating = movie.VoteAverage,
                            ShowType = ShowType.TvShow
                        };
                        if (TopRatedTvShows.Any(x => x.Id == movie.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(mov);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching top rated tv shows: " + e.Message);
            }
        }
        public static async Task GetAiringTodayTvShows(int page)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                AiringTodayTvShows.Clear();
                var airingTodayTvShows = await client.GetTvShowListAsync(TvShowListType.AiringToday, language, page);
                foreach (var movie in airingTodayTvShows.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = AiringTodayTvShows.DelayNotifications())
                    {
                        Movie mov = new Movie()
                        {
                            Poster = (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                            Id = movie.Id,
                            Name = movie.Name,
                            Rating = movie.VoteAverage,
                            ShowType = ShowType.TvShow
                        };
                        if (AiringTodayTvShows.Any(x => x.Id == movie.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(mov);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching airing today tv shows: " + e.Message);
            }
        }

       

        
        public static async Task GetSearchedMoviesTvShows(string searchKey,int searchPage, SearchPageResults searchPageResults)
        {
            SearchedList.Clear();
            var searchedMovies = await client.SearchMovieAsync(searchKey, language, searchPage);
            var searchedTvs = await client.SearchTvShowAsync(searchKey, language, searchPage);

            var itemCount = searchedMovies.TotalResults + searchedTvs.TotalResults;
            searchPageResults.ItemCountText.Text = itemCount + " items";

            foreach (var movie in searchedMovies.Results)
            {
                using (FastObservableCollection<Movie> iDelayed = SearchedList.DelayNotifications())
                {
                    Movie mov = new Movie()
                    {
                        Poster = (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                        Id = movie.Id,
                        Name = movie.Title,
                        Rating = movie.VoteAverage,
                        ShowType = ShowType.Movie
                    };

                    if (SearchedList.Any(x => x.Id == movie.Id))
                    {
                    }
                    else
                    {
                        iDelayed.Add(mov);
                    }
                }
            }

            foreach (var movie in searchedTvs.Results)
            {
                using (FastObservableCollection<Movie> iDelayed = SearchedList.DelayNotifications())
                {
                    Movie mov = new Movie()
                    {
                        Poster = (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                        Id = movie.Id,
                        Name = movie.Name,
                        Rating = movie.VoteAverage,
                        ShowType = ShowType.TvShow
                    };

                    if (SearchedList.Any(x => x.Id == movie.Id))
                    {
                    }
                    else
                    {
                        iDelayed.Add(mov);
                    }
                }
            }
        }

        public static FastObservableCollection<Movie> SearchedMoviesResult = new FastObservableCollection<Movie>();

        public static async Task GetSearchedMovies(string query, int page, SearchPageResults searchPageResults)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                SearchedMoviesResult.Clear();
                var searchedMovies = await client.SearchMovieAsync(query, language, page);

                var itemCount = searchedMovies.TotalResults;
                searchPageResults.TotalResultMovies = itemCount;
                searchPageResults.ItemCountText.Text = itemCount + " items";

                foreach (var movie in searchedMovies.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = SearchedMoviesResult.DelayNotifications())
                    {
                        var mov = new Movie()
                        {
                            Poster = client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri,
                            Id = movie.Id,
                            Name = movie.Title,
                            Rating = movie.VoteAverage,
                            ShowType = ShowType.Movie
                        };
                        if (SearchedMoviesResult.Any(x => x.Id == movie.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(mov);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching searched movies: " + e.Message);
            }
        }

        public static async Task GetMorePagesSearchedMovies(string query,  int page)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var searchedMovies = await client.SearchMovieAsync(query, language, page);

                foreach (var movie in searchedMovies.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = SearchedMoviesResult.DelayNotifications())
                    {
                        var mov = new Movie()
                        {
                            Poster = client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri,
                            Id = movie.Id,
                            Name = movie.Title,
                            Rating = movie.VoteAverage,
                            ShowType = ShowType.Movie
                        };
                        if (SearchedMoviesResult.Any(x => x.Id == movie.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(mov);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching searched more page movies: " + e.Message);
            }
        }

        public static FastObservableCollection<Movie> SearchedTvShowsResult = new FastObservableCollection<Movie>();

        public static async Task GetSearchedTvShows(string query,  int page, SearchPageResults searchPageResults)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                SearchedTvShowsResult.Clear();

                var tvshows = await client.SearchTvShowAsync(query, language, page);

                var itemCount = tvshows.TotalResults;
                searchPageResults.TotalResultTvShows = itemCount;
                searchPageResults.ItemCountText.Text = itemCount + " items";

                foreach (var tvshow in tvshows.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = SearchedTvShowsResult.DelayNotifications())
                    {
                        var tv = new Movie()
                        {
                            Id = tvshow.Id,
                            Name = tvshow.Name,
                            Poster = client.GetImageUrl("w500", tvshow.PosterPath).AbsoluteUri,
                            Rating = tvshow.VoteAverage,
                            ShowType = ShowType.TvShow
                        };
                        if (SearchedTvShowsResult.Any(x => x.Id == tvshow.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(tv);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching searched tv shows: " + e.Message);
            }
        }

        public static async Task GetMorePagesSearchedTvShows(string query, int page)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var tvshows = await client.SearchTvShowAsync(query, language, page);
                foreach (var tvshow in tvshows.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = SearchedTvShowsResult.DelayNotifications())
                    {
                        var tv = new Movie()
                        {
                            Id = tvshow.Id,
                            Name = tvshow.Name,
                            Poster = client.GetImageUrl("w500", tvshow.PosterPath).AbsoluteUri,
                            Rating = tvshow.VoteAverage,
                            ShowType = ShowType.TvShow
                        };
                        if (SearchedTvShowsResult.Any(x => x.Id == tvshow.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(tv);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching searched tv shows more page: " + e.Message);
            }
        }

        public static FastObservableCollection<Cast> SearchedCastsResult = new FastObservableCollection<Cast>();
        public static async Task GetSearchedCasts(string query,  int page,SearchPageResults searchPageResults)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                SearchedCastsResult.Clear();
                var persons = await client.SearchPersonAsync(query, language, page);

                var itemCount = persons.TotalResults;
                searchPageResults.TotalResultPersons = itemCount;
                searchPageResults.ItemCountText.Text = itemCount + " items";


                foreach (var x in persons.Results)
                {
                    using (FastObservableCollection<Cast> iDelayed = SearchedCastsResult.DelayNotifications())
                    {
                        var person = new Cast()
                            { Id = x.Id, Name = x.Name, Poster = client.GetImageUrl("w500", x.ProfilePath).AbsoluteUri };
                        if (SearchedCastsResult.Any(z => z.Id == x.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(person);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on searched casts: " + e.Message);
            }
        }

        public static async Task GetMorePagesSearchedCasts(string query,  int page)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var persons = await client.SearchPersonAsync(query, language, page);

                foreach (var x in persons.Results)
                {
                    using (FastObservableCollection<Cast> iDelayed = SearchedCastsResult.DelayNotifications())
                    {
                        var person = new Cast()
                            { Id = x.Id, Name = x.Name, Poster = client.GetImageUrl("w500", x.ProfilePath).AbsoluteUri };
                        if (SearchedCastsResult.Any(z => z.Id == x.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(person);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on searched casts more pages: " + e.Message);
            }
        }

        public static async Task GetMorePagesSearchedMoviesTvShows(int searchPage,string searchKey)
        {
            var searchedMovies = await client.SearchMovieAsync(searchKey, language, searchPage);
            var searchedTvShows = await client.SearchTvShowAsync(searchKey, language, searchPage);

            foreach (var movie in searchedMovies.Results)
            {
                Movie mov = new Movie()
                {
                    Poster =  (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                    Id = movie.Id,
                    Name = movie.Title,
                    Rating = movie.VoteAverage,
                    ShowType = ShowType.Movie
                };

                if (SearchedList.Any(x => x.Id == movie.Id))
                {
                }
                else
                {
                    SearchedList.Add(mov);
                }
            }

            foreach (var movie in searchedTvShows.Results)
            {
                Movie mov = new Movie()
                {
                    Poster =  (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                    Id = movie.Id,
                    Name = movie.Name,
                    Rating = movie.VoteAverage,
                    ShowType = ShowType.TvShow
                };

                if (SearchedList.Any(x => x.Id == movie.Id))
                {
                }
                else
                {
                    SearchedList.Add(mov);
                }
            }
        }

        public static FastObservableCollection<Movie> DiscoveredMovies = new FastObservableCollection<Movie>();

        public static async Task DiscoverMovies(List<int> genres,DateTime afterDate,DateTime beforeDate,double minVoteAverage,int voteCount,int minRuntime,int maxRuntime,/*List<TMDbLib.Objects.General.Genre> keywords,*/int page,DiscoverMovieSortBy sortBy, string originalLanguage)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                DiscoveredMovies.Clear();
                var discoveredMovies = await client.DiscoverMoviesAsync()
                    .IncludeWithAllOfGenre(genres)
                    .WherePrimaryReleaseDateIsAfter(afterDate)
                    .WherePrimaryReleaseDateIsBefore(beforeDate)
                    .WhereVoteAverageIsAtLeast(minVoteAverage)
                    .WhereVoteCountIsAtLeast(voteCount)
                    .WhereRuntimeIsAtLeast(minRuntime)
                    .WhereRuntimeIsAtMost(maxRuntime)
                    .WhereOriginalLanguageIs(originalLanguage)
                    .IncludeAdultMovies(true)
                    .OrderBy(sortBy).Query(language,page);

                foreach (var discoveredMovie in discoveredMovies.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = DiscoveredMovies.DelayNotifications())
                    {
                        var movie = new Movie()
                        {
                            Id = discoveredMovie.Id,
                            Name = discoveredMovie.Title,
                            Poster = client.GetImageUrl("w500",discoveredMovie.PosterPath).AbsoluteUri,
                            Rating = discoveredMovie.VoteAverage,
                            ShowType = ShowType.Movie
                        };


                        if (DiscoveredMovies.Any(x => x.Id == movie.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(movie);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on discover movies: " + e.Message);
            }
        }

        public static async Task GetMorePagesDiscoverMovies(List<int> genres, DateTime afterDate, DateTime beforeDate, double minVoteAverage, int voteCount, int minRuntime, int maxRuntime, /*List<TMDbLib.Objects.General.Genre> keywords,*/  int page, DiscoverMovieSortBy sortBy,string originalLanguage)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var discoveredMovies = await client.DiscoverMoviesAsync()
                    .IncludeWithAllOfGenre(genres)
                    .WherePrimaryReleaseDateIsAfter(afterDate)
                    .WherePrimaryReleaseDateIsBefore(beforeDate)
                    .WhereVoteAverageIsAtLeast(minVoteAverage)
                    .WhereVoteCountIsAtLeast(voteCount)
                    .WhereRuntimeIsAtLeast(minRuntime)
                    .WhereRuntimeIsAtMost(maxRuntime)
                    .WhereOriginalLanguageIs(originalLanguage)
                    .IncludeAdultMovies(true)
                    .OrderBy(sortBy).Query(language, page);

                foreach (var discoveredMovie in discoveredMovies.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = DiscoveredMovies.DelayNotifications())
                    {
                        var movie = new Movie()
                        {
                            Id = discoveredMovie.Id,
                            Name = discoveredMovie.Title,
                            Poster = client.GetImageUrl("w500", discoveredMovie.PosterPath).AbsoluteUri,
                            Rating = discoveredMovie.VoteAverage,
                            ShowType = ShowType.Movie
                        };


                        if (DiscoveredMovies.Any(x => x.Id == movie.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(movie);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on discover movies more pages: " + e.Message);
            }
        }


        public static FastObservableCollection<Movie> DiscoveredTvShows = new FastObservableCollection<Movie>();

        public static async Task DiscoverTvShows(List<int> genres, DateTime afterDate, DateTime beforeDate, double minVoteAverage, int voteCount, int minRuntime, int maxRuntime, /*List<TMDbLib.Objects.General.Genre> keywords,*/  int page, DiscoverTvShowSortBy sortBy,string originalLanguage)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                DiscoveredTvShows.Clear();
                var discoveredTvShows = await client.DiscoverTvShowsAsync()
                    .WhereGenresInclude(genres)
                    .WhereFirstAirDateIsAfter(afterDate)
                    .WhereFirstAirDateIsBefore(beforeDate)
                    .WhereVoteAverageIsAtLeast(minVoteAverage)
                    .WhereVoteCountIsAtLeast(voteCount)
                    .WhereRuntimeIsAtLeast(minRuntime)
                    .WhereRuntimeIsAtMost(maxRuntime)
                    .WhereOriginalLanguageIs(originalLanguage)
                    .OrderBy(sortBy).Query(language, page);

                foreach (var discoveredTvShow in discoveredTvShows.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = DiscoveredTvShows.DelayNotifications())
                    {
                        var tv = new Movie()
                        {
                            Id = discoveredTvShow.Id,
                            Name = discoveredTvShow.Name,
                            Poster = client.GetImageUrl("w500", discoveredTvShow.PosterPath).AbsoluteUri,
                            Rating = discoveredTvShow.VoteAverage,
                            ShowType = ShowType.TvShow
                        };


                        if (DiscoveredTvShows.Any(x => x.Id == tv.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(tv);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on discover tv shows: " + e.Message);
            }
        }

        public static async Task GetMorePagesDiscoverTvShows(List<int> genres, DateTime afterDate, DateTime beforeDate, double minVoteAverage, int voteCount, int minRuntime, int maxRuntime, /*List<TMDbLib.Objects.General.Genre> keywords,*/  int page, DiscoverTvShowSortBy sortBy, string originalLanguage)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var discoveredTvShows = await client.DiscoverTvShowsAsync()
                    .WhereGenresInclude(genres)
                    .WhereFirstAirDateIsAfter(afterDate)
                    .WhereFirstAirDateIsBefore(beforeDate)
                    .WhereVoteAverageIsAtLeast(minVoteAverage)
                    .WhereVoteCountIsAtLeast(voteCount)
                    .WhereRuntimeIsAtLeast(minRuntime)
                    .WhereRuntimeIsAtMost(maxRuntime)
                    .WhereOriginalLanguageIs(originalLanguage)
                    .OrderBy(sortBy).Query(language, page);

                foreach (var discoveredTvShow in discoveredTvShows.Results)
                {
                    using (FastObservableCollection<Movie> iDelayed = DiscoveredTvShows.DelayNotifications())
                    {
                        var tv = new Movie()
                        {
                            Id = discoveredTvShow.Id,
                            Name = discoveredTvShow.Name,
                            Poster = client.GetImageUrl("w500", discoveredTvShow.PosterPath).AbsoluteUri,
                            Rating = discoveredTvShow.VoteAverage,
                            ShowType = ShowType.TvShow
                        };


                        if (DiscoveredTvShows.Any(x => x.Id == tv.Id))
                        {
                        }
                        else
                        {
                            iDelayed.Add(tv);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on discover tv shows more pages: " + e.Message);
            }
        }


        public static async Task<FastObservableCollection<Genre>> GetMovieGenres()
        {
            FastObservableCollection<Genre> result = new FastObservableCollection<Genre>();
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var genres = await client.GetMovieGenresAsync(language);

                foreach (var genre in genres)
                {
                    using (FastObservableCollection<Genre> iDelayed = result.DelayNotifications())
                    {
                        iDelayed.Add(new Genre()
                        {
                            Id = genre.Id,
                            Name = genre.Name,
                            IsSelected = false

                        });
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get movie genres: " + e.Message);
            }
            return result;
        }

        public static async Task<FastObservableCollection<Genre>> GetTvGenres()
        {
            FastObservableCollection<Genre> result = new FastObservableCollection<Genre>();
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var genres = await client.GetTvGenresAsync(language);

                foreach (var genre in genres)
                {
                    using (FastObservableCollection<Genre> iDelayed = result.DelayNotifications())
                    {
                        iDelayed.Add(new Genre()
                        {
                            Id = genre.Id,
                            Name = genre.Name,
                            IsSelected = false

                        });
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get tv genres: " + e.Message);
            }

            return result;
        }

        public static async Task GetMainMovieDetail()
        {
            if (!client.HasConfig)
            {
                await client.GetConfigAsync();
            }
            if (PopularMovies.Count > 0)
                {
                    var mainPopularMovie = PopularMovies.FirstOrDefault();
                    var mainMovie = await client.GetMovieAsync(mainPopularMovie.Id, language);

                    var MainMovieMov = new MainMovie()
                    {
                        Poster =  (client.GetImageUrl(client.Config.Images.BackdropSizes[client.Config.Images.BackdropSizes.Count - 2], mainMovie.BackdropPath).AbsoluteUri),
                        Name = mainMovie.Title,
                        Duration = mainMovie.Runtime.ToString() + " " + Application.Current.Resources["MinString"],
                        Overview = mainMovie.Overview,
                        Rating = mainMovie.VoteAverage,
                        ReleaseYear = mainMovie.ReleaseDate.Value.Year.ToString(),
                        ReviewCount = mainMovie.VoteCount.ToString() + " " + Application.Current.Resources["ReviewsString"],
                        Id = mainMovie.Id,
                        ShowType = ShowType.Movie
                    };
                    MainMovieee = MainMovieMov;
                }
                else
                {
                    var popularMovies = await client.GetMoviePopularListAsync(language, 1);
                    var mainPopularMovie = popularMovies.Results.FirstOrDefault();
                    var mainMovie = await client.GetMovieAsync(mainPopularMovie.Id, language);

                    var MainMovieMov = new MainMovie()
                    {
                        Poster =  (client.GetImageUrl(client.Config.Images.BackdropSizes[client.Config.Images.BackdropSizes.Count - 2], mainMovie.BackdropPath).AbsoluteUri),
                        Name = mainMovie.Title,
                        Duration = mainMovie.Runtime.ToString() + " " + Application.Current.Resources["MinString"],
                        Overview = mainMovie.Overview,
                        Rating = mainMovie.VoteAverage,
                        ReleaseYear = mainMovie.ReleaseDate.Value.Year.ToString(),
                        ReviewCount = mainMovie.VoteCount.ToString() +" " + Application.Current.Resources["ReviewsString"],
                        Id = mainMovie.Id,
                        ShowType = ShowType.Movie
                    };
                
                    MainMovieee = MainMovieMov;
                }
            
        }

        public static async Task<string> GetTrailerLink(ShowType showType,int showId)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                if (showType == ShowType.Movie)
                {
                    string trailerLink = "https://www.youtube.com/watch?v=";
                    var videos = await client.GetMovieVideosAsync(showId);
                    var videoTrailer = videos.Results.FirstOrDefault(x => x.Type.ToLower().Contains("trailer"));
                    if (videoTrailer == null) return "";
                    string url = TruncateLongString(videoTrailer.Key, 11);
                    trailerLink += url;
                    return trailerLink;
                }
                else if (showType == ShowType.TvShow)
                {
                    string trailerLink = "https://www.youtube.com/watch?v=";
                    var videos = await client.GetTvShowVideosAsync(showId);
                    var videoTrailer = videos.Results.FirstOrDefault(x => x.Type.ToLower().Contains("trailer"));
                    if (videoTrailer == null) return "";
                    string url = TruncateLongString(videoTrailer.Key, 11);
                    trailerLink += url;
                    return trailerLink;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get trailer link: " + e.Message);
            }

            return "";
        }

        public static string TruncateLongString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            return str.Substring(0, Math.Min(str.Length, maxLength));
        }

        public static async Task GetMainTvShowDetail()
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                if (PopularTvShows.Count > 0)
                {
                    var mainPopularTvShow = PopularTvShows.FirstOrDefault();

                    var mainTvShow = await client.GetTvShowAsync(mainPopularTvShow.Id, TvShowMethods.Undefined, language);

                    var MainMovieMov = new MainMovie()
                    {
                        Poster =  (client.GetImageUrl(client.Config.Images.BackdropSizes[client.Config.Images.BackdropSizes.Count - 2], mainTvShow.BackdropPath).AbsoluteUri),
                        Name = mainTvShow.Name,
                        Duration = mainTvShow.EpisodeRunTime.OrderByDescending(x => x).FirstOrDefault() + " " + Application.Current.Resources["MinString"],
                        Overview = mainTvShow.Overview,
                        Rating = mainTvShow.VoteAverage,
                        ReleaseYear = mainTvShow.FirstAirDate.Value.Year.ToString(),
                        ReviewCount = mainTvShow.VoteCount.ToString() + " " + Application.Current.Resources["ReviewsString"],
                        Id = mainTvShow.Id,
                        ShowType = ShowType.TvShow
                    };
                    MainTvshoww = MainMovieMov;
                }
                else
                {
                    var popularTvShows = await client.GetTvShowPopularAsync(1, language);
                    var mainPopularTvShow = popularTvShows.Results.FirstOrDefault();

                    var mainTvShow = await client.GetTvShowAsync(mainPopularTvShow.Id, TvShowMethods.Undefined, language);

                    var MainMovieMov = new MainMovie()
                    {
                        Poster =  (client.GetImageUrl(client.Config.Images.BackdropSizes[client.Config.Images.BackdropSizes.Count - 2], mainTvShow.BackdropPath).AbsoluteUri),
                        Name = mainTvShow.Name,
                        Duration = mainTvShow.EpisodeRunTime.OrderByDescending(x => x).FirstOrDefault() + " " + Application.Current.Resources["MinString"],
                        Overview = mainTvShow.Overview,
                        Rating = mainTvShow.VoteAverage,
                        ReleaseYear = mainTvShow.FirstAirDate.Value.Year.ToString(),
                        ReviewCount = mainTvShow.VoteCount.ToString() + " " + Application.Current.Resources["ReviewsString"],
                        Id = mainTvShow.Id,
                        ShowType = ShowType.TvShow
                    };
                    MainTvshoww = MainMovieMov;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on main tv show detail: " + e.Message);
            }
            
        }

        //public static BitmapImage GetImage(string url,ImageType imageType)
        //{
        //    var bitmapImage = new BitmapImage(new Uri(url,UriKind.Absolute));
        //    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //    bitmapImage.CreateOptions = BitmapCreateOptions.DelayCreation;
        //    return bitmapImage;
        //    //using (HttpClient httpClient = new HttpClient())
        //    //{
        //    //    var stream = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        //    //    var responseStream = await stream.Content.ReadAsStreamAsync();
        //    //    var bitmapImage = new BitmapImage();

        //    //    using (var memoryStream = new MemoryStream())
        //    //    {
        //    //        await responseStream.CopyToAsync(memoryStream);
        //    //        memoryStream.Seek(0, SeekOrigin.Begin);

        //    //        byte[] bytes = memoryStream.ToArray();
        //    //        if (GetImageFormat(bytes) != ImageFormat.unknown)
        //    //        {
        //    //            bitmapImage.BeginInit();
        //    //            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //    //            bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        //    //            bitmapImage.StreamSource = memoryStream;
        //    //            bitmapImage.EndInit();
        //    //            bitmapImage.Freeze();
        //    //            return bitmapImage;
        //    //        }
        //    //        else
        //    //        {
        //    //            if (imageType == ImageType.Cast)
        //    //            {
        //    //                bitmapImage = new BitmapImage(new Uri("pack://application:,,,/NetStream;component/userprofile.png"));
        //    //                return bitmapImage;
        //    //            }
        //    //            else if (imageType == ImageType.Poster)
        //    //            {
        //    //                bitmapImage = new BitmapImage(new Uri("pack://application:,,,/NetStream;component/blankMoviePoster.png"));
        //    //                return bitmapImage;
        //    //            }
        //    //            else if (imageType == ImageType.Backdrop)
        //    //            {
        //    //                bitmapImage = new BitmapImage(new Uri("pack://application:,,,/NetStream;component/blankMovieBackdrop.png"));
        //    //                return bitmapImage;
        //    //            }
        //    //        }
        //    //    }
        //    //}

        //    //return null;

        //}

        public static ImageFormat GetImageFormat(byte[] bytes)
        {
            // see http://www.mikekunz.com/image_file_header.html  
            var bmp = Encoding.ASCII.GetBytes("BM");     // BMP
            var gif = Encoding.ASCII.GetBytes("GIF");    // GIF
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var tiff = new byte[] { 73, 73, 42 };         // TIFF
            var tiff2 = new byte[] { 77, 77, 42 };         // TIFF
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon

            if (bmp.SequenceEqual(bytes.Take(bmp.Length)))
                return ImageFormat.bmp;

            if (gif.SequenceEqual(bytes.Take(gif.Length)))
                return ImageFormat.gif;

            if (png.SequenceEqual(bytes.Take(png.Length)))
                return ImageFormat.png;

            if (tiff.SequenceEqual(bytes.Take(tiff.Length)))
                return ImageFormat.tiff;

            if (tiff2.SequenceEqual(bytes.Take(tiff2.Length)))
                return ImageFormat.tiff;

            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
                return ImageFormat.jpeg;

            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
                return ImageFormat.jpeg;

            return ImageFormat.unknown;
        }

        public static async Task GetCredits( Movie selectedMovie)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                MovieCasts.Clear();
                TvShowCasts.Clear();
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    var credits = await client.GetMovieCreditsAsync(selectedMovie.Id);
                    foreach (var cast in credits.Cast)
                    {
                        Cast castt = new Cast()
                        {
                            Name = cast.Name,
                            Role = cast.Character,
                            Id = cast.Id
                        };

                        var profileUrl =
                            client.GetImageUrl(
                                client.Config.Images.ProfileSizes[client.Config.Images.ProfileSizes.Count - 2],
                                cast.ProfilePath);
                        castt.Poster =  (profileUrl.AbsoluteUri);
                        if (MovieCasts.Any(x => x.Id == cast.Id))
                        {
                        }
                        else
                        {
                            MovieCasts.Add(castt);
                        }
                    }
                }
                else if (selectedMovie.ShowType == ShowType.TvShow)
                {
                    var credits = await client.GetTvShowCreditsAsync(selectedMovie.Id, language);

                    foreach (var cast in credits.Cast)
                    {
                        Cast castt = new Cast()
                        {
                            Name = cast.Name,
                            Role = cast.Character,
                            Id = cast.Id
                        };

                        var profileUrl =
                            client.GetImageUrl(
                                client.Config.Images.ProfileSizes[client.Config.Images.ProfileSizes.Count - 2],
                                cast.ProfilePath);
                        castt.Poster =  (profileUrl.AbsoluteUri);
                        if (TvShowCasts.Any(x => x.Id == cast.Id))
                        {
                        }
                        else
                        {
                            TvShowCasts.Add(castt);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get credits: " + e.Message);
            }
            
        }

        public static string GetRuntime(int minutes)
        {
            if (minutes / 60 == 0)
            {
                return $"{minutes % 60}{App.Current.Resources["MinString"]}";
            }
            else
            {
                return $"{minutes / 60}{App.Current.Resources["HourString"]} {minutes % 60}{App.Current.Resources["MinString"]}";
            }
            
        }

        public static async Task<MovieDetail> GetMovieDetails(Movie selectedMovie)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var currentMovie = await client.GetMovieAsync(selectedMovie.Id, language,null,MovieMethods.ExternalIds|MovieMethods.Credits);
                if (currentMovie != null)
                {
                    var movieDetail = new MovieDetail()
                    {
                        Poster =  (client.GetImageUrl("w500", currentMovie.PosterPath).AbsoluteUri),
                        Budget = currentMovie.Budget.ToString("#,##0,,M", CultureInfo.InvariantCulture) + "$",
                        Language = Languages.FirstOrDefault(x=> x.Iso_639_1 == currentMovie.OriginalLanguage) != null ? Languages.FirstOrDefault(x => x.Iso_639_1 == currentMovie.OriginalLanguage).EnglishName : "",
                        Overview = currentMovie.Overview,
                        Production = currentMovie.ProductionCompanies.Count > 0 ? currentMovie.ProductionCompanies.FirstOrDefault().Name : "",
                        ReleaseDate = currentMovie.ReleaseDate.HasValue ? currentMovie.ReleaseDate.Value.ToShortDateString() : "",
                        Revenue = currentMovie.Revenue.ToString("#,##0,,M", CultureInfo.InvariantCulture) + "$",
                        Runtime = currentMovie.Runtime.HasValue ? GetRuntime(currentMovie.Runtime.Value) :"",
                        Status = currentMovie.Status,
                        ExternalIdsMovie = currentMovie.ExternalIds
                    };
                    if (currentMovie.Credits != null)
                    {
                        var director = currentMovie.Credits.Crew.FirstOrDefault(x => x.Job == "Director");
                        if (director != null)
                        {
                            movieDetail.Director = director.Name;
                            movieDetail.DirectorID = director.Id;
                        }
                    }
                
                    for (int i= 0; i< currentMovie.Genres.Count;i++)
                    {
                        var currentMovieGenre = currentMovie.Genres[i];
                        movieDetail.Genre += currentMovieGenre.Name + (i != currentMovie.Genres.Count - 1 ? ", " : "");
                    }

                    return movieDetail;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get movie details: " + e.Message);
            }

            return null;
        }

        public static async Task<TvShowDetail> GetTvShowDetails( Movie selectedMovie)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var currentMovie = await client.GetTvShowAsync(selectedMovie.Id, TvShowMethods.Credits |TvShowMethods.ExternalIds, language);
                if (currentMovie != null)
                {
                
                    var tvSHowDetail = new TvShowDetail()
                    {
                        Poster =  (client.GetImageUrl("w500", currentMovie.PosterPath).AbsoluteUri),
                        Overview = currentMovie.Overview,
                        Language = Languages.FirstOrDefault(x=> currentMovie.OriginalLanguage == x.Iso_639_1) != null ? Languages.FirstOrDefault(x => currentMovie.OriginalLanguage == x.Iso_639_1).EnglishName : "",
                        Production = currentMovie.ProductionCompanies.Count > 0? currentMovie.ProductionCompanies.FirstOrDefault().Name:"",
                        Status = currentMovie.Status,
                        ExternalIdsTvShow = currentMovie.ExternalIds
                    };

                    if (currentMovie.Credits != null)
                    {
                        var director = currentMovie.Credits.Crew.FirstOrDefault(x => x.Job == "Director");
                        if (director != null)
                        {
                            tvSHowDetail.Director = director.Name;
                            tvSHowDetail.DirectorId = director.Id;
                        }
                    }
                
                    for (int i=0; i< currentMovie.Genres.Count;i++)
                    {
                        var currentMovieGenre = currentMovie.Genres[i];
                        tvSHowDetail.Genre += currentMovieGenre.Name + (i != currentMovie.Genres.Count - 1 ? ", " : "");
                    }

                    return tvSHowDetail;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get tv show details: " + e.Message);
            }

            return null;
        }

        public static async Task<MainMovie> GetMainMovieDetail( Movie selectedMovie)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    var mainMovie = await client.GetMovieAsync(selectedMovie.Id, language,null, client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) ? MovieMethods.AccountStates : MovieMethods.Undefined);
                    AccountState accountState = null;
                    if (client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates == null)
                    {
                        accountState = await client.GetMovieAccountStateAsync(mainMovie.Id);
                    }
                    var MainMovieMov = new MainMovie()
                    {
                        Poster =  (client
                            .GetImageUrl(client.Config.Images.BackdropSizes[client.Config.Images.BackdropSizes.Count - 2],
                                mainMovie.BackdropPath).AbsoluteUri),
                        Name = mainMovie.Title,
                        Duration = mainMovie.Runtime.HasValue ? GetRuntime(mainMovie.Runtime.Value) : "",
                        Overview = mainMovie.Overview,
                        Rating = mainMovie.VoteAverage,
                        ReleaseYear = mainMovie.ReleaseDate.HasValue ? mainMovie.ReleaseDate.Value.Year.ToString() : "",
                        ReviewCount = mainMovie.VoteCount + " " + Application.Current.Resources["ReviewsString"],
                        IsFavorite = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Favorite : (accountState != null ? accountState.Favorite : false),
                        IsInWatchlist = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Watchlist: (accountState != null ? accountState.Watchlist : false),
                        MyRating = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Rating : (accountState != null ? accountState.Rating : null),
                    };
                    return MainMovieMov;
                }
                else if (selectedMovie.ShowType == ShowType.TvShow)
                {
                    var mainMovie = await client.GetTvShowAsync(selectedMovie.Id, client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) ? TvShowMethods.AccountStates:TvShowMethods.Undefined, language);
                    AccountState accountState = null;
                    if (client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates == null)
                    {
                        accountState = await client.GetTvShowAccountStateAsync(mainMovie.Id);
                    }
                    var MainMovieMov = new MainMovie()
                    {
                        Poster =  (client
                            .GetImageUrl(client.Config.Images.BackdropSizes[client.Config.Images.BackdropSizes.Count - 2],
                                mainMovie.BackdropPath).AbsoluteUri),
                        Name = mainMovie.Name,
                        Overview = mainMovie.Overview,
                        Rating = mainMovie.VoteAverage,
                        ReleaseYear = mainMovie.FirstAirDate.HasValue ? mainMovie.FirstAirDate.Value.Year.ToString():"",
                        ReviewCount = mainMovie.VoteCount + " " + Application.Current.Resources["ReviewsString"],
                        IsFavorite = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Favorite : (accountState != null ? accountState.Favorite : false),
                        IsInWatchlist = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Watchlist : (accountState != null ? accountState.Watchlist : false),
                        MyRating = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Rating : (accountState != null ? accountState.Rating : null),
                    };
                    return MainMovieMov;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get main movie detail: " + e.Message);
            }

            return null;
        }

        public static FastObservableCollection<PhotoDetail> PhotoDetailsBackdrop =
            new FastObservableCollection<PhotoDetail>();

        public static FastObservableCollection<PhotoDetail> PhotoDetailsPoster =
            new FastObservableCollection<PhotoDetail>();

        public static FastObservableCollection<VideoDetail> VideoDetails = new FastObservableCollection<VideoDetail>();
        public static async Task GetMoviePhotos(Movie selectedMovie, MovieDetailsPhotosPage movieDetailsPhotosPage)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                ImagesWithId MovieImages = null;
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    MovieImages = await client.GetMovieImagesAsync(selectedMovie.Id);
                }
                else if (selectedMovie.ShowType == ShowType.TvShow)
                {
                    MovieImages = await client.GetTvShowImagesAsync(selectedMovie.Id);
                }

                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        movieDetailsPhotosPage.BackDropsImageCounter.Text = MovieImages.Backdrops.Count + " images";
                        movieDetailsPhotosPage.PosterImageCounter.Text = MovieImages.Posters.Count + " images";
                    }));


                PhotoDetailsBackdrop.Clear();
                foreach (var image in MovieImages.Backdrops)
                {
                    using (FastObservableCollection<PhotoDetail> iDelayed = PhotoDetailsBackdrop.DelayNotifications())
                    {
                        PhotoDetail mov = new PhotoDetail();
                        var url = client.GetImageUrl("w500", image.FilePath);
                        mov.Poster = (url.AbsoluteUri);

                        iDelayed.Add(mov);
                    }
                }


                PhotoDetailsPoster.Clear();
                foreach (var image in MovieImages.Posters)
                {
                    using (FastObservableCollection<PhotoDetail> iDelayed = PhotoDetailsPoster.DelayNotifications())
                    {
                        PhotoDetail mov = new PhotoDetail();
                        var url = client.GetImageUrl("w500", image.FilePath);
                        mov.Poster = (url.AbsoluteUri);

                        iDelayed.Add(mov);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get movie photos: " + e.Message);
            }

        }

        public static async Task GetVideos(Movie selectedMovie,MovieDetailsVideoPage movieDetailsVideoPage)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                VideoDetails.Clear();
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    var movieVideos = await client.GetMovieVideosAsync(selectedMovie.Id);

                    await Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            movieDetailsVideoPage.VideosCount.Text = movieVideos.Results.Count + " Videos";
                        }));
                    foreach (var video in movieVideos.Results)
                    {
                        VideoDetail videoDetail = new VideoDetail()
                        {
                            Name = video.Name,
                            VideoType = video.Type
                        };
                        string videoUrl = "https://www.youtube.com/watch?v=";
                        videoUrl += TruncateLongString(video.Key, 11);
                    
                        videoDetail.VideoLink = videoUrl;

                        string thumbnailUrl = $"https://img.youtube.com/vi/{video.Key}/hqdefault.jpg";

                        if (!String.IsNullOrWhiteSpace(thumbnailUrl))
                        {
                            videoDetail.Poster =  (thumbnailUrl);
                            VideoDetails.Add(videoDetail);
                        }
                    }
                }
                else if (selectedMovie.ShowType == ShowType.TvShow)
                {
                    var movieVideos = await client.GetTvShowVideosAsync(selectedMovie.Id);
                    await Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            movieDetailsVideoPage.VideosCount.Text = movieVideos.Results.Count + " Videos";
                        }));
                    foreach (var video in movieVideos.Results)
                    {
                        VideoDetail videoDetail = new VideoDetail()
                        {
                            Name = video.Name,
                            VideoType = video.Type
                        };
                        string videoUrl = "https://www.youtube.com/watch?v=";
                        videoUrl += TruncateLongString(video.Key, 11);
                        ;
                        videoDetail.VideoLink = videoUrl;

                        string thumbnailUrl = $"https://img.youtube.com/vi/{video.Key}/hqdefault.jpg";

                        if (!String.IsNullOrWhiteSpace(thumbnailUrl))
                        {
                            videoDetail.Poster =  (thumbnailUrl);
                            VideoDetails.Add(videoDetail);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get videos: " + e.Message);
            }
        }


        //public static BitmapImage GetImageWithoutControl(string url)
        //{
        //    //using (HttpClient httpClient = new HttpClient())
        //    //{
        //    //    var stream = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        //    //    var responseStream = await stream.Content.ReadAsStreamAsync();
        //    //    var bitmapImage = new BitmapImage();

        //    //    using (var memoryStream = new MemoryStream())
        //    //    {
        //    //        await responseStream.CopyToAsync(memoryStream);
        //    //        memoryStream.Seek(0, SeekOrigin.Begin);

        //    //        bitmapImage.BeginInit();
        //    //        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //    //        bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        //    //        bitmapImage.StreamSource = memoryStream;
        //    //        bitmapImage.EndInit();
        //    //        bitmapImage.Freeze();
        //    //        return bitmapImage;
        //    //    }
        //    //}
        //    BitmapImage image = new BitmapImage(new Uri(url, UriKind.Absolute));
        //    image.CacheOption = BitmapCacheOption.OnLoad;
        //    return image;

        //}

        public static FastObservableCollection<Movie> Similars = new FastObservableCollection<Movie>();
        public static async Task GetSimilars( Movie selectedMovie)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                using (FastObservableCollection<Movie> iDelayed = Similars.DelayNotifications())
                {
                    Similars.Clear();
                    if (selectedMovie.ShowType == ShowType.Movie)
                    {
                        var similarMovies = await client.GetMovieRecommendationsAsync(selectedMovie.Id, language, 0);
                        foreach (var movie in similarMovies.Results)
                        {
                            Movie mov = new Movie()
                            {
                                Poster = (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                                Id = movie.Id,
                                Name = movie.Title,
                                Rating = movie.VoteAverage,
                                ShowType = ShowType.Movie,
                            };
                            if (iDelayed.Any(x => x.Id == movie.Id))
                            {

                            }
                            else
                            {
                                iDelayed.Add(mov);
                            }
                        }
                    }
                    else if (selectedMovie.ShowType == ShowType.TvShow)
                    {
                        var similarMovies = await client.GetTvShowSimilarAsync(selectedMovie.Id, language, 0);
                        foreach (var movie in similarMovies.Results)
                        {
                            Movie mov = new Movie()
                            {
                                Poster = (client.GetImageUrl("w500", movie.PosterPath).AbsoluteUri),
                                Id = movie.Id,
                                Name = movie.Name,
                                Rating = movie.VoteAverage,
                                ShowType = ShowType.TvShow,
                            };
                            if (iDelayed.Any(x => x.Id == movie.Id))
                            {

                            }
                            else
                            {
                                iDelayed.Add(mov);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get similars: " + e.Message);
            }

        }

        public static FastObservableCollection<Season> TvShowSeasons = new FastObservableCollection<Season>();
        public static FastObservableCollection<Episode> TvSeasonEpisodes = new FastObservableCollection<Episode>();
        public static async Task GetTvShowSeasons(int showId)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                TvShowSeasons.Clear();
                var tvShow = await client.GetTvShowAsync(showId, TvShowMethods.EpisodeGroups, language);

                if(tvShow.Seasons.Count == 0) return;
                foreach (var searchTvSeason in tvShow.Seasons)
                {
                    Season season = new Season()
                    {
                        Description = searchTvSeason.Overview.Length >= 400 ? searchTvSeason.Overview.Substring(0, 397) + "..." : searchTvSeason.Overview,
                        EpisodeCount = searchTvSeason.EpisodeCount.ToString(),
                        Poster =  (client.GetImageUrl("w500", searchTvSeason.PosterPath).AbsoluteUri),
                        SeasonNumber = searchTvSeason.SeasonNumber,
                        Year = searchTvSeason.AirDate.HasValue ? searchTvSeason.AirDate.Value.Year.ToString() : "",
                        SeasonNumberText = searchTvSeason.Name,
                        EpisodeCountText = searchTvSeason.EpisodeCount + " " + App.Current.Resources["EpisodesString2"]
                    };
                
                    TvShowSeasons.Add(season);
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get tv show seasons: " + e.Message);
            }
        }

        public static async Task GetSeasonEpisodes(int showId,int seasonNumber)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                TvSeasonEpisodes.Clear();
                var getSeason = await client.GetTvSeasonAsync(showId, seasonNumber,
                    TvSeasonMethods.Images, language);
                foreach (var tvSeasonEpisode in getSeason.Episodes)
                {
                    Episode episode = new Episode()
                    {
                        EpisodeNumber = tvSeasonEpisode.EpisodeNumber,
                        Date = tvSeasonEpisode.AirDate.HasValue ? tvSeasonEpisode.AirDate.Value.ToShortDateString() : "",
                        Description = tvSeasonEpisode.Overview.Length >= 260 ? tvSeasonEpisode.Overview.Substring(0,257)+"...": tvSeasonEpisode.Overview,
                        DurationTime = tvSeasonEpisode.Runtime.HasValue ? tvSeasonEpisode.Runtime.Value.ToString() : "",
                        Poster =  (client.GetImageUrl("w500", tvSeasonEpisode.StillPath).AbsoluteUri),
                        Rating = tvSeasonEpisode.VoteAverage,
                        EpisodeNumberText = tvSeasonEpisode.EpisodeNumber + ". " + App.Current.Resources["EpisodesString3"],
                        SeasonNumber = getSeason.SeasonNumber
                    };
                    TvSeasonEpisodes.Add(episode);
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get season episodes: " + e.Message);
            }
        }

        public static async Task<List<string>> GetLanguages()
        {
            List<string> result = new List<string>();

            foreach (var language in Languages)
            {  
                if(!String.IsNullOrWhiteSpace(language.EnglishName))
                    result.Add(language.EnglishName);
            }

            result = result.OrderBy(x => x).ToList();

            return result;
        }

        public static async Task<bool> Login(AccountLoginPage accountLoginPage,string username, string password)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var login = await client.AuthenticationGetUserSessionAsync(username, password);
                if (login.Success)
                {
                    await client.SetSessionInformationAsync(login.SessionId, SessionType.UserSession);
                    AppSettingsManager.appSettings.TmdbUsername = username;
                    AppSettingsManager.appSettings.TmdbPassword = password;
                    AppSettingsManager.SaveAppSettings();
                    return true;
                }
                else
                {
                    accountLoginPage.TextBlockError.Visibility = Visibility.Visible;
                    accountLoginPage.TextBlockError.Text = "Username or password was incorrect.";
                    return false;
                }
            }
            catch (Exception exception)
            {
                Log.Error("Error on login TMDB: " + exception.Message);
                accountLoginPage.TextBlockError.Visibility = Visibility.Visible;
                accountLoginPage.TextBlockError.Text = exception.Message;
                return false;
            }
        }

        public static async Task<bool> LogOut()
        {
            try
            {
                await client.SetSessionInformationAsync("", SessionType.Unassigned);
                AppSettingsManager.appSettings.TmdbUsername = "";
                AppSettingsManager.appSettings.TmdbPassword = "";
                AppSettingsManager.SaveAppSettings();
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return false;
            }
        }

        public static async Task<bool> Login(string username, string password)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var login = await client.AuthenticationGetUserSessionAsync(username, password);
                if (login.Success)
                {
                    await client.SetSessionInformationAsync(login.SessionId, SessionType.UserSession);
                    AppSettingsManager.appSettings.TmdbUsername = username;
                    AppSettingsManager.appSettings.TmdbPassword = password;
                    AppSettingsManager.SaveAppSettings();
                    return true;
                }
                else
                {
                    Log.Error("Couldnt login to TMDB");
                    return false;
                }
            }
            catch (TMDbException exception)
            {
                Log.Error("Login to TMDB failed: " + exception.Message);
                return false;
            }
        }
        public static FastObservableCollection<Movie> AccountFavoritesMovies = new FastObservableCollection<Movie>();
        public static int MaxFavoritesMoviePage;

        public static async Task GetFavoritesMovies(int page,AccountSortBy accountSortBy,SortOrder sortOrder)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                using (FastObservableCollection<Movie> iDelayed = AccountFavoritesMovies.DelayNotifications())
                {
                    AccountFavoritesMovies.Clear();
                    var favorites = await client.AccountGetFavoriteMoviesAsync(page, accountSortBy, sortOrder,
                        language);
                    MaxFavoritesMoviePage = favorites.TotalPages;
                    foreach (var favoritesResult in favorites.Results)
                    {
                        Movie movie = new Movie()
                        {
                            Id = favoritesResult.Id,
                            Name = favoritesResult.Title,
                            Poster = client.GetImageUrl("w500", favoritesResult.PosterPath).AbsoluteUri,
                            Rating = favoritesResult.VoteAverage,
                            ShowType = ShowType.Movie
                        };
                        iDelayed.Add(movie);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get favorite movies " + e.Message);
            }
        }

        public static async Task GetFavoritesMoviesMorePages(int page, AccountSortBy accountSortBy, SortOrder sortOrder)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                using (FastObservableCollection<Movie> iDelayed = AccountFavoritesMovies.DelayNotifications())
                {
                    var favorites = await client.AccountGetFavoriteMoviesAsync(page, accountSortBy, sortOrder,
                        language);

                    foreach (var favoritesResult in favorites.Results)
                    {
                        Movie movie = new Movie()
                        {
                            Id = favoritesResult.Id,
                            Name = favoritesResult.Title,
                            Poster = client.GetImageUrl("w500", favoritesResult.PosterPath).AbsoluteUri,
                            Rating = favoritesResult.VoteAverage,
                            ShowType = ShowType.Movie
                        };
                        iDelayed.Add(movie);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get favorite movies more pages: " + e.Message);
            }
        }
        public static int MaxFavoritesTvShowPage;

        public static FastObservableCollection<Movie> AccountFavoritesTvShows = new FastObservableCollection<Movie>();
        public static async Task GetFavoritesTvShows(int page, AccountSortBy accountSortBy, SortOrder sortOrder)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                using (FastObservableCollection<Movie> iDelayed = AccountFavoritesTvShows.DelayNotifications())
                {
                    AccountFavoritesTvShows.Clear();
                    var favorites = await client.AccountGetFavoriteTvAsync(page, accountSortBy, sortOrder,
                        language);
                    MaxFavoritesTvShowPage = favorites.TotalPages;
                    foreach (var favoritesResult in favorites.Results)
                    {
                        Movie movie = new Movie()
                        {
                            Id = favoritesResult.Id,
                            Name = favoritesResult.Name,
                            Poster = client.GetImageUrl("w500", favoritesResult.PosterPath).AbsoluteUri,
                            Rating = favoritesResult.VoteAverage,
                            ShowType = ShowType.TvShow
                        };
                        iDelayed.Add(movie);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get favorite tv shows: " + e.Message);
            }
        }
        public static async Task GetFavoritesTvShowsMorePages(int page, AccountSortBy accountSortBy, SortOrder sortOrder)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                using (FastObservableCollection<Movie> iDelayed = AccountFavoritesTvShows.DelayNotifications())
                {
                    var favorites = await client.AccountGetFavoriteTvAsync(page, accountSortBy, sortOrder,
                        language);

                    foreach (var favoritesResult in favorites.Results)
                    {
                        Movie movie = new Movie()
                        {
                            Id = favoritesResult.Id,
                            Name = favoritesResult.Name,
                            Poster = client.GetImageUrl("w500", favoritesResult.PosterPath).AbsoluteUri,
                            Rating = favoritesResult.VoteAverage,
                            ShowType = ShowType.Movie
                        };
                        iDelayed.Add(movie);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get favorite tv shows more pages: " + e.Message);
            }
        }

        public static FastObservableCollection<Movie> WatchListMovies = new FastObservableCollection<Movie>();
        public static FastObservableCollection<Movie> WatchListTvShows = new FastObservableCollection<Movie>();

        public static int MaxWatchListMoviesPage;
        public static int MaxWatchListTvPage;

        public static async Task GetWatchListMovies(int page,AccountSortBy accountSortBy,SortOrder sortOrder,bool clear)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                using (FastObservableCollection<Movie> iDelayed = WatchListMovies.DelayNotifications())
                {
                    var watchList = await client.AccountGetMovieWatchlistAsync(page, accountSortBy, sortOrder,
                        language);

                    if (clear)
                    {
                        WatchListMovies.Clear();
                        MaxWatchListMoviesPage = watchList.TotalPages;
                    }

                    foreach (var watchListResult in watchList.Results)
                    {
                        var movie = new Movie()
                        {
                            Id = watchListResult.Id,
                            Name = watchListResult.Title,
                            Poster = client.GetImageUrl("w500", watchListResult.PosterPath).AbsoluteUri,
                            Rating = watchListResult.VoteAverage,
                            ShowType = ShowType.Movie
                        };
                        iDelayed.Add(movie);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get watchlist movies: " + e.Message);
            }
        }

        public static async Task GetWatchListTv(int page, AccountSortBy accountSortBy, SortOrder sortOrder, bool clear)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                using (FastObservableCollection<Movie> iDelayed = WatchListTvShows.DelayNotifications())
                {
                    var watchList = await client.AccountGetTvWatchlistAsync(page, accountSortBy, sortOrder,
                        language);

                    if (clear)
                    {
                        WatchListTvShows.Clear();
                        MaxWatchListTvPage = watchList.TotalPages;
                    }

                    foreach (var watchListResult in watchList.Results)
                    {
                        var movie = new Movie()
                        {
                            Id = watchListResult.Id,
                            Name = watchListResult.Name,
                            Poster = client.GetImageUrl("w500", watchListResult.PosterPath).AbsoluteUri,
                            Rating = watchListResult.VoteAverage,
                            ShowType = ShowType.TvShow
                        };
                        iDelayed.Add(movie);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get watchlist tv shows: " + e.Message);
            }
        }


        public static List<TMDbLib.Objects.Languages.Language> Languages =
            new List<TMDbLib.Objects.Languages.Language>();
        public static async Task<List<TMDbLib.Objects.Languages.Language>> InitLanguages()
        {
            try
            {
                //if (File.Exists("languages.json"))
                //{
                //    Languages = JsonConvert.DeserializeObject<List<TMDbLib.Objects.Languages.Language>>(
                //        File.ReadAllText("languages.json"));
                //    Log.Information("Initialized languages.");
                //}
                //else
                //{
                    if (!client.HasConfig)
                    {
                        await client.GetConfigAsync();
                    }
                    Languages = await client.GetLanguagesAsync();
                    Log.Information("Initialized languages.");
                    return Languages;
                    //var js = JsonConvert.SerializeObject(Languages);
                   // await File.WriteAllTextAsync("languages.json", js);
                    
               // }
            }
            catch (Exception e)
            {
                Log.Error("Error on initializing languages: " + e.Message);
                return null;
            }
           
        }

        public static async Task<bool> Vote(int id, ShowType showType,double rating)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                if (client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId))
                {
                    if (showType == ShowType.Movie)
                    {
                        return await client.MovieSetRatingAsync(id, rating);
                    }
                    else
                    {
                        return await client.TvShowSetRatingAsync(id, rating);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on Vote: " + e.Message);
            }
            return false;
        }

        public static async Task<bool> RemoveVote(int id, ShowType showType)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                if (client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId))
                {
                    if (showType == ShowType.Movie)
                    {
                        return await client.MovieRemoveRatingAsync(id);
                    }
                    else
                    {
                        return await client.TvShowRemoveRatingAsync(id);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on remove vote: " + e.Message);
            }
            return false;
        }

    }

}
