using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetStream.Services;
using NetStream.Views;

namespace NetStream
{
     
    public class VideoServer
    {
        private HttpListener _listener;
        private int _port;
        private bool _isRunning;
        private Dictionary<string, string> _videoFiles = new Dictionary<string, string>();
        private CancellationTokenSource _cancellationTokenSource;
        private string _serverAddress;
        
        // Server URL artık dışarıdan verilebilir
        public string ServerUrl => _serverAddress;
        
        public event EventHandler<string> VideoRequested;
        
        public VideoServer(int port = 5000)
        {
            _port = port;
        }
        
        /// <summary>
        /// Server'ı başlatır
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning) return;
            
            try
            {
                _listener = new HttpListener();
                
                // Tüm gelen istekleri dinle
                _listener.Prefixes.Add($"http://*:{_port}/");
                
                try
                {
                    _listener.Start();
                    Console.WriteLine($"[VideoServer] Wildcard adresle başlatma başarılı: http://*:{_port}/");
                }
                catch (HttpListenerException ex)
                {
                    Console.WriteLine($"[VideoServer] Wildcard adresle başlatma başarısız: {ex.Message}, alternatif deneniyor...");
                    
                    // Windows'ta "*" çalışmazsa, "+" dene
                    _listener = new HttpListener();
                    _listener.Prefixes.Add($"http://+:{_port}/");
                    
                    try 
                    {
                        _listener.Start();
                        Console.WriteLine($"[VideoServer] '+' adresle başlatma başarılı: http://+:{_port}/");
                    }
                    catch (HttpListenerException ex2)
                    {
                        Console.WriteLine($"[VideoServer] '+' adresle başlatma başarısız: {ex2.Message}, localhost deneniyor...");
                        
                        // "*" ve "+" çalışmazsa, localhost'u dene
                        _listener = new HttpListener();
                        _listener.Prefixes.Add($"http://localhost:{_port}/");
                        
                        try
                        {
                            _listener.Start();
                            Console.WriteLine($"[VideoServer] Localhost adresle başlatma başarılı: http://localhost:{_port}/");
                            
                            // Localhost çalışırsa, sadece yerel makineden erişim olacak
                            _serverAddress = $"http://localhost:{_port}";
                            Console.WriteLine("⚠️ [VideoServer] SADECE localhost modunda başlatıldı. Diğer cihazlardan erişim olmayacak!");
                        }
                        catch (Exception ex3)
                        {
                            Console.WriteLine($"[VideoServer] Localhost adresle başlatma başarısız: {ex3.Message}");
                            throw new Exception("Hiçbir bağlantı metodu çalışmadı. Video sunucusu başlatılamadı.");
                        }
                    }
                }
                
                _isRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();
                
                // IP adresi belirle (sadece logging için)
                string ipAddress = GetBestNetworkIPAddress();
                _serverAddress = $"http://{ipAddress}:{_port}";
                
                Console.WriteLine($"[VideoServer] Başlatıldı: {ServerUrl}");
                Console.WriteLine($"⚠️ [VideoServer] iPhone/iPad gibi cihazlardan server'a erişmek için bu adresi kullanın: {_serverAddress}");
                Console.WriteLine("⚠️ [VideoServer] iOS cihazlarda 'App Transport Security' hatası alıyorsanız, Info.plist'e güvenlik istisnası eklemeniz gerekebilir.");
                
                await ListenForRequestsAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VideoServer] Başlatma hatası: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Server'ı durdurur
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;
            
            try
            {
                _cancellationTokenSource?.Cancel();
                _listener?.Stop();
                _listener?.Close();
                _isRunning = false;
                Console.WriteLine("[VideoServer] Durduruldu");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VideoServer] Durdurma hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Video ekler ve video ID'si döndürür
        /// </summary>
        public string AddVideo(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Video dosyası bulunamadı", filePath);
            
            string videoId = Guid.NewGuid().ToString("N");
            _videoFiles[videoId] = filePath;
            
            Console.WriteLine($"Video eklendi: {Path.GetFileName(filePath)}, ID: {videoId}");
            return videoId;
        }
        
