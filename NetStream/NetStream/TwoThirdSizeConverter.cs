using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NetStream
{
    public class TwoThirdSizeConverter : IValueConverter
    {
        public static readonly TwoThirdSizeConverter Instance = new TwoThirdSizeConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double size)
            {
                // 2/3 oranını hesapla
                return size * 0.9;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 