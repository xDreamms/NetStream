using System.IO;
using System.Text;

namespace subtitle_downloader.downloader; 

public struct Arguments {
    private static readonly string[] SEASON_FLAGS   = {"-s", "-S", "--season"};
    private static readonly string[] EPISODE_FLAGS  = {"-e", "-E", "--episode"};
    private static readonly string[] YEAR_FLAGS     = {"-y", "--year"};
    private static readonly string[] LANGUAGE_FLAGS = {"--lang"};
    private static readonly string[] EXTENSION_FILTER_FLAGS = {"--filter"};
    private static readonly string[] LIST_FLAGS = {"-ls", "--list"};
    private static readonly string[] SKIP_SELECT_FLAGS = {"--skip-select"};
    private static readonly string[] EXTRACT_ARGS_FLAGS = {"--extract"};
    private static readonly string[] FROM_SUBTITLE_FLAGS = {"--from", "--subtitle"};
    private static readonly string[] SHIFT_FLAGS = { "--shift" };
    private static readonly string[] CONVERT_FLAGS = { "--to", "--convert-to" };
    private static readonly string[] OUTPUT_FLAGS = {"--dest", "--out"};
    private static readonly string[] HELP_FLAGS = {"-h", "-help", "--help"};
   
    private static readonly string[] SUBTITLE_FORMATS = { 
        "srt", "ssa", "vtt", "aqt", "gsub", "jss", "sub", "ttxt", "pjs", 
        "psb", "rt", "smi", "stl", "ssf", "ass", "sbv", "usf", "idx"
    };
    private const int MIN_YEAR = 1900;
    private const int MAX_SEASONS = 50;
    private const int MAX_EPISODES = 25000;
    // 12 hour limits
    private const int BACKWARD_SHIFT_CAP = -12 * 60 * 60 * 1000;
    private const int FORWARD_SHIFT_CAP = 12 * 60 * 60 * 1000;

    public string title = "";
    public string language = "all";
    public string extensionFilter = "";
    public string outputDirectory = ".";
    public string subtitlePath = "";
    public string convertToExtension = "";
    
    public uint season = 0;
    public uint episode = 0;
    public uint year = 0;

    public int shiftMs = 0;
    
    public bool subtitleFromFile = false;
    public bool convert = false;

    public bool isMovie = true;
    public bool listSeries = false;
    public bool skipSelect = false;
    
    private bool providedSeason = false;
    private bool providedEpisode = false;

    public string imdbId;

    public Arguments() {
    }
    
