using MovieCollection.OpenSubtitles.Models;
using MovieCollection.OpenSubtitles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;
using CountryFlags;
using System.Windows.Media;
using System.DirectoryServices;
using Serilog;
using subtitle_downloader.downloader;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Countries;
using System.Text.RegularExpressions;
using NetStream.Properties;
using ABI.System;
using HandyControl.Controls;
using HandyControl.Expression.Shapes;
using MaterialDesignColors.ColorManipulation;
using Moq;
using Polly.Caching;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;
using System.Drawing;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using System.Windows.Documents;
using System.Windows.Forms;
using TMDbLib.Objects.General;
using TMDbLib.Objects.TvShows;
using Typography.OpenFont.Tables;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using MaterialDesignColors;
using System.Runtime.Intrinsics.Arm;
using MessageBox = System.Windows.MessageBox;
using Microsoft.Win32;
using Typography.OpenFont;

namespace NetStream
{
    class SubtitleHandler
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static OpenSubtitlesOptions _options;
        private static OpenSubtitlesService _service;

        private static string _token;

        public static List<string> keys = new List<string>()
        {
            AppSettingsManager.appSettings.OpenSubtitlesApiKey,
            "",
            ""
        };

        public static int currentApiKey = 0;
        public static int? remaining = null;
        public static async System.Threading.Tasks.Task Init()
        {
            try
            {
                _options = new OpenSubtitlesOptions
                {
                    ApiKey = String.IsNullOrWhiteSpace(keys[0]) ? keys[1] : keys[0],
                    ProductInformation = new ProductHeaderValue("your-app-name", "your-app-version"),
                };

                _service = new OpenSubtitlesService(_httpClient, _options);
                Log.Information("İnitialized Open Subtitle");

                currentApiKey = String.IsNullOrWhiteSpace(keys[0]) ? 1 : 0;
            }
            catch (System.Exception e)
            {
                Log.Error("Subtitle Initialization failed: " + e.Message);
            }
        }

        public static async System.Threading.Tasks.Task ChangeKey()
        {
            try
            {
                int maxIndexKeys = keys.Count - 1;
                if (currentApiKey == maxIndexKeys)
                {
                    currentApiKey = 0;
                }
                else
                {
                    currentApiKey++;
                }
                _options = new OpenSubtitlesOptions
                {
                    ApiKey = keys[currentApiKey],
                    ProductInformation = new ProductHeaderValue("your-app-name", "your-app-version"),
                };

                _service = new OpenSubtitlesService(_httpClient, _options);
                Log.Information("İnitialized Open Subtitle");
            }
            catch (System.Exception e)
            {
                Log.Error("Subtitle Initialization failed: " + e.Message);
            }
        }

        public static async Task<Subtitle> GetSubtitlesByTMDbId(string name,int tmdbId,List<string> languages)
        {
            try
            {
                Subtitle subtitleResult = new Subtitle();

                var search = new NewSubtitleSearch
                {
                    Query = name,
                    TmdbId = tmdbId,
                    Languages = languages
                };

                var result = await _service.SearchSubtitlesAsync(search);

                if (result != null && result.Data.Count > 0)
                {
                    var subtitle = result.Data.OrderByDescending(x => JaroWinklerSimilarity(name, x.Attributes.Files.FirstOrDefault().FileName))
                        .ThenByDescending(x => Double.Parse(x.Attributes.Ratings) > 7)
                        .ThenByDescending(x=> x.Attributes.DownloadCount).FirstOrDefault();
                    if (subtitle != null)
                    {
                        subtitleResult.Fullpath = await DownloadSubtitle(subtitle.Attributes.Files.FirstOrDefault().FileId, subtitle.Attributes.Files.FirstOrDefault().FileName);
                        subtitleResult.HashDownload = false;
                        subtitleResult.Name = System.IO.Path.GetFileName(subtitleResult.Fullpath);
                        subtitleResult.Language = languages.FirstOrDefault();
                        subtitleResult.Synchronized = false;
                        subtitleResult.SubtitleId = subtitle.Attributes.SubtitleId;
                    }
                }

                return subtitleResult;
            }
            catch (System.Exception e)
            {
                Log.Error("Error on subtitle tmdb: "+ e.Message);
                if (currentApiKey != keys.Count - 1)
                {
                    await ChangeKey();
                    await GetSubtitlesByTMDbId(name, tmdbId, languages);
                }
                return null;
            }
        }

        public static async Task<Subtitle> GetSubtitlesByName(string path, List<string> languages)
        {
            Subtitle subtitleResult = new Subtitle();

            var search = new NewSubtitleSearch
            {
                Query = System.IO.Path.GetFileNameWithoutExtension(path),
                Languages = languages
            };

            var result = await _service.SearchSubtitlesAsync(search);

            if (result != null && result.Data.Count > 0)
            {
                var subtitle = result.Data.OrderByDescending(x => JaroWinklerSimilarity(System.IO.Path.GetFileNameWithoutExtension(path), x.Attributes.Files.FirstOrDefault().FileName))
                    .ThenByDescending(x => Double.Parse(x.Attributes.Ratings) > 7)
                    .ThenByDescending(x => x.Attributes.DownloadCount).FirstOrDefault();
                if (subtitle != null)
                {
                    subtitleResult.Fullpath = await DownloadSubtitle(subtitle.Attributes.Files.FirstOrDefault().FileId, subtitle.Attributes.Files.FirstOrDefault().FileName);
                    subtitleResult.HashDownload = false;
                    subtitleResult.Name = System.IO.Path.GetFileName(subtitleResult.Fullpath);
                    subtitleResult.Language = languages.FirstOrDefault();
                    subtitleResult.Synchronized = false;
                    subtitleResult.SubtitleId = subtitle.Attributes.SubtitleId;
                }
            }

            return subtitleResult;
        }

