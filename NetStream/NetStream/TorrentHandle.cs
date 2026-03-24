using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BencodeNET.Torrents;
using TorrentWrapper;

namespace NetStream;

public class TorrentHandle: IDisposable
{
    public TorrentHandleWrapper torrentHandleWrapper;
    private Task _loadTask;
    private static readonly object _loadLock = new object();
    private bool _isLoading = false;
    private bool _loadAttempted = false;
    private bool _disposed = false;

    public TorrentHandle(string hash)
    {
        this.Hash = hash;
        _loadTask = LoadAsync();
        TorrentPiecePriorities = new List<TorrentPiecePriority>();
    }

    private async Task LoadAsync()
    {
        if (torrentHandleWrapper != null && torrentHandleWrapper.IsValid) return;
            
        lock (_loadLock)
        {
            if (_isLoading) return;
            if (_loadAttempted) return;
            _isLoading = true;
            _loadAttempted = true;
        }
        
        try 
        {

            torrentHandleWrapper = await Libtorrent.Client.GetTorrentHandle(Hash);
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"Error loading torrent handle: {ex.Message}");
        }
        finally
        {
            lock (_loadLock)
            {
                _isLoading = false;
                _loadAttempted = false;
            }
        }
    }

    public string Name
    {
        get
        {
            return torrentHandleWrapper.Name;
        }
    }

    public bool IsPaused
    {
        get
        { 
            return torrentHandleWrapper.IsPaused; 
        }
    }

    public string Hash { get; private set; }


    public TorrentStatusInfo GetStatus()
    {
        try
        {
            if (TorrentStatusInfo != null)
            {
                return TorrentStatusInfo;
            }
            else
            {
                var sta = torrentHandleWrapper.GetStatus();
                TorrentStatusInfo = new TorrentStatusInfo
                {
                    Hash = sta.Hash,
                    Progress = sta.Progress,
                    EstimatedTime = sta.EstimatedTime,
                    DownloadSpeedString = sta.DownloadSpeedString,
                    Pieces = sta.Pieces
                };
                return TorrentStatusInfo;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error on get status: " + e.Message);
            return null;
        }
    }


    public async Task<TorrentState> GetTorrentState()
    {
        try
        {
            return (TorrentState)torrentHandleWrapper.GetTorrentState;
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        return TorrentState.Unknown;
    }

    private List<TorrentFile> Files = new List<TorrentFile>();

    public async Task<List<TorrentFile>> GetFiles()
    {
        if (torrentHandleWrapper == null || !torrentHandleWrapper.IsValid)
        {
            return Files ?? new List<TorrentFile>();
        }

        List<TorrentFile> files = null;
        try
        {
            int retryCount = 0;
            files = torrentHandleWrapper.GetFiles();

            while ((files == null || files.Count == 0) && retryCount < 50)
            {
                await Task.Delay(200);
                if (torrentHandleWrapper == null || !torrentHandleWrapper.IsValid)
                {
                    return Files ?? new List<TorrentFile>();
                }
                files = torrentHandleWrapper.GetFiles();
                retryCount++;
            }

            if (files == null || files.Count == 0) return Files ?? new List<TorrentFile>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting files: {ex.Message}");
            return Files ?? new List<TorrentFile>();
        }

        if (Files.Count > 0)
        {
            var fileLookup = new Dictionary<int, TorrentFile>();
            foreach (var f in files)
            {
                fileLookup[f.Index] = f;
            }
            foreach (var torrentFile in Files)
            {
                if (fileLookup.TryGetValue(torrentFile.Index, out var updated))
                {
                    torrentFile.Progress = updated.Progress;
                    torrentFile.IsCompleted = updated.IsCompleted;
                }
            }
        }
        else
        {
            foreach (var f in files)
            {
                Files.Add(new TorrentFile
                {
                    Name = f.Name,
                    FullPath = f.FullPath,
                    Index = f.Index,
                    Size = f.Size,
                    IsCompleted = f.IsCompleted,
                    IsMediaFile = f.IsMediaFile,
                    Progress = f.Progress,
                    DownloadPriority = f.DownloadPriority
                });
            }
        }


        return Files;
    }


    public async Task<FilePieceRange> GetFilePieceRange(int fileIndex)
    {
        if (torrentHandleWrapper == null || !torrentHandleWrapper.IsValid)
        {
            return new FilePieceRange
            {
                FileIndex = fileIndex,
                StartPieceIndex = -1,
                EndPieceIndex = -1
            };
        }

        try
        {
            var pieceRange = torrentHandleWrapper.GetFilePieceRange(fileIndex);
            if (pieceRange == null)
            {
                return new FilePieceRange
                {
                    FileIndex = fileIndex,
                    StartPieceIndex = -1,
                    EndPieceIndex = -1
                };
            }

            var result = new FilePieceRange
            {
                FileIndex = pieceRange.FileIndex,
                StartPieceIndex = pieceRange.StartPieceIndex,
                EndPieceIndex = pieceRange.EndPieceIndex
            };

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting file piece range: {ex.Message}");
            return new FilePieceRange
            {
                FileIndex = fileIndex,
                StartPieceIndex = -1,
                EndPieceIndex = -1
            };
        }
    }

    public long PieceSize
    {
        get
        {
            return torrentHandleWrapper.PieceSize;
        }
    }

    public int TotalPieces
    {
        get
        {
            return torrentHandleWrapper.TotalPieces;
        }
    }

    public DateTime AddedOn
    {
        get
        {
            return torrentHandleWrapper.AddedOn;
        }
    }
    public Int64 Size
    {
        get
        {
            return torrentHandleWrapper.Size;
        }
    }

    public Int64 Downloaded
    {
        get
        {
            return torrentHandleWrapper.Downloaded;
        }
    }

    public void QueuePriorityUpdate(int piece, int priority, int deadline)
    {
        Console.WriteLine("Hash: " + Hash + " Piece: " + piece + " Priority: " + (DownloadPriority)priority + " Deadline: " + deadline);
        torrentHandleWrapper.QueuePriorityUpdate(piece, priority, deadline);
    }

    public void FlushPriorityUpdates()
    {
        torrentHandleWrapper.FlushPriorityUpdates();
    }

    public void ResetPriorityRange(int start, int end)
    {
        torrentHandleWrapper.ResetPriorityRange(start, end);
    }

    public void ClearPieceDeadLines()
    {
        torrentHandleWrapper.ClearPieceDeadLines();
    }

    void ClearPiecePrioritiesExceptFile(int fileIndex)
    {
        torrentHandleWrapper.ClearPiecePrioritiesExceptFile(fileIndex);
    }

    public void Pause()
    {
        torrentHandleWrapper.Pause();
    }

    public void Resume()
    {
        torrentHandleWrapper.Resume();
    }


    public void Delete()
    {
        torrentHandleWrapper.Delete(true,true, Hash, null, null);
    }

    public async Task<bool> IsValid()
    {
        if (_loadTask != null && !_loadTask.IsCompleted)
        {
            try
            {
               await _loadTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error waiting for torrent load: {ex.Message}");
                return false;
            }
        }

        return torrentHandleWrapper != null && torrentHandleWrapper.IsValid;
    }

   
    const int checkInterval = 500;
    const int bufferPieces = 5; // 3 -> 5 (better buffering for playback)
    const int batchSize = 40; // 50 -> 40 (RAM optimization)
    public async Task<ChangeTvShowEpisodeFilePrioritiesNewResponse> ChangeTvShowEpisodeFilePrioritiesNew(CancellationToken token)
    {
        
        try
        {
            var files = await GetFiles();
            if (files == null || files.Count == 0)
            {
                return new ChangeTvShowEpisodeFilePrioritiesNewResponse()
                {
                    ErrorMessage = "No files found for torrent hash: " + Hash,
                    Success = false
                };
            }

            var episodeFiles = files.Where(x => x != null && x.IsMediaFile && !x.IsCompleted 
                                                && Helper.GetSeasonNumberFromFileName(x.Name) != -2
                                                && Helper.GetEpisodeNumberFromFileName(x.Name) != -2)
                .OrderBy(x => Helper.GetSeasonNumberFromFileName(x.Name))
                .ThenBy(x => Helper.GetEpisodeNumberFromFileName(x.Name)).ToList();

            while (!token.IsCancellationRequested && episodeFiles.Count != 0)
            {
                TorrentFile fileToDownload = null;

                // PRIORITY LOGIC:
                // 1. If user clicked an episode (currentEpisodeFile is set) and it's not completed, download it first
                // 2. Otherwise, download episodes sequentially starting from episode 1

                if (currentEpisodeFile != null)
                {
                    var clickedEpisode = episodeFiles.FirstOrDefault(x => x.Index == currentEpisodeFile.Index);
                    if (clickedEpisode != null)
                    {
                        // User clicked episode is still incomplete, prioritize it
                        fileToDownload = clickedEpisode;
                    }
                }

                // If no clicked episode or it's completed, find first incomplete episode
                if (fileToDownload == null && episodeFiles.Count > 0)
                {
                    fileToDownload = episodeFiles[0]; // First episode in sorted list (by season, then episode)
                }

                // Set priority for the chosen file
                if (fileToDownload != null && !IsPrioritySet(fileToDownload.Index))
                {
                    await SetPriorityForFile(fileToDownload.Index);
                }

                await Task.Delay(1000, token);

                files = await GetFiles();
                if (files == null || files.Count == 0) break;

                episodeFiles = files.Where(x => x != null && x.IsMediaFile && !x.IsCompleted && Helper.GetSeasonNumberFromFileName(x.Name) != -2
                                                && Helper.GetEpisodeNumberFromFileName(x.Name) != -2)
                    .OrderBy(x => Helper.GetSeasonNumberFromFileName(x.Name))
                    .ThenBy(x => Helper.GetEpisodeNumberFromFileName(x.Name)).ToList();

                // Reduce CPU usage by increasing delay slightly
                if (episodeFiles.Count > 0)
                {
                    await Task.Delay(500, token); // Reduced from 1000ms to balance responsiveness and CPU usage
                }
            }

            Console.WriteLine($"[ChangeTvShowEpisodeFilePrioritiesNew] All episode files downloaded for hash: {Hash}. Torrent handle preserved.");
            return new ChangeTvShowEpisodeFilePrioritiesNewResponse
            {
                ErrorMessage = "Successfully Downlaoded.",
                Success = true,
            };
        }
        catch (OperationCanceledException)
        {
            return new ChangeTvShowEpisodeFilePrioritiesNewResponse
            {
                ErrorMessage = "Operation was canceled in ChangeTvShowEpisodeFilePrioritiesNew for hash: " +
                               Hash,
                Success = false,
            };
        }
        catch (Exception e)
        {
            return new ChangeTvShowEpisodeFilePrioritiesNewResponse
            {
                ErrorMessage = "Error on Priorities: " + e.Message,
                Success = false,
            };
        }
    }

    private async void SafeRemoveTorrent()
    {
        try
        {
            Pause();
            await CancelDownloads();
            lock (_torrentHandleWrappersLock)
            {
                if (Libtorrent.Client.torrentHandleWrappers.ContainsKey(Hash))
                {
                    Libtorrent.Client.torrentHandleWrappers.Remove(Hash);
                }
            }

            lock (_torrentsLock)
            {
                Libtorrent.Client.Torrents.Remove(this);
            }
            torrentHandleWrapper.Dispose();
            Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing torrent: {ex.Message}");
        }
    }

    public async Task<bool> RemoveTorrent()
    {
        try
        {
            Console.WriteLine($"[TorrentHandle.RemoveTorrent] Starting removal for hash: {Hash}");
            
            Pause();
            await CancelDownloads();
            
            bool success = false;
            if (torrentHandleWrapper != null)
            {
                Console.WriteLine($"[TorrentHandle.RemoveTorrent] Calling Delete on torrentHandleWrapper");
                // Run on thread pool to avoid blocking
                success = await Task.Run(() => torrentHandleWrapper.Delete(true, true, Hash, null, null));
                
                if (success)
                {
                    torrentHandleWrapper.Dispose();
                    Console.WriteLine($"[TorrentHandle.RemoveTorrent] Torrent handle wrapper deleted and disposed");
                    
                    lock (_torrentHandleWrappersLock)
                    {
                        if (Libtorrent.Client.torrentHandleWrappers.ContainsKey(Hash))
                        {
                            Libtorrent.Client.torrentHandleWrappers.Remove(Hash);
                            Console.WriteLine($"[TorrentHandle.RemoveTorrent] Removed from torrentHandleWrappers");
                        }
                    }

                    lock (_torrentsLock)
                    {
                        Libtorrent.Client.Torrents.Remove(this);
                        Console.WriteLine($"[TorrentHandle.RemoveTorrent] Removed from Torrents list");
                    }
                }
                else
                {
                     Console.WriteLine($"[TorrentHandle.RemoveTorrent] Delete failed (wrapper returned false)");
                }
            }
            else
            {
                Console.WriteLine($"[TorrentHandle.RemoveTorrent] Warning: torrentHandleWrapper is null");
                // If wrapper is null, we can't delete via wrapper. 
                // Maybe we should remove from lists anyway if it's invalid?
                // But user wants robust deletion. If wrapper is null, maybe it's already gone.
                // Let's assume failure if we can't confirm deletion.
                success = false;
            }
            
            Dispose();
            Console.WriteLine($"[TorrentHandle.RemoveTorrent] Removal complete for hash: {Hash}, Success: {success}");
            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TorrentHandle.RemoveTorrent] Error removing torrent: {ex.Message}");
            Console.WriteLine($"[TorrentHandle.RemoveTorrent] Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    private readonly object _torrentPiecePrioritiesLock = new object();
    private readonly object _torrentsLock = new object();
    private readonly object _torrentHandleWrappersLock = new object();

    private bool IsPrioritySet(int fileIndex)
    {
        lock (_torrentPiecePrioritiesLock)
        {
            return TorrentPiecePriorities.Any(x =>
                x.Hash != null && Hash != null &&
                x.Hash.Equals(Hash, StringComparison.OrdinalIgnoreCase) &&
                x.FileIndex == fileIndex);
        }
    }

    private async Task SetPriorityForFile(int fileIndex)
    {
        CancelAllPreviousPriorities();

        var torrentPiecePriority = new TorrentPiecePriority
        {
            Hash = Hash,
            FileIndex = fileIndex,
            CancellationTokenSource = new CancellationTokenSource()
        };

        lock (_torrentPiecePrioritiesLock)
        {
            TorrentPiecePriorities.Add(torrentPiecePriority);
        }

        _ = Task.Run(() => DownloadFileSequentially(fileIndex, torrentPiecePriority.CancellationTokenSource.Token));
    }

    public CancelPrioritiesResponse CancelAllPreviousPriorities()
    {
        try
        {
            List<TorrentPiecePriority> toCancel;

            lock (_torrentPiecePrioritiesLock)
            {
                toCancel = TorrentPiecePriorities.ToList();
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
                    Console.WriteLine("Error cancelling token: " + ex.Message);
                }
            }

            foreach (var torrentPiecePriority in toCancel)
            {
                try
                {
                    torrentPiecePriority.CancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error disposing token: " + ex.Message);
                }
            }

            lock (_torrentPiecePrioritiesLock)
            {
                TorrentPiecePriorities.Clear();
            }

            return new CancelPrioritiesResponse { Success = true, ErrorMessage = "" };
        }
        catch (Exception e)
        {
            Console.WriteLine("Error on CancelAllPrios: " + e.Message);
            return new CancelPrioritiesResponse { Success = false, ErrorMessage = "Error on CancelAllPrios: " + e.Message };
        }
    }

    private List<TorrentPiecePriority> TorrentPiecePriorities;
    private TorrentFile? currentEpisodeFile;
    public TorrentStatusInfo TorrentStatusInfo;

    public UpdateStatusResponse UpdateStatus(TorrentStatusInfo status)
    {
        try
        {
            if (TorrentStatusInfo == null)
            {
                TorrentStatusInfo = new TorrentStatusInfo();
            }

            TorrentStatusInfo.Progress = status.Progress;
            TorrentStatusInfo.DownloadSpeedString = status.DownloadSpeedString;
            TorrentStatusInfo.EstimatedTime = status.EstimatedTime;
            TorrentStatusInfo.Pieces = status.Pieces;
            TorrentStatusInfo.Hash = this.Hash;

            return new UpdateStatusResponse { Success = true, ErrorMessage = "" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating status: {ex.Message}");
            return new UpdateStatusResponse { Success = false, ErrorMessage = $"Error updating status: {ex.Message}" };
        }
    }


    public async Task<CancelDownloadResponse> CancelDownload()
    {
        try
        {
            if (DownloadFileSequentiallyCancellationTokenSource != null)
            {
                if (!DownloadFileSequentiallyCancellationTokenSource.IsCancellationRequested)
                {
                    DownloadFileSequentiallyCancellationTokenSource.Cancel();
                }
                DownloadFileSequentiallyCancellationTokenSource.Dispose();
                return new CancelDownloadResponse() { ErrorMessage = "", Success = true };
            }

            return new CancelDownloadResponse() { ErrorMessage = "Couldnt find cancellation token", Success = false };
        }
        catch (Exception e)
        {
            return new CancelDownloadResponse() { ErrorMessage = e.Message, Success = false };
        }
    }

    public async Task<CancelDownloadResponse> CancelDownloads()
    {
        try
        {
            try
            {
                if (DownloadFileSequentiallyCancellationTokenSource != null 
                    && !DownloadFileSequentiallyCancellationTokenSource.IsCancellationRequested)
                {
                    DownloadFileSequentiallyCancellationTokenSource.Cancel();
                    DownloadFileSequentiallyCancellationTokenSource.Dispose();
                    DownloadFileSequentiallyCancellationTokenSource = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error cancelling download token: " + ex.Message);
                return new CancelDownloadResponse()
                {
                    ErrorMessage = "Error cancelling download token: " + ex.Message,
                    Success = false
                };
            }

            List<TorrentPiecePriority> prioritiesToRemove;
            lock (_torrentPiecePrioritiesLock)
            {
                prioritiesToRemove = TorrentPiecePriorities
                    .ToList();
            }

            foreach (var priority in prioritiesToRemove)
            {
                try
                {
                    if (priority.CancellationTokenSource != null &&
                        !priority.CancellationTokenSource.IsCancellationRequested)
                    {
                        priority.CancellationTokenSource.Cancel();
                        priority.CancellationTokenSource?.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error cancelling priority token: " + ex.Message);
                }
            }

            lock (_torrentPiecePrioritiesLock)
            {
                TorrentPiecePriorities.Clear();
            }

            // GC'yi bellek temizleme için teşvik et
            if (prioritiesToRemove.Count > 0)
            {
                GC.Collect(0, GCCollectionMode.Optimized, false);
            }

            return new CancelDownloadResponse() { ErrorMessage = "", Success = true };
        }
        catch (Exception e)
        {
            Console.WriteLine("Error in CancelDownloads: " + e.Message);
            return new CancelDownloadResponse() { ErrorMessage = "Error in CancelDownloads: " + e.Message, Success = false };
        }
    }


    private CancellationTokenSource DownloadFileSequentiallyCancellationTokenSource;
    private bool _isSequentialDownloadRunning = false;
    private readonly object _sequentialDownloadLock = new object();
    
    public async Task<DownloadFileSequentiallyResponse> DownloadFileSequentially()
    {
        // Prevent multiple concurrent sequential download tasks
        lock (_sequentialDownloadLock)
        {
            if (_isSequentialDownloadRunning)
            {
                Console.WriteLine($"[DownloadFileSequentially] Sequential download already running for {Hash}, skipping duplicate start");
                return new DownloadFileSequentiallyResponse { Success = true, ErrorMessage = "Already running" };
            }
            _isSequentialDownloadRunning = true;
        }
        
        try
        {
            Console.WriteLine($"[DownloadFileSequentially] Starting sequential download for hash: {Hash}");
            
            // Cancel any existing download first
            if (DownloadFileSequentiallyCancellationTokenSource != null)
            {
                Console.WriteLine($"[DownloadFileSequentially] Cancelling existing download for hash: {Hash}");
                try
                {
                    DownloadFileSequentiallyCancellationTokenSource.Cancel();
                    DownloadFileSequentiallyCancellationTokenSource.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DownloadFileSequentially] Error cancelling existing download: {ex.Message}");
                }
                DownloadFileSequentiallyCancellationTokenSource = null;
            }
            
            DownloadFileSequentiallyCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = DownloadFileSequentiallyCancellationTokenSource.Token;
            using var _ = cancellationToken.Register(() =>
                Console.WriteLine($"[DownloadFileSequentially] Download cancellation requested for {Hash}"));

            var files = await GetFiles();
            if (files == null || files.Count == 0)
            {
                Console.WriteLine($"[DownloadFileSequentially] No files found for hash: {Hash}");
                return new DownloadFileSequentiallyResponse { Success = false, ErrorMessage = "No files found"};
            }
            
            var mediaFiles = files.Where(x => x != null && x.IsMediaFile).ToList();
            if (mediaFiles.Count == 0)
            {
                Console.WriteLine($"[DownloadFileSequentially] No media files found for hash: {Hash}");
                return new DownloadFileSequentiallyResponse { Success = false, ErrorMessage = "No media files found" };
            }

            Console.WriteLine($"[DownloadFileSequentially] Found {mediaFiles.Count} media file(s) for hash: {Hash}");

            if (mediaFiles.Count > 1)
            {
                Console.WriteLine($"[DownloadFileSequentially] Processing multiple media files ({mediaFiles.Count}) for hash: {Hash}");
                await Task.Run(() => ChangeTvShowEpisodeFilePrioritiesNew(cancellationToken));
            }
            else
            {
                var mediaFile = mediaFiles.First();
                Console.WriteLine($"[DownloadFileSequentially] Processing single media file: {mediaFile.Name} for hash: {Hash}");

                var pieceRange = await GetValidPieceRange(mediaFile.Index);
                int startIndex = pieceRange.Start;
                int endIndex = pieceRange.End;
                
                Console.WriteLine($"[DownloadFileSequentially] Piece range: {startIndex} to {endIndex} for hash: {Hash}");
                
                if (startIndex > endIndex || startIndex < 0)
                {
                    Console.WriteLine($"[DownloadFileSequentially] Invalid piece range: {startIndex} to {endIndex} for hash: {Hash}");
                    return new DownloadFileSequentiallyResponse { Success = false, ErrorMessage = $"Invalid piece range: {startIndex} to {endIndex}" };
                }
                
                var pieces = await WaitForValidPieces(cancellationToken);
                if (pieces == null || pieces.Count() == 0)
                {
                    Console.WriteLine($"[DownloadFileSequentially] No valid pieces found for hash: {Hash}");
                    return new DownloadFileSequentiallyResponse { Success = false, ErrorMessage = "No valid pieces found" };
                }

                Console.WriteLine($"[DownloadFileSequentially] Setting sequential piece priorities for hash: {Hash}");
                ClearPieceDeadLines();
                ClearPiecePrioritiesExceptFile(mediaFile.Index);

                int firstBufferEnd = Math.Min(startIndex + bufferPieces - 1, endIndex);
                int lastBufferStart = Math.Max(endIndex - bufferPieces + 1, startIndex);

                try
                {
                    Console.WriteLine($"[DownloadFileSequentially] Downloading priority piece ranges: {startIndex}-{firstBufferEnd} and {lastBufferStart}-{endIndex} for hash: {Hash}");
                    await DownloadPieceRange(startIndex, firstBufferEnd, lastBufferStart, endIndex, checkInterval, cancellationToken);
                }
                catch (OperationCanceledException op)
                {
                    Console.WriteLine($"[DownloadFileSequentially] Download operation cancelled: {op.Message} for hash: {Hash}");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DownloadFileSequentially] Error downloading piece ranges: {ex.Message} for hash: {Hash}");
                    return new DownloadFileSequentiallyResponse { Success = false, ErrorMessage = $"Error downloading piece ranges: {ex.Message}" };
                }

                int middleStart = firstBufferEnd + 1;
                int middleEnd = lastBufferStart - 1;

                if (middleStart <= middleEnd)
                {
                    Console.WriteLine($"[DownloadFileSequentially] Downloading middle section: {middleStart}-{middleEnd} for hash: {Hash}");
                    for (int batchStart = middleStart; batchStart <= middleEnd && !cancellationToken.IsCancellationRequested; batchStart += batchSize)
                    {
                        int batchEnd = Math.Min(batchStart + batchSize - 1, middleEnd);
                        await DownloadPieceBatch(batchStart, batchEnd, checkInterval, cancellationToken);
                        
                        var status = GetStatus();
                        if (status != null)
                        {
                            Console.WriteLine($"[DownloadFileSequentially] Progress: {status.Progress:P2}, Speed: {status.DownloadSpeedString} for hash: {Hash}");
                        }
                    }
                }

                var finalStatus = GetStatus();
                if (finalStatus != null && finalStatus.Progress >= 0.999)
                {
                    Console.WriteLine($"[DownloadFileSequentially] Download completed successfully with progress: {finalStatus.Progress:P2} for hash: {Hash}");
                }
                else if (finalStatus != null)
                {
                    Console.WriteLine($"[DownloadFileSequentially] Download incomplete, progress: {finalStatus.Progress:P2} for hash: {Hash}");
                }

                Console.WriteLine($"[DownloadFileSequentially] Sequential download finished for hash: {Hash}. Torrent handle preserved.");
            }

            Console.WriteLine($"[DownloadFileSequentially] Sequential download completed for hash: {Hash}");
            return new DownloadFileSequentiallyResponse { Success = true, ErrorMessage = "" };
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[DownloadFileSequentially] Download cancelled for hash: {Hash}");
            return new DownloadFileSequentiallyResponse { Success = false, ErrorMessage = "İndirme başarıyla iptal edildi." };
        }
        catch (Exception e)
        {
            Console.WriteLine($"[DownloadFileSequentially] ERROR in sequential download for hash {Hash}: {e.Message}\nStack Trace: {e.StackTrace}");
            return new DownloadFileSequentiallyResponse { Success = false, ErrorMessage = $"Error in DownloadFileSequentially: {e.Message}" };
        }
        finally
        {
            lock (_sequentialDownloadLock)
            {
                _isSequentialDownloadRunning = false;
            }
            
            if (DownloadFileSequentiallyCancellationTokenSource != null)
            {
                if (!DownloadFileSequentiallyCancellationTokenSource.IsCancellationRequested)
                {
                    DownloadFileSequentiallyCancellationTokenSource.Cancel();
                }
                try 
                {
                    DownloadFileSequentiallyCancellationTokenSource.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DownloadFileSequentially] Error disposing cancellation token: {ex.Message}");
                }
                DownloadFileSequentiallyCancellationTokenSource = null;
            }
            
            Console.WriteLine($"[DownloadFileSequentially] Cleanup completed for hash: {Hash}");
        }
    }

    public async Task DownloadFileSequentially(int fileIndex, CancellationToken cancellationToken)
    {
        try
        {
            using var _ = cancellationToken.Register(() =>
                Console.WriteLine("İndirme işlemi iptal ediliyor..."));

            var files = await GetFiles();
            if (files == null || files.Count == 0)
            {
                Console.WriteLine("No files found for torrent hash: " + Hash);
                return;
            }

            var mediaFile = files.FirstOrDefault(x => x.Index == fileIndex);

            if (mediaFile == null)
            {
                Console.WriteLine($"Media file with index {fileIndex} not found");
                return;
            }

            Console.WriteLine(mediaFile.Name + " İndiriliyor...");

            var pieceRange = await GetValidPieceRange(mediaFile.Index);
            int startIndex = pieceRange.Start;
            int endIndex = pieceRange.End;

            if (startIndex > endIndex || startIndex < 0)
            {
                Console.WriteLine($"Invalid piece range: {startIndex} to {endIndex}");
                return;
            }

            var pieces = await WaitForValidPieces(cancellationToken);
            if (pieces == null || pieces.Count() == 0)
            {
                Console.WriteLine("No valid pieces found");
                return;
            }

            ClearPiecePrioritiesExceptFile(fileIndex);
            ClearPieceDeadLines();

            int firstBufferEnd = Math.Min(startIndex + bufferPieces - 1, endIndex);
            int lastBufferStart = Math.Max(endIndex - bufferPieces + 1, startIndex);

            try
            {
                await DownloadPieceRange(startIndex, firstBufferEnd, lastBufferStart, endIndex, checkInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading piece ranges: {ex.Message}");
            }

            int middleStart = firstBufferEnd + 1;
            int middleEnd = lastBufferStart - 1;

            if (middleStart <= middleEnd)
            {
                for (int batchStart = middleStart; batchStart <= middleEnd && !cancellationToken.IsCancellationRequested; batchStart += batchSize)
                {
                    int batchEnd = Math.Min(batchStart + batchSize - 1, middleEnd);
                    await DownloadPieceBatch(batchStart, batchEnd, checkInterval, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("İndirme başarıyla iptal edildi.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error on download file: {e.Message}");
        }
    }

    private async Task DownloadPieceRange(
        int start,
        int end,
        int start2,
        int end2,
        int checkInterval,
        CancellationToken ct)
    {
        if (start < 0 || end < start || start2 < 0 || end2 < start2)
        {
            Console.WriteLine("Invalid parameters in DownloadPieceRange");
            return;
        }
        
        var piecesToDownload = new List<int>();
        var currentPieces = GetStatus().Pieces.ToList();

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

        if (piecesToDownload.Count == 0)
            return;
        
        const int baseDeadline = 5000;
        int count = 1;
        
        foreach (int piece in piecesToDownload)
        {
            ct.ThrowIfCancellationRequested();
            QueuePriorityUpdate(piece, 7, baseDeadline + (count * 500));
            count++;
        }

        await Task.Run(() =>
        {
            try
            {
                FlushPriorityUpdates();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error flushing priority updates: {ex.Message}");
            }
        });

        var lastRemainingCount = piecesToDownload.Count;
        var lastPriorityUpdate = DateTime.Now;

        while (!ct.IsCancellationRequested && piecesToDownload.Count > 0)
        {
            var status = GetStatus();
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
            
            bool priorityUpdateNeeded = remainingPieces.Count != lastRemainingCount || 
                                         (DateTime.Now - lastPriorityUpdate).TotalSeconds >= 5;
            
            if (priorityUpdateNeeded)
            {
                piecesToDownload = remainingPieces;
                count = 1;
                
                foreach (int piece in piecesToDownload)
                {
                    if (ct.IsCancellationRequested) break;

                    try
                    {
                        QueuePriorityUpdate(piece, 7, baseDeadline + (count * 500));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating piece priority: {ex.Message}");
                    }
                    count++;
                }

                try
                {
                    await Task.Run(() => FlushPriorityUpdates());
                    lastPriorityUpdate = DateTime.Now;
                    lastRemainingCount = remainingPieces.Count;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error flushing priorities: {ex.Message}");
                }
            }

            await Task.Delay(checkInterval, ct);
        }
        
        Console.WriteLine($"Completed piece range download: {start}-{end}, {start2}-{end2}");
    }

    private async Task DownloadPieceBatch(
        int start,
        int end,
        int checkInterval,
        CancellationToken ct)
    {
        if (start < 0 || end < start)
        {
            Console.WriteLine("Invalid parameters in DownloadPieceBatch");
            return;
        }

        try
        {
            var piecesToDownload = new List<int>();
            var status = GetStatus();

            if (status == null || status.Pieces == null)
            {
                Console.WriteLine("Status or pieces information is null");
                return;
            }

            var currentPieces = status.Pieces.ToList();
            if (currentPieces.Count == 0)
            {
                Console.WriteLine("No pieces information available");
                return;
            }

            for (int i = start; i <= end && i < currentPieces.Count; i++)
            {
                if (!currentPieces[i])
                {
                    piecesToDownload.Add(i);
                }
            }

            if (piecesToDownload.Count == 0)
                return;

            const int baseDeadline = 5000;
            int count = 1;
            
            foreach (int piece in piecesToDownload)
            {
                if (ct.IsCancellationRequested) return;

                try
                {
                    QueuePriorityUpdate(piece, 7, baseDeadline + (count * 500));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting initial priority: {ex.Message}");
                }
                count++;
            }

            try
            {
                await Task.Run(() => FlushPriorityUpdates());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error flushing initial priorities: {ex.Message}");
            }

            var lastRemainingCount = piecesToDownload.Count;
            var lastPriorityUpdate = DateTime.Now;
            
            while (!ct.IsCancellationRequested && piecesToDownload.Count > 0)
            {
                status = GetStatus();
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
                var newCompletedPieces = new List<int>();
                
                foreach (int p in piecesToDownload)
                {
                    if (p < currentPieces.Count)
                    {
                        if (!currentPieces[p])
                        {
                            remainingPieces.Add(p);
                        }
                        else
                        {
                            newCompletedPieces.Add(p);
                        }
                    }
                }

                if (remainingPieces.Count == 0)
                {
                    Console.WriteLine($"All pieces in batch {start}-{end} completed");
                    break;
                }
                
                bool priorityUpdateNeeded = remainingPieces.Count != lastRemainingCount || 
                                             (DateTime.Now - lastPriorityUpdate).TotalSeconds >= 5;
                
                if (priorityUpdateNeeded)
                {
                    piecesToDownload = remainingPieces;
                    
                    if (newCompletedPieces.Count > 0)
                    {
                        Console.WriteLine($"Completed {newCompletedPieces.Count} pieces in batch {start}-{end}");
                    }
                    
                    count = 1;
                    foreach (int piece in piecesToDownload)
                    {
                        if (ct.IsCancellationRequested) break;

                        try
                        {
                            QueuePriorityUpdate(piece, 7, baseDeadline + (count * 500));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating piece priority: {ex.Message}");
                        }
                        count++;
                    }

                    try
                    {
                        await Task.Run(() => FlushPriorityUpdates());
                        lastPriorityUpdate = DateTime.Now;
                        lastRemainingCount = remainingPieces.Count;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error flushing priorities: {ex.Message}");
                    }
                }

                await Task.Delay(checkInterval, ct);
            }

            if (!ct.IsCancellationRequested && piecesToDownload.Count == 0)
            {
                Console.WriteLine($"Batch {start}-{end} completed successfully");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DownloadPieceBatch: {ex.Message}");
        }
    }

    private async Task<(int Start, int End)> GetValidPieceRange(int fileIndex)
    {
        if (fileIndex < 0)
        {
            Console.WriteLine("Invalid parameters in GetValidPieceRange");
            return (-1, -1);
        }

        try
        {
            int maxRetries = 10;
            int retryCount = 0;
            var indexRange = await Task.Run(() => GetFilePieceRange(fileIndex));

            while (indexRange == null && retryCount < maxRetries)
            {
                await Task.Delay(100);
                indexRange = await Task.Run(() => GetFilePieceRange(fileIndex));
                retryCount++;
            }

            if (indexRange == null)
            {
                Console.WriteLine($"Could not get file piece range after {maxRetries} attempts");
                return (-1, -1);
            }

            return (indexRange.StartPieceIndex, indexRange.EndPieceIndex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetValidPieceRange: {ex.Message}");
            return (-1, -1);
        }
    }

    private async Task<bool[]> WaitForValidPieces(CancellationToken ct)
    {
        

        try
        {
            int maxAttempts = 50;
            int attempts = 0;

            while (!ct.IsCancellationRequested && attempts < maxAttempts)
            {
                var status = await Task.Run(() =>
                {
                    try
                    {
                        var sta = torrentHandleWrapper.GetStatus();
                        var result = new TorrentStatusInfo
                        {
                            Hash = sta.Hash,
                            Progress = sta.Progress,
                            EstimatedTime = sta.EstimatedTime,
                            DownloadSpeedString = sta.DownloadSpeedString,
                            Pieces = sta.Pieces
                        };
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting torrent status: {ex.Message}");
                        return null;
                    }
                });

                if (status != null)
                {
                    UpdateStatus(status);
                    var pieces = status.Pieces;
                    if (pieces != null && pieces.Count() > 0)
                        return pieces.ToArray();
                }

                attempts++;
                await Task.Delay(100, ct);
            }

            Console.WriteLine($"Could not get valid pieces after {maxAttempts} attempts");
            return new bool []{};
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in WaitForValidPieces: {ex.Message}");
            return new bool[] { };
        }
    }
   
 
   
    public async Task<GetAvailaibleSecondsResponse> GetAvailaibleSeconds(int durationInSeconds, int fileIndex)
    {
        List<TimeRange> pieceTimeRanges = new List<TimeRange>();

        try
        {
            var files = await GetFiles();
            if (files == null|| files.Count == 0)
            {
                Console.WriteLine("No files found for hash: ");
                return new GetAvailaibleSecondsResponse
                {
                    TimeRanges = pieceTimeRanges,
                    Success = false,
                    ErrorMessage = "No files found"
                };
            }

            var currentFile = files.FirstOrDefault(x => x.Index == fileIndex);
            if (currentFile == null)
            {
                Console.WriteLine($"File with index {fileIndex} not found");
                return new GetAvailaibleSecondsResponse
                {
                    TimeRanges = pieceTimeRanges,
                    Success = false,
                    ErrorMessage = $"File with index {fileIndex} not found"
                };
            }

            long totalFileSize = currentFile.Size;
            if (totalFileSize <= 0)
            {
                Console.WriteLine("File size is invalid: " + totalFileSize);
                return new GetAvailaibleSecondsResponse
                {
                    TimeRanges = pieceTimeRanges,
                    Success = false,
                    ErrorMessage = "File size is invalid: " + totalFileSize
                };
            }

            int totalDuration = durationInSeconds;
            long pieceSize = PieceSize;

            var statusInfo = GetStatus();
            if (statusInfo == null)
            {
                Console.WriteLine("Status or pieces information is null");
                return new GetAvailaibleSecondsResponse
                {
                    TimeRanges = pieceTimeRanges,
                    Success = false,
                    ErrorMessage = "Status or pieces information is null"
                };
            }

            var torrentPieces = statusInfo.Pieces.ToList();
            if (torrentPieces.Count == 0)
            {
                Console.WriteLine("No piece information available");
                return new GetAvailaibleSecondsResponse
                {
                    TimeRanges = pieceTimeRanges,
                    Success = false,
                    ErrorMessage = "No piece information available"
                };
            }

            var filePieceRange = await Task.Run(() => GetFilePieceRange(currentFile.Index));

            int retryCount = 1;
            while (filePieceRange == null && retryCount < 10)
            {
                filePieceRange = await Task.Run(() => GetFilePieceRange(currentFile.Index));
                retryCount++;
                await Task.Delay(100);
            }

            if (filePieceRange == null)
            {
                Console.WriteLine("Could not get file piece range after multiple attempts");
                return new GetAvailaibleSecondsResponse
                {
                    TimeRanges = pieceTimeRanges,
                    Success = false,
                    ErrorMessage = "Could not get file piece range after multiple attempts"
                };
            }

            int startIndex = filePieceRange.StartPieceIndex;
            int endIndex = filePieceRange.EndPieceIndex;

            if (startIndex < 0 || endIndex < startIndex || endIndex >= torrentPieces.Count)
            {
                Console.WriteLine(
                    $"Invalid piece indices: start={startIndex}, end={endIndex}, total pieces={torrentPieces.Count}");
                return new GetAvailaibleSecondsResponse
                {
                    TimeRanges = pieceTimeRanges,
                    Success = false,
                    ErrorMessage =
                        $"Invalid piece indices: start={startIndex}, end={endIndex}, total pieces={torrentPieces.Count}"
                };
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
                Console.WriteLine("Invalid bytes per second: " + bytesPerSecond);
                return new GetAvailaibleSecondsResponse
                {
                    TimeRanges = pieceTimeRanges,
                    Success = false,
                    ErrorMessage = "Invalid bytes per second: " + bytesPerSecond
                };
            }

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

                    pieceTimeRanges.Add(new TimeRange(){Start = start,End = end});
                }
            }

            return new GetAvailaibleSecondsResponse
            { TimeRanges = pieceTimeRanges, Success = true, ErrorMessage = "" };
        }
        catch (Exception e)
        {
            Console.WriteLine("Error in GetAvailaibleSeconds: " + e.Message);
            return new GetAvailaibleSecondsResponse
            {
                TimeRanges = pieceTimeRanges,
                Success = false,
                ErrorMessage = "Error in GetAvailaibleSeconds: " + e.Message
            };
        }
    }

    public async Task<ObserveChangesResponse> ObserveChanges(string DownloadedSpeedString, string CompletedString, string EtaString)
    {
        try
        {
            if (!await IsValid())
            {
                Console.WriteLine("Could not get valid torrent handle");
                return new ObserveChangesResponse() { ErrorMessage = "Could not get valid torrent handle", Success = false };
            }

            var status = await Task.Run(() =>
            {
                var sta = torrentHandleWrapper.GetStatus();
                var result = new TorrentStatusInfo();
                if (sta != null)
                {
                    result.Hash = sta.Hash;
                    result.Progress = sta.Progress;
                    result.EstimatedTime = sta.EstimatedTime;
                    result.DownloadSpeedString = sta.DownloadSpeedString;
                    result.Pieces = sta.Pieces;
                }
                return result;
            });

            if (status == null)
            {
                Console.WriteLine("Status is null for torrent: " + Hash);
                return new ObserveChangesResponse() { ErrorMessage = "Status is null for torrent: ", Success = false };
            }

            UpdateStatus(status);

            bool IsCompleted = status.Progress >= 1;

            if (IsCompleted)
            {
                status.Pieces = null;
                GC.Collect(0, GCCollectionMode.Optimized, false);
            }

            return new ObserveChangesResponse()
            {
                DownloadPercent = status.Progress * 100,
                DownloadSpeed = IsCompleted ? "" : DownloadedSpeedString + ": " + status.DownloadSpeedString,
                ETA = IsCompleted ? CompletedString : EtaString + ": " + status.EstimatedTime,
                ErrorMessage = "",
                Success = true,
                IsCompleted = IsCompleted
            };
        }
        catch (Exception e)
        {
            var errorMessage = $"Error in ObserveChanges: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
            Console.WriteLine(errorMessage);
            return new ObserveChangesResponse()
            {
                ErrorMessage = $"Error in ObserveChanges: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}",
                Success = false
            };
        }
    }
    public static int lastEpisodeIndex = -1;
    public async Task<ChangeEpisodeResponse> ChangeEpisodeFileToMaximalPriority(int season, int episode)
    {

        try
        {
            var files = await GetFiles();
            if (files == null || files.Count == 0)
            {
                return new ChangeEpisodeResponse()
                {
                    ErrorMessage = "No files found" ,
                    Success = false,
                    ReadyToPlay = false
                };
            }

            currentEpisodeFile = files.FirstOrDefault(x => Helper.GetSeasonNumberFromFileName(x.Name) == season &&
                                                           Helper.GetEpisodeNumberFromFileName(x.Name) == episode);
            lastEpisodeIndex = currentEpisodeFile != null ? currentEpisodeFile.Index : -1;


            CancelAllPreviousPriorities();

            if (currentEpisodeFile != null)
            {
                await SetPriorityForFile(currentEpisodeFile.Index);

                // Check if first pieces are ready for early playback
                bool readyToPlay = await CheckEpisodeReadyToPlay(currentEpisodeFile.Index);

                return new ChangeEpisodeResponse()
                {
                    ErrorMessage = "",
                    Success = true,
                    ReadyToPlay = readyToPlay
                };
            }

            return new ChangeEpisodeResponse()
            {
                ErrorMessage = "",
                Success = true,
                ReadyToPlay = false
            };
        }
        catch (Exception e)
        {
            return new ChangeEpisodeResponse()
            {
                ErrorMessage = "Error on changing priority: " + e.Message,
                Success = false,
                ReadyToPlay = false
            };
        }
    }

    private async Task<bool> CheckEpisodeReadyToPlay(int fileIndex)
    {
        try
        {
            var pieceRange = await GetFilePieceRange(fileIndex);
            if (pieceRange.StartPieceIndex < 0 || pieceRange.EndPieceIndex < 0)
            {
                return false;
            }

            var status = GetStatus();
            if (status == null || status.Pieces == null || status.Pieces.Count() == 0)
            {
                return false;
            }

            // Check if first 5 pieces are downloaded (enough to start playback)
            int piecesToCheck = Math.Min(5, pieceRange.EndPieceIndex - pieceRange.StartPieceIndex + 1);
            int downloadedPieces = 0;

            for (int i = 0; i < piecesToCheck; i++)
            {
                int pieceIndex = pieceRange.StartPieceIndex + i;
                if (pieceIndex < status.Pieces.Count() && status.Pieces.ElementAt(pieceIndex))
                {
                    downloadedPieces++;
                }
            }

            // At least 3 out of first 5 pieces should be ready
            return downloadedPieces >= 3;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking episode ready to play: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Managed kaynakları temizle
            try 
            {
                // CancellationToken kaynaklarını temizle
                if (DownloadFileSequentiallyCancellationTokenSource != null)
                {
                    if (!DownloadFileSequentiallyCancellationTokenSource.IsCancellationRequested)
                    {
                        DownloadFileSequentiallyCancellationTokenSource.Cancel();
                    }
                    DownloadFileSequentiallyCancellationTokenSource.Dispose();
                    DownloadFileSequentiallyCancellationTokenSource = null;
                }

                // Piece önceliklerini temizle
                List<TorrentPiecePriority> prioritiesToRemove;
                lock (_torrentPiecePrioritiesLock)
                {
                    prioritiesToRemove = TorrentPiecePriorities.ToList();
                    TorrentPiecePriorities.Clear();
                }

                foreach (var priority in prioritiesToRemove)
                {
                    try
                    {
                        if (priority.CancellationTokenSource != null &&
                            !priority.CancellationTokenSource.IsCancellationRequested)
                        {
                            priority.CancellationTokenSource.Cancel();
                            priority.CancellationTokenSource?.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disposing priority token: {ex.Message}");
                    }
                }
                
                // Dosya listesini temizle
                if (Files != null)
                {
                    Files.Clear();
                    Files = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing managed resources: {ex.Message}");
            }
        }

        // Unmanaged kaynakları temizle
        try
        {
            // TorrentHandleWrapper'ı temizle - bu unmanaged kaynakları içerebilir
            if (torrentHandleWrapper != null)
            {
                try
                {
                    torrentHandleWrapper.Pause();
                    torrentHandleWrapper.Dispose();
                    torrentHandleWrapper = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing torrentHandleWrapper: {ex.Message}");
                }
            }

            // Static kaynakları temizle
            TorrentStatusInfo = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing unmanaged resources: {ex.Message}");
        }

        _disposed = true;
    }

    // Finalize metodunu ekleyelim, böylece GC çalıştığında Dispose çağrılmış olacak
    ~TorrentHandle()
    {
        Dispose(false);
    }
}