using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using NetStream.Annotations;
using System.Xml.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using MovieCollection.OpenSubtitles.Models;
using RestSharp.Serializers;
using DataFormat = RestSharp.DataFormat;
using TorznabClient.Models;
using System.Security.Policy;
using TorznabClient.Exceptions;
using TorznabClient.Serializer;
using Windows.Media.Protection.PlayReady;
using System.Web;
using System.Net;
using System.Net.Http;
using NetStream.Views;
using Serilog;
using TorznabClient.Jackett;
using TorznabClient.Torznab;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.Movies;
using LibVLCSharp.Shared;

namespace NetStream
{
    [DeserializeAs(Name = "category")]
    public class Category : INotifyPropertyChanged
    {
        public Category()
        {

        }
        public Category(string id, string name)
        {
            Id = id;
            Name = name;
        }
        public string Id { get; set; }
        public string Name { get; set; }
        private bool enabled;
        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
                this.NotifyPropertyChanged("Enabled");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }
    }
    [XmlRoot(ElementName = "link")]
    public class Link
    {

        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }

        [XmlAttribute(AttributeName = "rel")]
        public string Rel { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "jackettindexer")]
    public class Jackettindexer
    {

        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "enclosure")]
    public class Enclosure
    {

        [XmlAttribute(AttributeName = "url")]
        public string Url { get; set; }

        [XmlAttribute(AttributeName = "length")]
        public double Length { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "attr")]
    public class Attr
    {

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public int Value { get; set; }
    }

    [XmlRoot(ElementName = "item")]
    public class Item : INotifyPropertyChanged
    {

        [XmlElement(ElementName = "title")]
        public string Title { get; set; }

        [XmlElement(ElementName = "guid")]
        public string Guid { get; set; }

        [XmlElement(ElementName = "jackettindexer")]
        public Jackettindexer Jackettindexer { get; set; }

        [XmlElement(ElementName = "type")]
        public string Type { get; set; }

        [XmlElement(ElementName = "comments")]
        public string Comments { get; set; }

        [XmlElement(ElementName = "pubDate")]
        public DateTime PubDate { get; set; }

        [XmlElement(ElementName = "size")]
        public double Size { get; set; }

        [XmlElement(ElementName = "description")]
        public object Description { get; set; }

        [XmlElement(ElementName = "link")]
        public string Link { get; set; }

        [XmlElement(ElementName = "category")]
        public List<int> Category { get; set; }

        [XmlElement(ElementName = "enclosure")]
        public Enclosure Enclosure { get; set; }

        [XmlElement(ElementName = "attr")]
        public List<Attr> Attr { get; set; }

        [XmlElement(ElementName = "grabs")]
        public int Grabs { get; set; }

        public string Poster { get; set; }

        public string ImageUrl { get; set; }

        public string PublishDate { get; set; }

        public string SizeProperty { get; set; }

        private double downloadPercent;
        public double DownloadPercent { 
            get
        
            {
                return downloadPercent;
            }
            set
            {
                downloadPercent = value;
                OnPropertyChanged("DownloadPercent");
            }
            
        }

        private string downloadSpeed;
        public string DownloadSpeed
        {
            get

            {
                return downloadSpeed;
            }
            set
            {
                downloadSpeed = value;
                OnPropertyChanged("DownloadSpeed");
            }

        }

        private string eta;
        public string Eta
        {
            get

            {
                return eta;
            }
            set
            {
                eta = value;
                OnPropertyChanged("Eta");
            }

        }

        //private TorrentManager torrentManager;
        //public TorrentManager TorrentManager
        //{
        //    get

        //    {
        //        return torrentManager;
        //    }
        //    set
        //    {
        //        torrentManager = value;
        //        OnPropertyChanged("TorrentManager");
        //    }
        //}

        //private TorrentState torrentState;
        //public TorrentState TorrentState
        //{
        //    get

        //    {
        //        return torrentState;
        //    }
        //    set
        //    {
        //        torrentState = value;
        //        OnPropertyChanged("TorrentState");
        //    }
        //}

        private int movieId;
        public int MovieId {
            get

            {
                return movieId;
            }
            set
            {
                movieId = value;
                OnPropertyChanged("MovieId");
            }
        }

        private string movieName;
        public string MovieName
        {
            get

            {
                return movieName;
            }
            set
            {
                movieName = value;
                OnPropertyChanged("MovieName");
            }
        }

        private bool isCompleted;

        public bool IsCompleted
        {
            get

            {
                return isCompleted;
            }
            set
            {
                isCompleted = value;
                OnPropertyChanged("IsCompleted");
            }
        }

        private int seasonNumber;
        public int  SeasonNumber {
            get

            {
                return seasonNumber;
            }
            set
            {
                seasonNumber = value;
                OnPropertyChanged("SeasonNumber");
            }
        }
        private int episodeNumber;
        public int EpisodeNumber {
            get

            {
                return episodeNumber;
            }
            set
            {
                episodeNumber = value;
                OnPropertyChanged("EpisodeNumber");
            }
        }

        private ShowType showType;
        public ShowType ShowType
        {
            get

            {
                return showType;
            }
            set
            {
                showType = value;
                OnPropertyChanged("ShowType");
            }
        }

        private string torrentLocation;

        public string TorrentLocation
        {
            get

            {
                return torrentLocation;
            }
            set
            {
                torrentLocation = value;
                OnPropertyChanged("TorrentLocation");
            }
        }

        private int seeders;
        public int Seeders {
            get

            {
                return seeders;
            }
            set
            {
                seeders = value;
                OnPropertyChanged("Seeders");
            }
        }

        private string seedersProperty;

        public string SeedersProperty
        {
            get

            {
                return seedersProperty;
            }
            set
            {
                seedersProperty = value;
                OnPropertyChanged("SeedersProperty");
            }
        }

        private int imdbId;

        public int ImdbId
        {
            get

            {
                return imdbId;
            }
            set
            {
                imdbId = value;
                OnPropertyChanged("ImdbId");
            }
        }

        public string Magnet { get; set; }

        public string ContainingDirectory { get; set; }
        public List<string> FileNames { get; set; }

        public string Hash { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [XmlRoot(ElementName = "channel")]
    public class Channel
    {

        [XmlElement(ElementName = "link")]
        public List<Link> Link { get; set; }

        [XmlElement(ElementName = "title")]
        public string Title { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "language")]
        public string Language { get; set; }

        [XmlElement(ElementName = "category")]
        public string Category { get; set; }

        [XmlElement(ElementName = "item")]
        public List<Item> Item { get; set; }
    }

    [XmlRoot(ElementName = "rss")]
    public class Rss
    {

        [XmlElement(ElementName = "channel")]
        public Channel Channel { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public double Version { get; set; }

        [XmlAttribute(AttributeName = "atom")]
        public string Atom { get; set; }

        [XmlAttribute(AttributeName = "torznab")]
        public string Torznab { get; set; }

        [XmlText]
        public string Text { get; set; }
    }
    public class JackettService
    {
        private static string BASE_URL = "";
        private static string API_KEY = "";

        public static void Init()
        {
            if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiUrl) &&
                !String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiKey))
            {
                var url = AppSettingsManager.appSettings.JacketApiUrl;
                if (url.Last() != '/')
                {
                    url += "/";
                }
                var key = AppSettingsManager.appSettings.JacketApiKey;

                BASE_URL = url;
                API_KEY = key;
                Log.Information("Initialized Jackett");
            }
            else
            {
                Log.Error("Jackett Initialization failed. Please go to appliation settings page and fill Jacket Api key and Jacket Api Url");
            }
        }

        public ObservableCollection<Category> MovieCategories = new ObservableCollection<Category>()
        {
            new Category("2000","All"),
            new Category("2030","SD"),
            new Category("2040","HD"),
            new Category("2080","WEB-DL"),
            new Category("2060","BluRay"),
            new Category("2050","3D"),
            new Category("2070","DVD"),
            new Category("2010","Foreign"),
            new Category("2020","Other")
        };

        public ObservableCollection<Category> TVCategories = new ObservableCollection<Category>()
        {
            new Category("5000","All"),
            new Category("5030","SD"),
            new Category("5040","HD"),
            new Category("5010","WEB-DL"),
            new Category("5060","Sport"),
            new Category("5070","Anime"),
            new Category("5080","Documentary"),
            new Category("5020","Foreign"),
            new Category("5999","Other")
        };
        //public static async Task<List<Item>> GetTvShowTorrentsWithName(string name, int season, int episode)
        //{
        //    try
        //    {
        //        List<Item> result = new List<Item>();
        //        var restClient = new RestClient(BASE_URL);
        //        var request = new RestRequest();
        //        request.XmlSerializer = new DotNetXmlSerializer();
        //        request.RequestFormat = DataFormat.Xml;
        //        request.AddParameter("apikey", API_KEY);
        //        request.AddParameter("t", "tvsearch");
        //        request.AddParameter("q", name).AddParameter("season", season.ToString()).AddParameter("ep", episode.ToString());
        //        var response = await restClient.ExecuteAsync<List<Item>>(request);

        //        if (response.IsSuccessful)
        //        {
        //            var document = XDocument.Parse(response.Content);
        //            var items = GetItems(document).ToList();

        //            foreach (var item in items)
        //            {
        //                string Title = GetTitle(item);
        //                DateTime PublishDate = GetPublishDate(item);
        //                string DownloadUrl = GetDownloadUrl(item);
        //                double Size = GetSize(item);
        //                int Seeders = GetSeeders(item).Value;

        //                result.Add(new Item()
        //                {
        //                    Title = Title,
        //                    PubDate = PublishDate,
        //                    Link = DownloadUrl,
        //                    Size = Size,
        //                    Seeders = Seeders
        //                });
        //            }
        //        }

        //        return result.OrderByDescending(x => x.Seeders).ToList();
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show(e.Message);
        //    }

        //    return new List<Item>();
        //}

        //public static async Task<List<Item>> GetTvShowTorrentsWithName(string name, int season)
        //{
        //    try
        //    {
        //        List<Item> result = new List<Item>();
        //        var restClient = new RestClient(BASE_URL);
        //        var request = new RestRequest();
        //        request.XmlSerializer = new DotNetXmlSerializer();
        //        request.RequestFormat = DataFormat.Xml;
        //        request.AddParameter("apikey", API_KEY);
        //        request.AddParameter("t", "tvsearch");
        //        request.AddParameter("q", name).AddParameter("season", season.ToString());
        //        var response = await restClient.ExecuteAsync<List<Item>>(request);

        //        if (response.IsSuccessful)
        //        {
        //            var document = XDocument.Parse(response.Content);
        //            var items = GetItems(document).ToList();

        //            foreach (var item in items)
        //            {
        //                string Title = GetTitle(item);
        //                DateTime PublishDate = GetPublishDate(item);
        //                string DownloadUrl = GetDownloadUrl(item);
        //                double Size = GetSize(item);
        //                int Seeders = GetSeeders(item).Value;

        //                result.Add(new Item()
        //                {
        //                    Title = Title,
        //                    PubDate = PublishDate,
        //                    Link = DownloadUrl,
        //                    Size = Size,
        //                    Seeders = Seeders
        //                });
        //            }
        //        }

        //        return result.OrderByDescending(x=> x.Title.ToLower().Contains("complete"))
        //            .ThenByDescending(x=> !x.Title.ToLower().Contains("e"))
        //            .ThenByDescending(x => x.Seeders).ToList();
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show(e.Message);
        //    }

        //    return new List<Item>();
        //}
        //public static async Task<List<Item>> GetTvShowTorrentsWithName(string name)
        //{
        //    try
        //    {
        //        List<Item> result = new List<Item>();
        //        var restClient = new RestClient(BASE_URL);
        //        var request = new RestRequest();
        //        request.XmlSerializer = new DotNetXmlSerializer();
        //        request.RequestFormat = DataFormat.Xml;
        //        request.AddParameter("apikey", API_KEY);
        //        request.AddParameter("t", "tvsearch");
        //        request.AddParameter("q", name);
        //        var response = await restClient.ExecuteAsync<List<Item>>(request);

        //        if (response.IsSuccessful)
        //        {
        //            var document = XDocument.Parse(response.Content);
        //            var items = GetItems(document).ToList();

        //            foreach (var item in items)
        //            {
        //                string Title = GetTitle(item);
        //                DateTime PublishDate = GetPublishDate(item);
        //                string DownloadUrl = GetDownloadUrl(item);
        //                double Size = GetSize(item);
        //                int Seeders = GetSeeders(item).Value;

        //                result.Add(new Item()
        //                {
        //                    Title = Title,
        //                    PubDate = PublishDate,
        //                    Link = DownloadUrl,
        //                    Size = Size,
        //                    Seeders = Seeders
        //                });
        //            }
        //        }

        //        return result.OrderByDescending(x => x.Seeders).ToList();
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show(e.Message);
        //    }

        //    return new List<Item>();
        //}
        public static long GetSize(XElement item)
        {
            if (item.Element("size") != null)
            {
                return ParseSize(item.Element("size").Value, true);
            }

            return 0;
        }
        private static long ConvertToBytes(double value, int power, bool binaryPrefix)
        {
            var prefix = binaryPrefix ? 1024 : 1000;
            var multiplier = Math.Pow(prefix, power);
            var result = value * multiplier;

            return Convert.ToInt64(result);
        }
        private static readonly Regex ParseSizeRegex = new Regex(@"(?<value>(?<!\.\d*)(?:\d+,)*\d+(?:\.\d{1,3})?)\W?(?<unit>[KMG]i?B)(?![\w/])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static long ParseSize(string sizeString, bool defaultToBinaryPrefix)
        {
            if (String.IsNullOrWhiteSpace(sizeString))
            {
                return 0;
            }

            if (sizeString.All(char.IsDigit))
            {
                return long.Parse(sizeString);
            }

            var match = ParseSizeRegex.Matches(sizeString);

            if (match.Count != 0)
            {
                var value = decimal.Parse(Regex.Replace(match[0].Groups["value"].Value, "\\,", ""), CultureInfo.InvariantCulture);

                var unit = match[0].Groups["unit"].Value.ToLower();

                switch (unit)
                {
                    case "kb":
                        return ConvertToBytes(Convert.ToDouble(value), 1, defaultToBinaryPrefix);
                    case "mb":
                        return ConvertToBytes(Convert.ToDouble(value), 2, defaultToBinaryPrefix);
                    case "gb":
                        return ConvertToBytes(Convert.ToDouble(value), 3, defaultToBinaryPrefix);
                    case "kib":
                        return ConvertToBytes(Convert.ToDouble(value), 1, true);
                    case "mib":
                        return ConvertToBytes(Convert.ToDouble(value), 2, true);
                    case "gib":
                        return ConvertToBytes(Convert.ToDouble(value), 3, true);
                    default:
                        return (long)value;
                }
            }

            return 0;
        }
        public static string GetDownloadUrl(XElement item)
        {
            return (string)item.Element("link");
        }

        public static string GetMagnetUrl(XElement item)
        {
            return TryGetTorznabAttribute(item, "magneturl");
        }

        public static readonly Regex RemoveTimeZoneRegex = new Regex(@"\s[A-Z]{2,4}$", RegexOptions.Compiled);
        public static DateTime ParseDate(string dateString)
        {
            try
            {
                if (!DateTime.TryParse(dateString, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal, out var result))
                {
                    dateString = RemoveTimeZoneRegex.Replace(dateString, "");
                    result = DateTime.Parse(dateString, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal);
                }

                return result.ToUniversalTime();
            }
            catch (FormatException e)
            {
               
            }

            return new DateTime();
        }
        public static DateTime GetPublishDate(XElement item)
        {
            var dateString = TryGetValue(item,"pubDate");

            if (String.IsNullOrWhiteSpace(dateString))
            {
                MessageBox.Show("Each item in the RSS feed must have a pubDate element with a valid publish date.");
            }

            return ParseDate(dateString);
        }
        public static string GetTitle(XElement item)
        {
            return TryGetValue(item,"title", "Unknown");
        }
        public static string TryGetValue(XElement item, string elementName, string defaultValue = "")
        {
            var element = item.Element(elementName);

            return element != null ? element.Value : defaultValue;
        }
        public static IEnumerable<XElement> GetItems(XDocument document)
        {
            var root = document.Root;

            if (root == null)
            {
                return Enumerable.Empty<XElement>();
            }

            var channel = root.Element("channel");

            if (channel == null)
            {
                return Enumerable.Empty<XElement>();
            }

            return channel.Elements("item");
        }
        public const string ns = "{http://torznab.com/schemas/2015/feed}";
        public static string TryGetTorznabAttribute(XElement item, string key, string defaultValue = "")
        {
            var attrElement = item.Elements(ns + "attr").FirstOrDefault(e => e.Attribute("name").Value.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (attrElement != null)
            {
                var attrValue = attrElement.Attribute("value");
                if (attrValue != null)
                {
                    return attrValue.Value;
                }
            }

            return defaultValue;
        }

        public static int? GetSeeders(XElement item)
        {
            var seeders = TryGetTorznabAttribute(item, "seeders");

            if (!String.IsNullOrWhiteSpace(seeders))
            {
                return int.Parse(seeders);
            }

            return null;
        }


        public static async Task<List<Item>> GetMovieTorrentsWithImdbId(string imdbid)
        {
            try
            {
                List<Item> result = new List<Item>();
                var restClient = new RestSharp.RestClient(BASE_URL);
                var request = new RestRequest();
                request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
                request.RequestFormat = RestSharp.DataFormat.Xml;
                request.AddParameter("apikey", API_KEY);
                request.AddParameter("t", "movie").AddParameter("imdbid", imdbid).AddParameter("cat", "2000");
                var response = await restClient.ExecuteAsync<List<Item>>(request);

                if (response.IsSuccessful)
                {
                    var document = XDocument.Parse(response.Content);
                    var items = GetItems(document).ToList();

                    foreach (var item in items)
                    {
                        string Title = GetTitle(item);
                        DateTime PublishDate = GetPublishDate(item);
                        string DownloadUrl = GetDownloadUrl(item);
                        double Size = GetSize(item);
                        int Seeders = GetSeeders(item).Value;

                        result.Add(new Item()
                        {
                            Title = Title,
                            PubDate = PublishDate,
                            Link = DownloadUrl,
                            Size = Size,
                            Seeders = Seeders
                        });
                    }
                }


                return result.OrderByDescending(x => x.Seeders).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return new List<Item>();
        }

        public static async Task<List<Item>> GetMovieTorrentsWithName(string name,int year)
        {
            try
            {
                List<Item> result = new List<Item>();
                var restClient = new RestSharp.RestClient(BASE_URL);
                var request = new RestRequest();
                request.XmlSerializer = new DotNetXmlSerializer();
                request.RequestFormat = DataFormat.Xml;
                request.AddParameter("apikey", API_KEY);
                request.AddParameter("t", "search");
                request.AddParameter("q", name).AddParameter("cat", "2000");
                var response = await restClient.ExecuteAsync<List<Item>>(request);

                if (response.IsSuccessful)
                {
                    var document = XDocument.Parse(response.Content);
                    var items = GetItems(document).ToList();

                    foreach (var item in items)
                    {
                        string Title = GetTitle(item);
                        DateTime PublishDate = GetPublishDate(item);
                        string DownloadUrl = GetDownloadUrl(item);
                        double Size = GetSize(item);
                        int Seeders = GetSeeders(item).Value;

                        result.Add(new Item()
                        {
                            Title = Title,
                            PubDate = PublishDate,
                            Link = DownloadUrl,
                            Size = Size,
                            Seeders = Seeders
                        });
                    }
                }
                return result.OrderByDescending(x=> x.Title.Contains(year.ToString()))
                    .ThenByDescending(x=> x.Title.Contains((year-1).ToString())).
                    ThenByDescending(x=> x.Title.Contains((year+1).ToString())).
                    ThenByDescending(x => x.Seeders).ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return new List<Item>();
        }


        public static async Task<List<Item>> GetMovieTorrentsImdb(string imdbid)
        {
            List<Item> result = new List<Item>();
            var categories = new HashSet<int> { 2045, 2040, 2060, 2000 }; // HashSet kullanarak hızlı kontrol
            var indexSearchResults = await GetIndexSearchResults();

            // Filtrelenmiş indexer'lar üzerinden paralel görev oluşturma
            var tasks = indexSearchResults
                .Where(indexerSearch =>
                    indexerSearch.SearchingSupportList.Contains(ShowType.Movie) &&
                    indexerSearch.SupportedMovieParameters.Contains("imdbid"))
                .Select(async indexerSearch =>
                {
                    var validCategories = categories.Where(x => indexerSearch.Categories.Contains(x));
                    var movies = await MovieSearchAsync(imdbId: imdbid, url: indexerSearch.Link, categories: validCategories);

                    if (movies == null) return null;

                    return movies.Channel.Releases.Select(movie =>
                    {
                        // Hata kontrolü ve dönüşüm optimizasyonu
                        int.TryParse(movie.Attributes.FirstOrDefault(x => x.Name == "seeders")?.Value, out var seeders);

                        return new Item
                        {
                            Title = movie.Title,
                            PubDate = ParseDate(movie.PubDateString),
                            Link = movie.Link,
                            Size = ParseSize(movie.Size.ToString(), true),
                            Seeders = seeders,
                            Category = movie.Categories
                        };
                    });
                });

            // Görevleri tamamlayıp sonuçları birleştirme
            var results = await Task.WhenAll(tasks);
            result.AddRange(results.Where(r => r != null).SelectMany(r => r));

            // Kategorilere ve seeders'a göre sıralama
            return result
                .GroupBy(x => x.Category.Any(c => c == 2045) ? 1 :
                              x.Category.Any(c => c == 2040) ? 2 :
                              x.Category.Any(c => c == 2060) ? 3 : 4)
                .OrderBy(g => g.Key)
                .SelectMany(g => g.OrderByDescending(x => x.Seeders))
                .ToList();
        }



        public static async Task<List<Item>> GetMovieTorrentsName(string name, int year)
        {
            List<Item> result = new List<Item>();
            var categories = new HashSet<int> { 2045, 2040, 2060, 2000 }; // HashSet ile hızlı kontrol
            var indexSearchResults = await GetIndexSearchResults();

            // Filtrelenmiş indexer'lar üzerinden paralel görev oluşturma
            var tasks = indexSearchResults
                .Where(indexerSearch =>
                    indexerSearch.SearchingSupportList.Contains(ShowType.Movie) &&
                    indexerSearch.SupportedMovieParameters.Contains("q"))
                .Select(async indexerSearch =>
                {
                    var validCategories = categories.Where(x => indexerSearch.Categories.Contains(x));
                    var movies = await MovieSearchAsync(query: name, url: indexerSearch.Link, categories: validCategories);

                    if (movies == null) return null;

                    return movies.Channel.Releases.Select(movie =>
                    {
                        // Hata kontrolü ve dönüşüm optimizasyonu
                        int.TryParse(movie.Attributes.FirstOrDefault(x => x.Name == "seeders")?.Value, out var seeders);

                        return new Item
                        {
                            Title = movie.Title,
                            PubDate = ParseDate(movie.PubDateString),
                            Link = movie.Link,
                            Size = ParseSize(movie.Size.ToString(), true),
                            Seeders = seeders,
                            Category = movie.Categories
                        };
                    });
                });

            // Görevleri tamamlayıp sonuçları birleştirme
            var results = await Task.WhenAll(tasks);
            result.AddRange(results.Where(r => r != null).SelectMany(r => r));

            // Yıl filtresi ve sıralama
            return result
                .Where(x => x.Title.Contains(year.ToString())) // Yıl kontrolü
                .GroupBy(x => x.Category.Any(c => c == 2045) ? 1 :
                              x.Category.Any(c => c == 2040) ? 2 :
                              x.Category.Any(c => c == 2060) ? 3 : 4)
                .OrderBy(g => g.Key)
                .SelectMany(g => g.OrderByDescending(x => x.Seeders))
                .ToList();
        }


        public static async Task<List<Item>> GetTvEpisodeTorrents(string name, int season, int episode)
        {
            List<Item> result = new List<Item>();
            var categories = new HashSet<int> { 5040, 5000, 5045, 5030, 5010 }; // HashSet ile hızlı kontrol
            var indexSearchResults = await GetIndexSearchResults();

            // Filtrelenmiş indexer'lar üzerinden paralel görev oluşturma
            var tasks = indexSearchResults
                .Where(indexerSearch =>
                    indexerSearch.SearchingSupportList.Contains(ShowType.TvShow) &&
                    indexerSearch.SupportedTvParameters.Contains("q") &&
                    indexerSearch.SupportedTvParameters.Contains("season") &&
                    indexerSearch.SupportedTvParameters.Contains("ep"))
                .Select(async indexerSearch =>
                {
                    var validCategories = categories.Where(x => indexerSearch.Categories.Contains(x));
                    var tvShows = await TvSearchAsync(
                        query: name,
                        season: season.ToString(),
                        episode: episode.ToString(),
                        categories: validCategories,
                        url: indexerSearch.Link);

                    if (tvShows == null) return null;

                    return tvShows.Channel.Releases.Select(tvShow =>
                    {
                        // Hata kontrolü ve dönüşüm optimizasyonu
                        int.TryParse(tvShow.Attributes.FirstOrDefault(x => x.Name == "seeders")?.Value, out var seeders);

                        return new Item
                        {
                            Title = tvShow.Title,
                            PubDate = ParseDate(tvShow.PubDateString),
                            Link = tvShow.Link,
                            Size = ParseSize(tvShow.Size.ToString(), true),
                            Seeders = seeders,
                            Category = tvShow.Categories
                        };
                    });
                });

            // Görevleri tamamlayıp sonuçları birleştirme
            var results = await Task.WhenAll(tasks);
            result.AddRange(results.Where(r => r != null).SelectMany(r => r));

            // Bölüm numarası kontrolü ve isim eşleşmesi
            return result
                .Where(x =>
                    GetEpisodeNumberFromFileName(x.Title) == episode && // Bölüm numarası eşleşmesi
                    x.Title.ToLower().Contains(name.ToLower())) // İsim eşleşmesi
                .GroupBy(x => x.Category.Any(c => c == 5045) ? 1 :
                              x.Category.Any(c => c == 5040) ? 2 :
                              x.Category.Any(c => c == 5030) ? 3 :
                              x.Category.Any(c => c == 5010) ? 4 : 5)
                .OrderBy(g => g.Key)
                .SelectMany(g => g.OrderByDescending(x => x.Seeders))
                .ToList();
        }

        public static async Task<List<Item>> GetTvShowTorrents(string name, int year, int season, int episode)
        {
            List<Item> result = new List<Item>();
            var categories = new HashSet<int> { 5000, 5030, 5040, 5045, 5010, 5020, 5060, 5070, 5080, 5999 }; 

            var indexSearchResults = await GetIndexSearchResults();

            // Filtrelenmiş indexer'lar üzerinden paralel görev oluşturma
            var tasks = indexSearchResults
                .Where(indexerSearch =>
                    indexerSearch.SupportedTvParameters.Contains("q"))
                .Select(async indexerSearch =>
                {
                    var validCategories = categories.Where(x => indexerSearch.Categories.Contains(x));

                    var tvShows = await TvSearchAsync(
                        query: name,
                        categories:validCategories,
                        url: indexerSearch.Link);

                    if (tvShows == null) return null;

                    return tvShows.Channel.Releases.Select(tvShow =>
                    {
                        // Hata kontrolü ve dönüşüm optimizasyonu
                        int.TryParse(tvShow.Attributes.FirstOrDefault(x => x.Name == "seeders")?.Value, out var seeders);

                        return new Item
                        {
                            Title = tvShow.Title,
                            PubDate = ParseDate(tvShow.PubDateString),
                            Link = tvShow.Link,
                            Size = ParseSize(tvShow.Size.ToString(), true),
                            Seeders = seeders,
                            Category = tvShow.Categories
                        };
                    });
                });

            // Görevleri tamamlayıp sonuçları birleştirme
            var results = await Task.WhenAll(tasks);
            result.AddRange(results.Where(r => r != null).SelectMany(r => r));

            // Regex desenleri
            string seasonPattern = $@"([sS]0?{season}\b|[sS]eason\s0?{season}\b)";
            string singleEpisodePattern = $@"([eE]0?{episode}\b|[eE]pisode\s0?{episode}\b)";
            string rangeEpisodePattern = @"([eE]0?\d+\s?-\s?[eE]?0?\d+)";
            string anyEpisodePattern = @"([eE]0?\d+\b|[eE]pisode\s0?\d+\b)";
            string completePattern = "complete";

            return result.Where(x=> episode != -1 || DownloadsFilesPage.GetEpisodeNumberFromFileName(x.Title) == null)
                .OrderByDescending(x => {
                    string title = x.Title.ToLower();
                    string nameInTitle = name.ToLower();
                    string yearStr = year.ToString();

                    // Temel eşleşme kontrolleri
                    bool titleMatches = title.Contains(nameInTitle);
                    bool yearMatches = title.Contains(yearStr);
                    bool hasSeasonPattern = Regex.IsMatch(title, seasonPattern, RegexOptions.IgnoreCase);
                    bool hasSingleEpisodePattern = Regex.IsMatch(title, singleEpisodePattern, RegexOptions.IgnoreCase);
                    bool hasRangeEpisodePattern = Regex.IsMatch(title, rangeEpisodePattern, RegexOptions.IgnoreCase);
                    bool hasAnyEpisodePattern = Regex.IsMatch(title, anyEpisodePattern, RegexOptions.IgnoreCase);
                    bool hasCompletePattern = title.Contains(completePattern);

                    // Tam sezon indirme (episode = -1)
                    if (episode == -1)
                    {
                        // Tek bölüm içeren açıkça reddedilmeli, ancak aralıklı bölümler (E01-E12) kabul edilmeli
                        bool isValidFormat = hasSeasonPattern &&
                            (hasCompletePattern || hasRangeEpisodePattern || !hasAnyEpisodePattern);

                        // Öncelik belirleme
                        if (titleMatches && yearMatches && isValidFormat)
                        {
                            // Bütün sezonu almak için en ideal format
                            if (hasCompletePattern) return 40;
                            // Range episode pattern ama "complete" yok, hala iyi
                            if (hasRangeEpisodePattern) return 35;
                            // Sadece season patternı var, tek episode yok, yine iyi
                            return 30;
                        }
                        else if (titleMatches && isValidFormat)
                        {
                            // Year match yok
                            if (hasCompletePattern) return 25;
                            if (hasRangeEpisodePattern) return 20;
                            return 15;
                        }
                        else if (titleMatches)
                        {
                            // Format uygun değil (muhtemelen tek bölüm içeriyor)
                            return 5;
                        }
                        return 0;
                    }
                    // Tek bölüm indirme (episode != -1)
                    else
                    {
                        // Tam bölüm indirirken formatın uygun olması: Hem season hem de tek bölüm belirtici olmalı, range olmamalı
                        bool isValidFormat = hasSeasonPattern && hasSingleEpisodePattern && !hasRangeEpisodePattern;

                        // Öncelik belirleme
                        if (titleMatches && yearMatches && isValidFormat)
                        {
                            return 40;
                        }
                        else if (titleMatches && isValidFormat)
                        {
                            // Year match yok
                            return 30;
                        }
                        else if (titleMatches && hasSeasonPattern)
                        {
                            // Sadece season var, episode yok veya yanlış
                            return 15;
                        }
                        else if (titleMatches)
                        {
                            // Ne season ne episode uyuşuyor
                            return 5;
                        }
                        return 0;
                    }
                })
                // Aynı önceliğe sahip torrrentleri seeders'a göre sırala
                .ThenByDescending(x => x.Seeders)
                .ToList();
        }




        public static async Task<List<Item>> GetTvSeasonTorrents(string name, int season)
        {
            List<Item> result = new List<Item>();
            var categories = new HashSet<int> { 5040, 5000 }; // HashSet ile hızlı kontrol
            var indexSearchResults = await GetIndexSearchResults();

            // Filtrelenmiş indexer'lar üzerinden paralel görev oluşturma
            var tasks = indexSearchResults
                .Where(indexerSearch =>
                    indexerSearch.SearchingSupportList.Contains(ShowType.TvShow) &&
                    indexerSearch.SupportedTvParameters.Contains("q") &&
                    indexerSearch.SupportedTvParameters.Contains("season"))
                .Select(async indexerSearch =>
                {
                    var validCategories = categories.Where(x => indexerSearch.Categories.Contains(x));
                    var tvShows = await TvSearchAsync(
                        query: name,
                        season: season.ToString(),
                        categories: validCategories,
                        url: indexerSearch.Link);

                    if (tvShows == null) return null;

                    return tvShows.Channel.Releases.Select(tvShow =>
                    {
                        // Hata kontrolü ve dönüşüm optimizasyonu
                        int.TryParse(tvShow.Attributes.FirstOrDefault(x => x.Name == "seeders")?.Value, out var seeders);

                        return new Item
                        {
                            Title = tvShow.Title,
                            PubDate = ParseDate(tvShow.PubDateString),
                            Link = tvShow.Link,
                            Size = ParseSize(tvShow.Size.ToString(), true),
                            Seeders = seeders,
                            Category = tvShow.Categories
                        };
                    });
                });

            // Görevleri tamamlayıp sonuçları birleştirme
            var results = await Task.WhenAll(tasks);
            result.AddRange(results.Where(r => r != null).SelectMany(r => r));

            // Bölüm kontrolü ve isim eşleşmesi
            return result
                .Where(x =>
                    GetEpisodeNumberFromFileName(x.Title) == null && // Bölüm numarası bulunmamalı
                    x.Title.ToLower().Contains(name.ToLower())) // İsim eşleşmesi
                .GroupBy(x => x.Title.ToLower().Contains("complete") ? 1 :
                              x.Category.Any(c => c == 5040) ? 2 :
                              x.Category.Any(c => c == 5000) ? 3 : 4)
                .OrderBy(g => g.Key)
                .SelectMany(g => g.OrderByDescending(x => x.Seeders))
                .ToList();
        }

        public static List<string> Regexes
        {
            get
            {
                List<string> regexes = new List<string>();
                regexes.Add("[sS][0-9]+[eE][0-9]+-*[eE]*[0-9]*");
                regexes.Add("[0-9]+[xX][0-9]+");

                return regexes;
            }
        }
        private static int? GetEpisodeNumberFromFileName(string fileName)
        {
            foreach (string regex in Regexes)
            {
                Match match = Regex.Match(fileName, regex);
                if (match.Success)
                {
                    string matched = match.Value.ToLower();
                    if (regex.Contains("e")) //SDDEDD
                    {
                        matched = matched.Substring(matched.IndexOf("e") + 1);

                        if (matched.Contains("e") || matched.Contains("-"))
                        {
                            matched = matched.Substring(0, matched.IndexOf(matched.Contains("e") ? "e" : "-")).Replace("-", "");
                        }

                        return int.Parse(matched);
                    }
                    else if (regex.Contains("x")) //DDXDD
                    {
                        matched = matched.Substring(matched.IndexOf("x") + 1);
                        return int.Parse(matched);
                    }
                }
            }

            return null;
        }

        public static async Task<List<TorznabIndexer>> GetIndexersAsync()
        {
            if (!Essentials.StartJackettService())
            {
                await Essentials.RunJacketAsync();
            }
            var parameters = new Dictionary<string, object?>
            {
                ["t"] = "indexers",
                ["apikey"] = API_KEY,
                ["configured"] = true
            };
            var indexersResponse = await DoRequestAsync<TorznabIndexers>(parameters, GetUrl("all"));
            return indexersResponse.Indexers;
        }

        private static string GetUrl(string indexer)
        {
            return new Uri(new Uri(BASE_URL), $"/api/v2.0/indexers/{indexer}/results/torznab").AbsoluteUri;
        }

        public static List<Indexer> SelectedIndexers = new List<Indexer>();
        
        private static async Task<List<IndexerSearch>> GetIndexSearchResults()
        {
            var allIndexers = await GetIndexersAsync();
            List<IndexerSearch> result = new List<IndexerSearch>();

            foreach (var torznabIndexer in allIndexers)
            {
                if (SelectedIndexers.Any(x => x.Id == torznabIndexer.Id))
                {
                    var indexerSearch = new IndexerSearch();

                    indexerSearch.Link =
                        new Uri(new Uri(BASE_URL), $"/api/v2.0/indexers/{torznabIndexer.Id}/results/torznab")
                            .AbsoluteUri;
                    indexerSearch.Categories = torznabIndexer.Caps.Categories.Select(x => x.Id).ToList();

                    indexerSearch.SearchingSupportList = new List<ShowType>();
                    if (torznabIndexer.Caps.Searching.MovieSearch.Available)
                    {
                        indexerSearch.SearchingSupportList.Add(ShowType.Movie);
                    }

                    if (torznabIndexer.Caps.Searching.TvSearch.Available)
                    {
                        indexerSearch.SearchingSupportList.Add(ShowType.TvShow);
                    }

                    indexerSearch.SupportedMovieParameters = torznabIndexer.Caps.Searching.MovieSearch.SupportedParams.Split(',').ToList();
                    indexerSearch.SupportedTvParameters = torznabIndexer.Caps.Searching.TvSearch.SupportedParams.Split(',').ToList();

                    result.Add(indexerSearch);
                }
            }

            return result;
        }

        public static Task<TorznabRss> MovieSearchAsync(
        string? query = null,
        string? imdbId = null,
        IEnumerable<int>? categories = null,
        string? genre = null,
        IEnumerable<string>? attributes = null,
        bool? extended = null,
        bool? delete = null,
        int? maxAge = null,
        int? offset = null,
        string? url = null)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["t"] = "movie",
                ["apikey"] = API_KEY,
                ["q"] = query,
                ["imdbid"] = imdbId,
                ["cat"] = categories,
                ["genre"] = genre,
                ["attrs"] = attributes,
                ["extended"] = extended,
                ["del"] = delete,
                ["maxage"] = maxAge,
                ["offset"] = offset
            };
            return DoRequestAsync<TorznabRss>(parameters, url);
        }

        public static Task<TorznabRss> TvSearchAsync(
        string? query = null,
        string? season = null,
        string? episode = null,
        int? limit = null,
        string? tvRageId = null,
        int? tvMazeId = null,
        int? tvDbId = null,
        IEnumerable<int>? categories = null,
        IEnumerable<string>? attributes = null,
        bool? extended = null,
        bool? delete = null,
        int? maxAge = null,
        int? offset = null,
        string? url = null)
        {

            var parameters = new Dictionary<string, object?>
            {
                ["t"] = "tvsearch",
                ["apikey"] = API_KEY,
                ["q"] = query,
                ["season"] = season,
                ["ep"] = episode,
                ["limit"] = limit,
                ["rid"] = tvRageId,
                ["tvmazeid"] = tvMazeId,
                ["tvdbid"] = tvDbId,
                ["cat"] = categories,
                ["attrs"] = attributes,
                ["extended"] = extended,
                ["del"] = delete,
                ["maxage"] = maxAge,
                ["offset"] = offset
            };
            return DoRequestAsync<TorznabRss>(parameters, url);
        }

        public Task<TorznabRss> SearchAsync(
       string? query = null,
       IEnumerable<string>? groups = null,
       int? limit = null,
       IEnumerable<int>? categories = null,
       IEnumerable<string>? attributes = null,
       bool? extended = null,
       bool? delete = null,
       int? maxAge = null,
       long? minSize = null,
       long? maxSize = null,
       int? offset = null,
       string? sort = null,
       string? url = null)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["t"] = "search",
                ["apikey"] = API_KEY,
                ["q"] = query,
                ["group"] = groups,
                ["limit"] = limit,
                ["cat"] = categories,
                ["attrs"] = attributes,
                ["extended"] = extended,
                ["del"] = delete,
                ["maxage"] = maxAge,
                ["minsize"] = minSize,
                ["maxsize"] = maxSize,
                ["offset"] = offset,
                ["sort"] = sort
            };
            return DoRequestAsync<TorznabRss>(parameters, url);
        }


        protected static async Task<TResponse> DoRequestAsync<TResponse>(Dictionary<string, object?> parameters, string? url = null)
        {
            try
            {
                HttpClient client = new HttpClient();
                var queryString = BuildQueryString(parameters);
                HttpResponseMessage response;
                if (!string.IsNullOrEmpty(url))
                    response = await client.GetAsync(new Uri(new Uri(url), queryString));
                else
                    response = await client.GetAsync(queryString);
                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync();

                var serializer = new TorznabSerializer<TResponse>();
                var result = serializer.Deserialize(stream);

                return result ?? throw new TorznabException(-1, "Failed to deserialize Torznab response.");
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return default;
            }
        }

        protected static string BuildQueryString(Dictionary<string, object?> parameters)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            foreach (var parameter in parameters)
            {
                if (parameter.Value is null)
                    continue;

                var valueString = parameter.Value switch
                {
                    IEnumerable<string> enumerable => string.Join(",", enumerable),
                    IEnumerable<int> enumerable => string.Join(",", enumerable),
                    string or int or bool or long => parameter.Value.ToString(),
                    _ => throw new NotSupportedException($"Unknown type {parameter.Value.GetType()}.")
                };

                if (string.IsNullOrEmpty(valueString))
                    continue;

                query[parameter.Key] = valueString;
            }

            return "?" + query;
        }
    }

    public class IndexerSearch
    {
        public string Link { get; set; }
        public List<int> Categories { get; set; }
        public List<string> SupportedMovieParameters { get; set; }
        public List<string> SupportedTvParameters { get; set; }
        public List<ShowType> SearchingSupportList { get; set; }
    }
}
