using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStream
{
    public class PlayerCache
    {
        public int MovieId { get; set; }
        public ShowType ShowType { get; set; }
        public float LastPosition { get; set; }
        public int SeasonNumber { get; set; }
        public int  EpisodeNumber { get; set; }
        public bool DeletedTorrent { get; set; }
    }
}
