#pragma once

#ifdef _WIN32
    #ifdef LIBTORRENTSHARP2_EXPORTS
    #define LTS_API __declspec(dllexport)
    #else
    #define LTS_API __declspec(dllimport)
    #endif
#else
    #define LTS_API __attribute__((visibility("default")))
#endif

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/* Opaque handles */
typedef void* LtsSessionHandle;
typedef void* LtsTorrentHandle;
typedef void* LtsSettingsPackHandle;

/* Enums */
typedef enum {
    LTS_STATE_CHECKING_FILES = 0,
    LTS_STATE_DOWNLOADING_METADATA = 1,
    LTS_STATE_DOWNLOADING = 2,
    LTS_STATE_FINISHED = 3,
    LTS_STATE_SEEDING = 4,
    LTS_STATE_ALLOCATING = 5,
    LTS_STATE_CHECKING_RESUME_DATA = 6,
    LTS_STATE_UNKNOWN = 7
} LtsTorrentState;

typedef enum {
    LTS_PRIORITY_DONT_DOWNLOAD = 0,
    LTS_PRIORITY_LOW = 1,
    LTS_PRIORITY_DEFAULT = 4,
    LTS_PRIORITY_TOP = 7
} LtsDownloadPriority;

/* Data structs */
typedef struct {
    char hash[64];
    float progress;
    char estimated_time[32];
    char download_speed_string[64];
    int32_t pieces_count;
} LtsTorrentStatusInfo;

typedef struct {
    char name[512];
    char full_path[1024];
    int32_t index;
    int64_t size;
    int32_t is_completed;
    int32_t is_media_file;
    double progress;
    int32_t priority;
} LtsTorrentFileInfo;

typedef struct {
    int32_t file_index;
    int32_t start_piece_index;
    int32_t end_piece_index;
} LtsFilePieceRange;

/* ===== Session Management ===== */
LTS_API LtsSessionHandle lts_session_create_from_file(const char* session_file_path);
LTS_API LtsSessionHandle lts_session_create_new(const char* session_file_path);
LTS_API void lts_session_destroy(LtsSessionHandle session);
LTS_API void lts_session_save_state(LtsSessionHandle session, const char* file_path);
LTS_API void lts_session_load_all_torrents(LtsSessionHandle session, const char* resume_dir);
LTS_API int32_t lts_session_is_torrents_loaded(LtsSessionHandle session);
LTS_API void lts_session_pause_all(LtsSessionHandle session);

/* ===== Torrent Addition ===== */
LTS_API int32_t lts_session_add_torrent_file(LtsSessionHandle session, const char* torrent_file, const char* save_path, char* out_hash, int32_t out_hash_size);
LTS_API int32_t lts_session_add_torrent_magnet(LtsSessionHandle session, const char* magnet_uri, const char* save_path, char* out_hash, int32_t out_hash_size);

/* ===== Torrent Lookup ===== */
LTS_API LtsTorrentHandle lts_session_find_torrent(LtsSessionHandle session, const char* hash);
LTS_API int32_t lts_session_get_torrent_count(LtsSessionHandle session);
LTS_API LtsTorrentHandle lts_session_get_torrent_at(LtsSessionHandle session, int32_t index);
LTS_API int32_t lts_session_remove_torrent(LtsSessionHandle session, const char* hash, int32_t delete_files);

