//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Security.Policy;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Shapes;
//using BencodeNET.Parsing;
//using Windows.Media.Protection.PlayReady;
//using Serilog;
//using YoutubeExplode.Channels;
//using DynamicData;
//using NetStream.Views;
//using Polly;
//using Polly.Timeout;
//using Path = System.IO.Path;
//using BencodeNET.Torrents;
//using TorrentWrapper;
//using System.Reflection.Metadata;
//using System.Threading;
//using System.Net.Http;
//using System.Xml;
//using OpenCvSharp.ImgHash;
//using Microsoft.Win32;

//namespace NetStream
//{
//    public class TorrentPiecePriority
//    {
//        public string Hash { get; set; }
//        public int FileIndex { get; set; }
//        public CancellationTokenSource CancellationTokenSource { get; set; }
//    }
//    public class Libtorrent
//    {
//        public static Client client;
//        public static SessionManager sessionManager;

//        public static async Task Initialize()
//        {
//            try
//            {
//                client = new Client("lG!o0)%]?M85Q`57FZqzqf4U|t1@@"); 
//                sessionManager = new SessionManager();
//                _ = Task.Run((async () =>
//            {
//                sessionManager.StartListeningAlerts();
//            }));
//            }
//            catch (Exception e)
//            {
//                new CustomMessageBox("Initialization libtorrent failed. Please check your api settings",
//                    MessageType.Error, MessageButtons.Ok).ShowDialog();
//                Log.Error("Initialization libtorrent failed: " + e.Message);
//            }
//        }

//        public static TorrentFile? currentEpisodeFile;
//        public static int lastEpisodeIndex = -1;
//        public static List<TorrentPiecePriority> TorrentPiecePriorities = new List<TorrentPiecePriority>();
        
//        public static async Task ChangeTvShowEpisodeFilePrioritiesNew(TorrentHandleWrapper torrent,CancellationToken token)
//        {
//            try
//            {
//                var files = await GetFiles(torrent.Hash);
//                var episodeFiles = files.Where(x => x.IsMediaFile && !x.IsCompleted && GetSeasonNumberFromFileName(x.Name) != -2
//                                                    && GetEpisodeNumberFromFileName(x.Name) != -2)
//                    .OrderBy(x => GetSeasonNumberFromFileName(x.Name))
//                    .ThenBy(x => GetEpisodeNumberFromFileName(x.Name)).ToList();

//                while (!token.IsCancellationRequested && episodeFiles.Count != 0)
//                {
//                    if (currentEpisodeFile == null)
//                    {
//                        if (!TorrentPiecePriorities.Any(x =>
//                                x.Hash.ToLower() == torrent.Hash.ToLower() && x.FileIndex == episodeFiles[0].Index))
//                        {
//                            CancelAllPreviousPriorities(torrent);
//                            TorrentPiecePriority torrentPiecePriority = new TorrentPiecePriority();
//                            torrentPiecePriority.Hash = torrent.Hash;
//                            torrentPiecePriority.FileIndex = episodeFiles[0].Index;
//                            torrentPiecePriority.CancellationTokenSource = new CancellationTokenSource();
//                            TorrentPiecePriorities.Add(torrentPiecePriority);
//                            await Task.Run((() => DownloadFileSequentially(torrent, episodeFiles[0].Index, torrentPiecePriority.CancellationTokenSource.Token)));
//                        }
//                    }
//                    else
//                    {
//                        var c = (episodeFiles.FirstOrDefault(x => x.Index == currentEpisodeFile.Index));
//                        if (c == null)
//                        {
//                            if (!TorrentPiecePriorities.Any(x =>
//                                    x.Hash.ToLower() == torrent.Hash.ToLower() && x.FileIndex == episodeFiles[0].Index))
//                            {
//                                CancelAllPreviousPriorities(torrent);
//                                TorrentPiecePriority torrentPiecePriority = new TorrentPiecePriority();
//                                torrentPiecePriority.Hash = torrent.Hash;
//                                torrentPiecePriority.FileIndex = episodeFiles[0].Index;
//                                torrentPiecePriority.CancellationTokenSource = new CancellationTokenSource();
//                                TorrentPiecePriorities.Add(torrentPiecePriority);
//                                await Task.Run((() => DownloadFileSequentially(torrent, episodeFiles[0].Index,
//                                    torrentPiecePriority.CancellationTokenSource.Token)));
//                            }
//                        }
//                        else
//                        {
//                            if (!TorrentPiecePriorities.Any(x =>
//                                    x.Hash.ToLower() == torrent.Hash.ToLower() && x.FileIndex == currentEpisodeFile.Index))
//                            {
//                                CancelAllPreviousPriorities(torrent);
//                                TorrentPiecePriority torrentPiecePriority = new TorrentPiecePriority();
//                                torrentPiecePriority.Hash = torrent.Hash;
//                                torrentPiecePriority.FileIndex = currentEpisodeFile.Index;
//                                torrentPiecePriority.CancellationTokenSource = new CancellationTokenSource();
//                                TorrentPiecePriorities.Add(torrentPiecePriority);
//                                await Task.Run((() => DownloadFileSequentially(torrent, currentEpisodeFile.Index,
//                                    torrentPiecePriority.CancellationTokenSource.Token)));
//                            }
//                        }
//                    }

//                    files = await GetFiles(torrent.Hash);
//                    episodeFiles = files.Where(x => x.IsMediaFile && !x.IsCompleted && GetSeasonNumberFromFileName(x.Name) != -2
//                                                    && GetEpisodeNumberFromFileName(x.Name) != -2)
//                        .OrderBy(x => GetSeasonNumberFromFileName(x.Name))
//                        .ThenBy(x => GetEpisodeNumberFromFileName(x.Name)).ToList();
//                }
//                torrent.Dispose();
//                torrentHandleWrappers.Remove(torrent.Hash);
//                Torrents.Remove(torrent);
//                Console.WriteLine("İndirme işlemi bitti.");
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on Proiorities: " + e.Message);
//            }
//        }

//        public static void CancelAllPreviousPriorities(TorrentHandleWrapper torrent)
//        {
//            try
//            {
//                var toCancel = TorrentPiecePriorities
//                    .Where(x => x.Hash.Equals(torrent.Hash, StringComparison.OrdinalIgnoreCase))
//                    .ToList();

//                foreach (var torrentPiecePriority in toCancel)
//                {
//                    torrentPiecePriority.CancellationTokenSource.Cancel();
//                    torrentPiecePriority.CancellationTokenSource.Dispose(); 
//                }

//                TorrentPiecePriorities.RemoveAll(x => x.Hash.Equals(torrent.Hash, StringComparison.OrdinalIgnoreCase));
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on CancelAllPrios" + e.Message);
//            }
//        }


//        public static async Task DownloadFileSequentially(TorrentHandleWrapper torrent, int fileIndex, CancellationToken cancellationToken)
//        {
//            try
//            {
//                using var _ = cancellationToken.Register(() =>
//                    Console.WriteLine("İndirme işlemi iptal ediliyor..."));


//                var mediaFile = (await GetFiles(torrent.Hash)).FirstOrDefault(x => x.Index == fileIndex);

//                if (mediaFile == null) return;

//                Console.WriteLine(mediaFile.Name + " İndiriliyor...");
                
//                var (startIndex, endIndex) = await GetValidPieceRange(torrent, mediaFile.Index);
//                if (startIndex > endIndex) return;

//                await WaitForValidPieces(torrent, cancellationToken);
//                //endIndex = Math.Min(endIndex, pieces.Length - 1);

//                torrent.ClearPiecePrioritiesExceptFile(fileIndex);
//                torrent.ClearPieceDeadLines();

//                // Corrected buffer ranges (first 3 and last 3)
//                int firstBufferEnd = Math.Min(startIndex + bufferPieces - 1, endIndex);
//                int lastBufferStart = Math.Max(endIndex - bufferPieces + 1, startIndex);

//                // Download buffers in parallel
//                await Task.WhenAll(
//                    DownloadPieceRange(torrent, startIndex, firstBufferEnd, lastBufferStart, endIndex, checkInterval, cancellationToken)
//                );

//                // Calculate remaining pieces
//                int middleStart = firstBufferEnd + 1;
//                int middleEnd = lastBufferStart - 1;

//                // Download remaining pieces in batches
//                if (middleStart <= middleEnd)
//                {
//                    for (int batchStart = middleStart; batchStart <= middleEnd; batchStart += batchSize)
//                    {
//                        int batchEnd = Math.Min(batchStart + batchSize - 1, middleEnd);
//                        await DownloadPieceBatch(torrent, batchStart, batchEnd, checkInterval, cancellationToken);
//                    }
//                }
          
//            }
//            catch (OperationCanceledException)
//            {
//                Console.WriteLine("İndirme başarıyla iptal edildi.");
//            }
//            catch (Exception e)
//            {
//                Log.Error($"Error on download file: {e.Message}");
//            }
//        }



//        public static async Task ChangeEpisodeFileToMaximalPriority(Item item, int season, int episode)
//        {
//            try
//            {
//                var files = await GetFiles(item);
//                currentEpisodeFile = files.FirstOrDefault(x => GetSeasonNumberFromFileName(x.Name) == season &&
//                                                          GetEpisodeNumberFromFileName(x.Name) == episode);
//                lastEpisodeIndex = currentEpisodeFile != null ? currentEpisodeFile.Index : -1;
//                CancelAllPreviousPriorities(await GetTorrentHandle(item));
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on changing prio" + e.Message);
//            }
//        }

//        public static async Task ChangeMovieCollectionFilePriorityToMaximal(Item item, int fileIndex)
//        {
//            try
//            {
//                //TODO
//                //await DownloadFileSequentially(item.Hash, fileIndex);
//            }
//            catch (Exception e)
//            {
//                Log.Error(e.Message);
//            }
//        }


//        public static async Task<TorrentState> GetTorrentState(Item item)
//        {
//            try
//            {
//                var torrent = await GetTorrentHandle(item);
//                if (torrent != null)
//                {
//                    return torrent.GetTorrentState;
//                }
//            }
//            catch (Exception e)
//            {
//                Log.Error(e.Message);
//            }
//            return TorrentState.Unknown;
//        }

