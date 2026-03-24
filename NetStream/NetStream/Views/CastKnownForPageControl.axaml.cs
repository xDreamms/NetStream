using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetStream.Views;
using Serilog;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.TvShows;

namespace NetStream
{
    public partial class CastKnownForPageControl : UserControl, IDisposable
    {
        private Cast cast;
        private double currentWidth;
        
        public CastKnownForPageControl()
        {
            InitializeComponent();
        }

        public CastKnownForPageControl(Cast cast)
        {
            InitializeComponent();
            this.DataContext = this;
            this.cast = cast;
            Load();
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
        }
        
        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.width);
        }

        private DispatcherTimer timer;
        private async void Load()
        {
            MoviesDisplay.ItemsSource = movies;
            await GetKnownForMovies();
            ApplyResponsiveLayout(MainView.Instance.Bounds.Width);
            
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            
            timer.Tick += TimerOnTick;
            
            timer.Start();
        }

        private void TimerOnTick(object? sender, EventArgs e)
        {
            try
            {
                currentWidth = MainView.Instance.screenWidth;
                ApplyResponsiveLayout(currentWidth);
                
                timer.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Timer içinde responsive layout uygulama hatası: {ex.Message}");
            }
        }

        private ObservableCollection<Movie> movies = new ObservableCollection<Movie>();

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
                    
                    // Responsive düzeni yükleme işlemi bittikten sonra uygula
                    await Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        ApplyResponsiveLayout(currentWidth);
                    });
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

                        await Dispatcher.UIThread.InvokeAsync(() =>
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

                        await Dispatcher.UIThread.InvokeAsync(() =>
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

        private async Task LoadMoviesAsync(List<MovieRole> movieRoles)
        {
            try
            {
                if (castMovies == null)
                {
                    castMovies = movieRoles;
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

                        await Dispatcher.UIThread.InvokeAsync(() =>
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

        private async Task LoadTvShowsAsync(List<TvRole> tvRoles)
        {
            try
            {
                if (castTvShows == null)
                {
                    castTvShows = tvRoles;
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

                        await Dispatcher.UIThread.InvokeAsync(() =>
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

        private void CastKnownForPage_OnLoaded(object sender, RoutedEventArgs e)
        {
           
           
        }
        
        private void CastKnownForPage_OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            try
            {
                currentWidth = e.NewSize.Width;
                ApplyResponsiveLayout(currentWidth);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                double  cardWidth = CalculateScaledValue(width, 140, 400, 320, 3810);
                double  cardHeight = cardWidth * 1.5; // Oran koru
                double ratingFontSize = CalculateScaledValue(width, 10, 18, 320, 3810);
                double starSize = CalculateScaledValue(width, 15, 30, 320, 3810);
                foreach (var item in MoviesDisplay.GetVisualDescendants())
                {
                    if (item is Border border && border.Name == "MovieCardBorder")
                    {
                        border.Width = cardWidth;
                        border.Height = cardHeight;
                    }
                    else if (item is TextBlock textBlock)
                    {
                        if (textBlock.Name == "MovieTitle")
                        {
                            textBlock.FontSize = CalculateScaledValue(width, 12, 24, 320, 3810);
                            textBlock.MaxWidth = cardWidth*0.8;
                        }
                        else if (textBlock.Name == "RatingText")
                        {
                            textBlock.FontSize = ratingFontSize;
                            double leftMargin = cardWidth * 0.06;
                            textBlock.Margin = new Thickness(leftMargin, 3.5, 0, 0);
                        }
                    }
                    else if (item is Controls.RatingBar ratingBar && ratingBar.Name == "MovieRating")
                    {
                        ratingBar.StarSize = starSize;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        private double CalculateScaledValue(double width, double minValue, double maxValue, double minWidth, double maxWidth)
        {
            // Ensure width is in range
            width = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Calculate scaling factor (0 to 1)
            double scaleFactor = (width - minWidth) / (maxWidth - minWidth);
            
            // Scale the value
            return minValue + (scaleFactor * (maxValue - minValue));
        }

        private void MoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ApplyResponsiveLayout(MainView.Instance.Bounds.Width);
        }

        private void CastKnownForPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void StackPanelMovie_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if(stackPanel == null) return;
            var movie = stackPanel.DataContext as Movie;
            if (movie == null) return;
            var movieDetailspage = new MovieDetailsPage(movie);
            var mainView = this.FindAncestorOfType<MainView>();
            if (mainView != null)
            {
                mainView.SetContent(movieDetailspage);
            }
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Tick -= TimerOnTick;
            timer = null;
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
    }
} 