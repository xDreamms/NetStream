using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetStream;
using Serilog;

public class TorrentStream : Stream
{
        private const int MinBufferSize = 512 * 1024; // 1MB -> 512KB (RAM optimization)
        private const int MaxBufferSize = 1 * 1024 * 1024; // 2MB -> 1MB (RAM optimization)
        private const int BufferClearThreshold = 5 * 1024 * 1024; // 10MB -> 5MB (RAM optimization)
        private const int BufferAdjustmentStep = 256 * 1024; // 512KB -> 256KB (RAM optimization)
        private const int CheckIntervalMs = 50;

        private long position;
        private Int64 length;
        private readonly object syncRoot = new object();
        private bool disposed;
        private int pieceLength;
        private FileStream fileStream;
        private byte[] buffer;
        private long bufferStart;
        private int bufferLength;
        private string hash;
        private static int startIndex;
        private static int endIndex;

        public static string FilePath;
        private TorrentStream(string hash)
        {
            this.hash = hash;
            this.position = 0;
            this.bufferStart = 0;
            this.bufferLength = 0;
            this.disposed = false;
        }

        private static NetStream.TorrentFile? file;
        public static async Task<TorrentStream> Create(Item item, int fileIndex, CancellationToken cancellationToken = default)
        {
            var files = await Libtorrent.GetFiles(item.Hash);
            if (files == null || files.Count == 0)
            {
                throw new InvalidOperationException($"Torrent dosyaları bulunamadı. Hash: {item.Hash}");
            }
            
            file = files.FirstOrDefault(x => x.Index == fileIndex);
            if (file == null)
            {
                throw new InvalidOperationException($"Dosya bulunamadı. Hash: {item.Hash}, FileIndex: {fileIndex}");
            }
            
            if (string.IsNullOrEmpty(file.FullPath))
            {
                throw new InvalidOperationException($"Dosya yolu boş. Hash: {item.Hash}, FileIndex: {fileIndex}");
            }
            
            var filePath = file.FullPath;
            FilePath = filePath;
            int retryCount = 0;
            while (!File.Exists(filePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken);
                retryCount++;
                if (retryCount > 50)
                    Log.Error($"Dosya bulunamadı: {filePath}");
            }


            var pieceRange = await Libtorrent.GetFilePieceRange(item.Hash,fileIndex);
            retryCount = 0;
            while (pieceRange == null || pieceRange.StartPieceIndex < 0 || pieceRange.EndPieceIndex < 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken);
                pieceRange = await Libtorrent.GetFilePieceRange(item.Hash,fileIndex);
                retryCount++;
                if (retryCount > 50)
                {
                    Log.Error("Dosya parça aralığı alınamadı.");
                    throw new InvalidOperationException($"Dosya parça aralığı alınamadı. Hash: {item.Hash}, FileIndex: {fileIndex}");
                }
            }

            startIndex = pieceRange.StartPieceIndex;
            endIndex = pieceRange.EndPieceIndex;

            var pieceSize = await Libtorrent.GetPieceSize(item.Hash);
            if (pieceSize <= 0)
            {
                throw new InvalidOperationException($"Geçersiz parça boyutu: {pieceSize}. Hash: {item.Hash}");
            }

