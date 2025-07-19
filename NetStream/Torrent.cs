using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStream
{
    public class Torrent
    {
        public string Name { get; set; }
        public double Size { get; set; }
        public string ImageUrl { get; set; }
        public string DownloadLink { get; set; }
        public string PublishDate { get; set; }
        public int MovieId { get; set; }
        public string MovieName { get; set; }
        public bool IsCompleted { get; set; }
        public int ShowType { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string TorrentLocation { get; set; }
        public int ImdbId { get; set; }
        public string Magnet { get; set; }
        public string ContainingDirectory { get; set; }
        public List<string> FileNames { get; set; }
        public string Hash { get; set; }
    }
}
