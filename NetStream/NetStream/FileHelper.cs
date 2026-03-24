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
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetStream");
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string appSettingsFile = System.IO.Path.Combine(path, "appSettings.json");
            AppSettingsManager.AppSettingsPath = appSettingsFile;

            AppSettingsManager.LoadAppSettings();
            AppSettingsManager.ApplyEnvironmentOverrides();

            if (AppSettingsManager.appSettings == null)
            {
                AppSettingsManager.InitializeDefaultSettings();
            }

            CreateAndSetDirectory(path, "Torrents", (dir) => AppSettingsManager.appSettings.TorrentsPath = dir);
            
            string downloadingTorrentJson = System.IO.Path.Combine(AppSettingsManager.appSettings.TorrentsPath, "downloadingTorrent.json");
            CreateAndSetFile(downloadingTorrentJson, "", (file) => AppSettingsManager.appSettings.downloadingTorrentsJson = file);
            
            if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.MoviesPath))
            {
                CreateAndSetDirectory(path, "Movies", (dir) => AppSettingsManager.appSettings.MoviesPath = dir);
            }
            else if (!Directory.Exists(AppSettingsManager.appSettings.MoviesPath))
            {
                Directory.CreateDirectory(AppSettingsManager.appSettings.MoviesPath);
            }
            
            CreateAndSetDirectory(path, "Subtitles", (dir) => AppSettingsManager.appSettings.SubtitlesPath = dir);
            
            CreateAndSetFile(Path.Combine(path, "subtitleInfo.json"), "", (file) => AppSettingsManager.appSettings.SubtitleInfoPath = file);
            CreateAndSetFile(Path.Combine(path, "volume.txt"), "70", (file) => AppSettingsManager.appSettings.VolumeCachePath = file);
            
            CreateAndSetDirectory(path, "Youtube", (dir) => AppSettingsManager.appSettings.YoutubeVideoPath = dir);
            
            CreateAndSetFile(Path.Combine(path, "thumbnailCaches.json"), "", (file) => AppSettingsManager.appSettings.ThumbnailCachesPath = file);
            CreateAndSetFile(Path.Combine(path, "indexers.json"), "", (file) => AppSettingsManager.appSettings.IndexersPath = file);

            AppSettingsManager.SaveAppSettings();
        }

        private static void CreateAndSetDirectory(string basePath, string dirName, Action<string> setter)
        {
            string dirPath = Path.Combine(basePath, dirName);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            setter(dirPath);
        }

        private static void CreateAndSetFile(string filePath, string defaultContent, Action<string> setter)
        {
            if (!File.Exists(filePath))
            {
                if (!string.IsNullOrEmpty(defaultContent))
                {
                    File.WriteAllText(filePath, defaultContent);
                }
                else
                {
                    File.Create(filePath).Close(); // Make sure to close the file
                }
            }
            setter(filePath);
        }
    }
}
