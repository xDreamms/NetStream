using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NetStream.Services;

/// <summary>
/// Service for automatically selecting the best torrent based on quality, seeds, size, and user's connection speed
/// </summary>
public class TorrentSelectionService
{
    private const string YTS_INDEXER_NAME = "YTS";
    
    // Internet speed thresholds in Mbps
    private const double SPEED_THRESHOLD_720P = 5.0;
    private const double SPEED_THRESHOLD_1080P = 25.0;
    
    // Quality scores
    private const int QUALITY_SCORE_720P = 60;
    private const int QUALITY_SCORE_1080P = 80;
    private const int QUALITY_SCORE_2160P = 100;
    
    // Scoring weights
    private const double WEIGHT_SEEDS = 0.3;
    private const double WEIGHT_QUALITY = 0.4;
    private const double WEIGHT_SIZE = 0.2;
    private const double WEIGHT_SOURCE = 0.1;
    
    public class TorrentScore
    {
        public Item Torrent { get; set; }
        public double Score { get; set; }
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// Select the best torrent from a list based on multiple factors
    /// </summary>
    /// <param name="torrents">List of available torrents</param>
    /// <param name="preferredQuality">User's preferred quality (e.g. "1080p", "720p", "2160p", "480p"). If null, uses automatic selection.</param>
    public async Task<Item> SelectBestTorrentAsync(List<Item> torrents, string preferredQuality = null)
    {
        if (torrents == null || !torrents.Any())
        {
            Console.WriteLine("[TorrentSelectionService] No torrents available");
            return null;
        }
        
        Console.WriteLine($"[TorrentSelectionService] Analyzing {torrents.Count} torrents");
        
        // Get available disk space  
        long availableSpaceGB = GetAvailableDiskSpace();
        Console.WriteLine($"[TorrentSelectionService] Available disk space: {availableSpaceGB} GB");
        
        // Determine optimal quality - use user preference if provided, otherwise auto-detect
        string optimalQuality;
        if (!string.IsNullOrEmpty(preferredQuality))
        {
            optimalQuality = preferredQuality;
            Console.WriteLine($"[TorrentSelectionService] Using user-selected quality: {optimalQuality}");
        }
        else
        {
            // Get user's estimated internet speed
            double estimatedSpeedMbps = await EstimateInternetSpeed();
            Console.WriteLine($"[TorrentSelectionService] Estimated speed: {estimatedSpeedMbps:F2} Mbps");
            optimalQuality = DetermineOptimalQuality(estimatedSpeedMbps);
            Console.WriteLine($"[TorrentSelectionService] Auto-detected optimal quality: {optimalQuality}");
        }
        
        // Score each torrent
        var scoredTorrents = new List<TorrentScore>();
        foreach (var torrent in torrents)
        {
            double score = CalculateTorrentScore(torrent, optimalQuality, availableSpaceGB);
            scoredTorrents.Add(new TorrentScore
            {
                Torrent = torrent,
                Score = score,
                Reason = GetScoreReason(torrent, optimalQuality)
            });
        }
        
        // Sort by score descending
        var bestTorrent = scoredTorrents.OrderByDescending(s => s.Score).First();
        
        Console.WriteLine($"[TorrentSelectionService] Best torrent: {bestTorrent.Torrent.Title}");
        Console.WriteLine($"[TorrentSelectionService] Score: {bestTorrent.Score:F2}");
        Console.WriteLine($"[TorrentSelectionService] Reason: {bestTorrent.Reason}");
        
        return bestTorrent.Torrent;
    }
    
    private double CalculateTorrentScore(Item torrent, string optimalQuality, long availableSpaceGB)
    {
        // Seed score (normalize to 0-100)
        double seedScore = Math.Min(torrent.Seeders / 100.0 * 100, 100);
        
        // Quality score (check title for quality info)
        double qualityScore = GetQualityScore(torrent.Title, optimalQuality);
        
        // Size score (penalize if too large for available space)
        double sizeScore = GetSizeScore(torrent, availableSpaceGB);
        
        // Source priority (YTS gets bonus)
        double sourceScore = (torrent.Title != null && torrent.Title.ToLower().Contains("yts")) ? 100 : 50;
        
        // Calculate weighted score
        double totalScore = 
            (seedScore * WEIGHT_SEEDS) +
            (qualityScore * WEIGHT_QUALITY) +
            (sizeScore * WEIGHT_SIZE) +
            (sourceScore * WEIGHT_SOURCE);
        
        return totalScore;
    }
    
    private double GetQualityScore(string quality, string optimalQuality)
    {
        if (string.IsNullOrEmpty(quality))
            return 50; // Default score
        
        quality = quality.ToLower();
        
        // Parse quality
        int qualityValue = 0;
        if (quality.Contains("2160") || quality.Contains("4k"))
            qualityValue = QUALITY_SCORE_2160P;
        else if (quality.Contains("1080"))
            qualityValue = QUALITY_SCORE_1080P;
        else if (quality.Contains("720"))
            qualityValue = QUALITY_SCORE_720P;
        else
            qualityValue = 40; // Lower quality
        
        // Bonus if matches optimal
        if (quality.Contains(optimalQuality.ToLower()))
            qualityValue += 20;
        
        return Math.Min(qualityValue, 100);
    }
    
    private double GetSizeScore(Item torrent, long availableSpaceGB)
    {
        try
        {
            // Get size in GB from torrent.Size (long, in bytes)
            double sizeGB = torrent.Size / (1024.0 * 1024.0 * 1024.0);
            
            // Score based on size relative to available space
            if (sizeGB > availableSpaceGB * 0.8)
                return 20; // Too large, low score
            else if (sizeGB > availableSpaceGB * 0.5)
                return 60; // Acceptable
            else
                return 100; // Good size
        }
        catch
        {
            return 70; // Default if can't parse
        }
    }
    
    private string GetScoreReason(Item torrent, string optimalQuality)
    {
        var reasons = new List<string>();
        
        if (torrent.Title != null && torrent.Title.ToLower().Contains("yts"))
            reasons.Add("YTS source");
        
        if (torrent.Title != null && torrent.Title.Contains(optimalQuality))
            reasons.Add($"Optimal quality ({optimalQuality})");
        
        if (torrent.Seeders > 50)
            reasons.Add($"High seeds ({torrent.Seeders})");
        
        return string.Join(", ", reasons);
    }
    
    private string DetermineOptimalQuality(double speedMbps)
    {
        if (speedMbps >= SPEED_THRESHOLD_1080P)
            return "2160p"; // 4K
        else if (speedMbps >= SPEED_THRESHOLD_720P)
            return "1080p";
        else
            return "720p";
    }
    
    private async Task<double> EstimateInternetSpeed()
    {
        try
        {
            // TODO: Implement actual speed measurement from recent downloads
            // For now, return a conservative estimate
            
            // Default fallback: 10 Mbps (conservative estimate)
            return await Task.FromResult(10.0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TorrentSelectionService] Error estimating speed: {ex.Message}");
            return 10.0;
        }
    }
    
    private long GetAvailableDiskSpace()
    {
        try
        {
            string downloadPath = AppSettingsManager.appSettings?.TorrentsPath ?? 
                                  Path.GetTempPath();
            
            var drive = new DriveInfo(Path.GetPathRoot(downloadPath));
            long availableGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            
            return availableGB;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TorrentSelectionService] Error getting disk space: {ex.Message}");
            return 50; // Default 50 GB
        }
    }
}