    public static Arguments Parse(string[] args) {
        var arguments = new Arguments();
        bool isTitleSet = false;
        
        for (int i = 0; i < args.Length; i++) {
            string currentArg = args[i];
            int seasonIndex = StartsWithIndex(currentArg, SEASON_FLAGS);
            if (seasonIndex != -1) {
                arguments.isMovie = false;
                string key = SEASON_FLAGS[seasonIndex];
                bool isAdjacent = currentArg.Length > key.Length;
                
                uint value;
                switch (isAdjacent) {
                    case true:
                        string numerical = currentArg[key.Length..];
                        if (uint.TryParse(numerical, out value)) {
                            arguments.season = value;
                            arguments.providedSeason = true;
                        }
                        else {
                            Utils.FailExit("Failed to parse (adjacent) season number!");
                        }
                        break;
                    case false:
                        bool hasNext = i + 1 < args.Length;
                        if (hasNext && uint.TryParse(args[i + 1], out value)) {
                            arguments.season = value;
                            arguments.providedSeason = true;
                            i++;
                        }
                        else {
                            Utils.FailExit("Failed to parse (separate) season number!");
                        }
                        break;
                }
                continue;
            }
            
            int episodeIndex = StartsWithIndex(currentArg, EPISODE_FLAGS);
            if (episodeIndex != -1) {
                arguments.isMovie = false;
                string key = EPISODE_FLAGS[episodeIndex];
                bool isAdjacent = currentArg.Length > key.Length;
                
                uint value;
                switch (isAdjacent) {
                    case true:
                        string numerical = currentArg[key.Length..];
                        if (uint.TryParse(numerical, out value)) {
                            arguments.episode = value;
                            arguments.providedEpisode = true;
                        }
                        else {
                            Utils.FailExit("Failed to parse (adjacent) episode number!");
                        }
                        break;
                    case false:
                        bool hasNext = i + 1 < args.Length;
                        if (hasNext && uint.TryParse(args[i + 1], out value)) {
                            arguments.episode = value;
                            arguments.providedEpisode = true;
                            i++;
                        }
                        else {
                            Utils.FailExit("Failed to parse (separate) episode number!");
                        }
                        break;
                }
                continue;
            }
            
            int yearIndex = StartsWithIndex(currentArg, YEAR_FLAGS);
            if (yearIndex != -1) {
                string key = YEAR_FLAGS[yearIndex];
                bool isAdjacent = currentArg.Length > key.Length;
                
                uint value;
                switch (isAdjacent) {
                    case true:
                        string numerical = currentArg[key.Length..];
                        if (uint.TryParse(numerical, out value)) {
                            arguments.year = value;
                        }
                        else {
                            Utils.FailExit("Failed to parse (adjacent) year number!");
                        }
                        break;
                    case false:
                        bool hasNext = i + 1 < args.Length;
                        if (hasNext && uint.TryParse(args[i + 1], out value)) {
                            arguments.year = value;
                            i++;
                        }
                        else {
                            Utils.FailExit("Failed to parse (separate) year number!");
                        }
                        break;
                }
                continue;
            }
            
            if (EqualsAny(currentArg, LANGUAGE_FLAGS)) {
                bool hasNext = i + 1 < args.Length;
                if (!hasNext) {
                    Utils.FailExit("The language argument wasn't provided. Help: --lang <language>");
                }
                
                arguments.language = args[i + 1];
                i++;
                continue;
            }
            
            if (EqualsAny(currentArg, EXTENSION_FILTER_FLAGS)) {
                bool hasNext = i + 1 < args.Length;
                if (!hasNext) {
                    Utils.FailExit("No extension provided. Usage: --filter <extension>");
                }
                string ext = args[i + 1].ToLower();
                if (ext.Length < 2) {
                    Console.WriteLine("Extension name is too short to match any known subtitle format, skipping!");
                    i++;
                    continue;
                }
                if (ext.StartsWith('.')) {
                    ext = ext[1..];
                }
                arguments.extensionFilter = ext;
                i++;                
                continue;
            }

            if (EqualsAny(currentArg, LIST_FLAGS)) {
                arguments.listSeries = true;
                continue;
            }
            
            if (EqualsAny(currentArg, SKIP_SELECT_FLAGS)) {
                arguments.skipSelect = true;
                continue;
            }

            if (EqualsAny(currentArg, EXTRACT_ARGS_FLAGS)) {
                bool hasNext = i + 1 < args.Length;
                if (!hasNext) {
                    Utils.FailExit("A path to file was expected. Help: --extract <path>");
                }

                string path = args[i + 1];
                i++;
                parseFilename(Path.GetFileNameWithoutExtension(path), ref arguments);
                continue;
            }
            
            if (EqualsAny(currentArg, FROM_SUBTITLE_FLAGS)) {
                bool hasNext = i + 1 < args.Length;
                if (!hasNext) {
                    Utils.FailExit("A path to a subtitle file was expected. Help: --from <path>");
                }
                string path = args[i + 1];
                i++;
                arguments.subtitleFromFile = true;
                arguments.subtitlePath = path;
                continue;
            }
            
            if (EqualsAny(currentArg, SHIFT_FLAGS)) {
                bool hasNext = i + 1 < args.Length;
                if (hasNext && int.TryParse(args[i + 1], out int shiftMs)) {
                    arguments.shiftMs = shiftMs;
                    i++;
                }
                else {
                    Utils.FailExit("Milliseconds [+/-] were expected as the next argument. Help: --shift <ms>");
                }
                continue;
            }
            
            if (EqualsAny(currentArg, CONVERT_FLAGS)) {
                bool hasNext = i + 1 < args.Length;
                if (!hasNext) {
                    Utils.FailExit("A path to a subtitle file was expected. Help: --convert-to <extension>");
                }
                string ext = args[i + 1].ToLower();
                if (ext.Length < 2) {
                    Console.WriteLine("Extension name is too short to match any known subtitle format, skipping!");
                    i++;
                    continue;
                }
                if (ext.StartsWith('.')) {
                    ext = ext[1..];
                }
                
                i++;
                arguments.convert = true;
                arguments.convertToExtension = ext;
                continue;
            }

            if (EqualsAny(currentArg, OUTPUT_FLAGS)) {
                bool hasNext = i + 1 < args.Length;
                if (!hasNext) {
                    Utils.FailExit("An argument was expected. Help: --out <directory_path>");
                }
                string outputPath = args[i + 1];
                i++;
                arguments.outputDirectory = outputPath;
                continue;
            }
            
            if (EqualsAny(currentArg, HELP_FLAGS)) {
                Environment.Exit(0);
            }
            
            if (currentArg.StartsWith('-')) {
                Console.WriteLine($"Unrecognized argument identifier: {currentArg}");
                continue;
            }
            if (!isTitleSet) {
                arguments.title = currentArg;
                isTitleSet = true;
            }
        }

        return arguments;
    }

