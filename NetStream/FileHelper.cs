using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace NetStream
{
    public class FileHelper
    {
        public static bool IsFirstStart=true;
        public static void CreateImportantDirectories()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"NetStream");
            
            if (!Directory.Exists(path))
            {
               // IsFirstStart = true;
                Directory.CreateDirectory(path);
            }

            string appSettingsFile = System.IO.Path.Combine(path, "appSettings.json");
            AppSettingsManager.AppSettingsPath = appSettingsFile;

            if (File.Exists(appSettingsFile))
            {
                AppSettingsManager.GetAppSettings();
            }
            else
            {
                AppSettingsManager.appSettings = new AppSettings
                {
                    TorrentsPath = "",
                    MoviesPath = "",
                    SubtitlesPath = "",
                    PlayerCachePath = "",
                    VolumeCachePath = "",
                    YoutubeVideoPath = "",
                    downloadingTorrentsJson = "",
                    SubtitleInfoPath = "",
                    TmdbResultLanguage = "",
                    ProgramLanguage = "",
                    SubtitleLanguage = "",
                    JacketApiUrl = "",
                    JacketApiKey = "",
                    IsoTmdbResultLanguage = "",
                    IsoProgramLanguage = "",
                    IsoSubtitleLanguage = "",
                    PrimaryColorAlpha = 255,
                    PrimaryColorRed = 229,
                    PrimaryColorGreen = 9,
                    PrimaryColorBlue = 20,
                    TmdbUsername = "",
                    TmdbPassword = "",
                    FireStoreEmail = "",
                    FireStorePassword = "",
                    FireStoreDisplayName = "",
                    FireStoreProfilePhotoName = "",
                    OpenSubtitlesApiKey = "",
                    SignedOut = false,
                    Verified = false,
                    ThumbnailCachesPath = "",
                    IndexersPath = "",
                    PlayerSettingAutoSync = true,
                    PlayerSettingShowThumbnail = true
                };
            }

            string torrentDownloadLocation = Path.Combine(path, "Torrents");

            if (!Directory.Exists(torrentDownloadLocation))
            {
                Directory.CreateDirectory(torrentDownloadLocation);
                AppSettingsManager.appSettings.TorrentsPath = torrentDownloadLocation;
            }
            else
            {
                AppSettingsManager.appSettings.TorrentsPath = torrentDownloadLocation;
            }

            string downloadingTorrentJson = System.IO.Path.Combine(torrentDownloadLocation, "downloadingTorrent.json");
            if (!File.Exists(downloadingTorrentJson))
            {
                File.Create(downloadingTorrentJson);
                AppSettingsManager.appSettings.downloadingTorrentsJson = downloadingTorrentJson;
            }
            else
            {
                AppSettingsManager.appSettings.downloadingTorrentsJson = downloadingTorrentJson;
            }

            if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.MoviesPath))
            {
                string moviesDownloadLocation = Path.Combine(path, "Movies");

                if (!Directory.Exists(moviesDownloadLocation))
                {
                    Directory.CreateDirectory(moviesDownloadLocation);
                    AppSettingsManager.appSettings.MoviesPath = moviesDownloadLocation;
                }
                else
                {
                    AppSettingsManager.appSettings.MoviesPath = moviesDownloadLocation;
                }
            }
            else
            {
                if (!Directory.Exists(AppSettingsManager.appSettings.MoviesPath))
                {
                    Directory.CreateDirectory(AppSettingsManager.appSettings.MoviesPath);
                }
            }
            
            string subtitlesDownloadLocation = Path.Combine(path, "Subtitles");

            if (!Directory.Exists(subtitlesDownloadLocation))
            {
                Directory.CreateDirectory(subtitlesDownloadLocation);
                AppSettingsManager.appSettings.SubtitlesPath = subtitlesDownloadLocation;
            }
            else
            {
                AppSettingsManager.appSettings.SubtitlesPath = subtitlesDownloadLocation;
            }

            string playerCacheJson = System.IO.Path.Combine(path, "playerCache.json");
            if (!File.Exists(playerCacheJson))
            {
                File.Create(playerCacheJson);
                AppSettingsManager.appSettings.PlayerCachePath = playerCacheJson;
            }
            else
            {
                AppSettingsManager.appSettings.PlayerCachePath = playerCacheJson;
            }

            string SubtitleInfo = System.IO.Path.Combine(path, "subtitleInfo.json");
            if (!File.Exists(SubtitleInfo))
            {
                File.Create(SubtitleInfo);
                AppSettingsManager.appSettings.SubtitleInfoPath = SubtitleInfo;
            }
            else
            {
                AppSettingsManager.appSettings.SubtitleInfoPath = SubtitleInfo;
            }

            string playerVolumeCache = System.IO.Path.Combine(path, "volume.txt");
            if (!File.Exists(playerVolumeCache))
            {
                File.WriteAllText(playerVolumeCache, "70");
                AppSettingsManager.appSettings.VolumeCachePath = playerVolumeCache;
            }
            else
            {
                AppSettingsManager.appSettings.VolumeCachePath = playerVolumeCache;
            }

            string youtubeVideoDownloadLoc = Path.Combine(path, "Youtube");

            if (!Directory.Exists(youtubeVideoDownloadLoc))
            {
                Directory.CreateDirectory(youtubeVideoDownloadLoc);
                AppSettingsManager.appSettings.YoutubeVideoPath = youtubeVideoDownloadLoc;
            }
            else
            {
                AppSettingsManager.appSettings.YoutubeVideoPath = youtubeVideoDownloadLoc;
            }


            string thumbnailCachesJson = System.IO.Path.Combine(path, "thumbnailCaches.json");
            if (!File.Exists(thumbnailCachesJson))
            {
                File.Create(thumbnailCachesJson);
                AppSettingsManager.appSettings.ThumbnailCachesPath = thumbnailCachesJson;
            }
            else
            {
                AppSettingsManager.appSettings.ThumbnailCachesPath = thumbnailCachesJson;
            }

            string indexersJson = System.IO.Path.Combine(path, "indexers.json");
            if (!File.Exists(indexersJson))
            {
                File.Create(indexersJson);
                AppSettingsManager.appSettings.IndexersPath = indexersJson;
            }
            else
            {
                AppSettingsManager.appSettings.IndexersPath = indexersJson;
            }

            AppSettingsManager.SaveAppSettings();
            Log.Information("Created important directories");
        }
    }
}
