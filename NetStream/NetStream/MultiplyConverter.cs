using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace NetStream
{
    public class MultiplyConverter : IMultiValueConverter
    {
        public static readonly MultiplyConverter Instance = new MultiplyConverter();
        
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Count < 2 || values.Any(v => v == null))
                return 0.0;
                
            double result = 1.0;
            
            foreach (var value in values)
            {
                if (value is double d)
                {
                    result *= d;
                }
                else if (value is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
                {
                    result *= parsed;
                }
                else if (value is int i)
                {
                    result *= i;
                }
            }
            
            return result;
        }
    }
} 