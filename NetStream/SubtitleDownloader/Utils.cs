using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace subtitle_downloader.downloader;

public class Utils {
    public static string sanitizeFileName(string fileName) {
        StringBuilder str = new(fileName.Length);
        foreach (char chr in fileName) {
            switch (chr) {
                case '<':
                case '>':
                case ':':
                case '/':
                case '\\':
                case '\t':
                case '\n':
                case '\r':
                case '\b':
                case '\a':
                case '"':
                case '|':
                case '?':
                case '*':
                    break;
                default:
                    str.Append(chr);
                    break;
            }
        }

        return str.ToString();
    }

    public static string GetExtension(string path) {
        string? ext = Path.GetExtension(path);
        if (ext.StartsWith('.')) {
            ext = ext[1..];
        }
        return ext;
    }

    public static string correctOutputDirectory(string outputDir) {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        // C# does not correctly identify drive path as absolute
        if (isWindows && outputDir.EndsWith(':')) {
            return outputDir + "/";
        }

        return outputDir;
    }
    
    public static void unzipFile2(string zipPath, string outputDirectory) {
        try {
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, outputDirectory);
        }
        catch (IOException io) {
            Console.WriteLine(io.Message);
        }
    }

    // Extract .zip that contains the subtitle files
    public static List<string> unzip(string zipPath, string outputDirectory) {
        var extracted = new List<string>();
        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        foreach (ZipArchiveEntry entry in archive.Entries) {
            if (entry.FullName.EndsWith(".nfo")) {
                continue;
            }
            string destPath = Path.Combine(outputDirectory, entry.FullName);
            entry.ExtractToFile(destPath, overwrite: true);

            extracted.Add(destPath);
        }
        return extracted;
    }

    /*
         | Url         | Format | Downloads
      ---+-------------+--------+-----------
       1 | https://... | srt    | 132989
       2 | https://... | vtt    | 2999
       3 | https://... | ssa    | 4
     */
    public static string prettyFormatSubtitlesInTable(List<SubtitleRow> sortedRows) {
        int largestLink = 0;
        foreach (var sub in sortedRows) {
            largestLink = Math.Max(sub.getDownloadURL().Length, largestLink);
        }

        int largestDownload = Math.Max(9, sortedRows[0].downloads.ToString().Length);
        int largestPositionNum = sortedRows.Count.ToString().Length;

        largestLink += 2;
        largestDownload += 2;
        largestPositionNum += 2;

        string[] headers = { " ", "Download URL", "Format", "Downloads" };
        int[] lengths = { largestPositionNum, largestLink, 8, largestDownload };
        StringBuilder separator = GetTableHorizontalSeparator(lengths);

        StringBuilder table = new StringBuilder(128);
        table.Append(separator);
        table.Append("\n|");
        // HEADER FORMATTING
        for (var i = 0; i < lengths.Length; i++) {
            int length = lengths[i];

            table.Append(CenterPad(headers[i], length, true));
            table.Append('|');
        }

        table.Append('\n');
        table.Append(separator);
        table.Append('\n');

        // Align downloads according to top number
        int leftSpaces = (lengths[3] - sortedRows[0].downloads.ToString().Length) / 2;
        // SUBTITLE ROWS
        for (var r = 0; r < sortedRows.Count; r++) {
            var sub = sortedRows[r];
            int index = r + 1;
            table.Append('|');
            table.Append(CenterPad(index.ToString(), lengths[0], false));
            table.Append('|');
            table.Append(CenterPad(sub.getDownloadURL(), lengths[1], true));
            table.Append('|');
            table.Append(CenterPad(sub.format, lengths[2], true));
            table.Append('|');
            table.Append(PadRight(sub.downloads.ToString(), lengths[3], leftSpaces));
            table.Append("|\n");
        }

        table.Append(separator);
        return table.ToString();
    }

    private static StringBuilder GetTableHorizontalSeparator(params int[] lengths) {
        StringBuilder tableRow = new StringBuilder(lengths.Sum() + lengths.Length);
        tableRow.Append('+');
        foreach (var len in lengths) {
            AppendTimes(tableRow, len, '-');
            tableRow.Append('+');
        }

        return tableRow;
    }

    private static void AppendTimes(StringBuilder str, int length, char chr) {
        for (int i = 0; i < length; i++) {
            str.Append(chr);
        }
    }

    private static StringBuilder CenterPad(string str, int targetLen, bool leftLeaning) {
        int remainingLen = targetLen - str.Length;
        int halfLen;
        if (leftLeaning) {
            halfLen = remainingLen / 2;
        }
        else {
            halfLen = (remainingLen + 1) / 2;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < halfLen; i++) {
            builder.Append(' ');
        }

        builder.Append(str);
        int rightLen = remainingLen - halfLen;
        for (int i = 0; i < rightLen; i++) {
            builder.Append(' ');
        }

        return builder;
    }

    private static StringBuilder PadRight(string str, int targetLen, int leftSpaces) {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < leftSpaces && i < targetLen; i++) {
            builder.Append(' ');
        }
        builder.Append(str);
        int remainingLen = targetLen - str.Length - leftSpaces;
        for (int i = 0; i < remainingLen; i++) {
            builder.Append(' ');
        }

        return builder;
    }

    public static void FailExit(string message) {
        Console.WriteLine(message);
        Environment.Exit(1);
    }
}