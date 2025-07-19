using System.Text;
using System.Text.RegularExpressions;
using WebScrapper.scrapper;

namespace subtitle_downloader.downloader; 

public class SubtitleScraper {
    
    public static List<SubtitleRow> ScrapeSubtitleTable(string html) {
        var doc = new HtmlDoc(html);
        Tag? tableBody = doc.Find("tbody");
        if (tableBody is null) {
            return ScrapeDownloadAnchor(doc);
        }

        var subtitles = new List<SubtitleRow>();
        List<Tag> tableRows = doc.ExtractTags(tableBody, "tr");
        foreach (var tr in tableRows) {
            var id = tr.GetAttribute("id");
            if (id is null || !id.StartsWith("name")) {
                continue;
            }
            
            var strong = doc.FindFrom("strong", tr.StartOffset + 100);
            if (strong is null) {
                continue;
            }
            SubtitleRow subtitleRow = new SubtitleRow {
                broadcastTitle = doc.ExtractText(strong)
            };
            subtitleRow.fixTitle();
            
            var flagDiv = doc.FindFrom("div", strong.EndOffset, ("class", "flag", Compare.VALUE_STARTS_WITH));
            if (flagDiv != null) {
                subtitleRow.flag = flagDiv.GetAttribute("class") ?? "";
            } else {
                subtitles.Add(subtitleRow);
                continue;
            }
            
            var downloadAnchor = doc.FindFrom("a", flagDiv.StartOffset + 10, 
                ("href", "", Compare.KEY_ONLY), ("onclick", "", Compare.KEY_ONLY));
            if (downloadAnchor is null) {
                subtitles.Add(subtitleRow);
                continue;
            }

            string href = downloadAnchor.GetAttribute("href") ?? "";
            subtitleRow.setDownloadURL(href);
            string timesDownloaded = doc.ExtractText(downloadAnchor);
            int x = timesDownloaded.IndexOf('x');
            try {
                subtitleRow.downloads = ulong.Parse(timesDownloaded[..x]);
            } catch { }

            var extensionSpan = doc.FindFrom("span", downloadAnchor.EndOffset, ("class", "p", Compare.EXACT));
            if (extensionSpan is null) {
                subtitles.Add(subtitleRow);
                continue;
            }
            subtitleRow.format = doc.ExtractText(extensionSpan);
            
            var ratingSpan = doc.FindFrom("span", extensionSpan.EndOffset, ("title", "", Compare.KEY_ONLY));
            if (ratingSpan is null) {
                subtitles.Add(subtitleRow);
                continue;
            }
            string ratingExtracted = doc.ExtractText(ratingSpan);
            try {
                subtitleRow.rating = double.Parse(ratingExtracted);
            } catch { }

            // title değerini span'den al, yoksa boş değer ata
            var titleSpan = doc.FindFrom("span", tr.StartOffset, ("title", "", Compare.KEY_ONLY));
            if (titleSpan != null && !titleSpan.GetAttribute("title").ToLower().Contains("votes"))
            {
                subtitleRow.title = titleSpan.GetAttribute("title") ?? "";
            }
            else
            {
                subtitleRow.title = ""; // veya varsayılan bir değer
            }

            // cdCount değerini al
            var tdElements = doc.ExtractTags(tr, "td");
            if (tdElements.Count > 2) // Ensure 3rd TD exists
            {
                subtitleRow.cdCount = doc.ExtractText(tdElements[2]).Trim();
            }

            // subtitleId değerini al
            var hrefAnchor = doc.FindFrom("a", tr.StartOffset, ("href", "", Compare.KEY_ONLY));
            if (hrefAnchor != null)
            {
                string href2 = hrefAnchor.GetAttribute("href") ?? "";
                var match = System.Text.RegularExpressions.Regex.Match(href2, @"subtitles/(\d+)/");
                if (match.Success)
                {
                    subtitleRow.subtitleId = Int32.Parse(match.Groups[1].Value);
                }
                else
                {
                    subtitleRow.subtitleId = -1; // Varsayılan bir değer
                }
            }

            subtitles.Add(subtitleRow);
        }

        return subtitles;
    }

