using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetStream;
using Serilog;
using TorrentWrapper;

public class TorrentStream : Stream
{
        private const int MinBufferSize = 1 * 1024 * 1024; 
        private const int MaxBufferSize = 2 * 1024 * 1024;   
        private const int BufferClearThreshold = 10 * 1024 * 1024; 
        private const int BufferAdjustmentStep = 512 * 1024; 
        private const int CheckIntervalMs = 50;

        private TorrentHandleWrapper torrent;
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

        private TorrentStream(TorrentHandleWrapper torrent)
        {
            this.torrent = torrent;
            this.hash = torrent.Hash;
            this.position = 0;
            this.bufferStart = 0;
            this.bufferLength = 0;
            this.disposed = false;
        }

        private static TorrentFile file;
        public static async Task<TorrentStream> Create(Item item, int fileIndex, CancellationToken cancellationToken = default)
        {
            var torrent = await Libtorrent.GetTorrentHandle(item);
            var files = await Libtorrent.GetFiles(torrent.Hash);
            file = files.FirstOrDefault(x => x.Index == fileIndex);
            var filePath = file.FullPath;

            int retryCount = 0;
            while (!File.Exists(filePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken);
                retryCount++;
                if (retryCount > 50)
                    Log.Error($"Dosya bulunamadı: {filePath}");
            }


            var pieceRange = torrent.GetFilePieceRange(fileIndex);
            retryCount = 0;
            while (pieceRange == null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken);
                pieceRange = torrent.GetFilePieceRange(fileIndex);
                retryCount++;
                if (retryCount > 50)
                    Log.Error("Dosya parça aralığı alınamadı.");
            }

            startIndex = pieceRange.StartPieceIndex;
            endIndex = pieceRange.EndPieceIndex;

            var stream = new TorrentStream(torrent)
            {
                pieceLength = torrent.PieceSize,
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

        private async Task FillBufferAsync(int currentPiece, CancellationToken cancellationToken)
        {
            if (torrent == null || !torrent.IsValid)
            {
                Libtorrent.torrentHandleWrappers.Remove(torrent.Hash);
                Libtorrent.Torrents.Remove(torrent);
                torrent = await Libtorrent.GetTorrentHandle(hash);
            }
            // Eğer torrent seeding değilse, gerekli parçaların hazır olmasını bekleyelim
            if (torrent.GetTorrentState != TorrentState.Seeding || !file.IsCompleted)
            {
                int requiredPieces = (int)Math.Ceiling((double)MaxBufferSize / pieceLength);
                while (!CheckPieceAvailability(currentPiece, requiredPieces))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(CheckIntervalMs, cancellationToken);
                }
            }
            if (buffer == null) return;
            // Tampon boyutunu konum ve video uzunluğu oranına göre ayarlıyoruz
            int optimalBufferSize = MinBufferSize + (int)(BufferAdjustmentStep * Math.Min(1.0, (double)position / length * 4));
            if (buffer.Length != optimalBufferSize)
                Array.Resize(ref buffer, optimalBufferSize);

            Array.Clear(buffer, 0, buffer.Length);
            fileStream.Position = position;
            bufferStart = position;
            bufferLength = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        private bool CheckPieceAvailability(int startPiece, int count)
        {
            try
            {
                var pieces = Libtorrent.GetStatus(torrent.Hash).Pieces.ToList();
                int endPiece = Math.Min(startPiece + count, endIndex);
                for (int i = startPiece; i <= endPiece; i++)
                {
                    if (!pieces[i])
                        return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
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