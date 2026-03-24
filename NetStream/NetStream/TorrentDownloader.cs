using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStream
{
    public class TorrentDownloader
    {
        public Item Torrent { get; set; }
        //public TorrentManager manager { get; set; }
        public string TorrentPath { get; set; }
        public string Magnet { get; set; }
    }
}
