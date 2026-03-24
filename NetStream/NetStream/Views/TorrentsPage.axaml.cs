using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetStream.Services;
using Serilog;
using TMDbLib.Objects.Discover;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;

namespace NetStream.Views
{
    public partial class TorrentsPage : UserControl
    {
        public Movie selectedMovie;
        public int seasonNumber;
        public int episodeNumber;
        private List<string> SortComboBoxItems = new List<string>()
        {
            ResourceProvider.GetString("AscendingBySize").ToString(),
            ResourceProvider.GetString("DescendingBySize").ToString(),
            ResourceProvider.GetString("AscendingBySeeders").ToString(),
            ResourceProvider.GetString("DescendingBySeeders").ToString(),
            ResourceProvider.GetString("AscendingByDate").ToString(),
            ResourceProvider.GetString("DescendingByDate").ToString(),
            ResourceProvider.GetString("AscendingByVideoQuality").ToString(),
            ResourceProvider.GetString("DescendingByVideoQuality").ToString()
        };

        private ObservableCollection<Item> items = new ObservableCollection<Item>();
        private List<Item> torrents;
        private ObservableCollection<Item> torrentCollection = new ObservableCollection<Item>();
        private Random rnd = new Random();
        private TMDbLib.Objects.Movies.Movie movie;
        private ImagesWithId images;
        private PosterImages images2 = null;
        private TvShow tvShow;
        private bool isUnloaded = false;
        public bool isItemLoadingFinished = false;
        public bool isSortedCollectionLoadingFinished = false;
        public bool sortByComboboxSelectionChanged = false;
        private bool hasFirstResult = false;

        public int pageSize = 50;
        public int movieTorrentsCurrentPage = 0;
        public int movieTorrentsMaxPage = 0;
        public bool loadingMovieTorrentsFinished = false;
        public int tvTorrentsCurrentPage = 0;
        public int tvTorrentsMaxPage = 0;
        public bool loadingtvTorrentsFinished = false;
        public int torrentCollectionCurrentPage = 0;
        public ObservableCollection<Item> SortedCollection = new ObservableCollection<Item>();
        
        public TorrentsPage()
        {
            InitializeComponent();
        }

        public TorrentsPage(Movie selectedMovie)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
            SortByComboBox.ItemsSource = SortComboBoxItems;
            SortByComboBox.SelectedIndex = -1;
            MovieDetailsPage.TorrentsPage = this;
        }

