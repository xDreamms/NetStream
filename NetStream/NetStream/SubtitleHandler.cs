using MovieCollection.OpenSubtitles.Models;
using MovieCollection.OpenSubtitles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Controls;
using Avalonia.Platform;
using Serilog;
using subtitle_downloader.downloader;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Countries;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using TMDbLib.Objects.General;
using TMDbLib.Objects.TvShows;
using System.Runtime.Intrinsics.Arm;
using Microsoft.Win32;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Threading;
using NetStream.Views;

namespace NetStream
{
    class SubtitleHandler
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static OpenSubtitlesOptions _options;
        private static OpenSubtitlesService _service;

        private static string _token;

        public static List<string> keys = new List<string>();

        public static int currentApiKey = 0;
        public static int? remaining = null;
        
        private static bool ShouldChangeKey(Exception e)
        {
            if (e == null) return false;
            
            string errorMessage = e.Message?.ToLower() ?? "";
            string stackTrace = e.StackTrace?.ToLower() ?? "";
            
            // Forbidden, 401, 403, rate limit gibi hatalarda API key değiştir
            if (errorMessage.Contains("forbidden") || 
                errorMessage.Contains("401") || 
                errorMessage.Contains("403") ||
                errorMessage.Contains("unauthorized") ||
                errorMessage.Contains("rate limit") ||
                errorMessage.Contains("quota") ||
                errorMessage.Contains("api key") ||
                stackTrace.Contains("forbidden") ||
                stackTrace.Contains("401") ||
                stackTrace.Contains("403"))
            {
                return true;
            }
            
            // HttpRequestException veya HttpResponseException içinde status code kontrolü
            if (e is System.Net.Http.HttpRequestException httpEx)
            {
                errorMessage = httpEx.Message?.ToLower() ?? "";
                if (errorMessage.Contains("forbidden") || 
                    errorMessage.Contains("401") || 
                    errorMessage.Contains("403"))
                {
                    return true;
                }
            }
            
            return false;
        }

        private static List<string> BuildApiKeys()
        {
            var result = new List<string>();

            void AddKey(string? key)
            {
                if (!string.IsNullOrWhiteSpace(key) && !result.Contains(key))
                {
                    result.Add(key);
                }
            }

            AddKey(AppSettingsManager.appSettings.OpenSubtitlesApiKey);
            AddKey(NetStreamEnvironment.GetString("NETSTREAM_OPENSUBTITLES_API_KEY"));

            foreach (var key in NetStreamEnvironment.SplitList(NetStreamEnvironment.GetString("NETSTREAM_OPENSUBTITLES_API_KEYS")))
            {
                AddKey(key);
            }

            return result;
        }
        
