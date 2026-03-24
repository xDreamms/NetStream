using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetStream.Models;

namespace NetStream.Services
{
    public interface ITorrentService
    {
        event Action<DownloadInfo> DownloadStarted;
        event Action<DownloadInfo> DownloadProgress;
        event Action<DownloadInfo> DownloadCompleted;
        event Action<DownloadInfo, string> DownloadError;
        
        Task Initialize();
        Task<DownloadInfo> AddTorrent(string magnetUri, string title);
        void PauseTorrent(string downloadId);
        void ResumeTorrent(string downloadId);
        void CancelTorrent(string downloadId);
        List<DownloadInfo> GetDownloads();
        string GetStreamUrl(string downloadId, string fileName);
    }
} 