    private static void parseFilename(string filename, ref Arguments subtitle) {
        StringBuilder title = new StringBuilder();
        string[] parts;
        if (filename.Contains('.')) {
            // assume dot format
            parts = filename.Split('.');
        }
        else if (filename.Contains('-')) {
            parts = filename.Split('-');
        }
        else {
            parts = filename.Split(' ');
        }

        bool appendingTitle = true;
        foreach (var part in parts) {
            if (part.EndsWith("0p")) {
                appendingTitle = false;
                continue;
            }

            if (part.StartsWith("x264") || part.StartsWith("x265")) {
                appendingTitle = false;
                continue;
            }

            if (part.Length >= 4 && (part[0] == 'S' || part[0] == 's') && char.IsDigit(part[1]) &&
                char.IsDigit(part[^1])) {
                int episodeIndex = part.IndexOf('e', 2);
                if (episodeIndex == -1) {
                    episodeIndex = part.IndexOf('E', 2);
                }

                string seasonStr = part[1..episodeIndex];
                if (!uint.TryParse(seasonStr, out var season)) {
                    Console.WriteLine("Failed to parse season number");
                    continue;
                }

                subtitle.season = season;
                subtitle.providedSeason = true;
                string episodeStr = part[(episodeIndex + 1)..];
                if (!uint.TryParse(episodeStr, out var episode)) {
                    Console.WriteLine("Failed to parse episode number");
                    continue;
                }

                subtitle.episode = episode;
                subtitle.providedEpisode = true;

                subtitle.isMovie = false;
                appendingTitle = false;
            }
            else if (part.Length == 4 && isNumerical(part)) {
                if (!uint.TryParse(part, out var year)) {
                    Console.WriteLine("Failed to parse year value");
                    continue;
                }
                subtitle.year = year;
                appendingTitle = false;
            } 
            else if (part.Length == 6 && part[0] == '(' && part[5] == '(' && isNumerical(part[1..5])) {
                if (!uint.TryParse(part[1..5], out var year)) {
                    Console.WriteLine("Failed to parse year value");
                    continue;
                }
                subtitle.year = year;
                appendingTitle = false;
            }
            else {
                if (!appendingTitle) {
                    continue;
                }

                if (title.Length > 0) {
                    title.Append(' ');
                }

                title.Append(part);
            }
        }

        subtitle.title = title.ToString();
    }

    private static int countChar(string str, char target) {
        int count = 0;
        foreach (var chr in str) {
            if (chr == target) {
                count++;
            }
        }
        return count;
    }