    // returns the URL of the production
    public static List<Production> scrapeSearchResults(string html, bool isMovie) {
        var doc = new HtmlDoc(html);
        Tag? tableBody = doc.Find("tbody");
        if (tableBody is null) {
            return new List<Production>();
        }

        var productions = new List<Production>();
        List<Tag> tags = doc.ExtractTags(tableBody, "tr");
        foreach (var tag in tags) {
            string? id = tag.GetAttribute("id");
            if (id == null || !id.StartsWith("name")) {
                continue;
            }
            var data = doc.FindFrom("td", tag.StartOffset, ("id", "main", Compare.VALUE_STARTS_WITH));
            if (data is null) {
                continue;
            }
            var strong = doc.FindFrom("strong", data.StartOffset + 16);
            if (strong is null) {
                continue;
            }
            
            string productionName = doc.ExtractText(strong);
            
            var anchor = doc.FindFrom("a", strong.StartOffset + 6, ("class", "bnone", Compare.EXACT));
            if (anchor is null) {
                continue;
            }

            string? href = anchor.GetAttribute("href");
            if (href is null) {
                continue;
            }
            
            // Since title was extracted from the strong tag (specifically the anchor) the strong's end offset is known
            // This logic is meant to skip episode entries which follow this format: [S1E1] 
            int episodeSt = shortIndexOf(html, '[', strong.EndOffset, strong.EndOffset + 10);
            if (episodeSt != -1) {
                /*int episodeEnd = shortIndexOf(html, ']', episodeSt, episodeSt + 10);
                if (episodeEnd != -1) {
                    Console.WriteLine($"Skipping episode {html[(episodeSt+1)..episodeEnd]}");
                }*/
                continue;
            }
            var imdbAnchor = doc.FindFrom("a", tag.StartOffset, ("href", "/redirect/http://www.imdb.com/title/", Compare.VALUE_STARTS_WITH));
            string? imdbId = null;

            if (imdbAnchor != null)
            {
                string? href2 = imdbAnchor.GetAttribute("href");
                if (href2 != null)
                {
                    imdbId = ExtractImdbId(href2);
                }
            }

            var (title, year) = Production.ParseTitleYear(productionName);
            
            var production = new Production {
                name = title,
                year = year,
                id = extractUrlId(href),
                kind = isMovie ? "movie" : "tv",
                imdbId = imdbId
            };
            productions.Add(production);
        }

        return productions;
    }

