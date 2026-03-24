using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Serilog;

namespace NetStream
{
    
     public static class DiskImageCache
    {
        private static readonly string CacheDirectoryName = "ImageCache";
        private static readonly string CacheDirectory;
        private static readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        
        static DiskImageCache()
        {
            string libraryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetStream");
            string cachesPath = Path.Combine(libraryPath, "Caches");
            CacheDirectory = Path.Combine(cachesPath, CacheDirectoryName);
            
            // Cache dizinini oluştur
            if (!Directory.Exists(CacheDirectory))
            {
                Console.WriteLine("Directory doesn't exist");
                Directory.CreateDirectory(CacheDirectory);
            }
            else
            {
                Console.WriteLine("Cache exists");
            }
        }
        
        // URL'den benzersiz bir dosya adı oluştur
        private static string GetCacheFileName(string url)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(url));
                
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                
                return sb.ToString() + ".jpg";
            }
        }
        
        // URL'ye göre cache dosya yolunu döndür
        public static string GetCacheFilePath(string url)
        {
            return Path.Combine(CacheDirectory, GetCacheFileName(url));
        }
        
        // Görüntünün cache'de olup olmadığını kontrol et
        public static bool IsCached(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;
                
            string filePath = GetCacheFilePath(url);
            return File.Exists(filePath);
        }
        
        // Görüntüyü cache'e kaydet
        public static async Task SaveToCache(string url, byte[] imageData)
        {
            if (string.IsNullOrEmpty(url) || imageData == null)
                return;
                
            string filePath = GetCacheFilePath(url);
            
            await _cacheLock.WaitAsync();
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await fs.WriteAsync(imageData, 0, imageData.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISKCACHE] Görüntü cache'e kaydedilemedi: {ex.Message}");
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        
        // Görüntüyü cache'den yükle
        public static async Task<byte[]> LoadFromCache(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;
                
            string filePath = GetCacheFilePath(url);
            
            if (!File.Exists(filePath))
                return null;
                
            await _cacheLock.WaitAsync();
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] imageData = new byte[fs.Length];
                    await fs.ReadAsync(imageData, 0, imageData.Length);
                    return imageData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISKCACHE] Görüntü cache'den yüklenemedi: {ex.Message}");
                return null;
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        
        // Cache'i temizle
        public static void ClearCache()
        {
            try
            {
                if (Directory.Exists(CacheDirectory))
                {
                    foreach (string file in Directory.GetFiles(CacheDirectory))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISKCACHE] Cache temizlenemedi: {ex.Message}");
            }
        }
        
        // Cache boyutunu kontrol et ve gerekirse temizle
        public static void CleanupCache(long maxBytes = 100 * 1024 * 1024) // Varsayılan 100MB
        {
            try
            {
                if (!Directory.Exists(CacheDirectory))
                    return;
                    
                var cacheFiles = new DirectoryInfo(CacheDirectory).GetFiles();
                var totalSize = cacheFiles.Sum(f => f.Length);
                Console.WriteLine("Total cache size: " + totalSize);
                if (totalSize <= maxBytes)
                    return;
                    
                // En eski dosyaları sil
                var orderedFiles = cacheFiles.OrderBy(f => f.LastAccessTime).ToList();
                
                foreach (var file in orderedFiles)
                {
                    file.Delete();
                    totalSize -= file.Length;
                    
                    if (totalSize <= maxBytes * 0.8) // %80'in altına düşerse dur
                        break;
                }
                
                Console.WriteLine("Cache temizlendi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISKCACHE] Cache temizleme hatası: {ex.Message}");
            }
        }
    }
    public static class AsyncImageLoader
    {
        private static readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(15) };
        
        // Bounded in-memory bitmap cache with LRU eviction
        private static readonly ConcurrentDictionary<string, Bitmap> _memoryCache = new ConcurrentDictionary<string, Bitmap>();
        private static readonly ConcurrentQueue<string> _cacheOrder = new ConcurrentQueue<string>();
        private const int MaxMemoryCacheSize = 100;
        private static readonly object _evictionLock = new object();
        
        // Web platformunda çalışıp çalışmadığını kontrol eden özellik
        private static bool IsRunningInBrowser => 
            System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Browser") ||
            AppContext.GetData("IsRunningInBrowser") is bool isInBrowser && isInBrowser;
            
        // Android platformunda çalışıp çalışmadığını kontrol eden özellik
        private static bool IsRunningOnAndroid =>
            System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Android") ||
            AppContext.GetData("IsRunningOnAndroid") is bool isOnAndroid && isOnAndroid ||
            // Android'in bazı sürümleri için System.Environment.OSVersion.Platform kontrolü
            Environment.OSVersion.Platform.ToString().Contains("Unix") && 
            System.IO.Directory.Exists("/system/app");
            
        // Android için user-agent tanımı
        private static readonly string AndroidUserAgent = "Mozilla/5.0 (Linux; Android 10; SM-A205U) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.115 Mobile Safari/537.36";
        
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(19); // Maksimum 19 eşzamanlı istek
        public static async Task<Bitmap> LoadFromWeb(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            string processedUrl = url;
            
            try
            {
                // Check in-memory cache first
                if (_memoryCache.TryGetValue(processedUrl, out var cachedBitmap))
                {
                    return cachedBitmap;
                }
                
                if (DiskImageCache.IsCached(processedUrl))
                {
                    byte[] cachedData = await DiskImageCache.LoadFromCache(processedUrl);
                    if (cachedData != null && cachedData.Length > 0)
                    {
                        // UI thread'e geçmemiz gerekiyor
                        return await Dispatcher.UIThread.InvokeAsync(() => 
                        {
                            try 
                            {
                                using var ms = new System.IO.MemoryStream(cachedData);
                                var bitmap = new Bitmap(ms);
                                AddToMemoryCache(processedUrl, bitmap);
                                return bitmap;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ASYNCIMAGELOADER] Cache'den yüklenirken hata: {ex.Message}");
                                return null;
                            }
                        });
                    }
                }

                byte[] data = null;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                
                await _semaphore.WaitAsync(cts.Token);
                try
                {
                     data = await _httpClient.GetByteArrayAsync(processedUrl, cts.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ASYNCIMAGELOADER] Cache'den hata: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                }

                if (data == null) return null;
                await DiskImageCache.SaveToCache(processedUrl, data);
                
                // Burada doğrudan bitmap oluşturup döndürmek yerine
                // Bitmap nesnesi oluşturulmasını UI thread'e bırakıyoruz
                // ve memory stream her durumda dispose edilecek
                return await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    try 
                    {
                        // Bitmap oluştur - MemoryStream otomatik dispose olur
                        using var ms = new System.IO.MemoryStream(data);
                        var bitmap = new Bitmap(ms);
                        AddToMemoryCache(processedUrl, bitmap);
                        return bitmap;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ASYNCIMAGELOADER] Bitmap oluşturulurken hata: {ex.Message}");
                        return null;
                    }
                });
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"[ASYNCIMAGELOADER] Zaman aşımı: {url}");
                return null;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[ASYNCIMAGELOADER] HTTP hatası: {ex.Message}, URL: {url}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ASYNCIMAGELOADER] Genel hata: {ex.Message}, URL: {url}");
                return null;
            }
        }
        
        // URL'yi HTTPS formatına çeviren yardımcı metod
        private static string EnsureHttpsUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;
                
            if (url.StartsWith("http://"))
                return "https://" + url.Substring(7);
                
            return url;
        }
        
        // Web platformu için CORS destekli yükleme
        private static async Task<byte[]> FetchWithCorsSupport(string url, CancellationToken cancellationToken)
        {
            // Web platformunda CORS sorununu çözmek için burada birkaç farklı strateji deneyebiliriz
            
            // 1. Doğrudan HTTPS URL
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            // Tarayıcı ortamında çalışırken gerekli header'ları ekle
            request.Headers.Add("Origin", "https://null");
            request.Headers.Add("Referer", "https://null");
            
            // İsteği gönder
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            // Sonucu byte array olarak döndür
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        
        // Android platformu için özel yükleme
        private static async Task<byte[]> FetchForAndroid(string url, CancellationToken cancellationToken)
        {
            // Android için özel bir HttpClient oluştur
            using var client = new HttpClient();
            
            // User-Agent belirt (bazı sunucular için önemli)
            client.DefaultRequestHeaders.Add("User-Agent", AndroidUserAgent);
            
            // SSL/TLS ayarlarını Android'e uygun olarak ayarla
            // Not: Modern HttpClient sürümleri genelde bu ayarlamaları otomatik yapıyor
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            // Accept header'ı ekle
            request.Headers.Add("Accept", "image/png,image/jpeg,image/*;q=0.8,*/*;q=0.5");
            
            // Timeout ayarla
            client.Timeout = TimeSpan.FromSeconds(30);
            
            // İsteği gönder ve yanıtı al
            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            // Yanıtı bytea array olarak döndür
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        
        // Önbelleği temizle
        public static void ClearCache()
        {
            lock (_evictionLock)
            {
                // Don't dispose bitmaps in memory cache as they may be in use by UI controls
                // Just clear the references and let GC handle it
                _memoryCache.Clear();
                while (_cacheOrder.TryDequeue(out _)) { }
            }
        }
        
        // Add bitmap to memory cache with LRU eviction
        private static void AddToMemoryCache(string url, Bitmap bitmap)
        {
            if (bitmap == null || string.IsNullOrEmpty(url)) return;
            
            _memoryCache[url] = bitmap;
            _cacheOrder.Enqueue(url);
            
            // Evict oldest entries if cache exceeds limit
            lock (_evictionLock)
            {
                while (_cacheOrder.Count > MaxMemoryCacheSize && _cacheOrder.TryDequeue(out var oldUrl))
                {
                    if (_memoryCache.TryRemove(oldUrl, out var oldBitmap))
                    {
                        // Don't dispose - the bitmap may still be shown in UI
                        // Let GC collect it when no references remain
                    }
                }
            }
        }
    }
} 