    public bool Validate() {
        if (title.Length == 0 && !subtitleFromFile) {
            Console.WriteLine("The title cannot be empty");
            return false;
        }
        if (language.Length == 0) {
            Console.WriteLine("The language cannot be empty");
            return false;
        }
        if (year != 0 && year < MIN_YEAR) {
            Console.WriteLine($"The first sound film was projected in {MIN_YEAR}");
            return false;
        }

        if (!isMovie) {
            if (!providedSeason || !providedEpisode) {
                Console.WriteLine("For TV series season and episode arguments are required");
                return false;
            }

            if (season > MAX_SEASONS) {
                Console.WriteLine("Season number is too large!");
                return false;
            }
            if (episode > MAX_EPISODES) {
                Console.WriteLine("Episode number is too large!");
                return false;
            }
        }

        if (extensionFilter != "" && !SUBTITLE_FORMATS.Contains(extensionFilter)) {
            Console.WriteLine("Subtitle extension doesn't match any existing subtitle formats!");
            return false;
        }
        
        if (shiftMs != 0) {
            if (shiftMs < BACKWARD_SHIFT_CAP) {
                Console.WriteLine("The shift goes too far back!");
                return false;
            }
            if (shiftMs > FORWARD_SHIFT_CAP) {
                Console.WriteLine("The shift goes too far forward!");
                return false;
            }
        }
        
        if (convert && !SUBTITLE_FORMATS.Contains(convertToExtension)) {
            Console.WriteLine("Subtitle extension doesn't match any existing subtitle formats!");
            return false;
        }

        if (!Directory.Exists(outputDirectory)) {
            Console.WriteLine($"Specified directory does not exist! {outputDirectory}");
            return false;
        }

        return true;
    }
    
    // returns index of the parameter that the 'arg' starts with, -1 if does not start with any
    private static int StartsWithIndex(string arg, params string[] parameters) {
        for (var i = 0; i < parameters.Length; i++) {
            if (arg.StartsWith(parameters[i])) {
                return i;
            }
        }
        return -1;
    }
    private static bool EqualsAny(string arg, params string[] parameters) {
        foreach (var param in parameters) {
            if (arg == param) {
                return true;
            }
        }

        return false;
    }

    public static (string title, uint year) ParseTitleYear(string name) {
        int bracketOpen = name.LastIndexOf('(');
        if (bracketOpen == -1) {
            return (name, 0);
        }

        string title = name[..(bracketOpen - 1)];
        int close = name.LastIndexOf(')');
        string numericalYear = name[(bracketOpen+1)..close];
        if (uint.TryParse(numericalYear, out var year)) {
            return (title, year);
        }
        return (title, 0);
    }
    
    // Expected format: Movie Name (year) S1 E5
    // The year must be in brackets because some titles are literally just a number
    public static Arguments FancyParse(string input) {
        string[] parts = input.Split(' ');
        var subtitle = new Arguments();
        bool parsedProperty = false;
        var title = new StringBuilder(32);
        foreach (var part in parts) {
            if (part.Length == 0) {
                // Skip additional empty spaces
                continue;
            }

            // Parse year in brackets
            if (part.StartsWith('(')) {
                int closing = part.LastIndexOf(')');
                if (closing == -1) {
                    closing = part.Length;
                }

                string maybeYear = part[1..closing];
                if (!isNumerical(maybeYear)) {
                    Console.WriteLine("The year is not numerical, skipping!");
                    continue;
                }
                if (maybeYear.Length < 4) {
                    Console.WriteLine("The year is too short, skipping!");
                    continue;
                }
                subtitle.year = uint.Parse(maybeYear);
                if (subtitle.year < MIN_YEAR) {
                    Console.WriteLine($"The first sound film was projected in {MIN_YEAR}");
                    continue;
                }
                parsedProperty = true;
                continue;
            }

            if ((part.StartsWith('S') || part.StartsWith('s')) && part.Length > 1 && isNumerical(part[1])) {
                subtitle.season = uint.Parse(part[1..]);
                subtitle.isMovie = false;
                parsedProperty = true;
                continue;
            }
            if ((part.StartsWith('E') || part.StartsWith('e')) && part.Length > 1 && isNumerical(part[1])) {
                subtitle.episode = uint.Parse(part[1..]);
                subtitle.isMovie = false;
                parsedProperty = true;
                continue;
            }

            if (!parsedProperty) {
                if (title.Length > 0) {
                    title.Append(' ');
                }
                title.Append(part);
            }
        }

        subtitle.title = title.ToString();
        return subtitle;
    }

    

