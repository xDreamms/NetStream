using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NetStream;

public class AppSettings
{
    // General settings
    public string TmdbResultLanguage { get; set; } = "";
    public string IsoTmdbResultLanguage { get; set; } = "";
    public string ProgramLanguage { get; set; } = "";
    public string IsoProgramLanguage { get; set; } = "";
    public string SubtitleLanguage { get; set; } = "";
    public string IsoSubtitleLanguage { get; set; } = "";
    
    // Theme settings
    public byte? PrimaryColorAlpha { get; set; }
    public byte? PrimaryColorRed { get; set; }
    public byte? PrimaryColorGreen { get; set; }
    public byte? PrimaryColorBlue { get; set; }
    
    // API settings
    public string JacketApiUrl { get; set; } = "";
    public string JacketApiKey { get; set; } = "";
    
    // User settings
    public string FireStoreEmail { get; set; } = "";
    public string FireStorePassword { get; set; } = "";
    public string TmdbUsername { get; set; } = "";
    public string TmdbPassword { get; set; } = "";
    public bool SignedOut { get; set; } = false;
    
    // File paths
    public string YoutubeVideoPath { get; set; } = "";
    
    // Player settings
    public bool AutoSyncSubtitles { get; set; } = true;
    public bool ShowThumbnails { get; set; } = true;

    public string TorrentsPath { get; set; }
    public string MoviesPath { get; set; }
    public string SubtitlesPath { get; set; }
    public string VolumeCachePath { get; set; }
    public string downloadingTorrentsJson { get; set; }
    public string SubtitleInfoPath { get; set; }
    public string FireStoreDisplayName { get; set; }
    public string FireStoreProfilePhotoName { get; set; }
    public string OpenSubtitlesApiKey { get; set; }
    public bool Verified { get; set; }
    public string ThumbnailCachesPath { get; set; }
    public string IndexersPath { get; set; }
    public bool PlayerSettingAutoSync { get; set; }
    public bool PlayerSettingShowThumbnail { get; set; }
    
    // Watch Now settings
    public string WatchNowDefaultQuality { get; set; } = "1080p";
}