        public static async System.Threading.Tasks.Task Init()
        {
            try
            {
                keys = BuildApiKeys();
                if (keys.Count == 0)
                {
                    Log.Warning("No OpenSubtitles API key configured.");
                    return;
                }

                string selectedKey = keys[0];
                currentApiKey = 0;
                
                
                _options = new OpenSubtitlesOptions
                {
                    ApiKey = selectedKey,
                    ProductInformation = new ProductHeaderValue("your-app-name", "your-app-version"),
                };

                _service = new OpenSubtitlesService(_httpClient, _options);

                // API testi - basit bir arama yaparak test ediyoruz
                try
                {
                    var testSearch = new NewSubtitleSearch
                    {
                        Query = "test",
                        Languages = new List<string>() { "en" }
                    };
                    
                    var testResult = await _service.SearchSubtitlesAsync(testSearch);
                    
                    if (testResult != null)
                    {
                       
                        
                        if (testResult.Data != null && testResult.Data.Count > 0)
                        {
                            var firstResult = testResult.Data[0];
                           
                        }
                    }
                }
                catch (System.Exception testEx)
                {
                    Console.WriteLine($"✗ API Test BAŞARISIZ!");
                    Console.WriteLine($"  Hata: {testEx.Message}");
                    Console.WriteLine($"  Hata Tipi: {testEx.GetType().Name}");
                    
                    if (testEx.InnerException != null)
                    {
                        Console.WriteLine($"  İç Hata: {testEx.InnerException.Message}");
                    }
                    
                    Log.Error($"OpenSubtitles API test failed: {testEx.Message}");
                    
                    // API key değiştirmeyi dene
                    if (keys.Count > 1 && currentApiKey < keys.Count - 1)
                    {
                        Console.WriteLine("Diğer API key'leri deneniyor...");
                        for (int i = 1; i < keys.Count; i++)
                        {
                            try
                            {
                                Console.WriteLine($"  API Key {i} deneniyor...");
                                currentApiKey = i;
                                _options = new OpenSubtitlesOptions
                                {
                                    ApiKey = keys[i],
                                    ProductInformation = new ProductHeaderValue("your-app-name", "your-app-version"),
                                };
                                _service = new OpenSubtitlesService(_httpClient, _options);
                                
                                var retestSearch = new NewSubtitleSearch
                                {
                                    Query = "test",
                                    Languages = new List<string>() { "en" }
                                };
                                
                                var retestResult = await _service.SearchSubtitlesAsync(retestSearch);
                                
                                if (retestResult != null)
                                {
                                    Console.WriteLine($"✓ API Key {i} BAŞARILI!");
                                    Console.WriteLine($"  Bulunan subtitle sayısı: {retestResult.Data?.Count ?? 0}");
                                    if (retestResult.Data != null && retestResult.Data.Count > 0)
                                    {
                                        Console.WriteLine($"  İlk sonuç: {retestResult.Data[0].Attributes?.Files?.FirstOrDefault()?.FileName ?? "N/A"}");
                                    }
                                    break;
                                }
                            }
                            catch (System.Exception retryEx)
                            {
                                Console.WriteLine($"✗ API Key {i} başarısız: {retryEx.Message}");
                            }
                        }
                    }
                }
                
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"✗ Subtitle Initialization CRITICAL ERROR: {e.Message}");
                Console.WriteLine($"  Stack Trace: {e.StackTrace}");
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
                bool shouldChange = ShouldChangeKey(e) || currentApiKey < keys.Count - 1;
                
                if (shouldChange && currentApiKey < keys.Count - 1)
                {
                    Log.Information($"Changing API key due to error. Current key index: {currentApiKey}");
                    await ChangeKey();
                    return await GetSubtitlesByTMDbId(name, tmdbId, languages);
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
                bool shouldChange = ShouldChangeKey(e) || currentApiKey < keys.Count - 1;
                
                if (shouldChange && currentApiKey < keys.Count - 1)
                {
                    Log.Information($"Changing API key due to error. Current key index: {currentApiKey}");
                    await ChangeKey();
                    return await GetSubtitlesByHash(filePath, languages, movieName);
                }

                return null;
            }
        }
  
   


