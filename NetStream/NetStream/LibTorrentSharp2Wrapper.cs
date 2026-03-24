using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NetStream.Native;

namespace TorrentWrapper;

public class TorrentHandleWrapper : IDisposable
{
    internal IntPtr _handle;
    private bool _disposed;

    internal TorrentHandleWrapper(IntPtr handle)
    {
        _handle = handle;
    }

    public string Name
    {
        get
        {
            if (!IsValid) return string.Empty;
            var sb = new StringBuilder(512);
            LibTorrentNative.lts_torrent_get_name(_handle, sb, sb.Capacity);
            return sb.ToString();
        }
    }

    public string Hash
    {
        get
        {
            if (!IsValid) return string.Empty;
            var sb = new StringBuilder(64);
            LibTorrentNative.lts_torrent_get_hash(_handle, sb, sb.Capacity);
            return sb.ToString();
        }
    }

    public float Progress
    {
        get
        {
            if (!IsValid) return 0f;
            return LibTorrentNative.lts_torrent_get_progress(_handle);
        }
    }

    public bool IsValid
    {
        get
        {
            if (_disposed || _handle == IntPtr.Zero) return false;
            return LibTorrentNative.lts_torrent_is_valid(_handle) != 0;
        }
    }

    public bool IsPaused
    {
        get
        {
            if (!IsValid) return false;
            return LibTorrentNative.lts_torrent_is_paused(_handle) != 0;
        }
    }

    public NetStream.TorrentState GetTorrentState
    {
        get
        {
            if (!IsValid) return NetStream.TorrentState.Unknown;
            int state = LibTorrentNative.lts_torrent_get_state(_handle);
            return state switch
            {
                0 => NetStream.TorrentState.CheckingFiles,
                1 => NetStream.TorrentState.DownloadingMetadata,
                2 => NetStream.TorrentState.Downloading,
                3 => NetStream.TorrentState.Finished,
                4 => NetStream.TorrentState.Seeding,
                5 => NetStream.TorrentState.Allocating,
                6 => NetStream.TorrentState.CheckingResumeData,
                _ => NetStream.TorrentState.Unknown,
            };
        }
    }

    public long PieceSize
    {
        get
        {
            if (!IsValid) return 0;
            return LibTorrentNative.lts_torrent_get_piece_size(_handle);
        }
    }

    public int TotalPieces
    {
        get
        {
            if (!IsValid) return 0;
            return LibTorrentNative.lts_torrent_get_total_pieces(_handle);
        }
    }

    public DateTime AddedOn
    {
        get
        {
            if (!IsValid) return DateTime.MinValue;
            long unixTime = LibTorrentNative.lts_torrent_get_added_on(_handle);
            return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime;
        }
    }

    public long Size
    {
        get
        {
            if (!IsValid) return 0;
            return LibTorrentNative.lts_torrent_get_size(_handle);
        }
    }

    public long Downloaded
    {
        get
        {
            if (!IsValid) return 0;
            return LibTorrentNative.lts_torrent_get_downloaded(_handle);
        }
    }

    public string SavePath
    {
        get
        {
            if (!IsValid) return string.Empty;
            var sb = new StringBuilder(1024);
            LibTorrentNative.lts_torrent_get_save_path(_handle, sb, sb.Capacity);
            return sb.ToString();
        }
    }

    public NetStream.TorrentStatusInfo GetStatus()
    {
        if (!IsValid) return null;

        var nativeStatus = new LtsTorrentStatusInfo();
        int result = LibTorrentNative.lts_torrent_get_status(_handle, ref nativeStatus);
        if (result == 0) return null;

        var piecesCount = nativeStatus.pieces_count;
        bool[] pieces;
        if (piecesCount > 0)
        {
            var intPieces = new int[piecesCount];
            LibTorrentNative.lts_torrent_get_status_pieces(_handle, intPieces, piecesCount);
            pieces = new bool[piecesCount];
            for (int i = 0; i < piecesCount; i++)
                pieces[i] = intPieces[i] != 0;
        }
        else
        {
            pieces = Array.Empty<bool>();
        }

        return new NetStream.TorrentStatusInfo
        {
            Hash = nativeStatus.hash ?? string.Empty,
            Progress = nativeStatus.progress,
            EstimatedTime = nativeStatus.estimated_time ?? "00:00:00",
            DownloadSpeedString = nativeStatus.download_speed_string ?? "0.00 B/sec",
            Pieces = pieces
        };
    }

