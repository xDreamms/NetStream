using NetStream.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
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
using DynamicData;
using Serilog;
using TMDbLib.Objects.Discover;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;
using Vlc.DotNet.Wpf;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;
using Application = System.Windows.Application;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for TorrentsPage.xaml
    /// </summary>
    public partial class TorrentsPage : Page
    {
        public Movie selectedMovie;
        public int seasonNumber;
        public int episodeNumber;
        public TorrentsPage(Movie selectedMovie)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
            SortByComboBox.ItemsSource = SortComboBoxItems;
            SortByComboBox.SelectedIndex = -1;
            MovieDetailsPage.TorrentsPage = this;
        }

        public TorrentsPage(Movie selectedMovie,int seasonNumber,int episodeNumber)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
            this.seasonNumber = seasonNumber;
            this.episodeNumber = episodeNumber;

            SortByComboBox.ItemsSource = SortComboBoxItems;
            SortByComboBox.SelectedIndex = -1;

            MovieDetailsPage.TorrentsPage = this;
        }

        public TorrentsPage(Movie selectedMovie,  int seasonNumber)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
            this.seasonNumber = seasonNumber;
            this.episodeNumber = -1;

            SortByComboBox.ItemsSource = SortComboBoxItems;
            SortByComboBox.SelectedIndex = -1;

            MovieDetailsPage.TorrentsPage = this;
        }

        private List<string> SortComboBoxItems = new List<string>()
        {
            App.Current.Resources["AscendingBySize"].ToString(),
            App.Current.Resources["DescendingBySize"].ToString(),
            App.Current.Resources["AscendingBySeeders"].ToString(),
            App.Current.Resources["DescendingBySeeders"].ToString(),
            App.Current.Resources["AscendingByDate"].ToString(),
            App.Current.Resources["DescendingByDate"].ToString(),
            App.Current.Resources["AscendingByVideoQuality"].ToString(),
            App.Current.Resources["DescendingByVideoQuality"].ToString()
        };

        private FastObservableCollection<Item> items = new FastObservableCollection<Item>();


        private void TorrentsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }

        private async void TorrentsDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Item selectedTorrent = TorrentsDisplay.SelectedItem as Item;
                if (selectedTorrent != null)
                {
                    if (DownloadsPageQ.GetDownloadsPageInstance != null)
                    {
                        DownloadsPageQ.torrents.Add(selectedTorrent);
                        HomePage.GetHomePageInstance.HomePageNavigation.Navigate(DownloadsPageQ.GetDownloadsPageInstance);
                        await DownloadsPageQ.GetDownloadsPageInstance.StartTorrenting2(selectedTorrent);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public readonly string[] SizeSuffixes =
            { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" }; 
        public string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
        Random rnd = new Random();
        private List<Item> torrents;
        private FastObservableCollection<Item> torrentCollection = new FastObservableCollection<Item>();

        public int pageSize = 50;
        public int movieTorrentsCurrentPage = 0;
        public int movieTorrentsMaxPage = 0;
        public bool loadingMovieTorrentsFinished = false;
        public async Task LoadMovieTorrents()
        {
            try
            {
                if (movieTorrentsCurrentPage <= movieTorrentsMaxPage)
                {
                    loadingMovieTorrentsFinished = false;
                    var pagedMovies = torrents.Skip(movieTorrentsCurrentPage * pageSize).Take(pageSize);

                    foreach (var torrent in pagedMovies)
                    {
                        using (FastObservableCollection<Item> iDelayed = items.DelayNotifications())
                        {
                            int randomNumber = rnd.Next(0, images.Backdrops.Count - 1);
                            var image = Service.client
                                .GetImageUrl(
                                    Service.client.Config.Images.BackdropSizes
                                        [Service.client.Config.Images.BackdropSizes.Count - 4],
                                    images.Backdrops[randomNumber].FilePath).AbsoluteUri;
                            torrent.Poster = (image);
                            torrent.PublishDate = torrent.PubDate.ToShortDateString();
                            torrent.SizeProperty = SizeSuffix((Int64)torrent.Size);
                            torrent.ImageUrl = image;
                            torrent.MovieId = selectedMovie.Id;
                            torrent.MovieName = movie.Title;
                            torrent.ShowType = ShowType.Movie;
                            torrent.SeasonNumber = 0;
                            torrent.EpisodeNumber = 0;
                            torrent.SeedersProperty = torrent.Seeders + " " + App.Current.Resources["Seeders"];
                            torrent.ImdbId = Int32.Parse(new String(movie.ImdbId.Where(Char.IsDigit).ToArray()));
                            iDelayed.Add(torrent);
                        }
                    }

                    movieTorrentsCurrentPage++;
                    loadingMovieTorrentsFinished = true;
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
           
        }
        public int tvTorrentsCurrentPage = 0;
        public int tvTorrentsMaxPage = 0;
        public bool loadingtvTorrentsFinished = false;
        public async Task LoadTvShows()
        {
            try
            {
                if (tvTorrentsCurrentPage <= tvTorrentsMaxPage)
                {
                    loadingtvTorrentsFinished = false;
                    var page1Torrents = torrents.Skip(tvTorrentsCurrentPage * pageSize).Take(pageSize);

                    foreach (var torrent in page1Torrents)
                    {
                        using (FastObservableCollection<Item> iDelayed = items.DelayNotifications())
                        {
                            if (episodeNumber == -1)
                            {
                                var image = Service.client
                                    .GetImageUrl(
                                        Service.client.Config.Images.PosterSizes.Last(),
                                        images2.Posters.FirstOrDefault().FilePath).AbsoluteUri;
                                torrent.Poster = (image);
                                torrent.ImageUrl = image;
                            }
                            else
                            {
                                int randomNumber = rnd.Next(0, images.Backdrops.Count - 1);
                                var image = Service.client
                                    .GetImageUrl(
                                        Service.client.Config.Images.BackdropSizes
                                            [Service.client.Config.Images.BackdropSizes.Count - 4],
                                        images.Backdrops[randomNumber].FilePath).AbsoluteUri;
                                torrent.Poster = (image);
                                torrent.ImageUrl = image;
                            }


                            torrent.PublishDate = torrent.PubDate.ToShortDateString();
                            torrent.SizeProperty = SizeSuffix((Int64)torrent.Size);
                            torrent.MovieId = selectedMovie.Id;
                            torrent.MovieName = episodeNumber == -1
                                ? tvShow.Name
                                : tvShow.Name + " S" + seasonNumber + "E" + episodeNumber;
                            torrent.SeasonNumber = seasonNumber;
                            torrent.EpisodeNumber = episodeNumber;
                            torrent.ShowType = ShowType.TvShow;
                            torrent.SeedersProperty = torrent.Seeders + " " + App.Current.Resources["Seeders"];
                            torrent.ImdbId =
                                Int32.Parse(new String(tvShow.ExternalIds.ImdbId.Where(Char.IsDigit).ToArray()));
                            iDelayed.Add(torrent);
                        }
                    }

                    tvTorrentsCurrentPage++;
                    loadingtvTorrentsFinished = true;
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }

            
        }

        public int GetMaxPage(int totalItems)
        {
            return (int)Math.Ceiling((double)totalItems / pageSize);
        }

        private TMDbLib.Objects.Movies.Movie movie;
        private ImagesWithId images;
        public bool isItemLoadingFinished = false;

        PosterImages images2 = null;
        private TvShow tvShow;
        private async void TorrentsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SortByComboBox.IsEnabled = false;
                isItemLoadingFinished = false;
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    movie = await Service.client.GetMovieAsync(selectedMovie.Id, "en",null, MovieMethods.Images);
                    images = await Service.client.GetMovieImagesAsync(selectedMovie.Id);
                    torrents = await JackettService.GetMovieTorrentsImdb(movie.ImdbId);
                    torrents.AddRange(await JackettService.GetMovieTorrentsName(movie.Title,movie.ReleaseDate.HasValue ? movie.ReleaseDate.Value.Year:0));
                    movieTorrentsMaxPage = GetMaxPage(torrents.Count);
                    await Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            TorrentCount.Text = torrents.Count + " " + App.Current.Resources["Results"];

                        }));

                    await LoadMovieTorrents();

                    foreach (var torrent in torrents)
                    {
                        using (FastObservableCollection<Item> iDelayed = torrentCollection.DelayNotifications())
                        {
                            int randomNumber = rnd.Next(0, images.Backdrops.Count - 1);
                            var image = Service.client
                                .GetImageUrl(
                                    Service.client.Config.Images.BackdropSizes
                                        [Service.client.Config.Images.BackdropSizes.Count - 4],
                                    images.Backdrops[randomNumber].FilePath).AbsoluteUri;
                            torrent.Poster = (image);
                            torrent.PublishDate = torrent.PubDate.ToShortDateString();
                            torrent.SizeProperty = SizeSuffix((Int64)torrent.Size);
                            torrent.ImageUrl = image;
                            torrent.MovieId = selectedMovie.Id;
                            torrent.MovieName = movie.Title;
                            torrent.ShowType = ShowType.Movie;
                            torrent.SeasonNumber = 0;
                            torrent.EpisodeNumber = 0;
                            torrent.SeedersProperty = torrent.Seeders + " " + App.Current.Resources["Seeders"]; ;
                            torrent.ImdbId = Int32.Parse(new String(movie.ImdbId.Where(Char.IsDigit).ToArray()));
                            iDelayed.Add(torrent);
                        }
                    }
                }
                else if (selectedMovie.ShowType == ShowType.TvShow)
                {
                    tvShow = await Service.client.GetTvShowAsync(selectedMovie.Id,TvShowMethods.ExternalIds,"en");
                    if (episodeNumber == -1)
                    {
                        torrents = await JackettService.GetTvSeasonTorrents(tvShow.Name, seasonNumber);
                        torrents.AddRange(await JackettService.GetTvShowTorrents(tvShow.Name,tvShow.FirstAirDate.Value.Year,seasonNumber,episodeNumber));
                        //torrents.AddRange(await JackettService.GetTvShowTorrentsWithName(tvShow.Name + " season " + seasonNumber ));
                    }
                    else
                    {
                        torrents = await JackettService.GetTvEpisodeTorrents(tvShow.Name, seasonNumber, episodeNumber);
                        torrents.AddRange(await JackettService.GetTvShowTorrents(tvShow.Name, tvShow.FirstAirDate.Value.Year, seasonNumber, episodeNumber));
                    }

                    tvTorrentsMaxPage = GetMaxPage(torrents.Count);

                    if (episodeNumber == -1)
                    {
                        images2 = await Service.client.GetTvSeasonImagesAsync(selectedMovie.Id, seasonNumber);
                    }
                    else
                    {
                        images = await Service.client.GetTvShowImagesAsync(selectedMovie.Id);
                    }

                    await Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            TorrentCount.Text = torrents.Count + " " + App.Current.Resources["Results"];

                        }));

                    await LoadTvShows();

                    foreach (var torrent in torrents)
                    {
                        using (FastObservableCollection<Item> iDelayed = torrentCollection.DelayNotifications())
                        {
                            if (episodeNumber == -1)
                            {
                                var image = Service.client
                                    .GetImageUrl(
                                        Service.client.Config.Images.PosterSizes.Last(),
                                        images2.Posters.FirstOrDefault().FilePath).AbsoluteUri;
                                torrent.Poster = (image);
                                torrent.ImageUrl = image;
                            }
                            else
                            {
                                int randomNumber = rnd.Next(0, images.Backdrops.Count - 1);
                                var image = Service.client
                                    .GetImageUrl(
                                        Service.client.Config.Images.BackdropSizes
                                            [Service.client.Config.Images.BackdropSizes.Count - 4],
                                        images.Backdrops[randomNumber].FilePath).AbsoluteUri;
                                torrent.Poster = (image);
                                torrent.ImageUrl = image;
                            }


                            torrent.PublishDate = torrent.PubDate.ToShortDateString();
                            torrent.SizeProperty = SizeSuffix((Int64)torrent.Size);
                            torrent.MovieId = selectedMovie.Id;
                            torrent.MovieName = episodeNumber == -1
                                ? tvShow.Name
                                : tvShow.Name + " S" + seasonNumber + "E" + episodeNumber;
                            torrent.SeasonNumber = seasonNumber;
                            torrent.EpisodeNumber = episodeNumber;
                            torrent.ShowType = ShowType.TvShow;
                            torrent.SeedersProperty = torrent.Seeders + " " + App.Current.Resources["Seeders"];
                            torrent.ImdbId =
                                Int32.Parse(new String(tvShow.ExternalIds.ImdbId.Where(Char.IsDigit).ToArray()));
                            iDelayed.Add(torrent);
                        }
                    }
                }

                SearchingPanel.Visibility = Visibility.Collapsed;

                if (!isUnloaded)
                {
                    TorrentsDisplay.ItemsSource = items;
                }
                isItemLoadingFinished = true;
                SortByComboBox.IsEnabled = true;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public int torrentCollectionCurrentPage = 0;
        public bool sortByComboboxSelectionChanged = false;
        private SortTorrentsType GetSortType()
        {
            var selectedSort = SortByComboBox.SelectedItem;

            if (String.Equals(selectedSort, App.Current.Resources["AscendingBySize"].ToString()))
            {
                return SortTorrentsType.AscendingBySize;
            }
            else if (String.Equals(selectedSort, App.Current.Resources["DescendingBySize"].ToString()))
            {
                return SortTorrentsType.DescendingBySize;
            }
            else if (String.Equals(selectedSort, App.Current.Resources["AscendingBySeeders"].ToString()))
            {
                return SortTorrentsType.AscendingBySeeders;
            }
            else if (String.Equals(selectedSort, App.Current.Resources["DescendingBySeeders"].ToString()))
            {
                return SortTorrentsType.DescendingBySeeders;
            }
            else if (String.Equals(selectedSort, App.Current.Resources["AscendingByDate"].ToString()))
            {
                return SortTorrentsType.AscendingByDate;
            }
            else if (String.Equals(selectedSort, App.Current.Resources["DescendingByDate"].ToString()))
            {
                return SortTorrentsType.DescendingByDate;
            }
            else if (String.Equals(selectedSort, App.Current.Resources["AscendingByVideoQuality"].ToString()))
            {
                return SortTorrentsType.AscendingByVideoQuality;
            }
            else if (String.Equals(selectedSort, App.Current.Resources["DescendingByVideoQuality"].ToString()))
            {
                return SortTorrentsType.DescendingByVideoQuality;
            }
            else
            {
                return SortTorrentsType.Undefined;
            }
        }
        public FastObservableCollection<Item> SortedCollection = new FastObservableCollection<Item>();
        public bool isSortedCollectionLoadingFinished = false;
        public async Task LoadMoreTorrentCollection()
        {
            try
            {
                isSortedCollectionLoadingFinished = false;
                if (selectedMovie.ShowType == ShowType.Movie && torrentCollectionCurrentPage <= movieTorrentsMaxPage)
                {
                    torrentCollectionCurrentPage++;
                    SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                }
                else if (selectedMovie.ShowType == ShowType.TvShow && torrentCollectionCurrentPage <= tvTorrentsMaxPage)
                {
                    torrentCollectionCurrentPage++;
                    SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                }

                isSortedCollectionLoadingFinished = true;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void SortByComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                SortedCollection.Clear();
                var sortType = GetSortType();
                switch (sortType)
                {
                    case SortTorrentsType.AscendingBySize:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new FastObservableCollection<Item>(torrentCollection.OrderBy(x => x.Size).ToList());
                        SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.DescendingBySize:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new FastObservableCollection<Item>(torrentCollection.OrderByDescending(x => x.Size).ToList());
                        SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.AscendingBySeeders:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new FastObservableCollection<Item>(torrentCollection.OrderBy(x => x.Seeders).ToList());
                        SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.DescendingBySeeders:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new FastObservableCollection<Item>(torrentCollection.OrderByDescending(x => x.Seeders).ToList());
                        SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.AscendingByDate:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new FastObservableCollection<Item>(torrentCollection.OrderBy(x => x.PubDate).ToList());
                        SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.DescendingByDate:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new FastObservableCollection<Item>(torrentCollection.OrderByDescending(x => x.PubDate).ToList());
                        SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.AscendingByVideoQuality:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        if (selectedMovie.ShowType == ShowType.Movie)
                        {
                            torrentCollection = new FastObservableCollection<Item>(torrentCollection.OrderByDescending(x => x.Category.Any(z=> z == 2030))
                                .ThenBy(x=> x.Size)
                                .ThenBy(x=> x.Category.Any(z=> z== 2040))
                                .ThenBy(x=>x.Size)
                                .ThenBy(x=> x.Category.Any(z=> z== 2045))
                                .ThenBy(x=> x.Size)
                                .ToList());
                            SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                            TorrentsDisplay.ItemsSource = SortedCollection;
                            isSortedCollectionLoadingFinished = true;
                        }
                        else
                        {
                            torrentCollection = new FastObservableCollection<Item>(torrentCollection
                                .OrderByDescending(x => x.Category.Any(z => z == 5030))
                                .ThenBy(x => x.Size)
                                .ThenBy(x => x.Category.Any(z => z == 5040))
                                .ThenBy(x => x.Size)
                                .ThenBy(x => x.Category.Any(z => z == 5045))
                                .ThenBy(x => x.Size)
                                .ToList());
                            SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                            TorrentsDisplay.ItemsSource = SortedCollection;
                            isSortedCollectionLoadingFinished = true;
                        }
                        break;
                    case SortTorrentsType.DescendingByVideoQuality:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        if (selectedMovie.ShowType == ShowType.Movie)
                        {
                            torrentCollection = new FastObservableCollection<Item>(torrentCollection.OrderByDescending(x => x.Category.Any(z => z == 2045))
                                .ThenByDescending(x=> x.Size)
                                .ThenByDescending(x => x.Category.Any(z => z == 2040))
                                .ThenByDescending(x=> x.Size)
                                .ThenByDescending(x => x.Category.Any(z => z == 2030))
                                .ThenByDescending(x=> x.Size).ToList());
                            SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                            TorrentsDisplay.ItemsSource = SortedCollection;
                            isSortedCollectionLoadingFinished = true;
                        }
                        else
                        {
                            torrentCollection = new FastObservableCollection<Item>(torrentCollection
                                .OrderByDescending(x => x.Category.Any(z => z == 5045))
                                .ThenByDescending(x => x.Size)
                                .ThenByDescending(x => x.Category.Any(z => z == 5040))
                                .ThenByDescending(x => x.Size)
                                .ThenByDescending(x => x.Category.Any(z => z == 5030))
                                .ThenByDescending(x => x.Size).ToList());
                            SortedCollection.AddRange(torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize));
                            TorrentsDisplay.ItemsSource = SortedCollection;
                            isSortedCollectionLoadingFinished = true;
                        }
                        break;
                    case SortTorrentsType.Undefined:
                        break;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private bool isUnloaded = false;
        private void TorrentsPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            isUnloaded = true;
            MovieDetailsPage.TorrentsPage = null;
        }
    }

    public enum SortTorrentsType
    {
        AscendingBySize,
        DescendingBySize,
        AscendingBySeeders,
        DescendingBySeeders,
        AscendingByDate,
        DescendingByDate,
        AscendingByVideoQuality,
        DescendingByVideoQuality,
        Undefined
    }
}
