using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace NetStream
{
    public class WidthToFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 16.0; // Default font size
                
            double width = (double)value;
            
            // Check if parameter is provided for different element types
            if (parameter != null)
            {
                string paramType = parameter.ToString();
                
                switch (paramType)
                {
                    case "overview":
                        // Overview text scales smaller
                        if (width <= 450) return 12.0;
                        if (width <= 750) return 14.0;
                        return 16.0;
                        
                    case "button":
                        // Button text scales conservatively
                        if (width <= 450) return 12.0;
                        if (width <= 750) return 14.0;
                        return 16.0;
                        
                    case "starSize":
                        // Rating stars size
                        if (width <= 450) return 18.0;
                        if (width <= 750) return 22.0;
                        return 25.0;
                        
                    case "bannerHeight":
                        // Banner height scaling
                        if (width <= 450) return 300.0;
                        if (width <= 750) return 450.0;
                        if (width <= 1200) return 550.0;
                        return 659.0;
                }
            }
            
            // Default main title font size
            if (width <= 450) return 24;      // Extra small screen
            if (width <= 750) return 30;      // Small screen
            if (width <= 1200) return 40;     // Medium screen
            return 50;                        // Large screen
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
