
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
using System.Windows.Threading;
using Path = System.IO.Path;
using YoutubeExplode.Channels;
using Windows.Media.Protection.PlayReady;
using System.Security.Policy;
using HandyControl.Controls;
using HandyControl.Data;
using NetStream.Properties;
using Serilog;
using MessageBox = System.Windows.MessageBox;
using TMDbLib.Objects.Movies;
using System.Globalization;
using BencodeNET.Torrents;
using NETCore.Encrypt;
using OpenCvSharp.ImgHash;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for DownloadsPageQ.xaml
    /// </summary>
    public partial class DownloadsPageQ : Page
    {
        public static DownloadsPageQ GetDownloadsPageInstance = null;
        public static FastObservableCollection<Item> torrents = new FastObservableCollection<Item>();
        private List<Torrent> savedTorrents;

        public DownloadsPageQ()
        {
            InitializeComponent();
            OnLoad();
            Log.Information("Loaded Downloads Page");
        }

        private async void OnLoad()
        {
            DownloadsDisplay.ItemsSource = torrents;
            if (torrents.Count == 0)
                await LoadTorrents();
            await StartTorrenting();
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
        private async Task LoadTorrents()
        {
            try
            {
                var js = await File.ReadAllTextAsync(AppSettingsManager.appSettings.downloadingTorrentsJson);
                if (!String.IsNullOrWhiteSpace(js))
                    savedTorrents = JsonConvert.DeserializeObject<List<Torrent>>(js);
                if (savedTorrents != null)
                {
                    foreach (var savedTorrent in savedTorrents)
                    {
                        var item = new Item();
                        item.Poster = (savedTorrent.ImageUrl);
                        item.Title = savedTorrent.Name;
                        item.SizeProperty = SizeSuffix((Int64)savedTorrent.Size);
                        item.Size = savedTorrent.Size;
                        item.Link = savedTorrent.DownloadLink;
                        item.PublishDate = savedTorrent.PublishDate;
                        item.ImageUrl = savedTorrent.ImageUrl;
                        item.MovieId = savedTorrent.MovieId;
                        item.MovieName = savedTorrent.MovieName;
                        item.IsCompleted = savedTorrent.IsCompleted;
                        item.ShowType = (ShowType)savedTorrent.ShowType;
                        item.SeasonNumber = savedTorrent.SeasonNumber;
                        item.EpisodeNumber = savedTorrent.EpisodeNumber;
                        item.TorrentLocation = savedTorrent.TorrentLocation;
                        item.ImdbId = savedTorrent.ImdbId;
                        item.Magnet = savedTorrent.Magnet;
                        item.ContainingDirectory = savedTorrent.ContainingDirectory;
                        item.FileNames = savedTorrent.FileNames;
                        item.Hash = savedTorrent.Hash;
                        torrents.Add(item);
                    }
                    Log.Information($"Loaded {torrents.Count} torrents");
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
 
        public static double JaroWinklerSimilarity(string s1, string s2)
        {
            int matchDistance = Math.Max(s1.Length, s2.Length) / 2 - 1;
            bool[] s1Matches = new bool[s1.Length];
            bool[] s2Matches = new bool[s2.Length];

            int matches = 0;
            int transpositions = 0;

            // Matches
            for (int i = 0; i < s1.Length; i++)
            {
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, s2.Length);

                for (int j = start; j < end; j++)
                {
                    if (s2Matches[j]) continue;
                    if (s1[i] != s2[j]) continue;
                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0) return 0;

            // Transpositions
            int k = 0;
            for (int i = 0; i < s1.Length; i++)
            {
                if (!s1Matches[i]) continue;
                while (!s2Matches[k]) k++;
                if (s1[i] != s2[k]) transpositions++;
                k++;
            }

            transpositions /= 2;

            double similarity = ((double)matches / s1.Length +
                                 (double)matches / s2.Length +
                                 (double)(matches - transpositions) / matches) / 3.0;

            // Winkler adjustment
            int prefix = 0;
            for (int i = 0; i < Math.Min(s1.Length, s2.Length); i++)
            {
                if (s1[i] != s2[i]) break;
                prefix++;
            }

            return similarity + 0.1 * prefix * (1 - similarity);
        }
        public List<string> openedTorrents = new List<string>();
        private async void DownloadsDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedTorrent = DownloadsDisplay.SelectedItem as Item;
                if (selectedTorrent == null) return;
                DownloadsDisplay.UnselectAll();
                var files = await Task.Run((() => Libtorrent.GetFiles(selectedTorrent))) ;
                if (selectedTorrent.EpisodeNumber == -1 || files.Count(x => x.IsMediaFile) > 1)
                {
                    if (selectedTorrent.Hash == null || String.IsNullOrWhiteSpace(selectedTorrent.Hash))
                    {
                        if (String.IsNullOrWhiteSpace(selectedTorrent.TorrentLocation) &&
                            String.IsNullOrWhiteSpace(selectedTorrent.Magnet))
                        {
                            if (selectedTorrent.Link == torrents.Last().Link)
                            {
                                var allTorrents =  await Libtorrent.client.GetTorrentsAsync();
                                var sorted = allTorrents.OrderByDescending(x => x.AddedOn).FirstOrDefault();
                                var hash = sorted.Hash;
                                selectedTorrent.Hash = hash;
                                SaveAllTorrents();

                                var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId, selectedTorrent.MovieName,
                                    selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                                this.NavigationService.Navigate(filesPage);
                            }
                            else
                            {
                                var allTorrents = await Libtorrent.client.GetTorrentsAsync();
                                var current = allTorrents.OrderByDescending(s => JaroWinklerSimilarity(s.Name, selectedTorrent.Title)).First(); 
                                if (current != null)
                                {
                                    var hash = current.Hash;
                                    selectedTorrent.Hash = hash;
                                    SaveAllTorrents();

                                    var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId,
                                        selectedTorrent.MovieName,
                                        selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                                    this.NavigationService.Navigate(filesPage);
                                }
                            }
                        }
                        else
                        {
                            if (String.IsNullOrWhiteSpace(selectedTorrent.Magnet)
                                    ? await Libtorrent.IsTorrentExistPath(selectedTorrent.TorrentLocation)
                                    : await Libtorrent.IsTorrentExistUrl(selectedTorrent.Magnet))
                            {
                                var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId,
                                    selectedTorrent.MovieName,
                                    selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                                this.NavigationService.Navigate(filesPage);
                            }
                            else
                            {
                                await StartTorrenting(selectedTorrent);
                                var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId,
                                    selectedTorrent.MovieName,
                                    selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                                this.NavigationService.Navigate(filesPage);
                            }
                        }
                    }
                    else
                    {
                        bool isExist =
                            (await Libtorrent.client.GetTorrentsAsync()).Any(x =>
                                x.Hash.ToLower() == selectedTorrent.Hash.ToLower());
                        if (isExist)
                        {
                            var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId, selectedTorrent.MovieName,
                                selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                            this.NavigationService.Navigate(filesPage);
                        }
                        else
                        {
                            await StartTorrenting(selectedTorrent);
                            var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId, selectedTorrent.MovieName,
                                selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                            this.NavigationService.Navigate(filesPage);
                        }
                    }
                }
                else
                {
                    if (!(openedTorrents.Any(x => x == selectedTorrent.Link)))
                    {
                        if (!String.IsNullOrWhiteSpace(selectedTorrent.Hash))
                        {
                            Log.Information("Torrent hash is not empty");
                            bool isExist =
                                (await Libtorrent.client.GetTorrentsAsync()).Any(x =>
                                    x.Hash.ToLower() == selectedTorrent.Hash.ToLower());

                            if (isExist)
                            {
                                var mediaFile = files.FirstOrDefault(x => x.IsMediaFile);
                                Log.Information("Torrent already exist. Opening player");
                                if (mediaFile != null)
                                {
                                    var playerWindow = new PlayerWindow(selectedTorrent.MovieId, selectedTorrent.MovieName,
                                        selectedTorrent.ShowType, selectedTorrent.SeasonNumber, selectedTorrent.EpisodeNumber,
                                        new FileInfo(mediaFile.FullPath), selectedTorrent.IsCompleted, selectedTorrent.ImdbId, selectedTorrent, mediaFile.Index,selectedTorrent.Poster);
                                    playerWindow.Show();
                                    openedTorrents.Add(selectedTorrent.Link);
                                }
                                else
                                {
                                    new CustomMessageBox("There is no media to play.", MessageType.Error,
                                        MessageButtons.Ok).ShowDialog();
                                }
                            }
                            else
                            {
                                Log.Information("Torrent doesnt exist. Started torrenting");
                                await StartTorrenting(selectedTorrent);
                                var mediaFile = files.FirstOrDefault(x => x.IsMediaFile);
                                var playerWindow = new PlayerWindow(selectedTorrent.MovieId, selectedTorrent.MovieName,
                                    selectedTorrent.ShowType, selectedTorrent.SeasonNumber, selectedTorrent.EpisodeNumber,
                                    new FileInfo(mediaFile.FullPath), selectedTorrent.IsCompleted, selectedTorrent.ImdbId, selectedTorrent, mediaFile.Index,selectedTorrent.Poster);
                                playerWindow.Show();
                                openedTorrents.Add(selectedTorrent.Link);
                            }
                        }
                        else
                        {
                            Log.Information("Torrent hash is empty");
                            await StartTorrenting(selectedTorrent);
                            Log.Information("Torrent hash found: " + selectedTorrent.Hash);
                            var mediaFile = files.FirstOrDefault(x => x.IsMediaFile);
                            Log.Information("Opening player" );
                            if (mediaFile != null)
                            {
                                var playerWindow = new PlayerWindow(selectedTorrent.MovieId, selectedTorrent.MovieName,
                                    selectedTorrent.ShowType, selectedTorrent.SeasonNumber, selectedTorrent.EpisodeNumber,
                                    new FileInfo(mediaFile.FullPath), selectedTorrent.IsCompleted, selectedTorrent.ImdbId,
                                    selectedTorrent, mediaFile.Index,selectedTorrent.Poster);
                                playerWindow.Show();
                                openedTorrents.Add(selectedTorrent.Link);
                            }
                            else
                            {
                                new CustomMessageBox("There is no media to play.", MessageType.Error,
                                    MessageButtons.Ok).ShowDialog();
                            }
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


        private void DownloadsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {

        }

        private async void DownloadsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            //if (torrentCheckTimer == null)
            //{
            //    torrentCheckTimer = new DispatcherTimer();
            //    torrentCheckTimer.Interval = TimeSpan.FromMilliseconds(1000);
            //    torrentCheckTimer.Tick += TorrentCheckTimerOnTick;
            //    torrentCheckTimer.Start();
            //}

            _=Task.Run((async () =>
            {
                while (true)
                {
                    try
                    {
                        foreach (var item in torrents)
                        {
                            if (!item.IsCompleted && item.Hash != null && !(finishedTorrents.Any(x => x == item.Hash)))
                            {
                                await Libtorrent.ObserveChanges(item);
                                if (item.IsCompleted)
                                {
                                    finishedTorrents.Add(item.Hash);
                                    SaveAllTorrents();
                                    await Libtorrent.Pause(item);
                                    Growl.SuccessGlobal(new GrowlInfo()
                                    {
                                        Message = item.Title + " " +
                                                  App.Current.Resources["SuccessNotificationTorrentDownloaded"],
                                        StaysOpen = false,
                                        WaitTime = 5
                                    });
                                }
                                //else
                                //{
                                //    if (MoreThanOneMediaFileTorrentsTvShow.Any(x => x == item.Hash))
                                //    {
                                //        var currentTorrent = await Libtorrent.GetTorrentState(item);
                                //        if (currentTorrent == TorrentState.Downloading)
                                //        {
                                //            await Libtorrent.ChangeTvShowEpisodeFilePriorities(item);
                                //        }
                                //    }
                                //    else
                                //    {
                                //        var files = await Libtorrent.GetFiles(item);

                                //        if (item.ShowType == ShowType.TvShow && files.Count(x => x.IsMediaFile) > 1)
                                //        {
                                //            MoreThanOneMediaFileTorrentsTvShow.Add(item.Hash);
                                //        }
                                //    }
                                //}
                            }
                            else if (item.IsCompleted && !(finishedTorrents.Any(x => x == item.Hash)))
                            {
                                var currentTorrent = await Libtorrent.GetTorrentState(item);
                                if (currentTorrent == TorrentState.Seeding)
                                {
                                    await Libtorrent.Pause(item);
                                    finishedTorrents.Add(item.Hash);
                                }
                            }
                        }
                    }
                    catch (System.Exception exception)
                    {
                        var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                        Log.Error(errorMessage);
                    }

                    await Task.Delay(200);
                }
            }));
            SaveAllTorrents();
            UpdateUI();
        }


        private void SaveAllTorrents()
        {
            try
            {
                List<Torrent> json = new List<Torrent>();

                if (torrents.Count > 0)
                {
                    foreach (var item in torrents)
                    {
                        json.Add(new Torrent()
                        {
                            DownloadLink = item.Link,
                            ImageUrl = item.ImageUrl,
                            Name = item.Title,
                            PublishDate = item.PublishDate,
                            Size = item.Size,
                            MovieId = item.MovieId,
                            MovieName = item.MovieName,
                            IsCompleted = item.IsCompleted,
                            ShowType = (int)item.ShowType,
                            EpisodeNumber = item.EpisodeNumber,
                            SeasonNumber = item.SeasonNumber,
                            TorrentLocation = item.TorrentLocation,
                            ImdbId = item.ImdbId,
                            Magnet = item.Magnet,
                            ContainingDirectory = item.ContainingDirectory,
                            FileNames = item.FileNames,
                            Hash = item.Hash
                        });
                    }

                    if (json.Count > 0)
                    {
                        var js = JsonConvert.SerializeObject(json);
                        File.WriteAllText(AppSettingsManager.appSettings.downloadingTorrentsJson, js);
                    }
                }
                else
                {
                    var js = JsonConvert.SerializeObject(json);
                    File.WriteAllText(AppSettingsManager.appSettings.downloadingTorrentsJson, js);
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void UpdateUI()
        {
            try
            {
                foreach (var item in torrents)
                {
                    if (item.IsCompleted)
                    {
                        item.DownloadPercent = 100;
                        item.DownloadSpeed = "";
                        item.Eta = Application.Current.Resources["CompletedString"].ToString();
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        public async Task<string> DownloadTorrent(string url, string name)
        {
            try
            {
                if (url.ToLower().Contains("magnet"))
                {
                    return "";
                }
                else
                {
                    string path = System.IO.Path.Combine(AppSettingsManager.appSettings.TorrentsPath, name + ".torrent");
                    if (File.Exists(path))
                    {
                        return path;
                    }
                    else
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            using (var s = await httpClient.GetStreamAsync(url))
                            {
                                using (var fs = new FileStream(path, FileMode.CreateNew))
                                {
                                    await s.CopyToAsync(fs);
                                    return path;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return "MagnetUri";
            }
        }

        //public async Task<bool> IsUrlWorking(string url)
        //{
        //    try
        //    {
        //        using (HttpClient client = new HttpClient())
        //        {
        //            client.Timeout = TimeSpan.FromSeconds(2);
        //            HttpResponseMessage response = await client.GetAsync(url);
        //            return response.IsSuccessStatusCode;
        //        }
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}


        //DispatcherTimer dispatcherTimerForPlayerControl = new DispatcherTimer();

        private DispatcherTimer torrentCheckTimer;
        public static Dictionary<string, CancellationTokenSource> _cancellationTokenSources = new();

        private async Task StartTorrenting()
        {
            try
            {
                var torrentsList = await Libtorrent.client.GetTorrentsAsync();
                foreach (var torrent in torrents)
                {
                    if (!torrent.IsCompleted )
                    {
                        if (torrent.Hash == null || String.IsNullOrWhiteSpace(torrent.Hash))
                        {
                            var torrentPath = await DownloadTorrent(torrent.Link, torrent.Title);
                            if (!String.Equals(torrentPath, "MagnetUri"))
                            {
                                var hash = !String.IsNullOrWhiteSpace(torrentPath)
                                    ? await Libtorrent.AddTorrentFromFile(torrentPath)
                                    : await Libtorrent.AddTorrentFromMagnet(torrent.Link);
                                torrent.Hash = hash;
                                var cancellationTokenSource = new CancellationTokenSource();
                                _cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                                Task.Run(() => Libtorrent.DownloadFileSequentially(torrent, cancellationTokenSource.Token));
                                torrent.Magnet = !String.IsNullOrWhiteSpace(torrentPath) ? "" : torrent.Link;
                                torrent.TorrentLocation = !String.IsNullOrWhiteSpace(torrentPath) ? torrentPath : "";

                                if (String.IsNullOrWhiteSpace(hash))
                                {
                                    torrents.Remove(torrent);
                                    return;
                                }
                                else
                                {
                                    var allTorrents = await Libtorrent.client.GetTorrentsAsync();
                                    var myTorrent = allTorrents.FirstOrDefault(x => x.Hash.ToLower() == hash.ToLower());

                                    torrent.IsCompleted = myTorrent.Progress >= 1;

                                    SaveAllTorrents();
                                }
                            }
                            else
                            {
                                string hash = await Libtorrent.AddTorrentFromMagnet(torrent.Link);

                                if (String.IsNullOrWhiteSpace(hash))
                                {
                                    torrents.Remove(torrent);
                                }
                                else
                                {
                                    torrent.Hash = hash;

                                    var cancellationTokenSource = new CancellationTokenSource();
                                    _cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                                    Task.Run(() =>
                                        Libtorrent.DownloadFileSequentially(torrent, cancellationTokenSource.Token));
                                    torrent.Magnet = "";
                                    torrent.TorrentLocation = "";

                                    var allTorrents = await Libtorrent.client.GetTorrentsAsync();
                                    var myTorrent =
                                        allTorrents.FirstOrDefault(x => x.Hash.ToLower() == torrent.Hash.ToLower());

                                    if (myTorrent != null)
                                    {
                                        torrent.IsCompleted = myTorrent.Progress >= 1;

                                        SaveAllTorrents();
                                    }
                                }
                            }
                        }
                        else if (torrentsList.Any(x => x.Hash.ToLower() == torrent.Hash.ToLower()))
                        {
                            
                            
                                var alltorrents = await Libtorrent.client.GetTorrentsAsync();
                                var mytorrent = alltorrents.FirstOrDefault(x => x.Hash.ToLower() == torrent.Hash.ToLower());
                                if (mytorrent != null)
                                {
                                    var cancellationTokenSource = new CancellationTokenSource();
                                    _cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                                    Task.Run(() => Libtorrent.DownloadFileSequentially(torrent, cancellationTokenSource.Token));
                                    if (mytorrent.GetTorrentState != TorrentState.Downloading)
                                    {
                                        await Libtorrent.Resume(torrent);
                                    }
                                }
                                else
                                {
                                    var torrentPath = await DownloadTorrent(torrent.Link, torrent.Title);
                                    if (!String.Equals(torrentPath, "MagnetUri"))
                                    {
                                        var hash = !String.IsNullOrWhiteSpace(torrentPath)
                                            ? await Libtorrent.AddTorrentFromFile(torrentPath)
                                            : await Libtorrent.AddTorrentFromMagnet(torrent.Link);
                                        torrent.Hash = hash;
                                        var cancellationTokenSource = new CancellationTokenSource();
                                        _cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                                         Task.Run(() => Libtorrent.DownloadFileSequentially(torrent, cancellationTokenSource.Token));
                                        torrent.Magnet = !String.IsNullOrWhiteSpace(torrentPath) ? "" : torrent.Link;
                                        torrent.TorrentLocation = !String.IsNullOrWhiteSpace(torrentPath) ? torrentPath : "";

                                        if (String.IsNullOrWhiteSpace(hash))
                                        {
                                            torrents.Remove(torrent);
                                            return;
                                        }
                                        else
                                        {
                                            var allTorrents = await Libtorrent.client.GetTorrentsAsync();
                                            var myTorrent = allTorrents.FirstOrDefault(x => x.Hash.ToLower() == hash.ToLower());

                                            torrent.IsCompleted = myTorrent.Progress >= 1;

                                            SaveAllTorrents();
                                        }
                                    }
                                    else
                                    {
                                        string hash = await Libtorrent.AddTorrentFromMagnet(torrent.Link);

                                        if (String.IsNullOrWhiteSpace(hash))
                                        {
                                            torrents.Remove(torrent);
                                        }
                                        else
                                        {
                                            torrent.Hash = hash;

                                            var cancellationTokenSource = new CancellationTokenSource();
                                            _cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                                            Task.Run(() =>
                                                Libtorrent.DownloadFileSequentially(torrent,
                                                    cancellationTokenSource.Token));
                                            torrent.Magnet = "";
                                            torrent.TorrentLocation = "";

                                            var allTorrents = await Libtorrent.client.GetTorrentsAsync();
                                            var myTorrent =
                                                allTorrents.FirstOrDefault(x =>
                                                    x.Hash.ToLower() == torrent.Hash.ToLower());

                                            if (myTorrent != null)
                                            {
                                                torrent.IsCompleted = myTorrent.Progress >= 1;

                                                SaveAllTorrents();
                                            }
                                        }
                                }
                                }
                            
                        }
                    }
                    else
                    {
                        if (torrent.Hash != null)
                        {
                            var exits = torrentsList.Any(x => x.Hash.ToLower() == torrent.Hash.ToLower());
                            if (!exits)
                            {
                                torrent.IsCompleted = false;
                            }
                            //else
                            //{
                            //    var currentTorrent =
                            //        torrentsList.FirstOrDefault(x=>x.Hash.ToLower() == torrent.Hash.ToLower());
                            //    currentTorrent.Pause();
                            //}
                        }
                        else
                        {
                            torrent.IsCompleted = false;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void TorrentCheckTimerOnTick(object? sender, EventArgs e)
        {
            try
            {
                foreach (var item in torrents)
                {
                    if (!item.IsCompleted && item.Hash != null && !(finishedTorrents.Any(x => x == item.Hash)))
                    {
                        await Libtorrent.ObserveChanges(item);
                        if (item.IsCompleted)
                        {
                            finishedTorrents.Add(item.Hash);
                            SaveAllTorrents();
                            await Libtorrent.Pause(item);
                            Growl.SuccessGlobal(new GrowlInfo()
                            {
                                Message = item.Title + " " +
                                          App.Current.Resources["SuccessNotificationTorrentDownloaded"],
                                StaysOpen = false, WaitTime = 5
                            });
                        }
                        //else
                        //{
                        //    if (MoreThanOneMediaFileTorrentsTvShow.Any(x => x == item.Hash))
                        //    {
                        //        var currentTorrent = await Libtorrent.GetTorrentState(item);
                        //        if (currentTorrent == TorrentState.Downloading)
                        //        {
                        //            await Libtorrent.ChangeTvShowEpisodeFilePriorities(item);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        var files = await Libtorrent.GetFiles(item);

                        //        if (item.ShowType == ShowType.TvShow && files.Count(x => x.IsMediaFile) > 1)
                        //        {
                        //            MoreThanOneMediaFileTorrentsTvShow.Add(item.Hash);
                        //        }
                        //    }
                        //}
                    }
                    else if (item.IsCompleted && !(finishedTorrents.Any(x => x == item.Hash)))
                    {
                        var currentTorrent = await Libtorrent.GetTorrentState(item);
                        if (currentTorrent == TorrentState.Seeding)
                        {
                            await Libtorrent.Pause(item);
                            finishedTorrents.Add(item.Hash);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error("Error on timer: " + exception.Message);
            }
        }


        public async Task StartTorrenting2(Item torrent)
        {
            try
            {
                    if (!torrent.IsCompleted)
                    {
                        if (torrent.Hash == null || String.IsNullOrWhiteSpace(torrent.Hash))
                        {
                            var torrentPath = await DownloadTorrent(torrent.Link, torrent.Title);
                            if (!String.Equals(torrentPath, "MagnetUri"))
                            {
                                var hash = !String.IsNullOrWhiteSpace(torrentPath)
                                    ? await Libtorrent.AddTorrentFromFile(torrentPath)
                                    : await Libtorrent.AddTorrentFromMagnet(torrent.Link);
                                torrent.Hash = hash;
                                var cancellationTokenSource = new CancellationTokenSource();
                                _cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                                 Task.Run(() => Libtorrent.DownloadFileSequentially(torrent, cancellationTokenSource.Token));
                                torrent.Magnet = !String.IsNullOrWhiteSpace(torrentPath) ? "" : torrent.Link;
                                torrent.TorrentLocation = !String.IsNullOrWhiteSpace(torrentPath) ? torrentPath : "";

                                if (String.IsNullOrWhiteSpace(hash))
                                {
                                    torrents.Remove(torrent);
                                    return;
                                }
                                else
                                {
                                    var allTorrents = await Libtorrent.client.GetTorrentsAsync();
                                    var myTorrent = allTorrents.FirstOrDefault(x => x.Hash.ToLower() == hash.ToLower());

                                    if (myTorrent != null)
                                    {
                                        torrent.IsCompleted = myTorrent.Progress >= 1;

                                        SaveAllTorrents();
                                    }
                                }
                            }
                            else
                            {
                                string hash = await Libtorrent.AddTorrentFromMagnet(torrent.Link);

                                if (String.IsNullOrWhiteSpace(hash))
                                {
                                    torrents.Remove(torrent);
                                }
                                else
                                {
                                    torrent.Hash = hash;

                                    var cancellationTokenSource = new CancellationTokenSource();
                                    _cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                                    Task.Run(() =>
                                        Libtorrent.DownloadFileSequentially(torrent, cancellationTokenSource.Token));
                                    torrent.Magnet = "";
                                    torrent.TorrentLocation = "";

                                    var allTorrents = await Libtorrent.client.GetTorrentsAsync();
                                    var myTorrent =
                                        allTorrents.FirstOrDefault(x => x.Hash.ToLower() == torrent.Hash.ToLower());

                                    if (myTorrent != null)
                                    {
                                        torrent.IsCompleted = myTorrent.Progress >= 1;

                                        SaveAllTorrents();
                                    }
                                }  
                            }
                        }
                        else
                        {
                            var alltorrents = await Libtorrent.client.GetTorrentsAsync();
                            var mytorrent = alltorrents.FirstOrDefault(x => x.Hash.ToLower() == torrent.Hash.ToLower());
                            if (mytorrent != null)
                            {
                                var cancellationTokenSource = new CancellationTokenSource();
                                _cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                                 Task.Run(() => Libtorrent.DownloadFileSequentially(torrent, cancellationTokenSource.Token));
                            if (mytorrent.GetTorrentState != TorrentState.Downloading)
                                {
                                    await Libtorrent.Resume(torrent);
                                }
                            }
                            else
                            {
                                var torrentPath = await DownloadTorrent(torrent.Link, torrent.Title);
                                if (!String.Equals(torrentPath, "MagnetUri"))
                                {
                                    var hash = !String.IsNullOrWhiteSpace(torrentPath)
                                        ? await Libtorrent.AddTorrentFromFile(torrentPath)
                                        : await Libtorrent.AddTorrentFromMagnet(torrent.Link);
                                    torrent.Hash = hash;
                                    var cancellationTokenSource = new CancellationTokenSource();
                                    _cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                                     Task.Run(() => Libtorrent.DownloadFileSequentially(torrent, cancellationTokenSource.Token));
                                torrent.Magnet = !String.IsNullOrWhiteSpace(torrentPath) ? "" : torrent.Link;
                                    torrent.TorrentLocation = !String.IsNullOrWhiteSpace(torrentPath) ? torrentPath : "";

                                    if (String.IsNullOrWhiteSpace(hash))
                                    {
                                        torrents.Remove(torrent);
                                        return;
                                    }
                                    else
                                    {
                                        var allTorrents = await Libtorrent.client.GetTorrentsAsync();
                                        var myTorrent = allTorrents.FirstOrDefault(x => x.Hash.ToLower() == hash.ToLower());

                                        if (myTorrent != null)
                                        {
                                            torrent.IsCompleted = myTorrent.Progress >= 1;

                                            SaveAllTorrents();
                                        }
                                    }
                                }
                                else
                                {
                                    string hash = await Libtorrent.AddTorrentFromMagnet(torrent.Link);

                                    if (String.IsNullOrWhiteSpace(hash))
                                    {
                                        torrents.Remove(torrent);
                                    }
                                    else
                                    {
                                        torrent.Hash = hash;

                                        var cancellationTokenSource = new CancellationTokenSource();
                                        _cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                                        Task.Run(() =>
                                            Libtorrent.DownloadFileSequentially(torrent,
                                                cancellationTokenSource.Token));
                                        torrent.Magnet = "";
                                        torrent.TorrentLocation = "";

                                        var allTorrents = await Libtorrent.client.GetTorrentsAsync();
                                        var myTorrent =
                                            allTorrents.FirstOrDefault(x => x.Hash.ToLower() == torrent.Hash.ToLower());

                                        if (myTorrent != null)
                                        {
                                            torrent.IsCompleted = myTorrent.Progress >= 1;

                                            SaveAllTorrents();
                                        }
                                    }
                            }
                            }


                        }

                    }
                
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }


        private async Task StartTorrenting(Item torrentData)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(torrentData.TorrentLocation) && String.IsNullOrWhiteSpace(torrentData.Magnet))
                {
                    string hash = await Libtorrent.AddTorrentFromMagnet(torrentData.Link);

                    if (String.IsNullOrWhiteSpace(hash))
                    {
                        torrents.Remove(torrentData);
                    }
                    else
                    {
                        torrentData.Hash = hash;

                        var cancellationTokenSource = new CancellationTokenSource();
                        _cancellationTokenSources[torrentData.Hash] = cancellationTokenSource;
                        Task.Run(() =>
                            Libtorrent.DownloadFileSequentially(torrentData, cancellationTokenSource.Token));
                        torrentData.Magnet = "";
                        torrentData.TorrentLocation = "";

                        var allTorrents = await Libtorrent.client.GetTorrentsAsync();
                        var myTorrent =
                            allTorrents.FirstOrDefault(x => x.Hash.ToLower() == torrentData.Hash.ToLower());

                        if (myTorrent != null)
                        {
                            torrentData.IsCompleted = myTorrent.Progress >= 1;

                            SaveAllTorrents();
                        }
                    }
                }
                else
                {
                    var torrentHash = string.IsNullOrWhiteSpace(torrentData.Magnet)
                        ? await Libtorrent.AddTorrentFromFile(torrentData.TorrentLocation)
                        : await Libtorrent.AddTorrentFromMagnet(torrentData.Link);

                    var cancellationTokenSource = new CancellationTokenSource();
                    _cancellationTokenSources[torrentData.Hash] = cancellationTokenSource;
                     Task.Run(() => Libtorrent.DownloadFileSequentially(torrentData, cancellationTokenSource.Token));
                    foreach (var torrent in torrents)
                        if (torrent.MovieId == torrentData.MovieId && torrent.SeasonNumber == torrentData.SeasonNumber
                                                                   && torrent.EpisodeNumber == torrentData.EpisodeNumber)
                        {
                            torrent.Hash = torrentHash;
                            SaveAllTorrents();
                        }
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private List<string> finishedTorrents = new List<string>();
        private List<string> MoreThanOneMediaFileTorrentsTvShow = new List<string>();
        private async void DispatcherTimerForPlayerControlOnTick(object? sender, EventArgs e, Item torrent)
        {
            try
            {
                foreach (var item in torrents)
                {
                    if (item == torrent && !(finishedTorrents.Any(x=> x == item.Hash)))
                    {
                        //await Libtorrent.EnableStreaming(item);
                        await Libtorrent.ObserveChanges(item);
                        if (item.IsCompleted)
                        {
                            SaveAllTorrents();
                            finishedTorrents.Add(item.Hash);
                            await Libtorrent.Pause(item);
                            //if (item.EpisodeNumber != -1)
                            //{
                            //    if (!openedTorrents.Any(x => x == item.Link))
                            //    {
                            //        Growl.AskGlobal(new GrowlInfo()
                            //        {
                            //            Message = item.Title + " \n" + App.Current.Resources["AskNotificationTorrentDownloaded"],
                            //            CancelStr = "Kapat",
                            //            ConfirmStr = "Oynat",
                            //            StaysOpen = false,
                            //            WaitTime = 5,
                            //            ActionBeforeClose = isConfirmed =>
                            //            {
                            //                if (isConfirmed)
                            //                {
                            //                    DownloadsDisplay.SelectedItem = item;
                            //                }
                            //                return true;
                            //            }
                            //        });
                            //    }
                            //}
                            Growl.SuccessGlobal(new GrowlInfo(){Message = item.Title + " " + App.Current.Resources["SuccessNotificationTorrentDownloaded"] ,StaysOpen = false,WaitTime = 5});
                        }
                        //else
                        //{
                        //    if (MoreThanOneMediaFileTorrentsTvShow.Any(x => x == item.Hash))
                        //    {
                        //        await Libtorrent.ChangeTvShowEpisodeFilePriorities(item);
                        //    }
                        //    else
                        //    {
                        //        var files = await Libtorrent.GetFiles(item);

                        //        if (item.ShowType == ShowType.TvShow && files.Count(x => x.IsMediaFile) > 1)
                        //        {
                        //            MoreThanOneMediaFileTorrentsTvShow.Add(item.Hash);
                        //        }
                        //    }
                        //}
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error("Error on timer: " + exception.Message);
            }
        }
        public void ForceDeleteDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                // Tüm dosyaları sil
                foreach (var file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal); 
                        File.Delete(file);
                    }
                    catch (System.Exception ex)
                    {
                        var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                        Log.Error(errorMessage);
                    }
                }

                // Tüm alt dizinleri sil
                foreach (var dir in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        Directory.Delete(dir, true); // Alt dizinleri zorla sil
                    }
                    catch (System.Exception ex)
                    {
                        var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                        Log.Error(errorMessage);
                    }
                }

                // Son olarak ana dizini sil
                try
                {
                    Directory.Delete(directoryPath, true);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                    Log.Error(errorMessage);
                }
            }
        }
        private async void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTorrent = DownloadsDisplay.Items[selectedIndex] as Item;

                if (selectedTorrent != null)
                {
                    if (selectedTorrent.Hash == null || String.IsNullOrWhiteSpace(selectedTorrent.Hash))
                    {
                        torrents.Remove(selectedTorrent);
                    }
                    else
                    {
                        if (PlayerWindow.thumbnailCaches.Any())
                        {
                            foreach (var thumbnailCach in PlayerWindow.thumbnailCaches.ToList())
                            {
                                if (thumbnailCach.Hash == selectedTorrent.Hash)
                                {
                                    ForceDeleteDirectory(thumbnailCach.FolderPath);
                                    PlayerWindow.thumbnailCaches.Remove(thumbnailCach);
                                }
                            }

                            var js = JsonConvert.SerializeObject(PlayerWindow.thumbnailCaches);
                            await File.WriteAllTextAsync(AppSettingsManager.appSettings.ThumbnailCachesPath, js);
                        }
                        else
                        {
                            PlayerWindow.GetThumbnailCaches();
                            if (PlayerWindow.thumbnailCaches.Any())
                            {
                                foreach (var thumbnailCach in PlayerWindow.thumbnailCaches.ToList())
                                {
                                    if (thumbnailCach.Hash == selectedTorrent.Hash)
                                    {
                                        ForceDeleteDirectory(thumbnailCach.FolderPath);
                                        PlayerWindow.thumbnailCaches.Remove(thumbnailCach);
                                    }
                                }
                                var js = JsonConvert.SerializeObject(PlayerWindow.thumbnailCaches);
                                await File.WriteAllTextAsync(AppSettingsManager.appSettings.ThumbnailCachesPath, js);
                            }
                        }

                        GetPlayerCaches();
                        if (playerCaches.Count > 0)
                        {
                            if (playerCaches.Any(x =>
                                    x.MovieId== selectedTorrent.MovieId && x.ShowType == selectedTorrent.ShowType))
                            {
                                var deleteTorrentWatchHistoryRequest = new DeleteTorrentWatchHistoryRequest()
                                {
                                    Email = AppSettingsManager.appSettings.FireStoreEmail,
                                    ShowId = selectedTorrent.MovieId,
                                    ShowType = selectedTorrent.ShowType,
                                    DeletedTorrent = true
                                };
                                await FirestoreManager.DeleteTorrentWatchHistory(deleteTorrentWatchHistoryRequest);

                                foreach (var z in playerCaches.Where(x => x.MovieId == selectedTorrent.MovieId && x.ShowType == selectedTorrent.ShowType).ToList())
                                {
                                    z.DeletedTorrent = true;
                                }
                               
                                await File.WriteAllTextAsync(AppSettingsManager.appSettings.PlayerCachePath,
                                    EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(playerCaches), Encryptor.Key, Encryptor.IV));
                            }
                        }
                        await Libtorrent.Delete(selectedTorrent);
                        torrents.Remove(selectedTorrent);
                    }
                    SaveAllTorrents();
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private List<PlayerCache> playerCaches = new List<PlayerCache>();
        private async void GetPlayerCaches()
        {
            try
            {
                var js = File.ReadAllText(AppSettingsManager.appSettings.PlayerCachePath);
                if (!String.IsNullOrWhiteSpace(js))
                {
                    playerCaches = JsonConvert.DeserializeObject<List<PlayerCache>>(EncryptProvider.AESDecrypt(js,Encryptor.Key,Encryptor.IV));
                }
                else
                {
                    if (FirestoreManager.WatchHistories.Count > 0 && (playerCaches == null || playerCaches.Count == 0))
                    {
                        var result = await FirestoreManager.GetWatchHistory();
                        if (result.Success)
                        {
                            playerCaches = new List<PlayerCache>();
                            foreach (var resultWatchHistory in result.WatchHistories)
                            {
                                PlayerCache playerCache = new PlayerCache
                                {
                                    MovieId = resultWatchHistory.Id,
                                    ShowType = resultWatchHistory.ShowType,
                                    LastPosition = (float)resultWatchHistory.Progress,
                                    SeasonNumber = resultWatchHistory.SeasonNumber,
                                    EpisodeNumber = resultWatchHistory.EpisodeNumber,
                                    DeletedTorrent = resultWatchHistory.DeletedTorrent
                                };
                                playerCaches.Add(playerCache);
                            }

                            await File.WriteAllTextAsync(AppSettingsManager.appSettings.PlayerCachePath,
                                EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(playerCaches), Encryptor.Key, Encryptor.IV));
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }


        }

        private async void MenuItem_OnClickPause(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTorrent = DownloadsDisplay.Items[selectedIndex] as Item;

                if (selectedTorrent != null)
                {
                    var torrents = await Libtorrent.client.GetTorrentsAsync();
                    var current = torrents.FirstOrDefault(x => x.Hash.ToLower() == selectedTorrent.Hash.ToLower());
                    if (!current.IsPaused)
                    {
                        await Libtorrent.Pause(selectedTorrent);
                        (sender as MenuItem).Header = Application.Current.Resources["PlayString"];
                    }
                    else
                    {
                        await Libtorrent.Resume(selectedTorrent);
                        (sender as MenuItem).Header = Application.Current.Resources["PauseString"];
                    }

                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private int selectedIndex = -1;

        private void DownloadsDisplay_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;

                for (int i = 0; i < torrents.Count; i++)
                {
                    var lbi = DownloadsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (lbi == null) continue;
                    if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private static bool IsMouseOverTarget(Visual target, Point point)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(target);
            return bounds.Contains(point);
        }


        private void MenuItem_ContextMenuDetailsOnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTorrent = DownloadsDisplay.Items[selectedIndex] as Item;

                if (selectedTorrent != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedTorrent.MovieId,
                        selectedTorrent.ShowType);
                    HomePage.GetHomePageInstance.HomePageNavigation.Navigate(movieDetailsPage);
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var pauseMenuItem = sender as MenuItem;

                if (pauseMenuItem.DataContext is Item)
                {
                    var item = pauseMenuItem.DataContext as Item;
                    if (item.IsCompleted)
                    {
                        pauseMenuItem.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void DownloadsPageQ_OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
