using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using TorrentWrapper;

namespace NetStream;

public class LibtorrentClient
{
    public Client client;
    public List<TorrentHandle> Torrents = new List<TorrentHandle>();
    private readonly object _torrentsLock = new object();
    public object _torrentHandleWrappersLock = new object();
    public static SessionManager sessionManager;
    
    public LibtorrentClient()
    {
        client = new Client("lG!o0)%]?M85Q`57FZqzqf4U|t1@@");
        EnsureTorrensLoaded();
        client.PauseAllTorrents();
        sessionManager = new SessionManager();
        _ = Task.Run((async () =>
        {
            try
            {
                sessionManager.StartListeningAlerts();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in alert listening: " + ex.Message);
            }
        }));
    }

    public void CleanupCache()
    {
        try
        {
            var currentSize = 0;
            var handlesToRemove = new List<string>();

            // Önce tüm torrent'lerin bellek kullanımını kontrol et
            lock (_torrentHandleWrappersLock)
            {
                foreach (var handle in torrentHandleWrappers)
                {
                    var statusInfo = handle.Value.GetStatus();
                    // İndirme tamamlandıysa veya uzun süredir kullanılmıyorsa temizle
                    if (statusInfo != null && (statusInfo.Progress >= 0.99))
                    {
                        handlesToRemove.Add(handle.Key);
                    }
                    // Cache boyutunu hesapla
                    currentSize++;
                }

                // Torrent sayısı yeteri kadar azsa, temizlik yapma
                if (handlesToRemove.Count == 0)
                    return;

                // Tamamlanan torrent'leri temizle
                foreach (var key in handlesToRemove)
                {
                    if (torrentHandleWrappers.ContainsKey(key))
                    {
                        try
                        {
                            var wrapper = torrentHandleWrappers[key];
                            torrentHandleWrappers.Remove(key);

                            // Wrapper'ı düzgün şekilde temizle
                            try
                            {
                                wrapper.Pause();
                                wrapper.Dispose();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error disposing wrapper during cleanup: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error removing torrent from cache: {ex.Message}");
                        }
                    }
                }

                // Önbellek hala büyükse, en eski veya en az kullanılan öğeleri çıkar
               
                    // Temizlenecek öğe sayısı
                    int removeCount = torrentHandleWrappers.Count ;

                    // En eski öğeleri al
                    var oldestTorrents = torrentHandleWrappers
                        .OrderBy(x => x.Value.AddedOn)
                        .Take(removeCount)
                        .ToList();

                    foreach (var item in oldestTorrents)
                    {
                        try
                        {
                            var wrapper = item.Value;
                            torrentHandleWrappers.Remove(item.Key);

                            // Wrapper'ı düzgün şekilde temizle
                            try
                            {
                                wrapper.Pause();
                                wrapper.Dispose();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error disposing old wrapper: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error removing old torrent from cache: {ex.Message}");
                        }
                    }
                
            }

            // Temizlik işlemi için Torrent sınıflarını da güncelle
            lock (_torrentsLock)
            {
                foreach (var handle in Torrents.ToList())
                {
                    if (handlesToRemove.Contains(handle.Hash))
                    {
                        Torrents.Remove(handle);
                    }
                }
            }

            // Bellek temizliği isteyenleri öner - dikkatli şekilde
            if (handlesToRemove.Count > 0)
            {
                // Optimize edilmiş bir GC temizliği iste - sadece genç nesneleri temizle
                GC.Collect(0, GCCollectionMode.Optimized, false);
            }
            client.Clear();
            sessionManager.StopListeningAlerts();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CleanupCache: {ex.Message}");
        }
    }

    async void EnsureTorrensLoaded()
    {
        while (!Client.IsTorrentsLoaded)
        {
            await Task.Delay(200);
        }
    }

    public async Task<AddTorrentFromFileResponse> AddTorrentFromFile(string path, string moviesPath)
    {
        EnsureTorrensLoaded();
        if (client == null)
        {
            return new AddTorrentFromFileResponse()
                { Success = false, Hash = null, ErrorMessage = "Client was null." };
        }
            

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return new AddTorrentFromFileResponse
            {
                Hash = null,
                Success = false,
                ErrorMessage = "Invalid path in AddTorrentFromFile: " + path
            };
        }

        try
        {
            return new AddTorrentFromFileResponse
            {
                Hash = client.AddTorrentFromFile(path, moviesPath),
                Success = true,
                ErrorMessage = ""
            };
        }
        catch (Exception ex)
        {
            return new AddTorrentFromFileResponse
            {
                Hash = null,
                Success = false,
                ErrorMessage = $"Error adding torrent from file: {ex.Message}"
            };
        }
    }

    public async Task<List<TorrentHandleResponseShort>> GeTorrentList()
    {
        EnsureTorrensLoaded();
        var result = new List<TorrentHandleResponseShort>();
        var torrents = await client.GetTorrentsAsync();
        foreach (var torrentHandleWrapper in torrents)
        {
            var status = torrentHandleWrapper.GetStatus();
            result.Add(new TorrentHandleResponseShort
            {
                Name = torrentHandleWrapper.Name,
                Hash = torrentHandleWrapper.Hash,
                TorrentState = (TorrentState) torrentHandleWrapper.GetTorrentState,
                PieceSize = torrentHandleWrapper.PieceSize,
                TotalPieces = torrentHandleWrapper.TotalPieces,
                AddedOn = torrentHandleWrapper.AddedOn,
                Size = torrentHandleWrapper.Size,
                Downloaded = torrentHandleWrapper.Downloaded,
                Progress = status.Progress,
                IsPaused = torrentHandleWrapper.IsPaused
            });
        }

        return result;
    }


    public async Task<AddTorrentFromMagnetResponse> AddTorrentFromMagnet(string url, string moviesPath)
    {
        EnsureTorrensLoaded();
        if (string.IsNullOrEmpty(url))
        {
            return new AddTorrentFromMagnetResponse
            {
                Hash = null,
                Success = false,
                ErrorMessage = "URL is null or empty in AddTorrentFromMagnet"
            };
        }

        try
        {
           
            string hash = null;
            
            Console.WriteLine($"[AddTorrentFromMagnet] Starting with URL: {url}");
            
            // Torrent'i ekle
            if (url.StartsWith("magnet:"))
            {
                Console.WriteLine("[AddTorrentFromMagnet] Adding magnet link");
                hash = client.AddTorrentFromMagnet(url, moviesPath);
            }
            else
            {
                Console.WriteLine("[AddTorrentFromMagnet] Redirecting URL...");
                string redirectedUrl = await GetRedirectedUrl(url);
                if (!string.IsNullOrEmpty(redirectedUrl))
                {
                    Console.WriteLine($"[AddTorrentFromMagnet] Redirected to: {redirectedUrl}");
                    hash = client.AddTorrentFromMagnet(redirectedUrl, moviesPath);
                }
            }
            
            if (string.IsNullOrEmpty(hash))
            {
                Console.WriteLine("[AddTorrentFromMagnet] Failed to get hash from torrent client");
                return new AddTorrentFromMagnetResponse
                {
                    Hash = null,
                    Success = false,
                    ErrorMessage = "Failed to get hash from torrent client"
                };
            }
            
            Console.WriteLine($"[AddTorrentFromMagnet] Hash received from client: {hash}");
            
            // Hash başarıyla alındı, torrent eklendi
            return new AddTorrentFromMagnetResponse
            {
                Hash = hash,
                Success = true,
                ErrorMessage = ""
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in AddTorrentFromMagnet: {ex.Message}\n{ex.StackTrace}");
            return new AddTorrentFromMagnetResponse
            {
                Hash = null,
                Success = false,
                ErrorMessage = $"Error adding torrent from magnet: {ex.Message}"
            };
        }
    }

    private async Task<string> GetRedirectedUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        try
        {
            using (HttpClientHandler handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (HttpClient client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromSeconds(10); // Add timeout to prevent hanging

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.MovedPermanently)
                {
                    // Redirect edilen URL'yi döndür
                    return response.Headers.Location?.ToString() ?? string.Empty;
                }
                else if (response.IsSuccessStatusCode)
                {
                    // Eğer yönlendirme yoksa, yanıt içeriğini döndür
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"HTTP request failed: {response.StatusCode}");
                    return string.Empty;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error getting redirected URL: " + e.Message);
            return string.Empty;
        }
    }


    public Dictionary<string, TorrentHandleWrapper> torrentHandleWrappers = new Dictionary<string, TorrentHandleWrapper>();
    List<TorrentHandleWrapper> NativeTorrents = new List<TorrentHandleWrapper>();

    public async Task<TorrentHandleWrapper> GetTorrentHandle(string hash)
    {
        EnsureTorrensLoaded();
        if (string.IsNullOrEmpty(hash))
        {
            Console.WriteLine("Hash is null or empty in GetTorrentHandle");
            return null;
        }

        try
        {
            lock (_torrentHandleWrappersLock)
            {
                if (torrentHandleWrappers.ContainsKey(hash))
                {
                    Console.WriteLine("Already in dictionary returning value");
                    return torrentHandleWrappers[hash];
                }
                else
                {
                    var torrent = client.FindTorrent(hash);
                    if (torrent != null)
                    {
                        if (!torrentHandleWrappers.ContainsKey(hash))
                            torrentHandleWrappers.Add(hash, torrent);

                        return torrent;
                    }
                    else
                    {
                        return null;
                    }
                   
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error on torrent handle: " + e.Message);
            return null;
        }

    }



    public async Task<bool> IsTorrentExistPath(string path)
    {
        EnsureTorrensLoaded();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            var parser = new BencodeParser();
            var torrent = parser.Parse<BencodeNET.Torrents.Torrent>(path);
            if (torrent == null || string.IsNullOrEmpty(torrent.OriginalInfoHash))
            {
                return false;
            }
            var t = await GetTorrentHandle(torrent.OriginalInfoHash);
            return t != null;
        }
        catch (Exception e)
        {
            Console.WriteLine("Error checking torrent existence: " + e.Message);
        }

        return false;
    }
    public string TextAfter(string value, string search)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(search))
        {
            return string.Empty;
        }

        int index = value.IndexOf(search);
        if (index < 0)
        {
            return string.Empty;
        }

        return value.Substring(index + search.Length);
    }

    public async Task<bool> IsTorrentExistUrl(string url)
    {
        EnsureTorrensLoaded();
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        try
        {
            if (!url.StartsWith("magnet:?xt=urn:btih:"))
            {
                return false;
            }

            var text = url;
            var search = "magnet:?xt=urn:btih:";
            var textAfter = TextAfter(text, search);

            if (string.IsNullOrEmpty(textAfter) || !textAfter.Contains('&'))
            {
                return false;
            }

            string hash = textAfter.Substring(0, textAfter.IndexOf('&'));
            if (string.IsNullOrEmpty(hash))
            {
                return false;
            }

            var t = await GetTorrentHandle(hash);
            return t != null;
        }
        catch (Exception e)
        {
            Console.WriteLine("Error checking torrent URL existence: " + e.Message);
        }

        return false;
    }

    public async Task<bool> Delete(string hash)
    {
        try
        {
            if (string.IsNullOrEmpty(hash))
            {
                Console.WriteLine("Delete: Hash is null or empty");
                return false;
            }

            Console.WriteLine($"[LibtorrentClient.Delete] Starting deletion for hash: {hash}");

            // Get torrent handle
            TorrentHandleWrapper torrentHandleWrapper = null;
            lock (_torrentHandleWrappersLock)
            {
                if (torrentHandleWrappers.ContainsKey(hash))
                {
                    torrentHandleWrapper = torrentHandleWrappers[hash];
                }
            }

            if (torrentHandleWrapper != null)
            {
                Console.WriteLine($"[LibtorrentClient.Delete] Calling Delete on torrent handle wrapper");
                
                // Run on a separate thread to avoid blocking UI if it takes time (though Delete is synchronous in C++, we wrap it)
                // Since Delete in wrapper is synchronous and might sleep, we should probably run it in Task.Run if not already.
                // But here we are in async method.
                
                bool success = await Task.Run(() => torrentHandleWrapper.Delete(true, true, hash, null, null)); // deleteFiles=true, deleteResume=true
                
                if (success)
                {
                    torrentHandleWrapper.Dispose();
                    
                    lock (_torrentHandleWrappersLock)
                    {
                        if (torrentHandleWrappers.ContainsKey(hash))
                        {
                            torrentHandleWrappers.Remove(hash);
                            Console.WriteLine($"[LibtorrentClient.Delete] Removed from torrentHandleWrappers: {hash}");
                        }
                    }

                    // Remove from Torrents list
                    lock (_torrentsLock)
                    {
                        var torrentToRemove = Torrents.FirstOrDefault(t => t.Hash?.ToLower() == hash.ToLower());
                        if (torrentToRemove != null)
                        {
                            Torrents.Remove(torrentToRemove);
                            Console.WriteLine($"[LibtorrentClient.Delete] Removed from Torrents list: {hash}");
                        }
                    }

                    Console.WriteLine($"[LibtorrentClient.Delete] Torrent deleted successfully: {hash}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[LibtorrentClient.Delete] Torrent deletion failed (wrapper returned false): {hash}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"[LibtorrentClient.Delete] Warning: Torrent handle wrapper not found for hash: {hash}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LibtorrentClient.Delete] Error deleting torrent: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }
}
