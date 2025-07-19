using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStream
{
    public class AppSettings
    {
        public string TorrentsPath { get; set; }
        public string MoviesPath { get; set; }
        public string SubtitlesPath { get; set; }
        public string PlayerCachePath { get; set; }
        public string VolumeCachePath { get; set; }
        public string YoutubeVideoPath { get; set; }
        public string downloadingTorrentsJson { get; set; }
        public string SubtitleInfoPath { get; set; }
        public string TmdbResultLanguage { get; set; }
        public string ProgramLanguage { get; set; }
        public string SubtitleLanguage { get; set; }
        public string JacketApiUrl { get; set; }
        public string JacketApiKey { get; set; }
        public string IsoTmdbResultLanguage { get; set; }
        public string IsoProgramLanguage { get; set; }
        public string IsoSubtitleLanguage { get; set; }
        public double PrimaryColorAlpha { get; set; }
        public double PrimaryColorRed { get; set; }
        public double PrimaryColorGreen { get; set; }
        public double PrimaryColorBlue { get; set; }

        public string TmdbUsername { get; set; }
        public string TmdbPassword { get; set; }
        public string FireStoreEmail { get; set; }
        public string FireStorePassword { get; set; }
        public string FireStoreDisplayName { get; set; }
        public string FireStoreProfilePhotoName { get; set; }
        public string OpenSubtitlesApiKey { get; set; }
        public bool SignedOut { get; set; }
        public bool Verified { get; set; }
        public string ThumbnailCachesPath { get; set; }
        public string IndexersPath { get; set; }
        public bool PlayerSettingAutoSync { get; set; }
        public bool PlayerSettingShowThumbnail { get; set; }
        //public string InstallerPage { get; set; }
    }
}
