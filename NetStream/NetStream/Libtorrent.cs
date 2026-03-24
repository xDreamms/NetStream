using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using NetStream.Language;
using NetStream.Views;
using TorrentWrapper;

namespace NetStream
{
    public static class Libtorrent
    {
        public static TorrentFile? currentEpisodeFile;
        public static int lastEpisodeIndex = -1;
        
        public static LibtorrentClient Client { get; } = new();

        private static DateTime _lastObserveRestart = DateTime.MinValue;
        private static readonly TimeSpan _observeRestartInterval = TimeSpan.FromSeconds(5);

        public static Task<FileShareResponse> ShareFileAsync(string filePath, int expiryHours = 24)
        {
            return Task.FromResult(new FileShareResponse
                {
                    Success = false,
                ErrorMessage = "Dosya paylaşımı yalnızca ayrı sunucu modu ile desteklenir."
            });
        }
        
        public static async Task<long> GetPieceSize(string hash)
        {
            var torrent = await GetTorrentHandle(hash);
            return torrent?.PieceSize ?? 0;
        }

        public static async Task<bool> IsValidTorrent(string hash)
        {
            return await GetTorrentHandle(hash) != null;
        }

        public static async Task<ChangeEpisodeResponse> ChangeEpisodeFileToMaximalPriority(string hash, int season, int episode)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null)
            {
                return new ChangeEpisodeResponse { Success = false, ErrorMessage = "Couldnt find torrent." };
            }

            try
            {
                await torrent.ChangeEpisodeFileToMaximalPriority(season, episode);

                var files = await torrent.GetFiles();
                currentEpisodeFile = files?.FirstOrDefault(x =>
                    Helper.GetSeasonNumberFromFileName(x.Name) == season &&
                    Helper.GetEpisodeNumberFromFileName(x.Name) == episode);
                lastEpisodeIndex = currentEpisodeFile?.Index ?? -1;

                return new ChangeEpisodeResponse { Success = true };
            }
            catch (Exception ex)
            {
                return new ChangeEpisodeResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public static async Task<FilePieceRange> GetFilePieceRange(string hash, int fileIndex)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null) return new FilePieceRange();

