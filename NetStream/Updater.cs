using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Reflection;
using System.Windows;
using Serilog;

namespace NetStream
{
    public class Updater
    {
        public static async Task<bool> IsLatestVersion()
        {
            try
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                System.Version latestVersion;
                var latestVersionInfo = await GetUpdaterInfoFromUrlAsync("https://raw.githubusercontent.com/xDreamms/NetStream/refs/heads/main/updater.txt");

                if (latestVersionInfo != null)
                {
                    latestVersion = latestVersionInfo.LatestVersion;
                }
                else
                {
                    return false;
                }

                if (currentVersion < latestVersion)
                {
                    return false;
                }
                else if (currentVersion == latestVersion)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return false;
            }
        }

        static async Task<UpdaterInfo> GetUpdaterInfoFromUrlAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                string content = await client.GetStringAsync(url);
                var updaterInfo = JsonConvert.DeserializeObject<UpdaterInfo>(content);
                return updaterInfo;
            }
        }

        //public static async Task<UpdaterInfo> GetLatestVersion(string owner, string repo, string branch, string filePath)
        //{
        //    string url = $"https://api.github.com/repos/{owner}/{repo}/contents/{filePath}?ref={branch}";

        //    try
        //    {
        //        using (HttpClient client = new HttpClient())
        //        {
        //            client.DefaultRequestHeaders.UserAgent.ParseAdd("NetStream-Updater");
        //            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3.raw");

        //            string content = await client.GetStringAsync(url);
        //            var json = Encryptor.decrypt(content);
        //            var updaterInfo = JsonConvert.DeserializeObject<UpdaterInfo>(json);
        //            return updaterInfo;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Error.WriteLine($"Error fetching file from GitHub API: {ex.Message}");
        //        return null;
        //    }
        //}

        public static bool IsValidHash(string validHash)
        {
            string dll = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NetStream.dll");
            if (!File.Exists(dll)) return false;
            string hash = HesaplaSHA256(dll);
            return validHash == hash;
        }

        static string HesaplaSHA256(string dosyaYolu)
        {
            using (FileStream fs = File.OpenRead(dosyaYolu))
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(fs);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }

    public class UpdaterInfo
    {
        public string DownloadLink { get; set; }
        public System.Version LatestVersion { get; set; }
        public string Hash { get; set; }
    }
}
