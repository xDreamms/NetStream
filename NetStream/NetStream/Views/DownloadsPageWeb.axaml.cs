using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using NetStream.Services;
using NetStream.Models;
using Microsoft.JSInterop;

namespace NetStream.Views;

public partial class DownloadsPageWeb : UserControl
{
     public static DownloadsPageWeb Instance { get; private set; }
     public static ObservableCollection<Item> torrents = new ObservableCollection<Item>();
     private Item selectedTorrent;
     private List<string> finishedTorrents = new List<string>();
     //private readonly WebTorrentService _torrentService;
        
     public DownloadsPageWeb()
     {
         InitializeComponent();
         Instance = this;
         // _torrentService = App.Services.GetService(typeof(ITorrentService)) as WebTorrentService;
         //
         // if (_torrentService != null)
         // {
         //     _torrentService.DownloadProgress += TorrentService_DownloadProgress;
         //     _torrentService.DownloadCompleted += TorrentService_DownloadCompleted;
         //     _torrentService.DownloadError += TorrentService_DownloadError;
         // }
         
         OnLoad();
     }

     private async void OnLoad()
     {
         DownloadsDisplay.ItemsSource = torrents;
         await StartTorrenting();
     }
     
     private void TorrentService_DownloadProgress(DownloadInfo download)
     {
         // Handle progress update - this is used to update UI items
         var item = GetItemByHash(download.Id);
         if (item != null)
         {
             item.DownloadPercent = download.Progress * 100;
             item.DownloadSpeed = FormatSpeed(download.DownloadSpeed);
             item.Eta = CalculateEta(download);
             
             if (download.Progress >= 1.0)
             {
                 item.IsCompleted = true;
                 item.DownloadPercent = 100;
                 item.DownloadSpeed = "";
                 item.Eta = "Completed";
             }
         }
     }
     
     private void TorrentService_DownloadCompleted(DownloadInfo download)
     {
         var item = GetItemByHash(download.Id);
         if (item != null)
         {
             item.IsCompleted = true;
             item.DownloadPercent = 100;
             item.DownloadSpeed = "";
             item.Eta = "Completed";
         }
     }
     
     private void TorrentService_DownloadError(DownloadInfo download, string errorMessage)
     {
         Console.WriteLine($"Download error: {errorMessage}");
     }
     
     private Item GetItemByHash(string hash)
     {
         return torrents.FirstOrDefault(t => t.Hash == hash);
     }
     
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
     
     private string CalculateEta(DownloadInfo download)
     {
         if (download.DownloadSpeed <= 0)
             return "∞";
             
         long remainingBytes = (long)((1 - download.Progress) * download.Size);
         double secondsRemaining = remainingBytes / (double)download.DownloadSpeed;
         
         if (secondsRemaining < 60)
             return $"{secondsRemaining:0}s";
         if (secondsRemaining < 3600)
             return $"{secondsRemaining / 60:0}m {secondsRemaining % 60:0}s";
         if (secondsRemaining < 86400)
             return $"{secondsRemaining / 3600:0}h {(secondsRemaining % 3600) / 60:0}m";
             
         return $"{secondsRemaining / 86400:0}d {(secondsRemaining % 86400) / 3600:0}h";
     }
    