/* ===== Torrent Handle - Properties ===== */
LTS_API void lts_torrent_get_name(LtsTorrentHandle handle, char* out_buf, int32_t buf_size);
LTS_API void lts_torrent_get_hash(LtsTorrentHandle handle, char* out_buf, int32_t buf_size);
LTS_API float lts_torrent_get_progress(LtsTorrentHandle handle);
LTS_API int32_t lts_torrent_is_valid(LtsTorrentHandle handle);
LTS_API int32_t lts_torrent_is_paused(LtsTorrentHandle handle);
LTS_API LtsTorrentState lts_torrent_get_state(LtsTorrentHandle handle);
LTS_API int64_t lts_torrent_get_piece_size(LtsTorrentHandle handle);
LTS_API int32_t lts_torrent_get_total_pieces(LtsTorrentHandle handle);
LTS_API int64_t lts_torrent_get_size(LtsTorrentHandle handle);
LTS_API int64_t lts_torrent_get_downloaded(LtsTorrentHandle handle);
LTS_API int64_t lts_torrent_get_added_on(LtsTorrentHandle handle);
LTS_API void lts_torrent_get_save_path(LtsTorrentHandle handle, char* out_buf, int32_t buf_size);

/* ===== Torrent Handle - Status ===== */
LTS_API int32_t lts_torrent_get_status(LtsTorrentHandle handle, LtsTorrentStatusInfo* out_status);
LTS_API int32_t lts_torrent_get_status_pieces(LtsTorrentHandle handle, int32_t* out_pieces, int32_t max_pieces);

/* ===== Torrent Handle - Files ===== */
LTS_API int32_t lts_torrent_get_file_count(LtsTorrentHandle handle);
LTS_API int32_t lts_torrent_get_file_info(LtsTorrentHandle handle, int32_t file_index, LtsTorrentFileInfo* out_info);
LTS_API int32_t lts_torrent_get_file_piece_range(LtsTorrentHandle handle, int32_t file_index, LtsFilePieceRange* out_range);
LTS_API void lts_torrent_set_file_priority(LtsTorrentHandle handle, int32_t file_index, LtsDownloadPriority priority);

/* ===== Torrent Handle - Piece Control ===== */
LTS_API void lts_torrent_set_piece_priority(LtsTorrentHandle handle, int32_t piece, int32_t priority);
LTS_API void lts_torrent_set_piece_deadline(LtsTorrentHandle handle, int32_t piece, int32_t deadline_ms);
LTS_API int32_t lts_torrent_has_piece(LtsTorrentHandle handle, int32_t piece);
LTS_API void lts_torrent_clear_piece_deadlines(LtsTorrentHandle handle);
LTS_API void lts_torrent_clear_piece_priorities_in_range(LtsTorrentHandle handle, int32_t start, int32_t end);
LTS_API void lts_torrent_clear_piece_priorities_except_file(LtsTorrentHandle handle, int32_t file_index);
LTS_API void lts_torrent_reset_priority_range(LtsTorrentHandle handle, int32_t start, int32_t end);
LTS_API void lts_torrent_reset_piece_deadline(LtsTorrentHandle handle, int32_t piece);

/* Batch priority update */
LTS_API void lts_torrent_queue_priority_update(LtsTorrentHandle handle, int32_t piece, int32_t priority, int32_t deadline);
LTS_API void lts_torrent_flush_priority_updates(LtsTorrentHandle handle);

/* ===== Torrent Handle - Control ===== */
LTS_API void lts_torrent_pause(LtsTorrentHandle handle);
LTS_API void lts_torrent_resume(LtsTorrentHandle handle);
LTS_API int32_t lts_torrent_delete(LtsTorrentHandle handle, int32_t delete_files, int32_t delete_resume,
    const char* known_hash, const char* known_save_path, const char* known_name);
LTS_API void lts_torrent_dispose(LtsTorrentHandle handle);

/* ===== Session Settings ===== */
LTS_API LtsSettingsPackHandle lts_session_get_settings(LtsSessionHandle session);
LTS_API void lts_settings_set_int(LtsSettingsPackHandle sp, int32_t key, int32_t value);
LTS_API int32_t lts_settings_get_int(LtsSettingsPackHandle sp, int32_t key);
LTS_API void lts_settings_set_bool(LtsSettingsPackHandle sp, int32_t key, int32_t value);
LTS_API int32_t lts_settings_get_bool(LtsSettingsPackHandle sp, int32_t key);
LTS_API void lts_settings_set_str(LtsSettingsPackHandle sp, int32_t key, const char* value);
LTS_API void lts_settings_get_str(LtsSettingsPackHandle sp, int32_t key, char* out_buf, int32_t buf_size);
LTS_API void lts_settings_apply(LtsSessionHandle session, LtsSettingsPackHandle sp);
LTS_API void lts_settings_apply_defaults(LtsSettingsPackHandle sp);
LTS_API void lts_settings_destroy(LtsSettingsPackHandle sp);

