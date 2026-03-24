using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NetStream
{
    public class StringEqualsConverter : IValueConverter
    {
        // Singleton instance for static access
        public static readonly StringEqualsConverter Instance = new StringEqualsConverter();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If either value is null, return false
            if (value == null || parameter == null)
                return false;
                
            // Compare the strings
            return value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 