using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;
using NetStream.Annotations;
using System.Xml.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using RestSharp.Serializers;
using DataFormat = RestSharp.DataFormat;
using TorznabClient.Models;
using System.Security.Policy;
using TorznabClient.Exceptions;
using TorznabClient.Serializer;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using TorznabClient.Jackett;
using TorznabClient.Torznab;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.Movies;

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
                Log.Information("Jackett Initialization failed. Please go to appliation settings page and fill Jacket Api key and Jacket Api Url");
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
                //MessageBox.Show("Each item in the RSS feed must have a pubDate element with a valid publish date.");
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
                //MessageBox.Show(e.Message);
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
                //MessageBox.Show(e.Message);
            }

            return new List<Item>();
        }


        /*public static async Task<List<Item>> GetMovieTorrentsImdb(string imdbid)
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
        }*/


        
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
                    List<Item> resultItems = new List<Item>();
                    var validCategories = categories.Where(x => indexerSearch.Categories.Contains(x));
                    var parameters = new Dictionary<string, object?>
                    {
                        ["t"] = "movie",
                        ["apikey"] = API_KEY,
                        ["imdbid"] = imdbid,
                        ["cat"] = validCategories,
                    };
                    
                    HttpClient client = new HttpClient();
                    var queryString = BuildQueryString(parameters);
                    string url = indexerSearch.Link;
                    string fullUrl = !string.IsNullOrEmpty(url) ? new Uri(new Uri(url), queryString).ToString() : queryString;
                   
                    var response = await client.GetAsync(fullUrl);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Error($"HTTP Hata: {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Log.Error($"Hata içeriği: {errorContent}");
                        return new List<Item>(){};
                    }
                    
                    string xmlContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlContent);

                        string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);

                        JObject rootObj = JObject.Parse(json);

                        if (rootObj.TryGetValue("rss", out JToken rssToken) &&
                            rssToken is JObject rssObj &&
                            rssObj.TryGetValue("channel", out JToken channelToken) &&
                            channelToken is JObject channelObj &&
                            channelObj.TryGetValue("item", out JToken itemToken))
                        {
                            List<JObject> items = new List<JObject>();

                            if (itemToken is JArray itemArray)
                            {
                                foreach (JObject item in itemArray)
                                {
                                    items.Add(item);
                                }
                            }
                            else if (itemToken is JObject singleItem)
                            {
                                items.Add(singleItem);
                            }
                            else
                            {
                                return new List<Item>();
                            }
                            
                            foreach (var item in items)
                            {
                                string title = item["title"]?.ToString() ?? "No title";
                                string size = item["size"]?.ToString() ?? "No size";
                                string link = item["link"]?.ToString() ?? "";
                                string pubDate = item["pubDate"]?.ToString() ?? "";

                                // Torznab özelliklerini bul
                                int seeders = 0;
                                List<string> categories2 = new List<string>();

                                // XML'deki torznab:attr yapısını JSON'da kontrol et
                                if (item["torznab:attr"] != null)
                                {
                                    // Eğer birden fazla torznab:attr varsa dizi olacaktır
                                    if (item["torznab:attr"] is JArray attrArray)
                                    {
                                        foreach (JObject attr in attrArray)
                                        {
                                            string name = attr["@name"]?.ToString();
                                            if (name == "seeders")
                                            {
                                                int.TryParse(attr["@value"]?.ToString(), out seeders);
                                            }
                                            else if (name == "category")
                                            {
                                                string catValue = attr["@value"]?.ToString();
                                                if (!string.IsNullOrEmpty(catValue))
                                                {
                                                    categories2.Add(catValue);
                                                }
                                            }
                                        }
                                    }
                                    else if (item["torznab:attr"] is JObject attrObj)
                                    {
                                        string name = attrObj["@name"]?.ToString();
                                        if (name == "seeders")
                                        {
                                            int.TryParse(attrObj["@value"]?.ToString(), out seeders);
                                        }
                                        else if (name == "category")
                                        {
                                            string catValue = attrObj["@value"]?.ToString();
                                            if (!string.IsNullOrEmpty(catValue))
                                            {
                                                categories2.Add(catValue);
                                            }
                                        }
                                    }
                                }

                                // <category> etiketlerini kontrol et
                                if (item["category"] != null)
                                {
                                    // Eğer birden fazla kategori varsa dizi olacaktır
                                    if (item["category"] is JArray catArray)
                                    {
                                        foreach (var cat in catArray)
                                        {
                                            string catValue = cat.ToString();
                                            if (!string.IsNullOrEmpty(catValue) && !categories2.Contains(catValue))
                                            {
                                                categories2.Add(catValue);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        string catValue = item["category"].ToString();
                                        if (!string.IsNullOrEmpty(catValue) && !categories2.Contains(catValue))
                                        {
                                            categories2.Add(catValue);
                                        }
                                    }
                                }
                                
                               resultItems.Add(new Item()
                                {
                                    Title = title,
                                    PubDate = ParseDate(pubDate),
                                    Link = link,
                                    Size = ParseSize(size.ToString(), true),
                                    Seeders = seeders,
                                    Category = categories2.Select(x => System.Int32.Parse(x)).ToList()
                                });
                               
                            }
                        }
                        else
                        {
                            Console.WriteLine("Yanıt beklenen formatta değil. RSS/channel/item yapısı bulunamadı.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"XML/JSON dönüştürme hatası: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                    }

                    return resultItems;
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
        

        /*public static async Task<List<Item>> GetMovieTorrentsName(string name, int year)
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
        }*/
        
        public static async IAsyncEnumerable<Item> GetMovieTorrentsNameAsync(string name, int year, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var categories = new HashSet<int> { 2045, 2040, 2060, 2000 }; // HashSet ile hızlı kontrol
            var indexSearchResults = await GetIndexSearchResults();

            // Filtrelenmiş indexer'lar üzerinden paralel görev oluşturma
            var tasks = indexSearchResults
                .Where(indexerSearch =>
                    indexerSearch.SearchingSupportList.Contains(ShowType.Movie) &&
                    indexerSearch.SupportedMovieParameters.Contains("q"))
                .Select(async indexerSearch =>
                {
                    List<Item> resultItems = new List<Item>();

                    var validCategories = categories.Where(x => indexerSearch.Categories.Contains(x));
                    
                    var parameters = new Dictionary<string, object?>
                    {
                        ["t"] = "movie",
                        ["apikey"] = API_KEY,
                        ["q"] = name,
                        ["cat"] = validCategories,
                    };

                    HttpClient client = new HttpClient();
                    var queryString = BuildQueryString(parameters);
                    string url = indexerSearch.Link;
                    string fullUrl = !string.IsNullOrEmpty(url) ? new Uri(new Uri(url), queryString).ToString() : queryString;

                    var response = await client.GetAsync(fullUrl, cancellationToken);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Error($"HTTP Hata: {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        Log.Error($"Hata içeriği: {errorContent}");
                        return new List<Item>(){};
                    }

                    string xmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    
                     try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlContent);

                        string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);

                        JObject rootObj = JObject.Parse(json);

                        if (rootObj.TryGetValue("rss", out JToken rssToken) &&
                            rssToken is JObject rssObj &&
                            rssObj.TryGetValue("channel", out JToken channelToken) &&
                            channelToken is JObject channelObj &&
                            channelObj.TryGetValue("item", out JToken itemToken))
                        {
                            List<JObject> items = new List<JObject>();

                            if (itemToken is JArray itemArray)
                            {
                                foreach (JObject item in itemArray)
                                {
                                    items.Add(item);
                                }
                            }
                            else if (itemToken is JObject singleItem)
                            {
                                items.Add(singleItem);
                            }
                            else
                            {
                                return new List<Item>();
                            }
                            
                            foreach (var item in items)
                            {
                                string title = item["title"]?.ToString() ?? "No title";
                                string size = item["size"]?.ToString() ?? "No size";
                                string link = item["link"]?.ToString() ?? "";
                                string pubDate = item["pubDate"]?.ToString() ?? "";

                                // Torznab özelliklerini bul
                                int seeders = 0;
                                List<string> categories2 = new List<string>();

                                // XML'deki torznab:attr yapısını JSON'da kontrol et
                                if (item["torznab:attr"] != null)
                                {
                                    // Eğer birden fazla torznab:attr varsa dizi olacaktır
                                    if (item["torznab:attr"] is JArray attrArray)
                                    {
                                        foreach (JObject attr in attrArray)
                                        {
                                            string nameAttr = attr["@name"]?.ToString();
                                            if (nameAttr == "seeders")
                                            {
                                                int.TryParse(attr["@value"]?.ToString(), out seeders);
                                            }
                                            else if (nameAttr == "category")
                                            {
                                                string catValue = attr["@value"]?.ToString();
                                                if (!string.IsNullOrEmpty(catValue))
                                                {
                                                    categories2.Add(catValue);
                                                }
                                            }
                                        }
                                    }
                                    else if (item["torznab:attr"] is JObject attrObj)
                                    {
                                        string nameAttr = attrObj["@name"]?.ToString();
                                        if (nameAttr == "seeders")
                                        {
                                            int.TryParse(attrObj["@value"]?.ToString(), out seeders);
                                        }
                                        else if (nameAttr == "category")
                                        {
                                            string catValue = attrObj["@value"]?.ToString();
                                            if (!string.IsNullOrEmpty(catValue))
                                            {
                                                categories2.Add(catValue);
                                            }
                                        }
                                    }
                                }

                                // <category> etiketlerini kontrol et
                                if (item["category"] != null)
                                {
                                    // Eğer birden fazla kategori varsa dizi olacaktır
                                    if (item["category"] is JArray catArray)
                                    {
                                        foreach (var cat in catArray)
                                        {
                                            string catValue = cat.ToString();
                                            if (!string.IsNullOrEmpty(catValue) && !categories2.Contains(catValue))
                                            {
                                                categories2.Add(catValue);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        string catValue = item["category"].ToString();
                                        if (!string.IsNullOrEmpty(catValue) && !categories2.Contains(catValue))
                                        {
                                            categories2.Add(catValue);
                                        }
                                    }
                                }
                                
                               resultItems.Add(new Item()
                                {
                                    Title = title,
                                    PubDate = ParseDate(pubDate),
                                    Link = link,
                                    Size = ParseSize(size.ToString(), true),
                                    Seeders = seeders,
                                    Category = categories2.Select(x => System.Int32.Parse(x)).ToList()
                                });
                               
                            }
                        }
                        else
                        {
                            Console.WriteLine("Yanıt beklenen formatta değil. RSS/channel/item yapısı bulunamadı.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"XML/JSON dönüştürme hatası: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                    }

                    return resultItems;
                });

            // Her task tamamlandığında sonuçları hemen yield et
            foreach (var task in tasks)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                var resultItems = await task;
                if (resultItems != null && resultItems.Any())
                {
                    // Yıl filtresi ile filtrele
                    var filteredItems = resultItems
                        .Where(x => x.Title.Contains(year.ToString())) // Yıl kontrolü
                        .OrderByDescending(x => x.Category.Any(c => c == 2045) ? 1 :
                                              x.Category.Any(c => c == 2040) ? 2 :
                                              x.Category.Any(c => c == 2060) ? 3 : 4)
                        .ThenByDescending(x => x.Seeders);

                    foreach (var item in filteredItems)
                    {
                        yield return item;
                    }
                }
            }
        }

        // Geriye uyumluluk için eski metodu koruyoruz ama yeni async versiyonu kullanıyor
        public static async Task<List<Item>> GetMovieTorrentsName(string name, int year)
        {
            List<Item> result = new List<Item>();
            await foreach (var item in GetMovieTorrentsNameAsync(name, year))
            {
                result.Add(item);
            }

            // Son sıralama ve gruplama
            return result
                .GroupBy(x => x.Category.Any(c => c == 2045) ? 1 :
                              x.Category.Any(c => c == 2040) ? 2 :
                              x.Category.Any(c => c == 2060) ? 3 : 4)
                .OrderBy(g => g.Key)
                .SelectMany(g => g.OrderByDescending(x => x.Seeders))
                .ToList();
        }


        /*public static async Task<List<Item>> GetTvEpisodeTorrents(string name, int season, int episode)
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
        }*/
        
        public static async IAsyncEnumerable<Item> GetTvEpisodeTorrentsAsync(string name, int season, int episode, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
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
                    List<Item> resultItems = new List<Item>();

                    var validCategories = categories.Where(x => indexerSearch.Categories.Contains(x));
                    
                    var parameters = new Dictionary<string, object?>
                    {
                        ["t"] = "tvsearch",
                        ["apikey"] = API_KEY,
                        ["q"] = name,
                        ["season"] = season,
                        ["ep"] = episode,
                        ["cat"] = validCategories
                    };
                    
                     HttpClient client = new HttpClient();
                    var queryString = BuildQueryString(parameters);
                    string url = indexerSearch.Link;
                    string fullUrl = !string.IsNullOrEmpty(url) ? new Uri(new Uri(url), queryString).ToString() : queryString;
                   
                    var response = await client.GetAsync(fullUrl, cancellationToken);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Error($"HTTP Hata: {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        Log.Error($"Hata içeriği: {errorContent}");
                        return new List<Item>(){};
                    }
                    
                    string xmlContent = await response.Content.ReadAsStringAsync(cancellationToken);

                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlContent);

                        string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);

                        JObject rootObj = JObject.Parse(json);

                        if (rootObj.TryGetValue("rss", out JToken rssToken) &&
                            rssToken is JObject rssObj &&
                            rssObj.TryGetValue("channel", out JToken channelToken) &&
                            channelToken is JObject channelObj &&
                            channelObj.TryGetValue("item", out JToken itemToken))
                        {
                            List<JObject> items = new List<JObject>();

                            if (itemToken is JArray itemArray)
                            {
                                foreach (JObject item in itemArray)
                                {
                                    items.Add(item);
                                }
                            }
                            else if (itemToken is JObject singleItem)
                            {
                                items.Add(singleItem);
                            }
                            else
                            {
                                return new List<Item>();
                            }
                            
                            foreach (var item in items)
                            {
                                string title = item["title"]?.ToString() ?? "No title";
                                string size = item["size"]?.ToString() ?? "No size";
                                string link = item["link"]?.ToString() ?? "";
                                string pubDate = item["pubDate"]?.ToString() ?? "";

                                // Torznab özelliklerini bul
                                int seeders = 0;
                                List<string> categories2 = new List<string>();

                                // XML'deki torznab:attr yapısını JSON'da kontrol et
                                if (item["torznab:attr"] != null)
                                {
                                    // Eğer birden fazla torznab:attr varsa dizi olacaktır
                                    if (item["torznab:attr"] is JArray attrArray)
                                    {
                                        foreach (JObject attr in attrArray)
                                        {
                                            string nameAttr = attr["@name"]?.ToString();
                                            if (nameAttr == "seeders")
                                            {
                                                int.TryParse(attr["@value"]?.ToString(), out seeders);
                                            }
                                            else if (nameAttr == "category")
                                            {
                                                string catValue = attr["@value"]?.ToString();
                                                if (!string.IsNullOrEmpty(catValue))
                                                {
                                                    categories2.Add(catValue);
                                                }
                                            }
                                        }
                                    }
                                    else if (item["torznab:attr"] is JObject attrObj)
                                    {
                                        string nameAttr = attrObj["@name"]?.ToString();
                                        if (nameAttr == "seeders")
                                        {
                                            int.TryParse(attrObj["@value"]?.ToString(), out seeders);
                                        }
                                        else if (nameAttr == "category")
                                        {
                                            string catValue = attrObj["@value"]?.ToString();
                                            if (!string.IsNullOrEmpty(catValue))
                                            {
                                                categories2.Add(catValue);
                                            }
                                        }
                                    }
                                }

                                // <category> etiketlerini kontrol et
                                if (item["category"] != null)
                                {
                                    // Eğer birden fazla kategori varsa dizi olacaktır
                                    if (item["category"] is JArray catArray)
                                    {
                                        foreach (var cat in catArray)
                                        {
                                            string catValue = cat.ToString();
                                            if (!string.IsNullOrEmpty(catValue) && !categories2.Contains(catValue))
                                            {
                                                categories2.Add(catValue);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        string catValue = item["category"].ToString();
                                        if (!string.IsNullOrEmpty(catValue) && !categories2.Contains(catValue))
                                        {
                                            categories2.Add(catValue);
                                        }
                                    }
                                }
                                
                               resultItems.Add(new Item()
                                {
                                    Title = title,
                                    PubDate = ParseDate(pubDate),
                                    Link = link,
                                    Size = ParseSize(size.ToString(), true),
                                    Seeders = seeders,
                                    Category = categories2.Select(x => System.Int32.Parse(x)).ToList()
                                });
                               
                            }
                        }
                        else
                        {
                            Console.WriteLine("Yanıt beklenen formatta değil. RSS/channel/item yapısı bulunamadı.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"XML/JSON dönüştürme hatası: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                    }

                    return resultItems;
                    
                });

            // Her task tamamlandığında sonuçları hemen yield et
            foreach (var task in tasks)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                var resultItems = await task;
                if (resultItems != null && resultItems.Any())
                {
                    // Bölüm numarası kontrolü - Jackett zaten season ve episode ile arama yaptığı için 
                    // filtrelemeyi gevşetiyoruz. Eğer episode number parse edilebilirse ve eşleşiyorsa öncelik veriyoruz.
                    var filteredItems = resultItems
                        .Where(x =>
                        {
                            var parsedEpisode = GetEpisodeNumberFromFileName(x.Title);
                            // Eğer episode number parse edilebiliyorsa, eşleşmeli
                            // Eğer parse edilemiyorsa (null), yine de gösteriyoruz çünkü Jackett zaten doğru arama yapmış olabilir
                            return parsedEpisode == null || parsedEpisode == episode;
                        })
                        .OrderByDescending(x =>
                        {
                            // Episode number eşleşenleri önceliklendir
                            var parsedEpisode = GetEpisodeNumberFromFileName(x.Title);
                            return parsedEpisode == episode ? 1 : 0;
                        })
                        .ThenByDescending(x => x.Category.Any(c => c == 5045) ? 1 :
                                              x.Category.Any(c => c == 5040) ? 2 :
                                              x.Category.Any(c => c == 5030) ? 3 :
                                              x.Category.Any(c => c == 5010) ? 4 : 5)
                        .ThenByDescending(x => x.Seeders);

                    foreach (var item in filteredItems)
                    {
                        yield return item;
                    }
                }
            }
        }

        // Geriye uyumluluk için eski metodu koruyoruz ama yeni async versiyonu kullanıyor
        public static async Task<List<Item>> GetTvEpisodeTorrents(string name, int season, int episode)
        {
            List<Item> result = new List<Item>();
            await foreach (var item in GetTvEpisodeTorrentsAsync(name, season, episode))
            {
                result.Add(item);
            }

            // Son sıralama ve gruplama
            return result
                .GroupBy(x => x.Category.Any(c => c == 5045) ? 1 :
                              x.Category.Any(c => c == 5040) ? 2 :
                              x.Category.Any(c => c == 5030) ? 3 :
                              x.Category.Any(c => c == 5010) ? 4 : 5)
                .OrderBy(g => g.Key)
                .SelectMany(g => g.OrderByDescending(x => x.Seeders))
                .ToList();
        }

        

        /*public static async Task<List<Item>> GetTvSeasonTorrents(string name, int season)
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
        }*/
        
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
                    List<Item> resultItems = new List<Item>();
                    var validCategories = categories.Where(x => indexerSearch.Categories.Contains(x));
                    var parameters = new Dictionary<string, object?>
                    {
                        ["t"] = "tvsearch",
                        ["apikey"] = API_KEY,
                        ["q"] = name,
                        ["season"] = season,
                        ["cat"] = validCategories
                    };
                    
                    HttpClient client = new HttpClient();
                    var queryString = BuildQueryString(parameters);
                    string url = indexerSearch.Link;
                    string fullUrl = !string.IsNullOrEmpty(url) ? new Uri(new Uri(url), queryString).ToString() : queryString;

                    
                    var response = await client.GetAsync(fullUrl);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Error($"HTTP Hata: {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Log.Error($"Hata içeriği: {errorContent}");
                        return new List<Item>(){};
                    }

                    string xmlContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlContent);

                        string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);

                        JObject rootObj = JObject.Parse(json);

                        if (rootObj.TryGetValue("rss", out JToken rssToken) &&
                            rssToken is JObject rssObj &&
                            rssObj.TryGetValue("channel", out JToken channelToken) &&
                            channelToken is JObject channelObj &&
                            channelObj.TryGetValue("item", out JToken itemToken))
                        {
                            List<JObject> items = new List<JObject>();

                            if (itemToken is JArray itemArray)
                            {
                                foreach (JObject item in itemArray)
                                {
                                    items.Add(item);
                                }
                            }
                            else if (itemToken is JObject singleItem)
                            {
                                items.Add(singleItem);
                            }
                            else
                            {
                                return new List<Item>();
                            }
                            
                            foreach (var item in items)
                            {
                                string title = item["title"]?.ToString() ?? "No title";
                                string size = item["size"]?.ToString() ?? "No size";
                                string link = item["link"]?.ToString() ?? "";
                                string pubDate = item["pubDate"]?.ToString() ?? "";

                                // Torznab özelliklerini bul
                                int seeders = 0;
                                List<string> categories2 = new List<string>();

                                // XML'deki torznab:attr yapısını JSON'da kontrol et
                                if (item["torznab:attr"] != null)
                                {
                                    // Eğer birden fazla torznab:attr varsa dizi olacaktır
                                    if (item["torznab:attr"] is JArray attrArray)
                                    {
                                        foreach (JObject attr in attrArray)
                                        {
                                            string name = attr["@name"]?.ToString();
                                            if (name == "seeders")
                                            {
                                                int.TryParse(attr["@value"]?.ToString(), out seeders);
                                            }
                                            else if (name == "category")
                                            {
                                                string catValue = attr["@value"]?.ToString();
                                                if (!string.IsNullOrEmpty(catValue))
                                                {
                                                    categories2.Add(catValue);
                                                }
                                            }
                                        }
                                    }
                                    else if (item["torznab:attr"] is JObject attrObj)
                                    {
                                        string name = attrObj["@name"]?.ToString();
                                        if (name == "seeders")
                                        {
                                            int.TryParse(attrObj["@value"]?.ToString(), out seeders);
                                        }
                                        else if (name == "category")
                                        {
                                            string catValue = attrObj["@value"]?.ToString();
                                            if (!string.IsNullOrEmpty(catValue))
                                            {
                                                categories2.Add(catValue);
                                            }
                                        }
                                    }
                                }

                                // <category> etiketlerini kontrol et
                                if (item["category"] != null)
                                {
                                    // Eğer birden fazla kategori varsa dizi olacaktır
                                    if (item["category"] is JArray catArray)
                                    {
                                        foreach (var cat in catArray)
                                        {
                                            string catValue = cat.ToString();
                                            if (!string.IsNullOrEmpty(catValue) && !categories2.Contains(catValue))
                                            {
                                                categories2.Add(catValue);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        string catValue = item["category"].ToString();
                                        if (!string.IsNullOrEmpty(catValue) && !categories2.Contains(catValue))
                                        {
                                            categories2.Add(catValue);
                                        }
                                    }
                                }
                                
                               resultItems.Add(new Item()
                                {
                                    Title = title,
                                    PubDate = ParseDate(pubDate),
                                    Link = link,
                                    Size = ParseSize(size.ToString(), true),
                                    Seeders = seeders,
                                    Category = categories2.Select(x => System.Int32.Parse(x)).ToList()
                                });
                               
                            }
                        }
                        else
                        {
                            Console.WriteLine("Yanıt beklenen formatta değil. RSS/channel/item yapısı bulunamadı.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"XML/JSON dönüştürme hatası: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                    }

                    return resultItems;
                    
                });

            // Görevleri tamamlayıp sonuçları birleştirme
            var results = await Task.WhenAll(tasks);
            result.AddRange(results.Where(r => r != null).SelectMany(r => r));

            // Bölüm kontrolü - Sezon torrentlerinde standart episode formatı (S01E01, 1x01) olmamalı
            // "Episode", "Bölüm" gibi kelimeleri göz ardı ediyoruz çünkü sezon paketlerinde "Episode 1-10" gibi ifadeler olabilir
            return result
                .Where(x =>
                {
                    // Sadece standart formatları (S01E01, 1x01) kontrol et
                    string title = x.Title ?? "";
                    foreach (string regex in Regexes)
                    {
                        Match match = Regex.Match(title, regex);
                        if (match.Success)
                        {
                            // Standart episode formatı bulundu, bu bir sezon torrenti değil
                            return false;
                        }
                    }
                    // Standart format yok, sezon torrenti olabilir
                    return true;
                })
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
            if (string.IsNullOrEmpty(fileName))
                return null;

            // Önce standart formatları kontrol et (S01E01, 1x01)
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

                        if (int.TryParse(matched, out int episodeNum))
                        {
                            return episodeNum;
                        }
                    }
                    else if (regex.Contains("x")) //DDXDD
                    {
                        matched = matched.Substring(matched.IndexOf("x") + 1);
                        if (int.TryParse(matched, out int episodeNum))
                        {
                            return episodeNum;
                        }
                    }
                }
            }

            // Standart format bulunamazsa, sadece bölüm numarası içeren formatları kontrol et
            // Örnek: "Show Name 1", "Show Name Episode 1", "Show Name - 1", "1. Bölüm", "Episode 1"
            string lowerFileName = fileName.ToLower();
            
            // "episode 1", "ep 1", "episode.1", "ep.1" formatları
            var episodePatterns = new[]
            {
                @"episode\s*[\.\-\s]*(\d+)",
                @"ep\s*[\.\-\s]*(\d+)",
                @"bölüm\s*[\.\-\s]*(\d+)",
                @"bolum\s*[\.\-\s]*(\d+)"
            };

            foreach (var pattern in episodePatterns)
            {
                Match match = Regex.Match(lowerFileName, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    if (int.TryParse(match.Groups[1].Value, out int episodeNum))
                    {
                        return episodeNum;
                    }
                }
            }

            // Dosya isminde başından veya sonundan sadece bir sayı varsa (1-999 arası), bu bölüm numarası olabilir
            // Ama yalnızca sezon formatı yoksa ve episode/ep/bölüm gibi kelimeler varsa
            // Basit sayı pattern'ini kaldırıyoruz çünkü çok fazla yanlış pozitif veriyor
            // Sadece açıkça episode/ep/bölüm kelimeleri varsa sayıyı episode numarası olarak kabul ediyoruz

            return null;
        }

        /*public static async Task<List<TorznabIndexer>> GetIndexersAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(BASE_URL) || string.IsNullOrEmpty(API_KEY))
                {
                    Log.Information("Jackett service not properly initialized. BASE_URL or API_KEY is empty.");
                    return new List<TorznabIndexer>();
                }

                if (PlatformDetector.GetPlatform() != Platform.Web && PlatformDetector.GetPlatform() != Platform.Android && !Essentials.StartJackettService())
                {
                    await Essentials.RunJacketAsync();
                }

                Log.Information("Trying direct Jackett API call to get all config...");
                
                // Doğrudan HTTP çağrısı yapalım
                using (var httpClient = new HttpClient())
                {
                    var apiUrl = new Uri(new Uri(BASE_URL), $"api/v2.0/indexers/all/results/torznab/api?apikey={API_KEY}&t=indexers&configured=true");
                    Log.Information($"Sending request to: {apiUrl}");
                    
                    try 
                    {
                        var response = await httpClient.GetAsync(apiUrl);
                        response.EnsureSuccessStatusCode();
                        
                        string content = await response.Content.ReadAsStringAsync();
                        Log.Information($"Response: {content}");
                        Log.Information($"API response received ({content.Length} bytes)");
                        
                        // Deserialize without using TorznabSerializer
                        using (var reader = new StringReader(content))
                        {
                            var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(TorznabIndexers));
                            var indexersResponse = (TorznabIndexers)xmlSerializer.Deserialize(reader);
                            
                            if (indexersResponse != null && indexersResponse.Indexers != null)
                            {
                                Log.Information($"Successfully received {indexersResponse.Indexers.Count} indexers");
                                return indexersResponse.Indexers;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"Direct API call failed: {ex.Message}");
                        // Continue to fallback approach
                    }
                }
                
                // Fallback: Use example indexers
                Log.Information("Using fallback example indexers...");
               
                
                return new List<TorznabIndexer>();;
            }
            catch (Exception ex)
            {
                Log.Information($"Error in GetIndexersAsync: {ex.Message}");
                Log.Information($"Stack trace: {ex.StackTrace}");
                return new List<TorznabIndexer>();
            }
        }*/
        
        public static async Task<List<TorznabIndexer>> GetIndexersAsync()
        {
            try
            {

                using (var httpClient = new HttpClient())
                {
                    var apiUrl = new Uri(new Uri(BASE_URL), $"api/v2.0/indexers/all/results/torznab/api?apikey={API_KEY}&t=indexers&configured=true");

                    try
                    {
                        var response = await httpClient.GetAsync(apiUrl);
                        response.EnsureSuccessStatusCode();

                        string content = await response.Content.ReadAsStringAsync();

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(content);

                        string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);

                        List<TorznabIndexer> indexers = ParseWithSystemTextJson(json);
                        return indexers;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Direct API call failed: {ex.Message}");
                        // Continue to fallback approach
                    }
                }



                // Fallback: Use example indexers

                return new List<TorznabIndexer>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetIndexersAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<TorznabIndexer>();
            }
        }

        static List<TorznabIndexer> ParseWithSystemTextJson(string json)
        {
            try
            {
                JObject rootObj = JObject.Parse(json);
                JArray indexerArray = null;

                // indexers.indexer[] yapısını bulalım
                if (rootObj.TryGetValue("indexers", out JToken indexersToken) &&
                    indexersToken is JObject indexersObj &&
                    indexersObj.TryGetValue("indexer", out JToken indexerToken))
                {
                    if (indexerToken is JArray array)
                    {
                        indexerArray = array;
                    }
                    else if (indexerToken is JObject single)
                    {
                        // Tek bir indexer varsa, onu da bir array içine koyalım
                        indexerArray = new JArray();
                        indexerArray.Add(single);
                    }
                }

                if (indexerArray == null || indexerArray.Count == 0)
                {
                    Console.WriteLine("JSON'da indexers.indexer yapısı bulunamadı!");
                    return new List<TorznabIndexer>();
                }

                // Şimdi System.Text.Json ile dönüştürelim
                var indexers = new List<TorznabIndexer>();
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters =
                    {
                        new TorznabIndexerJsonConverter(),
                        new TorznabCapsJsonConverter(),
                        new TorznabCategoryJsonConverter()
                    }
                };

                // Her bir indexer öğesini ayrı ayrı deserialize edelim
                foreach (JObject indexerObj in indexerArray)
                {
                    try
                    {
                        string indexerJson = indexerObj.ToString();
                        var bytesJson = System.Text.Encoding.UTF8.GetBytes(indexerJson);
                        var reader = new Utf8JsonReader(bytesJson);

                        // Converter kullanarak deserialize
                        var indexer = System.Text.Json.JsonSerializer.Deserialize<TorznabIndexer>(ref reader, options);
                        if (indexer != null)
                        {
                            indexers.Add(indexer);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Indexer dönüştürme hatası: {ex.Message}");
                    }
                }

                return indexers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System.Text.Json dönüştürme hatası: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return new List<TorznabIndexer>();
            }
        }

        private static string GetUrl(string indexer)
        {
            try
            {
                if (string.IsNullOrEmpty(BASE_URL))
                {
                    Log.Information("BASE_URL is not initialized");
                    return null;
                }

                return new Uri(new Uri(BASE_URL), $"/api/v2.0/indexers/{indexer}/results/torznab").AbsoluteUri;
            }
            catch (Exception ex)
            {
                Log.Information($"Error in GetUrl: {ex.Message}");
                return null;
            }
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
                        new Uri(new Uri(BASE_URL), $"api/v2.0/indexers/{torznabIndexer.Id}/results/torznab")
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

                    indexerSearch.SupportedMovieParameters = torznabIndexer.Caps.Searching.MovieSearch.SupportedParams.ToList();
                    indexerSearch.SupportedTvParameters = torznabIndexer.Caps.Searching.TvSearch.SupportedParams.ToList();

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


        public static async Task<TResponse> DoRequestAsync<TResponse>(Dictionary<string, object?> parameters, string? url = null)
        {
            try
            {
                HttpClient client = new HttpClient();
                var queryString = BuildQueryString(parameters);
                string fullUrl = !string.IsNullOrEmpty(url) ? new Uri(new Uri(url), queryString).ToString() : queryString;
                
                Log.Information($"API URL: {fullUrl}");
                
                HttpResponseMessage response;
                if (!string.IsNullOrEmpty(url))
                    response = await client.GetAsync(new Uri(new Uri(url), queryString));
                else
                    response = await client.GetAsync(queryString);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Log.Information($"API hata yanıtı: {errorContent}");
                    throw new TorznabException((int)response.StatusCode, $"HTTP hata: {response.StatusCode}. İçerik: {errorContent}");
                }
                
                Log.Information($"API yanıt başarılı: {response.StatusCode}");
                
                // Doğrudan XML içeriğini alalım
                string content = await response.Content.ReadAsStringAsync();
                
                // XmlSerializer örneği oluşturalım
                var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(TResponse));
                
                // String içeriğinden deserialize edelim
                using (var reader = new StringReader(content))
                {
                    try
                    {
                        return (TResponse)xmlSerializer.Deserialize(reader);
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"XML deserialize hatası: {ex.Message}");
                        Log.Information($"XML içeriği: {content.Substring(0, Math.Min(500, content.Length))}...");
                        throw;
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                Log.Information($"HTTP istek hatası: {httpEx.Message}");
                throw new TorznabException(-2, $"HTTP istek başarısız: {httpEx.Message}");
            }
            catch (TorznabException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Information($"Genel DoRequestAsync hatası: {e.Message}");
                Log.Information($"Stack trace: {e.StackTrace}");
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
    
    public class Limits
    {
        public int Default { get; set; } = 100;
        public int Max { get; set; } = 100;
    }
    
    public class SearchCapability
    {
        public bool Available { get; set; }
        public List<string> SupportedParams { get; set; } = new List<string>();
    }
    
    public class SearchingCapabilities
    {
        public SearchCapability Search { get; set; } = new SearchCapability();
        public SearchCapability TvSearch { get; set; } = new SearchCapability();
        public SearchCapability MovieSearch { get; set; } = new SearchCapability();
        public SearchCapability MusicSearch { get; set; } = new SearchCapability();
        public SearchCapability AudioSearch { get; set; } = new SearchCapability();
        public SearchCapability BookSearch { get; set; } = new SearchCapability();
    }
    
    public class TorznabCaps
    {
        public string ServerTitle { get; set; } = string.Empty;
        public Limits Limits { get; set; } = new Limits();
        public SearchingCapabilities Searching { get; set; } = new SearchingCapabilities();
        public List<TorznabCategory> Categories { get; set; } = new List<TorznabCategory>();
    }
    
    public class TorznabCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<TorznabCategory> Subcategories { get; set; } = new List<TorznabCategory>();
    }
    
    public class TorznabIndexer
    {
        public string Id { get; set; } = string.Empty;
        public bool Configured { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public TorznabCaps Caps { get; set; }
        
        // Protected constructor for JsonConverter
        public TorznabIndexer() { }
    }
    
    public class TorznabIndexerJsonConverter : System.Text.Json.Serialization.JsonConverter<TorznabIndexer>
{
    public override TorznabIndexer Read(ref Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new System.Text.Json.JsonException("Expected StartObject token");
        }

        string id = null;
        bool configured = false;
        string title = null;
        string description = null;
        string link = null;
        string language = null;
        string type = null;
        TorznabCaps caps = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString();
                reader.Read(); // İleri git, değere geç

                try
                {
                    switch (propertyName)
                    {
                        case "@id":
                            id = reader.GetString();
                            break;
                        case "@configured":
                            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
                                configured = reader.GetBoolean();
                            else if (reader.TokenType == JsonTokenType.String)
                            {
                                string value = reader.GetString();
                                configured = value == "true" || value == "1";
                            }
                            break;
                        case "title":
                            title = reader.GetString();
                            break;
                        case "description":
                            description = reader.GetString();
                            break;
                        case "link":
                            link = reader.GetString();
                            break;
                        case "language":
                            language = reader.GetString();
                            break;
                        case "type":
                            type = reader.GetString();
                            break;
                        case "caps":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                caps = System.Text.Json.JsonSerializer.Deserialize<TorznabCaps>(ref reader, options);
                            }
                            break;
                        default:
                            // Bilinmeyen özelliği atla
                            reader.Skip();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Özellik okuma hatası ({propertyName}): {ex.Message}");
                    // Hatadan sonra bir sonraki özelliğe devam et
                    reader.Skip();
                }
            }
        }

        // TorznabIndexer nesnesini oluştur
        var indexer = new TorznabIndexer();
        
        // Özellikleri set et
        indexer.Id = id ?? string.Empty;
        indexer.Configured = configured;
        indexer.Title = title ?? string.Empty;
        indexer.Description = description ?? string.Empty;
        indexer.Link = link ?? string.Empty;
        indexer.Language = language ?? string.Empty;
        indexer.Type = type ?? string.Empty;
        indexer.Caps = caps;

        return indexer;
    }

    public override void Write(Utf8JsonWriter writer, TorznabIndexer value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("id", value.Id);
        writer.WriteBoolean("configured", value.Configured);
        writer.WriteString("title", value.Title);
        writer.WriteString("description", value.Description);
        writer.WriteString("link", value.Link);
        writer.WriteString("language", value.Language);
        writer.WriteString("type", value.Type);

        if (value.Caps != null)
        {
            writer.WritePropertyName("caps");
            System.Text.Json.JsonSerializer.Serialize(writer, value.Caps, options);
        }

        writer.WriteEndObject();
    }
}