     private async Task StartTorrenting()
     {
         /*try
         {
             await _torrentService.Initialize();
             var downloads = _torrentService.GetDownloads();
             
             foreach (var torrent in torrents)
             {
                 if (!torrent.IsCompleted)
                 {
                     if (torrent.Hash == null || String.IsNullOrWhiteSpace(torrent.Hash))
                     {
                         var downloadInfo = await _torrentService.AddTorrent(torrent.Link, torrent.Title);
                         
                         if (downloadInfo == null)
                         {
                             torrents.Remove(torrent);
                             return;
                         }
                         else
                         {
                             torrent.Hash = downloadInfo.Id;
                             torrent.Magnet = torrent.Link;
                             
                             var download = downloads.FirstOrDefault(x => x.Id == downloadInfo.Id);
                             if (download != null)
                             {
                                 torrent.IsCompleted = download.Progress >= 1;
                             }
                         }
                     }
                     else if (downloads.Any(x => x.Id == torrent.Hash))
                     {
                         var download = downloads.FirstOrDefault(x => x.Id == torrent.Hash);
                         if (download != null)
                         {
                             // Update the torrent status from existing download
                             torrent.DownloadPercent = download.Progress * 100;
                             torrent.DownloadSpeed = FormatSpeed(download.DownloadSpeed);
                             torrent.Eta = CalculateEta(download);
                             torrent.IsCompleted = download.Progress >= 1;
                             
                             if (download.Status != DownloadStatus.Downloading)
                             {
                                 await _torrentService.ResumeTorrent(torrent.Hash);
                             }
                         }
                     }
                     else
                     {
                         var downloadInfo = await _torrentService.AddTorrent(torrent.Link, torrent.Title);
                         
                         if (downloadInfo == null)
                         {
                             torrents.Remove(torrent);
                         }
                         else
                         {
                             torrent.Hash = downloadInfo.Id;
                             torrent.Magnet = torrent.Link;
                             
                             var download = downloads.FirstOrDefault(x => x.Id == downloadInfo.Id);
                             if (download != null)
                             {
                                 torrent.IsCompleted = download.Progress >= 1;
                             }
                         }
                     }
                 }
             }
         }
         catch (System.Exception e)
         {
             var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
             Console.WriteLine(errorMessage);
         }*/
     }

     public async Task AddTorrent(Item torrent)
     {
         /*try
         {
             if (!torrent.IsCompleted)
             {
                 await _torrentService.Initialize();
                 var downloadInfo = await _torrentService.AddTorrent(torrent.Link, torrent.Title);
                 
                 if (downloadInfo == null)
                 {
                     torrents.Remove(torrent);
                 }
                 else
                 {
                     torrent.Hash = downloadInfo.Id;
                     torrent.Magnet = torrent.Link;
                     
                     // Update any other torrents with the same ID
                     foreach (var t in torrents)
                         if (t.MovieId == torrent.MovieId && t.SeasonNumber == torrent.SeasonNumber
                                                         && t.EpisodeNumber == torrent.EpisodeNumber)
                         {
                             t.Hash = downloadInfo.Id;
                         }
                 }
             }
         }
         catch (System.Exception e)
         {
             var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
             Console.WriteLine(errorMessage);
         }*/
     }

   
    

     private void DownloadsPage_OnLoaded(object sender, RoutedEventArgs e)
     {
         /*Task.Run(async () =>
         {
             while (true)
             {
                 try
                 {
                     var downloads = _torrentService.GetDownloads();
                     
                     foreach (var item in torrents)
                     {
                         var download = downloads.FirstOrDefault(d => d.Id == item.Hash);
                         if (download != null)
                         {
                             if (!item.IsCompleted && !finishedTorrents.Contains(item.Hash))
                             {
                                 if (download.Progress >= 1.0)
                                 {
                                     finishedTorrents.Add(item.Hash);
                                     await _torrentService.PauseTorrent(item.Hash);
                                     item.IsCompleted = true;
                                 }
                             }
                             else if (item.IsCompleted && !finishedTorrents.Contains(item.Hash))
                             {
                                 if (download.Status == DownloadStatus.Completed)
                                 {
                                     await _torrentService.PauseTorrent(item.Hash);
                                     finishedTorrents.Add(item.Hash);
                                 }
                             }
                         }
                     }
                 }
                 catch (Exception exception)
                 {
                     var errorMessage = $"Error in DownloadsPage_OnLoaded: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                     Console.WriteLine(errorMessage);
                 }

                 await Task.Delay(TimeSpan.FromSeconds(10));
             }
         });*/
     }

     private void DownloadsItem_OnPointerPressed(object? sender, PointerPressedEventArgs e)
     {
         
     }

     private void ContextMenuPause_OnLoaded(object? sender, RoutedEventArgs e)
     {
         
     }

     private void MenuItem_OnClick(object? sender, RoutedEventArgs e)
     {
         
     }

     private void DownloadsDisplay_OnContextRequested(object? sender, ContextRequestedEventArgs e)
     {
         
     }

     private void DownloadsPage_OnUnloaded(object? sender, RoutedEventArgs e)
     {
         
     }
}