    private const int PAD_LENGTH = 32;
    private static string formatOption(string[] flags, string description) {
        StringBuilder str = new StringBuilder(100);
        str.Append("    ");
        
        bool first = true;
        foreach (string flag in flags) {
            if (first) {
                str.Append(flag);
                first = false;
                continue;
            }
            
            str.Append(", ");
            str.Append(flag);
        }

        while (str.Length < PAD_LENGTH) {
            str.Append(' ');
        }
        
        str.Append(description);
        return str.ToString();
    }

    public override string ToString() {
        StringBuilder str = new StringBuilder(32);
        str.Append($"{title} ");
        if (year != 0) {
            str.Append($"({year}) ");
        }
        if (!isMovie) {
            str.Append($"S{season} E{episode} ");
        }
        str.Append($"Language:{language}");
        return str.ToString();
    }

    private static bool isNumerical(string str) {
        foreach (var chr in str) {
            switch (chr) {
                case >= '0' and <= '9':
                    break;
                default:
                    return false;
            }
        }
        return true;
    }
    
    private static bool isNumerical(char chr) {
        switch (chr) {
            case >= '0' and <= '9':
                return true;
            default:
                return false;
        }
    }
    
    public static void PrintLanguages() {
        const string LANGUAGES = @"
ALL                    all
Abkhazian              abk
Afrikaans              afr
Albanian               alb
Amharic                amh
Arabic                 ara
Aragonese              arg
Armenian               arm
Assamese               asm
Asturian               ast
Azerbaijani            aze
Basque                 baq
Belarusian             bel
Bengali                ben
Bosnian                bos
Breton                 bre
Bulgarian              bul
Burmese                bur
Catalan                cat
Chinese (Cantonese)    zhc
Chinese (simplified)   chi
Chinese (traditional)  zht
Chinese bilingual      zhe
Croatian               hrv
Czech                  cze
Danish                 dan
Dari                   prs
Dutch                  dut
English                eng
Esperanto              epo
Estonian               est
Extremaduran           ext
Finnish                fin
French                 fre
Gaelic                 gla
Galician               glg
Georgian               geo
German                 ger
Greek                  ell
Hebrew                 heb
Hindi                  hin
Hungarian              hun
Icelandic              ice
Igbo                   ibo
Indonesian             ind
Interlingua            ina
Irish                  gle
Italian                ita
Japanese               jpn
Kannada                kan
Kazakh                 kaz
Khmer                  khm
Korean                 kor
Kurdish                kur
Latvian                lav
Lithuanian             lit
Luxembourgish          ltz
Macedonian             mac
Malay                  may
Malayalam              mal
Manipuri               mni
Marathi                mar
Mongolian              mon
Montenegrin            mne
Navajo                 nav
Nepali                 nep
Northern Sami          sme
Norwegian              nor
Occitan                oci
Odia                   ori
Persian                per
Polish                 pol
Portuguese             por
Portuguese (BR)        pob
Portuguese (MZ)        pom
Pushto                 pus
Romanian               rum
Russian                rus
Santali                sat
Serbian                scc
Sindhi                 snd
Sinhalese              sin
Slovak                 slo
Slovenian              slv
Somali                 som
Spanish                spa
Spanish (EU)           spn
Spanish (LA)           spl
Swahili                swa
Swedish                swe
Syriac                 syr
Tagalog                tgl
Tamil                  tam
Tatar                  tat
Telugu                 tel
Thai                   tha
Toki Pona              tok
Turkish                tur
Turkmen                tuk
Ukrainian              ukr
Urdu                   urd
Uzbek                  uzb
Vietnamese             vie
Welsh                  wel";
        Console.WriteLine(LANGUAGES);
    }
}