using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NetStream
{
    public class HeightPercentageConverter : IValueConverter
    {
        public static readonly HeightPercentageConverter Instance = new HeightPercentageConverter();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height && parameter is double percentage)
            {
                return height * percentage / 100;
            }
            
            if (value is double h && parameter is string percentStr && 
                double.TryParse(percentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double percent))
            {
                return h * percent / 100;
            }
            
            return value;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 