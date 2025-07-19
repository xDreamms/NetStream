using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Serilog;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.TvShows;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for CastKnownForPage.xaml
    /// </summary>
    public partial class CastKnownForPage : Page
    {
        private Cast cast;
        public CastKnownForPage(Cast cast)
        {
            InitializeComponent();
            this.DataContext = this;
            this.cast = cast;
            Load();
        }

        private async void Load()
        {
            MoviesDisplay.ItemsSource = movies;
            await GetKnownForMovies();
        }

        private FastObservableCollection<Movie> movies = new FastObservableCollection<Movie>();


        public const int PageSize = 10;
        public int MoviesPageIndex = 0;
        public int TvShowsPageIndex = 0;
        public int MaxTvShowsPage = 0;
        public int MaxMoviesPage = 0;
        public bool isItemLoadingFinished = false;

        string language = Service.language;
        Person person;
        private List<MovieJob> directorMovies = null;
        private List<TvJob> directorTvShows = null;
        private List<MovieRole> castMovies = null;
        private List<TvRole> castTvShows = null;

        public async Task GetKnownForMovies()
        {
            try
            {
                isItemLoadingFinished = false;

                if (person == null)
                {
                    person = await Service.client.GetPersonAsync(cast.Id, language, PersonMethods.TvCredits | PersonMethods.MovieCredits);
                }

                if (person != null)
                {
                    var tasks = new List<Task>();

                    if (person.KnownForDepartment == "Directing")
                    {
                        // Movies
                        if (person.MovieCredits.Crew.Count > 0 && MoviesPageIndex <= MaxMoviesPage)
                        {
                            tasks.Add(LoadMoviesAsync(person.MovieCredits.Crew.Where(x => x.Job == "Director").ToList()));
                        }

                        // TvShows
                        if (person.TvCredits.Crew.Count > 0 && TvShowsPageIndex <= MaxTvShowsPage)
                        {
                            tasks.Add(LoadTvShowsAsync(person.TvCredits.Crew.Where(x => x.Job == "Director").ToList()));
                        }
                    }
                    else
                    {
                        // Movies
                        if (person.MovieCredits.Cast.Count > 0 && MoviesPageIndex <= MaxMoviesPage)
                        {
                            tasks.Add(LoadMoviesAsync(person.MovieCredits.Cast.ToList()));
                        }

                        // TvShows
                        if (person.TvCredits.Cast.Count > 0 && TvShowsPageIndex <= MaxTvShowsPage)
                        {
                            tasks.Add(LoadTvShowsAsync(person.TvCredits.Cast.ToList()));
                        }
                    }

                    // Wait for all tasks to complete
                    await Task.WhenAll(tasks);
                    isItemLoadingFinished = true;
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task LoadMoviesAsync(List<MovieJob> movieJobs)
        {
            try
            {
                if (directorMovies == null)
                {
                    directorMovies = movieJobs;
                    MaxMoviesPage = GetMaxPage(directorMovies.Count);
                }

                var pagedMovies = directorMovies.Skip(MoviesPageIndex * PageSize).Take(PageSize);

                foreach (var movieRole in pagedMovies)
                {
                    var mov = await Service.client.GetMovieAsync(movieRole.Id, language);
                    if (mov != null)
                    {
                        Movie movie = new Movie()
                        {
                            Poster = Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri,
                            Id = mov.Id,
                            Name = mov.Title,
                            Rating = mov.VoteAverage,
                            ShowType = ShowType.Movie,
                        };

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (!movies.Any(x => x.Id == movie.Id))
                            {
                                movies.Add(movie);
                            }
                        });
                    }
                }

                // Increment page index
                MoviesPageIndex++;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task LoadTvShowsAsync(List<TvJob> tvJobs)
        {
            try
            {
                if (directorTvShows == null)
                {
                    directorTvShows = tvJobs;
                    MaxTvShowsPage = GetMaxPage(directorTvShows.Count);
                }

                var pagedTvShows = directorTvShows.Skip(TvShowsPageIndex * PageSize).Take(PageSize);

                foreach (var tvRole in pagedTvShows)
                {
                    var mov = await Service.client.GetTvShowAsync(tvRole.Id, TvShowMethods.Images, language);
                    if (mov != null)
                    {
                        Movie movie = new Movie()
                        {
                            Poster = Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri,
                            Id = mov.Id,
                            Name = mov.Name,
                            Rating = mov.VoteAverage,
                            ShowType = ShowType.TvShow,
                        };

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (!movies.Any(x => x.Id == movie.Id))
                            {
                                movies.Add(movie);
                            }
                        });
                    }
                }

                // Increment page index
                TvShowsPageIndex++;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        private async Task LoadMoviesAsync(List<MovieRole> movieJobs)
        {
            try
            {
                if (castMovies == null)
                {
                    castMovies = movieJobs;
                    MaxMoviesPage = GetMaxPage(castMovies.Count);
                }

                var pagedMovies = castMovies.Skip(MoviesPageIndex * PageSize).Take(PageSize);

                foreach (var movieRole in pagedMovies)
                {
                    var mov = await Service.client.GetMovieAsync(movieRole.Id, language);
                    if (mov != null)
                    {
                        Movie movie = new Movie()
                        {
                            Poster = Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri,
                            Id = mov.Id,
                            Name = mov.Title,
                            Rating = mov.VoteAverage,
                            ShowType = ShowType.Movie,
                        };

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (!movies.Any(x => x.Id == movie.Id))
                            {
                                movies.Add(movie);
                            }
                        });
                    }
                }

                // Increment page index
                MoviesPageIndex++;
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task LoadTvShowsAsync(List<TvRole> tvJobs)
        {
            try
            {
                if (castTvShows == null)
                {
                    castTvShows = tvJobs;
                    MaxTvShowsPage = GetMaxPage(castTvShows.Count);
                }

                var pagedTvShows = castTvShows.Skip(TvShowsPageIndex * PageSize).Take(PageSize);

                foreach (var tvRole in pagedTvShows)
                {
                    var mov = await Service.client.GetTvShowAsync(tvRole.Id, TvShowMethods.Images, language);
                    if (mov != null)
                    {
                        Movie movie = new Movie()
                        {
                            Poster = Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri,
                            Id = mov.Id,
                            Name = mov.Name,
                            Rating = mov.VoteAverage,
                            ShowType = ShowType.TvShow,
                        };

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (!movies.Any(x => x.Id == movie.Id))
                            {
                                movies.Add(movie);
                            }
                        });
                    }
                }

                // Increment page index
                TvShowsPageIndex++;
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        public int GetMaxPage(int totalItems)
        {
            return (int)Math.Ceiling((double)totalItems / PageSize);
        }



        //public const int PageSize = 10; // Her sayfada gösterilecek öğe sayısı
        //public int MoviesPageIndex = 0; // Başlangıçta ilk sayfa
        //public int TvShowsPageIndex = 0; // Başlangıçta ilk sayfa
        //public int MaxTvShowsPage = 0; // Başlangıçta ilk sayfa
        //public int MaxMoviesPage = 0; // Başlangıçta ilk sayfa
        //public bool isItemLoadingFinished = false;
        //string language = Service.language;
        //Person person;
        //private List<MovieJob> directorMovies = null;
        //private List<TvJob> directorTvShows = null;
        //private List<MovieRole> castMovies = null;
        //private List<TvRole> castTvShows = null;
        //public async Task GetKnownForMovies()
        //{
        //    isItemLoadingFinished = false;
        //    if (person == null)
        //    {
        //        person = await Service.client.GetPersonAsync(cast.Id, language, PersonMethods.TvCredits | PersonMethods.MovieCredits);
        //    }

        //    var tasks = new List<Task>();
        //    if (person != null)
        //    {
        //        if (person.KnownForDepartment == "Directing")
        //        {
        //            if (person.MovieCredits.Crew.Count > 0 && MoviesPageIndex <= MaxMoviesPage)
        //            {
        //                tasks.Add(Task.Run(async () =>
        //                {
        //                    if (directorMovies == null)
        //                    {
        //                        directorMovies = person.MovieCredits.Crew.Where(x => x.Job == "Director").ToList();
        //                        MaxMoviesPage = GetMaxPage(directorMovies.Count());
        //                    }

        //                    var pagedMovies = directorMovies.Skip(MoviesPageIndex * PageSize).Take(PageSize);

        //                    foreach (var movieRole in pagedMovies)
        //                    {
        //                        var mov = await Service.client.GetMovieAsync(movieRole.Id, language);
        //                        if (mov != null)
        //                        {
        //                            Movie movie = new Movie()
        //                            {
        //                                Poster = Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri,
        //                                Id = mov.Id,
        //                                Name = mov.Title,
        //                                Rating = mov.VoteAverage,
        //                                ShowType = ShowType.Movie,
        //                            };

        //                            await Application.Current.Dispatcher.InvokeAsync(() =>
        //                            {
        //                                if (!movies.Any(x => x.Id == movie.Id))
        //                                {
        //                                    movies.Add(movie);
        //                                }
        //                            });
        //                        }
        //                    }

        //                    // Sayfa index'ini artırıyoruz
        //                    MoviesPageIndex++;
        //                }));
        //            }

        //            // TvCredits Crew
        //            if (person.TvCredits.Crew.Count > 0 && TvShowsPageIndex <= MaxTvShowsPage)
        //            {
        //                tasks.Add(Task.Run(async () =>
        //                {
        //                    if (directorTvShows == null)
        //                    {
        //                        directorTvShows = person.TvCredits.Crew.Where(x => x.Job == "Director").ToList();
        //                        MaxTvShowsPage = GetMaxPage(directorTvShows.Count());
        //                    }

        //                    var pagedTvShows = directorTvShows.Skip(TvShowsPageIndex * PageSize).Take(PageSize);

        //                    foreach (var tvRole in pagedTvShows)
        //                    {
        //                        var mov = await Service.client.GetTvShowAsync(tvRole.Id, TvShowMethods.Images, language);
        //                        if (mov != null)
        //                        {
        //                            Movie movie = new Movie()
        //                            {
        //                                Poster = Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri,
        //                                Id = mov.Id,
        //                                Name = mov.Name,
        //                                Rating = mov.VoteAverage,
        //                                ShowType = ShowType.TvShow,
        //                            };

        //                            await Application.Current.Dispatcher.InvokeAsync(() =>
        //                            {
        //                                if (!movies.Any(x => x.Id == movie.Id))
        //                                {
        //                                    movies.Add(movie);
        //                                }
        //                            });
        //                        }
        //                    }

        //                    // Sayfa index'ini artırıyoruz
        //                    TvShowsPageIndex++;
        //                }));
        //            }
        //        }
        //        else
        //        {
        //            if (person.MovieCredits.Cast.Count > 0 && MoviesPageIndex <= MaxMoviesPage)
        //            {
        //                tasks.Add(Task.Run(async () =>
        //                {
        //                    if (castMovies == null)
        //                    {
        //                        castMovies = person.MovieCredits.Cast;
        //                        MaxMoviesPage = GetMaxPage(castMovies.Count());
        //                    }

        //                    var pagedCastMovies = castMovies.Skip(MoviesPageIndex * PageSize).Take(PageSize);

        //                    foreach (var movieRole in pagedCastMovies)
        //                    {
        //                        var mov = await Service.client.GetMovieAsync(movieRole.Id, language);
        //                        if (mov != null)
        //                        {
        //                            Movie movie = new Movie()
        //                            {
        //                                Poster = Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri,
        //                                Id = mov.Id,
        //                                Name = mov.Title,
        //                                Rating = mov.VoteAverage,
        //                                ShowType = ShowType.Movie,
        //                            };

        //                            await Application.Current.Dispatcher.InvokeAsync(() =>
        //                            {
        //                                if (!movies.Any(x => x.Id == movie.Id))
        //                                {
        //                                    movies.Add(movie);
        //                                }
        //                            });
        //                        }
        //                    }

        //                    // Sayfa index'ini artırıyoruz
        //                    MoviesPageIndex++;
        //                }));
        //            }

        //            // TvCredits Cast
        //            if (person.TvCredits.Cast.Count > 0 && TvShowsPageIndex <= MaxTvShowsPage)
        //            {
        //                tasks.Add(Task.Run(async () =>
        //                {
        //                    if (castTvShows == null)
        //                    {
        //                        castTvShows = person.TvCredits.Cast;
        //                        MaxTvShowsPage = GetMaxPage(castTvShows.Count());
        //                    }
        //                    var pagedCastTvShows = castTvShows.Skip(TvShowsPageIndex * PageSize).Take(PageSize);

        //                    foreach (var tvRole in pagedCastTvShows)
        //                    {
        //                        var mov = await Service.client.GetTvShowAsync(tvRole.Id, TvShowMethods.Images, language);
        //                        if (mov != null)
        //                        {
        //                            Movie movie = new Movie()
        //                            {
        //                                Poster = Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri,
        //                                Id = mov.Id,
        //                                Name = mov.Name,
        //                                Rating = mov.VoteAverage,
        //                                ShowType = ShowType.TvShow,
        //                            };

        //                            await Application.Current.Dispatcher.InvokeAsync(() =>
        //                            {
        //                                if (!movies.Any(x => x.Id == movie.Id))
        //                                {
        //                                    movies.Add(movie);
        //                                }
        //                            });
        //                        }
        //                    }

        //                    // Sayfa index'ini artırıyoruz
        //                    TvShowsPageIndex++;
        //                }));
        //            }
        //        }

        //        // Tüm görevlerin tamamlanmasını bekliyoruz
        //        await Task.WhenAll(tasks);
        //        isItemLoadingFinished = true;

        //    }
        //}

        //public int GetMaxPage(int totalItems)
        //{
        //    int totalPages = (int)Math.Ceiling((double)totalItems / PageSize);
        //    return totalPages;
        //}

        //private async Task GetKnownForMovies()
        //{
        //    var language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;

        //    var person = await Service.client.GetPersonAsync(cast.Id, language,
        //        PersonMethods.TvCredits | PersonMethods.MovieCredits);

        //    if (person != null)
        //    {
        //        if (person.KnownForDepartment == "Directing")
        //        {
        //            if (person.MovieCredits.Crew.Count > 0)
        //            {
        //                foreach (var movieRole in person.MovieCredits.Crew.Where(x=> x.Job == "Director"))
        //                {
        //                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
        //                    {
        //                        var mov = await Service.client.GetMovieAsync(movieRole.Id, language);
        //                        Movie movie = new Movie()
        //                        {
        //                            Poster = (
        //                                Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri),
        //                            Id = mov.Id,
        //                            Name = mov.Title,
        //                            Rating = mov.VoteAverage,
        //                            ShowType = ShowType.Movie,
        //                        };
        //                        if (!(movies.Any(x => x.Id == movie.Id)))
        //                        {
        //                            iDelayed.Add(movie);
        //                        }
        //                    }
        //                }
        //            }

        //            if (person.TvCredits.Crew.Count > 0)
        //            {
        //                foreach (var tvRole in person.TvCredits.Crew.Where(x => x.Job == "Director"))
        //                {
        //                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
        //                    {
        //                        var mov = await Service.client.GetTvShowAsync(tvRole.Id, TvShowMethods.Images,
        //                            language);
        //                        Movie movie = new Movie()
        //                        {
        //                            Poster = (
        //                                Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri),
        //                            Id = mov.Id,
        //                            Name = mov.Name,
        //                            Rating = mov.VoteAverage,
        //                            ShowType = ShowType.TvShow,
        //                        };

        //                        if (!(movies.Any(x => x.Id == movie.Id)))
        //                        {
        //                            iDelayed.Add(movie);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            if (person.MovieCredits.Cast.Count > 0)
        //            {
        //                foreach (var movieRole in person.MovieCredits.Cast)
        //                {
        //                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
        //                    {
        //                        var mov = await Service.client.GetMovieAsync(movieRole.Id, language);
        //                        Movie movie = new Movie()
        //                        {
        //                            Poster = (
        //                                Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri),
        //                            Id = mov.Id,
        //                            Name = mov.Title,
        //                            Rating = mov.VoteAverage,
        //                            ShowType = ShowType.Movie,
        //                        };

        //                        iDelayed.Add(movie);
        //                    }
        //                }
        //            }

        //            if (person.TvCredits.Cast.Count > 0)
        //            {

        //                foreach (var tvRole in person.TvCredits.Cast)
        //                {
        //                    using (FastObservableCollection<Movie> iDelayed = movies.DelayNotifications())
        //                    {
        //                        var mov = await Service.client.GetTvShowAsync(tvRole.Id, TvShowMethods.Images,
        //                            language);
        //                        Movie movie = new Movie()
        //                        {
        //                            Poster = (
        //                                Service.client.GetImageUrl("w500", mov.PosterPath).AbsoluteUri),
        //                            Id = mov.Id,
        //                            Name = mov.Name,
        //                            Rating = mov.VoteAverage,
        //                            ShowType = ShowType.TvShow,
        //                        };

        //                        iDelayed.Add(movie);
        //                    }
        //                }
        //            }
        //        }

        //    }

        //}


        private async void CastKnownForPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void MoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }

        private void MoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedMovie = MoviesDisplay.SelectedItem as Movie;
                if (selectedMovie != null)
                {
                    var movieDetailsPage = new MovieDetailsPage(selectedMovie);
                    HomePage.GetHomePageInstance.HomePageNavigation.Navigate(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void Image_OnImageFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            var image = sender as Image;
            if (image.DataContext is Movie)
            {
                image.Source = new BitmapImage(new Uri("pack://application:,,,/NetStream;component/Placeholder.jpg"));
                Movie s = (Movie)image.DataContext;
                s.Poster = (new Uri("pack://application:,,,/NetStream;component/Placeholder.jpg").AbsoluteUri);
            }
           
        }

        private void CastKnownForPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (movies != null)
                movies.Clear();

            // Clear any cached data
            directorMovies = null;
            directorTvShows = null;
            castMovies = null;
            castTvShows = null;
            person = null;
        }
    }
}
