using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NetStream
{
    class TruncateMainMovieNameTextConverter : IValueConverter
    {
        public int MaxLength { get; set; } = 100; // Maksimum karakter sayısı

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as string;
            if (text == null) return string.Empty;

            // Karakter sayısını kontrol et, gerekirse kısalt
            if (text.Length > MaxLength)
            {
                return text.Substring(0, MaxLength) + "...";
            }

            return text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
