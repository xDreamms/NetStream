using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NetStream.Views;
using Serilog;

namespace NetStream
{
    public partial class SearchPageControl : UserControl, IDisposable
    {
        private SearchPageEmptyControl searchPageEmpty;
        private double previousWidth = 0;

        public static SearchPageControl Instance;
        
        public SearchPageControl()
        {
            InitializeComponent();
            Instance =  this;
            searchPageEmpty = new SearchPageEmptyControl();
            SearchPageNavigation.Content = searchPageEmpty;
            
        }

     
        
        private void AdjustSearchBoxSize(double width)
        {
            try
            {
                const double minWidth = 320;   // En küçük ekran genişliği
                const double maxWidth = 3840;  // En büyük ekran genişliği
                
                // Font boyutu ölçeklendirme
                const double minFontSize = 16; // En küçük font boyutu
                const double maxFontSize = 30; // En büyük font boyutu
                
                // İkon boyutları ölçeklendirme 
                const double minIconSize = 24; // En küçük ikon boyutu
                const double maxIconSize = 32; // En büyük ikon boyutu
                
                // Ekran genişliğini sınırla
                double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
                
                // Doğrusal ölçeklendirme ile boyutları hesapla
                double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
                double fontSize = minFontSize + scale * (maxFontSize - minFontSize);
                double iconSize = minIconSize + scale * (maxIconSize - minIconSize);
                
                // Değerleri yuvarla
                fontSize = Math.Round(fontSize);
                iconSize = Math.Round(iconSize);
                
                // Arama kutusunun font boyutunu güncelle
                if (SearchTextBox != null && Math.Abs(SearchTextBox.FontSize - fontSize) > 0.1)
                {
                    SearchTextBox.FontSize = fontSize;
                }
                
                // Arama ikonunun boyutunu güncelle
                var searchIcon = this.FindControl<Image>("SearchIcon");
                if (searchIcon != null)
                {
                    // Minimum bir değer koyarak çok küçülmemesini sağla
                    searchIcon.Width = iconSize;
                    searchIcon.Height = iconSize;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustSearchBoxSize: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (searchPageResults != null)
            {
                searchPageResults.Dispose();
                searchPageResults = null;
            }
            
        }

        private SearchPageResultsControl searchPageResults;
        private void NameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchPageResults != null)
            {
                searchPageResults.SearchKey = SearchTextBox.Text;
            }

            if (!String.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                if (searchPageResults == null)
                {
                    searchPageResults = new SearchPageResultsControl();
                    SearchPageNavigation.Content = searchPageResults;
                    searchPageResults.SearchKey = SearchTextBox.Text;
                }
            }
            else
            {
                searchPageResults = null;
                SearchPageNavigation.Content = searchPageEmpty;
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
                    
                    // Arama kutusunun font boyutunu ayarla
                    AdjustSearchBoxSize(width);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in SearchPageControl_SizeChanged: {ex.Message}");
            }
        }

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
    }
} 