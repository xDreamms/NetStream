using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NetStream.Native;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct LtsTorrentStatusInfo
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string hash;
    public float progress;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string estimated_time;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string download_speed_string;
    public int pieces_count;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct LtsTorrentFileInfo
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
    public string name;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
    public string full_path;
    public int index;
    public long size;
    public int is_completed;
    public int is_media_file;
    public double progress;
    public int priority;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LtsFilePieceRange
{
    public int file_index;
    public int start_piece_index;
    public int end_piece_index;
}

internal static class LibTorrentNative
{
    const string DllName = "LibTorrentSharp2";

    // ===== Session Management =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr lts_session_create_from_file([MarshalAs(UnmanagedType.LPUTF8Str)] string sessionFilePath);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr lts_session_create_new([MarshalAs(UnmanagedType.LPUTF8Str)] string sessionFilePath);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_session_destroy(IntPtr session);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_session_save_state(IntPtr session, [MarshalAs(UnmanagedType.LPUTF8Str)] string filePath);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_session_load_all_torrents(IntPtr session, [MarshalAs(UnmanagedType.LPUTF8Str)] string resumeDir);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_session_is_torrents_loaded(IntPtr session);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_session_pause_all(IntPtr session);

    // ===== Torrent Addition =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_session_add_torrent_file(IntPtr session,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string torrentFile,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string savePath,
        StringBuilder outHash, int outHashSize);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_session_add_torrent_magnet(IntPtr session,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string magnetUri,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string savePath,
        StringBuilder outHash, int outHashSize);

    // ===== Torrent Lookup =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr lts_session_find_torrent(IntPtr session, [MarshalAs(UnmanagedType.LPUTF8Str)] string hash);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_session_get_torrent_count(IntPtr session);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr lts_session_get_torrent_at(IntPtr session, int index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_session_remove_torrent(IntPtr session, [MarshalAs(UnmanagedType.LPUTF8Str)] string hash, int deleteFiles);

    // ===== Torrent Handle - Properties =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_get_name(IntPtr handle, StringBuilder outBuf, int bufSize);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_get_hash(IntPtr handle, StringBuilder outBuf, int bufSize);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern float lts_torrent_get_progress(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_is_valid(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_is_paused(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_get_state(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long lts_torrent_get_piece_size(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_get_total_pieces(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long lts_torrent_get_size(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long lts_torrent_get_downloaded(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long lts_torrent_get_added_on(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_get_save_path(IntPtr handle, StringBuilder outBuf, int bufSize);

    // ===== Torrent Handle - Status =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_get_status(IntPtr handle, ref LtsTorrentStatusInfo outStatus);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_get_status_pieces(IntPtr handle, [Out] int[] outPieces, int maxPieces);

    // ===== Torrent Handle - Files =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_get_file_count(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_get_file_info(IntPtr handle, int fileIndex, ref LtsTorrentFileInfo outInfo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_get_file_piece_range(IntPtr handle, int fileIndex, ref LtsFilePieceRange outRange);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_set_file_priority(IntPtr handle, int fileIndex, int priority);

    // ===== Torrent Handle - Piece Control =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_set_piece_priority(IntPtr handle, int piece, int priority);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_set_piece_deadline(IntPtr handle, int piece, int deadlineMs);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_has_piece(IntPtr handle, int piece);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_clear_piece_deadlines(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_clear_piece_priorities_in_range(IntPtr handle, int start, int end);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_clear_piece_priorities_except_file(IntPtr handle, int fileIndex);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_reset_priority_range(IntPtr handle, int start, int end);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_reset_piece_deadline(IntPtr handle, int piece);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_queue_priority_update(IntPtr handle, int piece, int priority, int deadline);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_flush_priority_updates(IntPtr handle);

    // ===== Torrent Handle - Control =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_pause(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_resume(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_torrent_delete(IntPtr handle, int deleteFiles, int deleteResume,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string knownHash,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string knownSavePath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string knownName);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_torrent_dispose(IntPtr handle);

    // ===== Session Settings =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr lts_session_get_settings(IntPtr session);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_settings_set_int(IntPtr sp, int key, int value);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_settings_get_int(IntPtr sp, int key);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_settings_set_bool(IntPtr sp, int key, int value);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int lts_settings_get_bool(IntPtr sp, int key);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_settings_set_str(IntPtr sp, int key, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_settings_get_str(IntPtr sp, int key, StringBuilder outBuf, int bufSize);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_settings_apply(IntPtr session, IntPtr sp);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_settings_apply_defaults(IntPtr sp);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_settings_destroy(IntPtr sp);

    // ===== Alert System =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_session_start_alerts(IntPtr session, [MarshalAs(UnmanagedType.LPUTF8Str)] string resumeDir);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void lts_session_stop_alerts(IntPtr session);

    // ===== Settings Key Constants =====
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_connections_limit();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_upload_rate_limit();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_download_rate_limit();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_active_downloads();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_active_seeds();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_active_limit();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_max_peerlist_size();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_cache_size();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_send_socket_buffer_size();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_recv_socket_buffer_size();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_half_open_limit();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_unchoke_slots_limit();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_disk_io_read_mode();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_disk_io_write_mode();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_read_cache_line_size();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_cache_expiry();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_file_pool_size();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_max_queued_disk_bytes();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_alert_mask();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_in_enc_policy();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_out_enc_policy();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_allowed_enc_level();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_enable_dht();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_enable_lsd();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_enable_upnp();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_enable_natpmp();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_prefer_rc4();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_max_retry_port_bind();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_tracker_completion_timeout();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_udp_tracker_token_expiry();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_user_agent();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_handshake_client_version();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_outgoing_interfaces();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_peer_timeout();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_key_connection_speed();

    // Encryption policy constants
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_enc_pe_enabled();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_enc_pe_disabled();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_enc_pe_forced();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_enc_pe_both();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_enc_pe_rc4();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_enc_pe_plaintext();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_disk_enable_os_cache();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_disk_disable_os_cache();

    // Alert category constants
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_alert_status();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_alert_piece_progress();
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] internal static extern int lts_alert_file_progress();
}
