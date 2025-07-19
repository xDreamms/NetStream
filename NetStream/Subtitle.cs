using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using CountryFlags;
using subtitle_downloader.downloader;

namespace NetStream
{
    public class Subtitle
    {
        public int MovieId { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string Name { get; set; }
        public string Fullpath { get; set; }
        public bool HashDownload { get; set; }
        public string Language { get; set; }
        public bool Synchronized { get; set; }
        public int? SubtitleId { get; set; }
        
        public ImageSource? Country { get; set; }

        public int? FileId { get; set; }
        public string? FileName { get; set; }

        public string? DownloadCount { get; set; }
        public string? Votes { get; set; }
        public string? Ratings { get; set; }

        public string Name2 { get; set; }
        public DateTime PublishDate { get; set; }
        public bool IsOrg { get; set; }

        public string ImdbId { get; set; }

        public string DownloadUrl { get; set; }
    }
}