            return await torrent.GetFilePieceRange(fileIndex);
        }

        public static Task ChangeMovieCollectionFilePriorityToMaximal(Item item, int fileIndex)
        {
            Log.Warning("ChangeMovieCollectionFilePriorityToMaximal henüz uygulanmadı.");
            return Task.CompletedTask;
        }

        public static async Task<TorrentState> GetTorrentState(string hash)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null) return TorrentState.Unknown;

            return await torrent.GetTorrentState();
        }

        public static async Task<List<TorrentFile>> GetFiles(string hash)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null) return new List<TorrentFile>();

            var files = await torrent.GetFiles();
            return files?.OrderBy(f => f.Index).ToList() ?? new List<TorrentFile>();
        }

        public static async Task<List<TimeRange>> GetAvailaibleSeconds(string hash, int durationInSeconds, int fileIndex)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null) return new List<TimeRange>();

            var response = await torrent.GetAvailaibleSeconds(durationInSeconds, fileIndex);
            return response?.TimeRanges ?? new List<TimeRange>();
        }

        public static async Task<TorrentStatusInfo> GetStatus(string hash)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null) return new TorrentStatusInfo { Hash = hash, Pieces = Array.Empty<bool>() };

            return torrent.GetStatus() ?? new TorrentStatusInfo { Hash = hash, Pieces = Array.Empty<bool>() };
        }

        public static async Task<TorrentItemResponse> Pause(string hash)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null)
            {
                return new TorrentItemResponse { Success = false, ErrorMessage = "Couldnt find torrent." };
            }

            try
            {
                await Task.Run(() => torrent.Pause());
                return new TorrentItemResponse { Success = true };
            }
            catch (Exception ex)
            {
                return new TorrentItemResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public static async Task<TorrentItemResponse> Resume(string hash)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null)
            {
                return new TorrentItemResponse { Success = false, ErrorMessage = "Couldnt find torrent." };
            }

            try
            {
                await Task.Run(() => torrent.Resume());
                await torrent.DownloadFileSequentially();
                return new TorrentItemResponse { Success = true };
            }
            catch (Exception ex)
            {
                return new TorrentItemResponse { Success = false, ErrorMessage = ex.Message };
            }
        }
        
        public static async Task<CancelDownloadResponse> CancelDownload(string hash)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null)
            {
                return new CancelDownloadResponse { Success = false, ErrorMessage = "Couldnt find torrent." };
            }

            return await torrent.CancelDownload();
        }

        public static async Task<CancelDownloadResponse> CancelDownloads(string hash)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null)
            {
                return new CancelDownloadResponse { Success = false, ErrorMessage = "Couldnt find torrent." };
            }

            return await torrent.CancelDownloads();
        }

        public static async Task<bool> Delete(string hash)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null) return false;

            if (await torrent.IsValid())
            {
                return await torrent.RemoveTorrent();
            }
            return false;
        }

        public static async Task<string> AddTorrentFromFile(string path)
        {
                if (!File.Exists(path))
                {
                Log.Warning("Torrent dosyası bulunamadı: {Path}", path);
                    return string.Empty;
                }
                
            var moviesPath = AppSettingsManager.appSettings.MoviesPath;
            var response = await Client.AddTorrentFromFile(path, moviesPath);
            if (!response.Success)
            {
                Log.Warning("Torrent eklenemedi: {Message}", response.ErrorMessage);
                            return string.Empty;
                        }

            return response.Hash ?? string.Empty;
        }

        public static Task<bool> IsTorrentExistPath(string path) => Client.IsTorrentExistPath(path);

        public static Task<bool> IsTorrentExistUrl(string url) => Client.IsTorrentExistUrl(url);

        public static async Task<string> AddTorrentFromMagnet(string url)
        {
            var moviesPath = AppSettingsManager.appSettings.MoviesPath;
            var response = await Client.AddTorrentFromMagnet(url, moviesPath);
            if (!response.Success)
            {
                Log.Warning("Magnet eklenemedi: {Message}", response.ErrorMessage);
                return string.Empty;
            }

            return response.Hash ?? string.Empty;
        }

        public static async Task<DownloadFileSequentiallyResponse> DownloadFileSequentially(string hash)
        {
            var torrent = await GetTorrentHandle(hash);
            if (torrent == null)
            {
                return new DownloadFileSequentiallyResponse { Success = false, ErrorMessage = "Couldnt find torrent." };
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await torrent.DownloadFileSequentially();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Background download failed: {ex.Message}");
                }
            });

            return new DownloadFileSequentiallyResponse { Success = true };
        }

        public static async Task ObserveChanges(Item item)
        {
            if (item == null || string.IsNullOrEmpty(item.Hash))
            {
                Console.WriteLine("[ObserveChanges] Item is null or hash is empty");
                return;
            }

            var torrent = await GetTorrentHandle(item.Hash);
            if (torrent == null)
            {
                Console.WriteLine($"[ObserveChanges] Could not get torrent handle for hash: {item.Hash}. Setting NULL values.");
                item.DownloadPercent = -1;
                item.DownloadSpeed = "NULL";
                item.Eta = "NULL";
                item.IsCompleted = false;
                
                // Try to restart the torrent
                if ((DateTime.Now - _lastObserveRestart) >= _observeRestartInterval && DownloadsPage.Instance != null)
                {
                    Console.WriteLine($"[ObserveChanges] Attempting to restart torrent: {item.Hash}");
                    _lastObserveRestart = DateTime.Now;
                    await DownloadsPage.Instance.StartTorrenting2(item);
                    item.IsCompleted = false;
                    DownloadsPage.Instance.SaveAllTorrents();
                }
                return;
            }

            var response = await torrent.ObserveChanges(
                ResourceProvider.GetString("DownloadSpeedString"),
                ResourceProvider.GetString("CompletedString"),
                ResourceProvider.GetString("EtaString"));

            if (response.Success)
            {
                item.DownloadSpeed = response.DownloadSpeed;
                item.IsCompleted = response.IsCompleted;
                item.DownloadPercent = response.DownloadPercent;
                item.Eta = response.ETA;
            }
            else if (response.ErrorMessage == "not valid not added")
            {
                Console.WriteLine($"[ObserveChanges] Torrent not valid/added for hash: {item.Hash}");
                if ((DateTime.Now - _lastObserveRestart) >= _observeRestartInterval && DownloadsPage.Instance != null)
                {
                    Console.WriteLine($"[ObserveChanges] Restarting torrent: {item.Hash}");
                    _lastObserveRestart = DateTime.Now;
                    await DownloadsPage.Instance.StartTorrenting2(item);
                    item.IsCompleted = false;
                    DownloadsPage.Instance.SaveAllTorrents();
                }
            }
            else
            {
                Console.WriteLine($"[ObserveChanges] Error for hash {item.Hash}: {response.ErrorMessage}");
            }
        }

        public static Task<List<TorrentHandleResponseShort>> GetTorrentList()
        {
            return Client.GeTorrentList();
        }

        public static Task<bool> Load() => Task.FromResult(true);

        public static Task<bool> UnLoad()
        {
            try
            {
                Client.CleanupCache();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Task.FromResult(false);
            }
        }

        private static async Task<TorrentHandle?> GetTorrentHandle(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return null;
            }

            var currentTorrent = Client.Torrents.FirstOrDefault(x => x.Hash == hash);
            if (currentTorrent == null)
            {
                currentTorrent = new TorrentHandle(hash);
                Client.Torrents.Add(currentTorrent);
            }

            if (!await currentTorrent.IsValid())
            {
                RemoveHandleFromCaches(hash, currentTorrent);
                currentTorrent = new TorrentHandle(hash);
                Client.Torrents.Add(currentTorrent);

                if (!await currentTorrent.IsValid())
                {
                    RemoveHandleFromCaches(hash, currentTorrent);
                    return null;
                }
            }

            return currentTorrent;
        }

        private static void RemoveHandleFromCaches(string hash, TorrentHandle handle)
        {
            Client.Torrents.Remove(handle);
            if (Client.torrentHandleWrappers.ContainsKey(hash))
            {
                Client.torrentHandleWrappers.Remove(hash);
            }
        }
 }

 public class AddTorrentFromFileResponse
 {
     public string Hash { get; set; }
     public bool Success { get; set; }
     public string ErrorMessage { get; set; }
 }

 public class AddTorrentFromMagnetResponse
 {
     public string Hash { get; set; }
     public bool Success { get; set; }
     public string ErrorMessage { get; set; }
 }

 public class TorrentItemResponse
 {
     public bool Success { get; set; }
     public string ErrorMessage { get; set; }
 }

    public class ChangeEpisodeResponse
    {
     public bool Success { get; set; }
     public string ErrorMessage { get; set; }
     public bool ReadyToPlay { get; set; } // Indicates if first pieces are ready for playback
 }

    public class ChangeMovieCollectionResponse
 {
     public bool Success { get; set; }
     public string ErrorMessage { get; set; }
 }

    public class DownloadFileSequentiallyResponse
 {
     public string ErrorMessage { get; set; }
     public bool Success { get; set; }
 }

    public class CancelDownloadResponse
 {
     public string ErrorMessage { get; set; }
     public bool Success { get; set; }
 }

 public class ObserveChangesResponse
 {
     public double DownloadPercent { get; set; }
     public string ETA { get; set; }
     public string DownloadSpeed { get; set; }
     public bool Success { get; set; }
     public string ErrorMessage { get; set; }
     public bool IsCompleted { get; set; }
 }

    public class GetAvailaibleSecondsResponse
 {
        public List<TimeRange> TimeRanges { get; set; }
     public bool Success { get; set; }
     public string ErrorMessage { get; set; }
 }

    public class TimeRange
    {
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
 }

 public class TorrentStatusInfo
 {
     public string Hash { get; set; }
     public float Progress { get; set; }
     public string EstimatedTime { get; set; }
     public string DownloadSpeedString { get; set; }
        public IEnumerable<bool> Pieces { get; set; } = Array.Empty<bool>();
    }

 public enum TorrentState
 {
     CheckingFiles,
     DownloadingMetadata,
     Downloading,
     Finished,
     Seeding,
     Allocating,
     CheckingResumeData,
     Unknown
    }

 public enum DownloadPriority
 {
        DontDownload = 0,
        LowPriority = 1,
        DefaultPriority = 4,
        TopPriority = 7
    }

 public class TorrentFile
 {
     public string Name { get; set; }
     public string FullPath { get; set; }
     public int Index { get; set; }
        public long Size { get; set; }
     public bool IsCompleted { get; set; }
     public bool IsMediaFile { get; set; }
     public double Progress { get; set; }
     public DownloadPriority DownloadPriority { get; set; }
    }

 public class FilePieceRange
 {
     public int FileIndex { get; set; }
     public int StartPieceIndex { get; set; }
     public int EndPieceIndex { get; set; }
    }

 public class TorrentHandleResponseShort
 {
     public string Name { get; set; }
     public string Hash { get; set; }
     public TorrentState TorrentState { get; set; }
     public long PieceSize { get; set; }
     public int TotalPieces { get; set; }
     public DateTime AddedOn { get; set; }
        public long Size { get; set; }
        public long Downloaded { get; set; }
     public float Progress { get; set; }
     public bool IsPaused { get; set; }
 }

 public class BackgroundDownloadStatusResponse
 {
     public string Hash { get; set; }
     public bool IsRunning { get; set; }
     public bool HasError { get; set; }
     public string ErrorMessage { get; set; }
     public double Progress { get; set; }
     public DateTime StartTime { get; set; }
     public DateTime? LastError { get; set; }
     public DateTime? LastProgressUpdate { get; set; }
     public double LastProgressValue { get; set; }
     public bool Success { get; set; }
 }

    public class TorrentPiecePriority
    {
        public string Hash { get; set; }
        public int FileIndex { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }

    public class ChangeTvShowEpisodeFilePrioritiesNewResponse
    {
        public string ErrorMessage { get; set; }
        public bool Success { get; set; }
    }

    public class UpdateStatusResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CancelPrioritiesResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
}

public class FileShareRequest
{
    public string FilePath { get; set; }
    public int ExpiryHours { get; set; } = 24;
}

public class FileShareResponse
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public string FileId { get; set; }
    public string FileName { get; set; }
    public string DownloadUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
}
}
