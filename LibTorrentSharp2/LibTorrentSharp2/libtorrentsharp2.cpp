#include "libtorrentsharp2.h"

#include <libtorrent/session.hpp>
#include <libtorrent/torrent_info.hpp>
#include <libtorrent/magnet_uri.hpp>
#include <libtorrent/add_torrent_params.hpp>
#include <libtorrent/bencode.hpp>
#include <libtorrent/write_resume_data.hpp>
#include <libtorrent/read_resume_data.hpp>
#include <libtorrent/alert_types.hpp>
#include <libtorrent/session_params.hpp>
#include <libtorrent/torrent_status.hpp>

#ifndef __ANDROID__
#include <boost/filesystem.hpp>
#endif

#ifdef __ANDROID__
#include <android/log.h>
#define ANDROID_LOG_TAG "LibTorrentSharp2"
#endif

#include <string>
#include <vector>
#include <fstream>
#include <sstream>
#include <iomanip>
#include <thread>
#include <atomic>
#include <mutex>
#include <tuple>
#include <algorithm>
#include <ctime>
#include <chrono>
#include <filesystem>
#include <cstdlib>
#include <iostream>

namespace lt = libtorrent;

// ============================================================================
// Cross-platform path separator
// ============================================================================

#ifdef _WIN32
static const char PATH_SEP = '\\';
static const std::string PATH_SEP_STR = "\\";
#else
static const char PATH_SEP = '/';
static const std::string PATH_SEP_STR = "/";
#endif

// Configurable log/data directory (set by Android/caller via lts_set_log_directory)
static std::string g_log_directory;
static std::mutex g_log_dir_mutex;

// ============================================================================
// Internal data structures
// ============================================================================

struct LtsSessionData
{
    lt::session* session = nullptr;
    lt::settings_pack* settings = nullptr;
    std::atomic<bool> torrents_loaded{ false };
    std::atomic<bool> alert_stop_flag{ false };
    std::thread* alert_thread = nullptr;
    std::string resume_dir;
    std::string session_file_path;
};

struct TorrentHandleData
{
    lt::torrent_handle handle;
    std::mutex mtx;
    std::vector<std::tuple<int, int, int>> priority_queue; // piece, priority, deadline
};

// ============================================================================
// Helper functions
// ============================================================================

static std::string get_data_directory()
{
    {
        std::lock_guard<std::mutex> lock(g_log_dir_mutex);
        if (!g_log_directory.empty())
            return g_log_directory;
    }
#ifdef _WIN32
    const char* appdata = std::getenv("APPDATA");
    if (appdata)
        return std::string(appdata) + "\\NetStream";
#endif
    return "";
}

static void log_error(const std::string& msg)
{
    try
    {
#ifdef __ANDROID__
        __android_log_print(ANDROID_LOG_ERROR, ANDROID_LOG_TAG, "%s", msg.c_str());
#endif
        std::string dir = get_data_directory();
        if (dir.empty()) return;

        std::filesystem::create_directories(dir);

        std::string path = dir + PATH_SEP_STR + "libtorrent_logs.txt";
        std::ofstream out(path, std::ios::app);
        if (out.is_open())
        {
            auto now = std::chrono::system_clock::now();
            std::time_t t = std::chrono::system_clock::to_time_t(now);
            char timebuf[64];
            struct tm tm_buf;
#ifdef _WIN32
            localtime_s(&tm_buf, &t);
#else
            localtime_r(&t, &tm_buf);
#endif
            std::strftime(timebuf, sizeof(timebuf), "%Y-%m-%d %H:%M:%S", &tm_buf);
            out << timebuf << " - : " << msg << std::endl;
            out.close();
        }
    }
    catch (...)
    {
    }
}

static lt::sha1_hash hex_to_sha1(const char* hex)
{
    lt::sha1_hash hash;
    if (!hex || std::strlen(hex) != 40) return hash;

    for (size_t i = 0; i < 20; ++i)
    {
        std::string byte_str(hex + i * 2, 2);
        hash[static_cast<int>(i)] = static_cast<uint8_t>(std::stoi(byte_str, nullptr, 16));
    }
    return hash;
}

static std::string sha1_to_hex(const lt::sha1_hash& h)
{
    std::stringstream ss;
    for (auto byte : h)
    {
        ss << std::setw(2) << std::setfill('0') << std::hex << (int)(unsigned char)byte;
    }
    return ss.str();
}

static void safe_strcpy(char* dst, const char* src, int max_len)
{
    if (!dst || max_len <= 0) return;
    if (!src)
    {
        dst[0] = '\0';
        return;
    }
    int i = 0;
    for (; i < max_len - 1 && src[i] != '\0'; ++i)
    {
        dst[i] = src[i];
    }
    dst[i] = '\0';
}

static bool is_media_file(const std::string& path)
{
    if (path.empty()) return false;

    // Find the last '.' in the path
    size_t dot_pos = path.rfind('.');
    if (dot_pos == std::string::npos) return false;

    std::string ext = path.substr(dot_pos);
    // Convert to uppercase for comparison
    std::transform(ext.begin(), ext.end(), ext.begin(), ::toupper);

    return (ext == ".AVI" || ext == ".MP4" || ext == ".MOV" || ext == ".WMV" || ext == ".MKV");
}

static void apply_default_settings(lt::settings_pack* sp)
{
    if (!sp) return;

    sp->set_int(lt::settings_pack::read_cache_line_size, 32);
    sp->set_int(lt::settings_pack::send_socket_buffer_size, 512 * 1024);
    sp->set_int(lt::settings_pack::recv_socket_buffer_size, 512 * 1024);
    sp->set_int(lt::settings_pack::connections_limit, 300);
    sp->set_int(lt::settings_pack::unchoke_slots_limit, -1);
    sp->set_int(lt::settings_pack::max_peerlist_size, 1000);
    sp->set_int(lt::settings_pack::half_open_limit, 50);
    sp->set_int(lt::settings_pack::cache_size, 256);
    sp->set_int(lt::settings_pack::disk_io_write_mode, lt::settings_pack::enable_os_cache);
    sp->set_int(lt::settings_pack::disk_io_read_mode, lt::settings_pack::enable_os_cache);
    sp->set_int(lt::settings_pack::active_downloads, 20);
    sp->set_int(lt::settings_pack::tracker_completion_timeout, 30);
    sp->set_int(lt::settings_pack::udp_tracker_token_expiry, 60);
    sp->set_int(lt::settings_pack::allowed_enc_level, lt::settings_pack::pe_both);
    sp->set_bool(lt::settings_pack::prefer_rc4, true);
    sp->set_int(lt::settings_pack::out_enc_policy, lt::settings_pack::pe_enabled);
    sp->set_int(lt::settings_pack::in_enc_policy, lt::settings_pack::pe_enabled);
    sp->set_bool(lt::settings_pack::enable_dht, true);
    sp->set_bool(lt::settings_pack::enable_lsd, true);
    sp->set_bool(lt::settings_pack::enable_natpmp, true);
    sp->set_bool(lt::settings_pack::enable_upnp, true);
    sp->set_int(lt::settings_pack::max_retry_port_bind, 100);
    sp->set_int(lt::settings_pack::alert_mask,
        static_cast<int>(lt::alert_category::status
            | lt::alert_category::piece_progress
            | lt::alert_category::file_progress));
    sp->set_int(lt::settings_pack::file_pool_size, 20);
    sp->set_int(lt::settings_pack::max_queued_disk_bytes, 2 * 1024 * 1024);
    sp->set_int(lt::settings_pack::cache_expiry, 60);
}

