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
using Avalonia;
using Avalonia.Threading;
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
        public static TMDbClient client;
        
        static Service()
        {
            var tmdbApiKey = NetStreamEnvironment.GetRequiredString("NETSTREAM_TMDB_API_KEY", "TMDB API key");
            client = new TMDbClient(tmdbApiKey);
            client.Timeout = TimeSpan.FromSeconds(15);
        }

        public static ObservableCollection<Movie> SearchedList = new ObservableCollection<Movie>();

        public static ObservableCollection<Movie> PopularMovies = new ObservableCollection<Movie>();
        public static ObservableCollection<Movie> TopRatedMovies = new ObservableCollection<Movie>();
        public static ObservableCollection<Movie> UpComingMovies = new ObservableCollection<Movie>();
        public static ObservableCollection<Movie> NowPlayingMovies = new ObservableCollection<Movie>();

        public static ObservableCollection<Movie> PopularTvShows = new ObservableCollection<Movie>();
        public static ObservableCollection<Movie> TopRatedTvShows = new ObservableCollection<Movie>();
        public static ObservableCollection<Movie> AiringTodayTvShows = new ObservableCollection<Movie>();

        public static ObservableCollection<Cast> MovieCasts = new ObservableCollection<Cast>();
        public static ObservableCollection<Cast> TvShowCasts = new ObservableCollection<Cast>();


        public static MainMovie MainMovieee;
        public static MainMovie MainTvshoww;

        public static string language;

       
        public static async Task GetPopularTvShows(int page,string language)
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
        
       

        public static ObservableCollection<Movie> SearchedMoviesResult = new ObservableCollection<Movie>();

        public static async Task GetSearchedMovies(string query, int page, SearchPageResultsControl searchPageResults)
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
                            SearchedMoviesResult.Add(mov);
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
                            SearchedMoviesResult.Add(mov);
                        }
                    
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching searched more page movies: " + e.Message);
            }
        }

        public static ObservableCollection<Movie> SearchedTvShowsResult = new ObservableCollection<Movie>();

        public static async Task GetSearchedTvShows(string query,  int page, SearchPageResultsControl searchPageResults)
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
                            SearchedTvShowsResult.Add(tv);
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
                            SearchedTvShowsResult.Add(tv);
                        }
                    
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on fetching searched tv shows more page: " + e.Message);
            }
        }

        public static ObservableCollection<Cast> SearchedCastsResult = new ObservableCollection<Cast>();
        public static async Task GetSearchedCasts(string query,  int page,SearchPageResultsControl searchPageResults)
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
                   
                        var person = new Cast()
                            { Id = x.Id, Name = x.Name, Poster = client.GetImageUrl("w500", x.ProfilePath).AbsoluteUri };
                        if (SearchedCastsResult.Any(z => z.Id == x.Id))
                        {
                        }
                        else
                        {
                            SearchedCastsResult.Add(person);
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
                    
                        var person = new Cast()
                            { Id = x.Id, Name = x.Name, Poster = client.GetImageUrl("w500", x.ProfilePath).AbsoluteUri };
                        if (SearchedCastsResult.Any(z => z.Id == x.Id))
                        {
                        }
                        else
                        {
                            SearchedCastsResult.Add(person);
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

        public static ObservableCollection<Movie> DiscoveredMovies = new ObservableCollection<Movie>();

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
                            DiscoveredMovies.Add(movie);
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
                            DiscoveredMovies.Add(movie);
                        }
                    
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on discover movies more pages: " + e.Message);
            }
        }


        public static ObservableCollection<Movie> DiscoveredTvShows = new ObservableCollection<Movie>();

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
                            DiscoveredTvShows.Add(tv);
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
                            DiscoveredTvShows.Add(tv);
                        }
                    }
                
            }
            catch (Exception e)
            {
                Log.Error("Error on discover tv shows more pages: " + e.Message);
            }
        }


        public static async Task<ObservableCollection<Genre>> GetMovieGenres()
        {
            ObservableCollection<Genre> result = new ObservableCollection<Genre>();
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var genres = await client.GetMovieGenresAsync(language);

                foreach (var genre in genres)
                {
                   
                    result.Add(new Genre()
                        {
                            Id = genre.Id,
                            Name = genre.Name,
                            IsSelected = false

                        });
                    
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get movie genres: " + e.Message);
            }
            return result;
        }

        public static async Task<ObservableCollection<Genre>> GetTvGenres()
        {
            ObservableCollection<Genre> result = new ObservableCollection<Genre>();
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                var genres = await client.GetTvGenresAsync(language);

                foreach (var genre in genres)
                {
                   
                    result.Add(new Genre()
                        {
                            Id = genre.Id,
                            Name = genre.Name,
                            IsSelected = false

                        });
                    
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
                        Duration = mainMovie.Runtime.ToString() + " " + ResourceProvider.GetString("MinString"),
                        Overview = mainMovie.Overview,
                        Rating = mainMovie.VoteAverage,
                        ReleaseYear = mainMovie.ReleaseDate.Value.Year.ToString(),
                        ReviewCount = mainMovie.VoteCount.ToString() + " " + ResourceProvider.GetString("ReviewsString"),
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
                        Duration = mainMovie.Runtime.ToString() + " " + ResourceProvider.GetString("MinString"),
                        Overview = mainMovie.Overview,
                        Rating = mainMovie.VoteAverage,
                        ReleaseYear = mainMovie.ReleaseDate.Value.Year.ToString(),
                        ReviewCount = mainMovie.VoteCount.ToString() +" " + ResourceProvider.GetString("ReviewsString"),
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
                        Duration = mainTvShow.EpisodeRunTime.OrderByDescending(x => x).FirstOrDefault() + " " + ResourceProvider.GetString("MinString"),
                        Overview = mainTvShow.Overview,
                        Rating = mainTvShow.VoteAverage,
                        ReleaseYear = mainTvShow.FirstAirDate.Value.Year.ToString(),
                        ReviewCount = mainTvShow.VoteCount.ToString() + " " + ResourceProvider.GetString("ReviewsString"),
                        Id = mainTvShow.Id,
                        ShowType = ShowType.TvShow
                    };
                    MainTvshoww = MainMovieMov;
                }
                else
                {
                    await GetPopularTvShows(1, language);
                    var mainPopularTvShow = Service.PopularTvShows.FirstOrDefault();

                    var mainTvShow = await client.GetTvShowAsync(mainPopularTvShow.Id, TvShowMethods.Undefined, language);

                    var MainMovieMov = new MainMovie()
                    {
                        Poster =  (client.GetImageUrl(client.Config.Images.BackdropSizes[client.Config.Images.BackdropSizes.Count - 2], mainTvShow.BackdropPath).AbsoluteUri),
                        Name = mainTvShow.Name,
                        Duration = mainTvShow.EpisodeRunTime.OrderByDescending(x => x).FirstOrDefault() + " " + ResourceProvider.GetString("MinString"),
                        Overview = mainTvShow.Overview,
                        Rating = mainTvShow.VoteAverage,
                        ReleaseYear = mainTvShow.FirstAirDate.Value.Year.ToString(),
                        ReviewCount = mainTvShow.VoteCount.ToString() + " " + ResourceProvider.GetString("ReviewsString"),
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
                return $"{minutes % 60}{ResourceProvider.GetString("MinString")}";
            }
            else
            {
                return $"{minutes / 60}{ResourceProvider.GetString("HourString")} {minutes % 60}{ResourceProvider.GetString("MinString")}";
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
                    Log.Information("1");
                    var mainMovie = await client.GetMovieAsync(selectedMovie.Id, language,null, client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) ? MovieMethods.AccountStates : MovieMethods.Undefined);
                    Log.Information("2");
                    AccountState accountState = null;
                    if (client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates == null)
                    {
                        Log.Information("3");
                        accountState = await client.GetMovieAccountStateAsync(mainMovie.Id);
                        Log.Information("4");
                    }
                    Log.Information("5");
                    var MainMovieMov = new MainMovie();
                    Log.Information("6");
                    MainMovieMov.Poster = (client
                        .GetImageUrl(client.Config.Images.BackdropSizes[client.Config.Images.BackdropSizes.Count - 2],
                            mainMovie.BackdropPath).AbsoluteUri);
                    Log.Information("7");
                    MainMovieMov. Name = mainMovie.Title;
                    Log.Information("8");
                    MainMovieMov. Duration = mainMovie.Runtime.HasValue ? GetRuntime(mainMovie.Runtime.Value) : "";
                    Log.Information("9");
                    MainMovieMov.Overview = mainMovie.Overview;
                    Log.Information("10");
                    MainMovieMov. Rating = mainMovie.VoteAverage;
                    Log.Information("11");
                    MainMovieMov. ReleaseYear = mainMovie.ReleaseDate.HasValue ? mainMovie.ReleaseDate.Value.Year.ToString() : "";
                    Log.Information("12");
                    MainMovieMov. ReviewCount = mainMovie.VoteCount + " " + ResourceProvider.GetString("ReviewsString");
                    Log.Information("13");
                    MainMovieMov. IsFavorite = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Favorite : (accountState != null ? accountState.Favorite : false);
                    Log.Information("14");
                    MainMovieMov. IsInWatchlist = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Watchlist: (accountState != null ? accountState.Watchlist : false);
                    Log.Information("15");
                    MainMovieMov. MyRating = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Rating : (accountState != null ? accountState.Rating : null);
                    Log.Information("16");
                   
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
                        ReviewCount = mainMovie.VoteCount + " " + ResourceProvider.GetString("ReviewsString"),
                        IsFavorite = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Favorite : (accountState != null ? accountState.Favorite : false),
                        IsInWatchlist = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Watchlist : (accountState != null ? accountState.Watchlist : false),
                        MyRating = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId) && mainMovie.AccountStates != null ? mainMovie.AccountStates.Rating : (accountState != null ? accountState.Rating : null),
                    };
                    return MainMovieMov;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get main movie detail: " + e.Message +" " + e.StackTrace);
            }

            return null;
        }

        public static ObservableCollection<PhotoDetail> PhotoDetailsBackdrop =
            new ObservableCollection<PhotoDetail>();

        public static ObservableCollection<PhotoDetail> PhotoDetailsPoster =
            new ObservableCollection<PhotoDetail>();

        public static ObservableCollection<VideoDetail> VideoDetails = new ObservableCollection<VideoDetail>();
        /*public static async Task GetMoviePhotos(Movie selectedMovie, MovieDetailsPhotosPage movieDetailsPhotosPage)
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

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    movieDetailsPhotosPage.BackDropsImageCounter.Text = MovieImages.Backdrops.Count + " images";
                    movieDetailsPhotosPage.PosterImageCounter.Text = MovieImages.Posters.Count + " images";
                });


                PhotoDetailsBackdrop.Clear();
                foreach (var image in MovieImages.Backdrops)
                {
                    using (ObservableCollection<PhotoDetail> iDelayed = PhotoDetailsBackdrop.DelayNotifications())
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
                    using (ObservableCollection<PhotoDetail> iDelayed = PhotoDetailsPoster.DelayNotifications())
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
                Log.Error(e, "Couldn't get photos");
            }
        }*/

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

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        movieDetailsVideoPage.VideosCount.Text = movieVideos.Results.Count + " Videos";
                    });
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
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        movieDetailsVideoPage.VideosCount.Text = movieVideos.Results.Count + " Videos";
                    });
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

        public static ObservableCollection<Movie> Similars = new ObservableCollection<Movie>();
        public static async Task GetSimilars( Movie selectedMovie)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                
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
                            if (Similars.Any(x => x.Id == movie.Id))
                            {

                            }
                            else
                            {
                                Similars.Add(mov);
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
                            if (Similars.Any(x => x.Id == movie.Id))
                            {

                            }
                            else
                            {
                                Similars.Add(mov);
                            }
                        }
                    }
                
            }
            catch (Exception e)
            {
                Log.Error("Error on get similars: " + e.Message);
            }

        }

        public static ObservableCollection<Season> TvShowSeasons = new ObservableCollection<Season>();
        public static ObservableCollection<Episode> TvSeasonEpisodes = new ObservableCollection<Episode>();
        public static async Task GetTvShowSeasons(int showId)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                TvShowSeasons.Clear();
                var tvShow = await client.GetTvShowAsync(showId, TvShowMethods.Undefined, language);

                if(tvShow.Seasons == null || tvShow.Seasons.Count == 0) return;
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
                        EpisodeCountText = searchTvSeason.EpisodeCount + " " + ResourceProvider.GetString("EpisodesString2")
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
                if (getSeason?.Episodes == null) return;
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
                        EpisodeNumberText = tvSeasonEpisode.EpisodeNumber + ". " + ResourceProvider.GetString("EpisodesString3"),
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

        /*public static async Task<bool> Login(AccountLoginPage accountLoginPage,string username, string password)
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
                    //accountLoginPage.TextBlockError.Visibility = Visibility.Visible;
                    accountLoginPage.TextBlockError.Text = "Username or password was incorrect.";
                    return false;
                }
            }
            catch (Exception exception)
            {
                Log.Error("Error on login TMDB: " + exception.Message);
                //accountLoginPage.TextBlockError.Visibility = Visibility.Visible;
                accountLoginPage.TextBlockError.Text = exception.Message;
                return false;
            }
        }*/

        public static async Task<bool> LogOut()
        {
            try
            {
                await client.SetSessionInformationAsync("", SessionType.Unassigned);
                AppSettingsManager.appSettings.TmdbUsername = "";
                AppSettingsManager.appSettings.TmdbPassword = "";
                AppSettingsManager.SaveAppSettings();
                
                // Clear credentials in Firestore
                await FirestoreManager.UpdateTmdbCredentials("", "");
                
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
                    
                    // Update credentials in Firestore
                    await FirestoreManager.UpdateTmdbCredentials(username, password);
                    
                    return true;
                }
                else
                {
                    Console.WriteLine("Couldnt login to TMDB");
                    return false;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Login to TMDB failed: " + exception.Message);
                return false;
            }
        }
        public static ObservableCollection<Movie> AccountFavoritesMovies = new ObservableCollection<Movie>();
        public static int MaxFavoritesMoviePage;

        public static async Task GetFavoritesMovies(int page,AccountSortBy accountSortBy,SortOrder sortOrder)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
               
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
                        AccountFavoritesMovies.Add(movie);
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
                        AccountFavoritesMovies.Add(movie);
                    }
                
            }
            catch (Exception e)
            {
                Log.Error("Error on get favorite movies more pages: " + e.Message);
            }
        }
        public static int MaxFavoritesTvShowPage;

        public static ObservableCollection<Movie> AccountFavoritesTvShows = new ObservableCollection<Movie>();
        public static async Task GetFavoritesTvShows(int page, AccountSortBy accountSortBy, SortOrder sortOrder)
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }
                
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
                        AccountFavoritesTvShows.Add(movie);
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
                        AccountFavoritesTvShows.Add(movie);
                    }
                
            }
            catch (Exception e)
            {
                Log.Error("Error on get favorite tv shows more pages: " + e.Message);
            }
        }

        public static ObservableCollection<Movie> WatchListMovies = new ObservableCollection<Movie>();
        public static ObservableCollection<Movie> WatchListTvShows = new ObservableCollection<Movie>();

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
                        WatchListMovies.Add(movie);
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
                        WatchListTvShows.Add(movie);
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
                    if (!client.HasConfig)
                    {
                        await client.GetConfigAsync();
                    }
                    Languages = await client.GetLanguagesAsync();
                    Log.Information("Initialized languages.");
                    return Languages;
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

        // Recommendation collections
        public static ObservableCollection<Movie> RecommendedMovies = new ObservableCollection<Movie>();
        public static ObservableCollection<Movie> RecommendedTvShows = new ObservableCollection<Movie>();

        public static async Task GetRecommendations()
        {
            try
            {
                if (!client.HasConfig)
                {
                    await client.GetConfigAsync();
                }

                RecommendedMovies.Clear();
                RecommendedTvShows.Clear();

                var addedMovieIds = new HashSet<int>();
                var addedTvIds = new HashSet<int>();
                var sourceMovieIds = new List<int>();
                var sourceTvIds = new List<int>();

                bool hasAccount = client.ActiveAccount != null && !String.IsNullOrWhiteSpace(client.SessionId);

                // 1. TMDB hesabi varsa: once yuksek puan verdiklerinden, sonra favorilerden oneri al
                if (hasAccount)
                {
                    // Yuksek puan verilen filmler (rating'e gore sirali, en yuksek once)
                    try
                    {
                        var ratedMovies = await client.AccountGetRatedMoviesAsync(1, AccountSortBy.CreatedAt, SortOrder.Descending, language);
                        if (ratedMovies != null && ratedMovies.Results != null)
                        {
                            // En yuksek puan verilenleri once al
                            var sortedByRating = ratedMovies.Results.OrderByDescending(r => r.Rating).ToList();
                            foreach (var rated in sortedByRating.Take(8))
                            {
                                sourceMovieIds.Add(rated.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error getting rated movies: {ex.Message}");
                    }

                    // Yuksek puan verilen diziler
                    try
                    {
                        var ratedTv = await client.AccountGetRatedTvShowsAsync(1, AccountSortBy.CreatedAt, SortOrder.Descending, language);
                        if (ratedTv != null && ratedTv.Results != null)
                        {
                            var sortedByRating = ratedTv.Results.OrderByDescending(r => r.Rating).ToList();
                            foreach (var rated in sortedByRating.Take(8))
                            {
                                sourceTvIds.Add(rated.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error getting rated tv shows: {ex.Message}");
                    }

                    // Favori filmler
                    try
                    {
                        var favMovies = await client.AccountGetFavoriteMoviesAsync(1, AccountSortBy.CreatedAt, SortOrder.Descending, language);
                        if (favMovies != null && favMovies.Results != null)
                        {
                            foreach (var fav in favMovies.Results)
                            {
                                if (!sourceMovieIds.Contains(fav.Id))
                                    sourceMovieIds.Add(fav.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error getting favorite movies: {ex.Message}");
                    }

                    // Favori diziler
                    try
                    {
                        var favTv = await client.AccountGetFavoriteTvAsync(1, AccountSortBy.CreatedAt, SortOrder.Descending, language);
                        if (favTv != null && favTv.Results != null)
                        {
                            foreach (var fav in favTv.Results)
                            {
                                if (!sourceTvIds.Contains(fav.Id))
                                    sourceTvIds.Add(fav.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error getting favorite tv shows: {ex.Message}");
                    }
                }

                // 2. Hesap yoksa veya yeterli kaynak bulunamadiysa, izleme gecmisinden al
                if (sourceMovieIds.Count < 3 || sourceTvIds.Count < 3)
                {
                    try
                    {
                        var watchHistoryResult = await FirestoreManager.GetWatchHistory();
                        if (watchHistoryResult != null && watchHistoryResult.WatchHistories != null)
                        {
                            foreach (var wh in watchHistoryResult.WatchHistories)
                            {
                                if (wh.ShowType == ShowType.Movie && !sourceMovieIds.Contains(wh.Id))
                                    sourceMovieIds.Add(wh.Id);
                                else if (wh.ShowType == ShowType.TvShow && !sourceTvIds.Contains(wh.Id))
                                    sourceTvIds.Add(wh.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error getting watch history for recommendations: {ex.Message}");
                    }
                }

                // Kaynak ID'lerini exclude olarak kullan (zaten izlenmis/begenilmis)
                var excludeIds = new HashSet<int>(sourceMovieIds);
                foreach (var id in sourceTvIds) excludeIds.Add(id);

                // 3. Film onerilerini al (max 8 kaynak)
                foreach (var movieId in sourceMovieIds.Take(8))
                {
                    try
                    {
                        var recommendations = await client.GetMovieRecommendationsAsync(movieId, language, 1);
                        foreach (var rec in recommendations.Results.Take(4))
                        {
                            if (!addedMovieIds.Contains(rec.Id) && !excludeIds.Contains(rec.Id))
                            {
                                addedMovieIds.Add(rec.Id);
                                var mov = new Movie()
                                {
                                    Poster = client.GetImageUrl("w500", rec.PosterPath).AbsoluteUri,
                                    Name = rec.Title,
                                    Id = rec.Id,
                                    ShowType = ShowType.Movie,
                                    Rating = rec.VoteAverage
                                };
                                await Dispatcher.UIThread.InvokeAsync(() => RecommendedMovies.Add(mov));
                            }
                            if (RecommendedMovies.Count >= 30) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error getting movie recommendations for {movieId}: {ex.Message}");
                    }
                    if (RecommendedMovies.Count >= 30) break;
                }

                // 4. Dizi onerilerini al (max 8 kaynak)
                foreach (var tvId in sourceTvIds.Take(8))
                {
                    try
                    {
                        var recommendations = await client.GetTvShowRecommendationsAsync(tvId, language, 1);
                        foreach (var rec in recommendations.Results.Take(4))
                        {
                            if (!addedTvIds.Contains(rec.Id) && !excludeIds.Contains(rec.Id))
                            {
                                addedTvIds.Add(rec.Id);
                                var mov = new Movie()
                                {
                                    Poster = client.GetImageUrl("w500", rec.PosterPath).AbsoluteUri,
                                    Name = rec.Name,
                                    Id = rec.Id,
                                    ShowType = ShowType.TvShow,
                                    Rating = rec.VoteAverage
                                };
                                await Dispatcher.UIThread.InvokeAsync(() => RecommendedTvShows.Add(mov));
                            }
                            if (RecommendedTvShows.Count >= 30) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error getting tv recommendations for {tvId}: {ex.Message}");
                    }
                    if (RecommendedTvShows.Count >= 30) break;
                }

                // 5. Yeterli oneri yoksa trending ile doldur
                if (RecommendedMovies.Count < 5)
                {
                    try
                    {
                        var trending = await client.GetTrendingMoviesAsync(TMDbLib.Objects.Trending.TimeWindow.Week, 1, language);
                        foreach (var rec in trending.Results)
                        {
                            if (!addedMovieIds.Contains(rec.Id))
                            {
                                addedMovieIds.Add(rec.Id);
                                var mov = new Movie()
                                {
                                    Poster = client.GetImageUrl("w500", rec.PosterPath).AbsoluteUri,
                                    Name = rec.Title,
                                    Id = rec.Id,
                                    ShowType = ShowType.Movie,
                                    Rating = rec.VoteAverage
                                };
                                await Dispatcher.UIThread.InvokeAsync(() => RecommendedMovies.Add(mov));
                            }
                            if (RecommendedMovies.Count >= 20) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error getting trending movies: {ex.Message}");
                    }
                }

                if (RecommendedTvShows.Count < 5)
                {
                    try
                    {
                        var trending = await client.GetTrendingTvAsync(TMDbLib.Objects.Trending.TimeWindow.Week, 1, language);
                        foreach (var rec in trending.Results)
                        {
                            if (!addedTvIds.Contains(rec.Id))
                            {
                                addedTvIds.Add(rec.Id);
                                var mov = new Movie()
                                {
                                    Poster = client.GetImageUrl("w500", rec.PosterPath).AbsoluteUri,
                                    Name = rec.Name,
                                    Id = rec.Id,
                                    ShowType = ShowType.TvShow,
                                    Rating = rec.VoteAverage
                                };
                                await Dispatcher.UIThread.InvokeAsync(() => RecommendedTvShows.Add(mov));
                            }
                            if (RecommendedTvShows.Count >= 20) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error getting trending tv shows: {ex.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get recommendations: " + e.Message + " " + e.StackTrace);
            }
        }
    }

}