        public static async Task<ObservableCollection<Subtitle>> SearchSubtitle(Subtitle currentSubtitle,string path = "",string movieName = "",int imdbId = -1)
        {
            Log.Information($"Started Subtitle Search from Manager. Subtitle name: {currentSubtitle.Name}\n Id:{currentSubtitle.SubtitleId} \n HashDownload:{currentSubtitle.HashDownload}");
            ObservableCollection<Subtitle> result = new ObservableCollection<Subtitle>();
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
                        Log.Information($"Found {Searchresult.Data.Count} Subtitles");
                        foreach (var VARIABLE in Searchresult.Data)
                        {
                            Log.Information(VARIABLE.Attributes.Files.FirstOrDefault().FileName);
                        }
                        Log.Information("Searched completed found " + Searchresult.Data.Count + " subtitles");
                      
                        foreach (var s in Searchresult.Data)
                        {
                            Subtitle sub = new Subtitle()
                            {
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
                Log.Error("Search subtitles error: " + e.Message+"\n Stack Trace: " + e.StackTrace);
                bool shouldChange = ShouldChangeKey(e) || currentApiKey < keys.Count - 1;
                
                if (shouldChange && currentApiKey < keys.Count - 1)
                {
                    Log.Information($"Changing API key due to error. Current key index: {currentApiKey}");
                    await ChangeKey();
                    return await SearchSubtitle(currentSubtitle,  path ,  movieName ,  imdbId);
                }
            }
            return result;
        }

        public static async Task<ObservableCollection<Subtitle>> SearchSubtitle(int movieId,int seasonNumber,int episodeNumber,string language,string path = "", string movieName = "", int imdbId = -1)
        {
            ObservableCollection<Subtitle> result = new ObservableCollection<Subtitle>();
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
                        
                        foreach (var s in Searchresult.Data)
                        {
                            Subtitle sub = new Subtitle()
                            {
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
                bool shouldChange = ShouldChangeKey(e) || currentApiKey < keys.Count - 1;
                
                if (shouldChange && currentApiKey < keys.Count - 1)
                {
                    Log.Information($"Changing API key due to error. Current key index: {currentApiKey}");
                    await ChangeKey();
                    return await SearchSubtitle(movieId, seasonNumber, episodeNumber, language, path, movieName, imdbId );
                }
            }
            return result;
        }

      
        
        // Silebilirsiniz, artık kullanılmıyor
        private static MemoryStream CreateSimpleColorBitmap(byte r, byte g, byte b)
        {
            int width = 16;
            int height = 12;
            var bytes = new byte[width * height * 4]; // RGBA
            
            for (int i = 0; i < width * height; i++)
            {
                int offset = i * 4;
                bytes[offset] = r;
                bytes[offset + 1] = g;
                bytes[offset + 2] = b;
                bytes[offset + 3] = 255; // Alpha
            }
            
            return new MemoryStream(bytes);
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
                bool shouldChange = ShouldChangeKey(e) || currentApiKey < keys.Count - 1;
                
                if (shouldChange && currentApiKey < keys.Count - 1)
                {
                    Log.Information($"Changing API key due to error. Current key index: {currentApiKey}");
                    await ChangeKey();
                    return await GetSubtitlesByNameAndEpisode( query,  seasonNumber,  episodeNumber,  languages,  imdbId);
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
                        if (currentApiKey < keys.Count - 1)
                        {
                            Log.Information($"Changing API key for download. Current key index: {currentApiKey}");
                            await ChangeKey();
                            return await DownloadSubtitle(FileId, fileName);
                        }
                    }
                }
                else
                {
                    Log.Error("DOwnload subtitlle failed trying again...");
                    if (currentApiKey < keys.Count - 1)
                    {
                        Log.Information($"Changing API key for download. Current key index: {currentApiKey}");
                        await ChangeKey();
                        return await DownloadSubtitle(FileId, fileName);
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
                    if (currentApiKey < keys.Count - 1)
                    {
                        Log.Information($"Changing API key for download. Current key index: {currentApiKey}");
                        await ChangeKey();
                        return await DownloadSubtitleWithoutSync(FileId, fileName);
                    }
                }
            }
            else
            {
                Log.Error("DOwnload subtitlle failed trying again...");
                if (currentApiKey < keys.Count - 1)
                {
                    Log.Information($"Changing API key for download. Current key index: {currentApiKey}");
                    await ChangeKey();
                    return await DownloadSubtitleWithoutSync(FileId, fileName);
                }
            }
            return "";
        }

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
               await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    playerWindow.ProgressGrid.IsVisible = true;
                    playerWindow.LoadingTextBlock.IsVisible = true;
                    playerWindow.NextEpisodeStackPanel.IsVisible = false;
                    playerWindow.LoadingTextBlock.Text =
                        ResourceProvider.GetString("SynchronizationSubtitleProgress");
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
                        Dispatcher.UIThread.Invoke(() =>
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
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        playerWindow.ProgressGrid.IsVisible = false;
                        playerWindow.LoadingTextBlock.IsVisible = false;
                        playerWindow.NextEpisodeStackPanel.IsVisible = false;
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
        
          public static async Task<bool> SyncSubtitlesAsync(string videoDosyaYolu, string altyaziDosyaYolu, string ciktiDosyaYolu)
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

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine(args.Data);
                    errorOutput.AppendLine(args.Data);
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
                Console.WriteLine("Senkronizasyon tamamlandı.");

                return true;
            }
        }
        static string GetFFSubSyncInstallPath()
        {
            var pythonExePath = GetPythonInstallPath();
            var pythonDirectory = Path.GetDirectoryName(pythonExePath);
            string scriptsPath = Path.Combine(pythonDirectory, "Scripts");
            string ffsubsyncPath = Path.Combine(scriptsPath, "ffsubsync_wrapper.exe");
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
            string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
           
            string pythonPath =  localFolder + "\\Programs\\Python\\Python310\\python.exe";

            return pythonPath ;
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


        public static async Task<ObservableCollection<Subtitle>> GetSubtitles(ShowType showType, string showName,
            int showId, int year, int seasonNumber, int episodeNumber, string language, string imdbId,bool justDownload)
        {
            ObservableCollection<Subtitle> subtitles = new ObservableCollection<Subtitle>();



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
                                    return new ObservableCollection<Subtitle>() { };
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
                                        return new ObservableCollection<Subtitle>() { };
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
    
    public class SubtitleJsonConverter : JsonConverter<Subtitle>
    {
        public override Subtitle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new Subtitle
            {
                MovieId = 0,
                SeasonNumber = 0,
                EpisodeNumber = 0,
                Name = null,
                Fullpath = null,
                HashDownload = false,
                Language = null,
                Synchronized = false,
                SubtitleId = null,
                Country = null,
                FileId = null,
                FileName = null,
                DownloadCount = null,
                Votes = null,
                Ratings = null,
                Name2 = null,
                PublishDate = default,
                IsOrg = false,
                ImdbId = null,
                DownloadUrl = null
            };
        
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    try
                    {
                        switch (propertyName?.ToLower())
                        {
                            case "movieid":
                                result.MovieId = reader.TokenType == JsonTokenType.Null ? 0 : reader.GetInt32();
                                break;
                            case "seasonnumber":
                                result.SeasonNumber = reader.TokenType == JsonTokenType.Null ? 0 : reader.GetInt32();
                                break;
                            case "episodenumber":
                                result.EpisodeNumber = reader.TokenType == JsonTokenType.Null ? 0 : reader.GetInt32();
                                break;
                            case "name":
                                result.Name = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                                break;
                            case "fullpath":
                                result.Fullpath = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                                break;
                            case "hashdownload":
                                result.HashDownload = reader.TokenType == JsonTokenType.Null ? false : reader.GetBoolean();
                                break;
                            case "language":
                                result.Language = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                                break;
                            case "synchronized":
                                result.Synchronized = reader.TokenType == JsonTokenType.Null ? false : reader.GetBoolean();
                                break;
                            case "subtitleid":
                                result.SubtitleId = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                                break;
                            case "fileid":
                                result.FileId = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                                break;
                            case "filename":
                                result.FileName = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                                break;
                            case "downloadcount":
                                result.DownloadCount = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                                break;
                            case "votes":
                                result.Votes = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                                break;
                            case "ratings":
                                result.Ratings = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                                break;
                            case "name2":
                                result.Name2 = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                                break;
                            case "publishdate":
                                result.PublishDate = reader.TokenType == JsonTokenType.Null ? default : reader.GetDateTime();
                                break;
                            case "isorg":
                                result.IsOrg = reader.TokenType == JsonTokenType.Null ? false : reader.GetBoolean();
                                break;
                            case "imdbid":
                                result.ImdbId = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                                break;
                            case "downloadurl":
                                result.DownloadUrl = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                                break;
                            // Country özelliği IImage türünde olduğundan basit bir şekilde ayrıştırılamaz
                            // Karmaşık nesneler özel işlem gerektirebilir
                            default:
                                // Tanınmayan özellikleri atla
                                reader.Skip();
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"Özellik okuma hatası ({propertyName}): {ex.Message}");
                        // Hata durumunda devam et, okumayı durdurma
                    }
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, Subtitle value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            
            // Temel özellikleri yaz
            writer.WriteNumber("movieid", value.MovieId);
            writer.WriteNumber("seasonnumber", value.SeasonNumber);
            writer.WriteNumber("episodenumber", value.EpisodeNumber);
            
            // Null kontrolü yaparak string özellikleri yaz
            if (value.Name != null)
                writer.WriteString("name", value.Name);
            
            if (value.Fullpath != null)
                writer.WriteString("fullpath", value.Fullpath);
            
            writer.WriteBoolean("hashdownload", value.HashDownload);
            
            if (value.Language != null)
                writer.WriteString("language", value.Language);
            
            writer.WriteBoolean("synchronized", value.Synchronized);
            
            if (value.SubtitleId != null)
                writer.WriteNumber("subtitleid", value.SubtitleId.Value);
            
            if (value.FileId != null)
                writer.WriteNumber("fileid", value.FileId.Value);
            
            if (value.FileName != null)
                writer.WriteString("filename", value.FileName);
            
            if (value.DownloadCount != null)
                writer.WriteString("downloadcount", value.DownloadCount);
            
            if (value.Votes != null)
                writer.WriteString("votes", value.Votes);
            
            if (value.Ratings != null)
                writer.WriteString("ratings", value.Ratings);
            
            if (value.Name2 != null)
                writer.WriteString("name2", value.Name2);
            
            // DateTime değeri için varsayılan değer kontrolü
            if (value.PublishDate != default)
                writer.WriteString("publishdate", value.PublishDate.ToString("o"));
            
            writer.WriteBoolean("isorg", value.IsOrg);
            
            if (value.ImdbId != null)
                writer.WriteString("imdbid", value.ImdbId);
            
            if (value.DownloadUrl != null)
                writer.WriteString("downloadurl", value.DownloadUrl);
            
            // Country (IImage) özelliği karmaşık bir tür ve burada seri hale getirilemez
            // Gerekirse bu özellik için özel bir işleme eklenebilir
            
            writer.WriteEndObject();
        }
    }
}
