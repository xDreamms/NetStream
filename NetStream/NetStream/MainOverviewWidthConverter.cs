using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;

namespace NetStream
{
    class MainOverviewWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double width)
            {
                // Adapt width percentage based on screen size
                if (width <= 450)
                {
                    // Extra small screens - use almost the full width
                    return width * 0.9;
                }
                else if (width <= 750)
                {
                    // Small screens - use more width
                    return width * 0.8;
                }
                else if (width <= 1200)
                {
                    // Medium screens
                    return width * 0.7;
                }
                else
                {
                    // Large screens - use original 60% width
                    return width * 0.6;
                }
            }
            return 0; // Default value
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