    static string? ExtractImdbId(string href)
    {
        var match = Regex.Match(href, @"\/title\/(tt\d+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static uint extractUrlId(string url) {
        var idMovie = url.IndexOf("idmovie-", StringComparison.InvariantCulture);
        if (idMovie == -1) {
            return 0;
        }
        int stIndex = idMovie + 8;
        string numerical = url[stIndex..];
        return uint.TryParse(numerical, out var id) ? id : 0;
    }
    
    private static int shortIndexOf(string text, char chr, int from, int to) {
        for (int i = from; i < text.Length && i < to; i++) {
            if (text[i] == chr) {
                return i;
            }
        }
        return -1;
    }
    
    private static List<SubtitleRow> ScrapeDownloadAnchor(HtmlDoc doc) {
        Tag? downloadAnchor = doc.Find("a", 
            ("itemprop", "url", Compare.EXACT),
            ("title", "Download", Compare.EXACT),
            ("href", "", Compare.KEY_ONLY));
        if (downloadAnchor is null) {
            Console.WriteLine("Download anchor not found");
            return new List<SubtitleRow>();
        }

        SubtitleRow subtitle = new SubtitleRow();
        string href = downloadAnchor.GetAttribute("href") ?? "";
        subtitle.downloadURL = href;

        Tag? titleSpan = doc.FindFrom("span",
            downloadAnchor.StartOffset + 50,
            ("itemprop", "name", Compare.EXACT));
        
        if (titleSpan == null) {
            Console.WriteLine("No title found!");
            return new List<SubtitleRow>(1) { subtitle };
        }
        
        subtitle.broadcastTitle = doc.ExtractText(titleSpan);

        Tag? h2 = doc.FindFrom("h2", titleSpan.StartOffset + 50);
        if (h2 != null) {
            // The subtitle extension was put at the very end of this header tag for some reason
            string content = doc.ExtractText(h2);
            string[] spaceSplit = content.Split(' ');
            for (int i = spaceSplit.Length - 1; i >= 0; i--) {
                int length = spaceSplit[i].Length;
                if (length >= 2 && length <= 4) {
                    subtitle.format = spaceSplit[i].Trim().ToLower();
                    break;
                }
            }
        }

        if (h2 != null) {
            Tag? downloadedAnchor = doc.FindFrom("a", h2.EndOffset,
                ("class", "none", Compare.EXACT), ("title", "downloaded", Compare.EXACT));
            if (downloadedAnchor == null) {
                return new List<SubtitleRow>(1) { subtitle };
            }
            string times = doc.ExtractText(downloadedAnchor);
            int x = times.IndexOf('x');
            if (x != -1 && ulong.TryParse(times[..x], out ulong downloaded)) {
                subtitle.downloads = downloaded;
            }
        }
        
        return new List<SubtitleRow>(1) { subtitle };
    }

    public static List<Season> ScrapeSeriesTable(string html) {
        var doc = new HtmlDoc(html);
        Tag? tableBody = doc.Find("tbody");
        if (tableBody is null) {
            throw new Exception("<tbody> not found in the page, how's the page structured?");
        }

        List<Tag> tableRows = doc.ExtractTags(tableBody, "tr");

        List<Season> seasons = new();

        bool hasUnclassified = false;
        // 1. Scrape season packages
        foreach (var tr in tableRows) {
            Season season = new Season();
            if (tr.Attributes.Count != 0) {
                continue;
            }

            Tag? seasonTag = doc.FindFrom("span", tr.StartOffset + 2, ("id", "season", Compare.VALUE_STARTS_WITH));
            if (seasonTag is null) {
                continue;
            }

            string seasonStr = seasonTag.GetAttribute("id") ?? "";
            int dash = seasonStr.IndexOf('-');
            if (dash == -1) {
                continue;
            }

            try {
                season.number = int.Parse(seasonStr[(dash + 1)..]);
                if (season.number == -1) {
                    hasUnclassified = true;
                }
            }
            catch { }

            Tag? seasonAnchor = doc.FindFrom("a", seasonTag.StartOffset + 10, ("href", "", Compare.KEY_ONLY));
            if (seasonAnchor is null) {
                continue;
            }

            if (seasonAnchor.Attributes.Count > 1) {
                seasonAnchor = doc.FindFrom("a", seasonAnchor.StartOffset + 10, ("href", "", Compare.KEY_ONLY));
                if (seasonAnchor is null) {
                    continue;
                }
            }
            
            string url = seasonAnchor.GetAttribute("href") ?? "";
            if (url.StartsWith("/download/")) {
                season.wholeDownload = true;
                season.packageDownloadUrl = url;
            }
            seasons.Add(season);
        }

        // Console.WriteLine($"Has unclassified episodes: {hasUnclassified}");
        int seasonIndex = -1;
        // Scrape episodes
        foreach (var tr in tableRows) {
            if (tr.Attributes.Count == 0) {
                seasonIndex++;
                continue;
            }

            string? prop = tr.GetAttribute("itemprop");
            if (prop is not "episode") {
                continue;
            }

            Tag? spanEpisodeNumber = doc.FindFrom("span", tr.StartOffset + 10, ("itemprop", "episodeNumber", Compare.EXACT));
            if (spanEpisodeNumber is null) {
                continue;
            }

            Episode episode = new Episode();
            string episodeStr = doc.ExtractText(spanEpisodeNumber);
            try {
                episode.number = int.Parse(episodeStr);
            }
            catch { continue; }
            
            Tag? episodeInfo = doc.FindFrom("a", spanEpisodeNumber.StartOffset + 10, 
                ("itemprop", "url", Compare.EXACT),
                ("href", "", Compare.KEY_ONLY));
            if (episodeInfo is null || spanEpisodeNumber.EndOffset + 10 < episodeInfo.StartOffset) {
                // Extract td after tr
                Tag? td = doc.FindFrom("td", tr.StartOffset + 10);
                var dirtyName = doc.ExtractText(td ?? tr);
                int dot = dirtyName.IndexOf('.');
                if (dot != 1) {
                    episode.name = dirtyName[(dot + 2)..];
                    seasons[seasonIndex].episodes.Add(episode);
                }
                continue;
            }
            episode.url = episodeInfo.GetAttribute("href") ?? "";

            Tag? episodeName = doc.FindFrom("span", episodeInfo.StartOffset + 10,
                ("itemprop", "name", Compare.EXACT));
            if (episodeName is null) {
                continue;
            }
            episode.name = doc.ExtractText(episodeName);
            
            Season season = seasons[seasonIndex];
            season.episodes.Add(episode);
        }

        return seasons;
    }

    public static List<Language> ScrapeSubtitleLanguages(string html) {
        HtmlDoc doc = new HtmlDoc(html);
        int selectIndex = html.IndexOf("<select", StringComparison.Ordinal);
        var ul = doc.FindFrom("select", selectIndex, ("name", "SubLanguageID", Compare.EXACT), 
            ("id", "SubLanguageID", Compare.EXACT)
        );
        if (ul == null) {
            return new List<Language>();
        }
        List<Language> languages = new List<Language>(128);
        List<Tag> listElements = doc.ExtractTags(ul, "option");
        foreach (Tag option in listElements) {
            string name = doc.ExtractText(option);
            string code = option.GetAttribute("value") ?? "";
            var lang = new Language(name, code);
            languages.Add(lang);
        }
        return languages;
    }
}

public struct Language {
    private const int LANGUAGE_NAME_LENGTH = 24;
    private readonly string name;
    private readonly string code;

