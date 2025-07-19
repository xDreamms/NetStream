using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
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
using System.Windows.Threading;
using FontAwesome.Sharp;
using HandyControl.Controls;
using HandyControl.Data;
using LibVLCSharp.Shared;
using MaterialDesignThemes.Wpf;
using NetStream.Views;
using Serilog;
using TinifyAPI;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops.Signatures;
using Vlc.DotNet.Wpf;
using YoutubeDLSharp.Options;
using YoutubeDLSharp;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;
using Credits = TMDbLib.Objects.Movies.Credits;
using MediaType = TMDbLib.Objects.General.MediaType;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using ScrollViewer = System.Windows.Controls.ScrollViewer;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for MovieDetailsPage.xaml
    /// </summary>
    public partial class MovieDetailsPage : Page,IDisposable
    {
        private Movie selectedMovie;
        public VlcVideoSourceProvider sourceProvider;
        private DirectoryInfo libDirectory;
        private string trailerLink = "";
        public static List<SourceProviderInfo> VlcVideoSourceProviders = new List<SourceProviderInfo>();
        public static SourceProviderInfo current;
        public static string currentMediaFile;
        public static List<MovieDetailsPage> MovieDetailsPagess = new List<MovieDetailsPage>();
        public MovieDetailsPage(Movie selectedMovie)
        {
            InitializeComponent();
            try
            {
                libDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
            MovieDetailsPagess.Add(this);
            this.selectedMovie = selectedMovie;
            this.DataContext = this;
            Service.Similars.Clear();
            SetVisibilityWatchButton(selectedMovie.Id);
            if (selectedMovie.ShowType != ShowType.TvShow && (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiUrl) ||
                                                              String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiKey)))
            {
                MenuTorrents.Visibility = Visibility.Collapsed;
            }

            if (selectedMovie.ShowType == ShowType.Movie)
            {
                MovieDetailsNavigation.Navigate(new MovieDetailsOverViewPage(selectedMovie));
            }
            else if (selectedMovie.ShowType == ShowType.TvShow)
            {
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        MenuTorrents.Text = Application.Current.Resources["EpisodesString"].ToString();
                    }));
                MovieDetailsNavigation.Navigate(new TvShowDetailsOverViewPage(selectedMovie));
            }
            
            if (selectedMovie.ShowType == ShowType.TvShow)
            {
                DurationElipse.Visibility = Visibility.Collapsed;
                DurationViewBox.Visibility = Visibility.Collapsed;
            }
        }

     

        public MovieDetailsPage(int movie_id,ShowType showType)
        {
            InitializeComponent();
            try
            {
                libDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
            MovieDetailsPagess.Add(this);
            Movie mo = new Movie()
            {
                Id = movie_id,
                ShowType = showType
            };
            this.selectedMovie = mo;
            this.DataContext = this;
            Service.Similars.Clear();
            SetVisibilityWatchButton(movie_id);
            if (selectedMovie.ShowType != ShowType.TvShow && (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiUrl) ||
                                                              String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiKey)))
            {
                MenuTorrents.Visibility = Visibility.Collapsed;
            }
            if (selectedMovie.ShowType == ShowType.Movie)
            {
                MovieDetailsNavigation.Navigate(new MovieDetailsOverViewPage(selectedMovie));
            }
            else if (selectedMovie.ShowType == ShowType.TvShow)
            {
                MovieDetailsNavigation.Navigate(new TvShowDetailsOverViewPage(selectedMovie));
                MenuTorrents.Text = Application.Current.Resources["EpisodesString"].ToString();
            }
            
            if (selectedMovie.ShowType == ShowType.TvShow)
            {
                DurationElipse.Visibility = Visibility.Collapsed;
                DurationViewBox.Visibility = Visibility.Collapsed;
            }
        }

        private void SetVisibility()
        {
            try
            {
                var mainMovieDetail = MainMovieTvShow.DataContext as MainMovie;
                if (mainMovieDetail != null)
                {
                    if (String.IsNullOrWhiteSpace(mainMovieDetail.Overview))
                    {
                        OverviewBox.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private bool favorite = false;
        private bool watchList = false;
        private double? rating;
        private async void SetStateAccountIcons()
        {
            try
            {
                if (Service.client.ActiveAccount != null && !String.IsNullOrWhiteSpace(Service.client.SessionId))
                {
                    FavoritesElipse.Visibility = Visibility.Visible;
                    FavoritesIconBlock.Visibility = Visibility.Visible;
                    WatchListElipse.Visibility = Visibility.Visible;
                    WatchListIconBlock.Visibility = Visibility.Visible;

                    var mainMovie = MainMovieTvShow.DataContext as MainMovie;
                    if (mainMovie != null)
                    {
                        if (mainMovie.IsFavorite)
                        {
                            FavoritesIconBlock.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                            FavoritesIconBlock.IconFont = IconFont.Solid;
                            favorite = true;
                        }
                        else
                        {
                            FavoritesIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                            FavoritesIconBlock.IconFont = IconFont.Regular;
                            favorite = false;
                        }

                        if (mainMovie.IsInWatchlist)
                        {
                            WatchListIconBlock.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                            WatchListIconBlock.IconFont = IconFont.Solid;
                            watchList = true;
                        }
                        else
                        {
                            WatchListIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                            WatchListIconBlock.IconFont = IconFont.Regular;
                            watchList = false;
                        }

                        if (mainMovie.MyRating.HasValue)
                        {
                            BtnSetRating.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                            BtnSetRating.IconFont = IconFont.Solid;
                            rating = mainMovie.MyRating;
                            TextBlockDialogText.Text = App.Current.Resources["YourVote"].ToString() + " " + rating;
                            BtnConfirm.Content = App.Current.Resources["Rerate"].ToString();
                            SetRating.Value = mainMovie.MyRating.Value;
                            BtnRemoveRating.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            BtnSetRating.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                            BtnSetRating.IconFont = IconFont.Regular;
                            rating = null;
                            TextBlockDialogText.Text = App.Current.Resources["WhatIsYourRating"].ToString();
                            BtnConfirm.Content = App.Current.Resources["Confirm"].ToString();
                            SetRating.Value = 0;
                            BtnRemoveRating.Visibility = Visibility.Collapsed;
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

        private void SetVisibilityWatchButton(int MovId)
        {
            try
            {
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    if (DownloadsPageQ.torrents.Any(x => x.MovieId == MovId))
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                ButtonWatch.Visibility = Visibility.Visible;

                            }));
                    }
                    else
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                ButtonWatch.Visibility = Visibility.Collapsed;

                            }));
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        private MainMovie MainMovieee;

        public FastObservableCollection<Cast> Casts
        {
            get
            {
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    return Service.MovieCasts;
                }
                else
                {
                    return Service.TvShowCasts;
                }
            }
        }

        public FastObservableCollection<Movie> Similars
        {
            get
            {
                return Service.Similars;
            }
        }

        async Task DownloadToPipeStreamAsync(string pipeName, IStreamInfo mediaStreamInfo, CancellationToken cancellationToken)
        {
            using (var pipe = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                try
                {
                    await pipe.WaitForConnectionAsync(cancellationToken); // Kullanıcı iptal ederse bekleme iptal olur.
                    await Service.YoutubeClient.Videos.Streams.CopyToAsync(mediaStreamInfo, pipe, null,cancellationToken); // Medya akışını indirme
                }
                catch (OperationCanceledException)
                {
                    // İptal edildiğinde burada işlem yapabilirsiniz
                    Log.Information($"Download to {pipeName} was cancelled.");
                }
                finally
                {
                    if (pipe.IsConnected)
                        pipe.Disconnect();
                }
            }
        }
        public string videoPath;
        string GenerateSixLetterGuid()
        {
            const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"; // Harf havuzu
            string guid = Guid.NewGuid().ToString("N"); // GUID oluştur ve düz format al
            var onlyLetters = guid.Where(c => char.IsLetter(c)).ToArray(); // Sadece harfleri al
            var random = new Random();

            // Rastgele 6 harf seç
            return new string(Enumerable.Range(0, 6).Select(_ => onlyLetters[random.Next(onlyLetters.Length)]).ToArray());
        }

        async Task RunFFMpegAsync()
        {
            try
            {
                var ytdl = new YoutubeDL();
              
                ytdl.YoutubeDLPath = Path.Combine(Environment.CurrentDirectory, "yt-dlp.exe");
                ytdl.FFmpegPath = Environment.CurrentDirectory + "\\ffmpeg\\bin\\ffmpeg.exe";
                var options = new OptionSet
                {
                    Format = "bestvideo+bestaudio/best",
                    MergeOutputFormat = DownloadMergeFormat.Mp4,
                    Output = videoPath
                };

                var res = await ytdl.RunVideoDownload(
                    trailerLink,
                    overrideOptions: options
                );

                //var a = GenerateSixLetterGuid();
                //string FFmpegPath = Environment.CurrentDirectory + "ffmpeg\\bin\\ffmpeg.exe";
                //string videoPipeName = "ffvideo" + a;
                //string audioPipeName = "ffaudio" + a;

                //var streamManifest = await Service.YoutubeClient.Videos.Streams.GetManifestAsync(trailerLink);
                //var streamInfoVideo = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
                //var streamInfoAudio = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                //var processStartInfo = new ProcessStartInfo
                //{
                //    FileName = Path.GetFullPath(FFmpegPath),
                //    Arguments = $@"-i \\.\pipe\{videoPipeName} -i \\.\pipe\{audioPipeName} -c:v copy -c:a aac -movflags frag_keyframe+empty_moov -f mp4 -",
                //    UseShellExecute = false,
                //    RedirectStandardOutput = true,
                //    CreateNoWindow = true
                //};

                //using (var process = Process.Start(processStartInfo))
                //{
                //    using (var fileStream = new FileStream(videoPath, FileMode.Create))
                //    {
                //        var audioDownloadTask = DownloadToPipeStreamAsync(audioPipeName, streamInfoAudio, cancellationTokenSource.Token);
                //        var videoDownloadTask = DownloadToPipeStreamAsync(videoPipeName, streamInfoVideo, cancellationTokenSource.Token);
                //        var outputTask = process.StandardOutput.BaseStream.CopyToAsync(fileStream, cancellationTokenSource.Token);

                //        try
                //        {
                //            // Tüm görevler tamamlanana kadar bekleyin
                //            await Task.WhenAll(audioDownloadTask, videoDownloadTask, outputTask);
                //        }
                //        catch (OperationCanceledException)
                //        {
                //            Console.WriteLine("FFMpeg processing was cancelled.");
                //        }
                //        finally
                //        {
                //            process.StandardOutput.Close();
                //        }
                //    }
                //    process.WaitForExit();
                //}
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task GetMainMovieDetail()
        {
            MainMovieTvShow.DataContext = await Service.GetMainMovieDetail( selectedMovie);
        }

        private async Task GetTrailerLink()
        {
            trailerLink = await Service.GetTrailerLink(selectedMovie.ShowType,selectedMovie.Id);
        }

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private async void Load()
        {
            if (Service.client.ActiveAccount == null || String.IsNullOrWhiteSpace(Service.client.SessionId))
            {
                FavoritesElipse.Visibility = Visibility.Collapsed;
                FavoritesIconBlock.Visibility = Visibility.Collapsed;
                WatchListElipse.Visibility = Visibility.Collapsed;
                WatchListIconBlock.Visibility = Visibility.Collapsed;
                BtnSetRating.Visibility = Visibility.Collapsed;
                RatingElipse.Visibility = Visibility.Collapsed;
            }
            sourceProvider = TrailerPlayer.SourceProvider;
            sourceProvider.CreatePlayer(libDirectory);

            current = new SourceProviderInfo();
            current.Id = selectedMovie.Id;
            current.VlcVideoSourceProvider = sourceProvider;

            var tasks = new List<Task>()
            {
                GetMainMovieDetail(),
                GetTrailerLink(),
                Service.GetCredits(selectedMovie),
                Service.GetSimilars( selectedMovie)
            };

            await Task.WhenAll(tasks).ContinueWith(t =>
            {
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        SetLastVisibleItemLowerOpacity(SimilarMoviesDisplay, SimilarMoviesDisplay);
                        SetLastVisibleItemLowerOpacity(CastDisplay, CastDisplay);
                    }));

            }); ;
            SetVisibility();
            SetStateAccountIcons();
            videoPath = System.IO.Path.Combine(AppSettingsManager.appSettings.YoutubeVideoPath, selectedMovie.Id + ".mp4");
            currentMediaFile = videoPath;
            if (!String.IsNullOrWhiteSpace(trailerLink))
            {
                try
                {
                    await Task.Run(async () =>
                    {
                        if (!File.Exists(videoPath) || new FileInfo(videoPath).Length == 0)
                        {
                            await RunFFMpegAsync();
                        }
                    }).ContinueWith(t =>
                    {
                        if (File.Exists(videoPath) && new FileInfo(videoPath).Length > 0)
                        {
                            Application.Current.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(() =>
                                {
                                    MainMovieTvShow.Visibility = Visibility.Collapsed;
                                    TrailerPlayer.Visibility = Visibility.Visible;
                                    //var rowDefinition = new RowDefinition();
                                    //rowDefinition.Height = new GridLength(1.3, GridUnitType.Star);
                                    //GridSide.RowDefinitions.RemoveAt(0);
                                    //GridSide.RowDefinitions.Insert(0, rowDefinition);
                                }));

                            if (sourceProvider.MediaPlayer == null)
                            {
                                sourceProvider = TrailerPlayer.SourceProvider;
                                sourceProvider.CreatePlayer(libDirectory);
                                current.VlcVideoSourceProvider = sourceProvider;

                                sourceProvider.MediaPlayer.SetMedia(new FileInfo(videoPath));

                                sourceProvider.MediaPlayer.Play();

                                VlcVideoSourceProviders.Add(new SourceProviderInfo()
                                {
                                    Id = selectedMovie.Id,
                                    VlcVideoSourceProvider = sourceProvider
                                });

                                sourceProvider.MediaPlayer.Video.IsKeyInputEnabled = false;
                                sourceProvider.MediaPlayer.Video.IsMouseInputEnabled = false;

                                sourceProvider.MediaPlayer.Stopped += MediaPlayerOnStopped;
                            }
                            else
                            {
                                sourceProvider.MediaPlayer.SetMedia(new FileInfo(videoPath));

                                sourceProvider.MediaPlayer.Play();

                                VlcVideoSourceProviders.Add(new SourceProviderInfo()
                                {
                                    Id = selectedMovie.Id,
                                    VlcVideoSourceProvider = sourceProvider
                                });

                                sourceProvider.MediaPlayer.Video.IsKeyInputEnabled = false;
                                sourceProvider.MediaPlayer.Video.IsMouseInputEnabled = false;

                                sourceProvider.MediaPlayer.Stopped += MediaPlayerOnStopped;
                            }
                           
                        }
                    });
                }
                catch (Exception exception)
                {
                    var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                    Log.Error(errorMessage);
                }
            }
            
        }

     
        private void Home_OnLoaded(object sender, RoutedEventArgs e)
        {
            Load();
            MovieDetailsNavigation.Navigated += MovieDetailsNavigationOnNavigated;
            
        }



        private void MovieDetailsNavigationOnNavigated(object sender, NavigationEventArgs e)
        {
            NavigationService.RemoveBackEntry();
        }

        private void MediaPlayerOnStopped(object? sender, VlcMediaPlayerStoppedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    //var rowDefinition = new RowDefinition();
                    //rowDefinition.Height = new GridLength(750, GridUnitType.Pixel);
                    //GridSide.RowDefinitions.RemoveAt(0);
                    //GridSide.RowDefinitions.Insert(0, rowDefinition);
                    TrailerPlayer.Visibility = Visibility.Collapsed;
                    MainMovieTvShow.Visibility = Visibility.Visible;
                }));
        }

        private void MoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private async void MenuOverview_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Service.PhotoDetailsBackdrop.Clear();
                Service.PhotoDetailsPoster.Clear();
                Service.VideoDetails.Clear();
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.MenuBottomLine.SetValue(Grid.ColumnProperty, 0);
                        MenuOverview.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        MenuVideo.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuPhotos.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuTorrents.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuComments.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    }));
                //var rowDefinition = new RowDefinition();
                //rowDefinition.Height = new GridLength(800, GridUnitType.Pixel);
                //GridSide.RowDefinitions.RemoveAt(1);
                //GridSide.RowDefinitions.Insert(1, rowDefinition);
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    MovieDetailsNavigation.Navigate(new MovieDetailsOverViewPage(selectedMovie));
                }
                else if (selectedMovie.ShowType == ShowType.TvShow)
                {
                    MovieDetailsNavigation.Navigate(new TvShowDetailsOverViewPage(selectedMovie));
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }

           

        }

        private async void MenuVideo_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //var rowDefinition = new RowDefinition();
            //rowDefinition.Height = new GridLength(1, GridUnitType.Auto);
            //GridSide.RowDefinitions.RemoveAt(1);
            //GridSide.RowDefinitions.Insert(1, rowDefinition);
            try
            {
                Service.PhotoDetailsBackdrop.Clear();
                Service.PhotoDetailsPoster.Clear();
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.MenuBottomLine.SetValue(Grid.ColumnProperty, 2);
                        MenuOverview.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuVideo.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        MenuPhotos.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuTorrents.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuComments.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    }));

                MovieDetailsNavigation.Navigate(new MovieDetailsVideoPage(selectedMovie,this));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MenuPhotos_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //var rowDefinition = new RowDefinition();
            //rowDefinition.Height = new GridLength(1, GridUnitType.Auto);
            //GridSide.RowDefinitions.RemoveAt(1);
            //GridSide.RowDefinitions.Insert(1, rowDefinition);
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.MenuBottomLine.SetValue(Grid.ColumnProperty, 4);
                        MenuOverview.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuVideo.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuPhotos.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        MenuTorrents.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuComments.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    }));
                MovieDetailsNavigation.Navigate(new MovieDetailsPhotosPage(selectedMovie));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }

        }

        private void SimilarMoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Movie selectedMov = (Movie)SimilarMoviesDisplay.SelectedItem;
                if (selectedMov != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMov);
                    this.NavigationService.Navigate(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void SimilarMoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Task.Delay(100);
            SetLastVisibleItemLowerOpacity(SimilarMoviesDisplay, SimilarMoviesDisplay);
        }

        public static TorrentsPage TorrentsPage;
        private async void MenuTorrents_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //var rowDefinition = new RowDefinition();
            //rowDefinition.Height = new GridLength(1, GridUnitType.Auto);
            //GridSide.RowDefinitions.RemoveAt(1);
            //GridSide.RowDefinitions.Insert(1, rowDefinition);
            try
            {
                Service.PhotoDetailsBackdrop.Clear();
                Service.PhotoDetailsPoster.Clear();
                Service.VideoDetails.Clear();
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.MenuBottomLine.SetValue(Grid.ColumnProperty, 8);
                        MenuOverview.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuVideo.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuPhotos.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuTorrents.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        MenuComments.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                    }));
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    MovieDetailsNavigation.Navigate(new TorrentsPage(selectedMovie));
                }
                else
                {
                    MovieDetailsNavigation.Navigate(new EpisodesPage(selectedMovie, MovieDetailsNavigation));
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (e.VerticalChange == 0 && e.HorizontalChange == 0)
                {
                
                }
                else
                {
                    if (TorrentsPage != null && TorrentsPage.isItemLoadingFinished)
                    {
                        if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 900)
                        {
                            if (!TorrentsPage.sortByComboboxSelectionChanged)
                            {
                                if (TorrentsPage.selectedMovie.ShowType == ShowType.Movie && TorrentsPage.loadingMovieTorrentsFinished)
                                {
                                    await TorrentsPage.LoadMovieTorrents();
                                }
                                else if (TorrentsPage.selectedMovie.ShowType == ShowType.TvShow &&
                                         TorrentsPage.loadingtvTorrentsFinished)
                                {
                                    await TorrentsPage.LoadTvShows();
                                }
                            }
                            else
                            {
                                if (TorrentsPage.isSortedCollectionLoadingFinished)
                                {
                                    await TorrentsPage.LoadMoreTorrentCollection();
                                }   
                            }
                        }
                    }
                }
            


                ScrollViewer sv = (ScrollViewer)sender;
                Rect svViewportBounds =
                    new Rect(sv.HorizontalOffset, sv.VerticalOffset, sv.ViewportWidth, sv.ViewportHeight);
                var container = TrailerPlayer;
                var container2 = MainMovieTvShow;

                if (container != null && container.ActualHeight > 0)
                {
                    var offset = VisualTreeHelper.GetOffset(container);
                    var bounds = new Rect(offset.X, offset.Y - 200, container.ActualWidth, container.ActualHeight - container.ActualHeight / 2);

                    if (!svViewportBounds.IntersectsWith(bounds))
                    {
                        if (sourceProvider != null && sourceProvider.MediaPlayer != null &&
                            sourceProvider.MediaPlayer.IsPlaying())
                        {
                            sourceProvider.MediaPlayer.Pause();
                            Application.Current.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(() =>
                                {
                                    //var rowDefinition = new RowDefinition();
                                    //rowDefinition.Height = new GridLength(750, GridUnitType.Pixel);
                                    //GridSide.RowDefinitions.RemoveAt(0);
                                    //GridSide.RowDefinitions.Insert(0, rowDefinition);
                                    TrailerPlayer.Visibility = Visibility.Collapsed;
                                    MainMovieTvShow.Visibility = Visibility.Visible;

                                }));
                        }

                    }
                }

                if (container2 != null && container2.ActualHeight > 0)
                {
                    var offset = VisualTreeHelper.GetOffset(container2);
                    var bounds = new Rect(offset.X, offset.Y - 200, container2.ActualWidth, container2.ActualHeight - container2.ActualHeight / 1.5);

                    if (svViewportBounds.IntersectsWith(bounds))
                    {
                        if (sourceProvider != null && sourceProvider.MediaPlayer != null && sourceProvider.MediaPlayer.CouldPlay)
                        {
                            sourceProvider.MediaPlayer.Play();
                            Application.Current.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(() =>
                                {
                                    //var rowDefinition = new RowDefinition();
                                    //rowDefinition.Height = new GridLength(1.3, GridUnitType.Star);
                                    //GridSide.RowDefinitions.RemoveAt(0);
                                    //GridSide.RowDefinitions.Insert(0, rowDefinition);
                                    TrailerPlayer.Visibility = Visibility.Visible;
                                    MainMovieTvShow.Visibility = Visibility.Collapsed;

                                }));
                        }

                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void WatchTrailerButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if(sourceProvider != null && sourceProvider.MediaPlayer != null && sourceProvider.MediaPlayer.IsPlaying()) return;
                sourceProvider = TrailerPlayer.SourceProvider;
                sourceProvider.CreatePlayer(libDirectory);
                await Task.Run(async () =>
                {
                    await RunFFMpegAsync();
                }).ContinueWith(t =>
                {
                    Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            
                            TrailerPlayer.Visibility = Visibility.Visible;
                            MainMovieTvShow.Visibility = Visibility.Collapsed;

                        }));

                    sourceProvider.MediaPlayer.SetMedia(new FileInfo(videoPath));

                    sourceProvider.MediaPlayer.Play();
                    sourceProvider.MediaPlayer.Video.IsKeyInputEnabled = false;
                    sourceProvider.MediaPlayer.Video.IsMouseInputEnabled = false;

                    sourceProvider.MediaPlayer.Stopped += MediaPlayerOnStopped;
                });


            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void WatchButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var selectedTorrent = DownloadsPageQ.torrents.Where(x => x.MovieId == selectedMovie.Id).OrderByDescending(x => x.IsCompleted).FirstOrDefault();
                if (selectedTorrent != null)
                {
                    var files = await Libtorrent.GetFiles(selectedTorrent.Hash);
                    var mediaFileCount = files.Count(x => x.IsMediaFile);
                    if (mediaFileCount == 1)
                    {
                        var mediaFile = files.FirstOrDefault(x => x.IsMediaFile);
                        var playerWindow = new PlayerWindow(selectedTorrent.MovieId, selectedTorrent.MovieName,
                            selectedTorrent.ShowType,
                            selectedTorrent.SeasonNumber, selectedTorrent.EpisodeNumber,
                            new FileInfo(mediaFile.FullPath), selectedTorrent.IsCompleted,
                            selectedTorrent.ImdbId, selectedTorrent, mediaFile.Index, selectedTorrent.Poster);
                        playerWindow.Show();
                        DownloadsPageQ.GetDownloadsPageInstance.openedTorrents.Add(selectedTorrent.Link);
                        sourceProvider?.Dispose();
                    }
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
            try
            {
                cancellationTokenSource.Cancel();
                this.DataContext = null;
                MainMovieTvShow.DataContext = null;
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    Service.MovieCasts.Clear();
                }
                else
                {
                    Service.TvShowCasts.Clear();
                }
                Service.Similars.Clear();
                MovieDetailsNavigation.Navigated -= MovieDetailsNavigationOnNavigated;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CastDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Cast selectedCast = CastDisplay.SelectedItem as Cast;
                if (selectedCast != null)
                {
                    CastPage castPage = new CastPage(selectedCast);
                    this.NavigationService.Navigate(castPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void FavoritesIconBlock_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    if (favorite)
                    {
                        await Service.client.AccountChangeFavoriteStatusAsync(MediaType.Movie, selectedMovie.Id, false);
                        favorite = false;
                        FavoritesIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                        FavoritesIconBlock.IconFont = IconFont.Regular;
                        Growl.Success(new GrowlInfo(){Message = MainMovieName.Text + " " + App.Current.Resources["RemoveFavoritesNotify"],StaysOpen = false,WaitTime = 4});
                    }
                    else
                    {
                        await Service.client.AccountChangeFavoriteStatusAsync(MediaType.Movie, selectedMovie.Id, true);
                        favorite = true;
                        FavoritesIconBlock.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                        FavoritesIconBlock.IconFont = IconFont.Solid;
                        Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " + App.Current.Resources["AddFavoritesNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                }
                else
                {
                    if (favorite)
                    {
                        await Service.client.AccountChangeFavoriteStatusAsync(MediaType.Tv, selectedMovie.Id, false);
                        favorite = false;
                        FavoritesIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                        FavoritesIconBlock.IconFont = IconFont.Regular;
                        Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " + App.Current.Resources["RemoveFavoritesNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                    else
                    {
                        await Service.client.AccountChangeFavoriteStatusAsync(MediaType.Tv, selectedMovie.Id, true);
                        favorite = true;
                        FavoritesIconBlock.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                        FavoritesIconBlock.IconFont = IconFont.Solid;
                        Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " + App.Current.Resources["AddFavoritesNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void SimilarMoviesScrollRightButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToRight(SimilarMoviesDisplay);
        }

        private void SimilarMoviesScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollToLeft(SimilarMoviesDisplay);
        }

        private void ScrollToRight(ListBox listBox)
        {
            try
            {
                Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                if (border != null)
                {
                    ScrollViewer scrollViewer = border.Child as ScrollViewer;
                    if (scrollViewer != null)
                    {
                        var currentScrollWidth = (((Service.Similars.Count - 5) - scrollViewer.ScrollableWidth) * 1) + 5.0;
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

        private void ScrollToLeft(ListBox listBox)
        {
            try
            {
                Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                if (border != null)
                {
                    ScrollViewer scrollViewer = border.Child as ScrollViewer;
                    if (scrollViewer != null)
                    {
                        var currentScrollWidth = (((Service.Similars.Count - 5) - scrollViewer.ScrollableWidth) * 1) + 5.0;
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

        List<ListBoxItem> SimilarMoviesVisibleItems = new List<ListBoxItem>();
        List<ListBoxItem> CastVisibleItems = new List<ListBoxItem>();

        private void SetLastVisibleItemLowerOpacity(ListBox listBox, FrameworkElement parentToTestVisibility)
        {
            try
            {
                if (listBox == SimilarMoviesDisplay)
                {
                    foreach (var visibleItem in SimilarMoviesVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }

                    SimilarMoviesVisibleItems.Clear();
                    foreach (Movie item in SimilarMoviesDisplay.Items)
                    {
                        var container = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem(item);
                        if (IsUserVisible(container, parentToTestVisibility))
                        {
                            SimilarMoviesVisibleItems.Add(container);
                        }
                    }

                    Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                    if (border != null)
                    {
                        ScrollViewer scrollViewer = border.Child as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            if (SimilarMoviesVisibleItems.Count > 0 &&
                                scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                SimilarMoviesVisibleItems.Last().Opacity = 0.5;
                            }
                        }
                    }
                }
                else if(listBox == CastDisplay)
                {
                    foreach (var visibleItem in CastVisibleItems)
                    {
                        visibleItem.Opacity = 1;
                    }

                    CastVisibleItems.Clear();
                    foreach (Cast item in CastDisplay.Items)
                    {
                        var container = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem(item);
                        if (IsUserVisible(container, parentToTestVisibility))
                        {
                            CastVisibleItems.Add(container);
                        }
                    }

                    Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
                    if (border != null)
                    {
                        ScrollViewer scrollViewer = border.Child as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            if (CastVisibleItems.Count > 0 &&
                                scrollViewer.ScrollableWidth != scrollViewer.ContentHorizontalOffset)
                            {
                                CastVisibleItems.Last().Opacity = 0.5;
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

        private void MovieDetailsPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetLastVisibleItemLowerOpacity(SimilarMoviesDisplay, SimilarMoviesDisplay);
            SetLastVisibleItemLowerOpacity(CastDisplay, CastDisplay);
        }

        private void CastsScrollRightButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Decorator border = VisualTreeHelper.GetChild(CastDisplay, 0) as Decorator;
                if (border != null)
                {
                    ScrollViewer scrollViewer = border.Child as ScrollViewer;
                    if (scrollViewer != null)
                    {
                        var currentScrollWidth = ((((selectedMovie.ShowType == ShowType.Movie ? Service.MovieCasts.Count : Service.TvShowCasts.Count) - 5) - scrollViewer.ScrollableWidth) * 1) + 5.0;
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
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CastsScrollLeftButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Decorator border = VisualTreeHelper.GetChild(CastDisplay, 0) as Decorator;
                if (border != null)
                {
                    ScrollViewer scrollViewer = border.Child as ScrollViewer;
                    if (scrollViewer != null)
                    {
                        var currentScrollWidth = ((((selectedMovie.ShowType == ShowType.Movie ? Service.MovieCasts.Count : Service.TvShowCasts.Count) - 5) - scrollViewer.ScrollableWidth) * 1) + 5.0;
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
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CastDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Task.Delay(100);
            SetLastVisibleItemLowerOpacity(CastDisplay, CastDisplay);
        }

        private async void WatchListIconBlock_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    if (watchList)
                    {
                        await Service.client.AccountChangeWatchlistStatusAsync(MediaType.Movie, selectedMovie.Id, false);
                        watchList = false;
                        WatchListIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                        WatchListIconBlock.IconFont = IconFont.Regular;
                        Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " + App.Current.Resources["RemoveWatchListNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                    else
                    {
                        await Service.client.AccountChangeWatchlistStatusAsync(MediaType.Movie, selectedMovie.Id, true);
                        watchList = true;
                        WatchListIconBlock.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                        WatchListIconBlock.IconFont = IconFont.Solid;
                        Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " + App.Current.Resources["AddWatchListNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                }
                else
                {
                    if (watchList)
                    {
                        await Service.client.AccountChangeWatchlistStatusAsync(MediaType.Tv, selectedMovie.Id, false);
                        watchList = false;
                        WatchListIconBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                        WatchListIconBlock.IconFont = IconFont.Regular;
                        Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " + App.Current.Resources["RemoveWatchListNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                    else
                    {
                        await Service.client.AccountChangeWatchlistStatusAsync(MediaType.Tv, selectedMovie.Id, true);
                        watchList = true;
                        WatchListIconBlock.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                        WatchListIconBlock.IconFont = IconFont.Solid;
                        Growl.Success(new GrowlInfo() { Message = MainMovieName.Text + " " + App.Current.Resources["AddWatchListNotify"], StaysOpen = false, WaitTime = 4 });
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MenuComments_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        this.MenuBottomLine.SetValue(Grid.ColumnProperty, 6);
                        MenuOverview.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuVideo.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuPhotos.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuTorrents.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                        MenuComments.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    }));
                MovieDetailsNavigation.Navigate(new CommentPage(selectedMovie));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnSetRating_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DialogHost.IsOpen = true;
        }

        private void BtnSetRating_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if(rating.HasValue) return;
            BtnSetRating.IconFont = IconFont.Solid;
        }

        private void BtnSetRating_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (rating.HasValue) return;
            BtnSetRating.IconFont = IconFont.Regular;
        }

        private void FavoritesIconBlock_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (favorite) return;
            FavoritesIconBlock.IconFont = IconFont.Solid;
        }

        private void FavoritesIconBlock_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (favorite) return;
            FavoritesIconBlock.IconFont = IconFont.Regular;
        }

        private void WatchListIconBlock_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if(watchList) return;
            WatchListIconBlock.IconFont = IconFont.Solid;
        }

        private void WatchListIconBlock_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (watchList) return;
            WatchListIconBlock.IconFont = IconFont.Regular;
        }

        private void BtnCloseDialog_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DialogHost.IsOpen = false;
        }

        private async void BtnConfirm_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (await Service.Vote(selectedMovie.Id, selectedMovie.ShowType, SetRating.Value))
                {
                    DialogHost.IsOpen = false;
                    BtnSetRating.Foreground = new SolidColorBrush((Color)Application.Current.Resources["ColorDefault"]);
                    BtnSetRating.IconFont = IconFont.Solid;
                    rating = SetRating.Value;
                    TextBlockDialogText.Text = Application.Current.Resources["YourVote"].ToString() + " " + rating;
                    BtnConfirm.Content = Application.Current.Resources["Rerate"].ToString();
                    BtnRemoveRating.Visibility = Visibility.Visible;
                    Growl.Success(new GrowlInfo() { Message = App.Current.Resources["VoteNotify"].ToString(), StaysOpen = false, WaitTime = 4 });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnRemoveRating_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (await Service.RemoveVote(selectedMovie.Id, selectedMovie.ShowType))
                {
                    DialogHost.IsOpen = false;
                    BtnSetRating.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                    BtnSetRating.IconFont = IconFont.Regular;
                    rating = null;
                    TextBlockDialogText.Text = App.Current.Resources["WhatIsYourRating"].ToString();
                    BtnConfirm.Content = App.Current.Resources["Confirm"].ToString();
                    SetRating.Value = 0;
                    BtnRemoveRating.Visibility = Visibility.Collapsed;
                    Growl.Success(new GrowlInfo() { Message = App.Current.Resources["RemoveVoteNotify"].ToString(), StaysOpen = false, WaitTime = 4 });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MovieDetailsPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                MovieDetailsNavigation.Navigated -= MovieDetailsNavigationOnNavigated;
                if(sourceProvider != null && sourceProvider.MediaPlayer != null)
                    sourceProvider.MediaPlayer.Stopped -= MediaPlayerOnStopped;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
    }

    public class SourceProviderInfo
    {
        public int Id { get; set; }
        public VlcVideoSourceProvider? VlcVideoSourceProvider { get; set; }
    }

   
}