public class TorznabCapsJsonConverter : System.Text.Json.Serialization.JsonConverter<TorznabCaps>
{
    public override TorznabCaps Read(ref Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new System.Text.Json.JsonException("Expected StartObject token");
        }

        var caps = new TorznabCaps();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString();
                reader.Read(); // İleri git, değere geç

                try
                {
                    switch (propertyName)
                    {
                        case "server":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                while (reader.Read())
                                {
                                    if (reader.TokenType == JsonTokenType.EndObject)
                                        break;
                                        
                                    if (reader.TokenType == JsonTokenType.PropertyName)
                                    {
                                        string attrName = reader.GetString();
                                        reader.Read();
                                        
                                        if (attrName == "@title" && reader.TokenType == JsonTokenType.String)
                                        {
                                            caps.ServerTitle = reader.GetString();
                                        }
                                        else
                                        {
                                            reader.Skip();
                                        }
                                    }
                                }
                            }
                            break;
                        case "limits":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                caps.Limits = ReadLimits(ref reader);
                            }
                            break;
                        case "categories":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                caps.Categories = ReadCategories(ref reader, options);
                            }
                            break;
                        case "searching":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                caps.Searching = ReadSearchingCapabilities(ref reader);
                            }
                            break;
                        default:
                            // Diğer özellikleri atla
                            reader.Skip();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Caps özellik okuma hatası ({propertyName}): {ex.Message}");
                    reader.Skip();
                }
            }
        }

        return caps;
    }
    
    private Limits ReadLimits(ref Utf8JsonReader reader)
    {
        var limits = new Limits();
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;
                
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string attrName = reader.GetString();
                reader.Read();
                
                if (attrName == "@default" && (reader.TokenType == JsonTokenType.Number || reader.TokenType == JsonTokenType.String))
                {
                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        limits.Default = reader.GetInt32();
                    }
                    else
                    {
                        if (int.TryParse(reader.GetString(), out int defaultValue))
                        {
                            limits.Default = defaultValue;
                        }
                    }
                }
                else if (attrName == "@max" && (reader.TokenType == JsonTokenType.Number || reader.TokenType == JsonTokenType.String))
                {
                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        limits.Max = reader.GetInt32();
                    }
                    else
                    {
                        if (int.TryParse(reader.GetString(), out int maxValue))
                        {
                            limits.Max = maxValue;
                        }
                    }
                }
                else
                {
                    reader.Skip();
                }
            }
        }
        
        return limits;
    }

    // Searching özelliklerini okuma
    private SearchingCapabilities ReadSearchingCapabilities(ref Utf8JsonReader reader)
    {
        var searching = new SearchingCapabilities();

        // searching objesi içindeyiz
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString();
                reader.Read(); // İleri git, değere geç

                try
                {
                    switch (propertyName)
                    {
                        case "search":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                searching.Search = ReadSearchCapability(ref reader);
                            }
                            break;
                        case "tv-search":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                searching.TvSearch = ReadSearchCapability(ref reader);
                            }
                            break;
                        case "movie-search":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                searching.MovieSearch = ReadSearchCapability(ref reader);
                            }
                            break;
                        case "music-search":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                searching.MusicSearch = ReadSearchCapability(ref reader);
                            }
                            break;
                        case "audio-search":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                searching.AudioSearch = ReadSearchCapability(ref reader);
                            }
                            break;
                        case "book-search":
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                searching.BookSearch = ReadSearchCapability(ref reader);
                            }
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Searching özellikleri okuma hatası ({propertyName}): {ex.Message}");
                    reader.Skip();
                }
            }
        }

        return searching;
    }
    
    private SearchCapability ReadSearchCapability(ref Utf8JsonReader reader)
    {
        var capability = new SearchCapability();
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;
                
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string attrName = reader.GetString();
                reader.Read();
                
                if (attrName == "@available" && reader.TokenType == JsonTokenType.String)
                {
                    string value = reader.GetString();
                    capability.Available = value.ToLower() == "yes";
                }
                else if (attrName == "@supportedParams" && reader.TokenType == JsonTokenType.String)
                {
                    string supportedParams = reader.GetString();
                    if (!string.IsNullOrEmpty(supportedParams))
                    {
                        capability.SupportedParams = supportedParams.Split(',').ToList();
                    }
                }
                else
                {
                    reader.Skip();
                }
            }
        }
        
        return capability;
    }

    // category dizisi veya tek bir kategori okuma
    private List<TorznabCategory> ReadCategories(ref Utf8JsonReader reader, System.Text.Json.JsonSerializerOptions options)
    {
        var categories = new List<TorznabCategory>();

        // categories objesi içindeyiz
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString();
                reader.Read(); // İleri git, değere geç

                if (propertyName == "category")
                {
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        // Dizi başladı, birden çok kategori okuyacağız
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray)
                                break;

                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                try
                                {
                                    var category = ReadSingleCategory(ref reader);
                                    if (category != null)
                                    {
                                        categories.Add(category);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Kategori okuma hatası: {ex.Message}");
                                    reader.Skip();
                                }
                            }
                        }
                    }
                    else if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        // Tek bir kategori
                        try
                        {
                            var category = ReadSingleCategory(ref reader);
                            if (category != null)
                            {
                                categories.Add(category);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Tek kategori okuma hatası: {ex.Message}");
                            reader.Skip();
                        }
                    }
                }
                else
                {
                    // Diğer özellikleri atla
                    reader.Skip();
                }
            }
        }

        return categories;
    }
    
    private TorznabCategory ReadSingleCategory(ref Utf8JsonReader reader)
    {
        var category = new TorznabCategory();
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;
                
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString();
                reader.Read();
                
                switch (propertyName)
                {
                    case "@id":
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            category.Id = reader.GetInt32();
                        }
                        else if (reader.TokenType == JsonTokenType.String)
                        {
                            if (int.TryParse(reader.GetString(), out int id))
                            {
                                category.Id = id;
                            }
                        }
                        break;
                    case "@name":
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            category.Name = reader.GetString();
                        }
                        break;
                    case "subcat":
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            // Birden fazla alt kategori var
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.EndArray)
                                    break;
                                    
                                if (reader.TokenType == JsonTokenType.StartObject)
                                {
                                    var subcat = ReadSingleCategory(ref reader);
                                    if (subcat != null)
                                    {
                                        category.Subcategories.Add(subcat);
                                    }
                                }
                            }
                        }
                        else if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            // Tek bir alt kategori
                            var subcat = ReadSingleCategory(ref reader);
                            if (subcat != null)
                            {
                                category.Subcategories.Add(subcat);
                            }
                        }
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }
        
        return category;
    }

    public override void Write(Utf8JsonWriter writer, TorznabCaps value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Server
        if (!string.IsNullOrEmpty(value.ServerTitle))
        {
            writer.WritePropertyName("server");
            writer.WriteStartObject();
            writer.WriteString("@title", value.ServerTitle);
            writer.WriteEndObject();
        }
        
        // Limits
        writer.WritePropertyName("limits");
        writer.WriteStartObject();
        writer.WriteNumber("@default", value.Limits.Default);
        writer.WriteNumber("@max", value.Limits.Max);
        writer.WriteEndObject();
        
        // Searching capabilities
        writer.WritePropertyName("searching");
        writer.WriteStartObject();
        
        // Search
        WriteSearchCapability(writer, "search", value.Searching.Search);
        
        // TV Search
        WriteSearchCapability(writer, "tv-search", value.Searching.TvSearch);
        
        // Movie Search
        WriteSearchCapability(writer, "movie-search", value.Searching.MovieSearch);
        
        // Music Search
        WriteSearchCapability(writer, "music-search", value.Searching.MusicSearch);
        
        // Audio Search
        WriteSearchCapability(writer, "audio-search", value.Searching.AudioSearch);
        
        // Book Search
        WriteSearchCapability(writer, "book-search", value.Searching.BookSearch);
        
        writer.WriteEndObject();

        // Categories
        if (value.Categories != null && value.Categories.Count > 0)
        {
            writer.WritePropertyName("categories");
            writer.WriteStartObject();

            writer.WritePropertyName("category");
            if (value.Categories.Count > 1)
            {
                writer.WriteStartArray();
                foreach (var category in value.Categories)
                {
                    WriteCategory(writer, category);
                }
                writer.WriteEndArray();
            }
            else if (value.Categories.Count == 1)
            {
                // Tek bir kategori
                WriteCategory(writer, value.Categories[0]);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
    
    private void WriteSearchCapability(Utf8JsonWriter writer, string name, SearchCapability capability)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        writer.WriteString("@available", capability.Available ? "yes" : "no");
        if (capability.SupportedParams.Count > 0)
        {
            writer.WriteString("@supportedParams", string.Join(",", capability.SupportedParams));
        }
        writer.WriteEndObject();
    }
    
    private void WriteCategory(Utf8JsonWriter writer, TorznabCategory category)
    {
        writer.WriteStartObject();
        writer.WriteNumber("@id", category.Id);
        writer.WriteString("@name", category.Name);
        
        if (category.Subcategories.Count > 0)
        {
            writer.WritePropertyName("subcat");
            if (category.Subcategories.Count > 1)
            {
                writer.WriteStartArray();
                foreach (var subcat in category.Subcategories)
                {
                    WriteCategory(writer, subcat);
                }
                writer.WriteEndArray();
            }
            else if (category.Subcategories.Count == 1)
            {
                // Tek bir alt kategori
                WriteCategory(writer, category.Subcategories[0]);
            }
        }
        
        writer.WriteEndObject();
    }
}

public class TorznabCategoryJsonConverter : System.Text.Json.Serialization.JsonConverter<TorznabCategory>
{
    public override TorznabCategory Read(ref Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        // Bu metot artık kullanılmıyor, TorznabCapsJsonConverter içindeki ReadSingleCategory metodu kullanılıyor
        throw new NotImplementedException("TorznabCategoryJsonConverter.Read artık kullanılmıyor.");
    }

    public override void Write(Utf8JsonWriter writer, TorznabCategory value, System.Text.Json.JsonSerializerOptions options)
    {
        // Bu metot artık kullanılmıyor, TorznabCapsJsonConverter içindeki WriteCategory metodu kullanılıyor
        throw new NotImplementedException("TorznabCategoryJsonConverter.Write artık kullanılmıyor.");
    }
}

// AOT uyumlu JSON serileştirme için JsonSerializerContext
[JsonSerializable(typeof(List<TorznabIndexer>))]
[JsonSerializable(typeof(TorznabIndexer))]
[JsonSerializable(typeof(TorznabCaps))]
[JsonSerializable(typeof(TorznabCategory))]
public partial class TorznabJsonContext : JsonSerializerContext
{
}
}
