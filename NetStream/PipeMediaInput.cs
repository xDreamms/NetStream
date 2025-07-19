using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
//using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStream
{
    //public static class Producer
    //{
    //    public static async Task Run(string videoPath, PipeWriter writer, CancellationToken cancellationToken)
    //    {
    //        try
    //        {
    //            // Video dosyasını aç
    //            using (var fileStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.Read))
    //            {
    //                byte[] buffer = new byte[8192]; // 8 KB bloklarla okuma
    //                int bytesRead;

    //                while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
    //                {
    //                    if (cancellationToken.IsCancellationRequested)
    //                        break;

    //                    // Veriyi Pipe'a yaz
    //                    await writer.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
    //                }
    //            }

    //            // Pipe'ı yazmayı tamamla
    //            await writer.CompleteAsync();
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Producer Error: {ex.Message}");
    //        }
    //    }
    //}
    //public class PipeMediaInput : MediaInput
    //{
    //    private readonly PipeReader _reader;
    //    private bool _completed = true;// True until the media input has been opened by libvlc.

    //    /// <summary>
    //    /// The constructor
    //    /// </summary>
    //    /// <param name="reader">The reader end of the pipe</param>
    //    public PipeMediaInput(PipeReader reader)
    //    {
    //        this._reader = reader;
    //        this.CanSeek = false;
    //    }

    //    /// <summary>
    //    /// Called by libvlc when the stream is closed
    //    /// </summary>
    //    public override void Close()
    //    {
    //        this._reader.Complete();
    //        this._reader.CancelPendingRead();
    //    }

    //    /// <summary>
    //    /// Called by libvlc when the stream opens
    //    /// </summary>
    //    /// <param name="size">Filled with ulong.MaxValue to indicate that the stream has an unknown length</param>
    //    /// <returns>true</returns>
    //    public override bool Open(out ulong size)
    //    {
    //        size = ulong.MaxValue;
    //        this._completed = false;
    //        return true;
    //    }

    //    /// <summary>
    //    /// Called by libvlc when it wants to read more data
    //    /// </summary>
    //    /// <param name="buf">The buffer pointer</param>
    //    /// <param name="len">The buffer length</param>
    //    /// <returns>The number of bytes written to the stream, 0 for EOF, -1 for error</returns>
    //    public unsafe override int Read(IntPtr buf, uint len)
    //    {
    //        if (this._completed)
    //        {
    //            return 0;
    //        }

    //        var readResult = this._reader.ReadAsync().AsTask().GetAwaiter().GetResult();

    //        if (readResult.IsCanceled)
    //        {
    //            return -1;
    //        }

    //        var buffer = (readResult.Buffer.Length > len) ? readResult.Buffer.Slice(0, len) : readResult.Buffer;
    //        var outputBuffer = new Span<byte>(buf.ToPointer(), (int)len);

    //        if (buffer.IsSingleSegment)
    //        {
    //            buffer.FirstSpan.CopyTo(outputBuffer);
    //        }
    //        else
    //        {
    //            var outputPosition = 0;
    //            foreach (var memory in buffer)
    //            {
    //                memory.Span.CopyTo(outputBuffer.Slice(outputPosition));
    //                outputPosition += memory.Length;
    //            }
    //        }

    //        var consumed = (int)buffer.Length;
    //        this._reader.AdvanceTo(buffer.End);

    //        if (readResult.IsCompleted)
    //        {
    //            this._completed = true;
    //        }
    //        return consumed;
    //    }

    //    /// <summary>
    //    /// Seek override that should not be called by libvlc
    //    /// </summary>
    //    /// <param name="offset">The offset at which libvlc wants to seek</param>
    //    /// <returns>false</returns>
    //    public override bool Seek(ulong offset)
    //    {
    //        return false;
    //    }
    //}
}