// ============================================================================
// Session Management
// ============================================================================

LTS_API LtsSessionHandle lts_session_create_from_file(const char* session_file_path)
{
    try
    {
        if (!session_file_path)
        {
            return lts_session_create_new(session_file_path);
        }

        std::string path(session_file_path);
        if (!std::filesystem::exists(path))
        {
            log_error("Session file not found: " + path);
            return lts_session_create_new(session_file_path);
        }

        std::ifstream in(path, std::ios_base::binary);
        if (!in.is_open())
        {
            log_error("Could not open session file: " + path);
            return lts_session_create_new(session_file_path);
        }

        in.seekg(0, std::ios_base::end);
        size_t file_size = static_cast<size_t>(in.tellg());
        in.seekg(0, std::ios_base::beg);

        if (file_size == 0)
        {
            in.close();
            log_error("Session file is empty: " + path);
            return lts_session_create_new(session_file_path);
        }

        std::vector<char> buffer(file_size);
        in.read(buffer.data(), file_size);
        in.close();

        if (!in)
        {
            log_error("Failed to read session file: " + path);
            return lts_session_create_new(session_file_path);
        }

        lt::bdecode_node decoded_node;
        lt::error_code ec;
        lt::bdecode(buffer.data(), buffer.data() + buffer.size(), decoded_node, ec);

        if (ec)
        {
            log_error("Bdecode failed: " + ec.message());
            return lts_session_create_new(session_file_path);
        }

        lt::session_params session_params = lt::read_session_params(decoded_node);

        LtsSessionData* data = new LtsSessionData();
        data->settings = new lt::settings_pack(session_params.settings);
        data->session = new lt::session(session_params);
        data->session_file_path = path;

        log_error("Session successfully loaded from file.");
        return static_cast<LtsSessionHandle>(data);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_create_from_file error: ") + ex.what());
        return lts_session_create_new(session_file_path);
    }
    catch (...)
    {
        log_error("lts_session_create_from_file unknown error");
        return lts_session_create_new(session_file_path);
    }
}

LTS_API LtsSessionHandle lts_session_create_new(const char* session_file_path)
{
    try
    {
        log_error("Starting new session...");

        LtsSessionData* data = new LtsSessionData();
        data->settings = new lt::settings_pack();
        apply_default_settings(data->settings);
        data->session = new lt::session(*data->settings);

        if (session_file_path)
        {
            data->session_file_path = std::string(session_file_path);

            // Save the new session state immediately
            try
            {
                lt::session_params params = data->session->session_state(lt::save_state_flags_t::all());
                lt::entry e = lt::write_session_params(params);
                std::ofstream out(data->session_file_path, std::ios_base::binary);
                if (out.is_open())
                {
                    lt::bencode(std::ostream_iterator<char>(out), e);
                    out.close();
                }
            }
            catch (...)
            {
            }
        }

        return static_cast<LtsSessionHandle>(data);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_create_new error: ") + ex.what());
        return nullptr;
    }
    catch (...)
    {
        log_error("lts_session_create_new unknown error");
        return nullptr;
    }
}

LTS_API void lts_session_destroy(LtsSessionHandle session)
{
    try
    {
        if (!session) return;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);

        // Stop alerts
        lts_session_stop_alerts(session);

        // Save session state before destroying
        if (data->session && !data->session_file_path.empty())
        {
            try
            {
                lt::session_params params = data->session->session_state(lt::save_state_flags_t::all());
                lt::entry e = lt::write_session_params(params);
                std::ofstream out(data->session_file_path, std::ios_base::binary);
                if (out.is_open())
                {
                    lt::bencode(std::ostream_iterator<char>(out), e);
                    out.close();
                }
            }
            catch (const std::exception& ex)
            {
                log_error(std::string("Error saving session state on destroy: ") + ex.what());
            }
        }

        if (data->session)
        {
            delete data->session;
            data->session = nullptr;
        }
        if (data->settings)
        {
            delete data->settings;
            data->settings = nullptr;
        }

        delete data;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_destroy error: ") + ex.what());
    }
    catch (...)
    {
        log_error("lts_session_destroy unknown error");
    }
}

LTS_API void lts_session_save_state(LtsSessionHandle session, const char* file_path)
{
    try
    {
        if (!session || !file_path) return;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return;

        lt::session_params params = data->session->session_state(lt::save_state_flags_t::all());
        lt::entry e = lt::write_session_params(params);
        std::ofstream out(file_path, std::ios_base::binary);
        if (!out.is_open())
        {
            log_error(std::string("Cannot write to file: ") + file_path);
            return;
        }
        lt::bencode(std::ostream_iterator<char>(out), e);
        out.close();
        log_error(std::string("Session saved successfully: ") + file_path);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_save_state error: ") + ex.what());
    }
    catch (...)
    {
        log_error("lts_session_save_state unknown error");
    }
}

LTS_API void lts_session_load_all_torrents(LtsSessionHandle session, const char* resume_dir)
{
    try
    {
        if (!session || !resume_dir) return;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return;

        data->resume_dir = std::string(resume_dir);
        data->torrents_loaded = false;

        // Capture what we need for the thread
        lt::session* ses = data->session;
        std::string resume_path = data->resume_dir;
        std::atomic<bool>* loaded_flag = &data->torrents_loaded;

        std::thread([ses, resume_path, loaded_flag]()
        {
            try
            {
                if (!std::filesystem::exists(resume_path))
                {
                    log_error("Resume data directory not found: " + resume_path);
                    loaded_flag->store(true);
                    return;
                }

                std::vector<lt::add_torrent_params> torrents;

                for (const auto& entry : std::filesystem::directory_iterator(resume_path))
                {
                    if (entry.path().extension() == ".fastresume")
                    {
                        try
                        {
                            std::ifstream in_file(entry.path(), std::ios::binary);
                            if (!in_file)
                            {
                                log_error("Could not open file: " + entry.path().string());
                                continue;
                            }

                            std::vector<char> buffer(
                                (std::istreambuf_iterator<char>(in_file)),
                                std::istreambuf_iterator<char>());
                            in_file.close();

                            lt::error_code ec;
                            lt::bdecode_node node;
                            lt::bdecode(buffer.data(), buffer.data() + buffer.size(), node, ec);

                            if (ec)
                            {
                                log_error("Bdecode failed: " + ec.message());
                                continue;
                            }

                            lt::add_torrent_params params = lt::read_resume_data(node, ec);
                            if (ec)
                            {
                                log_error("read_resume_data failed: " + ec.message());
                                continue;
                            }

                            bool all_downloaded = params.unfinished_pieces.empty();

                            if (all_downloaded)
                            {
                                if (params.flags & lt::torrent_flags::need_save_resume)
                                {
                                    params.flags &= ~lt::torrent_flags::need_save_resume;
                                }

                                if (!(params.flags & lt::torrent_flags::seed_mode))
                                {
                                    params.flags |= lt::torrent_flags::seed_mode;
                                }

                                params.flags |= lt::torrent_flags::no_verify_files;
                            }

                            torrents.push_back(std::move(params));
                        }
                        catch (const std::exception& e)
                        {
                            log_error(std::string("Error loading torrent: ") + e.what());
                        }
                    }
                }

                for (auto& params : torrents)
                {
                    try
                    {
                        ses->async_add_torrent(params);
                    }
                    catch (const std::exception& e)
                    {
                        log_error(std::string("Error adding torrent: ") + e.what());
                    }
                }

                loaded_flag->store(true);
            }
            catch (const std::exception& e)
            {
                log_error(std::string("LoadAllTorrents thread error: ") + e.what());
                loaded_flag->store(true);
            }
        }).detach();
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_load_all_torrents error: ") + ex.what());
    }
    catch (...)
    {
        log_error("lts_session_load_all_torrents unknown error");
    }
}