    public List<NetStream.TorrentFile> GetFiles()
    {
        var files = new List<NetStream.TorrentFile>();
        if (!IsValid) return files;

        int fileCount = LibTorrentNative.lts_torrent_get_file_count(_handle);
        if (fileCount <= 0) return files;

        for (int i = 0; i < fileCount; i++)
        {
            var nativeInfo = new LtsTorrentFileInfo();
            if (LibTorrentNative.lts_torrent_get_file_info(_handle, i, ref nativeInfo) != 0)
            {
                files.Add(new NetStream.TorrentFile
                {
                    Index = nativeInfo.index,
                    Name = nativeInfo.name ?? string.Empty,
                    FullPath = nativeInfo.full_path ?? string.Empty,
                    Size = nativeInfo.size,
                    Progress = nativeInfo.progress,
                    IsCompleted = nativeInfo.is_completed != 0,
                    IsMediaFile = nativeInfo.is_media_file != 0,
                    DownloadPriority = (NetStream.DownloadPriority)nativeInfo.priority
                });
            }
        }

        return files;
    }

    public NetStream.FilePieceRange GetFilePieceRange(int fileIndex)
    {
        if (!IsValid) return new NetStream.FilePieceRange();

        var nativeRange = new LtsFilePieceRange();
        if (LibTorrentNative.lts_torrent_get_file_piece_range(_handle, fileIndex, ref nativeRange) != 0)
        {
            return new NetStream.FilePieceRange
            {
                FileIndex = nativeRange.file_index,
                StartPieceIndex = nativeRange.start_piece_index,
                EndPieceIndex = nativeRange.end_piece_index
            };
        }

        return new NetStream.FilePieceRange();
    }

    public void SetFilePriority(int fileIndex, NetStream.DownloadPriority priority)
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_set_file_priority(_handle, fileIndex, (int)priority);
    }

    public void SetPiecePriority(int i, int priority)
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_set_piece_priority(_handle, i, priority);
    }

    public void SetPieceDeadline(int i, int duration)
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_set_piece_deadline(_handle, i, duration);
    }

    public bool HasPiece(int index)
    {
        if (!IsValid) return false;
        return LibTorrentNative.lts_torrent_has_piece(_handle, index) != 0;
    }

    public void ClearPieceDeadLines()
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_clear_piece_deadlines(_handle);
    }

    public void ClearPiecePrioritiesInRange(int startIndex, int endIndex)
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_clear_piece_priorities_in_range(_handle, startIndex, endIndex);
    }

    public void ClearPiecePrioritiesExceptFile(int fileIndex)
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_clear_piece_priorities_except_file(_handle, fileIndex);
    }

    public void ResetPriorityRange(int start, int end)
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_reset_priority_range(_handle, start, end);
    }

    public void ResetPieceDeadline(int i)
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_reset_piece_deadline(_handle, i);
    }

    public void QueuePriorityUpdate(int piece, int priority, int deadline)
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_queue_priority_update(_handle, piece, priority, deadline);
    }

    public void FlushPriorityUpdates()
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_flush_priority_updates(_handle);
    }

    public void Pause()
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_pause(_handle);
    }

    public void Resume()
    {
        if (!IsValid) return;
        LibTorrentNative.lts_torrent_resume(_handle);
    }

    public bool Delete(bool deleteFiles, bool deleteResume, string knownHash, string knownSavePath, string knownName)
    {
        return LibTorrentNative.lts_torrent_delete(
            _handle,
            deleteFiles ? 1 : 0,
            deleteResume ? 1 : 0,
            knownHash,
            knownSavePath,
            knownName) != 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_handle != IntPtr.Zero)
        {
            LibTorrentNative.lts_torrent_dispose(_handle);
            _handle = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }

    ~TorrentHandleWrapper()
    {
        Dispose();
    }

    public override bool Equals(object obj)
    {
        if (obj is TorrentHandleWrapper other)
            return string.Equals(Hash, other.Hash, StringComparison.Ordinal);
        return false;
    }

    public override int GetHashCode() => Hash?.GetHashCode() ?? 0;
}

