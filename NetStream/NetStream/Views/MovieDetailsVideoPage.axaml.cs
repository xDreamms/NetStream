using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LibVLCSharp.Shared;
using Material.Icons.Avalonia;
using NetStream.Navigation;
using Serilog;

namespace NetStream.Views
{
    public partial class MovieDetailsVideoPage : UserControl
    {
        private Movie selectedMovie;
        private MovieDetailsPage movieDetailsPage;
        
        // Responsive design için kullanılacak değişkenler
        private double previousWidth = 0;
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
        private bool isSmallScreen = false;
        private bool isExtraSmallScreen = false;

        public MovieDetailsVideoPage()
        {
            InitializeComponent();
        }

        public MovieDetailsVideoPage(Movie selectedMovie, MovieDetailsPage movieDetailsPage)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
            this.DataContext = this;
            this.movieDetailsPage = movieDetailsPage;
        }
        
        
        private async void MovieDetailsVideoPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                MainView.Instance.SizeChanged += InstanceOnSizeChanged;
                await Service.GetVideos(selectedMovie,this);
                
                // İlk yükleme sırasında boyuta göre ölçeklendirme yap
                ApplyResponsiveLayout(this.Bounds.Width);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            try
            {
                // Mevcut genişliği al
                double width = MainView.Instance.Bounds.Width;
                
                if (width != previousWidth)
                {
                    previousWidth = width;
                    
                    // Ekran boyutu kategorisini belirle
                    isSmallScreen = width <= SMALL_SCREEN_THRESHOLD;
                    isExtraSmallScreen = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                    
                    // Ekran boyutuna göre düzeni uygula
                    ApplyResponsiveLayout(width);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in MovieDetailsVideoPage_SizeChanged: {ex.Message}");
            }
        }

        public ObservableCollection<VideoDetail> VideoDetails
        {
            get
            {
                return Service.VideoDetails;
            }
        }

        private async void VideosDisplay_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                var videoDetail = VideosDisplay.SelectedItem as VideoDetail;
                if (videoDetail != null)
                {
                    // Seçimi hemen temizle
                    VideosDisplay.SelectedItem = null;

                    // Mevcut details sayfasındaki trailer'ı durdur ve temizle
                    await movieDetailsPage.CleanupVideoPlayer();

                    // TrailerPlayPage'de aç (MainWindow seviyesinde, tam ekran player)
                    var trailerPage = new TrailerPlayPage(videoDetail);
                    MainWindow.Instance.SetContent(trailerPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
                Console.WriteLine(errorMessage);
            }
        }

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            Service.VideoDetails.Clear();
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
        
      
        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                // Ekran genişliğini maksimum 3840px ile sınırla
                double clampedWidth = Math.Min(width, 3840);
                
                // Video öğelerinin boyutlarını dinamik olarak ayarla
                UpdateVideoItemSizes(clampedWidth);
                
                // Video başlıklarının font boyutunu ayarla
                UpdateVideoTextStyles(clampedWidth);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ApplyResponsiveLayout: {ex.Message}");
            }
        }

        private double itemwidth = 0;
        private void UpdateVideoItemSizes(double width)
        {
            try
            {
                if (VideosDisplay == null) return;
                
                // ListBox içindeki tüm öğeleri dolaş
                foreach (var item in VideosDisplay.GetVisualDescendants().OfType<Border>().Where(b => b.Name == "VideoBorder"))
                {
                    var heightBorder = CalculateCardHeight(width);
                    var widthBorder = heightBorder*1.33;

                    itemwidth = widthBorder;
                    item.Width = widthBorder;
                    item.Height = heightBorder;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in UpdateVideoItemSizes: {ex.Message}");
            }
        }
        private double CalculateScaledValue(double width, double minValue, double maxValue)
        {
            const double minWidth = 320;   // En küçük ekran genişliği (piksel)
            const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
            
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);
            
            return Math.Round(scaledValue);
        }
        
        private double CalculateCardHeight(double width)
        {
            return CalculateScaledValue(width, 210, 540);
        }
        private void UpdateVideoTextStyles(double width)
        {
            try
            {
                if (VideosDisplay == null) return;
                
                // Video başlıklarını bul
                foreach (var title in VideosDisplay.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Name == "VideoTitle"))
                {
                    // Başlık font boyutunu ölçeklendir
                    title.FontSize = CalculateScaledValue(width, 14, 24);
                    title.MaxWidth = itemwidth;
                }
                
                // Video türü metinlerini bul
                foreach (var typeText in VideosDisplay.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Name == "VideoTypeText"))
                {
                    typeText.FontSize = CalculateScaledValue(width, 12, 18);
                    typeText.MaxWidth = itemwidth;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in UpdateVideoTextStyles: {ex.Message}");
            }
        }
        
     
    }
    
  
} 