//        public static async Task<TorrentHandleWrapper> GetTorrentHandle(Item item)
//        {
//            try
//            {
//                string hash = item.Hash;
//                if (torrentHandleWrappers.ContainsKey(hash))
//                {
//                    return torrentHandleWrappers[hash];
//                }
//                else
//                {
//                    var torrent =
//                        Torrents.FirstOrDefault(x => string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
//                    if (torrent != null)
//                    {
//                        if (!torrentHandleWrappers.ContainsKey(hash))
//                            torrentHandleWrappers.Add(hash, torrent);
//                        return torrent;
//                    }
//                    else
//                    {
//                        Torrents = await client.GetTorrentsAsync();
//                    }
//                    torrent = Torrents.FirstOrDefault(x => string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
//                    if (torrent != null)
//                    {
//                        if (!torrentHandleWrappers.ContainsKey(hash))
//                            torrentHandleWrappers.Add(hash, torrent);
//                        return torrent;
//                    }
//                    else
//                    {
//                        return null;
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on torrent handle: "+e.Message);
//                return null;
//            }
//        }

//        public static List<TorrentHandleWrapper> Torrents = new List<TorrentHandleWrapper>();

//        public static async Task<TorrentHandleWrapper> GetTorrentHandle(string hash)
//        {
//            try
//            {
//                if (torrentHandleWrappers.ContainsKey(hash))
//                {
//                    return torrentHandleWrappers[hash];
//                }
//                else
//                {
//                    var torrent =
//                        Torrents.FirstOrDefault(x => string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
//                    if (torrent != null)
//                    {
//                        if (!torrentHandleWrappers.ContainsKey(hash))
//                            torrentHandleWrappers.Add(hash, torrent);
//                        return torrent;
//                    }
//                    else
//                    {
//                        Torrents = await client.GetTorrentsAsync();
//                    }
//                    torrent = Torrents.FirstOrDefault(x => string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
//                    if (torrent != null)
//                    {
//                        if(!torrentHandleWrappers.ContainsKey(hash))
//                            torrentHandleWrappers.Add(hash, torrent);
//                        return torrent;
//                    }
//                    else
//                    {
//                        return null;
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on torrent handle: " + e.Message);
//                return null;
//            }
//        }

//        public static async Task<List<TorrentFile>> GetFiles(Item item)
//        {
//            try
//            {
//                TorrentHandleWrapper torrent = await GetTorrentHandle(item);
//                if (torrent != null)
//                {
//                    try
//                    {
//                        var content = await Task.Run(() => torrent.GetFiles());
//                        while (content.Count == 0)
//                        {
//                            content = await Task.Run(() => torrent.GetFiles());
//                        }

//                        return content.OrderBy(f => f.Index).ToList();
//                    }
//                    catch (Exception e)
//                    {
//                        Log.Error("Error on get files " + e.Message);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on get files: " +e.Message);
//            }
//            return new List<TorrentFile>();
//        }

//        public static async Task<List<TorrentFile>> GetFiles(string hash)
//        {
//            try
//            {
//                TorrentHandleWrapper torrent = await GetTorrentHandle(hash);

//                if (torrent != null)
//                {
//                    try
//                    {
//                        var content =  await Task.Run(() => torrent.GetFiles());
//                        while (content.Count == 0)
//                        {
//                            content = await Task.Run(() => torrent.GetFiles());
//                        }

//                        return content.OrderBy(f => f.Index).ToList();
//                    }
//                    catch (Exception e)
//                    {
//                        Log.Error(e.Message);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on get files: " +e.Message);
//            }

//            return new List<TorrentFile>();
//        }

//        public static async Task<List<(TimeSpan start, TimeSpan end)>> GetAvailaibleSeconds(string hash,int durationInSeconds,int fileIndex)
//        {
//            TorrentHandleWrapper torrent = await GetTorrentHandle(hash);
//            List<(TimeSpan start, TimeSpan end)> pieceTimeRanges = new List<(TimeSpan, TimeSpan)>();
//            if (torrent != null)
//            {
//                try
//                {
//                    var content =await GetFiles(hash);
//                    if (content.Count == 0)
//                    {
//                        return new List<(TimeSpan start, TimeSpan end)>();
//                    }
//                    var currentFile = content.FirstOrDefault(x => x.Index == fileIndex);

//                    if (currentFile != null)
//                    {
//                        long totalFileSize = currentFile.Size;
//                        int totalDuration = durationInSeconds;


//                        long pieceSize = torrent.PieceSize;
//                        var torrentPieces = GetStatus(torrent.Hash).Pieces.ToList();

//                        var filePiexeRange =
//                            await Task.Run((() => torrent.GetFilePieceRange(currentFile.Index)));

//                        int retryCount = 1;
//                        while (filePiexeRange == null && retryCount < 10)
//                        {
//                            filePiexeRange = await Task.Run((() => torrent.GetFilePieceRange(currentFile.Index)));
//                            retryCount++;
//                            await Task.Delay(100);
//                        }

//                        if (filePiexeRange == null) return new List<(TimeSpan start, TimeSpan end)>();

//                        int startIndex = filePiexeRange.StartPieceIndex;
//                        int endIndex = filePiexeRange.EndPieceIndex;

//                        if (torrentPieces != null && torrentPieces.Count > 0)
//                        {
//                            List<bool> filePieces = new List<bool>();
//                            for (int i = 0; i <= torrentPieces.Count - 1; i++)
//                            {
//                                if (i >= startIndex && i <= endIndex)
//                                {
//                                    filePieces.Add(torrentPieces[i]);
//                                }
//                            }

//                            double bytesPerSecond = (double)totalFileSize / totalDuration;

//                            // Parça başına süre
//                            double secondsPerPiece = pieceSize / bytesPerSecond;

//                            for (int i = 0; i <= filePieces.Count - 1; i++)
//                            {
//                                var currentState = filePieces[i];
//                                if (currentState)
//                                {
//                                    var pieceIndex = i;
//                                    double startTime = pieceIndex * secondsPerPiece;
//                                    double endTime = (pieceIndex + 1) * secondsPerPiece;

//                                    TimeSpan start = TimeSpan.FromSeconds(startTime);
//                                    TimeSpan end = TimeSpan.FromSeconds(endTime);

//                                    pieceTimeRanges.Add((start, end));
//                                }
//                            }
//                        }
//                        else
//                        {
//                            Log.Information("Torrent pieces states null");
//                        }
//                    }
//                    else
//                    {
//                        Log.Information("currentFile null");
//                    }
//                }
//                catch (Exception e)
//                {
//                    Console.WriteLine(e.Message);
//                    Log.Error("Pieces error : " + e.Message);
//                }
//            }

            
//            return pieceTimeRanges;
//        }
//        public static TorrentStatusInfo GetStatus(string hash)
//        {
//            try
//            {
//                TorrentStatusInfos.TryGetValue(hash, out TorrentStatusInfo status);
//                return status;
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on get status: " +e.Message);
//                return null;
//            }
//        }
//        public static string TextAfter(string value, string search)
//        {
//            return value.Substring(value.IndexOf(search) + search.Length);
//        }

//        public static async void CancelDownloads(Item item)
//        {
//            try
//            {
//                var hash = item.Hash; 

//                var keysToRemove = DownloadsPageQ._cancellationTokenSources.Keys
//                    .Where(key => string.Equals(key, hash, StringComparison.OrdinalIgnoreCase))
//                    .ToList();

//                foreach (var key in keysToRemove)
//                {
//                    if (DownloadsPageQ._cancellationTokenSources.TryGetValue(key, out var cts))
//                    {
//                        cts.Cancel();
//                        cts.Dispose(); 
//                        DownloadsPageQ._cancellationTokenSources.Remove(key);
//                    }
//                }

//                var prioritiesToRemove = TorrentPiecePriorities
//                    .Where(x => string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase))
//                    .ToList();

//                foreach (var priority in prioritiesToRemove)
//                {
//                    priority.CancellationTokenSource.Cancel();
//                    priority.CancellationTokenSource.Dispose(); 
//                    TorrentPiecePriorities.Remove(priority);
//                }
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e.Message);
//                Log.Error(e.Message);
//            }
//        }

//        public static async void CancelAll()
//        {
//            try
//            {
//                var keysToRemove = DownloadsPageQ._cancellationTokenSources.Keys
//                    .ToList();

//                foreach (var key in keysToRemove)
//                {
//                    if (DownloadsPageQ._cancellationTokenSources.TryGetValue(key, out var cts))
//                    {
//                        cts.Cancel();
//                        cts.Dispose();
//                        DownloadsPageQ._cancellationTokenSources.Remove(key);
//                    }
//                }

//                var prioritiesToRemove = TorrentPiecePriorities
//                    .ToList();

//                foreach (var priority in prioritiesToRemove)
//                {
//                    priority.CancellationTokenSource.Cancel();
//                    priority.CancellationTokenSource.Dispose();
//                    TorrentPiecePriorities.Remove(priority);
//                }
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e.Message);
//                Log.Error(e.Message);
//            }
//        }

//        public static async Task Pause(Item item)
//        {
//            try
//            {
//                TorrentHandleWrapper torrent = await GetTorrentHandle(item);
//                if (torrent != null)
//                {
//                    torrent.Pause();
//                    CancelDownloads(item);
//                }
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on pause: " +e.Message);
//            }
//        }

//        public static async Task Resume(Item item)
//        {
//            try
//            {
//                TorrentHandleWrapper torrent = await GetTorrentHandle(item);

//                if (torrent != null)
//                {
//                    torrent.Resume();
//                    var cancellationTokenSource = new CancellationTokenSource();
//                    DownloadsPageQ._cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
//                    Task.Run(() => DownloadFileSequentially(item, cancellationTokenSource.Token));
//                }
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on resume: " +e.Message);
//            }
//        }

//        public static async Task Delete(Item item)
//        {
//            try
//            {
//                TorrentHandleWrapper torrent = await GetTorrentHandle(item);

//                await Task.Run((() => torrent.Pause()));
//                CancelDownloads(item);

//                if (torrent != null)
//                {
//                    _=Task.Run((() =>
//                    {
//                        torrent.Delete();
//                    }));
//                }
//            }
//            catch (Exception e)
//            {
//                Log.Error("Error on delete: " +e.Message);
//            }
//        }

//        public static async Task<string> AddTorrentFromFile(string path)
//        {
//            return client.AddTorrentFromFile(path,AppSettingsManager.appSettings.MoviesPath);
//        }

//        public static async Task<bool> IsTorrentExistPath(string path)
//        {
//            try
//            {
//                var parser = new BencodeParser();
//                var torrent = parser.Parse<BencodeNET.Torrents.Torrent>(path);
//                var t = await GetTorrentHandle(torrent.OriginalInfoHash);
//                return t != null;
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e.Message);
//                Log.Error(e.Message);
//            }

