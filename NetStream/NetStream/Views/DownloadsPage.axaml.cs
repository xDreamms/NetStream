using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Serilog;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Avalonia.Controls.Presenters;
using TMDbLib.Objects.Movies;
using NETCore.Encrypt;
using NetStream.Services;

namespace NetStream.Views
{
    public partial class DownloadsPage : UserControl, IDisposable
    {
        public static DownloadsPage Instance { get; private set; }
        public static ObservableCollection<Item> torrents = new ObservableCollection<Item>();
        private List<Torrent> savedTorrents;
        private Item selectedTorrent;
        private List<string> finishedTorrents = new List<string>();
        private List<string> MoreThanOneMediaFileTorrentsTvShow = new List<string>();
        
        public DownloadsPage()
        {
            InitializeComponent();
            Instance = this;
            OnLoad();
            Log.Information("Loaded Downloads Page");
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
        }
        
        private async void OnLoad()
        {
            DownloadsDisplay.ItemsSource = torrents;
            if (torrents.Count == 0)
                await LoadTorrents();
            
            await StartTorrenting();
            
            _ =Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        // Process torrents in batches to reduce CPU usage
                        var activeTorrents = torrents.Where(x => !x.IsCompleted && x.Hash != null && !finishedTorrents.Contains(x.Hash)).ToList();

                        // var completedTorrents = torrents.Where(x => x.IsCompleted && x.Hash != null&& !finishedTorrents.Contains(x.Hash));
                        // foreach (var item in completedTorrents)
                        // {
                        //     await Libtorrent.Pause(item.Hash);
                        //     finishedTorrents.Add(item.Hash);
                        // }
                        foreach (var item in activeTorrents)
                        {
                            if (!item.IsCompleted && item.Hash != null && !(finishedTorrents.Any(x => x == item.Hash)))
                            {
                                 await Libtorrent.ObserveChanges(item);
                                 
                                 // if (WebSocketServer._instance != null && !string.IsNullOrEmpty(item.Hash))
                                 // {
                                 //     WebSocketServer._instance.SendTorrentStatusMessage(item);
                                 // }
                                 
                                 if (item.IsCompleted)
                                 {
                                    finishedTorrents.Add(item.Hash);
                                    await FirestoreManager.EditDownloadHistory(item.Hash, item.IsCompleted);
                                    SaveAllTorrents();
                                    await Libtorrent.Pause(item.Hash);
                                    await NotificationService.Instance.ShowNotification(item.Title + " "  + ResourceProvider.GetString("SuccessNotificationTorrentDownloaded"),DateTime.Now.ToLongDateString(),TimeSpan.FromSeconds(4),true);

                                    // await Dispatcher.UIThread.InvokeAsync(async () =>
                                    // {
                                    //     await MessageBox.ShowAsync(item.Title + " download completed.", "Success", MessageBox.MessageBoxButtons.Ok);
                                    // });
                                 }
                            }
                            else if (item.IsCompleted && !(finishedTorrents.Any(x => x == item.Hash)))
                            {
                                var currentTorrent = await Libtorrent.GetTorrentState(item.Hash);
                                if (currentTorrent == TorrentState.Seeding)
                                {
                                    await Libtorrent.Pause(item.Hash);
                                    finishedTorrents.Add(item.Hash);
                                }
                                
                                // if (WebSocketServer._instance != null && !string.IsNullOrEmpty(item.Hash))
                                // {
                                //     WebSocketServer._instance.SendTorrentStatusMessage(item);
                                // }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                        Log.Error(errorMessage);
                    }

                    // Increased delay for low-end PC optimization (200ms -> 300ms)
                    await Task.Delay(300);
                }
            });
            UpdateUI();
          
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

        public bool loadedTorrents = false;
        
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

                    loadedTorrents = true;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        static double JaroWinklerSimilarity(string s1, string s2)
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
        
        private async void DownloadsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            SaveAllTorrents();
            UpdateUI();
            ApplyDirectStylesToDownloadsItems(MainView.Instance.Bounds.Width);
            
