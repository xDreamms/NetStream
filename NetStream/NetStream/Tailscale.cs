using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetStream;

public class Tailscale
{
    
}

public class TailscaleInstaller
{
    private static readonly string tailscaleDownloadUrl =
        "https://pkgs.tailscale.com/stable/tailscale-setup-latest.exe";

    private static readonly string installerPath = Path.Combine(Path.GetTempPath(), "tailscale-setup.exe");

    public static async Task DownloadAndInstallAsync()
    {
        if (!File.Exists(installerPath))
        {
            Console.WriteLine("Tailscale indiriliyor...");
            using var client = new WebClient();
            await client.DownloadFileTaskAsync(tailscaleDownloadUrl, installerPath);
        }

        Console.WriteLine("Tailscale kuruluyor...");
        var process = new Process();
        process.StartInfo.FileName = installerPath;
        process.StartInfo.Arguments = "/quiet"; // Sessiz kurulum
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
        
        File.Delete(installerPath);

        Console.WriteLine("Tailscale kurulum tamamlandı.");
    }

}

public class TailscaleHelper
{

    public static void EnsureLoggedIn(string hostname = "netstream-pc")
    {
        var tailscale = Essentials.FindExeManually("tailscale.exe");
        if (tailscale != null)
        {
            Console.WriteLine("Tailscale yüklü! : " + tailscale);
            var process = new Process();
            process.StartInfo.FileName = tailscale;
            process.StartInfo.Arguments = $"up --hostname={hostname} --accept-routes --accept-dns";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false; 
            process.Start();

            process.WaitForExit();
            Console.WriteLine("Giriş işlemi tamamlandı (kullanıcı tarayıcıdan onaylamalı).");
            
        }
        else
        {
            Console.WriteLine("Tailscale yüklü değil!");
            return;
        }
       
       
    }
    
    public static void StartTailscaleService()
    {
        var process = new Process();
        process.StartInfo.FileName = "sc";
        process.StartInfo.Arguments = "start Tailscale";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
    }

    public static async Task<string> GetTailscaleIpAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:41112/");
            var response = await httpClient.GetAsync("localapi/v0/status");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            foreach (var peer in doc.RootElement.GetProperty("Self").GetProperty("Addresses").EnumerateArray())
            {
                var address = peer.GetString();
                if (address.StartsWith("100.")) // Tailscale IP'leri 100.x.x.x ile başlar
                {
                    return address.Split('/')[0]; // CIDR'ı ("/32") atıyoruz
                }
            }

            Console.WriteLine("Tailscale IP bulunamadı.");
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + " " + e.StackTrace);
        }
        return "";
    }
    
    public static string GetTailscaleIpFromCli()
    {
        try
        {
            var tailscale = Essentials.FindExeManually("tailscale.exe");
            var psi = new ProcessStartInfo
            {
                FileName = tailscale,
                Arguments = "ip",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // IPv4 al
            var ip = output.Split('\n')
                .Select(line => line.Trim())
                .FirstOrDefault(ip => ip.Contains("."));

            return ip;
        }
        catch (Exception e)
        {
            Console.WriteLine("HATA (CLI): " + e.Message);
            return null;
        }
    }

}