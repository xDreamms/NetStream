using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NetStream
{
    class TruncateMainMovieOverViewTextConverter : IValueConverter
    {
        public int MaxLength { get; set; } = 100; // Maksimum karakter sayısı

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as string;
            if (text == null) return string.Empty;

            // Karakter sayısını kontrol et, ancak kelimenin ortasında kesilmesin
            if (text.Length > MaxLength)
            {
                int lastSpaceIndex = text.LastIndexOf(' ', MaxLength);
                if (lastSpaceIndex > 0)
                {
                    return text.Substring(0, lastSpaceIndex) + "...";
                }
                else
                {
                    return text.Substring(0, MaxLength) + "...";
                }
            }

            return text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