            await Task.Delay(150);
            if (DownloadsDisplayScrollViewer != null)
            {
                DownloadsDisplayScrollViewer.Offset = new Vector(DownloadsDisplayScrollViewer.Offset.X, _savedScrollOffset);
            }
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyDirectStylesToDownloadsItems(e.width);
        }

        public static double _savedScrollOffset = 0;
        private void DownloadsPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            _savedScrollOffset = DownloadsDisplayScrollViewer.Offset.Y;
        }
        
        public void SaveAllTorrents()
        {
            try
            {
                List<Torrent> json = new List<Torrent>();

                if (torrents.Count > 0)
                {
                    foreach (var item in torrents)
                    {
                        Console.WriteLine(item.MovieName);
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
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
                Console.WriteLine(errorMessage);
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
                        item.Eta = ResourceProvider.GetString("CompletedString");
                    }
                }
            }
            catch (Exception exception)
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
                    string path = Path.Combine(AppSettingsManager.appSettings.TorrentsPath, name + ".torrent");
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
                Log.Error($"Error downloading torrent: {e.Message}");
                return "MagnetUri";
            }
        }

        private async Task StartTorrenting()
        {
            
                try
                {
                    var torrentsList = await Libtorrent.GetTorrentList();
                    foreach (var torrent in torrents)
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
                                    
                                    await Libtorrent.DownloadFileSequentially(torrent.Hash);
                                    torrent.Magnet = !String.IsNullOrWhiteSpace(torrentPath) ? "" : torrent.Link;
                                    torrent.TorrentLocation =
                                        !String.IsNullOrWhiteSpace(torrentPath) ? torrentPath : "";

                                    if (String.IsNullOrWhiteSpace(hash))
                                    {
                                        torrents.Remove(torrent);
                                        return;
                                    }
                                    else
                                    {
                                        var allTorrents = await Libtorrent.GetTorrentList();
                                        var myTorrent =
                                            allTorrents.FirstOrDefault(x => x.Hash.ToLower() == hash.ToLower());

                                        torrent.IsCompleted = myTorrent.Progress >= 1;

                                        AddTorrentToDownloadHistory(torrent);
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

                                        await Libtorrent.DownloadFileSequentially(torrent.Hash);;
                                        torrent.Magnet = "";
                                        torrent.TorrentLocation = "";

                                        var allTorrents = await Libtorrent.GetTorrentList();
                                        var myTorrent =
                                            allTorrents.FirstOrDefault(x => x.Hash.ToLower() == torrent.Hash.ToLower());

                                        if (myTorrent != null)
                                        {
                                            torrent.IsCompleted = myTorrent.Progress >= 1;

                                            AddTorrentToDownloadHistory(torrent);
                                            SaveAllTorrents();
                                        }
                                    }
                                }
                            }
                            else if (torrentsList.Any(x => x.Hash.ToLower() == torrent.Hash.ToLower()))
                            {
                                var alltorrents = await Libtorrent.GetTorrentList();
                                var mytorrent =
                                    alltorrents.FirstOrDefault(x => x.Hash.ToLower() == torrent.Hash.ToLower());
                                if (mytorrent != null)
                                {
                                    await Libtorrent.DownloadFileSequentially(torrent.Hash);;
                                    if (mytorrent.TorrentState != TorrentState.Downloading)
                                    {
                                        await Libtorrent.Resume(torrent.Hash);
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
                                        await Libtorrent.DownloadFileSequentially(torrent.Hash);;
                                        torrent.Magnet = !String.IsNullOrWhiteSpace(torrentPath) ? "" : torrent.Link;
                                        torrent.TorrentLocation =
                                            !String.IsNullOrWhiteSpace(torrentPath) ? torrentPath : "";

                                        if (String.IsNullOrWhiteSpace(hash))
                                        {
                                            torrents.Remove(torrent);
                                            return;
                                        }
                                        else
                                        {
                                            var allTorrents = await Libtorrent.GetTorrentList();
                                            var myTorrent =
                                                allTorrents.FirstOrDefault(x => x.Hash.ToLower() == hash.ToLower());

                                            torrent.IsCompleted = myTorrent.Progress >= 1;
                                            AddTorrentToDownloadHistory(torrent);
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

                                            await Libtorrent.DownloadFileSequentially(torrent.Hash);;
                                            torrent.Magnet = "";
                                            torrent.TorrentLocation = "";

                                            var allTorrents = await Libtorrent.GetTorrentList();
                                            var myTorrent =
                                                allTorrents.FirstOrDefault(x =>
                                                    x.Hash.ToLower() == torrent.Hash.ToLower());

                                            if (myTorrent != null)
                                            {
                                                torrent.IsCompleted = myTorrent.Progress >= 1;
                                                AddTorrentToDownloadHistory(torrent);
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

        public async Task StartTorrenting2(Item torrent)
        {
            try
            {
                Console.WriteLine("Started torrenting 2");
                if (!torrent.IsCompleted)
                {
                    if (torrent.Hash == null || String.IsNullOrWhiteSpace(torrent.Hash))
                    {
                        // Link boşsa Enclosure.Url'i kullan
                        string torrentLink = torrent.Link;
                        if (string.IsNullOrEmpty(torrentLink) && torrent.Enclosure != null && !string.IsNullOrEmpty(torrent.Enclosure.Url))
                        {
                            torrentLink = torrent.Enclosure.Url;
                            torrent.Link = torrentLink; // Link'i güncelle
                        }
                        
                        if (string.IsNullOrEmpty(torrentLink))
                        {
                            Log.Error($"Torrent link boş: {torrent.Title}");
                            torrents.Remove(torrent);
                            return;
                        }
                        
                        var torrentPath = await DownloadTorrent(torrentLink, torrent.Title);
                        if (!String.Equals(torrentPath, "MagnetUri"))
                        {
                            var hash = !String.IsNullOrWhiteSpace(torrentPath)
                                ? await Libtorrent.AddTorrentFromFile(torrentPath)
                                : await Libtorrent.AddTorrentFromMagnet(torrentLink);
                            torrent.Hash = hash;
                            await Libtorrent.DownloadFileSequentially(torrent.Hash);;
                            torrent.Magnet = !String.IsNullOrWhiteSpace(torrentPath) ? "" : torrentLink;
                            torrent.TorrentLocation = !String.IsNullOrWhiteSpace(torrentPath) ? torrentPath : "";

                            if (String.IsNullOrWhiteSpace(hash))
                            {
                                torrents.Remove(torrent);
                                return;
                            }
                            else
                            {
                                var allTorrents = await Libtorrent.GetTorrentList();
                                var myTorrent = allTorrents.FirstOrDefault(x => x.Hash.ToLower() == hash.ToLower());

                                if (myTorrent != null)
                                {
                                    torrent.IsCompleted = myTorrent.Progress >= 1;
                                    AddTorrentToDownloadHistory(torrent);
                                    SaveAllTorrents();
                                }
                            }
                        }
                        else
                        {
                            string hash = await Libtorrent.AddTorrentFromMagnet(torrentLink);

                            if (String.IsNullOrWhiteSpace(hash))
                            {
                                torrents.Remove(torrent);
                            }
                            else
                            {
                                torrent.Hash = hash;

                                await Libtorrent.DownloadFileSequentially(torrent.Hash);;
                                torrent.Magnet = "";
                                torrent.TorrentLocation = "";

                                var allTorrents = await Libtorrent.GetTorrentList();
                                var myTorrent =
                                    allTorrents.FirstOrDefault(x => x.Hash.ToLower() == torrent.Hash.ToLower());

                                if (myTorrent != null)
                                {
                                    torrent.IsCompleted = myTorrent.Progress >= 1;
                                    AddTorrentToDownloadHistory(torrent);
                                    SaveAllTorrents();
                                }
                            }
                        }
                    }
                    else
                    {
                        var alltorrents = await Libtorrent.GetTorrentList();
                        var mytorrent = alltorrents.FirstOrDefault(x => x.Hash.ToLower() == torrent.Hash.ToLower());
                        if (mytorrent != null)
                        {
                            await Libtorrent.DownloadFileSequentially(torrent.Hash);;
                            if (mytorrent.TorrentState != TorrentState.Downloading)
                            {
                                await Libtorrent.Resume(torrent.Hash);
                            }
                        }
                        else
                        {
                            // Link boşsa Enclosure.Url'i kullan
                            string torrentLink = torrent.Link;
                            if (string.IsNullOrEmpty(torrentLink) && torrent.Enclosure != null && !string.IsNullOrEmpty(torrent.Enclosure.Url))
                            {
                                torrentLink = torrent.Enclosure.Url;
                                torrent.Link = torrentLink; // Link'i güncelle
                            }
                            
                            if (string.IsNullOrEmpty(torrentLink))
                            {
                                Log.Error($"Torrent link boş: {torrent.Title}");
                                torrents.Remove(torrent);
                                return;
                            }
                            
                            var torrentPath = await DownloadTorrent(torrentLink, torrent.Title);
                            if (!String.Equals(torrentPath, "MagnetUri"))
                            {
                                var hash = !String.IsNullOrWhiteSpace(torrentPath)
                                    ? await Libtorrent.AddTorrentFromFile(torrentPath)
                                    : await Libtorrent.AddTorrentFromMagnet(torrentLink);
                                torrent.Hash = hash;
                                await Libtorrent.DownloadFileSequentially(torrent.Hash);;
                                torrent.Magnet = !String.IsNullOrWhiteSpace(torrentPath) ? "" : torrentLink;
                                torrent.TorrentLocation = !String.IsNullOrWhiteSpace(torrentPath) ? torrentPath : "";

                                if (String.IsNullOrWhiteSpace(hash))
                                {
                                    torrents.Remove(torrent);
                                    return;
                                }
                                else
                                {
                                    var allTorrents = await Libtorrent.GetTorrentList();
                                    var myTorrent = allTorrents.FirstOrDefault(x => x.Hash.ToLower() == hash.ToLower());

                                    if (myTorrent != null)
                                    {
                                        torrent.IsCompleted = myTorrent.Progress >= 1;
                                        AddTorrentToDownloadHistory(torrent);
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

                                    await Libtorrent.DownloadFileSequentially(torrent.Hash);;
                                    torrent.Magnet = "";
                                    torrent.TorrentLocation = "";

                                    var allTorrents = await Libtorrent.GetTorrentList();
                                    var myTorrent =
                                        allTorrents.FirstOrDefault(x => x.Hash.ToLower() == torrent.Hash.ToLower());

                                    if (myTorrent != null)
                                    {
                                        torrent.IsCompleted = myTorrent.Progress >= 1;
                                        AddTorrentToDownloadHistory(torrent);
                                        SaveAllTorrents();
                                    }
                                }
                            }
                        }
                    }
                    
                     // if (WebSocketServer._instance != null && !string.IsNullOrEmpty(torrent.Hash))
                     // {
                     //     WebSocketServer._instance.SendTorrentAddMessage(torrent);
                     // }
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

                await Libtorrent.DownloadFileSequentially(torrentData.Hash);;
                torrentData.Magnet = "";
                torrentData.TorrentLocation = "";

                var allTorrents = await Libtorrent.GetTorrentList();
                var myTorrent =
                    allTorrents.FirstOrDefault(x => x.Hash.ToLower() == torrentData.Hash.ToLower());

                if (myTorrent != null)
                {
                    torrentData.IsCompleted = myTorrent.Progress >= 1;
                    AddTorrentToDownloadHistory(torrentData);
                    SaveAllTorrents();
                }
            }
        }
        else
        {
            var torrentHash = string.IsNullOrWhiteSpace(torrentData.Magnet)
                ? await Libtorrent.AddTorrentFromFile(torrentData.TorrentLocation)
                : await Libtorrent.AddTorrentFromMagnet(torrentData.Link);

            await Libtorrent.DownloadFileSequentially(torrentData.Hash);
            foreach (var torrent in torrents)
                if (torrent.MovieId == torrentData.MovieId && torrent.SeasonNumber == torrentData.SeasonNumber
                                                           && torrent.EpisodeNumber == torrentData.EpisodeNumber)
                {
                    torrent.Hash = torrentHash;
                    AddTorrentToDownloadHistory(torrent);
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
        
        private async void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTorrentForDeletion = DownloadsDisplay.Items[selectedIndex] as Item;

                if (selectedTorrentForDeletion != null)
                {
                    if (selectedTorrentForDeletion.Hash == null || String.IsNullOrWhiteSpace(selectedTorrentForDeletion.Hash))
                    {
                        torrents.Remove(selectedTorrentForDeletion);
                        SaveAllTorrents();
                    }
                    else
                    {
                        await Task.Run(async () =>
                {
                    
                    if (PlayerWindow.thumbnailCaches.Any())
                {
                    foreach (var thumbnailCach in PlayerWindow.thumbnailCaches.ToList())
                    {
                        if (thumbnailCach.Hash == selectedTorrentForDeletion.Hash)
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
                            if (thumbnailCach.Hash == selectedTorrentForDeletion.Hash)
                            {
                                ForceDeleteDirectory(thumbnailCach.FolderPath);
                                PlayerWindow.thumbnailCaches.Remove(thumbnailCach);
                            }
                        }

                        var js = JsonConvert.SerializeObject(PlayerWindow.thumbnailCaches);
                        await File.WriteAllTextAsync(AppSettingsManager.appSettings.ThumbnailCachesPath, js);
                    }
                }


                var deleteTorrentWatchHistoryRequest = new DeleteTorrentWatchHistoryRequest()
                {
                    Email = AppSettingsManager.appSettings.FireStoreEmail,
                    ShowId = selectedTorrentForDeletion.MovieId,
                    ShowType = selectedTorrentForDeletion.ShowType,
                    DeletedTorrent = true
                };
                await FirestoreManager.DeleteTorrentWatchHistory(deleteTorrentWatchHistoryRequest);

                bool success = false;
                int retryCount = 0;
                while (!success && retryCount < 20)
                {
                    success = await Libtorrent.Delete(selectedTorrentForDeletion.Hash);
                    if (!success)
                    {
                         Log.Error($"Failed to delete torrent: {selectedTorrentForDeletion.Hash}. Retrying... {retryCount + 1}/20");
                         await Task.Delay(500);
                         retryCount++;
                    }
                }

                if (success)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var deletedTorrent = torrents.FirstOrDefault(x => x.Hash == selectedTorrentForDeletion.Hash);
                        if (deletedTorrent != null)
                        {
                            torrents.Remove(deletedTorrent);
                            SaveAllTorrents();
                        }
                    });

                    await FirestoreManager.DeleteDownloadHistory(selectedTorrentForDeletion.Hash);
                    //WebSocketServer._instance.SendTorrentDeleteMessage(selectedTorrentForDeletion);
                }
                else
                {
                     Log.Error($"Failed to delete torrent after 20 retries: {selectedTorrentForDeletion.Hash}");
                }
                });
                
            }
        }
    }
    catch (System.Exception exception)
    {
        var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
        Log.Error(errorMessage);
    }
}
        
        private async Task DeleteTorrentData(Item torrent)
        {
            // Clean up associated data like thumbnail caches and player caches
            // This is a simplified version of the original code
            // if (PlayerWindow.thumbnailCaches.Any(x => x.Hash == torrent.Hash))
            // {
            //     foreach (var thumbnailCache in PlayerWindow.thumbnailCaches.ToList())
            //     {
            //         if (thumbnailCache.Hash == torrent.Hash)
            //         {
            //             ForceDeleteDirectory(thumbnailCache.FolderPath);
            //             PlayerWindow.thumbnailCaches.Remove(thumbnailCache);
            //         }
            //     }
            // }
            
           
        }
        
        private void ForceDeleteDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                try
                {
                    // Delete all files
                    foreach (var file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal); 
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error deleting file: {ex.Message}");
                        }
                    }

                    // Delete all subdirectories
                    foreach (var dir in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error deleting directory: {ex.Message}");
                        }
                    }

                    // Delete the main directory
                    Directory.Delete(directoryPath, true);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in ForceDeleteDirectory: {ex.Message}");
                }
            }
        }
        
        private async void MenuItem_OnClickPause(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTorrent = DownloadsDisplay.Items[selectedIndex] as Item;

                if (selectedTorrent != null)
                {
                    var torrents = await Libtorrent.GetTorrentList();
                    var current = torrents.FirstOrDefault(x => x.Hash.ToLower() == selectedTorrent.Hash.ToLower());
                    if (!current.IsPaused)
                    {
                        await Libtorrent.Pause(selectedTorrent.Hash);
                        (sender as MenuItem).Header = Application.Current.Resources["PlayString"];
                    }
                    else
                    {
                        await Libtorrent.Resume(selectedTorrent.Hash);
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
        
        private void MenuItem_ContextMenuDetailsOnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTorrent = DownloadsDisplay.Items[selectedIndex] as Item;

                if (selectedTorrent != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedTorrent.MovieId,
                        selectedTorrent.ShowType);
                    var mainView = this.FindAncestorOfType<MainView>();
                    if (mainView != null)
                    {
                        mainView.SetContent(movieDetailsPage);
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        private void DownloadsDisplay_OnContextRequested(object sender, ContextRequestedEventArgs e)
        {
            /*var point = e.GetPosition(this.FindControl<ItemsControl>("DownloadsDisplay"));
            var element = this.FindControl<ItemsControl>("DownloadsDisplay").InputHitTest(point) as IVisual;
            
            if (element != null)
            {
                // Find the parent ContentPresenter or ContentControl that contains the data
                var contentPresenter = element.FindAncestorOfType<ContentPresenter>();
                if (contentPresenter != null)
                {
                    selectedTorrent = contentPresenter.DataContext as Item;
                }
            }*/
        }
        
        
     
        private int selectedIndex = -1;

        private async void DownloadsItem_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                try
                {
                    var grid = sender as Grid;
                    if(grid == null) return;
                    var selectedTorrent = grid.DataContext as Item;
                    if (selectedTorrent == null) return;

                    // Ensure hash is valid before trying to get files
                    if (string.IsNullOrWhiteSpace(selectedTorrent.Hash))
                    {
                        Log.Warning($"Torrent hash is null or empty for: {selectedTorrent.Title}");
                        // Try to start torrenting to get hash
                        await StartTorrenting2(selectedTorrent);
                        if (string.IsNullOrWhiteSpace(selectedTorrent.Hash))
                        {
                            Log.Error($"Failed to get torrent hash for: {selectedTorrent.Title}");
                            return;
                        }
                    }

                    List<NetStream.TorrentFile> files = null;
                    try
                    {
                        files = await Task.Run(() => Libtorrent.GetFiles(selectedTorrent.Hash));
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error getting files for hash {selectedTorrent.Hash}: {ex.Message}");
                        return;
                    }

                    if (files == null || files.Count == 0)
                    {
                        Log.Warning($"No files found for torrent: {selectedTorrent.Title} (Hash: {selectedTorrent.Hash})");
                        return;
                    }
                    if (selectedTorrent.EpisodeNumber == -1 || files.Count(x => x.IsMediaFile) > 1)
                    {
                        if (selectedTorrent.Hash == null || String.IsNullOrWhiteSpace(selectedTorrent.Hash))
                        {
                            if (String.IsNullOrWhiteSpace(selectedTorrent.TorrentLocation) &&
                                String.IsNullOrWhiteSpace(selectedTorrent.Magnet))
                            {
                                if (selectedTorrent.Link == torrents.Last().Link)
                                {
                                    var allTorrents = await Libtorrent.GetTorrentList();
                                    var sorted = allTorrents.OrderByDescending(x => x.AddedOn).FirstOrDefault();
                                    var hash = sorted.Hash;
                                    selectedTorrent.Hash = hash;
                                    AddTorrentToDownloadHistory(selectedTorrent);
                                    SaveAllTorrents();

                                    var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId,
                                        selectedTorrent.MovieName,
                                        selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                                    MainView.Instance.SetContent(filesPage);
                                }
                                else
                                {
                                    var allTorrents = await Libtorrent.GetTorrentList();
                                    var current = allTorrents.OrderByDescending(s =>
                                        JaroWinklerSimilarity(s.Name, selectedTorrent.Title)).First();
                                    if (current != null)
                                    {
                                        var hash = current.Hash;
                                        selectedTorrent.Hash = hash;
                                       
                                        SaveAllTorrents();

                                        var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId,
                                            selectedTorrent.MovieName,
                                            selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                                        MainView.Instance.SetContent(filesPage);
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
                                    MainView.Instance.SetContent(filesPage);
                                }
                                else
                                {
                                    await StartTorrenting(selectedTorrent);
                                    var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId,
                                        selectedTorrent.MovieName,
                                        selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                                    MainView.Instance.SetContent(filesPage);
                                }
                            }
                        }
                        else
                        {
                            bool isExist =
                                (await Libtorrent.GetTorrentList()).Any(x =>
                                    x.Hash.ToLower() == selectedTorrent.Hash.ToLower());
                            if (isExist)
                            {
                                var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId,
                                    selectedTorrent.MovieName,
                                    selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                                MainView.Instance.SetContent(filesPage);
                            }
                            else
                            {
                                await StartTorrenting(selectedTorrent);
                                var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId,
                                    selectedTorrent.MovieName,
                                    selectedTorrent.ShowType, selectedTorrent.ImdbId, selectedTorrent);
                                MainView.Instance.SetContent(filesPage);
                            }
                        }
                    }

                
                    else
                    {
                        
                            if (!String.IsNullOrWhiteSpace(selectedTorrent.Hash))
                            {
                                Log.Information("Torrent hash is not empty");
                                bool isExist =
                                    (await Libtorrent.GetTorrentList()).Any(x =>
                                        x.Hash.ToLower() == selectedTorrent.Hash.ToLower());

                                if (isExist)
                                {
                                    var mediaFile = files.FirstOrDefault(x => x.IsMediaFile);
                                    Log.Information("Torrent already exist. Opening player");
                                    if (mediaFile != null)
                                    {
                                        var playerWindow = new PlayerWindow(selectedTorrent.MovieId,
                                            selectedTorrent.MovieName,
                                            selectedTorrent.ShowType, selectedTorrent.SeasonNumber,
                                            selectedTorrent.EpisodeNumber,
                                            new FileInfo(mediaFile.FullPath), selectedTorrent.IsCompleted,
                                            selectedTorrent.ImdbId, selectedTorrent, mediaFile.Index,
                                            selectedTorrent.Poster);
                                       
                                            MainWindow.Instance.SetContent(playerWindow);
                                        
                                    }
                                    else
                                    {
                                        Console.WriteLine("There is no media to play.");
                                    }
                                }
                                else
                                {
                                    Log.Information("Torrent doesnt exist. Started torrenting");
                                    await StartTorrenting(selectedTorrent);
                                    var mediaFile = files.FirstOrDefault(x => x.IsMediaFile);
                                    var playerWindow = new PlayerWindow(selectedTorrent.MovieId,
                                        selectedTorrent.MovieName,
                                        selectedTorrent.ShowType, selectedTorrent.SeasonNumber,
                                        selectedTorrent.EpisodeNumber,
                                        new FileInfo(mediaFile.FullPath), selectedTorrent.IsCompleted,
                                        selectedTorrent.ImdbId, selectedTorrent, mediaFile.Index,
                                        selectedTorrent.Poster);
                                    MainWindow.Instance.SetContent(playerWindow);
                                }
                            }
                            else
                            {
                                Log.Information("Torrent hash is empty");
                                await StartTorrenting(selectedTorrent);
                                Log.Information("Torrent hash found: " + selectedTorrent.Hash);
                                var mediaFile = files.FirstOrDefault(x => x.IsMediaFile);
                                Log.Information("Opening player");
                                if (mediaFile != null)
                                {
                                    var playerWindow = new PlayerWindow(selectedTorrent.MovieId,
                                        selectedTorrent.MovieName,
                                        selectedTorrent.ShowType, selectedTorrent.SeasonNumber,
                                        selectedTorrent.EpisodeNumber,
                                        new FileInfo(mediaFile.FullPath), selectedTorrent.IsCompleted,
                                        selectedTorrent.ImdbId,
                                        selectedTorrent, mediaFile.Index, selectedTorrent.Poster);
                                    MainWindow.Instance.SetContent(playerWindow);
                                }
                                else
                                {
                                    Console.WriteLine("There is no media to play.");
                                }
                            }
                        
                    }
                }
                catch (System.Exception exception)
                {
                    var errorMessage =
                        $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                    Log.Error(errorMessage);
                }
            }
            else
            {
                try
                {
                    e.Handled = true;
                    var grid = sender as Grid;
                    if (grid != null)
                    {
                        selectedTorrent = grid.DataContext as Item;
                        if (selectedTorrent == null) return;
                        selectedIndex = torrents.IndexOf(selectedTorrent);
                    }
                }
                catch (System.Exception exception)
                {
                    var errorMessage =
                        $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                    Log.Error(errorMessage);
                }
            }
        }
        private List<Visual> TraverseVisualTree(Visual parent)
        {
            var result = new List<Visual>();
            var children = parent.GetVisualChildren();
    
            foreach (var child in children)
            {
                if (child is Visual visual)
                {
                    result.Add(visual);
                    result.AddRange(TraverseVisualTree(visual));
                }
            }
    
            return result;
        }
        private static bool IsMouseOverTarget(Visual target, Point point)
        {
            var bounds = target.Bounds;
            return bounds.Contains(point);
        }

        public void Dispose()
        {
            try
            {
                // Unsubscribe from SizeChanged to prevent memory leak
                if (MainView.Instance != null)
                {
                    MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
                }
                
                this.DataContext = null;
            }
            catch (Exception ex)
            {
                Log.Error($"DownloadsPage Dispose error: {ex.Message}");
            }
        }

       void AddTorrentToDownloadHistory(Item selectedTorrent)
        {
            _ = Task.Run((async () =>
            {
                await FirestoreManager.AddShowToDownloadHistory(new Torrent()
                {
                    ImageUrl = selectedTorrent.ImageUrl,
                    Name = selectedTorrent.Title,
                    PublishDate = selectedTorrent.PublishDate,
                    Size = selectedTorrent.Size,
                    MovieId = selectedTorrent.MovieId,
                    MovieName = selectedTorrent.MovieName,
                    IsCompleted = selectedTorrent.IsCompleted,
                    ShowType = (int)selectedTorrent.ShowType,
                    EpisodeNumber = selectedTorrent.EpisodeNumber,
                    SeasonNumber = selectedTorrent.SeasonNumber,
                    ImdbId = selectedTorrent.ImdbId,
                    Hash = selectedTorrent.Hash
                });
            }));
            
        }
        private void ContextMenuPause_OnLoaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                var pauseMenuItem = sender as MenuItem;

                if (pauseMenuItem.DataContext is Item)
                {
                    var item = pauseMenuItem.DataContext as Item;
                    if (item.IsCompleted)
                    {
                        pauseMenuItem.IsVisible = false;
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
        
         private void ApplyDirectStylesToDownloadsItems(double width)
        {
            bool isExtraSmall = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
            bool isSmall = width <= SMALL_SCREEN_THRESHOLD && !isExtraSmall;
            
            // Ekran genişliğini maksimum 3840px ile sınırla
            double clampedWidth = Math.Min(width, 3840);
            
            // Öğe boyutlarını hesapla
            double itemWidth = CalculateItemWidth(clampedWidth);
            double itemHeight = CalculateItemHeight(clampedWidth);
            
            // Font boyutlarını hesapla
            double headerFontSize = CalculateTextSize(clampedWidth, 20, 50);
            double titleFontSize = CalculateTextSize(clampedWidth, 12, 30);
            double infoFontSize = CalculateTextSize(clampedWidth, 10, 24);
            double progressBarHeight = CalculateProgressBarHeight(clampedWidth);
            
            DownloadTextBlock.FontSize = headerFontSize;
            // DownloadsDisplay ItemsControl için öğeleri güncelle
            var downloadsDisplay = this.FindControl<ItemsControl>("DownloadsDisplay");
            if (downloadsDisplay == null || downloadsDisplay.Items == null) return;
            
            foreach (var item in downloadsDisplay.GetVisualDescendants())
            {
                // İndirme kartı Grid'ini güncelle
                if (item is Grid grid && grid.Name == "ItemGrid")
                {
                    // Kartın maksimum boyutlarını güncelle
                    grid.Width = itemWidth;
                    grid.MaxHeight = itemHeight;
                    grid.Margin = new Thickness(0, isExtraSmall ? 10 : (isSmall ? 15 : 20), 
                                               isExtraSmall ? 10 : (isSmall ? 15 : 20), 
                                               isExtraSmall ? 15 : (isSmall ? 20 : 30));
                }
                // İndirme bilgilerinin bulunduğu Grid'i güncelle
                else if (item is Grid infoGrid && infoGrid.Children.OfType<TextBlock>().Any(t => t.Name == "DownloadSpeedTextBlock" || t.Name == "EtaTextBlock"))
                {
                    // Küçük ekranlarda düzeni değiştir
                    if (isExtraSmall || isSmall)
                    {
                        // Grid'in mevcut tanımlarını temizle
                        infoGrid.RowDefinitions.Clear();
                        infoGrid.ColumnDefinitions.Clear();
                        
                        // Yeni satır tanımları ekle - 3 satır (hız, eta, ilerleme çubuğu)
                        infoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.06, GridUnitType.Star) });
                        infoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.06, GridUnitType.Star) });
                        infoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                        
                        // Tek sütun tanımı
                        infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        
                        // Öğeleri yeniden yerleştir
                        foreach (var child in infoGrid.Children)
                        {
                            if (child is TextBlock speedText && speedText.Name == "DownloadSpeedTextBlock")
                            {
                                Grid.SetRow(speedText, 0);
                                Grid.SetColumn(speedText, 0);
                                speedText.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                                speedText.Margin = new Thickness(20, 0, 0, 5);
                            }
                            else if (child is TextBlock etaText && etaText.Name == "EtaTextBlock")
                            {
                                Grid.SetRow(etaText, 1);
                                Grid.SetColumn(etaText, 0);
                                etaText.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                                etaText.Margin = new Thickness(20, 0, 0, 5);
                            }
                            else if (child is ProgressBar progressBar && progressBar.Name == "TorrentProgress")
                            {
                                Grid.SetRow(progressBar, 2);
                                Grid.SetColumn(progressBar, 0);
                                Grid.SetColumnSpan(progressBar, 1);
                            }
                        }
                    }
                    else
                    {
                        // Büyük ekranlar için normal düzeni geri yükle
                        infoGrid.RowDefinitions.Clear();
                        infoGrid.ColumnDefinitions.Clear();
                        
                        // Orijinal satır tanımları
                        infoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.06, GridUnitType.Star) });
                        infoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                        
                        // Orijinal sütun tanımları
                        infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.1, GridUnitType.Star) });
                        infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        
                        // Öğeleri orijinal pozisyonlarına yerleştir
                        foreach (var child in infoGrid.Children)
                        {
                            if (child is TextBlock speedText && speedText.Name == "DownloadSpeedTextBlock")
                            {
                                Grid.SetRow(speedText, 0);
                                Grid.SetColumn(speedText, 0);
                                speedText.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                                speedText.Margin = new Thickness(20, 0, 0, 0);
                            }
                            else if (child is TextBlock etaText && etaText.Name == "EtaTextBlock")
                            {
                                Grid.SetRow(etaText, 0);
                                Grid.SetColumn(etaText, 2);
                                etaText.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
                                etaText.Margin = new Thickness(20, 0, 0, 0);
                            }
                            else if (child is ProgressBar progressBar && progressBar.Name == "TorrentProgress")
                            {
                                Grid.SetRow(progressBar, 1);
                                Grid.SetColumn(progressBar, 0);
                                Grid.SetColumnSpan(progressBar, 3);
                            }
                        }
                    }
                }
                // Başlık yazılarını güncelle
                else if (item is TextBlock textBlock)
                {
                    if (textBlock.Name == "TitleTextBlock")
                    {
                        textBlock.FontSize = titleFontSize;
                        textBlock.MaxWidth = itemWidth * 0.8;
                    }
                    else if (textBlock.Name == "PublishDateTextBlock")
                    {
                        textBlock.FontSize = infoFontSize;
                    }
                    else if (textBlock.Name == "SizePropertyTextBlock")
                    {
                        textBlock.FontSize = infoFontSize;
                    }
                    else if (textBlock.Name == "DownloadSpeedTextBlock")
                    {
                        textBlock.FontSize = infoFontSize;
                    }
                    else if (textBlock.Name == "EtaTextBlock")
                    {
                        textBlock.FontSize = infoFontSize;
                    }
                }
                // İlerleme çubuğunu güncelle
                else if (item is ProgressBar progressBar && progressBar.Name == "TorrentProgress")
                {
                    progressBar.Height = progressBarHeight;
                   // progressBar.MaxWidth = itemWidth * 0.60;
                }
            }
        }
        private double CalculateScaledValue(double width, double minValue, double maxValue)
        {
            // Ekran genişliği sınırları
            const double minWidth = 320;   // En küçük ekran genişliği (piksel)
            const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
            
            // Ekran genişliğini sınırla
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Doğrusal ölçeklendirme formülü
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);
            
            // Değeri yuvarla
            return Math.Round(scaledValue);
        }
        // İndirme kartı genişliği için ölçekleme
        private double CalculateItemWidth(double width)
        {
            return CalculateScaledValue(width, 320, 2000);
        }
        
        // İndirme kartı yüksekliği için ölçekleme
        private double CalculateItemHeight(double width)
        {
            return CalculateScaledValue(width, 300, 900);
        }
        
        // Font boyutu için ölçekleme
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }
        
        // İlerleme çubuğu yüksekliği için ölçekleme
        private double CalculateProgressBarHeight(double width)
        {
            return CalculateScaledValue(width, 2, 9);
        }
        
    }
} 