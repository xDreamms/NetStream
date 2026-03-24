using System;

namespace NetStream.Models
{
    public class TorrentResult
    {
        public string Title { get; set; }
        public long Size { get; set; }
        public int Seeders { get; set; }
        public int Peers { get; set; }
        public DateTime PublishDate { get; set; }
        public string Category { get; set; }
        public string MagnetUri { get; set; }
        public string Tracker { get; set; }
        public int FileCount { get; set; }
        
        public string FormattedSize 
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double len = Size;
                
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                
                return $"{len:0.##} {sizes[order]}";
            }
        }
    }
} 