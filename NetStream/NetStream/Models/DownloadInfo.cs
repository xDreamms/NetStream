using System;
using System.Collections.Generic;

namespace NetStream.Models
{
    public class DownloadInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string MagnetUri { get; set; }
        public DownloadStatus Status { get; set; }
        public double Progress { get; set; }
        public long DownloadSpeed { get; set; }
        public long UploadSpeed { get; set; }
        public int Peers { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ErrorMessage { get; set; }
        
        public long Size { get; set; }
        
        public string FormattedDownloadSpeed => FormatSpeed(DownloadSpeed);
        public string FormattedUploadSpeed => FormatSpeed(UploadSpeed);
        
        private string FormatSpeed(long bytesPerSecond)
        {
            string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
            double speed = bytesPerSecond;
            int order = 0;
            
            while (speed >= 1024 && order < sizes.Length - 1)
            {
                order++;
                speed = speed / 1024;
            }
            
            return $"{speed:0.##} {sizes[order]}";
        }
    }
    
    public enum DownloadStatus
    {
        Preparing,
        Downloading,
        Paused,
        Completed,
        Error
    }
    
   
} 