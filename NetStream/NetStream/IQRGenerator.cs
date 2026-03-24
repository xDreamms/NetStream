using System;
using System.Threading.Tasks;

namespace NetStream.Services
{
    /// <summary>
    /// QR kod üreteci için platform bağımsız interface
    /// </summary>
    public interface IQRGenerator
    {
        /// <summary>
        /// WebSocket bağlantısı için QR kod oluşturur
        /// </summary>
        /// <param name="port">WebSocket port numarası</param>
        /// <param name="endpoint">WebSocket endpoint yolu</param>
        /// <returns>QR kod bilgisi</returns>
        Task<QRCodeGenerationResult> GenerateWebSocketQRCodeAsync(int port, string endpoint);
        
        /// <summary>
        /// Verilen metin için QR kod oluşturur
        /// </summary>
        /// <param name="content">QR kodda saklanacak içerik</param>
        /// <returns>QR kod bilgisi</returns>
        Task<QRCodeGenerationResult> GenerateQRCodeAsync(string content);
    }
    
    /// <summary>
    /// QR kod oluşturma sonucu
    /// </summary>
    public class QRCodeGenerationResult
    {
        /// <summary>
        /// QR kod görüntüsü (platform bağımsız veri olarak byte dizisi)
        /// </summary>
        public byte[] QRCodeImageData { get; set; }
        
        /// <summary>
        /// QR kodun içerdiği metin
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// Sunucu IP Adresi
        /// </summary>
        public string ServerIPAddress { get; set; }
        
        /// <summary>
        /// İşlem başarılı oldu mu?
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Hata mesajı (başarısız olursa)
        /// </summary>
        public string ErrorMessage { get; set; }
    }
} 