//            return false;
//        }

//        public static async Task<bool> IsTorrentExistUrl(string url)
//        {
//            try
//            {
//                var text = url;
//                var search = "magnet:?xt=urn:btih:";
//                var textAfter = TextAfter(text, search);
//                string hash = textAfter.Substring(0, textAfter.IndexOf('&'));
//                var t = await GetTorrentHandle(hash);
//                return t != null;
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e.Message);
//                Log.Error(e.Message);
//            }

//            return false;
//        }

//        public static async Task<string> AddTorrentFromMagnet(string url)
//        {
//            if (url.StartsWith("magnet:"))
//            {
//                return client.AddTorrentFromMagnet(url, AppSettingsManager.appSettings.MoviesPath);
//            }
//            else
//            {
//                string redirectedUrl = await GetRedirectedUrl(url);
//                if (redirectedUrl != "")
//                {
//                    return client.AddTorrentFromMagnet(redirectedUrl, AppSettingsManager.appSettings.MoviesPath);
//                }
//                else
//                {
//                    return "";
//                }
//            }
//        }

//        static async Task<string> GetRedirectedUrl(string url)
//        {
//            try
//            {
//                using (HttpClientHandler handler = new HttpClientHandler { AllowAutoRedirect = false })
//                using (HttpClient client = new HttpClient(handler))
//                {
//                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
//                    HttpResponseMessage response = await client.SendAsync(request);

//                    if (response.StatusCode == HttpStatusCode.Found ||
//                        response.StatusCode == HttpStatusCode.MovedPermanently)
//                    {
//                        // Redirect edilen URL'yi döndür
//                        return response.Headers.Location.ToString();
//                    }
//                    else if (response.IsSuccessStatusCode)
//                    {
//                        // Eğer yönlendirme yoksa, yanıt içeriğini döndür
//                        return await response.Content.ReadAsStringAsync();
//                    }
//                    else
//                    {
//                        throw new Exception($"HTTP isteği başarısız: {response.StatusCode}");
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Log.Error(e.Message);
//                return "";
//            }
//        }

//        private static DateTime _lastExecutionTime = DateTime.MinValue;
//        private static readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

//        public static Dictionary<string, TorrentHandleWrapper> torrentHandleWrappers =
//            new Dictionary<string, TorrentHandleWrapper>();
//        private static ConcurrentDictionary<string, TorrentStatusInfo> TorrentStatusInfos =
//            new ConcurrentDictionary<string, TorrentStatusInfo>();
//        public static void UpdateStatus(TorrentStatusInfo status)
//        {
//            // AddOrUpdate metodu; eğer key yoksa ekler, varsa günceller.
//            TorrentStatusInfos.AddOrUpdate(
//                status.Hash,
//                status,
//                (key, existing) =>
//                {
//                    existing.Progress = status.Progress;
//                    existing.EstimatedTime = status.EstimatedTime;
//                    existing.DownloadSpeedString = status.DownloadSpeedString;
//                    existing.Pieces = status.Pieces;
//                    return existing;
//                });
//        }
//        public static async Task ObserveChanges(Item item)
//        {
//            try
//            {
//                TorrentHandleWrapper currentTorrent = await GetTorrentHandle(item);
           
//                if (currentTorrent == null)
//                {
//                    if ((DateTime.Now - _lastExecutionTime) < _interval)
//                        return;

//                    _lastExecutionTime = DateTime.Now;
//                    await DownloadsPageQ.GetDownloadsPageInstance.StartTorrenting2(item);

//                    return;
//                }

//                if (!currentTorrent.IsValid)
//                {
//                    torrentHandleWrappers.Remove(currentTorrent.Hash);
//                    Torrents.Remove(currentTorrent);
//                    currentTorrent = await GetTorrentHandle(item.Hash);
//                }

//                var status = await Task.Run((() => currentTorrent.GetStatus()));
//                UpdateStatus(status);
//                item.DownloadPercent = status.Progress * 100;
//                item.DownloadSpeed = status.Progress >= 1 ? "": App.Current.Resources["DownloadSpeedString"] + ": " + status.DownloadSpeedString;
//                item.IsCompleted = status.Progress >= 1;
            
//                if (item.IsCompleted)
//                    item.Eta = App.Current.Resources["CompletedString"].ToString();
//                else
//                    item.Eta = App.Current.Resources["EtaString"] + ": " + status.EstimatedTime;
//            }
//            catch (Exception e)
//            {
//                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
//                Log.Error(errorMessage);
//            }
//        }

//        public static List<string> Regexes
//        {
//            get
//            {
//                List<string> regexes = new List<string>();
//                regexes.Add("[sS][0-9]+[eE][0-9]+-*[eE]*[0-9]*");
//                regexes.Add("[0-9]+[xX][0-9]+");

//                return regexes;
//            }
//        }

//        private static int GetSeasonNumberFromFileName(string fileName)
//        {
//            foreach (string regex in Regexes)
//            {
//                Match match = Regex.Match(fileName, regex);
//                if (match.Success)
//                {
//                    string matched = match.Value.ToLower();
//                    if (regex.Contains("e")) //SDDEDD
//                    {
//                        matched = matched.Replace("s", "");
//                        matched = matched.Substring(0, matched.IndexOf("e"));
//                        return int.Parse(matched);
//                    }
//                    else if (regex.Contains("x")) //DDXDD
//                    {
//                        matched = matched.Substring(0, matched.IndexOf("x"));
//                        return int.Parse(matched);
//                    }
//                }
//            }

//            return -2;
//        }
//        private static int GetEpisodeNumberFromFileName(string fileName)
//        {
//            foreach (string regex in Regexes)
//            {
//                Match match = Regex.Match(fileName, regex);
//                if (match.Success)
//                {
//                    string matched = match.Value.ToLower();
//                    if (regex.Contains("e")) //SDDEDD
//                    {
//                        matched = matched.Substring(matched.IndexOf("e") + 1);

//                        if (matched.Contains("e") || matched.Contains("-"))
//                        {
//                            matched = matched.Substring(0, matched.IndexOf(matched.Contains("e") ? "e" : "-")).Replace("-", "");
//                        }

//                        return int.Parse(matched);
//                    }
//                    else if (regex.Contains("x")) //DDXDD
//                    {
//                        matched = matched.Substring(matched.IndexOf("x") + 1);
//                        return int.Parse(matched);
//                    }
//                }
//            }

//            return -2;
//        }

       
//        const int checkInterval = 500;
//        const int bufferPieces = 3;
//        const int batchSize = 50;
//        public static async Task DownloadFileSequentially(Item item, CancellationToken cancellationToken)
//        {
//            TorrentHandleWrapper torrent = await GetTorrentHandle(item);
//            try
//            {
//                using var _ = cancellationToken.Register(() =>
//                    Console.WriteLine("İndirme işlemi iptal ediliyor..."));


                
//                if (torrent == null)
//                {
//                    Console.WriteLine("Torrent was null.");
//                    return;
//                }

//                var mediaFiles = (await GetFiles(item.Hash))
//                    .Where(x => x.IsMediaFile);

//                if (mediaFiles.Count() > 1)
//                {
//                    await Task.Run((() => ChangeTvShowEpisodeFilePrioritiesNew(torrent, cancellationToken)));
//                }
//                else
//                {
//                    var mediaFile = mediaFiles.First();

//                    var (startIndex, endIndex) = await GetValidPieceRange(torrent, mediaFile.Index);
//                    if (startIndex > endIndex) return;

//                    await WaitForValidPieces(torrent, cancellationToken);

//                    torrent.ClearPieceDeadLines();
//                    torrent.ClearPiecePrioritiesExceptFile(mediaFile.Index);

//                    // Corrected buffer ranges (first 3 and last 3)
//                    int firstBufferEnd = Math.Min(startIndex + bufferPieces - 1, endIndex);
//                    int lastBufferStart = Math.Max(endIndex - bufferPieces + 1, startIndex);

//                    // Download buffers in parallel
//                    await Task.WhenAll(
//                        DownloadPieceRange(torrent, startIndex, firstBufferEnd, lastBufferStart, endIndex, checkInterval, cancellationToken)
//                    );

//                    // Calculate remaining pieces
//                    int middleStart = firstBufferEnd + 1;
//                    int middleEnd = lastBufferStart - 1;

//                    // Download remaining pieces in batches
//                    if (middleStart <= middleEnd)
//                    {
//                        for (int batchStart = middleStart; batchStart <= middleEnd; batchStart += batchSize)
//                        {
//                            int batchEnd = Math.Min(batchStart + batchSize - 1, middleEnd);
//                            await DownloadPieceBatch(torrent, batchStart, batchEnd, checkInterval, cancellationToken);
//                        }
//                    }

//                    torrent.Dispose();
//                    torrentHandleWrappers.Remove(torrent.Hash);
//                    Torrents.Remove(torrent);
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                Console.WriteLine("İndirme başarıyla iptal edildi.");
//                torrent.Dispose();
//                torrentHandleWrappers.Remove(torrent.Hash);
//                Torrents.Remove(torrent);
//            }
//            catch (Exception e)
//            {
//                Log.Error($"Error: {e.Message}\nStack Trace: {e.StackTrace}");
//            }
//        }

//        private static async Task DownloadPieceRange(
//            TorrentHandleWrapper torrent,
//            int start,
//            int end,
//            int start2,
//            int end2,
//            int checkInterval,
//            CancellationToken ct)
//        {
//            var piecesToDownload = new List<int>();
//            var currentPieces = GetStatus(torrent.Hash).Pieces.ToList();

//            for (int i = start; i <= end; i++)
//            {
//                if (i < currentPieces.Count && !currentPieces[i])
//                {
//                    piecesToDownload.Add(i);
//                }
//            }

//            for (int i = start2; i <= end2; i++)
//            {
//                if (i < currentPieces.Count && !currentPieces[i])
//                {
//                    piecesToDownload.Add(i);
//                }
//            }

//            int count = 1;
//            foreach (int piece in piecesToDownload)
//            {
//                ct.ThrowIfCancellationRequested();
//                torrent.QueuePriorityUpdate(piece, 7, count * 50);
//                Console.WriteLine($"Piece: {piece}: Priority: Top, Deadline: {count * 50}");
//                count++;
//            }

