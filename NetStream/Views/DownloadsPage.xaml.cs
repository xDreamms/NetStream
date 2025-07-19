using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
using CliWrap;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using NetStream.Annotations;
using Newtonsoft.Json;
using TMDbLib.Objects.Movies;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;
using Path = System.IO.Path;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for DownloadsPage.xaml
    /// </summary>
    //public partial class DownloadsPage : Page
    //{
    //    public static DownloadsPage GetDownloadsPageInstance = null;
    //    public static FastObservableCollection<Item> torrents = new FastObservableCollection<Item>();
    //    private Item torrent;
    //    private List<Torrent> savedTorrents;
    //    private List<string> Trackers = new List<string>();
    //    public DownloadsPage(Item torrent)
    //    {
    //        InitializeComponent();
    //        this.torrent = torrent;
    //        OnLoadWithTorrent();
    //        HomePage.GetHomePageInstance.MyEvent += HomePageOnMyEvent;
    //    }

    //    public DownloadsPage()
    //    {
    //        InitializeComponent();
    //        OnLoad();
    //        HomePage.GetHomePageInstance.MyEvent += HomePageOnMyEvent;
    //    }

    //    private async void OnLoadWithTorrent()
    //    {
    //        Trackers = await GetTrackers();
    //        DownloadsDisplay.ItemsSource = torrents;
    //        if (torrents.Count == 0)
    //            await LoadTorrents();
    //        if (torrent != null)
    //        {
    //            torrents.Add(torrent);
    //            await SaveNewTorrent();
    //        }
    //        await StartTorrenting();
    //    }

    //    private async void OnLoad()
    //    {
    //        Trackers = await GetTrackers();
    //        DownloadsDisplay.ItemsSource = torrents;
    //        if(torrents.Count == 0)
    //            await LoadTorrents();
    //        await StartTorrenting();
    //    }

    //    public readonly string[] SizeSuffixes =
    //        { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    //    public string SizeSuffix(Int64 value, int decimalPlaces = 1)
    //    {
    //        if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
    //        if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
    //        if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

    //        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
    //        int mag = (int)Math.Log(value, 1024);

    //        // 1L << (mag * 10) == 2 ^ (10 * mag) 
    //        // [i.e. the number of bytes in the unit corresponding to mag]
    //        decimal adjustedSize = (decimal)value / (1L << (mag * 10));

    //        // make adjustment when the value is large enough that
    //        // it would round up to 1000 or more
    //        if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
    //        {
    //            mag += 1;
    //            adjustedSize /= 1024;
    //        }

    //        return string.Format("{0:n" + decimalPlaces + "} {1}",
    //            adjustedSize,
    //            SizeSuffixes[mag]);
    //    }
    //    private async Task LoadTorrents()
    //    {
    //        try
    //        {
    //            var js = await File.ReadAllTextAsync(AppSettingsManager.appSettings.downloadingTorrentsJson);
    //            if (!String.IsNullOrWhiteSpace(js))
    //                savedTorrents = JsonConvert.DeserializeObject<List<Torrent>>(js);
    //            if (savedTorrents != null)
    //            {
    //                foreach (var savedTorrent in savedTorrents)
    //                {
    //                    var item = new Item();
    //                    item.Poster = (savedTorrent.ImageUrl);
    //                    item.Title = savedTorrent.Name;
    //                    item.SizeProperty = SizeSuffix((Int64)savedTorrent.Size);
    //                    item.Size = savedTorrent.Size;
    //                    item.Link = savedTorrent.DownloadLink;
    //                    item.PublishDate = savedTorrent.PublishDate;
    //                    item.ImageUrl = savedTorrent.ImageUrl;
    //                    item.MovieId = savedTorrent.MovieId;
    //                    item.MovieName = savedTorrent.MovieName;
    //                    item.IsCompleted = savedTorrent.IsCompleted;
    //                    item.ShowType = (ShowType)savedTorrent.ShowType;
    //                    item.SeasonNumber = savedTorrent.SeasonNumber;
    //                    item.EpisodeNumber = savedTorrent.EpisodeNumber;
    //                    item.TorrentLocation = savedTorrent.TorrentLocation;
    //                    item.ImdbId = savedTorrent.ImdbId;
    //                    item.Magnet = savedTorrent.Magnet;
    //                    item.ContainingDirectory = savedTorrent.ContainingDirectory;
    //                    item.FileNames = savedTorrent.FileNames;
    //                    torrents.Add(item);
    //                }
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            MessageBox.Show(e.Message);
    //        }
    //    }

    //    public List<string> openedTorrents = new List<string>();
    //    private async void DownloadsDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    //    {
    //        var selectedTorrent = DownloadsDisplay.SelectedItem as Item;
    //        if(selectedTorrent == null) return;
    //        if (selectedTorrent.EpisodeNumber == -1)
    //        {
                
    //                if (selectedTorrent.TorrentManager != null)
    //                {
    //                    var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId,selectedTorrent.MovieName,selectedTorrent.TorrentManager,selectedTorrent.ShowType, selectedTorrent.ShowType == ShowType.Movie ? -1 : selectedTorrent.SeasonNumber, selectedTorrent.ImdbId);
    //                    this.NavigationService.Navigate(filesPage);
    //                    DownloadsDisplay.UnselectAll();
    //                }
    //                else
    //                {
    //                    var a = new TorrentDownloader()
    //                    {
    //                        Torrent = selectedTorrent,
    //                        TorrentPath = selectedTorrent.TorrentLocation == null || String.IsNullOrWhiteSpace(selectedTorrent.TorrentLocation) ? await DownloadTorrent(selectedTorrent.Link, selectedTorrent.Title) : selectedTorrent.TorrentLocation,
    //                        Magnet = selectedTorrent.Magnet
    //                    };

    //                    if (!String.IsNullOrWhiteSpace(a.TorrentPath) || !String.IsNullOrWhiteSpace(a.Magnet))
    //                    {
    //                        selectedTorrent.TorrentLocation = a.TorrentPath;
    //                        await StartTorrenting(a);

    //                        var filesPage = new DownloadsFilesPage(selectedTorrent.MovieId, selectedTorrent.MovieName,
    //                            selectedTorrent.TorrentManager, selectedTorrent.ShowType,
    //                            selectedTorrent.ShowType == ShowType.Movie ? -1 : selectedTorrent.SeasonNumber,
    //                            selectedTorrent.ImdbId);
    //                        this.NavigationService.Navigate(filesPage);
    //                        DownloadsDisplay.UnselectAll();
    //                    }
    //                }
                
    //        }
    //        else
    //        {
    //            if (selectedTorrent != null && !(openedTorrents.Any(x => x == selectedTorrent.Link)))
    //            {
    //                if (selectedTorrent.TorrentManager != null)
    //                {
    //                    var playerWindow = new PlayerWindow(selectedTorrent.MovieId, selectedTorrent.MovieName, selectedTorrent.ShowType, selectedTorrent.SeasonNumber, selectedTorrent.EpisodeNumber,
    //                        selectedTorrent.TorrentManager, selectedTorrent.TorrentManager.Complete ? selectedTorrent.TorrentManager.Files.OrderByDescending(x => x.BitField.Length).FirstOrDefault().FullPath : "", selectedTorrent.ImdbId);
    //                    playerWindow.Show();
    //                    openedTorrents.Add(selectedTorrent.Link);
    //                    playerWindow.Unloaded += (o, args) =>
    //                    {
    //                        var itemToRemove = openedTorrents.FirstOrDefault(x => x == selectedTorrent.Link);
    //                        openedTorrents.Remove(itemToRemove);
    //                    };
    //                    DownloadsDisplay.UnselectAll();
    //                }
    //                else
    //                {
    //                    var a = new TorrentDownloader()
    //                    {
    //                        Torrent = selectedTorrent,
    //                        TorrentPath = selectedTorrent.TorrentLocation == null || String.IsNullOrWhiteSpace(selectedTorrent.TorrentLocation) ? await DownloadTorrent(selectedTorrent.Link, selectedTorrent.Title) : selectedTorrent.TorrentLocation,
    //                        Magnet = selectedTorrent.Magnet
    //                    };
    //                    if (!String.IsNullOrWhiteSpace(a.TorrentPath) || !String.IsNullOrWhiteSpace(a.Magnet))
    //                    {
    //                        selectedTorrent.TorrentLocation = a.TorrentPath;
    //                        await StartTorrenting(a);

    //                        var playerWindow = new PlayerWindow(selectedTorrent.MovieId, selectedTorrent.MovieName,
    //                            selectedTorrent.ShowType, selectedTorrent.SeasonNumber, selectedTorrent.EpisodeNumber,
    //                            selectedTorrent.TorrentManager,
    //                            selectedTorrent.TorrentManager.Complete
    //                                ? selectedTorrent.TorrentManager.Files.OrderByDescending(x => x.BitField.Length)
    //                                    .FirstOrDefault().FullPath
    //                                : "", selectedTorrent.ImdbId);
    //                        playerWindow.Show();
    //                        openedTorrents.Add(selectedTorrent.Link);
    //                        playerWindow.Unloaded += (o, args) =>
    //                        {
    //                            var itemToRemove = openedTorrents.FirstOrDefault(x => x == selectedTorrent.Link);
    //                            openedTorrents.Remove(itemToRemove);
    //                        };
    //                        DownloadsDisplay.UnselectAll();
    //                    }
    //                }
    //            }
    //        }
            
    //    }

    //    private void DownloadsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    //    {
            
    //    }

    //    private async void DownloadsPage_OnLoaded(object sender, RoutedEventArgs e)
    //    {
    //        if (MovieDetailsPage.current != null)
    //        {
    //            MovieDetailsPage.current.Id = 0;
    //        }
    //        SaveAllTorrents();
    //        UpdateUI();
    //        await StartTorrenting();
    //    }

    //    private void HomePageOnMyEvent(object? sender, EventArgs e)
    //    {
    //        if(this.NavigationService != null && this.NavigationService.CanGoBack)
    //            this.NavigationService.GoBack();
    //    }

    //    private void SaveAllTorrents()
    //    {
    //        List<Torrent> json = new List<Torrent>();

    //        if (torrents.Count > 0)
    //        {
    //            foreach (var item in torrents)
    //            {
    //                json.Add(new Torrent()
    //                {
    //                    DownloadLink = item.Link,
    //                    ImageUrl = item.ImageUrl,
    //                    Name = item.Title,
    //                    PublishDate = item.PublishDate,
    //                    Size = item.Size,
    //                    MovieId = item.MovieId,
    //                    MovieName = item.MovieName,
    //                    IsCompleted = item.IsCompleted,
    //                    ShowType = (int)item.ShowType,
    //                    EpisodeNumber = item.EpisodeNumber,
    //                    SeasonNumber = item.SeasonNumber,
    //                    TorrentLocation = item.TorrentLocation,
    //                    ImdbId = item.ImdbId,
    //                    Magnet = item.Magnet,
    //                    ContainingDirectory = item.ContainingDirectory,
    //                    FileNames = item.FileNames
    //                });
    //            }

    //            if (json.Count > 0)
    //            {
    //                var js = JsonConvert.SerializeObject(json);
    //                File.WriteAllText(AppSettingsManager.appSettings.downloadingTorrentsJson, js);
    //            }
    //        }
    //    }

    //    private void UpdateUI()
    //    {
    //        try
    //        {
    //            foreach (var item in torrents)
    //            {
    //                if (item.IsCompleted)
    //                {
    //                    item.DownloadPercent = 100;
    //                    item.DownloadSpeed = "";
    //                    item.Eta = Application.Current.Resources["CompletedString"].ToString();
    //                }
    //            }
    //        }
    //        catch (Exception exception)
    //        {
    //            MessageBox.Show(exception.Message);
    //        }
    //    }

    //    private async Task SaveNewTorrent()
    //    {
    //        try
    //        {
    //            List<Torrent> listTorrents = new List<Torrent>();

    //            if (savedTorrents != null)
    //            {
    //                foreach (var to in savedTorrents)
    //                {
    //                    listTorrents.Add(to);
    //                }

    //            }

    //            Torrent t = new Torrent();
    //            t.Name = torrent.Title;
    //            t.Size = torrent.Size;
    //            t.DownloadLink = torrent.Link;
    //            t.ImageUrl = torrent.ImageUrl;
    //            t.PublishDate = torrent.PublishDate;
    //            t.MovieId = torrent.MovieId;
    //            t.MovieName = torrent.MovieName;
    //            t.IsCompleted = torrent.IsCompleted;
    //            t.ShowType = (int) torrent.ShowType;
    //            t.SeasonNumber = torrent.SeasonNumber;
    //            t.EpisodeNumber = torrent.EpisodeNumber;
    //            t.TorrentLocation = torrent.TorrentLocation;
    //            t.ImdbId = torrent.ImdbId;
    //            t.Magnet = torrent.Magnet;
    //            t.ContainingDirectory = torrent.ContainingDirectory;
    //            t.FileNames = torrent.FileNames;

    //            listTorrents.Add(t);

    //            if (listTorrents.Count > 0)
    //            {
    //                var js = JsonConvert.SerializeObject(listTorrents);
    //                File.WriteAllText(AppSettingsManager.appSettings.downloadingTorrentsJson, js);
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            MessageBox.Show(e.Message);
    //        }
    //    }


    //    public async Task<string> DownloadTorrent(string url,string name)
    //    {
    //        try
    //        {
    //            if (url.ToLower().Contains("magnet"))
    //            {
    //                return "";
    //            }
    //            else
    //            {
    //                using (HttpClient httpClient = new HttpClient())
    //                {
    //                    using (var s = await httpClient.GetStreamAsync(url))
    //                    {
    //                        string path = System.IO.Path.Combine(AppSettingsManager.appSettings.TorrentsPath, name + ".torrent");
    //                        if (!File.Exists(path))
    //                        {
    //                            using (var fs = new FileStream(path, FileMode.CreateNew))
    //                            {
    //                                await s.CopyToAsync(fs);
    //                                return path;
    //                            }
    //                        }
    //                        else
    //                        {
    //                            return path;
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            MessageBox.Show(e.Message);
    //        }

    //        return "InvalidURL";
    //    }

    //    private async Task<List<TorrentDownloader>> FetchTorrentData()
    //    {
    //        try
    //        {
    //            List<TorrentDownloader> torrentDownloaders = new List<TorrentDownloader>();

    //            foreach (var item in torrents)
    //            {
    //                if (item.TorrentManager == null && !item.IsCompleted)
    //                {
    //                    var a = new TorrentDownloader()
    //                    {
    //                        Torrent = item,
    //                        TorrentPath = item.TorrentLocation == null || String.IsNullOrWhiteSpace(item.TorrentLocation) ? await DownloadTorrent(item.Link, item.Title):item.TorrentLocation
    //                    };
    //                    if (!String.Equals(a.TorrentPath, "InvalidURL"))
    //                    {
    //                        if (!String.IsNullOrWhiteSpace(a.TorrentPath))
    //                        {
    //                            item.TorrentLocation = a.TorrentPath;
    //                        }
    //                        else
    //                        {
    //                            a.Magnet = item.Link;
    //                            item.Magnet = a.Magnet;
    //                        }
    //                        torrentDownloaders.Add(a);
    //                    }
    //                }
    //            }
    //            return torrentDownloaders;
    //        }
    //        catch (Exception e)
    //        {
    //            MessageBox.Show(e.Message);
    //        }

    //        return new List<TorrentDownloader>();

    //    }
    //    DispatcherTimer dispatcherTimerForPlayerControl = new DispatcherTimer();

    
    //    private async Task StartTorrenting()
    //    {
    //        try
    //        {
    //            foreach (var torrentData in await FetchTorrentData())
    //            {
    //                if (!String.IsNullOrWhiteSpace(torrentData.TorrentPath))
    //                {
    //                    var torrent = await MonoTorrent.Torrent.LoadAsync(torrentData.TorrentPath);
    //                    TorrentManager manager = await InitClientEngine().AddStreamingAsync(torrent, AppSettingsManager.appSettings.MoviesPath,GetTorrentSettings());
    //                    foreach (var trackerUrl in Trackers)
    //                    {
    //                        var uri = new Uri(trackerUrl);
    //                        await manager.TrackerManager.AddTrackerAsync(uri);
    //                    }
    //                    await manager.StartAsync();

    //                    if (!manager.HasMetadata)
    //                    {
    //                        await manager.WaitForMetadataAsync();
    //                    }

    //                    torrentData.Torrent.ContainingDirectory = manager.ContainingDirectory;
    //                    if (String.Equals(manager.ContainingDirectory, AppSettingsManager.appSettings.MoviesPath))
    //                    {
    //                        torrentData.Torrent.FileNames = manager.Files.Select(x => x.FullPath).ToList();
    //                    }

    //                    torrentData.Torrent.IsCompleted = manager.Complete;
    //                    torrentData.manager = manager;
    //                    torrentData.Torrent.TorrentManager = manager;

    //                    manager.TorrentStateChanged += (sender, e) => ManagerOnTorrentStateChanged(sender, e, torrentData);

    //                    dispatcherTimerForPlayerControl.Interval = TimeSpan.FromSeconds(1);
    //                    dispatcherTimerForPlayerControl.Tick += (sender, e) => DispatcherTimerForPlayerControlOnTick(sender, e, torrentData.Torrent, manager);
    //                    dispatcherTimerForPlayerControl.Start();
    //                }
    //                else
    //                {
    //                    if (!String.IsNullOrWhiteSpace(torrentData.Magnet))
    //                    {
    //                        MagnetLink magnetLink = FromUri(new Uri(torrentData.Magnet));
    //                        TorrentManager manager = await InitClientEngine().AddStreamingAsync(magnetLink, AppSettingsManager.appSettings.MoviesPath, GetTorrentSettings());
    //                        foreach (var trackerUrl in Trackers)
    //                        {
    //                            var uri = new Uri(trackerUrl);
    //                            await manager.TrackerManager.AddTrackerAsync(uri);
    //                        }
    //                        await manager.StartAsync();

    //                        if (!manager.HasMetadata)
    //                        {
    //                            await manager.WaitForMetadataAsync();
    //                        }

    //                        torrentData.Torrent.ContainingDirectory = manager.ContainingDirectory;
    //                        if (String.Equals(manager.ContainingDirectory, AppSettingsManager.appSettings.MoviesPath))
    //                        {
    //                            torrentData.Torrent.FileNames = manager.Files.Select(x => x.FullPath).ToList();
    //                        }

    //                        torrentData.Torrent.IsCompleted = manager.Complete;
    //                        torrentData.manager = manager;
    //                        torrentData.Torrent.TorrentManager = manager;

    //                        manager.TorrentStateChanged += (sender, e) => ManagerOnTorrentStateChanged(sender, e, torrentData);

    //                        dispatcherTimerForPlayerControl.Interval = TimeSpan.FromSeconds(1);
    //                        dispatcherTimerForPlayerControl.Tick += (sender, e) => DispatcherTimerForPlayerControlOnTick(sender, e, torrentData.Torrent, manager);
    //                        dispatcherTimerForPlayerControl.Start();
    //                    }
    //                }
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            MessageBox.Show(e.Message);
    //        }
    //    }

    //    public static MagnetLink FromUri(Uri uri)
    //    {
    //        InfoHashes? infoHashes = null;
    //        string? name = null;
    //        var announceUrls = new List<string>();
    //        var webSeeds = new List<string>();
    //        long? size = null;

    //        if (uri.Scheme != "magnet")
    //            throw new FormatException("Magnet links must start with 'magnet:'.");

    //        string[] parameters = uri.Query.Substring(1).Split('&');
    //        for (int i = 0; i < parameters.Length; i++)
    //        {
    //            string[] keyval = parameters[i].Split('=');
    //            if (keyval.Length != 2)
    //            {
    //                // Skip anything we don't understand. Urls could theoretically contain many
    //                // unknown parameters.
    //                continue;
    //            }
    //            switch (keyval[0].Substring(0, 2))
    //            {
    //                case "xt"://exact topic
    //                    string val = keyval[1].Substring(9);
    //                    switch (keyval[1].Substring(0, 9))
    //                    {
    //                        case "urn:sha1:"://base32 hash
    //                        case "urn:btih:":
    //                            if (infoHashes?.V1 != null)
    //                                throw new FormatException("More than one v1 infohash in magnet link is not allowed.");

    //                            if (val.Length == 32)
    //                                infoHashes = new InfoHashes(InfoHash.FromBase32(val), infoHashes?.V2);
    //                            else if (val.Length == 40)
    //                                infoHashes = new InfoHashes(InfoHash.FromHex(val), infoHashes?.V2);
    //                            else
    //                                throw new FormatException("Infohash must be base32 or hex encoded.");
    //                            break;

    //                        case "urn:btmh:":
    //                            if (infoHashes?.V2 != null)
    //                                throw new FormatException("More than one v2 multihash in magnet link is not allowed.");

    //                            // BEP52: Support v2 magnet links
    //                            infoHashes = new InfoHashes(infoHashes?.V1, InfoHash.FromMultiHash(val));
    //                            break;
    //                    }
    //                    break;
    //                case "tr"://address tracker
    //                    announceUrls.Add(keyval[1].UrlDecodeUTF8());
    //                    break;
    //                case "as"://Acceptable Source
    //                    webSeeds.Add(keyval[1].UrlDecodeUTF8());
    //                    break;
    //                case "dn"://display name
    //                    name = keyval[1].UrlDecodeUTF8();
    //                    break;
    //                case "xl"://exact length
    //                    size = long.Parse(keyval[1]);
    //                    break;
    //                //case "xs":// eXact Source - P2P link.
    //                //case "kt"://keyword topic
    //                //case "mt"://manifest topic
    //                // Unused
    //                //break;
    //                default:
    //                    // Unknown/unsupported
    //                    break;
    //            }
    //        }

    //        if (infoHashes == null)
    //            throw new FormatException("The magnet link did not contain a valid 'xt' parameter referencing the infohash");

    //        return new MagnetLink(infoHashes, name, announceUrls, webSeeds, size);
    //    }

    //    public async Task StartTorrenting(TorrentDownloader torrentData)
    //    {
    //        try
    //        {
    //            if (!String.IsNullOrWhiteSpace(torrentData.Magnet))
    //            {
    //                MagnetLink magnetLink = FromUri(new Uri(torrentData.Magnet));
    //                TorrentManager manager = await InitClientEngine().AddStreamingAsync(magnetLink, AppSettingsManager.appSettings.MoviesPath, GetTorrentSettings());
    //                foreach (var trackerUrl in Trackers)
    //                {
    //                    var uri = new Uri(trackerUrl);
    //                    await manager.TrackerManager.AddTrackerAsync(uri);
    //                }
    //                await manager.StartAsync();

    //                if (!manager.HasMetadata)
    //                {
    //                    await manager.WaitForMetadataAsync();
    //                }

    //                torrentData.Torrent.ContainingDirectory = manager.ContainingDirectory;
    //                if (String.Equals(manager.ContainingDirectory, AppSettingsManager.appSettings.MoviesPath))
    //                {
    //                    torrentData.Torrent.FileNames = manager.Files.Select(x => x.FullPath).ToList();
    //                }

    //                torrentData.manager = manager;
    //                torrentData.Torrent.TorrentManager = manager;
    //            }
    //            else
    //            {
    //                var torrent = await MonoTorrent.Torrent.LoadAsync(torrentData.TorrentPath);
    //                TorrentManager manager = await InitClientEngine().AddStreamingAsync(torrent, AppSettingsManager.appSettings.MoviesPath, GetTorrentSettings());
    //                foreach (var trackerUrl in Trackers)
    //                {
    //                    var uri = new Uri(trackerUrl);
    //                    await manager.TrackerManager.AddTrackerAsync(uri);
    //                }
    //                await manager.StartAsync();

    //                if (!manager.HasMetadata)
    //                {
    //                    await manager.WaitForMetadataAsync();
    //                }

    //                torrentData.Torrent.ContainingDirectory = manager.ContainingDirectory;
    //                if (String.Equals(manager.ContainingDirectory, AppSettingsManager.appSettings.MoviesPath))
    //                {
    //                    torrentData.Torrent.FileNames = manager.Files.Select(x => x.FullPath).ToList();
    //                }

    //                torrentData.manager = manager;
    //                torrentData.Torrent.TorrentManager = manager;
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            MessageBox.Show(e.Message);
    //        }
    //    }

    //    private void ManagerOnTorrentStateChanged(object? sender, TorrentStateChangedEventArgs e,TorrentDownloader torrentData)
    //    {
    //        foreach (var item in torrents)
    //        {
    //            if (item == torrentData.Torrent)
    //            {
    //                if (e.NewState == TorrentState.Downloading)
    //                {
    //                    item.TorrentState = TorrentState.Downloading;
    //                }

    //                if (e.NewState == TorrentState.Paused)
    //                {
    //                    item.TorrentState = TorrentState.Paused;
    //                }

    //                if (e.NewState == TorrentState.Stopped)
    //                {
    //                    item.TorrentState = TorrentState.Stopped;
    //                }
    //            }
                
    //        }
    //    }

    //    public string ConvertBytesToMegaBytes(long bytes)
    //    {
    //        double scalingFactor;
    //        string formatText;

    //        //Figure out if we should display in bytes, kilobytes or megabytes per second.
    //        if (bytes < 1024)
    //        {
    //            formatText = "{0:N0} B/sec";
    //            scalingFactor = 1;
    //        }
    //        else if (bytes < 1024 * 1024)
    //        {
    //            formatText = "{0:N2} Kb/sec";
    //            scalingFactor = 1024;
    //        }
    //        else
    //        {
    //            formatText = "{0:N2} Mb/sec";
    //            scalingFactor = 1024 * 1024;
    //        }

    //        //Display the speed to the user, scaled to the correct size.
    //        return String.Format(formatText, bytes / scalingFactor);
    //    }
    //    private void DispatcherTimerForPlayerControlOnTick(object? sender, EventArgs e,Item torrent,TorrentManager manager)
    //    {
    //        try
    //        {
    //            foreach (var item in torrents)
    //            {
    //                if (item == torrent)
    //                {
    //                    item.DownloadPercent = manager.Progress;
    //                    item.DownloadSpeed = Application.Current.Resources["DownloadSpeedString"] + ": " +
    //                                         ConvertBytesToMegaBytes(manager.Monitor.DownloadRate);
    //                    //// Calculate ETA
    //                    if (manager.Complete)
    //                    {
    //                        if (!item.IsCompleted)
    //                        {
    //                            item.Eta = Application.Current.Resources["CompletedString"].ToString();
    //                            item.DownloadSpeed = "";
    //                            item.IsCompleted = true;
    //                            SaveAllTorrents();
    //                            //var notificationManager = new Notifications.Wpf.NotificationManager();

    //                            //notificationManager.Show(new NotificationContent
    //                            //{
    //                            //    Title = item.MovieName,
    //                            //    Message = "Downloaded successfully",
    //                            //    Type = NotificationType.Information
    //                            //}, "", TimeSpan.FromSeconds(5), onClick: (() =>
    //                            //{
    //                            //    if (item != null && !(openedTorrents.Any(x => x == item.Link)))
    //                            //    {
    //                            //        if (item.TorrentManager != null)
    //                            //        {
    //                            //            var playerWindow = new PlayerWindow(item.MovieId,
    //                            //                item.MovieName, item.ShowType,
    //                            //                item.SeasonNumber, item.EpisodeNumber,
    //                            //                item.TorrentManager,
    //                            //                item.TorrentManager.Files
    //                            //                    .OrderByDescending(x => x.BitField.Length).FirstOrDefault().FullPath
    //                            //                , item.ImdbId);
    //                            //            playerWindow.Show();
    //                            //            openedTorrents.Add(item.Link);
    //                            //            playerWindow.Unloaded += (o, args) =>
    //                            //            {
    //                            //                var itemToRemove =
    //                            //                    openedTorrents.FirstOrDefault(x => x == item.Link);
    //                            //                openedTorrents.Remove(itemToRemove);
    //                            //            };
    //                            //            DownloadsDisplay.UnselectAll();
    //                            //        }
    //                            //    }
    //                            //}));
    //                        }
    //                    }
    //                    else
    //                    {
    //                        int MaxETA = 60 * 60 * 24 * 300;
    //                        var remainingSize = ((1 - manager.Progress / 100.0) * manager.Torrent.Size);
    //                        var eta = Math.Min(MaxETA, remainingSize /  manager.Monitor.DownloadRate);
    //                        TimeSpan time = TimeSpan.FromSeconds(eta);
    //                        string str = time.ToString(@"hh\:mm\:ss");
    //                        item.Eta = Application.Current.Resources["EtaString"] + ": " + str;
    //                    }

    //                }
    //            }
    //        }
    //        catch (Exception exception)
    //        {
    //            MessageBox.Show(exception.Message);
    //        }
    //    }

       



    //    private async void MenuItem_OnClick(object sender, RoutedEventArgs e)
    //    {
    //        try
    //        {
    //            var selectedTorrent = DownloadsDisplay.Items[selectedIndex] as Item;

    //            if (selectedTorrent != null)
    //            {
    //                if (!String.IsNullOrWhiteSpace(selectedTorrent.ContainingDirectory))
    //                {
    //                    if (String.Equals(selectedTorrent.ContainingDirectory, AppSettingsManager.appSettings.MoviesPath))
    //                    {
    //                        foreach (var selectedTorrentFileName in selectedTorrent.FileNames)
    //                        {
    //                            if (File.Exists(selectedTorrentFileName))
    //                            {
    //                                File.Delete(selectedTorrentFileName);
    //                            }
    //                        }
    //                    }
    //                    else
    //                    {
    //                        if (Directory.Exists(selectedTorrent.ContainingDirectory))
    //                        {
    //                            Directory.Delete(selectedTorrent.ContainingDirectory, true);
    //                        }
    //                    }
    //                }

    //                var itemToRemove = savedTorrents.FirstOrDefault(x => x.DownloadLink == selectedTorrent.Link);
    //                savedTorrents.Remove(itemToRemove);

    //                if (savedTorrents.Count > 0)
    //                {
    //                    var js = JsonConvert.SerializeObject(savedTorrents);
    //                    File.WriteAllText(AppSettingsManager.appSettings.downloadingTorrentsJson, js);
    //                }

    //                torrents.Remove(selectedTorrent);
    //                if (selectedTorrent.TorrentManager != null)
    //                {
    //                    await selectedTorrent.TorrentManager.StopAsync();
    //                }
    //            }
    //        }
    //        catch (Exception exception)
    //        {

    //            MessageBox.Show(exception.Message);
    //        }
    //    }

    //    private async void MenuItem_OnClickPause(object sender, RoutedEventArgs e)
    //    {
    //        try
    //        {
    //            var selectedTorrent = DownloadsDisplay.Items[selectedIndex] as Item;

    //            if (selectedTorrent != null)
    //            {
    //                var manager = selectedTorrent.TorrentManager;
    //                if (manager != null)
    //                {
    //                    if (selectedTorrent.TorrentManager.State == TorrentState.Downloading)
    //                    {
    //                        await manager.PauseAsync();
    //                        (sender as MenuItem).Header = Application.Current.Resources["PlayString"];
    //                    }
    //                    else
    //                    {
    //                        await manager.StartAsync();
    //                        (sender as MenuItem).Header = Application.Current.Resources["PauseString"];
    //                    }
    //                }
    //            }
    //        }
    //        catch (Exception exception)
    //        {
    //            MessageBox.Show(exception.Message);
    //        }
    //    }

    //    private int selectedIndex = -1;

    //    private void DownloadsDisplay_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    //    {
    //        e.Handled = true;

    //        for (int i = 0; i < torrents.Count; i++)
    //        {
    //            var lbi = DownloadsDisplay.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
    //            if (lbi == null) continue;
    //            if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
    //            {
    //                selectedIndex = i;
    //                break;
    //            }
    //        }
    //    }

    //    private static bool IsMouseOverTarget(Visual target, Point point)
    //    {
    //        var bounds = VisualTreeHelper.GetDescendantBounds(target);
    //        return bounds.Contains(point);
    //    }

    //    private ClientEngine InitClientEngine()
    //    {
    //        int DefaultPort = 55123;

    //        var settingBuilder = new EngineSettingsBuilder
    //        {
    //            MaximumConnections = 5000,
    //            MaximumOpenFiles = 500,
    //            MaximumUploadRate = 0,
    //            MaximumDownloadRate = 0,
    //            MaximumDiskReadRate = 0,
    //            MaximumDiskWriteRate = 0,
    //            MaximumHalfOpenConnections = 12,
    //            AllowPortForwarding = true,

    //            AutoSaveLoadDhtCache = true,

    //            AutoSaveLoadFastResume = true,

    //            AutoSaveLoadMagnetLinkMetadata = true,

    //            ListenEndPoints = new Dictionary<string, IPEndPoint> {
    //                { "ipv4", new IPEndPoint (IPAddress.Any, DefaultPort) },
    //                { "ipv6", new IPEndPoint (IPAddress.IPv6Any, DefaultPort) }
    //            },

    //            DhtEndPoint = new IPEndPoint(IPAddress.Any, DefaultPort),
    //        }.ToSettings();

    //        return new ClientEngine(settingBuilder);
    //    }

    //    private TorrentSettings GetTorrentSettings()
    //    {
    //        return new TorrentSettingsBuilder
    //        {
    //            MaximumConnections = 5000,
    //            UploadSlots = 200,
    //            MaximumUploadRate = 0,
    //            MaximumDownloadRate = 0
    //        }.ToSettings();
    //    }

    //    private ClientEngine InitClientEngine2()
    //    {
    //        const int httpListeningPort = 55125;

    //        var settingBuilder = new EngineSettingsBuilder
    //        {
    //            AllowPortForwarding = true,
    //            AutoSaveLoadDhtCache = true,
    //            AutoSaveLoadFastResume = true,

    //            AutoSaveLoadMagnetLinkMetadata = true,

    //            ListenEndPoints = new Dictionary<string, IPEndPoint> {
    //                { "ipv4", new IPEndPoint (IPAddress.Any, 55123) },
    //                { "ipv6", new IPEndPoint (IPAddress.IPv6Any, 55123) }
    //            },

    //            DhtEndPoint = new IPEndPoint(IPAddress.Any, 55123),

    //            HttpStreamingPrefix = $"http://0.0.0.0:{httpListeningPort}/"
    //        }.ToSettings();

    //        return new ClientEngine(settingBuilder);
    //    }

    //    private TorrentSettings GetTorrentSettings2()
    //    {
    //        return new TorrentSettingsBuilder()
    //        {
    //            CreateContainingDirectory = false,
    //            AllowPeerExchange = true,
    //            AllowInitialSeeding = true,

    //        }.ToSettings();
    //    }

    //    private static async Task<List<string>> GetTrackers()
    //    {
    //        try
    //        {
    //            var text = AppSettingsManager.appSettings.Trackers;
    //            var trackers = new List<string>();
    //            var lines = text.Split("\n");
    //            foreach (var line in lines)
    //                if (string.IsNullOrWhiteSpace(line) == false)
    //                    trackers.Add(line.Trim());
    //            return trackers;
    //        }
    //        catch
    //        {
    //            return new List<string>();
    //        }
    //    }

    //    private void MenuItem_ContextMenuDetailsOnClick(object sender, RoutedEventArgs e)
    //    {
    //        var selectedTorrent = DownloadsDisplay.Items[selectedIndex] as Item;

    //        if (selectedTorrent != null)
    //        {
    //            MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedTorrent.MovieId,
    //                selectedTorrent.ShowType, HomePage.GetHomePageInstance.HomePageNavigation);
    //            HomePage.GetHomePageInstance.HomePageNavigation.Navigate(movieDetailsPage);
    //        }
    //    }

    //    private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    //    {
    //        var pauseMenuItem = sender as MenuItem;

    //        if (pauseMenuItem.DataContext is Item)
    //        {
    //            var item = pauseMenuItem.DataContext as Item;
    //            if (item.IsCompleted)
    //            {
    //                pauseMenuItem.Visibility = Visibility.Collapsed;
    //            }
    //        }
    //    }
    //}

   
}
