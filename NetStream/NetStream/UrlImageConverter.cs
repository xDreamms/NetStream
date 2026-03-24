using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Serilog;

namespace NetStream
{
    public class UrlImageConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is string))
                return null;
            
            string url = (string)value;
            
            try
            {
                // URL boş değilse işleme devam et
                if (!string.IsNullOrEmpty(url))
                {
                    // Asenkron olarak resmi yükleme işlemini başlat
                    // UI bloklamadan arka planda yüklenecek
                    Task.Run(async () => 
                    {
                        try 
                        {
                            var bitmap = await AsyncImageLoader.LoadFromWeb(url);
                            Log.Debug($"Resim yüklendi: {url}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Asenkron resim yüklenirken hata: {ex.Message} - URL: {url}");
                        }
                    });
                    
                    // İlk başta null döndürülebilir, resim yüklendiğinde PropertyChanged tetiklenecek
                    // ve UI otomatik olarak güncellenecek
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Resim yükleme converter hatası: {ex.Message} - URL: {url}");
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 