LTS_API int32_t lts_session_is_torrents_loaded(LtsSessionHandle session)
{
    try
    {
        if (!session) return 0;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        return data->torrents_loaded.load() ? 1 : 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API void lts_session_pause_all(LtsSessionHandle session)
{
    try
    {
        if (!session) return;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return;

        std::vector<lt::torrent_handle> torrents = data->session->get_torrents();
        for (auto& torrent : torrents)
        {
            if (torrent.is_valid())
            {
                torrent.unset_flags(lt::torrent_flags::auto_managed);
                torrent.pause();
                torrent.flush_cache();
                torrent.set_upload_limit(0);
                torrent.set_download_limit(0);
            }
        }
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_pause_all error: ") + ex.what());
    }
    catch (...)
    {
        log_error("lts_session_pause_all unknown error");
    }
}

// ============================================================================
// Torrent Addition
// ============================================================================

LTS_API int32_t lts_session_add_torrent_file(LtsSessionHandle session, const char* torrent_file,
    const char* save_path, char* out_hash, int32_t out_hash_size)
{
    try
    {
        if (!session || !torrent_file || !save_path) return 0;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return 0;

        std::string torrent_path(torrent_file);
        lt::torrent_info t_info(torrent_path);

        lt::add_torrent_params p;
        p.ti = std::make_shared<lt::torrent_info>(t_info);
        p.save_path = std::string(save_path);
        p.flags |= lt::torrent_flags::need_save_resume;

        data->session->async_add_torrent(p);

        std::string hex = sha1_to_hex(t_info.info_hashes().get_best());
        if (out_hash && out_hash_size > 0)
        {
            safe_strcpy(out_hash, hex.c_str(), out_hash_size);
        }

        return 1;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_add_torrent_file error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        log_error("lts_session_add_torrent_file unknown error");
        return 0;
    }
}

LTS_API int32_t lts_session_add_torrent_magnet(LtsSessionHandle session, const char* magnet_uri,
    const char* save_path, char* out_hash, int32_t out_hash_size)
{
    try
    {
        if (!session || !magnet_uri || !save_path) return 0;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return 0;

        std::string magnet(magnet_uri);
        lt::add_torrent_params p = lt::parse_magnet_uri(magnet);

        p.save_path = std::string(save_path);
        p.flags |= lt::torrent_flags::need_save_resume;

        data->session->async_add_torrent(p);

        std::string hex = sha1_to_hex(p.info_hashes.get_best());
        if (out_hash && out_hash_size > 0)
        {
            safe_strcpy(out_hash, hex.c_str(), out_hash_size);
        }

        return 1;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_add_torrent_magnet error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        log_error("lts_session_add_torrent_magnet unknown error");
        return 0;
    }
}

// ============================================================================
// Torrent Lookup
// ============================================================================

LTS_API LtsTorrentHandle lts_session_find_torrent(LtsSessionHandle session, const char* hash)
{
    try
    {
        if (!session || !hash) return nullptr;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return nullptr;

        std::string hex_str(hash);
        std::transform(hex_str.begin(), hex_str.end(), hex_str.begin(), ::tolower);

        lt::sha1_hash h = hex_to_sha1(hex_str.c_str());
        lt::torrent_handle th = data->session->find_torrent(h);

        if (!th.is_valid())
        {
            return nullptr;
        }

        TorrentHandleData* thd = new TorrentHandleData();
        thd->handle = th;
        return static_cast<LtsTorrentHandle>(thd);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_find_torrent error: ") + ex.what());
        return nullptr;
    }
    catch (...)
    {
        log_error("lts_session_find_torrent unknown error");
        return nullptr;
    }
}

LTS_API int32_t lts_session_get_torrent_count(LtsSessionHandle session)
{
    try
    {
        if (!session) return 0;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return 0;

        return static_cast<int32_t>(data->session->get_torrents().size());
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_get_torrent_count error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API LtsTorrentHandle lts_session_get_torrent_at(LtsSessionHandle session, int32_t index)
{
    try
    {
        if (!session) return nullptr;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return nullptr;

        std::vector<lt::torrent_handle> torrents = data->session->get_torrents();
        if (index < 0 || index >= static_cast<int32_t>(torrents.size()))
        {
            return nullptr;
        }

        TorrentHandleData* thd = new TorrentHandleData();
        thd->handle = torrents[index];
        return static_cast<LtsTorrentHandle>(thd);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_get_torrent_at error: ") + ex.what());
        return nullptr;
    }
    catch (...)
    {
        return nullptr;
    }
}

LTS_API int32_t lts_session_remove_torrent(LtsSessionHandle session, const char* hash, int32_t delete_files)
{
    try
    {
        if (!session || !hash) return 0;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return 0;

        std::string hex_str(hash);
        std::transform(hex_str.begin(), hex_str.end(), hex_str.begin(), ::tolower);

        lt::sha1_hash h = hex_to_sha1(hex_str.c_str());
        lt::torrent_handle th = data->session->find_torrent(h);

        if (!th.is_valid())
        {
            log_error("lts_session_remove_torrent: Torrent handle invalid for hash");
            return 0;
        }

        if (delete_files)
        {
            data->session->remove_torrent(th, lt::session_handle::delete_files);
        }
        else
        {
            data->session->remove_torrent(th);
        }

        return 1;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_remove_torrent error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        log_error("lts_session_remove_torrent unknown error");
        return 0;
    }
}

// ============================================================================
// Torrent Handle - Properties
// ============================================================================

LTS_API void lts_torrent_get_name(LtsTorrentHandle handle, char* out_buf, int32_t buf_size)
{
    try
    {
        if (!handle || !out_buf || buf_size <= 0) return;
        out_buf[0] = '\0';

        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        lt::torrent_status status = thd->handle.status();
        safe_strcpy(out_buf, status.name.c_str(), buf_size);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_name error: ") + ex.what());
        if (out_buf && buf_size > 0) out_buf[0] = '\0';
    }
    catch (...)
    {
        if (out_buf && buf_size > 0) out_buf[0] = '\0';
    }
}

LTS_API void lts_torrent_get_hash(LtsTorrentHandle handle, char* out_buf, int32_t buf_size)
{
    try
    {
        if (!handle || !out_buf || buf_size <= 0) return;
        out_buf[0] = '\0';

        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        auto info_hash = thd->handle.info_hashes().get_best();
        std::string hex = sha1_to_hex(info_hash);
        safe_strcpy(out_buf, hex.c_str(), buf_size);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_hash error: ") + ex.what());
        if (out_buf && buf_size > 0) out_buf[0] = '\0';
    }
    catch (...)
    {
        if (out_buf && buf_size > 0) out_buf[0] = '\0';
    }
}

LTS_API float lts_torrent_get_progress(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return 0.0f;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0.0f;

        std::lock_guard<std::mutex> lock(thd->mtx);
        return thd->handle.status().progress;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_progress error: ") + ex.what());
        return 0.0f;
    }
    catch (...)
    {
        return 0.0f;
    }
}

LTS_API int32_t lts_torrent_is_valid(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        return thd->handle.is_valid() ? 1 : 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API int32_t lts_torrent_is_paused(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        return thd->handle.status().paused ? 1 : 0;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_is_paused error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API LtsTorrentState lts_torrent_get_state(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return LTS_STATE_UNKNOWN;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return LTS_STATE_UNKNOWN;

        lt::torrent_status status = thd->handle.status();

        switch (status.state)
        {
        case lt::torrent_status::checking_files:
            return LTS_STATE_CHECKING_FILES;
        case lt::torrent_status::downloading_metadata:
            return LTS_STATE_DOWNLOADING_METADATA;
        case lt::torrent_status::downloading:
            return LTS_STATE_DOWNLOADING;
        case lt::torrent_status::finished:
            return LTS_STATE_FINISHED;
        case lt::torrent_status::seeding:
            return LTS_STATE_SEEDING;
        case lt::torrent_status::allocating:
            return LTS_STATE_ALLOCATING;
        case lt::torrent_status::checking_resume_data:
            return LTS_STATE_CHECKING_RESUME_DATA;
        default:
            return LTS_STATE_UNKNOWN;
        }
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_state error: ") + ex.what());
        return LTS_STATE_UNKNOWN;
    }
    catch (...)
    {
        return LTS_STATE_UNKNOWN;
    }
}

LTS_API int64_t lts_torrent_get_piece_size(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        std::shared_ptr<const lt::torrent_info> ti = thd->handle.torrent_file();
        if (!ti) return 0;

        return ti->piece_length();
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_piece_size error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API int32_t lts_torrent_get_total_pieces(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        std::shared_ptr<const lt::torrent_info> ti = thd->handle.torrent_file();
        if (!ti) return 0;

        return ti->num_pieces();
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_total_pieces error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API int64_t lts_torrent_get_size(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        lt::torrent_status status = thd->handle.status();
        return status.total;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_size error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API int64_t lts_torrent_get_downloaded(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        lt::torrent_status status = thd->handle.status();
        return status.total_done;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_downloaded error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API int64_t lts_torrent_get_added_on(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        return static_cast<int64_t>(thd->handle.status().added_time);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_added_on error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API void lts_torrent_get_save_path(LtsTorrentHandle handle, char* out_buf, int32_t buf_size)
{
    try
    {
        if (!handle || !out_buf || buf_size <= 0) return;
        out_buf[0] = '\0';

        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        std::string save_path = thd->handle.status().save_path;
        safe_strcpy(out_buf, save_path.c_str(), buf_size);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_save_path error: ") + ex.what());
        if (out_buf && buf_size > 0) out_buf[0] = '\0';
    }
    catch (...)
    {
        if (out_buf && buf_size > 0) out_buf[0] = '\0';
    }
}

// ============================================================================
// Torrent Handle - Status
// ============================================================================

LTS_API int32_t lts_torrent_get_status(LtsTorrentHandle handle, LtsTorrentStatusInfo* out_status)
{
    try
    {
        if (!handle || !out_status) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        std::lock_guard<std::mutex> lock(thd->mtx);

        lt::torrent_status status = thd->handle.status();

        // Hash
        auto info_hash = thd->handle.info_hashes().get_best();
        std::string hex = sha1_to_hex(info_hash);
        safe_strcpy(out_status->hash, hex.c_str(), sizeof(out_status->hash));

        // Progress
        out_status->progress = status.progress;

        // ETA
        int64_t remaining_bytes = status.total_wanted - status.total_wanted_done;
        int download_rate = status.download_rate;

        int eta_seconds = -1;
        if (download_rate > 0)
        {
            eta_seconds = static_cast<int>(remaining_bytes / download_rate);
        }

        if (eta_seconds >= 0)
        {
            int hours = eta_seconds / 3600;
            int minutes = (eta_seconds % 3600) / 60;
            int seconds = eta_seconds % 60;
            char eta_buf[32];
            snprintf(eta_buf, sizeof(eta_buf), "%02d:%02d:%02d", hours, minutes, seconds);
            safe_strcpy(out_status->estimated_time, eta_buf, sizeof(out_status->estimated_time));
        }
        else
        {
            safe_strcpy(out_status->estimated_time, "00:00:00", sizeof(out_status->estimated_time));
        }

        // Download speed string
        try
        {
            double download_speed = static_cast<double>(status.download_rate);
            char speed_buf[64];

            if (download_speed >= 1e9)
            {
                snprintf(speed_buf, sizeof(speed_buf), "%.2f Gb/sec", download_speed / 1e9);
            }
            else if (download_speed >= 1e6)
            {
                snprintf(speed_buf, sizeof(speed_buf), "%.2f Mb/sec", download_speed / 1e6);
            }
            else if (download_speed >= 1e3)
            {
                snprintf(speed_buf, sizeof(speed_buf), "%.2f Kb/sec", download_speed / 1e3);
            }
            else
            {
                snprintf(speed_buf, sizeof(speed_buf), "%.2f B/sec", download_speed);
            }
            safe_strcpy(out_status->download_speed_string, speed_buf, sizeof(out_status->download_speed_string));
        }
        catch (...)
        {
            safe_strcpy(out_status->download_speed_string, "0.00 B/sec", sizeof(out_status->download_speed_string));
        }

        // Pieces count
        out_status->pieces_count = static_cast<int32_t>(status.pieces.size());

        return 1;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_status error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API int32_t lts_torrent_get_status_pieces(LtsTorrentHandle handle, int32_t* out_pieces, int32_t max_pieces)
{
    try
    {
        if (!handle || !out_pieces || max_pieces <= 0) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        std::lock_guard<std::mutex> lock(thd->mtx);

        lt::torrent_status status = thd->handle.status();
        const auto& pieces = status.pieces;
        int count = static_cast<int>(pieces.size());
        if (count > max_pieces) count = max_pieces;

        for (int i = 0; i < count; ++i)
        {
            out_pieces[i] = pieces[lt::piece_index_t(i)] ? 1 : 0;
        }

        return count;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_status_pieces error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

// ============================================================================
// Torrent Handle - Files
// ============================================================================

LTS_API int32_t lts_torrent_get_file_count(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        std::shared_ptr<const lt::torrent_info> ti = thd->handle.torrent_file();
        if (!ti || !ti->is_valid()) return 0;

        return ti->num_files();
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_file_count error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API int32_t lts_torrent_get_file_info(LtsTorrentHandle handle, int32_t file_index, LtsTorrentFileInfo* out_info)
{
    try
    {
        if (!handle || !out_info) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        std::shared_ptr<const lt::torrent_info> ti = thd->handle.torrent_file();
        if (!ti || !ti->is_valid()) return 0;

        const lt::file_storage& fs = ti->orig_files();
        if (file_index < 0 || file_index >= fs.num_files()) return 0;

        lt::file_index_t idx(file_index);

        // Get progress
        std::vector<float> progress;
        thd->handle.file_progress(progress);

        float file_progress = 0.0f;
        if (file_index < static_cast<int32_t>(progress.size()))
        {
            file_progress = progress[file_index];
        }

        // File name
        lt::string_view file_name = fs.file_name(idx);
        std::string name_str(file_name.data(), file_name.size());
        safe_strcpy(out_info->name, name_str.c_str(), sizeof(out_info->name));

        // Full path
        std::string save_path = thd->handle.status().save_path;
        std::string full_path = save_path + "/" + fs.file_path(idx);
        safe_strcpy(out_info->full_path, full_path.c_str(), sizeof(out_info->full_path));

        out_info->index = file_index;
        out_info->size = fs.file_size(idx);
        out_info->progress = static_cast<double>(file_progress);
        out_info->is_completed = (file_progress >= 1.0f) ? 1 : 0;
        out_info->is_media_file = is_media_file(full_path) ? 1 : 0;

        // Priority
        lt::download_priority_t prio = thd->handle.file_priority(idx);
        switch (static_cast<int>(static_cast<std::uint8_t>(prio)))
        {
        case 0: out_info->priority = LTS_PRIORITY_DONT_DOWNLOAD; break;
        case 1: out_info->priority = LTS_PRIORITY_LOW; break;
        case 4: out_info->priority = LTS_PRIORITY_DEFAULT; break;
        case 7: out_info->priority = LTS_PRIORITY_TOP; break;
        default: out_info->priority = LTS_PRIORITY_DEFAULT; break;
        }

        return 1;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_file_info error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API int32_t lts_torrent_get_file_piece_range(LtsTorrentHandle handle, int32_t file_index, LtsFilePieceRange* out_range)
{
    try
    {
        if (!handle || !out_range) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        std::shared_ptr<const lt::torrent_info> ti = thd->handle.torrent_file();
        if (!ti || file_index < 0 || file_index >= ti->num_files()) return 0;

        const lt::file_storage& fs = ti->files();
        lt::file_index_t idx(file_index);

        out_range->file_index = file_index;

        // First byte of file: offset=0, size=1
        lt::peer_request start_pr = fs.map_file(idx, 0, 1);
        out_range->start_piece_index = static_cast<int>(start_pr.piece);

        // Last byte of file: offset = file_size - 1, size=1
        int64_t file_size = fs.file_size(idx);
        if (file_size <= 0)
        {
            out_range->end_piece_index = out_range->start_piece_index;
            return 1;
        }
        lt::peer_request end_pr = fs.map_file(idx, file_size - 1, 1);
        out_range->end_piece_index = static_cast<int>(end_pr.piece);

        return 1;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_get_file_piece_range error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API void lts_torrent_set_file_priority(LtsTorrentHandle handle, int32_t file_index, LtsDownloadPriority priority)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;
        if (file_index < 0) return;

        lt::download_priority_t lt_priority = static_cast<lt::download_priority_t>(static_cast<int>(priority));
        thd->handle.file_priority(static_cast<lt::file_index_t>(file_index), lt_priority);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_set_file_priority error: ") + ex.what());
    }
    catch (...)
    {
        log_error("lts_torrent_set_file_priority unknown error");
    }
}

// ============================================================================
// Torrent Handle - Piece Control
// ============================================================================

LTS_API void lts_torrent_set_piece_priority(LtsTorrentHandle handle, int32_t piece, int32_t priority)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        thd->handle.piece_priority(lt::piece_index_t(piece), static_cast<lt::download_priority_t>(priority));
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_set_piece_priority error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API void lts_torrent_set_piece_deadline(LtsTorrentHandle handle, int32_t piece, int32_t deadline_ms)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        thd->handle.set_piece_deadline(lt::piece_index_t(piece), deadline_ms, {});
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_set_piece_deadline error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API int32_t lts_torrent_has_piece(LtsTorrentHandle handle, int32_t piece)
{
    try
    {
        if (!handle) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return 0;

        return thd->handle.have_piece(lt::piece_index_t(piece)) ? 1 : 0;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_has_piece error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API void lts_torrent_clear_piece_deadlines(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        thd->handle.clear_piece_deadlines();
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_clear_piece_deadlines error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API void lts_torrent_clear_piece_priorities_in_range(LtsTorrentHandle handle, int32_t start, int32_t end)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        std::shared_ptr<const lt::torrent_info> ti = thd->handle.torrent_file();
        if (!ti) return;

        int total_pieces = ti->num_pieces();
        if (start < 0 || end >= total_pieces || start > end) return;

        std::vector<std::pair<lt::piece_index_t, lt::download_priority_t>> pieces_to_update;

        for (int i = start; i <= end; ++i)
        {
            if (thd->handle.piece_priority(lt::piece_index_t(i)) != lt::download_priority_t{1})
            {
                pieces_to_update.emplace_back(lt::piece_index_t(i), lt::download_priority_t{1});
            }
        }

        if (!pieces_to_update.empty())
        {
            thd->handle.prioritize_pieces(pieces_to_update);
        }
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_clear_piece_priorities_in_range error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API void lts_torrent_clear_piece_priorities_except_file(LtsTorrentHandle handle, int32_t file_index)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        std::shared_ptr<const lt::torrent_info> ti = thd->handle.torrent_file();
        if (!ti) return;

        int total_pieces = ti->num_pieces();
        int total_files = ti->num_files();

        if (file_index < 0 || file_index >= total_files) return;

        const lt::file_storage& fs = ti->files();
        int start_piece = static_cast<int>(ti->map_file(lt::file_index_t(file_index), 0, 0).piece);
        int end_piece = static_cast<int>(ti->map_file(lt::file_index_t(file_index), fs.file_size(lt::file_index_t(file_index)) - 1, 0).piece);

        std::vector<std::pair<lt::piece_index_t, lt::download_priority_t>> pieces_to_update;

        for (int i = 0; i < total_pieces; ++i)
        {
            if (i < start_piece || i > end_piece)
            {
                if (thd->handle.piece_priority(lt::piece_index_t(i)) != lt::download_priority_t{1})
                {
                    pieces_to_update.emplace_back(lt::piece_index_t(i), lt::download_priority_t{1});
                }
            }
        }

        if (!pieces_to_update.empty())
        {
            thd->handle.prioritize_pieces(pieces_to_update);
        }
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_clear_piece_priorities_except_file error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API void lts_torrent_reset_priority_range(LtsTorrentHandle handle, int32_t start, int32_t end)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        std::vector<std::pair<lt::piece_index_t, lt::download_priority_t>> pieces;
        for (int i = start; i <= end; ++i)
        {
            pieces.emplace_back(lt::piece_index_t(i), lt::default_priority);
        }

        thd->handle.prioritize_pieces(pieces);

        for (auto& p : pieces)
        {
            thd->handle.reset_piece_deadline(p.first);
        }
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_reset_priority_range error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API void lts_torrent_reset_piece_deadline(LtsTorrentHandle handle, int32_t piece)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        thd->handle.reset_piece_deadline(lt::piece_index_t(piece));
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_reset_piece_deadline error: ") + ex.what());
    }
    catch (...)
    {
    }
}

// ============================================================================
// Batch Priority Update
// ============================================================================

LTS_API void lts_torrent_queue_priority_update(LtsTorrentHandle handle, int32_t piece, int32_t priority, int32_t deadline)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        std::lock_guard<std::mutex> lock(thd->mtx);
        thd->priority_queue.emplace_back(piece, priority, deadline);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_queue_priority_update error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API void lts_torrent_flush_priority_updates(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        std::lock_guard<std::mutex> lock(thd->mtx);

        if (thd->priority_queue.empty()) return;

        // Grab the batch
        std::vector<std::tuple<int, int, int>> batch;
        batch.swap(thd->priority_queue);

        // Get total pieces for validation
        std::shared_ptr<const lt::torrent_info> ti = thd->handle.torrent_file();
        int total_pieces = ti ? ti->num_pieces() : 0;

        std::vector<std::pair<lt::piece_index_t, lt::download_priority_t>> native_priorities;
        std::vector<std::pair<lt::piece_index_t, int>> native_deadlines;

        for (const auto& update : batch)
        {
            int piece = std::get<0>(update);
            int priority = std::get<1>(update);
            int deadline = std::get<2>(update);

            if (piece < 0 || (total_pieces > 0 && piece >= total_pieces))
            {
                continue;
            }

            if (priority < 0 || priority > 7)
            {
                continue;
            }

            if (deadline < 0 || deadline > 60000)
            {
                continue;
            }

            native_priorities.emplace_back(
                lt::piece_index_t(piece),
                static_cast<lt::download_priority_t>(priority)
            );

            native_deadlines.emplace_back(
                lt::piece_index_t(piece),
                deadline
            );
        }

        try
        {
            if (!native_priorities.empty())
            {
                thd->handle.prioritize_pieces(native_priorities);
            }

            for (const auto& dl : native_deadlines)
            {
                thd->handle.set_piece_deadline(dl.first, dl.second, {});
            }
        }
        catch (const std::exception& ex)
        {
            log_error(std::string("lts_torrent_flush_priority_updates libtorrent error: ") + ex.what());
        }
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_flush_priority_updates error: ") + ex.what());
    }
    catch (...)
    {
        log_error("lts_torrent_flush_priority_updates unknown error");
    }
}

// ============================================================================
// Torrent Handle - Control
// ============================================================================

LTS_API void lts_torrent_pause(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        thd->handle.unset_flags(lt::torrent_flags::auto_managed);
        thd->handle.pause();
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_pause error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API void lts_torrent_resume(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        if (!thd->handle.is_valid()) return;

        thd->handle.set_flags(lt::torrent_flags::auto_managed);
        thd->handle.resume();
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_resume error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API int32_t lts_torrent_delete(LtsTorrentHandle handle, int32_t delete_files, int32_t delete_resume,
    const char* known_hash, const char* known_save_path, const char* known_name)
{
    try
    {
        if (!handle) return 0;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);

        bool success = true;
        std::string hash_str;
        std::string torrent_name;
        std::string save_path;

        // Try to get info from handle if valid
        if (thd->handle.is_valid())
        {
            try
            {
                auto info_hash = thd->handle.info_hashes().get_best();
                hash_str = sha1_to_hex(info_hash);
                torrent_name = thd->handle.status().name;
                save_path = thd->handle.status().save_path;
            }
            catch (...)
            {
                // Fallback to provided args
            }
        }

        // Use provided args if handle info is missing
        if (hash_str.empty() && known_hash)
        {
            hash_str = std::string(known_hash);
        }
        if (torrent_name.empty() && known_name)
        {
            torrent_name = std::string(known_name);
        }
        if (save_path.empty() && known_save_path)
        {
            save_path = std::string(known_save_path);
        }

        if (thd->handle.is_valid())
        {
            thd->handle.pause();
            thd->handle.flush_cache();

            // Wait briefly for flush
            std::this_thread::sleep_for(std::chrono::milliseconds(100));

            // We need the session to remove the torrent.
            // Since we don't store session pointer in TorrentHandleData,
            // we rely on caller to use lts_session_remove_torrent for session removal.
            // Here we just handle the file cleanup part.
        }

        // Delete resume file
        if (delete_resume && !hash_str.empty() && !torrent_name.empty())
        {
            std::string data_dir = get_data_directory();
            if (!data_dir.empty())
            {
                std::string resume_dir = data_dir + PATH_SEP_STR + "Resume";
                std::string resume_file = resume_dir + PATH_SEP_STR + torrent_name + ".fastresume";

                if (std::filesystem::exists(resume_file))
                {
                    int retry = 0;
                    while (retry < 50)
                    {
                        try
                        {
                            std::filesystem::remove(resume_file);
                            break;
                        }
                        catch (...)
                        {
                            std::this_thread::sleep_for(std::chrono::milliseconds(200));
                            retry++;
                        }
                    }

                    if (std::filesystem::exists(resume_file))
                    {
                        success = false;
                    }
                }
            }
        }

        // Delete content files
        if (delete_files && !save_path.empty() && !torrent_name.empty())
        {
            std::string full_content_path = save_path + "/" + torrent_name;
            if (std::filesystem::exists(full_content_path))
            {
                try
                {
                    std::filesystem::remove_all(full_content_path);
                }
                catch (...)
                {
                }
            }
            if (std::filesystem::exists(full_content_path))
            {
                success = false;
            }
        }

        return success ? 1 : 0;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_delete error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        log_error("lts_torrent_delete unknown error");
        return 0;
    }
}

LTS_API void lts_torrent_dispose(LtsTorrentHandle handle)
{
    try
    {
        if (!handle) return;
        TorrentHandleData* thd = static_cast<TorrentHandleData*>(handle);
        delete thd;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_torrent_dispose error: ") + ex.what());
    }
    catch (...)
    {
    }
}

// ============================================================================
// Session Settings
// ============================================================================

LTS_API LtsSettingsPackHandle lts_session_get_settings(LtsSessionHandle session)
{
    try
    {
        if (!session) return nullptr;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        return static_cast<LtsSettingsPackHandle>(data->settings);
    }
    catch (...)
    {
        return nullptr;
    }
}

LTS_API void lts_settings_set_int(LtsSettingsPackHandle sp, int32_t key, int32_t value)
{
    try
    {
        if (!sp) return;
        lt::settings_pack* pack = static_cast<lt::settings_pack*>(sp);
        pack->set_int(key, value);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_settings_set_int error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API int32_t lts_settings_get_int(LtsSettingsPackHandle sp, int32_t key)
{
    try
    {
        if (!sp) return 0;
        lt::settings_pack* pack = static_cast<lt::settings_pack*>(sp);
        return pack->get_int(key);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_settings_get_int error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API void lts_settings_set_bool(LtsSettingsPackHandle sp, int32_t key, int32_t value)
{
    try
    {
        if (!sp) return;
        lt::settings_pack* pack = static_cast<lt::settings_pack*>(sp);
        pack->set_bool(key, value != 0);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_settings_set_bool error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API int32_t lts_settings_get_bool(LtsSettingsPackHandle sp, int32_t key)
{
    try
    {
        if (!sp) return 0;
        lt::settings_pack* pack = static_cast<lt::settings_pack*>(sp);
        return pack->get_bool(key) ? 1 : 0;
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_settings_get_bool error: ") + ex.what());
        return 0;
    }
    catch (...)
    {
        return 0;
    }
}

LTS_API void lts_settings_set_str(LtsSettingsPackHandle sp, int32_t key, const char* value)
{
    try
    {
        if (!sp || !value) return;
        lt::settings_pack* pack = static_cast<lt::settings_pack*>(sp);
        pack->set_str(key, std::string(value));
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_settings_set_str error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API void lts_settings_get_str(LtsSettingsPackHandle sp, int32_t key, char* out_buf, int32_t buf_size)
{
    try
    {
        if (!sp || !out_buf || buf_size <= 0) return;
        out_buf[0] = '\0';
        lt::settings_pack* pack = static_cast<lt::settings_pack*>(sp);
        std::string val = pack->get_str(key);
        safe_strcpy(out_buf, val.c_str(), buf_size);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_settings_get_str error: ") + ex.what());
        if (out_buf && buf_size > 0) out_buf[0] = '\0';
    }
    catch (...)
    {
        if (out_buf && buf_size > 0) out_buf[0] = '\0';
    }
}

LTS_API void lts_settings_apply(LtsSessionHandle session, LtsSettingsPackHandle sp)
{
    try
    {
        if (!session || !sp) return;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return;

        lt::settings_pack* pack = static_cast<lt::settings_pack*>(sp);
        data->session->apply_settings(*pack);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_settings_apply error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API void lts_settings_apply_defaults(LtsSettingsPackHandle sp)
{
    try
    {
        if (!sp) return;
        lt::settings_pack* pack = static_cast<lt::settings_pack*>(sp);
        apply_default_settings(pack);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_settings_apply_defaults error: ") + ex.what());
    }
    catch (...)
    {
    }
}

LTS_API void lts_settings_destroy(LtsSettingsPackHandle sp)
{
    // Note: The settings_pack is owned by LtsSessionData and will be deleted
    // in lts_session_destroy. This function is for standalone settings packs
    // that are NOT part of a session (e.g., temporary packs).
    // If the caller allocated a standalone pack, they may call this.
    // However, we do NOT delete here because the normal flow is that settings
    // are owned by the session. If the user creates an independent pack,
    // they should manage its lifetime.
    // For safety, this is a no-op if the settings pack is owned by a session.
    // The caller must be careful not to double-free.
    try
    {
        // Intentionally a no-op for session-owned settings.
        // Standalone settings packs are not currently supported.
        (void)sp;
    }
    catch (...)
    {
    }
}

// ============================================================================
// Alert System
// ============================================================================

static void alert_thread_func(LtsSessionData* data)
{
    try
    {
        while (!data->alert_stop_flag.load())
        {
            if (!data->session) break;

            try
            {
                data->session->post_torrent_updates();
                data->session->wait_for_alert(std::chrono::seconds(5));

                std::vector<lt::alert*> alerts;
                data->session->pop_alerts(&alerts);

                for (lt::alert* alert : alerts)
                {
                    if (data->alert_stop_flag.load()) break;

                    try
                    {
                        // state_update_alert - check for stuck metadata torrents
                        if (const auto* state_alert = lt::alert_cast<lt::state_update_alert>(alert))
                        {
                            for (const auto& status : state_alert->status)
                            {
                                if (status.state == lt::torrent_status::downloading_metadata)
                                {
                                    auto duration = std::chrono::duration_cast<std::chrono::minutes>(
                                        std::chrono::system_clock::now() -
                                        std::chrono::system_clock::from_time_t(status.added_time));

                                    if (duration.count() > 5)
                                    {
                                        // Remove stuck metadata torrent
                                        try
                                        {
                                            if (status.handle.is_valid())
                                            {
                                                data->session->remove_torrent(status.handle);
                                            }
                                        }
                                        catch (const std::exception& e)
                                        {
                                            log_error(std::string("Error removing invalid torrent: ") + e.what());
                                        }
                                    }
                                }
                            }
                        }

                        // piece_finished_alert - save resume data
                        if (const auto* piece_alert = lt::alert_cast<lt::piece_finished_alert>(alert))
                        {
                            auto h = piece_alert->handle;
                            if (h.is_valid())
                            {
                                try
                                {
                                    auto ti = h.torrent_file();
                                    if (ti)
                                    {
                                        h.save_resume_data(lt::torrent_handle::save_info_dict);
                                    }
                                }
                                catch (const lt::system_error& e)
                                {
                                    log_error(std::string("save_resume_data error (piece): ") + e.what());
                                }
                            }
                        }
                        // torrent_finished_alert - save resume data and pause to stop uploading
                        else if (const auto* finished_alert = lt::alert_cast<lt::torrent_finished_alert>(alert))
                        {
                            auto h = finished_alert->handle;
                            if (h.is_valid())
                            {
                                try
                                {
                                    auto ti = h.torrent_file();
                                    if (ti)
                                    {
                                        h.save_resume_data(lt::torrent_handle::save_info_dict);
                                    }
                                    // Tamamlanan torrenti durdur - upload yapmasın
                                    h.pause();
                                }
                                catch (const lt::system_error& e)
                                {
                                    log_error(std::string("save_resume_data error (finished): ") + e.what());
                                }
                            }
                        }
                        // save_resume_data_alert - write .fastresume file
                        else if (auto* rd = lt::alert_cast<lt::save_resume_data_alert>(alert))
                        {
                            try
                            {
                                std::string resume_path = data->resume_dir;
                                if (resume_path.empty())
                                {
                                    std::string data_dir = get_data_directory();
                                    if (!data_dir.empty())
                                    {
                                        resume_path = data_dir + PATH_SEP_STR + "Resume";
                                    }
                                }

                                if (!resume_path.empty())
                                {
                                    std::filesystem::create_directories(resume_path);

                                    // Ensure seed_mode flag
                                    if (!(rd->params.flags & lt::torrent_flags::seed_mode))
                                    {
                                        rd->params.flags |= lt::torrent_flags::seed_mode;
                                    }

                                    std::string file_path = resume_path + PATH_SEP_STR + rd->params.name + ".fastresume";
                                    std::ofstream out(file_path, std::ios_base::binary);
                                    if (out.is_open())
                                    {
                                        std::vector<char> buffer = lt::write_resume_data_buf(rd->params);
                                        out.write(buffer.data(), buffer.size());
                                        out.close();
                                    }
                                }
                            }
                            catch (const std::exception& e)
                            {
                                log_error(std::string("Error writing resume data: ") + e.what());
                            }
                        }
                        else if (const auto* fail_alert = lt::alert_cast<lt::save_resume_data_failed_alert>(alert))
                        {
                            log_error(std::string("Save resume failed: ") + fail_alert->error.message());
                        }
                    }
                    catch (const std::exception& e)
                    {
                        log_error(std::string("Error processing alert: ") + e.what());
                    }
                }
            }
            catch (const std::exception& e)
            {
                log_error(std::string("Alert loop iteration error: ") + e.what());
            }
        }
    }
    catch (const std::exception& e)
    {
        log_error(std::string("Alert thread fatal error: ") + e.what());
    }
}

LTS_API void lts_session_start_alerts(LtsSessionHandle session, const char* resume_dir)
{
    try
    {
        if (!session) return;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);
        if (!data->session) return;

        // Stop existing alert thread if running
        if (data->alert_thread)
        {
            lts_session_stop_alerts(session);
        }

        if (resume_dir)
        {
            data->resume_dir = std::string(resume_dir);
        }

        data->alert_stop_flag.store(false);
        data->alert_thread = new std::thread(alert_thread_func, data);
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_start_alerts error: ") + ex.what());
    }
    catch (...)
    {
        log_error("lts_session_start_alerts unknown error");
    }
}

LTS_API void lts_session_stop_alerts(LtsSessionHandle session)
{
    try
    {
        if (!session) return;
        LtsSessionData* data = static_cast<LtsSessionData*>(session);

        data->alert_stop_flag.store(true);

        if (data->alert_thread)
        {
            if (data->alert_thread->joinable())
            {
                data->alert_thread->join();
            }
            delete data->alert_thread;
            data->alert_thread = nullptr;
        }
    }
    catch (const std::exception& ex)
    {
        log_error(std::string("lts_session_stop_alerts error: ") + ex.what());
    }
    catch (...)
    {
        log_error("lts_session_stop_alerts unknown error");
    }
}

// ============================================================================
// Settings Pack Key Constants
// ============================================================================

LTS_API int32_t lts_key_connections_limit(void)
{
    return lt::settings_pack::connections_limit;
}

LTS_API int32_t lts_key_upload_rate_limit(void)
{
    return lt::settings_pack::upload_rate_limit;
}

LTS_API int32_t lts_key_download_rate_limit(void)
{
    return lt::settings_pack::download_rate_limit;
}

LTS_API int32_t lts_key_active_downloads(void)
{
    return lt::settings_pack::active_downloads;
}

LTS_API int32_t lts_key_active_seeds(void)
{
    return lt::settings_pack::active_seeds;
}

LTS_API int32_t lts_key_active_limit(void)
{
    return lt::settings_pack::active_limit;
}

LTS_API int32_t lts_key_max_peerlist_size(void)
{
    return lt::settings_pack::max_peerlist_size;
}

LTS_API int32_t lts_key_cache_size(void)
{
    return lt::settings_pack::cache_size;
}

LTS_API int32_t lts_key_send_socket_buffer_size(void)
{
    return lt::settings_pack::send_socket_buffer_size;
}

LTS_API int32_t lts_key_recv_socket_buffer_size(void)
{
    return lt::settings_pack::recv_socket_buffer_size;
}

LTS_API int32_t lts_key_half_open_limit(void)
{
    return lt::settings_pack::half_open_limit;
}

LTS_API int32_t lts_key_unchoke_slots_limit(void)
{
    return lt::settings_pack::unchoke_slots_limit;
}

LTS_API int32_t lts_key_disk_io_read_mode(void)
{
    return lt::settings_pack::disk_io_read_mode;
}

LTS_API int32_t lts_key_disk_io_write_mode(void)
{
    return lt::settings_pack::disk_io_write_mode;
}

LTS_API int32_t lts_key_read_cache_line_size(void)
{
    return lt::settings_pack::read_cache_line_size;
}

LTS_API int32_t lts_key_cache_expiry(void)
{
    return lt::settings_pack::cache_expiry;
}

LTS_API int32_t lts_key_file_pool_size(void)
{
    return lt::settings_pack::file_pool_size;
}

LTS_API int32_t lts_key_max_queued_disk_bytes(void)
{
    return lt::settings_pack::max_queued_disk_bytes;
}

LTS_API int32_t lts_key_alert_mask(void)
{
    return lt::settings_pack::alert_mask;
}

LTS_API int32_t lts_key_in_enc_policy(void)
{
    return lt::settings_pack::in_enc_policy;
}

LTS_API int32_t lts_key_out_enc_policy(void)
{
    return lt::settings_pack::out_enc_policy;
}

LTS_API int32_t lts_key_allowed_enc_level(void)
{
    return lt::settings_pack::allowed_enc_level;
}

LTS_API int32_t lts_key_enable_dht(void)
{
    return lt::settings_pack::enable_dht;
}

LTS_API int32_t lts_key_enable_lsd(void)
{
    return lt::settings_pack::enable_lsd;
}

LTS_API int32_t lts_key_enable_upnp(void)
{
    return lt::settings_pack::enable_upnp;
}

LTS_API int32_t lts_key_enable_natpmp(void)
{
    return lt::settings_pack::enable_natpmp;
}

LTS_API int32_t lts_key_prefer_rc4(void)
{
    return lt::settings_pack::prefer_rc4;
}

LTS_API int32_t lts_key_max_retry_port_bind(void)
{
    return lt::settings_pack::max_retry_port_bind;
}

LTS_API int32_t lts_key_tracker_completion_timeout(void)
{
    return lt::settings_pack::tracker_completion_timeout;
}

LTS_API int32_t lts_key_udp_tracker_token_expiry(void)
{
    return lt::settings_pack::udp_tracker_token_expiry;
}

LTS_API int32_t lts_key_user_agent(void)
{
    return lt::settings_pack::user_agent;
}

LTS_API int32_t lts_key_handshake_client_version(void)
{
    return lt::settings_pack::handshake_client_version;
}

LTS_API int32_t lts_key_outgoing_interfaces(void)
{
    return lt::settings_pack::outgoing_interfaces;
}

LTS_API int32_t lts_key_peer_timeout(void)
{
    return lt::settings_pack::peer_timeout;
}

LTS_API int32_t lts_key_connection_speed(void)
{
    return lt::settings_pack::connection_speed;
}

// ============================================================================
// Encryption Policy Constants
// ============================================================================

LTS_API int32_t lts_enc_pe_enabled(void)
{
    return lt::settings_pack::pe_enabled;
}

LTS_API int32_t lts_enc_pe_disabled(void)
{
    return lt::settings_pack::pe_disabled;
}

LTS_API int32_t lts_enc_pe_forced(void)
{
    return lt::settings_pack::pe_forced;
}

LTS_API int32_t lts_enc_pe_both(void)
{
    return lt::settings_pack::pe_both;
}

LTS_API int32_t lts_enc_pe_rc4(void)
{
    return lt::settings_pack::pe_rc4;
}

LTS_API int32_t lts_enc_pe_plaintext(void)
{
    return lt::settings_pack::pe_plaintext;
}

LTS_API int32_t lts_disk_enable_os_cache(void)
{
    return lt::settings_pack::enable_os_cache;
}

LTS_API int32_t lts_disk_disable_os_cache(void)
{
    return lt::settings_pack::disable_os_cache;
}

// ============================================================================
// Alert Category Constants
// ============================================================================

LTS_API int32_t lts_alert_status(void)
{
    return static_cast<int32_t>(lt::alert_category::status);
}

LTS_API int32_t lts_alert_piece_progress(void)
{
    return static_cast<int32_t>(lt::alert_category::piece_progress);
}

LTS_API int32_t lts_alert_file_progress(void)
{
    return static_cast<int32_t>(lt::alert_category::file_progress);
}

// ============================================================================
// Platform Configuration
// ============================================================================

LTS_API void lts_set_log_directory(const char* dir)
{
    if (!dir) return;
    std::lock_guard<std::mutex> lock(g_log_dir_mutex);
    g_log_directory = std::string(dir);
}
