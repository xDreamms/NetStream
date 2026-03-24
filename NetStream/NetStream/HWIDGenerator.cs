/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Serilog;

namespace NetStream
{
    public class HWIDGenerator
    {
        public static string GetCpuId()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows için alternatif bilgi kaynağı
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "wmic",
                            Arguments = "cpu get ProcessorId",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length >= 2)
                    {
                        return lines[1].Trim();
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Linux için
                    if (File.Exists("/proc/cpuinfo"))
                    {
                        string[] cpuInfo = File.ReadAllLines("/proc/cpuinfo");
                        foreach (string line in cpuInfo)
                        {
                            if (line.StartsWith("processor", StringComparison.OrdinalIgnoreCase))
                            {
                                return line.Split(':')[1].Trim();
                            }
                        }
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // macOS için
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "sysctl",
                            Arguments = "-n machdep.cpu.brand_string",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output.Trim();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CPU ID alınırken hata oluştu");
            }

            return Environment.ProcessorCount.ToString();
        }

        public static string GetDiskSerialNumber()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "wmic",
                            Arguments = "diskdrive get SerialNumber",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length >= 2)
                    {
                        return lines[1].Trim();
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Linux için
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "lsblk",
                            Arguments = "--nodeps -o serial",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length >= 2)
                    {
                        return lines[1].Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Disk seri numarası alınırken hata oluştu");
            }

            // Alternatif olarak ana sürücüyü kullan
            try
            {
                DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
                return driveInfo.VolumeLabel + driveInfo.TotalSize.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetMacAddress()
        {
            string macAddress = string.Empty;
            try
            {
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface networkInterface in networkInterfaces)
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        macAddress = networkInterface.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(macAddress))
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MAC adresi alınırken hata oluştu");
            }

            return macAddress;
        }

        public static string GetMotherboardSerialNumber()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "wmic",
                            Arguments = "baseboard get SerialNumber",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length >= 2)
                    {
                        return lines[1].Trim();
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Linux için
                    if (File.Exists("/sys/class/dmi/id/board_serial"))
                    {
                        return File.ReadAllText("/sys/class/dmi/id/board_serial").Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Anakart seri numarası alınırken hata oluştu");
            }

            return Environment.MachineName;
        }

        public static string GetHWID()
        {
            string cpuId = GetCpuId();
            string diskSerial = GetDiskSerialNumber();
            string motherboardSerial = GetMotherboardSerialNumber();

            string rawHwid = cpuId + diskSerial + motherboardSerial;

            return GetHash(rawHwid);
        }

        private static string GetHash(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(bytes);

                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }
    }
}
*/