using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NetStream.Views;
using Serilog;

namespace NetStream
{
    public partial class SearchPageEmptyControl : UserControl
    {
        private double previousWidth = 0;
        
        public SearchPageEmptyControl()
        {
            InitializeComponent();
            
        }
        
   
        private void AdjustEmptyTextSize(double width)
        {
            try
            {
                var emptyTextBlock = this.FindControl<TextBlock>("EmptyMessageText");
                if (emptyTextBlock == null) return;
                
                const double minWidth = 320;   // En küçük ekran genişliği
                const double maxWidth = 3840;  // En büyük ekran genişliği
                
                // Daha küçük font değerleri (özellikle küçük ekranlar için)
                const double minFontSize = 14; // En küçük font boyutu
                const double maxFontSize = 32; // En büyük font boyutu
                
                // Ekran genişliğini sınırla
                double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
                
                // Doğrusal ölçeklendirme ile font boyutunu hesapla
                double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
                double fontSize = minFontSize + scale * (maxFontSize - minFontSize);
                
                // Değeri yuvarla
                fontSize = Math.Round(fontSize);
                
                // Boş sayfa mesajının font boyutunu güncelle
                if (Math.Abs(emptyTextBlock.FontSize - fontSize) > 0.1)
                {
                    emptyTextBlock.FontSize = fontSize;
                    
                    // Ekstra küçük ekranlarda kenar boşluklarını arttır
                    bool isSmallScreen = width <= 450;
                    emptyTextBlock.Margin = isSmallScreen 
                        ? new Thickness(30, 0, 30, 0) 
                        : new Thickness(10, 0, 10, 0);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustEmptyTextSize: {ex.Message}");
            }
        }

        private void Control_OnLoaded(object? sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            try
            {
                double width = e.width;
                
                if (width != previousWidth)
                {
                    previousWidth = width;
                    
                    // Boş sayfa mesajının boyutunu ayarla
                    AdjustEmptyTextSize(width);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in SearchPageEmptyControl_SizeChanged: {ex.Message}");
            }
        }

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
    }
} 