//using MonoTorrent.Client;
//using MonoTorrent;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;

//namespace NetStream
//{
//    public class MonoTorrentStream
//    {
//        public static List<string> Trackers = new List<string>();
//        public static List<(string,TorrentManager)> TorrentManagers = new List<(string, TorrentManager)>();
//        public static async Task<TorrentManager> GetManager(Item item)
//        {
//            if (String.IsNullOrWhiteSpace(item.Magnet) && !String.IsNullOrWhiteSpace(item.TorrentLocation))
//            {
//                if (String.IsNullOrWhiteSpace(item.TorrentLocation))
//                {
//                    return null;
//                }
//                else
//                {
//                    var torrent = await MonoTorrent.Torrent.LoadAsync(item.TorrentLocation);
//                    TorrentManager manager = await InitClientEngine().AddStreamingAsync(torrent, AppSettingsManager.appSettings.MoviesPath, GetTorrentSettings());
//                    foreach (var trackerUrl in Trackers)
//                    {
//                        var uri = new Uri(trackerUrl);
//                        await manager.TrackerManager.AddTrackerAsync(uri);
//                    }
//                    await manager.StartAsync();

//                    if (!manager.HasMetadata)
//                    {
//                        await manager.WaitForMetadataAsync();
//                    }
//                    TorrentManagers.Add((item.Hash,manager));
//                    return manager;
//                }
//            }
//            else
//            {
//                MagnetLink magnetLink = FromUri(new Uri(item.Magnet));
//                TorrentManager manager = await InitClientEngine().AddStreamingAsync(magnetLink, AppSettingsManager.appSettings.MoviesPath, new TorrentSettings());
//                foreach (var trackerUrl in Trackers)
//                {
//                    var uri = new Uri(trackerUrl);
//                    await manager.TrackerManager.AddTrackerAsync(uri);
//                }
//                await manager.StartAsync();

//                if (!manager.HasMetadata)
//                {
//                    await manager.WaitForMetadataAsync();
//                }
//                TorrentManagers.Add((item.Hash, manager));
//                return manager;
//            }
//        }
//        public static async Task<Stream> StartStreaming(TorrentManager manager,FileInfo fileInfo)
//        {
//            var file = manager.Files.FirstOrDefault(x => x.FullPath == fileInfo.FullName);
//            var stream = await manager.StreamProvider.CreateStreamAsync(file);
//            return stream;
//        }
//        //public static void GetTrackers()
//        //{
//        //    try
//        //    {
//        //        var text = AppSettingsManager.appSettings.Trackers;
//        //        var trackers = new List<string>();
//        //        var lines = text.Split("\n");
//        //        foreach (var line in lines)
//        //            if (string.IsNullOrWhiteSpace(line) == false)
//        //                trackers.Add(line.Trim());
//        //        Trackers = trackers;
//        //    }
//        //    catch
//        //    {
               
//        //    }
//        //}
//        public static MagnetLink FromUri(Uri uri)
//        {
//            try
//            {
//                InfoHashes? infoHashes = null;
//                string? name = null;
//                var announceUrls = new List<string>();
//                var webSeeds = new List<string>();
//                long? size = null;

//                //if (uri.Scheme != "magnet")
//                //    throw new FormatException("Magnet links must start with 'magnet:'.");

//                string[] parameters = uri.Query.Substring(1).Split('&');
//                for (int i = 0; i < parameters.Length; i++)
//                {
//                    string[] keyval = parameters[i].Split('=');
//                    if (keyval.Length != 2)
//                    {
//                        // Skip anything we don't understand. Urls could theoretically contain many
//                        // unknown parameters.
//                        continue;
//                    }
//                    switch (keyval[0].Substring(0, 2))
//                    {
//                        case "xt"://exact topic
//                            string val = keyval[1].Substring(9);
//                            switch (keyval[1].Substring(0, 9))
//                            {
//                                case "urn:sha1:"://base32 hash
//                                case "urn:btih:":
//                                    if (infoHashes?.V1 != null)
//                                        throw new FormatException("More than one v1 infohash in magnet link is not allowed.");

//                                    if (val.Length == 32)
//                                        infoHashes = new InfoHashes(InfoHash.FromBase32(val), infoHashes?.V2);
//                                    else if (val.Length == 40)
//                                        infoHashes = new InfoHashes(InfoHash.FromHex(val), infoHashes?.V2);
//                                    else
//                                        throw new FormatException("Infohash must be base32 or hex encoded.");
//                                    break;

//                                case "urn:btmh:":
//                                    if (infoHashes?.V2 != null)
//                                        throw new FormatException("More than one v2 multihash in magnet link is not allowed.");

//                                    // BEP52: Support v2 magnet links
//                                    infoHashes = new InfoHashes(infoHashes?.V1, InfoHash.FromMultiHash(val));
//                                    break;
//                            }
//                            break;
//                        case "tr"://address tracker
//                            announceUrls.Add(keyval[1].UrlDecodeUTF8());
//                            break;
//                        case "as"://Acceptable Source
//                            webSeeds.Add(keyval[1].UrlDecodeUTF8());
//                            break;
//                        case "dn"://display name
//                            name = keyval[1].UrlDecodeUTF8();
//                            break;
//                        case "xl"://exact length
//                            size = long.Parse(keyval[1]);
//                            break;
//                        //case "xs":// eXact Source - P2P link.
//                        //case "kt"://keyword topic
//                        //case "mt"://manifest topic
//                        // Unused
//                        //break;
//                        default:
//                            // Unknown/unsupported
//                            break;
//                    }
//                }

//                if (infoHashes == null)
//                    throw new FormatException("The magnet link did not contain a valid 'xt' parameter referencing the infohash");

//                return new MagnetLink(infoHashes, name, announceUrls, webSeeds, size);
//            }
//            catch (Exception e)
//            {
//                MessageBox.Show(e.Message);
//                return null;
//            }
//        }
//        private static ClientEngine InitClientEngine()
//        {
//            int DefaultPort = 55123;

//            var settingBuilder = new EngineSettingsBuilder
//            {
//                MaximumConnections = 5000,
//                MaximumOpenFiles = 500,
//                MaximumUploadRate = 0,
//                MaximumDownloadRate = 0,
//                MaximumDiskReadRate = 0,
//                MaximumDiskWriteRate = 0,
//                MaximumHalfOpenConnections = 12,
//                AllowPortForwarding = true,

//                AutoSaveLoadDhtCache = true,

//                AutoSaveLoadFastResume = true,

//                AutoSaveLoadMagnetLinkMetadata = true,

//                ListenEndPoints = new Dictionary<string, IPEndPoint> {
//                        { "ipv4", new IPEndPoint (IPAddress.Any, DefaultPort) },
//                        { "ipv6", new IPEndPoint (IPAddress.IPv6Any, DefaultPort) }
//                    },

//                DhtEndPoint = new IPEndPoint(IPAddress.Any, DefaultPort),
//            }.ToSettings();

//            return new ClientEngine(settingBuilder);
//        }
//        private static TorrentSettings GetTorrentSettings()
//        {
//            return new TorrentSettingsBuilder
//            {
//                MaximumConnections = 5000,
//                UploadSlots = 200,
//                MaximumUploadRate = 0,
//                MaximumDownloadRate = 0
//            }.ToSettings();
//        }
//    }
//}
