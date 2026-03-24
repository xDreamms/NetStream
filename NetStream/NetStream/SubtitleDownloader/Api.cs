using subtitle_downloader.downloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubtitleDownloader
{
    public class Api
    {
        public static List<Production> GetProductions(Arguments arguments)
        {
            var api = new OpenSubtitleAPI();
            List<Production> productions =  api.searchProductions(arguments);
            return productions;
        }
    }
}