        public static async Task<Subtitle> GetSubtitlesByHash(string filePath,List<string> languages,string movieName)
        {
            try
            {
                Subtitle subtitleResult = new Subtitle();
                var search = new NewSubtitleSearch
                {
                    Query = System.IO.Path.GetFileNameWithoutExtension(filePath),
                    MovieHash = OpenSubtitlesHasher.GetFileHash(filePath),
                    Languages = languages,
                };
                var result = await _service.SearchSubtitlesAsync(search);

                if (result != null && result.Data.Count > 0)
                {
                    var subtitle = result.Data.OrderByDescending(x=> JaroWinklerSimilarity(System.IO.Path.GetFileNameWithoutExtension(filePath),x.Attributes.Files.FirstOrDefault().FileName))
                        .ThenByDescending(x=> Double.Parse(x.Attributes.Ratings) > 7)
                        .ThenByDescending(x => x.Attributes.DownloadCount).FirstOrDefault();

                    if (subtitle != null)
                    {
                        subtitleResult.Fullpath = await DownloadSubtitle(subtitle.Attributes.Files.FirstOrDefault().FileId, subtitle.Attributes.Files.FirstOrDefault().FileName);
                        subtitleResult.HashDownload = true;
                        subtitleResult.Name = System.IO.Path.GetFileName(subtitleResult.Fullpath);
                        subtitleResult.Language = AppSettingsManager.appSettings.IsoSubtitleLanguage;
                        // subtitleResult.Synchronized = true;
                        subtitleResult.SubtitleId = subtitle.Attributes.SubtitleId;
                    }
                }

                return subtitleResult;
            }
            catch (System.Exception e)
            {
                Log.Error("Error on subtitle hash: " +e.Message);
                if (currentApiKey != keys.Count - 1)
                {
                    await ChangeKey();
                    await GetSubtitlesByHash(filePath, languages, movieName);
                }

                return null;
            }
        }
        private static Dictionary<string, CountryEnum> languageCountries = new Dictionary<string, CountryEnum>()
{
    // Languages from LanguagesToOpenSubtitles
    { "ab", CountryEnum.Azerbaijan },    // Azerbaycan dili
    { "af", CountryEnum.South_Africa },  // Afrikaans dili, Güney Afrika
    { "sq", CountryEnum.Albania },       // Arnavutluk
    { "am", CountryEnum.Ethiopia },      // Amharca, Etiyopya
    { "ar", CountryEnum.Saudi_Arabia },  // Arapça, Suudi Arabistan
    { "an", CountryEnum.Spain },         // Aragonca, İspanya
    { "hy", CountryEnum.Armenia },      // Ermenistan
    { "as", CountryEnum.India },         // Assamca, Hindistan
    { "az", CountryEnum.Azerbaijan },   // Azerbaycan
    { "eu", CountryEnum.Spain },         // Baskça, İspanya
    { "be", CountryEnum.Belarus },      // Beyaz Rusya
    { "bn", CountryEnum.Bangladesh },   // Bengalce, Bangladeş
    { "bs", CountryEnum.Bosnia_Herzegovina }, // Boşnakça, Bosna-Hersek
    { "br", CountryEnum.Brazil },       // Brezilya
    { "bg", CountryEnum.Bulgaria },     // Bulgaristan
    { "my", CountryEnum.Myanmar },      // Burmaca, Myanmar
    { "ca", CountryEnum.Spain },    // Katalanca, İspanya
    { "cn", CountryEnum.China },        // Çince, Çin
    { "sh", CountryEnum.Croatia },      // Hırvatça, Hırvatistan
    { "cs", CountryEnum.Czech_Republic }, // Çekçe, Çek Cumhuriyeti
    { "da", CountryEnum.Denmark },      // Danca, Danimarka
    { "nl", CountryEnum.Netherlands },  // Felemenkçe, Hollanda
    { "en", CountryEnum.United_Kingdom }, // İngilizce, Birleşik Krallık
    { "eo", CountryEnum.Poland },         // Esperanto, genel olarak
    { "et", CountryEnum.Estonia },      // Estonya
    { "fi", CountryEnum.Finland },      // Fince, Finlandiya
    { "fr", CountryEnum.France },       // Fransızca, Fransa
    { "gd", CountryEnum.United_Kingdom },     // İskoçça, İskoçya
    { "gl", CountryEnum.Spain },      // Galce, İspanya
    { "ka", CountryEnum.Georgia },      // Gürcüce, Gürcistan
    { "de", CountryEnum.Germany },      // Almanca, Almanya
    { "el", CountryEnum.Greece },       // Yunanca, Yunanistan
    { "he", CountryEnum.Israel },       // İbranice, İsrail
    { "hi", CountryEnum.India },        // Hintçe, Hindistan
    { "hu", CountryEnum.Hungary },      // Macarca, Macaristan
    { "is", CountryEnum.Iceland },      // İzlandaca, İzlanda
    { "ig", CountryEnum.Nigeria },      // Igbo, Nijerya
    { "id", CountryEnum.Indonesia },    // Endonezce, Endonezya
    { "ia", CountryEnum.None },         // Interlingua, genel olarak
    { "ga", CountryEnum.Ireland },      // İrlandaca, İrlanda
    { "it", CountryEnum.Italy },        // İtalyanca, İtalya
    { "ja", CountryEnum.Japan },        // Japonca, Japonya
    { "kn", CountryEnum.India },        // Kannada, Hindistan
    { "kk", CountryEnum.Kazakhstan },   // Kazakça, Kazakistan
    { "km", CountryEnum.Cambodia },     // Kmerce, Kamboçya
    { "ko", CountryEnum.Korea_South },  // Korece, Güney Kore
    { "ku", CountryEnum.Turkey },       // Kürtçe, Türkiye
    { "lv", CountryEnum.Latvia },       // Letonca, Letonya
    { "lt", CountryEnum.Lithuania },    // Litvanca, Litvanya
    { "mk", CountryEnum.North_Macedonia }, // Makedonca, Kuzey Makedonya
    { "ms", CountryEnum.Malaysia },     // Malayca, Malezya
    { "ml", CountryEnum.India },        // Malayalam, Hindistan
    { "mr", CountryEnum.India },        // Marathi, Hindistan
    { "mn", CountryEnum.Mongolia },     // Moğolca, Moğolistan
    { "nv", CountryEnum.United_States_of_America },         // Navajo, genel olarak
    { "ne", CountryEnum.Nepal },        // Nepalce, Nepal
    { "se", CountryEnum.Sweden },      // Sami dili, İsveç
    { "no", CountryEnum.Norway },       // Norveççe, Norveç
    { "oc", CountryEnum.France },       // Okitan, Fransa
    { "fa", CountryEnum.Iran },         // Farsça, İran
    { "pl", CountryEnum.Poland },       // Lehçe, Polonya
    { "pt", CountryEnum.Portugal },     // Portekizce, Portekiz
    { "ps", CountryEnum.Afghanistan }, // Peştu, Afganistan
    { "ro", CountryEnum.Romania },      // Rumence, Romanya
    { "ru", CountryEnum.Russia },       // Rusça, Rusya
    { "sr", CountryEnum.Serbia },       // Sırpça, Sırbistan
    { "sd", CountryEnum.Pakistan },     // Sindhi, Pakistan
    { "si", CountryEnum.Sri_Lanka },    // Sinhala, Sri Lanka
    { "sk", CountryEnum.Slovakia },    // Slovakça, Slovakya
    { "sl", CountryEnum.Slovenia },    // Slovence, Slovenya
    { "so", CountryEnum.Somalia },     // Somali, Somali
    { "es", CountryEnum.Spain },        // İspanyolca, İspanya
    { "sw", CountryEnum.Kenya },        // Swahili, Kenya
    { "sv", CountryEnum.Sweden },      // İsveççe, İsveç
    { "tl", CountryEnum.Philippines }, // Tagalog, Filipinler
    { "ta", CountryEnum.Sri_Lanka },   // Tamilce, Sri Lanka
    { "tt", CountryEnum.Russia },   // Tatarca, Tataristan
    { "te", CountryEnum.India },        // Telugu, Hindistan
    { "th", CountryEnum.Thailand },     // Tayca, Tayland
    { "tr", CountryEnum.Turkey },       // Türkçe, Türkiye
    { "tk", CountryEnum.Turkmenistan }, // Türkmence, Türkmenistan
    { "uk", CountryEnum.Ukraine },      // Ukraynaca, Ukrayna
    { "ur", CountryEnum.Pakistan },     // Urduca, Pakistan
    { "uz", CountryEnum.Uzbekistan },   // Özbekçe, Özbekistan
    { "vi", CountryEnum.Vietnam },      // Vietnamca, Vietnam
    { "cy", CountryEnum.United_Kingdom },        // Galce, Galler
};