//            await Task.Run((() =>
//            {
//                torrent.FlushPriorityUpdates();
//            }));
            
//            while (!ct.IsCancellationRequested && piecesToDownload.Count > 0)
//            {
//                currentPieces = GetStatus(torrent.Hash).Pieces.ToList();
//                var remainingPieces = piecesToDownload.Where(p => !currentPieces[p]).ToList();
//                if (remainingPieces.Count == 0)
//                {
//                    break;
//                }
//                else
//                {
//                    count = 1;
//                    foreach (int piece in piecesToDownload)
//                    {
//                        ct.ThrowIfCancellationRequested();
//                        torrent.QueuePriorityUpdate(piece, 7, count * 50);
//                        count++;
//                    }
//                    await Task.Run((() =>
//                    {
//                        torrent.FlushPriorityUpdates();
//                    }));
//                }

//                await Task.Delay(checkInterval, ct);
//            }
//        }

//        private static async Task DownloadPieceBatch(
//            TorrentHandleWrapper torrent,
//            int start,
//            int end,
//            int checkInterval,
//            CancellationToken ct)
//        {
//            var piecesToDownload = new List<int>();
//            var currentPieces = GetStatus(torrent.Hash).Pieces.ToList();

//            // Identify pieces that need downloading
//            for (int i = start; i <= end; i++)
//            {
//                if (i < currentPieces.Count && !currentPieces[i])
//                {
//                    piecesToDownload.Add(i);
//                }
//            }

//            if (piecesToDownload.Count == 0)
//                return;

//            // Set initial priorities and deadlines
//            int count = 1;
//            foreach (int piece in piecesToDownload)
//            {
//                torrent.QueuePriorityUpdate(piece, 7, count * 100);
//                Console.WriteLine($"Piece: {piece}: Priority: Top, Deadline: {count * 100}");
//                count++;
//            }
//            await Task.Run((() =>
//            {
//                torrent.FlushPriorityUpdates();
//            }));


//            while (!ct.IsCancellationRequested)
//            {
//                currentPieces = GetStatus(torrent.Hash).Pieces.ToList();
//                var remainingPieces = piecesToDownload.Where(p => !currentPieces[p]).ToList();

//                if (remainingPieces.Count == 0)
//                {
//                    break;
//                }
//                else
//                {
//                    count = 1;
//                    foreach (int piece in piecesToDownload)
//                    {
//                        torrent.QueuePriorityUpdate(piece, 7, count * 100);
//                        count++;
//                    }
//                    await Task.Run((() =>
//                    {
//                        torrent.FlushPriorityUpdates();
//                    }));
//                }

//                await Task.Delay(checkInterval, ct);
//            }

//            // Reset priorities
//            foreach (int piece in piecesToDownload)
//            {
//                torrent.QueuePriorityUpdate(piece, 4, 0);
//            }
//            torrent.FlushPriorityUpdates();
//        }

//        // Keep existing helper methods (GetValidPieceRange, WaitForValidPieces) unchanged

//        private static async Task<(int Start, int End)> GetValidPieceRange(TorrentHandleWrapper torrent, int fileIndex)
//        {
//            var indexRange = await Task.Run(() => torrent.GetFilePieceRange(fileIndex));
//            while (indexRange == null)
//            {
//                await Task.Delay(100);
//                indexRange = await Task.Run(() => torrent.GetFilePieceRange(fileIndex));
//            }
//            return (indexRange.StartPieceIndex, indexRange.EndPieceIndex);
//        }

//        private static async Task<List<bool>> WaitForValidPieces(TorrentHandleWrapper torrent, CancellationToken ct)
//        {
//            while (!ct.IsCancellationRequested)
//            {
//                var status = await Task.Run((() => torrent.GetStatus()));
//                UpdateStatus(status);
//                var pieces = status.Pieces.ToList();
//                if (pieces.Count > 0) return pieces.ToList();
//                await Task.Delay(100, ct);
//            }
//            return new List<bool>();
//        }

        
//    }

//    public class TorrentPiece
//    {
//        public int Index { get; set; }
//        public TorrentPieceState TorrentPieceState { get; set; }
//    }

//    public class AsyncRateLimiter
//    {
//        private DateTime _lastExecutionTime = DateTime.MinValue;
//        private readonly TimeSpan _interval;
//        private readonly object _lock = new object();

//        public AsyncRateLimiter(TimeSpan interval)
//        {
//            _interval = interval;
//        }

//        public async Task ExecuteAsync(Func<Task> action)
//        {
//            lock (_lock)
//            {
//                if ((DateTime.Now - _lastExecutionTime) < _interval)
//                    return;

//                _lastExecutionTime = DateTime.Now;
//            }

//            await action();
//        }

//        public async Task ExecuteAsync<T>(Func<T, Task> action, T parameter)
//        {
//            lock (_lock)
//            {
//                if ((DateTime.Now - _lastExecutionTime) < _interval)
//                    return;

//                _lastExecutionTime = DateTime.Now;
//            }

//            await action(parameter);
//        }
//    }


//}


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using BencodeNET.Parsing;
using Windows.Media.Protection.PlayReady;
using Serilog;
using YoutubeExplode.Channels;
using DynamicData;
using NetStream.Views;
using Polly;
using Polly.Timeout;
using Path = System.IO.Path;
using BencodeNET.Torrents;
using TorrentWrapper;
using System.Reflection.Metadata;
using System.Threading;
using System.Net.Http;
using System.Xml;
using OpenCvSharp.ImgHash;
using Microsoft.Win32;

namespace NetStream
{
    public class TorrentPiecePriority
    {
        public string Hash { get; set; }
        public int FileIndex { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }
    public class Libtorrent
    {
        public static Client client;
        public static SessionManager sessionManager;
        // Thread-safe koleksiyonlar için uygun implementasyonlar
        private static readonly object _torrentPiecePrioritiesLock = new object();
        private static readonly object _torrentsLock = new object();
        private static readonly object _torrentHandleWrappersLock = new object();

        public static async Task Initialize()
        {
            try
            {
                client = new Client("lG!o0)%]?M85Q`57FZqzqf4U|t1@@"); 
                sessionManager = new SessionManager();
                _ = Task.Run((async () =>
                {
                    try 
                    {
                        sessionManager.StartListeningAlerts();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error in alert listening: " + ex.Message);
                    }
                }));
            }
            catch (Exception e)
            {
                new CustomMessageBox("Initialization libtorrent failed. Please check your api settings",
                    MessageType.Error, MessageButtons.Ok).ShowDialog();
                Log.Error("Initialization libtorrent failed: " + e.Message);
            }
        }

        public static TorrentFile? currentEpisodeFile;
        public static int lastEpisodeIndex = -1;
        public static List<TorrentPiecePriority> TorrentPiecePriorities = new List<TorrentPiecePriority>();
        
        public static async Task ChangeTvShowEpisodeFilePrioritiesNew(TorrentHandleWrapper torrent, CancellationToken token)
        {
            if (torrent == null)
            {
                Log.Warning("Torrent handle is null in ChangeTvShowEpisodeFilePrioritiesNew");
                return;
            }

            try
            {
                var files = await GetFiles(torrent.Hash);
                if (files == null || files.Count == 0)
                {
                    Log.Warning("No files found for torrent hash: " + torrent.Hash);
                    return;
                }

                var episodeFiles = files.Where(x => x != null && x.IsMediaFile && !x.IsCompleted && GetSeasonNumberFromFileName(x.Name) != -2
                                                    && GetEpisodeNumberFromFileName(x.Name) != -2)
                    .OrderBy(x => GetSeasonNumberFromFileName(x.Name))
                    .ThenBy(x => GetEpisodeNumberFromFileName(x.Name)).ToList();

                while (!token.IsCancellationRequested && episodeFiles.Count != 0)
                {
                    if (currentEpisodeFile == null)
                    {
                        if (episodeFiles.Count > 0 && !IsPrioritySet(torrent, episodeFiles[0].Index))
                        {
                            await SetPriorityForFile(torrent, episodeFiles[0].Index);
                        }
                    }
                    else
                    {
                        var c = episodeFiles.FirstOrDefault(x => x.Index == currentEpisodeFile.Index);
                        if (c == null)
                        {
                            if (episodeFiles.Count > 0 && !IsPrioritySet(torrent, episodeFiles[0].Index))
                            {
                                await SetPriorityForFile(torrent, episodeFiles[0].Index);
                            }
                        }
                        else
                        {
                            if (!IsPrioritySet(torrent, currentEpisodeFile.Index))
                            {
                                await SetPriorityForFile(torrent, currentEpisodeFile.Index);
                            }
                        }
                    }

                    await Task.Delay(1000, token); // Prevent CPU spike in the loop
                    
                    files = await GetFiles(torrent.Hash);
                    if (files == null || files.Count == 0) break;
                    
                    episodeFiles = files.Where(x => x != null && x.IsMediaFile && !x.IsCompleted && GetSeasonNumberFromFileName(x.Name) != -2
                                                    && GetEpisodeNumberFromFileName(x.Name) != -2)
                        .OrderBy(x => GetSeasonNumberFromFileName(x.Name))
                        .ThenBy(x => GetEpisodeNumberFromFileName(x.Name)).ToList();
                }

                SafeRemoveTorrent(torrent);
                Console.WriteLine("İndirme işlemi bitti.");
            }
            catch (OperationCanceledException)
            {
                Log.Information("Operation was canceled in ChangeTvShowEpisodeFilePrioritiesNew for hash: " + torrent.Hash);
            }
            catch (Exception e)
            {
                Log.Error("Error on Priorities: " + e.Message);
            }
        }

        private static bool IsPrioritySet(TorrentHandleWrapper torrent, int fileIndex)
        {
            lock (_torrentPiecePrioritiesLock)
            {
                return TorrentPiecePriorities.Any(x => 
                    x.Hash != null && torrent.Hash != null && 
                    x.Hash.Equals(torrent.Hash, StringComparison.OrdinalIgnoreCase) && 
                    x.FileIndex == fileIndex);
            }
        }

        private static async Task SetPriorityForFile(TorrentHandleWrapper torrent, int fileIndex)
        {
            CancelAllPreviousPriorities(torrent);
            
            var torrentPiecePriority = new TorrentPiecePriority
            {
                Hash = torrent.Hash,
                FileIndex = fileIndex,
                CancellationTokenSource = new CancellationTokenSource()
            };
            
            lock (_torrentPiecePrioritiesLock)
            {
                TorrentPiecePriorities.Add(torrentPiecePriority);
            }
            
            await Task.Run(() => DownloadFileSequentially(torrent, fileIndex, torrentPiecePriority.CancellationTokenSource.Token));
        }

