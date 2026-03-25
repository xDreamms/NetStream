using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetStream;

public sealed class AutoUpdateManifest
{
    [JsonPropertyName("latest_version")]
    public string LatestVersion { get; set; } = "";

    [JsonPropertyName("generated_at_utc")]
    public DateTime GeneratedAtUtc { get; set; }

    [JsonPropertyName("files")]
    public List<AutoUpdateFileEntry> Files { get; set; } = new();
}

public sealed class AutoUpdateFileEntry
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "file";

    [JsonPropertyName("extract_to")]
    public string? ExtractTo { get; set; }
}

[JsonSerializable(typeof(AutoUpdateManifest))]
internal partial class UpdateJsonContext : JsonSerializerContext { }

public class UpdateProgressInfo
{
    public int DownloadingFileIndex { get; set; }
    public int TotalFiles { get; set; }
    public string FileName { get; set; } = "";
    public double ProgressPercentage { get; set; }
    public string DownloadSpeed { get; set; } = "";
}

public static class AutoUpdater
{
    public const string CurrentVersion = "2.6.0.0";
    private const string UpdateCheckUrl = "https://github.com/xDreamms/NetStream/raw/refs/heads/main/autoupdate2.txt";
    private const int TimeoutSeconds = 15;

    // TODO: ZIP HASHLERINI BURAYA YAZINIZ. Her guncellemede bu degerleri degistirmeniz gerekmektedir.
    private static readonly Dictionary<string, string> LocalZipHashes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ffmpeg.zip", "d52ea2fb30f4f906f77050f3beb6ed24909a1920483c68b3f2792e37f0cccdf8" }, 
        { "JacketConfig.zip", "7824b49a76290d03e0d2f80163c65e8b8842b87c1417edafdec20d44b92fc7cb" }, 
        { "libvlc.zip", "800e1ab5f51064d487df106bc18517a76330854f85a1ff312e58b78bbec03f5a" } 
    };

    public static async Task<List<AutoUpdateFileEntry>> GetUpdatesToDownloadAsync()
    {
        var manifest = await CheckForUpdateAsync();
        if (manifest == null) return new List<AutoUpdateFileEntry>();

        var toDownload = new List<AutoUpdateFileEntry>();
        string appDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (string.IsNullOrEmpty(appDir)) appDir = AppContext.BaseDirectory;

        foreach (var file in manifest.Files)
        {
            if (string.Equals(file.Type, "zip", StringComparison.OrdinalIgnoreCase))
            {
                bool missingExtractedDir = false;
                if (!string.IsNullOrEmpty(file.ExtractTo))
                {
                    var extractPath = Path.Combine(appDir, file.ExtractTo);
                    if (!Directory.Exists(extractPath))
                    {
                        missingExtractedDir = true;
                    }
                }

                if (missingExtractedDir)
                {
                    toDownload.Add(file);
                }
                else if (LocalZipHashes.TryGetValue(file.Path, out var localHash))
                {
                    if (!string.Equals(localHash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        toDownload.Add(file);
                    }
                }
                else
                {
                    toDownload.Add(file);
                }
            }
            else
            {
                var filePath = Path.Combine(appDir, file.Path);
                if (!File.Exists(filePath))
                {
                    toDownload.Add(file);
                }
                else
                {
                    var localHash = ComputeSHA256(filePath);
                    if (!string.Equals(localHash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        toDownload.Add(file);
                    }
                }
            }
        }

        return toDownload;
    }

    private static async Task<AutoUpdateManifest?> CheckForUpdateAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "NetStream-AutoUpdater");

            var json = await client.GetStringAsync(UpdateCheckUrl);
            if (string.IsNullOrWhiteSpace(json)) return null;

            return JsonSerializer.Deserialize(json, UpdateJsonContext.Default.AutoUpdateManifest);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoUpdater] Versiyon kontrol hatasi: {ex.Message}");
            return null;
        }
    }

    public static async Task DownloadAndApplyUpdateAsync(List<AutoUpdateFileEntry> filesToDownload, IProgress<UpdateProgressInfo> progress)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "NetStream_Update");
        string extractDir = Path.Combine(tempDir, "extracted");

        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);

            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(extractDir);

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(30);
            client.DefaultRequestHeaders.Add("User-Agent", "NetStream-AutoUpdater");

            int totalFiles = filesToDownload.Count;
            for (int i = 0; i < totalFiles; i++)
            {
                var fileEntry = filesToDownload[i];
                var isZip = string.Equals(fileEntry.Type, "zip", StringComparison.OrdinalIgnoreCase);

                progress?.Report(new UpdateProgressInfo
                {
                    DownloadingFileIndex = i + 1,
                    TotalFiles = totalFiles,
                    FileName = fileEntry.Path,
                    ProgressPercentage = 0,
                    DownloadSpeed = "0 KB/s"
                });

                // If Path like "ffmpeg/ffmpeg.exe", just download it as "ffmpeg.exe" in temp root.
                // We will reconstruct the structure later while copying to `extractDir`.
                var safeDownloadName = Path.GetFileName(fileEntry.Path.Replace("/", "\\"));
                string targetDlPath = Path.Combine(tempDir, safeDownloadName);

                using var response = await client.GetAsync(fileEntry.Url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var stream = await response.Content.ReadAsStreamAsync();
                
                using (var fileStream = new FileStream(targetDlPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[81920]; 
                    int bytesRead;
                    long totalRead = 0;
                    
                    var stopwatch = Stopwatch.StartNew();
                    var lastReportTime = stopwatch.ElapsedMilliseconds;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        var now = stopwatch.ElapsedMilliseconds;
                        if (now - lastReportTime >= 500 || totalRead == totalBytes)
                        {
                            double percent = totalBytes > 0 ? (double)totalRead / totalBytes * 100 : 0;
                            double speedBytesPerSec = totalRead / (stopwatch.Elapsed.TotalSeconds + 0.0001);
                            string speedStr = FormatSize(speedBytesPerSec) + "/s";

                            progress?.Report(new UpdateProgressInfo
                            {
                                DownloadingFileIndex = i + 1,
                                TotalFiles = totalFiles,
                                FileName = fileEntry.Path,
                                ProgressPercentage = percent,
                                DownloadSpeed = speedStr
                            });
                            lastReportTime = now;
                        }
                    }
                } // Unlock fileStream before extracting it

                if (isZip)
                {
                    string unzipTarget = string.IsNullOrEmpty(fileEntry.ExtractTo)
                        ? extractDir
                        : Path.Combine(extractDir, fileEntry.ExtractTo);

                    Directory.CreateDirectory(unzipTarget);
                    // ExtractToDirectory will recreate directory structure defined inside the zip itself.
                    ZipFile.ExtractToDirectory(targetDlPath, unzipTarget, true);
                }
                else
                {
                    // For single files, fileEntry.Path could have slashes (like 'ffmpeg\ffmpeg.exe').
                    // We must recreate that same relative path inside `extractDir`.
                    var normalizedPath = fileEntry.Path.Replace("/", "\\");
                    string targetFile = Path.Combine(extractDir, normalizedPath);
                    string targetFileDir = Path.GetDirectoryName(targetFile);

                    if (!string.IsNullOrEmpty(targetFileDir))
                    {
                        Directory.CreateDirectory(targetFileDir);
                    }
                    
                    File.Copy(targetDlPath, targetFile, true);
                }
            }

            string appDir = Path.GetDirectoryName(Environment.ProcessPath);
            if (string.IsNullOrEmpty(appDir)) appDir = AppContext.BaseDirectory;
            string exeName = Path.GetFileName(Environment.ProcessPath);
            if (string.IsNullOrEmpty(exeName)) exeName = "NetStream.Desktop.exe";
            string batPath = Path.Combine(tempDir, "update.bat");

            WriteUpdateBatch(batPath, extractDir, appDir, exeName, tempDir);

            Console.WriteLine("[AutoUpdater] Guncelleme baslatiliyor...");
            Process.Start(new ProcessStartInfo
            {
                FileName = batPath,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoUpdater] Guncelleme hatasi: {ex.Message}");
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            throw;
        }
    }

    private static void WriteUpdateBatch(string batPath, string extractedDir, string appDir, string exeName, string tempDir)
    {
        if (!appDir.EndsWith("\\")) appDir += "\\";

        string script = $@"@echo off
chcp 65001 >nul
timeout /t 3 /nobreak >nul
xcopy /s /y /q ""{extractedDir}\*"" ""{appDir}""
start """" ""{appDir}{exeName}""
timeout /t 2 /nobreak >nul
rd /s /q ""{tempDir}""
del ""%~f0""
";
        File.WriteAllText(batPath, script, System.Text.Encoding.UTF8);
    }

    private static string ComputeSHA256(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch
        {
            return "";
        }
    }

    private static string FormatSize(double bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (bytes >= 1024 && order < sizes.Length - 1)
        {
            order++;
            bytes = bytes / 1024;
        }
        return $"{bytes:0.##} {sizes[order]}";
    }
}