    public Language(string name, string code) {
        this.name = name;
        this.code = code;
    }

    public override string ToString() {
        StringBuilder builder = new StringBuilder(name);
        int diff = LANGUAGE_NAME_LENGTH - name.Length;
        for (int i = 0; i < diff; i++) {
            builder.Append(' ');
        }
        builder.Append(code);
        return builder.ToString();
    }
}

public class SubtitleRow {
    private const string DOWNLOAD_URL = "https://dl.opensubtitles.org/en/download/sub/";
    // title is either movie name or episode name
    public string broadcastTitle = "";
    public string format = "";
    public string flag = "";
    public string title;
    public string downloadURL = "";
    public string cdCount;
    public int subtitleId;
    public ulong downloads;
    public double rating;

    public void fixTitle() {
        StringBuilder fixedTitle = new(broadcastTitle);
        fixedTitle.Replace('\n', ' ');
        fixedTitle.Replace("\t", "");
        int quoteSt = indexOf(fixedTitle, '"');
        if (quoteSt != -1 && quoteSt + 1 < fixedTitle.Length && fixedTitle[quoteSt + 1] == ' ') {
            fixedTitle.Remove(quoteSt + 1, 1);
        }
        
        broadcastTitle = fixedTitle.ToString();
        broadcastTitle = broadcastTitle.Trim();
    }

    private static int indexOf(StringBuilder str, char chr) {
        for (int i = 0; i < str.Length; i++) {
            if (str[i] == chr) {
                return i;
            }
        }

        return -1;
    }
    
    public override string ToString() {
        return $"{broadcastTitle} {getDownloadURL()} format:{format} rating:{rating} downloads:{downloads}";
    }
    public string ToStringAsElement() {
        return $"{getDownloadURL()} format:{format} downloads:{downloads}";
    }

    public void setDownloadURL(string href) {
        int slash = href.LastIndexOf('/');
        if (slash == -1) {
            downloadURL = href;
        }
        string sub_id = href[(slash+1)..];
        downloadURL = $"{DOWNLOAD_URL}{sub_id}";
    }

    public string getDownloadURL() {
        return downloadURL;
    }
    
}

public class Season {
    private const string DOMAIN = "https://opensubtitles.org";
    public int number;
    public List<Episode> episodes = new();

    public bool wholeDownload;
    public string packageDownloadUrl = "";

    public override string ToString() {
        if (wholeDownload) {
            return $"S{number} {DOMAIN}{packageDownloadUrl}";
        }
        return $"S{number}";
    }
}
public class Episode {
    private const string DOMAIN = "https://opensubtitles.org";
    public int number = 0;
    public string name = "";
    public string url = "";

    public string getPageUrl() {
        return $"{DOMAIN}{url}";
    }
}