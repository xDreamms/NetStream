using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETCore.Encrypt;
using Newtonsoft.Json;

namespace NetStream
{
    public class AppSettingsManager
    {
        public static AppSettings appSettings;
        public static string AppSettingsPath { get; set; }


        public static void GetAppSettings()
        {
            var encrypted = File.ReadAllText(AppSettingsPath);
            if (!String.IsNullOrWhiteSpace(encrypted))
            {
                var contents = EncryptProvider.AESDecrypt(encrypted, Encryptor.Key, Encryptor.IV);
                appSettings = JsonConvert.DeserializeObject<AppSettings>(contents);
            }
        }

        public static void SaveAppSettings()
        {
            File.WriteAllText(AppSettingsPath, EncryptProvider.AESEncrypt(JsonConvert.SerializeObject(appSettings), Encryptor.Key, Encryptor.IV));
            
        }
    }
}
