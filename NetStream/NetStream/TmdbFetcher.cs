// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Text.Json;
// using System.Text.Json.Serialization;
// using System.Threading.Tasks;
// using Google.Protobuf.WellKnownTypes;
//
// namespace NetStream;
//
// public class TmdbFetcher
// {
//     
//     public static async Task Main()
//     {
//         // Console.WriteLine("fetching Tmdb...");
//         //
//         // string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetStream");
//         //     
//         // if (!Directory.Exists(path))
//         // {
//         //     Directory.CreateDirectory(path);
//         // }
//         //
//         // string tmdbAllDataJson = System.IO.Path.Combine(path, "tmdbAllData.json");
//         //
//         // var allTmdbItems = await FetchAllTmdbItemsAsync("movie");
//         //
//         // await File.WriteAllTextAsync(tmdbAllDataJson, JsonSerializer.Serialize(allTmdbItems));
//         //
//         // Console.WriteLine("Veriler kaydedildi: movies.json, tv.json, people.json");
//         
//         
//         var json = File.ReadAllText("tmdbAllData.json");
//         var tmdbAllItems = JsonSerializer.DeserializeAsync<List<TmdbItem>>()
//     }
//
//    static async Task<List<TmdbItem>> FetchAllTmdbItemsAsync(string type)
// {
//     var result = new List<TmdbItem>();
//
//     try
//     {
//         // Movies (by year to bypass 500-page limit)
//         for (int year = 1900; year <= DateTime.Now.Year; year++)
//         {
//             try
//             {
//                 var discover = Service.client.DiscoverMoviesAsync();
//                 discover = discover.WherePrimaryReleaseIsInYear(year);
//
//                 var discoveredMovies = await discover.Query("en", 1);
//                 int totalPages = Math.Min(discoveredMovies.TotalPages, 500);
//
//                 for (int page = 1; page <= totalPages; page++)
//                 {
//                     var movies = await Service.client.DiscoverMoviesAsync()
//                         .WherePrimaryReleaseIsInYear(year)
//                         .Query("en", page);
//
//                     Console.WriteLine("Page: ");
//                     foreach (var item in movies.Results)
//                     {
//                         result.Add(new TmdbItem() { Name = item.Title, Type = ItemType.Movie });
//                         Console.WriteLine($"[Movie - {year}] {item.Title}");
//                     }
//
//                     Console.WriteLine("Result count: " + result.Count);
//                     await Task.Delay(200);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine($"Movie year {year} failed: {e.Message}");
//             }
//         }
//
//         // TV Shows (by year)
//         for (int year = 1950; year <= DateTime.Now.Year; year++)
//         {
//             try
//             {
//                 var discover = Service.client.DiscoverTvShowsAsync();
//                 discover = discover.WhereFirstAirDateIsInYear(year);
//
//                 var discoveredTv = await discover.Query(1);
//                 int totalPages = Math.Min(discoveredTv.TotalPages, 500);
//
//                 for (int page = 1; page <= totalPages; page++)
//                 {
//                     var shows = await Service.client.DiscoverTvShowsAsync()
//                         .WhereFirstAirDateIsInYear(year)
//                         .Query(page);
//
//                     foreach (var item in shows.Results)
//                     {
//                         result.Add(new TmdbItem() { Name = item.OriginalName, Type = ItemType.TvShow });
//                         Console.WriteLine($"[TV - {year}] {item.OriginalName}");
//                     }
//                     Console.WriteLine("Result count: " + result.Count);
//                     await Task.Delay(200);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine($"TV year {year} failed: {e.Message}");
//             }
//         }
//
//         // People (limited to 500 pages)
//         try
//         {
//             var discoveredPeoples = await Service.client.GetPersonPopularListAsync(1);
//             int totalPagesPeople = Math.Min(discoveredPeoples.TotalPages, 500);
//
//             for (int page = 1; page <= totalPagesPeople; page++)
//             {
//                 var peoplePage = await Service.client.GetPersonPopularListAsync(page);
//                 foreach (var item in peoplePage.Results)
//                 {
//                     result.Add(new TmdbItem() { Name = item.Name, Type = ItemType.People });
//                     Console.WriteLine($"[Person] {item.Name}");
//                 }
//                 Console.WriteLine("Result count: " + result.Count);
//                 await Task.Delay(200);
//             }
//         }
//         catch (Exception e)
//         {
//             Console.WriteLine($"People fetch failed: {e.Message}");
//         }
//     }
//     catch (Exception e)
//     {
//         Console.WriteLine($"Fatal error: {e.Message}");
//     }
//
//     return result;
// }
//
//   
// }
//
// public class TmdbItem
// {
//     public string Name { get; set; }
//     public ItemType Type { get; set; }
// }
//
// public enum ItemType
// {
//     Movie,
//     TvShow,
//     People
// }