public class Client
{
    private IntPtr _session;
    public bool disposed = false;
    public static bool IsTorrentsLoaded
    {
        get
        {
            if (_instance == null || _instance._session == IntPtr.Zero) return false;
            return LibTorrentNative.lts_session_is_torrents_loaded(_instance._session) != 0;
        }
        set { /* kept for compatibility */ }
    }

    private static Client _instance;
    internal static Client Instance => _instance;

    public Client(string pass)
    {
        if (pass != "lG!o0)%]?M85Q`57FZqzqf4U|t1@@") return;

        _instance = this;
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string netStreamPath = Path.Combine(appDataPath, "NetStream");
        string sessionFilePath = Path.Combine(netStreamPath, "torrent_session.session");
        string resumePath = Path.Combine(netStreamPath, "Resume");

        Directory.CreateDirectory(netStreamPath);
        Directory.CreateDirectory(resumePath);

        if (File.Exists(sessionFilePath) && new FileInfo(sessionFilePath).Length > 0)
        {
            _session = LibTorrentNative.lts_session_create_from_file(sessionFilePath);
        }

        if (_session == IntPtr.Zero)
        {
            _session = LibTorrentNative.lts_session_create_new(sessionFilePath);
        }

        if (_session != IntPtr.Zero)
        {
            LibTorrentNative.lts_session_load_all_torrents(_session, resumePath);
        }
    }

    public string AddTorrentFromFile(string torrentFile, string savePath)
    {
        if (disposed || _session == IntPtr.Zero) return string.Empty;
        var sb = new StringBuilder(64);
        int result = LibTorrentNative.lts_session_add_torrent_file(_session, torrentFile, savePath, sb, sb.Capacity);
        return result != 0 ? sb.ToString() : string.Empty;
    }

    public string AddTorrentFromMagnet(string magnetUri, string savePath)
    {
        if (disposed || _session == IntPtr.Zero) return string.Empty;
        var sb = new StringBuilder(64);
        int result = LibTorrentNative.lts_session_add_torrent_magnet(_session, magnetUri, savePath, sb, sb.Capacity);
        return result != 0 ? sb.ToString() : string.Empty;
    }

    public TorrentHandleWrapper FindTorrent(string hash)
    {
        if (disposed || _session == IntPtr.Zero || string.IsNullOrEmpty(hash)) return null;
        IntPtr handle = LibTorrentNative.lts_session_find_torrent(_session, hash);
        return handle != IntPtr.Zero ? new TorrentHandleWrapper(handle) : null;
    }

    public List<TorrentHandleWrapper> GetTorrents()
    {
        var result = new List<TorrentHandleWrapper>();
        if (disposed || _session == IntPtr.Zero) return result;

        int count = LibTorrentNative.lts_session_get_torrent_count(_session);
        for (int i = 0; i < count; i++)
        {
            IntPtr handle = LibTorrentNative.lts_session_get_torrent_at(_session, i);
            if (handle != IntPtr.Zero)
            {
                result.Add(new TorrentHandleWrapper(handle));
            }
        }
        return result;
    }

    public Task<List<TorrentHandleWrapper>> GetTorrentsAsync()
    {
        return Task.Run(() => GetTorrents());
    }

    public bool RemoveTorrent(string hash)
    {
        if (disposed || _session == IntPtr.Zero) return false;
        return LibTorrentNative.lts_session_remove_torrent(_session, hash, 1) != 0;
    }

    public void PauseAllTorrents()
    {
        if (disposed || _session == IntPtr.Zero) return;
        LibTorrentNative.lts_session_pause_all(_session);
    }

