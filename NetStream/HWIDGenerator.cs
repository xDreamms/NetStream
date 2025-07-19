using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NetStream
{
    public class HWIDGenerator
    {
        public static string GetCpuId()
        {
            string cpuId = string.Empty;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                cpuId = obj["ProcessorId"].ToString();
                break;
            }

            return cpuId;
        }

        public static string GetDiskSerialNumber()
        {
            string serialNumber = string.Empty;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_DiskDrive");

            foreach (ManagementObject obj in searcher.Get())
            {
                serialNumber = obj["SerialNumber"].ToString();
                break;
            }

            return serialNumber;
        }

        public static string GetMacAddress()
        {
            string macAddress = string.Empty;
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    macAddress = networkInterface.GetPhysicalAddress().ToString();
                    break;
                }
            }

            return macAddress;
        }

        public static string GetMotherboardSerialNumber()
        {
            string serialNumber = string.Empty;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_BaseBoard");

            foreach (ManagementObject obj in searcher.Get())
            {
                serialNumber = obj["SerialNumber"].ToString();
                break;
            }

            return serialNumber;
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