        public TorrentsPage(Movie selectedMovie, int seasonNumber, int episodeNumber)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
            this.seasonNumber = seasonNumber;
            this.episodeNumber = episodeNumber;
            SortByComboBox.ItemsSource = SortComboBoxItems;
            SortByComboBox.SelectedIndex = -1;
            MovieDetailsPage.TorrentsPage = this;
        }

        public TorrentsPage(Movie selectedMovie, int seasonNumber)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
            this.seasonNumber = seasonNumber;
            this.episodeNumber = -1;
            SortByComboBox.ItemsSource = SortComboBoxItems;
            SortByComboBox.SelectedIndex = -1;
            MovieDetailsPage.TorrentsPage = this;
        }

        private async void TorrentsDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (PlatformDetector.IsDesktop())
                {
                    Item selectedTorrent = TorrentsDisplay.SelectedItem as Item;
                    if (selectedTorrent != null)
                    {
                        if (DownloadsPage.Instance != null)
                        {
                            DownloadsPage.torrents.Add(selectedTorrent);
                            
                            var mainView = this.FindAncestorOfType<MainView>();
                            if (mainView != null)
                            {
                                mainView.SetContent(DownloadsPage.Instance);
                            }
                        
                            await DownloadsPage.Instance.StartTorrenting2(selectedTorrent);
                        }
                    }
                }
                else if (PlatformDetector.IsMobile())
                {
                    //TODO
                }
                
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TorrentsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Implement scroll handling if needed
        }

        public readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        public int GetMaxPage(int totalItems)
        {
            return (int)Math.Ceiling((double)totalItems / pageSize);
        }

        private async void TorrentsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                isUnloaded = false;

                if (torrentCollection.Count > 0 || items.Count > 0)
                {
                     SearchingPanel.IsVisible = false;
                     TorrentsDisplay.IsVisible = true;
                     if (TorrentsDisplay.ItemsSource == null)
                        TorrentsDisplay.ItemsSource = torrentCollection;
                     return;
                }

                SortByComboBox.IsEnabled = false;
                isItemLoadingFinished = false;
                hasFirstResult = false;
                torrentCollection.Clear();
                items.Clear();
                
                // Başlangıç UI durumunu ayarla
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SearchingPanel.IsVisible = true;
                    TorrentsDisplay.IsVisible = false;
                    TorrentsDisplay.ItemsSource = null;
                });
                
                if (selectedMovie.ShowType == ShowType.Movie)
                {
                    movie = await Service.client.GetMovieAsync(selectedMovie.Id, "en", null, MovieMethods.Images);
                    images = await Service.client.GetMovieImagesAsync(selectedMovie.Id);
                    
                    torrents = await JackettService.GetMovieTorrentsImdb(movie.ImdbId);
                    
                    // İlk IMDb sonuçlarını hazırla ve ekle
                    foreach (var torrent in torrents)
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
                        torrent.SeedersProperty = torrent.Seeders + " " + ResourceProvider.GetString("Seeders");
                        torrent.ImdbId = Int32.Parse(new String(movie.ImdbId.Where(Char.IsDigit).ToArray()));
                        torrentCollection.Add(torrent);
                    }
                    
                    // İlk sonuçlar varsa UI'ı güncelle
                    if (torrentCollection.Count > 0 && !hasFirstResult)
                    {
                        hasFirstResult = true;
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (!isUnloaded)
                            {
                                TorrentsDisplay.ItemsSource = torrentCollection;
                                SearchingPanel.IsVisible = false;
                                TorrentsDisplay.IsVisible = true;
                            }
                            TorrentCount.Text = torrentCollection.Count + " " + ResourceProvider.GetString("Results");
                        });
                    }
                    
                    int totalCount = torrents.Count;
                    
                    // GetMovieTorrentsNameAsync ile anında sonuçları ekle
                    await foreach (var torrent in JackettService.GetMovieTorrentsNameAsync(movie.Title, movie.ReleaseDate.HasValue ? movie.ReleaseDate.Value.Year : 0))
                    {
                        torrents.Add(torrent);
                        totalCount++;
                        
                        // Torrent bilgilerini hazırla ve ekle
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
                        torrent.SeedersProperty = torrent.Seeders + " " + ResourceProvider.GetString("Seeders");
                        torrent.ImdbId = Int32.Parse(new String(movie.ImdbId.Where(Char.IsDigit).ToArray()));
                        
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            torrentCollection.Add(torrent);
                            
                            // İlk sonuç geldiğinde UI'ı göster
                            if (!hasFirstResult)
                            {
                                hasFirstResult = true;
                                if (!isUnloaded)
                                {
                                    TorrentsDisplay.ItemsSource = torrentCollection;
                                    SearchingPanel.IsVisible = false;
                                    TorrentsDisplay.IsVisible = true;
                                }
                            }
                            
                            TorrentCount.Text = totalCount + " " + ResourceProvider.GetString("Results");
                        });
                    }
                    
                    movieTorrentsMaxPage = GetMaxPage(torrents.Count);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        TorrentCount.Text = torrents.Count + " " + ResourceProvider.GetString("Results");
                    });

                    await LoadMovieTorrents();
                }
                else if (selectedMovie.ShowType == ShowType.TvShow)
                {
                    tvShow = await Service.client.GetTvShowAsync(selectedMovie.Id, TvShowMethods.ExternalIds, "en");
                    
                    if (episodeNumber == -1)
                    {
                        images2 = await Service.client.GetTvSeasonImagesAsync(selectedMovie.Id, seasonNumber);
                        torrents = await JackettService.GetTvSeasonTorrents(tvShow.Name, seasonNumber);
                        
                        // Sezon torrentlerini hazırla ve ekle
                        foreach (var torrent in torrents)
                        {
                            var image = Service.client
                                .GetImageUrl(
                                    Service.client.Config.Images.PosterSizes.Last(),
                                    images2.Posters.FirstOrDefault().FilePath).AbsoluteUri;
                            torrent.Poster = (image);
                            torrent.ImageUrl = image;
                            torrent.PublishDate = torrent.PubDate.ToShortDateString();
                            torrent.SizeProperty = SizeSuffix((Int64)torrent.Size);
                            torrent.MovieId = selectedMovie.Id;
                            torrent.MovieName = tvShow.Name;
                            torrent.SeasonNumber = seasonNumber;
                            torrent.EpisodeNumber = episodeNumber;
                            torrent.ShowType = ShowType.TvShow;
                            torrent.SeedersProperty = torrent.Seeders + " " + ResourceProvider.GetString("Seeders");
                            torrent.ImdbId =
                                Int32.Parse(new String(tvShow.ExternalIds.ImdbId.Where(Char.IsDigit).ToArray()));
                            torrentCollection.Add(torrent);
                        }
                        
                        // İlk sonuçlar varsa UI'ı güncelle
                        if (torrentCollection.Count > 0 && !hasFirstResult)
                        {
                            hasFirstResult = true;
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (!isUnloaded)
                                {
                                    TorrentsDisplay.ItemsSource = torrentCollection;
                                    SearchingPanel.IsVisible = false;
                                    TorrentsDisplay.IsVisible = true;
                                }
                                TorrentCount.Text = torrentCollection.Count + " " + ResourceProvider.GetString("Results");
                            });
                        }
                    }
                    else
                    {
                        images = await Service.client.GetTvShowImagesAsync(selectedMovie.Id);
                        torrents = new List<Item>();
                        
                        // GetTvEpisodeTorrentsAsync ile anında sonuçları ekle
                        int totalCount = 0;
                        await foreach (var torrent in JackettService.GetTvEpisodeTorrentsAsync(tvShow.Name, seasonNumber, episodeNumber))
                        {
                            torrents.Add(torrent);
                            totalCount++;
                            
                            // Torrent bilgilerini hazırla ve ekle
                            int randomNumber = rnd.Next(0, images.Backdrops.Count - 1);
                            var image = Service.client
                                .GetImageUrl(
                                    Service.client.Config.Images.BackdropSizes
                                        [Service.client.Config.Images.BackdropSizes.Count - 4],
                                    images.Backdrops[randomNumber].FilePath).AbsoluteUri;
                            torrent.Poster = (image);
                            torrent.ImageUrl = image;
                            torrent.PublishDate = torrent.PubDate.ToShortDateString();
                            torrent.SizeProperty = SizeSuffix((Int64)torrent.Size);
                            torrent.MovieId = selectedMovie.Id;
                            torrent.MovieName = tvShow.Name + " S" + seasonNumber + "E" + episodeNumber;
                            torrent.SeasonNumber = seasonNumber;
                            torrent.EpisodeNumber = episodeNumber;
                            torrent.ShowType = ShowType.TvShow;
                            torrent.SeedersProperty = torrent.Seeders + " " + ResourceProvider.GetString("Seeders");
                            torrent.ImdbId =
                                Int32.Parse(new String(tvShow.ExternalIds.ImdbId.Where(Char.IsDigit).ToArray()));
                            
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                torrentCollection.Add(torrent);
                                
                                // İlk sonuç geldiğinde UI'ı göster
                                if (!hasFirstResult)
                                {
                                    hasFirstResult = true;
                                    if (!isUnloaded)
                                    {
                                        TorrentsDisplay.ItemsSource = torrentCollection;
                                        SearchingPanel.IsVisible = false;
                                        TorrentsDisplay.IsVisible = true;
                                    }
                                }
                                
                                TorrentCount.Text = totalCount + " " + ResourceProvider.GetString("Results");
                            });
                        }
                    }

                    tvTorrentsMaxPage = GetMaxPage(torrents.Count);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        TorrentCount.Text = torrents.Count + " " + ResourceProvider.GetString("Results");
                    });

                    await LoadTvShows();
                }

                // Tüm arama bittiğinde UI'ı güncelle (eğer hala görünür değilse)
                if (!hasFirstResult)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (!isUnloaded)
                        {
                            SearchingPanel.IsVisible = false;
                            if (torrentCollection.Count > 0)
                            {
                                TorrentsDisplay.ItemsSource = torrentCollection;
                                TorrentsDisplay.IsVisible = true;
                            }
                        }
                    });
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SearchingPanel.IsVisible = false;
                        if (!isUnloaded && TorrentsDisplay.ItemsSource == null)
                        {
                            TorrentsDisplay.ItemsSource = torrentCollection;
                            TorrentsDisplay.IsVisible = true;
                        }
                    });
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

        private void TorrentsPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            isUnloaded = true;
            MovieDetailsPage.TorrentsPage = null;
        }

        private SortTorrentsType GetSortType()
        {
            var selectedSort = SortByComboBox.SelectedItem as string;

            if (String.Equals(selectedSort, ResourceProvider.GetString("AscendingBySize").ToString()))
            {
                return SortTorrentsType.AscendingBySize;
            }
            else if (String.Equals(selectedSort, ResourceProvider.GetString("DescendingBySize").ToString()))
            {
                return SortTorrentsType.DescendingBySize;
            }
            else if (String.Equals(selectedSort, ResourceProvider.GetString("AscendingBySeeders").ToString()))
            {
                return SortTorrentsType.AscendingBySeeders;
            }
            else if (String.Equals(selectedSort, ResourceProvider.GetString("DescendingBySeeders").ToString()))
            {
                return SortTorrentsType.DescendingBySeeders;
            }
            else if (String.Equals(selectedSort, ResourceProvider.GetString("AscendingByDate").ToString()))
            {
                return SortTorrentsType.AscendingByDate;
            }
            else if (String.Equals(selectedSort, ResourceProvider.GetString("DescendingByDate").ToString()))
            {
                return SortTorrentsType.DescendingByDate;
            }
            else if (String.Equals(selectedSort, ResourceProvider.GetString("AscendingByVideoQuality").ToString()))
            {
                return SortTorrentsType.AscendingByVideoQuality;
            }
            else if (String.Equals(selectedSort, ResourceProvider.GetString("DescendingByVideoQuality").ToString()))
            {
                return SortTorrentsType.DescendingByVideoQuality;
            }
            else
            {
                return SortTorrentsType.Undefined;
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
                        torrentCollection = new ObservableCollection<Item>(torrentCollection.OrderBy(x => x.Size).ToList());
                        foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                        {
                            SortedCollection.Add(VARIABLE);
                        }
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.DescendingBySize:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new ObservableCollection<Item>(torrentCollection.OrderByDescending(x => x.Size).ToList());
                        foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                        {
                            SortedCollection.Add(VARIABLE);
                        }
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.AscendingBySeeders:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new ObservableCollection<Item>(torrentCollection.OrderBy(x => x.Seeders).ToList());
                        foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                        {
                            SortedCollection.Add(VARIABLE);
                        }
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.DescendingBySeeders:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new ObservableCollection<Item>(torrentCollection.OrderByDescending(x => x.Seeders).ToList());
                        foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                        {
                            SortedCollection.Add(VARIABLE);
                        }
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.AscendingByDate:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new ObservableCollection<Item>(torrentCollection.OrderBy(x => x.PubDate).ToList());
                        foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                        {
                            SortedCollection.Add(VARIABLE);
                        }
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.DescendingByDate:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        torrentCollection = new ObservableCollection<Item>(torrentCollection.OrderByDescending(x => x.PubDate).ToList());
                        foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                        {
                            SortedCollection.Add(VARIABLE);
                        }
                        TorrentsDisplay.ItemsSource = SortedCollection;
                        isSortedCollectionLoadingFinished = true;
                        break;
                    case SortTorrentsType.AscendingByVideoQuality:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        if (selectedMovie.ShowType == ShowType.Movie)
                        {
                            torrentCollection = new ObservableCollection<Item>(torrentCollection.OrderByDescending(x => x.Category.Any(z => z == 2030))
                                .ThenBy(x => x.Size)
                                .ThenBy(x => x.Category.Any(z => z == 2040))
                                .ThenBy(x => x.Size)
                                .ThenBy(x => x.Category.Any(z => z == 2045))
                                .ThenBy(x => x.Size)
                                .ToList());
                            foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                            {
                                SortedCollection.Add(VARIABLE);
                            }
                            TorrentsDisplay.ItemsSource = SortedCollection;
                            isSortedCollectionLoadingFinished = true;
                        }
                        else
                        {
                            torrentCollection = new ObservableCollection<Item>(torrentCollection
                                .OrderByDescending(x => x.Category.Any(z => z == 5030))
                                .ThenBy(x => x.Size)
                                .ThenBy(x => x.Category.Any(z => z == 5040))
                                .ThenBy(x => x.Size)
                                .ThenBy(x => x.Category.Any(z => z == 5045))
                                .ThenBy(x => x.Size)
                                .ToList());
                            foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                            {
                                SortedCollection.Add(VARIABLE);
                            }
                            TorrentsDisplay.ItemsSource = SortedCollection;
                            isSortedCollectionLoadingFinished = true;
                        }
                        break;
                    case SortTorrentsType.DescendingByVideoQuality:
                        sortByComboboxSelectionChanged = true;
                        torrentCollectionCurrentPage = 0;
                        if (selectedMovie.ShowType == ShowType.Movie)
                        {
                            torrentCollection = new ObservableCollection<Item>(torrentCollection.OrderByDescending(x => x.Category.Any(z => z == 2045))
                                .ThenByDescending(x => x.Size)
                                .ThenByDescending(x => x.Category.Any(z => z == 2040))
                                .ThenByDescending(x => x.Size)
                                .ThenByDescending(x => x.Category.Any(z => z == 2030))
                                .ThenByDescending(x => x.Size).ToList());
                            foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                            {
                                SortedCollection.Add(VARIABLE);
                            }
                            TorrentsDisplay.ItemsSource = SortedCollection;
                            isSortedCollectionLoadingFinished = true;
                        }
                        else
                        {
                            torrentCollection = new ObservableCollection<Item>(torrentCollection
                                .OrderByDescending(x => x.Category.Any(z => z == 5045))
                                .ThenByDescending(x => x.Size)
                                .ThenByDescending(x => x.Category.Any(z => z == 5040))
                                .ThenByDescending(x => x.Size)
                                .ThenByDescending(x => x.Category.Any(z => z == 5030))
                                .ThenByDescending(x => x.Size).ToList());
                            foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                            {
                                SortedCollection.Add(VARIABLE);
                            }
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
                            torrent.SeedersProperty = torrent.Seeders + " " + ResourceProvider.GetString("Seeders");
                            torrent.ImdbId = Int32.Parse(new String(movie.ImdbId.Where(Char.IsDigit).ToArray()));
                            items.Add(torrent);
                        
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
                            torrent.SeedersProperty = torrent.Seeders + " " + ResourceProvider.GetString("Seeders");
                            torrent.ImdbId =
                                Int32.Parse(new String(tvShow.ExternalIds.ImdbId.Where(Char.IsDigit).ToArray()));
                            items.Add(torrent);
                        
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

        public async Task LoadMoreTorrentCollection()
        {
            try
            {
                isSortedCollectionLoadingFinished = false;
                if (selectedMovie.ShowType == ShowType.Movie && torrentCollectionCurrentPage <= movieTorrentsMaxPage)
                {
                    torrentCollectionCurrentPage++;
                    foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                    {
                        SortedCollection.Add(VARIABLE);
                    }
                }
                else if (selectedMovie.ShowType == ShowType.TvShow && torrentCollectionCurrentPage <= tvTorrentsMaxPage)
                {
                    torrentCollectionCurrentPage++;
                    foreach (var VARIABLE in torrentCollection.Skip(torrentCollectionCurrentPage * pageSize).Take(pageSize))
                    {
                        SortedCollection.Add(VARIABLE);
                    }
                }

                isSortedCollectionLoadingFinished = true;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
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