    public void Clear()
    {
        if (_session != IntPtr.Zero)
        {
            disposed = true;
            Console.WriteLine("Session siliniyor...");
            LibTorrentNative.lts_session_destroy(_session);
            _session = IntPtr.Zero;
            Console.WriteLine("Session silindi...");
        }
    }

    internal IntPtr SessionHandle => _session;
}

public class SessionManager
{
    private Client _client;

    public Action<string> OnDownloadedPiece;
    public Action<string> OnFinishedTorrent;
    public Action<string> OnAddedTorrent;
    public Action<List<string>> OnStatusUpdate;

    public SessionManager()
    {
    }

    public void StartListeningAlerts()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string resumePath = Path.Combine(appDataPath, "NetStream", "Resume");
        Directory.CreateDirectory(resumePath);

        var client = Client.Instance;
        if (client != null && client.SessionHandle != IntPtr.Zero)
        {
            LibTorrentNative.lts_session_start_alerts(client.SessionHandle, resumePath);
        }
    }

    public void StopListeningAlerts()
    {
        var client = Client.Instance;
        if (client != null && client.SessionHandle != IntPtr.Zero)
        {
            LibTorrentNative.lts_session_stop_alerts(client.SessionHandle);
        }
    }
}

public class SessionSettings
{
    private static IntPtr _sessionHandle;
    private static IntPtr _settingsHandle;

    private static void EnsureHandles()
    {
        if (_sessionHandle == IntPtr.Zero)
        {
            var client = Client.Instance;
            if (client != null)
            {
                _sessionHandle = client.SessionHandle;
                if (_sessionHandle != IntPtr.Zero)
                    _settingsHandle = LibTorrentNative.lts_session_get_settings(_sessionHandle);
            }
        }
    }

    private static int GetInt(int key)
    {
        EnsureHandles();
        if (_settingsHandle == IntPtr.Zero) return 0;
        return LibTorrentNative.lts_settings_get_int(_settingsHandle, key);
    }

    private static void SetInt(int key, int value)
    {
        EnsureHandles();
        if (_settingsHandle == IntPtr.Zero) return;
        LibTorrentNative.lts_settings_set_int(_settingsHandle, key, value);
        if (_sessionHandle != IntPtr.Zero)
            LibTorrentNative.lts_settings_apply(_sessionHandle, _settingsHandle);
    }

    private static bool GetBool(int key)
    {
        EnsureHandles();
        if (_settingsHandle == IntPtr.Zero) return false;
        return LibTorrentNative.lts_settings_get_bool(_settingsHandle, key) != 0;
    }

    private static void SetBool(int key, bool value)
    {
        EnsureHandles();
        if (_settingsHandle == IntPtr.Zero) return;
        LibTorrentNative.lts_settings_set_bool(_settingsHandle, key, value ? 1 : 0);
        if (_sessionHandle != IntPtr.Zero)
            LibTorrentNative.lts_settings_apply(_sessionHandle, _settingsHandle);
    }