        public static async Task<FastObservableCollection<Subtitle>> SearchSubtitle(Subtitle currentSubtitle,string path = "",string movieName = "",int imdbId = -1)
        {
            Log.Information($"Started Subtitle Search from Manager. Subtitle name: {currentSubtitle.Name}\n Id:{currentSubtitle.SubtitleId} \n HashDownload:{currentSubtitle.HashDownload}");
            FastObservableCollection<Subtitle> result = new FastObservableCollection<Subtitle>();
            try
            {
                NewSubtitleSearch search = null;
                if (currentSubtitle.EpisodeNumber == 0 && currentSubtitle.SeasonNumber == 0)
                {
                    Log.Information("Searching for movie subtitles");
                    //Movie
                    if (currentSubtitle.HashDownload && !String.IsNullOrWhiteSpace(path))
                    {
                        search = new NewSubtitleSearch
                        {
                            Query = System.IO.Path.GetFileNameWithoutExtension(path),
                            MovieHash = OpenSubtitlesHasher.GetFileHash(path),
                            Languages = new List<string>() { currentSubtitle.Language },
                        };
                        Log.Information($"Search Query: {search.Query} \n MovieHash: {search.MovieHash}");
                    }
                    else
                    {
                        search = new NewSubtitleSearch
                        {
                            Query = movieName,
                            TmdbId = currentSubtitle.MovieId,
                            Languages = new List<string>(){currentSubtitle.Language}
                        };
                        Log.Information($"Search Query: {search.Query} \n TMdbId: {search.TmdbId}");
                    }
                }
                else
                {
                    //TvShow
                    Log.Information("Searching for tvshow subtitles");
                    if (currentSubtitle.HashDownload && !String.IsNullOrWhiteSpace(path))
                    {
                        search = new NewSubtitleSearch
                        {
                            Query = System.IO.Path.GetFileNameWithoutExtension(path),
                            MovieHash = OpenSubtitlesHasher.GetFileHash(path),
                            Languages = new List<string>() { currentSubtitle.Language }
                        };
                        Log.Information($"Search Query: {search.Query} \n Hash: {search.MovieHash}");
                    }
                    else
                    {
                        search = new NewSubtitleSearch
                        {
                            Query = movieName,
                            SeasonNumber = currentSubtitle.SeasonNumber,
                            EpisodeNumber = currentSubtitle.EpisodeNumber,
                            Languages = new List<string>(){currentSubtitle.Language},
                            ImdbId = imdbId
                        };
                        Log.Information($"Search Query: {search.Query} \n S: {search.SeasonNumber} E: {search.EpisodeNumber} ImdbId: {imdbId}");
                    }
                }

                if (search != null)
                {
                    var Searchresult = await _service.SearchSubtitlesAsync(search);
                    if (Searchresult != null && Searchresult.Data.Count > 0)
                    {
                        Log.Information("Searched completed found " + Searchresult.Data.Count + " subtitles");
                        var country = SetFlag(languageCountries
                            .FirstOrDefault(x => x.Key == currentSubtitle.Language).Value);
                        foreach (var s in Searchresult.Data)
                        {
                            Subtitle sub = new Subtitle()
                            {
                                Country = country,
                                EpisodeNumber = currentSubtitle.EpisodeNumber,
                                FileId = s.Attributes.Files.FirstOrDefault().FileId,
                                FileName = s.Attributes.Files.FirstOrDefault().FileName,
                                HashDownload = currentSubtitle.HashDownload,
                                Language = currentSubtitle.Language,
                                MovieId = currentSubtitle.MovieId,
                                SeasonNumber = currentSubtitle.SeasonNumber,
                                Name = s.Attributes.Files.FirstOrDefault().FileName,
                                DownloadCount = s.Attributes.DownloadCount.ToString(),
                                Votes = s.Attributes.Votes,
                                Ratings = s.Attributes.Ratings,
                                SubtitleId = s.Attributes.SubtitleId
                            };
                            result.Add(sub);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.Error("Search subtitles error: " + e.Message);
                if (currentApiKey != keys.Count - 1)
                {
                    await ChangeKey();
                    await SearchSubtitle(currentSubtitle,  path ,  movieName ,  imdbId);
                }
            }
            return result;
        }

        public static async Task<FastObservableCollection<Subtitle>> SearchSubtitle(int movieId,int seasonNumber,int episodeNumber,string language,string path = "", string movieName = "", int imdbId = -1)
        {
            FastObservableCollection<Subtitle> result = new FastObservableCollection<Subtitle>();
            try
            {
                NewSubtitleSearch search = null;
                if (episodeNumber == 0 && seasonNumber == 0)
                {
                    Log.Information("Searching for movie subtitles");
                    //Movie
                    if (!String.IsNullOrWhiteSpace(path))
                    {
                        search = new NewSubtitleSearch
                        {
                            Query = System.IO.Path.GetFileNameWithoutExtension(path),
                            MovieHash = OpenSubtitlesHasher.GetFileHash(path),
                            Languages = new List<string>() { language },
                        };
                        Log.Information($"Search Query: {search.Query} \n MovieHash: {search.MovieHash}");
                    }
                    else
                    {
                        search = new NewSubtitleSearch
                        {
                            Query = movieName,
                            TmdbId = movieId,
                            Languages = new List<string>() { language }
                        };
                        Log.Information($"Search Query: {search.Query} \n TMdbId: {search.TmdbId}");
                    }
                }
                else
                {
                    //TvShow
                    Log.Information("Searching for tvshow subtitles");
                    if (!String.IsNullOrWhiteSpace(path))
                    {
                        search = new NewSubtitleSearch
                        {
                            Query = Path.GetFileNameWithoutExtension(path),
                            MovieHash = OpenSubtitlesHasher.GetFileHash(path),
                            Languages = new List<string>() { language }
                        };
                        Log.Information($"Search Query: {search.Query} \n Hash: {search.MovieHash}");
                    }
                    else
                    {
                        search = new NewSubtitleSearch
                        {
                            Query = movieName,
                            SeasonNumber = seasonNumber,
                            EpisodeNumber = episodeNumber,
                            Languages = new List<string>() { language },
                            ImdbId = imdbId
                        };
                        Log.Information($"Search Query: {search.Query} \n S: {search.SeasonNumber} E: {search.EpisodeNumber} ImdbId: {imdbId}");
                    }
                }

                if (search != null)
                {
                    var Searchresult = await _service.SearchSubtitlesAsync(search);
                    if (Searchresult != null && Searchresult.Data.Count > 0)
                    {
                        Log.Information("Searched completed found " + Searchresult.Data.Count + " subtitles");
                        var country = SetFlag(languageCountries
                            .FirstOrDefault(x => x.Key == language).Value);
                        foreach (var s in Searchresult.Data)
                        {
                            Subtitle sub = new Subtitle()
                            {
                                Country = country,
                                EpisodeNumber = episodeNumber,
                                FileId = s.Attributes.Files.FirstOrDefault().FileId,
                                FileName = s.Attributes.Files.FirstOrDefault().FileName,
                                HashDownload = !String.IsNullOrWhiteSpace(path),
                                Language = language,
                                MovieId = movieId,
                                SeasonNumber = seasonNumber,
                                Name = s.Attributes.Files.FirstOrDefault().FileName,
                                DownloadCount = s.Attributes.DownloadCount.ToString(),
                                Votes = s.Attributes.Votes,
                                Ratings = s.Attributes.Ratings,
                                SubtitleId = s.Attributes.SubtitleId
                            };
                            result.Add(sub);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.Error("Search subtitles error: " + e.Message);
                if (currentApiKey != keys.Count - 1)
                {
                    await ChangeKey();
                    await SearchSubtitle(movieId, seasonNumber, episodeNumber, language, path, movieName, imdbId );
                }
            }
            return result;
        }

        private static ImageSource SetFlag(CountryEnum country)
        {
            FlagControlBase f = new FlagIcon();
            string data = null;
            CountryDataFactory.CountryIndex.Value?.TryGetValue(country, out data);
            f.Data = data;
            return f.CreateImageSource(country);
        }


        //private static int HowMuchSame(string filePath, string subtitleFileName)
        //{
        //    int result = 0;

        //    var filePathAsArray = filePath.Split(".");
        //    var subitleFileNameAsArray = subtitleFileName.Split(".");

        //    foreach (var s in subitleFileNameAsArray)
        //    {
        //        var subtileFileNameWord = s.ToLower();
        //        foreach (var s1 in filePathAsArray)
        //        {
        //            var fileNameWord = s1.ToLower();

        //            if (fileNameWord == subtileFileNameWord)
        //            {
        //                result++;
        //            }
        //        }
        //    }


        //    return result;
        //}

        //private static int HowMuchSameSpace(string name, string subtitleFileName)
        //{
        //    int result = 0;

        //    var filePathAsArray = name.Split(" ");
        //    var subitleFileNameAsArray = subtitleFileName.Split(".");

        //    foreach (var s in subitleFileNameAsArray)
        //    {
        //        var subtileFileNameWord = s.ToLower();
        //        foreach (var s1 in filePathAsArray)
        //        {
        //            var fileNameWord = s1.ToLower();

        //            if (fileNameWord == subtileFileNameWord)
        //            {
        //                result++;
        //            }
        //        }
        //    }


        //    return result;
        //}


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


        public static async Task<string> GetSubtitlesByNameAndTmdbId(string name,int tmdbid, List<string> languages)
        {
            var search = new NewSubtitleSearch();
            search.Languages = languages;
            search.Query = name;
            search.TmdbId = tmdbid;

            var result = await _service.SearchSubtitlesAsync(search);
            if (result != null && result.Data.Count > 0)
            {
                return await DownloadSubtitle(result.Data.FirstOrDefault().Attributes.Files.FirstOrDefault().FileId, result.Data.FirstOrDefault().Attributes.Files.FirstOrDefault().FileName);
            }

            return "";
        }

        //public static async Task<string> GetSubtitlesByName(string query,List<string> languages)
        //{
        //    var search = new NewSubtitleSearch
        //    {
        //        Query = query,
        //        Languages = languages,
        //    };

        //    var result = await _service.SearchSubtitlesAsync(search);
        //    if (result != null && result.Data.Count > 0)
        //    {
        //        return await DownloadSubtitle(result.Data.FirstOrDefault().Attributes.Files.FirstOrDefault().FileId, result.Data.FirstOrDefault().Attributes.Files.FirstOrDefault().FileName);
        //    }

        //    return "";
        //}

        public static async Task<Subtitle> GetSubtitlesByNameAndEpisode(string query,int seasonNumber,int episodeNumber,List<string> languages,int imdbId)
        {
            try
            {
                Subtitle subtitleResult = new Subtitle();

                var search = new NewSubtitleSearch
                {
                    Query = query,
                    SeasonNumber = seasonNumber,
                    EpisodeNumber = episodeNumber,
                    Languages = languages,
                    ImdbId = imdbId
                };

                var result = await _service.SearchSubtitlesAsync(search);
                if (result != null && result.Data.Count > 0)
                {
                    var subtitle = result.Data.OrderByDescending(x => JaroWinklerSimilarity(query, x.Attributes.Files.FirstOrDefault().FileName))
                        .ThenByDescending(x => Double.Parse(x.Attributes.Ratings) > 7)
                        .ThenByDescending(x => x.Attributes.DownloadCount).FirstOrDefault();

                    if (subtitle != null)
                    {
                        subtitleResult.Fullpath = await DownloadSubtitle(subtitle.Attributes.Files.FirstOrDefault().FileId, subtitle.Attributes.Files.FirstOrDefault().FileName);
                        subtitleResult.HashDownload = false;
                        subtitleResult.Name = System.IO.Path.GetFileName(subtitleResult.Fullpath);
                        subtitleResult.Language = languages.FirstOrDefault();
                        subtitleResult.Synchronized = false;
                        subtitleResult.SubtitleId = subtitle.Attributes.SubtitleId;
                    }
                }

                return subtitleResult;
            }
            catch (System.Exception e)
            {
                Log.Error(e.Message);
                if (currentApiKey != keys.Count - 1)
                {
                    await ChangeKey();
                    await GetSubtitlesByNameAndEpisode( query,  seasonNumber,  episodeNumber,  languages,  imdbId);
                }
                return null;
            }
        }

        public static async Task<string> DownloadSubtitle(int FileId,string fileName)
        {
            var download = new NewDownload
            {
                FileId = FileId,
            };
            var path = System.IO.Path.Combine(AppSettingsManager.appSettings.SubtitlesPath, (fileName.HasSpecialChar() ? fileName.MakeStringWithoutSpecialChar() : fileName) + ".srt");
            if (!File.Exists(path))
            {
                var result = await _service.GetSubtitleForDownloadAsync(download, _token);
                if (result != null)
                {
                    remaining = new int();
                    remaining = result.Remaining;
                    if (result.Link != null)
                    {
                        try
                        {
                            var webClient = new WebClient();
                            await webClient.DownloadFileTaskAsync(result.Link, path);
                            webClient.Dispose();
                            return path;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error(ex.Message);
                        }
                    }
                    else
                    {
                        Log.Error("DOwnload subtitlle failed trying again...");
                        if (currentApiKey != keys.Count - 1)
                        {
                            await ChangeKey();
                            await DownloadSubtitle(FileId, fileName);
                        }
                    }
                }
                else
                {
                    Log.Error("DOwnload subtitlle failed trying again...");
                    if (currentApiKey != keys.Count - 1)
                    {
                        await ChangeKey();
                        await DownloadSubtitle(FileId, fileName);
                    }
                }
            }
            else
            {
                return path;
            }

            return "";
        }

        public static async Task<string> DownloadSubtitleWithoutSync(int FileId, string fileName)
        {
            var download = new NewDownload
            {
                FileId = FileId,
            };
            var path = System.IO.Path.Combine(AppSettingsManager.appSettings.SubtitlesPath, (fileName.HasSpecialChar() ? fileName.MakeStringWithoutSpecialChar() : fileName) + ".srt");

            var result = await _service.GetSubtitleForDownloadAsync(download, _token);
            remaining = new int();
            remaining = result.Remaining;
            if (result != null)
            {
                if (result.Link != null)
                {
                    try
                    {
                        var webClient = new WebClient();
                        await webClient.DownloadFileTaskAsync(result.Link, path);
                        webClient.Dispose();
                        return path;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                }
                else
                {
                    Log.Error("DOwnload subtitlle failed trying again...");
                    if (currentApiKey != keys.Count - 1)
                    {
                        await ChangeKey();
                        await DownloadSubtitleWithoutSync(FileId, fileName);
                    }
                }
            }
            else
            {
                Log.Error("DOwnload subtitlle failed trying again...");
                if (currentApiKey != keys.Count - 1)
                {
                    await ChangeKey();
                    await DownloadSubtitleWithoutSync(FileId, fileName);
                }
            }
            return "";
        }

        //public static void CopySrt(string srtFullFilePath, string subtitleName, string movieFolder)
        //{
        //    File.Move(srtFullFilePath, Path.Combine(movieFolder, subtitleName),true);
        //}

        //public static void SyncSubtitle(string movieFolder, string movieName, string subtitleFullPath, string subtitleName, string subtitlesFolder)
        //{
        //    //CopySrt(subtitleFullPath, subtitleName, movieFolder);
        //    //runCommand(movieFolder, $"ffs {movieName} -i {subtitleName} -o {subtitleName}",true);
        //    //CopySrt(Path.Combine(movieFolder, subtitleName), subtitleName, subtitlesFolder);

        //    SyncSubtitles(Path.Combine(movieFolder, movieName), subtitleFullPath, subtitleFullPath);
        //}

        //public static async Task<bool> SyncSubtitlesAsync(string videoDosyaYolu, string altyaziDosyaYolu, string ciktiDosyaYolu, PlayerWindow playerWindow)
        //{
        //    string pythonYolu = GetPythonInstallPath();
        //    string ffsubsyncYolu = GetFFSubSyncInstallPath();
        //    string ffmpegPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\ffmpeg\\bin\\", "ffmpeg.exe");

        //    // Komut argümanları
        //    string argumanlar = $"\"{ffsubsyncYolu}\" --ffmpeg-path \"{ffmpegPath}\" \"{videoDosyaYolu}\" -i \"{altyaziDosyaYolu}\" -o \"{ciktiDosyaYolu}\"";

        //    // Process başlatma
        //    Process process = new Process();
        //    process.StartInfo.FileName = pythonYolu;
        //    process.StartInfo.Arguments = argumanlar;
        //    process.StartInfo.UseShellExecute = false;
        //    process.StartInfo.RedirectStandardOutput = true;
        //    process.StartInfo.RedirectStandardError = true;
        //    process.StartInfo.CreateNoWindow = true;

        //    StringBuilder errorOutput = new StringBuilder();

        //    if (!playerWindow.closed)
        //    {
        //        playerWindow.Dispatcher.Invoke(() =>
        //        {
        //            playerWindow.ProgressGrid.Visibility = Visibility.Visible;
        //            playerWindow.LoadingTextBlock.Visibility = Visibility.Visible;
        //            playerWindow.NextEpisodeStackPanel.Visibility = Visibility.Collapsed;
        //            playerWindow.LoadingTextBlock.Text =
        //                App.Current.Resources["SynchronizationSubtitleProgress"].ToString();
        //            playerWindow.ProgressBarToPlay.IsIndeterminate = false;
        //            if (playerWindow.Player.MediaPlayer != null && playerWindow.Player.MediaPlayer.IsPlaying)
        //            {
        //                playerWindow.Player.MediaPlayer.Pause();
        //            }
        //        });
        //    }

        //    process.ErrorDataReceived += (sender, args) =>
        //    {
        //        Console.WriteLine(args.Data);
        //        errorOutput.AppendLine(args.Data);
        //        if (!string.IsNullOrEmpty(args.Data))
        //        {
        //            var match = Regex.Match(args.Data, @"(\d+)%");
        //            if (match.Success)
        //            {
        //                double progress = double.Parse(match.Groups[1].Value);
        //                if (!playerWindow.closed)
        //                {
        //                    playerWindow.Dispatcher.Invoke(() =>
        //                    {
        //                        playerWindow.ProgressBarToPlay.Value = progress;
        //                        if (playerWindow.Player.MediaPlayer != null &&
        //                            playerWindow.Player.MediaPlayer.IsPlaying)
        //                        {
        //                            playerWindow.Player.MediaPlayer.Pause();
        //                        }
        //                    });
        //                }
        //            }
        //        }
        //    };

        //    process.Start();
        //    process.BeginOutputReadLine();
        //    process.BeginErrorReadLine();

        //    await System.Threading.Tasks.Task.Run(() => process.WaitForExit());

        //    if (process.ExitCode != 0)
        //    {
        //        Log.Error("There was an error while synching subtitles: " + errorOutput.ToString());
        //        return false;
        //    }
        //    else
        //    {
        //        if (!playerWindow.closed)
        //        {
        //            playerWindow.Dispatcher.Invoke(() =>
        //            {
        //                playerWindow.ProgressGrid.Visibility = Visibility.Collapsed;
        //                playerWindow.LoadingTextBlock.Visibility = Visibility.Collapsed;
        //                playerWindow.NextEpisodeStackPanel.Visibility = Visibility.Collapsed;
        //                playerWindow.ProgressBarToPlay.IsIndeterminate = true;
        //                if (playerWindow.Player.MediaPlayer != null && !playerWindow.Player.MediaPlayer.IsPlaying)
        //                {
        //                    playerWindow.Player.MediaPlayer.Play();
        //                }
        //            });
        //        }

        //        return true;
        //    }
        //}

        public static async Task<bool> SyncSubtitlesAsync(string videoDosyaYolu, string altyaziDosyaYolu, string ciktiDosyaYolu, PlayerWindow playerWindow)
        {
            string ffsubsyncExeYolu = Path.Combine(Environment.CurrentDirectory, "ffsubsync_wrapper.exe");
            string ffmpegPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin", "ffmpeg.exe");

            string argumanlar = $"--ffmpeg-path \"{ffmpegPath}\" \"{videoDosyaYolu}\" -i \"{altyaziDosyaYolu}\" -o \"{ciktiDosyaYolu}\"";

            Process process = new Process();
            process.StartInfo.FileName = ffsubsyncExeYolu; 
            process.StartInfo.Arguments = argumanlar;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            StringBuilder errorOutput = new StringBuilder();

            if (!playerWindow.closed)
            {
                playerWindow.Dispatcher.Invoke(() =>
                {
                    playerWindow.ProgressGrid.Visibility = Visibility.Visible;
                    playerWindow.LoadingTextBlock.Visibility = Visibility.Visible;
                    playerWindow.NextEpisodeStackPanel.Visibility = Visibility.Collapsed;
                    playerWindow.LoadingTextBlock.Text =
                        App.Current.Resources["SynchronizationSubtitleProgress"].ToString();
                    playerWindow.ProgressBarToPlay.IsIndeterminate = false;
                    if (playerWindow.Player.MediaPlayer != null && playerWindow.Player.MediaPlayer.IsPlaying)
                    {
                        playerWindow.Player.MediaPlayer.Pause();
                    }
                });
            }

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine(args.Data);
                    errorOutput.AppendLine(args.Data);

                    // Progress yüzdesini yakala (Örnek: "50% completed")
                    var match = Regex.Match(args.Data, @"(\d+)%");
                    if (match.Success && double.TryParse(match.Groups[1].Value, out double progress))
                    {
                        playerWindow.Dispatcher.Invoke(() =>
                        {
                            playerWindow.ProgressBarToPlay.Value = progress;
                        });
                    }
                }
            };


            process.Start();
            process.BeginErrorReadLine();

            await System.Threading.Tasks.Task.Run(() => process.WaitForExit());

            // Sonuç Kontrolü (Aynı Kalıyor)
            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Altyazı senkronizasyon hatası: {errorOutput}");
                return false;
            }
            else
            {
                if (!playerWindow.closed)
                {
                    playerWindow.Dispatcher.Invoke(() =>
                    {
                        playerWindow.ProgressGrid.Visibility = Visibility.Collapsed;
                        playerWindow.LoadingTextBlock.Visibility = Visibility.Collapsed;
                        playerWindow.NextEpisodeStackPanel.Visibility = Visibility.Collapsed;
                        playerWindow.ProgressBarToPlay.IsIndeterminate = true;
                        if (playerWindow.Player.MediaPlayer != null && !playerWindow.Player.MediaPlayer.IsPlaying)
                        {
                            playerWindow.Player.MediaPlayer.Play();
                        }
                    });
                }

                return true;
            }
        }
        static string GetFFSubSyncInstallPath()
        {
            var pythonExePath = GetPythonInstallPath();
            var pythonDirectory = Path.GetDirectoryName(pythonExePath);
            string scriptsPath = Path.Combine(pythonDirectory, "Scripts");
            string ffsubsyncPath = Path.Combine(scriptsPath, "ffsubsync.exe");
            if (File.Exists(ffsubsyncPath))
            {
                return ffsubsyncPath;
            }
            else
            {
                return "";
            }
        }

        static string GetPythonInstallPath()
        {
            string pythonPath = null;

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Python\PythonCore"))
            {
                if (key != null)
                {
                    foreach (var version in key.GetSubKeyNames())
                    {
                        using (RegistryKey installKey = key.OpenSubKey(version + @"\InstallPath"))
                        {
                            pythonPath = installKey?.GetValue("")?.ToString();
                            if (!string.IsNullOrEmpty(pythonPath))
                                return System.IO.Path.Combine(pythonPath, "python.exe");
                        }
                    }
                }
            }

            return pythonPath ?? "Python bulunamadı";
        }

        public static void runCommand(string where, string command, bool admin = false)
        {
            const int ERROR_CANCELLED = 1223;

            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            if (admin)
            {
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.Verb = "runas";
            }
            p.StartInfo.Arguments = "/C cd " + where + " &" + command;

            if (admin)
            {
                try
                {
                    p.Start();
                    p.WaitForExit();
                }
                catch (Win32Exception ex)
                {
                    if (ex.NativeErrorCode == ERROR_CANCELLED)
                        Console.WriteLine("This operation needs administrator permission.", "peerflixGen");
                    else
                        throw;
                }
            }
            else
            {
                p.Start();
                p.WaitForExit();
            }
        }


        public static Dictionary<string, string> LanguagesToOpenSubtitles = new Dictionary<string, string>()
        {
                {"ab", "abk"},
                {"af", "afr"},
                {"sq", "alb"},
                {"am", "amh"},
                {"ar", "ara"},
                {"an", "arg"},
                {"hy", "arm"},
                {"as", "asm"},
                {"az", "aze"},
                {"eu", "baq"},
                {"be", "bel"},
                {"bn", "ben"},
                {"bs", "bos"},
                {"br", "bre"},
                {"bg", "bul"},
                {"my", "bur"},
                {"ca", "cat"},
                {"cn", "zhc"},
                {"sh", "hrv"},
                {"cs", "cze"},
                {"da", "dan"},
                {"nl", "dut"},
                {"en", "eng"},
                {"eo", "epo"},
                {"et", "est"},
                {"fi", "fin"},
                {"fr", "fre"},
                {"gd", "gla"},
                {"gl", "glg"},
                {"ka", "geo"},
                {"de", "ger"},
                {"el", "ell"},
                {"he", "heb"},
                {"hi", "hin"},
                {"hu", "hun"},
                {"is", "ice"},
                {"ig", "ibo"},
                {"id", "ind"},
                {"ia", "ina"},
                {"ga", "gle"},
                {"it", "ita"},
                {"ja", "jpn"},
                {"kn", "kan"},
                {"kk", "kaz"},
                {"km", "khm"},
                {"ko", "kor"},
                {"ku", "kur"},
                {"lv", "lav"},
                {"lt", "lit"},
                {"mk", "mac"},
                {"ms", "may"},
                {"ml", "mal"},
                {"mr", "mar"},
                {"mn", "mon"},
                {"nv", "nav"},
                {"ne", "nep"},
                {"se", "sme"},
                {"no", "nor"},
                {"oc", "oci"},
                {"fa", "per"},
                {"pl", "pol"},
                {"pt", "por"},
                {"ps", "pus"},
                {"ro", "rum"},
                {"ru", "rus"},
                {"sr", "scc"},
                {"sd", "snd"},
                {"si", "sin"},
                {"sk", "slo"},
                {"sl", "slv"},
                {"so", "som"},
                {"es", "spa"},
                {"sw", "swa"},
                {"sv", "swe"},
                {"tl", "tgl"},
                {"ta", "tam"},
                {"tt", "tat"},
                {"te", "tel"},
                {"th", "tha"},
                {"tr", "tur"},
                {"tk", "tuk"},
                {"uk", "ukr"},
                {"ur", "urd"},
                {"uz", "uzb"},
                {"vi", "vie"},
                {"cy", "wel"},
        };


        public static async Task<FastObservableCollection<Subtitle>> GetSubtitles(ShowType showType, string showName,
            int showId, int year, int seasonNumber, int episodeNumber, string language, string imdbId,bool justDownload)
        {
            FastObservableCollection<Subtitle> subtitles = new FastObservableCollection<Subtitle>();



            string pageUrl = "";

            Arguments arguments = new Arguments()
            {
                isMovie = showType == ShowType.Movie,
                season = (uint)seasonNumber,
                episode = (uint)episodeNumber,
                title = showName,
                year = (uint)year,
                language = language,
                outputDirectory = AppSettingsManager.appSettings.SubtitlesPath
            };

            OpenSubtitleAPI api = new OpenSubtitleAPI();

            List<Production> productions = api.searchProductions(arguments);
            Production selectedProduction;
            if (productions.Count != 0)
            {
                if (productions.Count == 1 && productions.FirstOrDefault().id == 0)
                {
                    pageUrl = ChangeLanguage(productions.FirstOrDefault().subtitleUrl, language);
                }
                else
                {
                    if (imdbId == "tt-1")
                    {
                        selectedProduction = productions.FirstOrDefault();

                        if (!string.IsNullOrWhiteSpace(selectedProduction.id.ToString()))
                        {
                            pageUrl = createSubtitleUrl(language, selectedProduction.id.ToString());
                            if (showType == ShowType.TvShow)
                            {
                                string seasonsHtml = api.fetchHtml(pageUrl).content;
                                List<subtitle_downloader.downloader.Season> seasons = SubtitleScraper.ScrapeSeriesTable(seasonsHtml);

                                if (seasons == null || seasons.Count == 0)
                                {
                                    return new FastObservableCollection<Subtitle>() { };
                                }

                                subtitle_downloader.downloader.Episode episode = getRequestedEpisode(seasons, arguments.season, arguments.episode);
                                if (episode != null)
                                {
                                    pageUrl = episode.getPageUrl();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (productions.Any(x => x.imdbId == imdbId))
                        {
                            selectedProduction = productions.FirstOrDefault(x => x.imdbId == imdbId);


                            if (!string.IsNullOrWhiteSpace(selectedProduction.id.ToString()))
                            {
                                pageUrl = createSubtitleUrl(language, selectedProduction.id.ToString());
                                if (showType == ShowType.TvShow)
                                {
                                    string seasonsHtml = api.fetchHtml(pageUrl).content;
                                    List<subtitle_downloader.downloader.Season> seasons = SubtitleScraper.ScrapeSeriesTable(seasonsHtml);

                                    if (seasons == null || seasons.Count == 0)
                                    {
                                        return new FastObservableCollection<Subtitle>() { };
                                    }

                                    subtitle_downloader.downloader.Episode episode = getRequestedEpisode(seasons, arguments.season, arguments.episode);
                                    if (episode != null)
                                    {
                                        pageUrl = episode.getPageUrl();
                                    }
                                }
                            }
                        }
                    }
                }



                if (pageUrl != "")
                {
                    var response = api.fetchHtml(pageUrl);
                    if (response.isError())
                    {
                        Log.Error("Failed to download subtitle");
                    }
                    else
                    {
                        string html = response.content;

                        List<SubtitleRow> rows = SubtitleScraper.ScrapeSubtitleTable(html);
                        foreach (var subtitleRow in rows)
                        {
                            if (subtitleRow.cdCount == "1CD")
                            {
                                Subtitle subtitle = new Subtitle();

                                if (subtitleRow.title != "")
                                {
                                    subtitle.Name = subtitleRow.title;
                                }
                                else
                                {
                                    subtitle.Name = subtitleRow.broadcastTitle;
                                }

                                subtitle.Votes = "-";
                                subtitle.Ratings = subtitleRow.rating.ToString();
                                subtitle.DownloadUrl = subtitleRow.getDownloadURL();
                                subtitle.SubtitleId = subtitleRow.subtitleId;
                                subtitle.DownloadCount = subtitleRow.downloads.ToString();
                                subtitle.EpisodeNumber = episodeNumber;
                                subtitle.SeasonNumber = seasonNumber;
                                subtitle.Language = language;
                                subtitle.MovieId = showId;
                                subtitle.IsOrg = true;
                                if (!justDownload)
                                {
                                    var country = SetFlag(languageCountries
                                        .FirstOrDefault(x => x.Key == language).Value);
                                    subtitle.Country = country;
                                }
                               
                                subtitles.Add(subtitle);
                            }
                        }
                    }
                }
            }

            return subtitles;
        }
        public static string ChangeLanguage(string url, string languageCode)
        {
            languageCode = OpenSubtitleAPI.toSubLanguageID(LanguagesToOpenSubtitles[languageCode]);
            return System.Text.RegularExpressions.Regex.Replace(url, @"sublanguageid-\w+", $"sublanguageid-{languageCode}");
        }

        public static async Task<Subtitle> SelectAndDownloadSubtitle(ShowType showType, string showName, int showId, int year, int seasonNumber, int episodeNumber, string language,string imdbId)
        {
            var subtitles = await GetSubtitles(showType, showName, showId, year, seasonNumber, episodeNumber, language, imdbId,true);
            if (subtitles == null || subtitles.Count == 0)
            {
                return null;
            }
            var a = subtitles.ToList().OrderByDescending(x => x.DownloadCount).ThenByDescending(x => double.Parse(x.Ratings) > 5 || double.Parse(x.Ratings) == 0);

            if (a.Any())
            {
                return await DownloadSubtitle(a.FirstOrDefault());
            }
            else
            {
                return null;
            }
        }

        public static async Task<Subtitle> DownloadSubtitle(Subtitle subtitle)
        {
            try
            {
                OpenSubtitleAPI api = new OpenSubtitleAPI();

                var fileName = Utils.sanitizeFileName(subtitle.Name);
                string outputDir = AppSettingsManager.appSettings.SubtitlesPath;

                string downloadedZip = outputDir == "." ? fileName + ".zip" : Path.Combine(outputDir, fileName) + ".zip";
                var download = await api.downloadSubtitle(subtitle.DownloadUrl, downloadedZip);
                if (!download)
                {
                    subtitle.Fullpath = "";
                    return subtitle;
                }

                List<string> extracted = Utils.unzip(downloadedZip, outputDir);
                if (extracted.Count == 0)
                {
                    subtitle.Fullpath = "";
                    return subtitle;
                }
                File.Delete(downloadedZip);

                subtitle.Fullpath = extracted[0];
                subtitle.Name = Path.GetFileName(subtitle.Fullpath);
                subtitle.HashDownload = false;
                subtitle.Synchronized = false;
                subtitle.IsOrg = true;

                return subtitle;
            }
            catch (System.Exception e)
            {
                Log.Error(e.Message);
                return null;
            }
        }

        public static async Task<Subtitle> DownloadSubtitleWithoutSync(Subtitle subtitle)
        {
            try
            {
                OpenSubtitleAPI api = new OpenSubtitleAPI();

                var fileName = Utils.sanitizeFileName(subtitle.Name);
                string outputDir = AppSettingsManager.appSettings.SubtitlesPath;

                string downloadedZip = outputDir == "." ? fileName + ".zip" : Path.Combine(outputDir, fileName) + ".zip";
                var download = await api.downloadSubtitle(subtitle.DownloadUrl, downloadedZip);
                if (!download)
                {
                    subtitle.Fullpath = "";
                    return subtitle;
                }

                List<string> extracted = Utils.unzip(downloadedZip, outputDir);
                if (extracted.Count == 0)
                {
                    subtitle.Fullpath = "";
                    return subtitle;
                }
                File.Delete(downloadedZip);

                subtitle.Fullpath = extracted[0];
                subtitle.Name = Path.GetFileName(subtitle.Fullpath);
                subtitle.HashDownload = true;
                subtitle.Synchronized = true;
                subtitle.IsOrg = true;

                return subtitle;
            }
            catch (System.Exception e)
            {
                Log.Error(e.Message);
                return null;
            }
        }

        static subtitle_downloader.downloader.Episode getRequestedEpisode(List<subtitle_downloader.downloader.Season> seasons, uint seasonNum, uint episodeNum)
        {
            subtitle_downloader.downloader.Season season = seasons.FirstOrDefault(x => x.number == seasonNum);
            if (season == null) return null;
            foreach (var episode in season.episodes)
            {
                if (episode.number == episodeNum)
                {
                    return episode;
                }
            }

            return null;
        }

        static string createSubtitleUrl(string language, string prodId)
        {
            string languageId = OpenSubtitleAPI.toSubLanguageID(LanguagesToOpenSubtitles[language]);
            return $"https://www.opensubtitles.org/en/search/sublanguageid-{languageId}/idmovie-{prodId}";
        }
    }

    public class SubtitleDownloadResult
    {
        public int Id { get; set; }
        public string FullPath { get; set; }
    }
}