/* ===== Alert System ===== */
LTS_API void lts_session_start_alerts(LtsSessionHandle session, const char* resume_dir);
LTS_API void lts_session_stop_alerts(LtsSessionHandle session);

/* ===== Settings Pack Key Constants ===== */
LTS_API int32_t lts_key_connections_limit(void);
LTS_API int32_t lts_key_upload_rate_limit(void);
LTS_API int32_t lts_key_download_rate_limit(void);
LTS_API int32_t lts_key_active_downloads(void);
LTS_API int32_t lts_key_active_seeds(void);
LTS_API int32_t lts_key_active_limit(void);
LTS_API int32_t lts_key_max_peerlist_size(void);
LTS_API int32_t lts_key_cache_size(void);
LTS_API int32_t lts_key_send_socket_buffer_size(void);
LTS_API int32_t lts_key_recv_socket_buffer_size(void);
LTS_API int32_t lts_key_half_open_limit(void);
LTS_API int32_t lts_key_unchoke_slots_limit(void);
LTS_API int32_t lts_key_disk_io_read_mode(void);
LTS_API int32_t lts_key_disk_io_write_mode(void);
LTS_API int32_t lts_key_read_cache_line_size(void);
LTS_API int32_t lts_key_cache_expiry(void);
LTS_API int32_t lts_key_file_pool_size(void);
LTS_API int32_t lts_key_max_queued_disk_bytes(void);
LTS_API int32_t lts_key_alert_mask(void);
LTS_API int32_t lts_key_in_enc_policy(void);
LTS_API int32_t lts_key_out_enc_policy(void);
LTS_API int32_t lts_key_allowed_enc_level(void);
LTS_API int32_t lts_key_enable_dht(void);
LTS_API int32_t lts_key_enable_lsd(void);
LTS_API int32_t lts_key_enable_upnp(void);
LTS_API int32_t lts_key_enable_natpmp(void);
LTS_API int32_t lts_key_prefer_rc4(void);
LTS_API int32_t lts_key_max_retry_port_bind(void);
LTS_API int32_t lts_key_tracker_completion_timeout(void);
LTS_API int32_t lts_key_udp_tracker_token_expiry(void);
LTS_API int32_t lts_key_user_agent(void);
LTS_API int32_t lts_key_handshake_client_version(void);
LTS_API int32_t lts_key_outgoing_interfaces(void);
LTS_API int32_t lts_key_peer_timeout(void);
LTS_API int32_t lts_key_connection_speed(void);

/* Enc policy constants */
LTS_API int32_t lts_enc_pe_enabled(void);
LTS_API int32_t lts_enc_pe_disabled(void);
LTS_API int32_t lts_enc_pe_forced(void);
LTS_API int32_t lts_enc_pe_both(void);
LTS_API int32_t lts_enc_pe_rc4(void);
LTS_API int32_t lts_enc_pe_plaintext(void);
LTS_API int32_t lts_disk_enable_os_cache(void);
LTS_API int32_t lts_disk_disable_os_cache(void);

/* Alert category constants */
LTS_API int32_t lts_alert_status(void);
LTS_API int32_t lts_alert_piece_progress(void);
LTS_API int32_t lts_alert_file_progress(void);

/* Platform configuration */
LTS_API void lts_set_log_directory(const char* dir);

#ifdef __cplusplus
}
#endif