            var stream = new TorrentStream(item.Hash)
            {
                pieceLength = (int)pieceSize,
                length = file.Size,
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, MinBufferSize, useAsync: true),
                buffer = new byte[MinBufferSize]
            };
            // İlk tamponu doldurarak video hemen açılmasını sağlıyoruz
            await stream.FillBufferAsync((int)(stream.position / stream.pieceLength) + startIndex, cancellationToken);
            return stream;
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek => true;
        public override long Length => this.length;

        public override long Position
        {
            get => this.position;
            set => Seek(value, SeekOrigin.Begin);
        }



        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public override int Read(byte[] outputBuffer, int offset, int count)
        {
            // Senkron Read() için asenkron ReadAsync metodunu kullanıp sonucunu bekliyoruz
            return ReadAsync(outputBuffer, offset, count, cancellationTokenSource.Token).GetAwaiter().GetResult();
        }

        public override async Task<int> ReadAsync(byte[] outputBuffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (disposed) return 0;
            int totalRead = 0;

            while (totalRead < count)
            {
                cancellationToken.ThrowIfCancellationRequested();

                lock (syncRoot)
                {
                    // Eğer mevcut pozisyon tamponda yoksa, gerekiyorsa tampon boyutunu resetleyin
                    if (!IsInBuffer(position) && buffer.Length != MinBufferSize && position % MaxBufferSize == 0)
                    {
                        Array.Resize(ref buffer, MinBufferSize);
                    }
                }

                // Gerektiğinde tamponu dolduruyoruz
                int currentPiece = (int)(position / pieceLength) + startIndex;
                await FillBufferAsync(currentPiece, cancellationToken);

                int bytesAvailable = (int)Math.Min(count - totalRead, (bufferStart + bufferLength) - position);
                if (bytesAvailable <= 0)
                    break;

                int bufferOffset = (int)(position - bufferStart);
                int bytesToCopy = Math.Min(bytesAvailable, buffer.Length - bufferOffset);
                Buffer.BlockCopy(buffer, bufferOffset, outputBuffer, offset + totalRead, bytesToCopy);

                totalRead += bytesToCopy;
                position += bytesToCopy;

                // Eğer okunan verinin miktarı belirli eşik değeri aştıysa tamponu temizleyelim
                if (position - bufferStart > BufferClearThreshold)
                {
                    lock (syncRoot)
                    {
                        Array.Clear(buffer, 0, buffer.Length);
                        bufferStart = 0;
                        bufferLength = 0;
                    }
                }
            }
            return totalRead;
        }

        private bool allPiecesDownlaoded = false;
        private async Task FillBufferAsync(int currentPiece, CancellationToken cancellationToken)
        {
             // Dosya tamamlanmamışsa piece kontrolü yap
             try
             {
                 if (!allPiecesDownlaoded)
                 {
                     // Piece durumunu önce al (hiç bekleme olmadan)
                     var statusInfo = await Libtorrent.GetStatus(hash);
                     var pieces = statusInfo?.Pieces;

                     if (pieces.All(x => x == true))
                     {
                         allPiecesDownlaoded = true;
                     }
                     // İhtiyaç duyulan parça sayısını hesapla - video başlangıcında daha fazla, ortada daha az
                     double progressRatio = (double)currentPiece / (endIndex - startIndex + 1);
                     int requiredPieces = progressRatio < 0.02 ? 10 : (progressRatio < 0.10 ? 6 : 3); // Beginning needs more pieces

                     // Eğer mevcut parça ve çevresi indirilmiş mi kontrol et
                     bool readyToPlay = await CheckPieceAvailability(currentPiece, requiredPieces);

                     // Eğer parçalar hazır değilse bir süre bekleyelim - başlangıçta daha uzun bekle
                     int maxWaitMs = progressRatio < 0.02 ? 20000 : 8000; // 20s at start, 8s otherwise (improved from 10s)
                     int waitMs = 0;
                     int checkInterval = 100; // 100ms

                     while (!readyToPlay && waitMs < maxWaitMs)
                     {
                         cancellationToken.ThrowIfCancellationRequested();

                         // Kısa bir süre bekle
                         await Task.Delay(checkInterval, cancellationToken);
                         waitMs += checkInterval;

                         // Durumu tekrar kontrol et
                         statusInfo = await Libtorrent.GetStatus(hash);
                         pieces = statusInfo?.Pieces;
                         readyToPlay = await CheckPieceAvailability(currentPiece, requiredPieces);

                         // Her 500ms'de bir durum mesajı ver
                         if (waitMs % 500 == 0)
                         {
                             Console.WriteLine(
                                 $"Parça {currentPiece} için bekleniyor ({waitMs}ms), hazır: {readyToPlay}");
                         }
                     }

                     // Hala hazır değilse uyar ama devam et - video atlayabilir
                     if (!readyToPlay)
                     {
                         Console.WriteLine($"Parça {currentPiece} hazır değil ama devam ediliyor. Video atlayabilir.");
                     }
                 }

                 if (buffer == null)
                 {
                     Console.WriteLine("Buffer null, yeniden oluşturuluyor.");
                     buffer = new byte[MinBufferSize];
                 }

                 // Tampon boyutunu konum ve video uzunluğu oranına göre ayarlıyoruz
                 int optimalBufferSize = MinBufferSize +
                                         (int)(BufferAdjustmentStep * Math.Min(1.0, (double)position / length * 4));
                 if (buffer.Length != optimalBufferSize)
                 {
                     Array.Resize(ref buffer, optimalBufferSize);
                 }

                 try
                 {
                     Array.Clear(buffer, 0, buffer.Length);
                     fileStream.Position = position;
                     bufferStart = position;
                     bufferLength = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                 }
                 catch (Exception ex)
                 {
                     Console.WriteLine($"Dosyadan okuma hatası: {ex.Message}");
                     bufferLength = 0;
                 }
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"FillBufferAsync hatası: {ex.Message}");
             }
        }

        private async Task<bool> CheckPieceAvailability(int startPiece, int count)
        {
            try
            {
                var status = await Libtorrent.GetStatus(hash);
                if (status == null || status.Pieces == null || status.Pieces.Count() == 0)
                {
                    Console.WriteLine($"Torrent parça bilgisi alınamadı. Hash: {hash}");
                    return false;
                }
                
                int endPiece = Math.Min(startPiece + count, endIndex);
                int availablePieces = 0;
                
                for (int i = startPiece; i <= endPiece; i++)
                {
                    // Eğer parça indeksi geçerli değilse (array sınırları dışında)
                    if (i < 0 || i >= status.Pieces.Count())
                    {
                        Console.WriteLine($"Geçersiz parça indeksi: {i}, toplam: {status.Pieces.Count()}");
                        continue;
                    }
                    
                    if (status.Pieces.ElementAt(i))
                    {
                        availablePieces++;
                    }
                }
                
                // En az 1 parça varsa true döndür - akışa başlayabilelim
                bool result = availablePieces > 0;
                if (result && availablePieces < (endPiece - startPiece + 1))
                {
                    Console.WriteLine($"Bazı parçalar hazır ({availablePieces}/{endPiece - startPiece + 1}), video başlatılıyor.");
                }
                
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Parça durumu kontrolünde hata: {e.Message}");
                return false;
            }
        }

        private bool IsInBuffer(long pos) => pos >= bufferStart && pos < (bufferStart + bufferLength);

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (syncRoot)
            {
                long newPosition = origin switch
                {
                    SeekOrigin.Begin => offset,
                    SeekOrigin.Current => position + offset,
                    SeekOrigin.End => length + offset,
                    _ => position
                };

                if (newPosition < 0 || newPosition > length)
                {
                    Console.WriteLine("Geçersiz seek pozisyonu.");
                    return position;
                }
                
                if (Math.Abs(newPosition - position) > pieceLength * 2)
                    bufferLength = 0;

                position = newPosition;
                return position;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            
        }

        public override void SetLength(long value)
        {
            
        }

        public override void Flush()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    fileStream?.Dispose();
                    if (buffer != null)
                    {
                        Array.Clear(buffer, 0, buffer.Length);
                        buffer = null;
                    }
                }
                cancellationTokenSource.Cancel();
                disposed = true;
                base.Dispose(disposing);
            }
        }

        ~TorrentStream() => Dispose(false);
}