        public static void CancelAllPreviousPriorities(TorrentHandleWrapper torrent)
        {
            if (torrent == null)
            {
                Log.Warning("Torrent is null in CancelAllPreviousPriorities");
                return;
            }

            try
            {
                List<TorrentPiecePriority> toCancel;
                
                lock (_torrentPiecePrioritiesLock)
                {
                    toCancel = TorrentPiecePriorities
                        .Where(x => x != null && x.Hash != null && torrent.Hash != null && 
                               x.Hash.Equals(torrent.Hash, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                foreach (var torrentPiecePriority in toCancel)
                {
                    try
                    {
                        if (torrentPiecePriority.CancellationTokenSource != null && 
                            !torrentPiecePriority.CancellationTokenSource.IsCancellationRequested)
                        {
                            torrentPiecePriority.CancellationTokenSource.Cancel();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error cancelling token: " + ex.Message);
                    }
                }

                // Dispose in a separate loop to ensure all cancellations happen first
                foreach (var torrentPiecePriority in toCancel)
                {
                    try
                    {
                        torrentPiecePriority.CancellationTokenSource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error disposing token: " + ex.Message);
                    }
                }

                lock (_torrentPiecePrioritiesLock)
                {
                    TorrentPiecePriorities.RemoveAll(x => x != null && x.Hash != null && torrent.Hash != null && 
                                                    x.Hash.Equals(torrent.Hash, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on CancelAllPrios: " + e.Message);
            }
        }

        public static async Task DownloadFileSequentially(TorrentHandleWrapper torrent, int fileIndex, CancellationToken cancellationToken)
        {
            if (torrent == null)
            {
                Log.Warning("Torrent is null in DownloadFileSequentially");
                return;
            }

            try
            {
                using var _ = cancellationToken.Register(() =>
                    Console.WriteLine("İndirme işlemi iptal ediliyor..."));

                var files = await GetFiles(torrent.Hash);
                if (files == null || files.Count == 0)
                {
                    Log.Warning("No files found for torrent hash: " + torrent.Hash);
                    return;
                }

                var mediaFile = files.FirstOrDefault(x => x.Index == fileIndex);

                if (mediaFile == null)
                {
                    Log.Warning($"Media file with index {fileIndex} not found");
                    return;
                }

                Console.WriteLine(mediaFile.Name + " İndiriliyor...");
                
                var pieceRange = await GetValidPieceRange(torrent, mediaFile.Index);
                int startIndex = pieceRange.Start;
                int endIndex = pieceRange.End;
                
                if (startIndex > endIndex || startIndex < 0)
                {
                    Log.Warning($"Invalid piece range: {startIndex} to {endIndex}");
                    return;
                }

                List<bool> pieces = await WaitForValidPieces(torrent, cancellationToken);
                if (pieces == null || pieces.Count == 0)
                {
                    Log.Warning("No valid pieces found");
                    return;
                }

                torrent.ClearPiecePrioritiesExceptFile(fileIndex);
                torrent.ClearPieceDeadLines();

                // Corrected buffer ranges (first 3 and last 3)
                int firstBufferEnd = Math.Min(startIndex + bufferPieces - 1, endIndex);
                int lastBufferStart = Math.Max(endIndex - bufferPieces + 1, startIndex);

                // Download buffers in parallel
                try
                {
                    await Task.WhenAll(
                        DownloadPieceRange(torrent, startIndex, firstBufferEnd, lastBufferStart, endIndex, checkInterval, cancellationToken)
                    );
                }
                catch (OperationCanceledException)
                {
                    throw; // Let the outer catch handle cancellation
                }
                catch (Exception ex)
                {
                    Log.Error($"Error downloading piece ranges: {ex.Message}");
                }

                // Calculate remaining pieces
                int middleStart = firstBufferEnd + 1;
                int middleEnd = lastBufferStart - 1;

                // Download remaining pieces in batches
                if (middleStart <= middleEnd)
                {
                    for (int batchStart = middleStart; batchStart <= middleEnd && !cancellationToken.IsCancellationRequested; batchStart += batchSize)
                    {
                        int batchEnd = Math.Min(batchStart + batchSize - 1, middleEnd);
                        await DownloadPieceBatch(torrent, batchStart, batchEnd, checkInterval, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("İndirme başarıyla iptal edildi.");
            }
            catch (Exception e)
            {
                Log.Error($"Error on download file: {e.Message}");
            }
        }

        public static async Task ChangeEpisodeFileToMaximalPriority(Item item, int season, int episode)
        {
            if (item == null)
            {
                Log.Warning("Item is null in ChangeEpisodeFileToMaximalPriority");
                return;
            }

            try
            {
                var files = await GetFiles(item);
                
                // Safe null check
                if (files == null || files.Count == 0)
                {
                    Log.Warning("No files found for item: " + item.MovieName);
                    return;
                }
                
                currentEpisodeFile = files.FirstOrDefault(x => GetSeasonNumberFromFileName(x.Name) == season &&
                                                          GetEpisodeNumberFromFileName(x.Name) == episode);
                lastEpisodeIndex = currentEpisodeFile != null ? currentEpisodeFile.Index : -1;
                
                var torrent = await GetTorrentHandle(item);
                if (torrent != null)
                {
                    CancelAllPreviousPriorities(torrent);
                }
                else
                {
                    Log.Warning($"Could not get torrent handle for item: {item.MovieName}");
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on changing priority: " + e.Message);
            }
        }

        public static async Task ChangeMovieCollectionFilePriorityToMaximal(Item item, int fileIndex)
        {
            if (item == null || fileIndex < 0)
            {
                Log.Warning("Invalid parameters in ChangeMovieCollectionFilePriorityToMaximal");
                return;
            }

            try
            {
                //TODO
                //await DownloadFileSequentially(item.Hash, fileIndex);
                Log.Information($"ChangeMovieCollectionFilePriorityToMaximal called for item {item.MovieName}, fileIndex {fileIndex}");
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }


        public static async Task<TorrentState> GetTorrentState(Item item)
        {
            try
            {
                var torrent = await GetTorrentHandle(item);
                if (torrent != null)
                {
                    return torrent.GetTorrentState;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
            return TorrentState.Unknown;
        }

        public static async Task<TorrentHandleWrapper> GetTorrentHandle(Item item)
        {
            try
            {
                string hash = item.Hash;
                if (torrentHandleWrappers.ContainsKey(hash))
                {
                    return torrentHandleWrappers[hash];
                }
                else
                {
                    var torrent =
                        Torrents.FirstOrDefault(x => string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
                    if (torrent != null)
                    {
                        if (!torrentHandleWrappers.ContainsKey(hash))
                            torrentHandleWrappers.Add(hash, torrent);
                        return torrent;
                    }
                    else
                    {
                        Torrents = await client.GetTorrentsAsync();
                    }
                    torrent = Torrents.FirstOrDefault(x => string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
                    if (torrent != null)
                    {
                        if (!torrentHandleWrappers.ContainsKey(hash))
                            torrentHandleWrappers.Add(hash, torrent);
                        return torrent;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on torrent handle: "+e.Message);
                return null;
            }
        }

        public static List<TorrentHandleWrapper> Torrents = new List<TorrentHandleWrapper>();

        public static async Task<TorrentHandleWrapper> GetTorrentHandle(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                Log.Warning("Hash is null or empty in GetTorrentHandle");
                return null;
            }

            try
            {
                lock (_torrentHandleWrappersLock)
                {
                    if (torrentHandleWrappers.ContainsKey(hash))
                    {
                        return torrentHandleWrappers[hash];
                    }
                }
                
                lock (_torrentsLock)
                {
                    var torrent = Torrents.FirstOrDefault(x => string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
                    if (torrent != null)
                    {
                        lock (_torrentHandleWrappersLock)
                        {
                            if (!torrentHandleWrappers.ContainsKey(hash))
                                torrentHandleWrappers.Add(hash, torrent);
                        }
                        return torrent;
                    }
                }

                // If we reach here, we need to refresh the torrents list
                var updatedTorrents = await client.GetTorrentsAsync();
                if (updatedTorrents != null)
                {
                    lock (_torrentsLock)
                    {
                        Torrents = updatedTorrents;
                    }
                    
                    var torrent = updatedTorrents.FirstOrDefault(x => string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
                    if (torrent != null)
                    {
                        lock (_torrentHandleWrappersLock)
                        {
                            if(!torrentHandleWrappers.ContainsKey(hash))
                                torrentHandleWrappers.Add(hash, torrent);
                        }
                        return torrent;
                    }
                }
                
                return null;
            }
            catch (Exception e)
            {
                Log.Error("Error on torrent handle: " + e.Message);
                return null;
            }
        }

        public static async Task<List<TorrentFile>> GetFiles(Item item)
        {
            if (item == null)
            {
                Log.Warning("Item is null in GetFiles");
                return new List<TorrentFile>();
            }

            try
            {
                TorrentHandleWrapper torrent = await GetTorrentHandle(item);
                if (torrent != null)
                {
                    try
                    {
                        var content = await Task.Run(() => torrent.GetFiles());
                        
                        // Retry if content is empty
                        int retryCount = 0;
                        while (content.Count == 0 && retryCount < 5)
                        {
                            await Task.Delay(200);
                            content = await Task.Run(() => torrent.GetFiles());
                            retryCount++;
                        }

                        return content.OrderBy(f => f.Index).ToList();
                    }
                    catch (Exception e)
                    {
                        Log.Error("Error on get files " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get files: " +e.Message);
            }
            return new List<TorrentFile>();
        }

        public static async Task<List<TorrentFile>> GetFiles(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                Log.Warning("Hash is null or empty in GetFiles");
                return new List<TorrentFile>();
            }

            try
            {
                TorrentHandleWrapper torrent = await GetTorrentHandle(hash);

                if (torrent != null)
                {
                    try
                    {
                        var content = await Task.Run(() => torrent.GetFiles());
                        
                        // Retry if content is empty
                        int retryCount = 0;
                        while (content.Count == 0 && retryCount < 5)
                        {
                            await Task.Delay(200);
                            content = await Task.Run(() => torrent.GetFiles());
                            retryCount++;
                        }

                        return content.OrderBy(f => f.Index).ToList();
                    }
                    catch (Exception e)
                    {
                        Log.Error("Error getting files: " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on get files: " +e.Message);
            }

            return new List<TorrentFile>();
        }

        public static async Task<List<(TimeSpan start, TimeSpan end)>> GetAvailaibleSeconds(string hash, int durationInSeconds, int fileIndex)
        {
            if (string.IsNullOrEmpty(hash) || durationInSeconds <= 0 || fileIndex < 0)
            {
                Log.Warning("Invalid parameters in GetAvailaibleSeconds");
                return new List<(TimeSpan start, TimeSpan end)>();
            }

            List<(TimeSpan start, TimeSpan end)> pieceTimeRanges = new List<(TimeSpan, TimeSpan)>();
            
            try
            {
                TorrentHandleWrapper torrent = await GetTorrentHandle(hash);
                if (torrent == null)
                {
                    Log.Warning("Torrent not found for hash: " + hash);
                    return pieceTimeRanges;
                }

                var content = await GetFiles(hash);
                if (content == null || content.Count == 0)
                {
                    Log.Warning("No files found for hash: " + hash);
                    return pieceTimeRanges;
                }
                
                var currentFile = content.FirstOrDefault(x => x.Index == fileIndex);
                if (currentFile == null)
                {
                    Log.Warning($"File with index {fileIndex} not found");
                    return pieceTimeRanges;
                }

                long totalFileSize = currentFile.Size;
                if (totalFileSize <= 0)
                {
                    Log.Warning("File size is invalid: " + totalFileSize);
                    return pieceTimeRanges;
                }

                int totalDuration = durationInSeconds;
                long pieceSize = torrent.PieceSize;
                
                var statusInfo = GetStatus(torrent.Hash);
                if (statusInfo == null || statusInfo.Pieces == null)
                {
                    Log.Warning("Status or pieces information is null");
                    return pieceTimeRanges;
                }
                
                var torrentPieces = statusInfo.Pieces.ToList();
                if (torrentPieces.Count == 0)
                {
                    Log.Warning("No piece information available");
                    return pieceTimeRanges;
                }

                var filePieceRange = await Task.Run(() => torrent.GetFilePieceRange(currentFile.Index));

                // Retry if file piece range is null
                int retryCount = 1;
                while (filePieceRange == null && retryCount < 10)
                {
                    filePieceRange = await Task.Run(() => torrent.GetFilePieceRange(currentFile.Index));
                    retryCount++;
                    await Task.Delay(100);
                }

                if (filePieceRange == null)
                {
                    Log.Warning("Could not get file piece range after multiple attempts");
                    return pieceTimeRanges;
                }

                int startIndex = filePieceRange.StartPieceIndex;
                int endIndex = filePieceRange.EndPieceIndex;

                if (startIndex < 0 || endIndex < startIndex || endIndex >= torrentPieces.Count)
                {
                    Log.Warning($"Invalid piece indices: start={startIndex}, end={endIndex}, total pieces={torrentPieces.Count}");
                    return pieceTimeRanges;
                }

                List<bool> filePieces = new List<bool>();
                for (int i = 0; i < torrentPieces.Count; i++)
                {
                    if (i >= startIndex && i <= endIndex)
                    {
                        filePieces.Add(torrentPieces[i]);
                    }
                }

                double bytesPerSecond = (double)totalFileSize / totalDuration;
                if (bytesPerSecond <= 0)
                {
                    Log.Warning("Invalid bytes per second: " + bytesPerSecond);
                    return pieceTimeRanges;
                }

                // Parça başına süre
                double secondsPerPiece = pieceSize / bytesPerSecond;

                for (int i = 0; i < filePieces.Count; i++)
                {
                    var currentState = filePieces[i];
                    if (currentState)
                    {
                        var pieceIndex = i;
                        double startTime = pieceIndex * secondsPerPiece;
                        double endTime = (pieceIndex + 1) * secondsPerPiece;

                        TimeSpan start = TimeSpan.FromSeconds(startTime);
                        TimeSpan end = TimeSpan.FromSeconds(endTime);

                        pieceTimeRanges.Add((start, end));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error in GetAvailaibleSeconds: " + e.Message);
            }

            return pieceTimeRanges;
        }
        
        public static TorrentStatusInfo GetStatus(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                Log.Warning("Hash is null or empty in GetStatus");
                return null;
            }

            try
            {
                TorrentStatusInfo status;
                TorrentStatusInfos.TryGetValue(hash, out status);
                return status;
            }
            catch (Exception e)
            {
                Log.Error("Error on get status: " + e.Message);
                return null;
            }
        }
        
        public static string TextAfter(string value, string search)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(search))
            {
                return string.Empty;
            }

            int index = value.IndexOf(search);
            if (index < 0)
            {
                return string.Empty;
            }

            return value.Substring(index + search.Length);
        }

        public static async void CancelDownloads(Item item)
        {
            if (item == null)
            {
                Log.Warning("Item is null in CancelDownloads");
                return;
            }

            try
            {
                var hash = item.Hash; 
                if (string.IsNullOrEmpty(hash))
                {
                    Log.Warning("Hash is null or empty");
                    return;
                }

                // Cancel download tokens
                var keysToRemove = DownloadsPageQ._cancellationTokenSources.Keys
                    .Where(key => string.Equals(key, hash, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    if (DownloadsPageQ._cancellationTokenSources.TryGetValue(key, out var cts) && cts != null)
                    {
                        try
                        {
                            if (!cts.IsCancellationRequested)
                            {
                                cts.Cancel();
                            }
                            cts.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error cancelling download token: " + ex.Message);
                        }
                        DownloadsPageQ._cancellationTokenSources.Remove(key);
                    }
                }

                // Cancel priority tokens
                List<TorrentPiecePriority> prioritiesToRemove;
                lock (_torrentPiecePrioritiesLock)
                {
                    prioritiesToRemove = TorrentPiecePriorities
                        .Where(x => x != null && x.Hash != null && 
                                string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                foreach (var priority in prioritiesToRemove)
                {
                    try
                    {
                        if (priority.CancellationTokenSource != null && !priority.CancellationTokenSource.IsCancellationRequested)
                        {
                            priority.CancellationTokenSource.Cancel();
                        }
                        priority.CancellationTokenSource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error cancelling priority token: " + ex.Message);
                    }
                }

                lock (_torrentPiecePrioritiesLock)
                {
                    TorrentPiecePriorities.RemoveAll(x => x != null && x.Hash != null && 
                                                   string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception e)
            {
                Log.Error("Error in CancelDownloads: " + e.Message);
            }
        }

        public static async void CancelAll()
        {
            try
            {
                // Cancel all downloads
                var keysToRemove = DownloadsPageQ._cancellationTokenSources.Keys.ToList();

                foreach (var key in keysToRemove)
                {
                    if (DownloadsPageQ._cancellationTokenSources.TryGetValue(key, out var cts) && cts != null)
                    {
                        try
                        {
                            if (!cts.IsCancellationRequested)
                            {
                                cts.Cancel();
                            }
                            cts.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error cancelling token: " + ex.Message);
                        }
                        DownloadsPageQ._cancellationTokenSources.Remove(key);
                    }
                }

                // Cancel all priorities
                List<TorrentPiecePriority> prioritiesToRemove;
                lock (_torrentPiecePrioritiesLock)
                {
                    prioritiesToRemove = TorrentPiecePriorities.ToList();
                }

                foreach (var priority in prioritiesToRemove)
                {
                    try
                    {
                        if (priority.CancellationTokenSource != null && !priority.CancellationTokenSource.IsCancellationRequested)
                        {
                            priority.CancellationTokenSource.Cancel();
                        }
                        priority.CancellationTokenSource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error cancelling token: " + ex.Message);
                    }
                }

                lock (_torrentPiecePrioritiesLock)
                {
                    TorrentPiecePriorities.Clear();
                }
            }
            catch (Exception e)
            {
                Log.Error("Error in CancelAll: " + e.Message);
            }
        }

        private static void SafeRemoveTorrent(TorrentHandleWrapper torrent)
        {
            if (torrent == null) return;
            
            try
            {
                lock (_torrentHandleWrappersLock)
                {
                    if (torrentHandleWrappers.ContainsKey(torrent.Hash))
                    {
                        torrentHandleWrappers.Remove(torrent.Hash);
                    }
                }
                
                lock (_torrentsLock)
                {
                    Torrents.Remove(torrent);
                }
                
                torrent.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"Error removing torrent: {ex.Message}");
            }
        }

        public static async Task Pause(Item item)
        {
            if (item == null)
            {
                Log.Warning("Item is null in Pause");
                return;
            }

            try
            {
                TorrentHandleWrapper torrent = await GetTorrentHandle(item);
                if (torrent != null)
                {
                    await Task.Run(() => torrent.Pause());
                    CancelDownloads(item);
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on pause: " + e.Message);
            }
        }

        public static async Task Resume(Item item)
        {
            if (item == null)
            {
                Log.Warning("Item is null in Resume");
                return;
            }

            try
            {
                TorrentHandleWrapper torrent = await GetTorrentHandle(item);

                if (torrent != null)
                {
                    await Task.Run(() => torrent.Resume());
                    var cancellationTokenSource = new CancellationTokenSource();
                    DownloadsPageQ._cancellationTokenSources[torrent.Hash] = cancellationTokenSource;
                    Task.Run(() => DownloadFileSequentially(item, cancellationTokenSource.Token));
                }
            }
            catch (Exception e)
            {
                Log.Error("Error on resume: " + e.Message);
            }
        }

        public static async Task Delete(Item item)
        {
            if (item == null)
            {
                Log.Warning("Item is null in Delete");
                return;
            }

            try
            {
                TorrentHandleWrapper torrent = await GetTorrentHandle(item);
                if (torrent == null) return;

                await Task.Run(() => torrent.Pause());
                CancelDownloads(item);

                await Task.Run(() => torrent.Delete(true,true));
                //SafeRemoveTorrent(torrent);
            }
            catch (Exception e)
            {
                Log.Error("Error on delete: " + e.Message);
            }
        }

        public static async Task<string> AddTorrentFromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Log.Warning("Invalid path in AddTorrentFromFile: " + path);
                return string.Empty;
            }

            try
            {
                return client.AddTorrentFromFile(path, AppSettingsManager.appSettings.MoviesPath);
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding torrent from file: {ex.Message}");
                return string.Empty;
            }
        }

        public static async Task<bool> IsTorrentExistPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                var parser = new BencodeParser();
                var torrent = parser.Parse<BencodeNET.Torrents.Torrent>(path);
                if (torrent == null || string.IsNullOrEmpty(torrent.OriginalInfoHash))
                {
                    return false;
                }
                var t = await GetTorrentHandle(torrent.OriginalInfoHash);
                return t != null;
            }
            catch (Exception e)
            {
                Log.Error("Error checking torrent existence: " + e.Message);
            }

            return false;
        }

        public static async Task<bool> IsTorrentExistUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            try
            {
                if (!url.StartsWith("magnet:?xt=urn:btih:"))
                {
                    return false;
                }
                
                var text = url;
                var search = "magnet:?xt=urn:btih:";
                var textAfter = TextAfter(text, search);
                
                if (string.IsNullOrEmpty(textAfter) || !textAfter.Contains('&'))
                {
                    return false;
                }
                
                string hash = textAfter.Substring(0, textAfter.IndexOf('&'));
                if (string.IsNullOrEmpty(hash))
                {
                    return false;
                }
                
                var t = await GetTorrentHandle(hash);
                return t != null;
            }
            catch (Exception e)
            {
                Log.Error("Error checking torrent URL existence: " + e.Message);
            }

            return false;
        }

        public static async Task<string> AddTorrentFromMagnet(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Log.Warning("URL is null or empty in AddTorrentFromMagnet");
                return string.Empty;
            }

            try
            {
                if (url.StartsWith("magnet:"))
                {
                    return client.AddTorrentFromMagnet(url, AppSettingsManager.appSettings.MoviesPath);
                }
                else
                {
                    string redirectedUrl = await GetRedirectedUrl(url);
                    if (!string.IsNullOrEmpty(redirectedUrl))
                    {
                        return client.AddTorrentFromMagnet(redirectedUrl, AppSettingsManager.appSettings.MoviesPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding torrent from magnet: {ex.Message}");
            }
            
            return string.Empty;
        }

        static async Task<string> GetRedirectedUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            try
            {
                using (HttpClientHandler handler = new HttpClientHandler { AllowAutoRedirect = false })
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(10); // Add timeout to prevent hanging
                    
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.Found ||
                        response.StatusCode == HttpStatusCode.MovedPermanently)
                    {
                        // Redirect edilen URL'yi döndür
                        return response.Headers.Location?.ToString() ?? string.Empty;
                    }
                    else if (response.IsSuccessStatusCode)
                    {
                        // Eğer yönlendirme yoksa, yanıt içeriğini döndür
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        Log.Warning($"HTTP request failed: {response.StatusCode}");
                        return string.Empty;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error getting redirected URL: " + e.Message);
                return string.Empty;
            }
        }

        private static DateTime _lastExecutionTime = DateTime.MinValue;
        private static readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

        public static Dictionary<string, TorrentHandleWrapper> torrentHandleWrappers =
            new Dictionary<string, TorrentHandleWrapper>();
        private static ConcurrentDictionary<string, TorrentStatusInfo> TorrentStatusInfos =
            new ConcurrentDictionary<string, TorrentStatusInfo>();
            
        public static void UpdateStatus(TorrentStatusInfo status)
        {
            if (status == null || string.IsNullOrEmpty(status.Hash))
            {
                Log.Warning("Invalid status in UpdateStatus");
                return;
            }

            try
            {
                // AddOrUpdate metodu; eğer key yoksa ekler, varsa günceller.
                TorrentStatusInfos.AddOrUpdate(
                    status.Hash,
                    status,
                    (key, existing) =>
                    {
                        if (existing == null) return status;
                        
                        existing.Progress = status.Progress;
                        existing.EstimatedTime = status.EstimatedTime;
                        existing.DownloadSpeedString = status.DownloadSpeedString;
                        existing.Pieces = status.Pieces;
                        return existing;
                    });
            }
            catch (Exception ex)
            {
                Log.Error($"Error updating status: {ex.Message}");
            }
        }
        
        public static async Task ObserveChanges(Item item)
        {
            if (item == null)
            {
                Log.Warning("Item is null in ObserveChanges");
                return;
            }

            try
            {
                TorrentHandleWrapper currentTorrent = await GetTorrentHandle(item);
           
                if (currentTorrent == null)
                {
                    if ((DateTime.Now - _lastExecutionTime) < _interval)
                        return;

                    _lastExecutionTime = DateTime.Now;
                    
                    // Try to start torrenting if torrent not found
                    if (DownloadsPageQ.GetDownloadsPageInstance != null)
                    {
                        await DownloadsPageQ.GetDownloadsPageInstance.StartTorrenting2(item);
                    }
                    else
                    {
                        Log.Warning("DownloadsPageQ instance is null");
                    }

                    return;
                }

                if (!currentTorrent.IsValid)
                {
                    SafeRemoveTorrent(currentTorrent);
                    currentTorrent = await GetTorrentHandle(item.Hash);
                    
                    if (currentTorrent == null)
                    {
                        Log.Warning("Could not get valid torrent handle");
                        return;
                    }
                }

                var status = await Task.Run(() => currentTorrent.GetStatus());
                if (status == null)
                {
                    Log.Warning("Status is null for torrent: " + item.Hash);
                    return;
                }
                
                UpdateStatus(status);
                
                item.DownloadPercent = status.Progress * 100;
                item.DownloadSpeed = status.Progress >= 1 ? "": App.Current.Resources["DownloadSpeedString"] + ": " + status.DownloadSpeedString;
                item.IsCompleted = status.Progress >= 1;
            
                if (item.IsCompleted)
                    item.Eta = App.Current.Resources["CompletedString"]?.ToString() ?? "Completed";
                else
                    item.Eta = App.Current.Resources["EtaString"] + ": " + status.EstimatedTime;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error in ObserveChanges: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
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

        private static int GetSeasonNumberFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return -2;
            }

            try
            {
                foreach (string regex in Regexes)
                {
                    Match match = Regex.Match(fileName, regex);
                    if (match.Success)
                    {
                        string matched = match.Value.ToLower();
                        if (regex.Contains("e")) //SDDEDD
                        {
                            matched = matched.Replace("s", "");
                            int eIndex = matched.IndexOf("e");
                            if (eIndex < 0) continue;
                            
                            matched = matched.Substring(0, eIndex);
                            if (int.TryParse(matched, out int seasonNumber))
                            {
                                return seasonNumber;
                            }
                        }
                        else if (regex.Contains("x")) //DDXDD
                        {
                            int xIndex = matched.IndexOf("x");
                            if (xIndex < 0) continue;
                            
                            matched = matched.Substring(0, xIndex);
                            if (int.TryParse(matched, out int seasonNumber))
                            {
                                return seasonNumber;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting season number: {ex.Message}");
            }

            return -2;
        }
        
        private static int GetEpisodeNumberFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return -2;
            }

            try
            {
                foreach (string regex in Regexes)
                {
                    Match match = Regex.Match(fileName, regex);
                    if (match.Success)
                    {
                        string matched = match.Value.ToLower();
                        if (regex.Contains("e")) //SDDEDD
                        {
                            int eIndex = matched.IndexOf("e");
                            if (eIndex < 0) continue;
                            
                            matched = matched.Substring(eIndex + 1);

                            if (matched.Contains("e") || matched.Contains("-"))
                            {
                                int secondIndex = matched.Contains("e") ? 
                                    matched.IndexOf("e") : matched.IndexOf("-");
                                    
                                if (secondIndex < 0) continue;
                                
                                matched = matched.Substring(0, secondIndex).Replace("-", "");
                            }

                            if (int.TryParse(matched, out int episodeNumber))
                            {
                                return episodeNumber;
                            }
                        }
                        else if (regex.Contains("x")) //DDXDD
                        {
                            int xIndex = matched.IndexOf("x");
                            if (xIndex < 0) continue;
                            
                            matched = matched.Substring(xIndex + 1);
                            if (int.TryParse(matched, out int episodeNumber))
                            {
                                return episodeNumber;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting episode number: {ex.Message}");
            }

            return -2;
        }

       
        const int checkInterval = 500;
        const int bufferPieces = 3;
        const int batchSize = 50;
        
        public static async Task DownloadFileSequentially(Item item, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                Log.Warning("Item is null in DownloadFileSequentially");
                return;
            }

            TorrentHandleWrapper torrent = null;
            
            try
            {
                torrent = await GetTorrentHandle(item);
                if (torrent == null)
                {
                    Log.Warning("Torrent was null for item: " + item.MovieName);
                    return;
                }

                using var _ = cancellationToken.Register(() =>
                    Console.WriteLine("İndirme işlemi iptal ediliyor..."));

                var files = await GetFiles(item.Hash);
                if (files == null || files.Count == 0)
                {
                    Log.Warning("No files found for item: " + item.MovieName);
                    return;
                }

                var mediaFiles = files.Where(x => x != null && x.IsMediaFile).ToList();
                if (mediaFiles.Count == 0)
                {
                    Log.Warning("No media files found");
                    return;
                }

                if (mediaFiles.Count > 1)
                {
                    await Task.Run(() => ChangeTvShowEpisodeFilePrioritiesNew(torrent, cancellationToken));
                }
                else
                {
                    var mediaFile = mediaFiles.First();

                    var pieceRange = await GetValidPieceRange(torrent, mediaFile.Index);
                    int startIndex = pieceRange.Start;
                    int endIndex = pieceRange.End;
                    
                    if (startIndex > endIndex || startIndex < 0)
                    {
                        Log.Warning($"Invalid piece range: {startIndex} to {endIndex}");
                        return;
                    }

                    List<bool> pieces = await WaitForValidPieces(torrent, cancellationToken);
                    if (pieces == null || pieces.Count == 0)
                    {
                        Log.Warning("No valid pieces found");
                        return;
                    }

                    torrent.ClearPieceDeadLines();
                    torrent.ClearPiecePrioritiesExceptFile(mediaFile.Index);

                    // Corrected buffer ranges (first 3 and last 3)
                    int firstBufferEnd = Math.Min(startIndex + bufferPieces - 1, endIndex);
                    int lastBufferStart = Math.Max(endIndex - bufferPieces + 1, startIndex);

                    // Download buffers in parallel
                    try
                    {
                        await Task.WhenAll(
                            DownloadPieceRange(torrent, startIndex, firstBufferEnd, lastBufferStart, endIndex, checkInterval, cancellationToken)
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        throw; // Let the outer catch handle cancellation
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error downloading piece ranges: {ex.Message}");
                    }

                    // Calculate remaining pieces
                    int middleStart = firstBufferEnd + 1;
                    int middleEnd = lastBufferStart - 1;

                    // Download remaining pieces in batches
                    if (middleStart <= middleEnd)
                    {
                        for (int batchStart = middleStart; batchStart <= middleEnd && !cancellationToken.IsCancellationRequested; batchStart += batchSize)
                        {
                            int batchEnd = Math.Min(batchStart + batchSize - 1, middleEnd);
                            await DownloadPieceBatch(torrent, batchStart, batchEnd, checkInterval, cancellationToken);
                        }
                    }

                    SafeRemoveTorrent(torrent);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("İndirme başarıyla iptal edildi.");
                SafeRemoveTorrent(torrent);
            }
            catch (Exception e)
            {
                Log.Error($"Error in DownloadFileSequentially: {e.Message}\nStack Trace: {e.StackTrace}");
                SafeRemoveTorrent(torrent);
            }
        }

        private static async Task DownloadPieceRange(
            TorrentHandleWrapper torrent,
            int start,
            int end,
            int start2,
            int end2,
            int checkInterval,
            CancellationToken ct)
        {
            if (torrent == null || start < 0 || end < start || start2 < 0 || end2 < start2)
            {
                Log.Warning("Invalid parameters in DownloadPieceRange");
                return;
            }
            var piecesToDownload = new List<int>();
            var currentPieces = GetStatus(torrent.Hash).Pieces.ToList();

            for (int i = start; i <= end; i++)
            {
                if (i < currentPieces.Count && !currentPieces[i])
                {
                    piecesToDownload.Add(i);
                }
            }

            for (int i = start2; i <= end2; i++)
            {
                if (i < currentPieces.Count && !currentPieces[i])
                {
                    piecesToDownload.Add(i);
                }
            }

            int count = 1;
            foreach (int piece in piecesToDownload)
            {
                ct.ThrowIfCancellationRequested();
                torrent.QueuePriorityUpdate(piece, 7, count * 50);
                Console.WriteLine($"Piece: {piece}: Priority: Top, Deadline: {count * 50}");
                count++;
            }

            await Task.Run(() => 
            {
                try
                {
                    torrent.FlushPriorityUpdates();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error flushing priority updates: {ex.Message}");
                }
            });
            
            while (!ct.IsCancellationRequested && piecesToDownload.Count > 0)
            {
                var status = GetStatus(torrent.Hash);
                if (status == null || status.Pieces == null)
                {
                    await Task.Delay(checkInterval, ct);
                    continue;
                }
                
                currentPieces = status.Pieces.ToList();
                if (currentPieces.Count == 0)
                {
                    await Task.Delay(checkInterval, ct);
                    continue;
                }
                
                var remainingPieces = new List<int>();
                foreach (int p in piecesToDownload)
                {
                    if (p < currentPieces.Count && !currentPieces[p])
                    {
                        remainingPieces.Add(p);
                    }
                }
                
                if (remainingPieces.Count == 0)
                {
                    break;
                }
                else
                {
                    piecesToDownload = remainingPieces;
                    count = 1;
                    foreach (int piece in piecesToDownload)
                    {
                        if (ct.IsCancellationRequested) break;
                        
                        try
                        {
                            torrent.QueuePriorityUpdate(piece, 7, count * 50);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error updating piece priority: {ex.Message}");
                        }
                        count++;
                    }
                    
                    try
                    {
                        await Task.Run(() => torrent.FlushPriorityUpdates());
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error flushing priorities: {ex.Message}");
                    }
                }

                await Task.Delay(checkInterval, ct);
            }
        }

        private static async Task DownloadPieceBatch(
            TorrentHandleWrapper torrent,
            int start,
            int end,
            int checkInterval,
            CancellationToken ct)
        {
            if (torrent == null || start < 0 || end < start)
            {
                Log.Warning("Invalid parameters in DownloadPieceBatch");
                return;
            }
            
            try
            {
                var piecesToDownload = new List<int>();
                var status = GetStatus(torrent.Hash);
                
                if (status == null || status.Pieces == null)
                {
                    Log.Warning("Status or pieces information is null");
                    return;
                }
                
                var currentPieces = status.Pieces.ToList();
                if (currentPieces.Count == 0)
                {
                    Log.Warning("No pieces information available");
                    return;
                }

                // Identify pieces that need downloading
                for (int i = start; i <= end && i < currentPieces.Count; i++)
                {
                    if (!currentPieces[i])
                    {
                        piecesToDownload.Add(i);
                    }
                }

                if (piecesToDownload.Count == 0)
                    return;

                // Set initial priorities and deadlines
                int count = 1;
                foreach (int piece in piecesToDownload)
                {
                    if (ct.IsCancellationRequested) return;
                    
                    try
                    {
                        torrent.QueuePriorityUpdate(piece, 7, count * 100);
                        Console.WriteLine($"Piece: {piece}: Priority: Top, Deadline: {count * 100}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error setting initial priority: {ex.Message}");
                    }
                    count++;
                }
                
                try
                {
                    await Task.Run(() => torrent.FlushPriorityUpdates());
                }
                catch (Exception ex)
                {
                    Log.Error($"Error flushing initial priorities: {ex.Message}");
                }

                while (!ct.IsCancellationRequested)
                {
                    status = GetStatus(torrent.Hash);
                    if (status == null || status.Pieces == null)
                    {
                        await Task.Delay(checkInterval, ct);
                        continue;
                    }
                    
                    currentPieces = status.Pieces.ToList();
                    if (currentPieces.Count == 0)
                    {
                        await Task.Delay(checkInterval, ct);
                        continue;
                    }
                    
                    var remainingPieces = new List<int>();
                    foreach (int p in piecesToDownload)
                    {
                        if (p < currentPieces.Count && !currentPieces[p])
                        {
                            remainingPieces.Add(p);
                        }
                    }

                    if (remainingPieces.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        piecesToDownload = remainingPieces;
                        count = 1;
                        foreach (int piece in piecesToDownload)
                        {
                            if (ct.IsCancellationRequested) break;
                            
                            try
                            {
                                torrent.QueuePriorityUpdate(piece, 7, count * 100);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Error updating piece priority: {ex.Message}");
                            }
                            count++;
                        }
                        
                        try
                        {
                            await Task.Run(() => torrent.FlushPriorityUpdates());
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error flushing priorities: {ex.Message}");
                        }
                    }

                    await Task.Delay(checkInterval, ct);
                }

                // Reset priorities for completed batch
                if (!ct.IsCancellationRequested)
                {
                    foreach (int piece in piecesToDownload)
                    {
                        try
                        {
                            torrent.QueuePriorityUpdate(piece, 4, 0);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error resetting piece priority: {ex.Message}");
                        }
                    }
                    
                    try
                    {
                        torrent.FlushPriorityUpdates();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error flushing final priorities: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Let the cancellation propagate
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in DownloadPieceBatch: {ex.Message}");
            }
        }

        private static async Task<(int Start, int End)> GetValidPieceRange(TorrentHandleWrapper torrent, int fileIndex)
        {
            if (torrent == null || fileIndex < 0)
            {
                Log.Warning("Invalid parameters in GetValidPieceRange");
                return (-1, -1);
            }
            
            try
            {
                int maxRetries = 10;
                int retryCount = 0;
                var indexRange = await Task.Run(() => torrent.GetFilePieceRange(fileIndex));
                
                while (indexRange == null && retryCount < maxRetries)
                {
                    await Task.Delay(100);
                    indexRange = await Task.Run(() => torrent.GetFilePieceRange(fileIndex));
                    retryCount++;
                }
                
                if (indexRange == null)
                {
                    Log.Warning($"Could not get file piece range after {maxRetries} attempts");
                    return (-1, -1);
                }
                
                return (indexRange.StartPieceIndex, indexRange.EndPieceIndex);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in GetValidPieceRange: {ex.Message}");
                return (-1, -1);
            }
        }

        private static async Task<List<bool>> WaitForValidPieces(TorrentHandleWrapper torrent, CancellationToken ct)
        {
            if (torrent == null)
            {
                Log.Warning("Torrent is null in WaitForValidPieces");
                return new List<bool>();
            }
            
            try
            {
                int maxAttempts = 50; // Maximum number of attempts to get pieces
                int attempts = 0;
                
                while (!ct.IsCancellationRequested && attempts < maxAttempts)
                {
                    var status = await Task.Run(() => 
                    {
                        try
                        {
                            return torrent.GetStatus();
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error getting torrent status: {ex.Message}");
                            return null;
                        }
                    });
                    
                    if (status != null)
                    {
                        UpdateStatus(status);
                        var pieces = status.Pieces?.ToList();
                        if (pieces != null && pieces.Count > 0) 
                            return pieces;
                    }
                    
                    attempts++;
                    await Task.Delay(100, ct);
                }
                
                Log.Warning($"Could not get valid pieces after {maxAttempts} attempts");
                return new List<bool>();
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate cancellation
            }
            catch (Exception ex)
            {
                Log.Error($"Error in WaitForValidPieces: {ex.Message}");
                return new List<bool>();
            }
        }
    }

    public class TorrentPiece
    {
        public int Index { get; set; }
        public TorrentPieceState TorrentPieceState { get; set; }
    }

    public class AsyncRateLimiter
    {
        private DateTime _lastExecutionTime = DateTime.MinValue;
        private readonly TimeSpan _interval;
        private readonly object _lock = new object();

        public AsyncRateLimiter(TimeSpan interval)
        {
            _interval = interval;
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            bool shouldExecute;
            
            lock (_lock)
            {
                shouldExecute = (DateTime.Now - _lastExecutionTime) >= _interval;
                if (shouldExecute)
                {
                    _lastExecutionTime = DateTime.Now;
                }
            }
            
            if (shouldExecute)
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in rate-limited action: {ex.Message}");
                }
            }
        }

        public async Task ExecuteAsync<T>(Func<T, Task> action, T parameter)
        {
            bool shouldExecute;
            
            lock (_lock)
            {
                shouldExecute = (DateTime.Now - _lastExecutionTime) >= _interval;
                if (shouldExecute)
                {
                    _lastExecutionTime = DateTime.Now;
                }
            }
            
            if (shouldExecute)
            {
                try
                {
                    await action(parameter);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in rate-limited action with parameter: {ex.Message}");
                }
            }
        }
    }
}