        /// <summary>
        /// Doğrudan dosya yolu ile video ekle
        /// </summary>
        public string AddDirectVideoPath(string fullPath, string videoName = null)
        {
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Video dosyası bulunamadı: {fullPath}");
                throw new FileNotFoundException("Video dosyası bulunamadı", fullPath);
            }
            
            // URL'de kullanılacak benzersiz ID oluştur
            string videoId = Guid.NewGuid().ToString("N");
            
            // Dosya yolunu kaydet
            _videoFiles[videoId] = fullPath;
            
            Console.WriteLine($"Video direkt eklendi: {videoName ?? Path.GetFileName(fullPath)}, ID: {videoId}, Path: {fullPath}");
            return videoId;
        }
        
        /// <summary>
        /// Video ID ile video URL'sini döndürür
        /// </summary>
        public string GetVideoUrl(string videoId)
        {
            if (!_videoFiles.ContainsKey(videoId))
                throw new KeyNotFoundException("Video ID bulunamadı");
                
            return $"{ServerUrl}/video/{videoId}";
        }
        
        /// <summary>
        /// Client'ın erişebileceği tüm videoları listeler
        /// </summary>
        public Dictionary<string, string> ListVideos()
        {
            var result = new Dictionary<string, string>();
            
            foreach (var entry in _videoFiles)
            {
                result[entry.Key] = Path.GetFileName(entry.Value);
            }
            
            return result;
        }
        
        /// <summary>
        /// WebSocket üzerinden gelen bir dosya yolu için video ID oluşturur ve URL'sini döndürür
        /// </summary>
        public string PrepareVideoFromPath(string filePath, string torrentName = null)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"PrepareVideoFromPath: Dosya bulunamadı: {filePath}");
                    return null;
                }
                
                // Dosya ekle ve ID'sini al
                string videoId = AddDirectVideoPath(filePath, Path.GetFileName(filePath));
                
                // Video URL'si oluştur
                string videoUrl = GetVideoUrl(videoId);
                
                Console.WriteLine($"PrepareVideoFromPath: Video hazır: {filePath}, URL: {videoUrl}");
                
                return videoUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PrepareVideoFromPath Hatası: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// En iyi yerel ağ IP adresini alır (Tailscale hariç)
        /// </summary>
        private string GetBestNetworkIPAddress()
        {
            return QRCodeGeneratorControl.GetLocalIPv4Address();
        }
        
        private async Task ListenForRequestsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    
                    // İstek paralel işlensin
                    _ = Task.Run(() => ProcessRequestAsync(context), cancellationToken);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"[VideoServer] İstek dinleme hatası: {ex.Message}");
                }
            }
        }
        
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            
            try
            {
                string path = request.Url.AbsolutePath;
                Console.WriteLine($"İstek alındı: {path}, Uzak adres: {request.RemoteEndPoint}");
                
                if (path.StartsWith("/video/"))
                {
                    string videoId = path.Substring("/video/".Length);
                    
                    if (_videoFiles.TryGetValue(videoId, out string filePath))
                    {
                        VideoRequested?.Invoke(this, filePath);
                        await SendVideoAsync(response, filePath, request);
                    }
                    else
                    {
                        Send404Response(response);
                    }
                }
                else if (path == "/list")
                {
                    // Video listesini JSON olarak döndür
                    await SendVideoListAsync(response);
                }
                else
                {
                    Send404Response(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İstek işleme hatası: {ex.Message}");
                try
                {
                    response.StatusCode = 500;
                    response.Close();
                }
                catch { /* İkincil hataları sessizce geç */ }
            }
        }
        
        private async Task SendVideoAsync(HttpListenerResponse response, string filePath, HttpListenerRequest request)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                long fileLength = fileInfo.Length;
                
                response.ContentType = GetMimeTypeFromExtension(Path.GetExtension(filePath));
                response.ContentLength64 = fileLength;
                response.AddHeader("Accept-Ranges", "bytes");
                
                // Partial Content (Range) desteği
                if (request.Headers["Range"] != null)
                {
                    string rangeHeader = request.Headers["Range"];
                    long start, end;
                    ParseRangeHeader(rangeHeader, fileLength, out start, out end);
                    
                    response.StatusCode = 206; // Partial Content
                    response.AddHeader("Content-Range", $"bytes {start}-{end}/{fileLength}");
                    response.ContentLength64 = end - start + 1;
                    
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.Seek(start, SeekOrigin.Begin);
                        await StreamToOutputAsync(fileStream, response.OutputStream, end - start + 1);
                    }
                }
                else
                {
                    response.StatusCode = 200;
                    
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        await StreamToOutputAsync(fileStream, response.OutputStream, fileLength);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Video gönderme hatası: {ex.Message}");
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }
        }
        
        private async Task SendVideoListAsync(HttpListenerResponse response)
        {
            response.ContentType = "application/json";
            response.StatusCode = 200;
            
            StringBuilder json = new StringBuilder();
            json.Append("{\"videos\":[");
            
            bool first = true;
            foreach (var entry in _videoFiles)
            {
                if (!first) json.Append(",");
                first = false;
                
                string fileName = Path.GetFileName(entry.Value);
                json.Append($"{{\"id\":\"{entry.Key}\",\"name\":\"{fileName}\",\"url\":\"{GetVideoUrl(entry.Key)}\"}}");
            }
            
            json.Append("]}");
            
            byte[] buffer = Encoding.UTF8.GetBytes(json.ToString());
            response.ContentLength64 = buffer.Length;
            
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }
        
        private void Send404Response(HttpListenerResponse response)
        {
            response.StatusCode = 404;
            response.ContentType = "text/plain";
            byte[] buffer = Encoding.UTF8.GetBytes("Dosya bulunamadı");
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }
        
        private async Task StreamToOutputAsync(Stream input, Stream output, long count)
        {
            byte[] buffer = new byte[65536]; // 64KB buffer
            long bytesRemaining = count;
            
            while (bytesRemaining > 0)
            {
                int bytesToRead = (int)Math.Min(bytesRemaining, buffer.Length);
                int bytesRead = await input.ReadAsync(buffer, 0, bytesToRead);
                
                if (bytesRead == 0) break;
                
                await output.WriteAsync(buffer, 0, bytesRead);
                bytesRemaining -= bytesRead;
            }
            
            await output.FlushAsync();
        }
        
        private void ParseRangeHeader(string rangeHeader, long fileLength, out long start, out long end)
        {
            start = 0;
            end = fileLength - 1;
            
            if (string.IsNullOrEmpty(rangeHeader)) return;
            
            // Örnek: "bytes=0-1023"
            string[] ranges = rangeHeader.Replace("bytes=", "").Split('-');
            
            if (ranges.Length > 0 && !string.IsNullOrEmpty(ranges[0]))
            {
                start = long.Parse(ranges[0]);
            }
            
            if (ranges.Length > 1 && !string.IsNullOrEmpty(ranges[1]))
            {
                end = long.Parse(ranges[1]);
            }
            
            // Sınır kontrolü
            if (start < 0) start = 0;
            if (end >= fileLength) end = fileLength - 1;
        }
        
        private string GetMimeTypeFromExtension(string extension)
        {
            switch (extension.ToLower())
            {
                case ".mp4": return "video/mp4";
                case ".m4v": return "video/mp4";
                case ".mov": return "video/quicktime";
                case ".avi": return "video/x-msvideo";
                case ".wmv": return "video/x-ms-wmv";
                case ".mkv": return "video/x-matroska";
                default: return "application/octet-stream";
            }
        }
    }
} 