    // Int properties
    public static int ConnectionLimit { get => GetInt(LibTorrentNative.lts_key_connections_limit()); set => SetInt(LibTorrentNative.lts_key_connections_limit(), value); }
    public static int DownloadRateLimit { get => GetInt(LibTorrentNative.lts_key_download_rate_limit()); set => SetInt(LibTorrentNative.lts_key_download_rate_limit(), value); }
    public static int UploadRateLimit { get => GetInt(LibTorrentNative.lts_key_upload_rate_limit()); set => SetInt(LibTorrentNative.lts_key_upload_rate_limit(), value); }
    public static int CacheSize { get => GetInt(LibTorrentNative.lts_key_cache_size()); set => SetInt(LibTorrentNative.lts_key_cache_size(), value); }
    public static int MaxPeerListSize { get => GetInt(LibTorrentNative.lts_key_max_peerlist_size()); set => SetInt(LibTorrentNative.lts_key_max_peerlist_size(), value); }
    public static int UnchokeSlotsLimit { get => GetInt(LibTorrentNative.lts_key_unchoke_slots_limit()); set => SetInt(LibTorrentNative.lts_key_unchoke_slots_limit(), value); }
    public static int send_socket_buffer_size { get => GetInt(LibTorrentNative.lts_key_send_socket_buffer_size()); set => SetInt(LibTorrentNative.lts_key_send_socket_buffer_size(), value); }
    public static int recv_socket_buffer_size { get => GetInt(LibTorrentNative.lts_key_recv_socket_buffer_size()); set => SetInt(LibTorrentNative.lts_key_recv_socket_buffer_size(), value); }
    public static int half_open_limit { get => GetInt(LibTorrentNative.lts_key_half_open_limit()); set => SetInt(LibTorrentNative.lts_key_half_open_limit(), value); }
    public static int active_downloads { get => GetInt(LibTorrentNative.lts_key_active_downloads()); set => SetInt(LibTorrentNative.lts_key_active_downloads(), value); }
    public static int read_cache_line_size { get => GetInt(LibTorrentNative.lts_key_read_cache_line_size()); set => SetInt(LibTorrentNative.lts_key_read_cache_line_size(), value); }
    public static int disk_io_read_mode { get => GetInt(LibTorrentNative.lts_key_disk_io_read_mode()); set => SetInt(LibTorrentNative.lts_key_disk_io_read_mode(), value); }
    public static int disk_io_write_mode { get => GetInt(LibTorrentNative.lts_key_disk_io_write_mode()); set => SetInt(LibTorrentNative.lts_key_disk_io_write_mode(), value); }
    public static int connections_limit { get => GetInt(LibTorrentNative.lts_key_connections_limit()); set => SetInt(LibTorrentNative.lts_key_connections_limit(), value); }
    public static int in_enc_policy { get => GetInt(LibTorrentNative.lts_key_in_enc_policy()); set => SetInt(LibTorrentNative.lts_key_in_enc_policy(), value); }
    public static int out_enc_policy { get => GetInt(LibTorrentNative.lts_key_out_enc_policy()); set => SetInt(LibTorrentNative.lts_key_out_enc_policy(), value); }
    public static int allowed_enc_level { get => GetInt(LibTorrentNative.lts_key_allowed_enc_level()); set => SetInt(LibTorrentNative.lts_key_allowed_enc_level(), value); }

    // Bool properties
    public static bool EnableDht { get => GetBool(LibTorrentNative.lts_key_enable_dht()); set => SetBool(LibTorrentNative.lts_key_enable_dht(), value); }
    public static bool enable_dht { get => GetBool(LibTorrentNative.lts_key_enable_dht()); set => SetBool(LibTorrentNative.lts_key_enable_dht(), value); }
    public static bool enable_lsd { get => GetBool(LibTorrentNative.lts_key_enable_lsd()); set => SetBool(LibTorrentNative.lts_key_enable_lsd(), value); }
    public static bool enable_natpmp { get => GetBool(LibTorrentNative.lts_key_enable_natpmp()); set => SetBool(LibTorrentNative.lts_key_enable_natpmp(), value); }
    public static bool enable_upnp { get => GetBool(LibTorrentNative.lts_key_enable_upnp()); set => SetBool(LibTorrentNative.lts_key_enable_upnp(), value); }
    public static bool PreferRc4 { get => GetBool(LibTorrentNative.lts_key_prefer_rc4()); set => SetBool(LibTorrentNative.lts_key_prefer_rc4(), value); }

    public static void DefaultSettings()
    {
        EnsureHandles();
        if (_settingsHandle != IntPtr.Zero)
            LibTorrentNative.lts_settings_apply_defaults(_settingsHandle);
    }

    public static void ResetToDefaults()
    {
        DefaultSettings();
        EnsureHandles();
        if (_sessionHandle != IntPtr.Zero && _settingsHandle != IntPtr.Zero)
            LibTorrentNative.lts_settings_apply(_sessionHandle, _settingsHandle);
    }

    public static void ApplyToSession()
    {
        EnsureHandles();
        if (_sessionHandle != IntPtr.Zero && _settingsHandle != IntPtr.Zero)
            LibTorrentNative.lts_settings_apply(_sessionHandle, _settingsHandle);
    }
}
