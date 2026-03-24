using System;
using System.Threading.Tasks;
using TorrentWrapper;

namespace NetStream;

public enum DiskIOMode
{
    EnableOsCache = 0,
    DisableOsCache = 2,
    WriteThrough = 3,
}

public enum EncryptionLevel
{
    PePlaintext = 1,
    PeRc4 = 2,
    PeBoth = 3
}

public enum EncryptionPolicy
{
    PeForced = 0,
    PeEnabled = 1,
    PeDisabled = 2
}

public class SessionSettings2
{
    // Send Socket Buffer Size
    public static Task<int> GetSendSocketBufferSize()
    {
        try
        {
            int value = SessionSettings.send_socket_buffer_size;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetSendSocketBufferSize] Exception: {ex.Message}");
            return Task.FromResult(1 * 1024 * 1024); // Default: 1MB
        }
    }

    public static Task<bool> SetSendSocketBufferSize(int value)
    {
        try
        {
            SessionSettings.send_socket_buffer_size = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetSendSocketBufferSize] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Receive Socket Buffer Size
    public static Task<int> GetRecvSocketBufferSize()
    {
        try
        {
            int value = SessionSettings.recv_socket_buffer_size;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetRecvSocketBufferSize] Exception: {ex.Message}");
            return Task.FromResult(1 * 1024 * 1024); // Default: 1MB
        }
    }

    public static Task<bool> SetRecvSocketBufferSize(int value)
    {
        try
        {
            SessionSettings.recv_socket_buffer_size = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetRecvSocketBufferSize] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Connections Limit
    public static Task<int> GetConnectionsLimit()
    {
        try
        {
            int value = SessionSettings.ConnectionLimit;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetConnectionsLimit] Exception: {ex.Message}");
            return Task.FromResult(500); // Default: 500
        }
    }

    public static Task<bool> SetConnectionsLimit(int value)
    {
        try
        {
            SessionSettings.ConnectionLimit = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetConnectionsLimit] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Unchoke Slots Limit
    public static Task<int> GetUnchokeSlotsLimit()
    {
        try
        {
            int value = SessionSettings.UnchokeSlotsLimit;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetUnchokeSlotsLimit] Exception: {ex.Message}");
            return Task.FromResult(-1); // Default: -1 (unlimited)
        }
    }

    public static Task<bool> SetUnchokeSlotsLimit(int value)
    {
        try
        {
            SessionSettings.UnchokeSlotsLimit = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetUnchokeSlotsLimit] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Max Peer List Size
    public static Task<int> GetMaxPeerListSize()
    {
        try
        {
            int value = SessionSettings.MaxPeerListSize;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetMaxPeerListSize] Exception: {ex.Message}");
            return Task.FromResult(2000); // Default: 2000
        }
    }

    public static Task<bool> SetMaxPeerListSize(int value)
    {
        try
        {
            SessionSettings.MaxPeerListSize = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetMaxPeerListSize] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Half Open Limit
    public static Task<int> GetHalfOpenLimit()
    {
        try
        {
            int value = SessionSettings.half_open_limit;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetHalfOpenLimit] Exception: {ex.Message}");
            return Task.FromResult(100); // Default: 100
        }
    }

    public static Task<bool> SetHalfOpenLimit(int value)
    {
        try
        {
            SessionSettings.half_open_limit = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetHalfOpenLimit] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Active Downloads
    public static Task<int> GetActiveDownloads()
    {
        try
        {
            int value = SessionSettings.active_downloads;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetActiveDownloads] Exception: {ex.Message}");
            return Task.FromResult(50); // Default: 50
        }
    }

    public static Task<bool> SetActiveDownloads(int value)
    {
        try
        {
            SessionSettings.active_downloads = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetActiveDownloads] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Cache Size
    public static Task<int> GetCacheSize()
    {
        try
        {
            int value = SessionSettings.CacheSize;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetCacheSize] Exception: {ex.Message}");
            return Task.FromResult(512); // Default: 512
        }
    }

    public static Task<bool> SetCacheSize(int value)
    {
        try
        {
            SessionSettings.CacheSize = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetCacheSize] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Read Cache Line Size
    public static Task<int> GetReadCacheLineSize()
    {
        try
        {
            int value = SessionSettings.read_cache_line_size;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetReadCacheLineSize] Exception: {ex.Message}");
            return Task.FromResult(32); // Default: 32
        }
    }

    public static Task<bool> SetReadCacheLineSize(int value)
    {
        try
        {
            SessionSettings.read_cache_line_size = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetReadCacheLineSize] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Prefer RC4
    public static Task<bool> GetPreferRc4()
    {
        try
        {
            bool value = SessionSettings.PreferRc4;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetPreferRc4] Exception: {ex.Message}");
            return Task.FromResult(true); // Default: true
        }
    }

    public static Task<bool> SetPreferRc4(bool value)
    {
        try
        {
            SessionSettings.PreferRc4 = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetPreferRc4] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Enable DHT
    public static Task<bool> GetEnableDht()
    {
        try
        {
            bool value = SessionSettings.EnableDht;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetEnableDht] Exception: {ex.Message}");
            return Task.FromResult(true); // Default: true
        }
    }

    public static Task<bool> SetEnableDht(bool value)
    {
        try
        {
            SessionSettings.EnableDht = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetEnableDht] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Enable LSD
    public static Task<bool> GetEnableLsd()
    {
        try
        {
            bool value = SessionSettings.enable_lsd;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetEnableLsd] Exception: {ex.Message}");
            return Task.FromResult(true); // Default: true
        }
    }

    public static Task<bool> SetEnableLsd(bool value)
    {
        try
        {
            SessionSettings.enable_lsd = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetEnableLsd] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Enable NATPMP
    public static Task<bool> GetEnableNatpmp()
    {
        try
        {
            bool value = SessionSettings.enable_natpmp;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetEnableNatpmp] Exception: {ex.Message}");
            return Task.FromResult(true); // Default: true
        }
    }

    public static Task<bool> SetEnableNatpmp(bool value)
    {
        try
        {
            SessionSettings.enable_natpmp = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetEnableNatpmp] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Enable UPNP
    public static Task<bool> GetEnableUpnp()
    {
        try
        {
            bool value = SessionSettings.enable_upnp;
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetEnableUpnp] Exception: {ex.Message}");
            return Task.FromResult(true); // Default: true
        }
    }

    public static Task<bool> SetEnableUpnp(bool value)
    {
        try
        {
            SessionSettings.enable_upnp = value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetEnableUpnp] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Disk IO Write Mode
    public static Task<DiskIOMode> GetDiskIoWriteMode()
    {
        try
        {
            int value = SessionSettings.disk_io_write_mode;
            return Task.FromResult((DiskIOMode)value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetDiskIoWriteMode] Exception: {ex.Message}");
            return Task.FromResult(DiskIOMode.DisableOsCache); // Default: DisableOsCache
        }
    }

    public static Task<bool> SetDiskIoWriteMode(DiskIOMode value)
    {
        try
        {
            SessionSettings.disk_io_write_mode = (int)value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetDiskIoWriteMode] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Disk IO Read Mode
    public static Task<DiskIOMode> GetDiskIoReadMode()
    {
        try
        {
            int value = SessionSettings.disk_io_read_mode;
            return Task.FromResult((DiskIOMode)value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetDiskIoReadMode] Exception: {ex.Message}");
            return Task.FromResult(DiskIOMode.DisableOsCache); // Default: DisableOsCache
        }
    }

    public static Task<bool> SetDiskIoReadMode(DiskIOMode value)
    {
        try
        {
            SessionSettings.disk_io_read_mode = (int)value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetDiskIoReadMode] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Allowed Encryption Level
    public static Task<EncryptionLevel> GetAllowedEncLevel()
    {
        try
        {
            int value = SessionSettings.allowed_enc_level;
            return Task.FromResult((EncryptionLevel)value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetAllowedEncLevel] Exception: {ex.Message}");
            return Task.FromResult(EncryptionLevel.PeBoth); // Default: PeBoth
        }
    }

    public static Task<bool> SetAllowedEncLevel(EncryptionLevel value)
    {
        try
        {
            SessionSettings.allowed_enc_level = (int)value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetAllowedEncLevel] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // Out Encryption Policy
    public static Task<EncryptionPolicy> GetOutEncPolicy()
    {
        try
        {
            int value = SessionSettings.out_enc_policy;
            return Task.FromResult((EncryptionPolicy)value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetOutEncPolicy] Exception: {ex.Message}");
            return Task.FromResult(EncryptionPolicy.PeEnabled); // Default: PeEnabled
        }
    }

    public static Task<bool> SetOutEncPolicy(EncryptionPolicy value)
    {
        try
        {
            SessionSettings.out_enc_policy = (int)value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetOutEncPolicy] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    // In Encryption Policy
    public static Task<EncryptionPolicy> GetInEncPolicy()
    {
        try
        {
            int value = SessionSettings.in_enc_policy;
            return Task.FromResult((EncryptionPolicy)value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetInEncPolicy] Exception: {ex.Message}");
            return Task.FromResult(EncryptionPolicy.PeEnabled); // Default: PeEnabled
        }
    }

    public static Task<bool> SetInEncPolicy(EncryptionPolicy value)
    {
        try
        {
            SessionSettings.in_enc_policy = (int)value;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetInEncPolicy] Exception: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}
