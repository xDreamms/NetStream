using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetStream.Views;
using Serilog;
using TMDbLib.Objects.People;

namespace NetStream
{
    public partial class CastPhotosPageControl : UserControl,IDisposable
    {
        private Cast cast;
        private double currentWidth;
        
        public CastPhotosPageControl()
        {
            InitializeComponent();
        }
        
        public CastPhotosPageControl(Cast cast)
        {
            InitializeComponent();
            this.cast = cast;
            Load();
        }
        
        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.width);
        }

        private async void PhotoItem_OnTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (sender is Grid grid && grid.DataContext is CastImage castImage)
                {
                    await ImageViewerOverlay.Instance.ShowImage(castImage.Poster);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in PhotoItem_OnTapped: {ex.Message}");
            }
        }

        private ObservableCollection<CastImage> castImages = new ObservableCollection<CastImage>();

        private async Task GetCastPhotos()
        {
            try
            {
                var language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;
                var person = await Service.client.GetPersonAsync(cast.Id, language, PersonMethods.Images);
                if (person != null)
                {
                    var photos = person.Images;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ImageCounter.Text = photos.Profiles.Count + " " + Application.Current.Resources["ImagesString"];
                    });
                    
                    foreach (var image in photos.Profiles)
                    {
                        castImages.Add(new CastImage()
                        {
                            Poster = (Service.client.GetImageUrl("w500",image.FilePath).AbsoluteUri)
                        });
                    }

                    ProfileImagesDisplay.ItemsSource = castImages;
                    
                    // Responsive düzeni yükleme işlemi bittikten sonra uygula
                    await Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        ApplyResponsiveLayout(currentWidth);
                    });
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        private void CastPhotosPage_OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            try
            {
                currentWidth = e.NewSize.Width;
                ApplyResponsiveLayout(currentWidth);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                // Başlık font boyutlarını ayarla
                double titleFontSize = CalculateScaledValue(width, 16, 24, 320, 3810);
                double counterFontSize = CalculateScaledValue(width, 12, 16, 320, 3810);
                
                // Başlık ve sayaç boyutlarını güncelle
                if (PhotosTitle != null)
                    PhotosTitle.FontSize = titleFontSize;
                
                if (ImageCounter != null)
                    ImageCounter.FontSize = counterFontSize;
                
                // Fotoğraf kartlarını boyutlandır
                foreach (var item in ProfileImagesDisplay.GetVisualDescendants())
                {
                    if (item is Border border && border.Name == "PhotoBorder")
                    {
                        double cardWidth, cardHeight;
                        
                        if (width < 600)
                        {
                            cardWidth = CalculateScaledValue(width, 150, 200, 320, 3810);
                            cardHeight = cardWidth * 1.5; // Oran koru
                        }
                        else if (width < 900)
                        {
                            cardWidth = CalculateScaledValue(width, 180, 220, 320, 3810);
                            cardHeight = cardWidth * 1.5;
                        }
                        else
                        {
                            cardWidth = CalculateScaledValue(width, 220, 250, 320, 3810);
                            cardHeight = cardWidth * 1.5;
                        }
                        
                        border.Width = cardWidth;
                        border.Height = cardHeight;
                    }
                    else if (item is Grid grid && grid.Name == "PhotoGridItem")
                    {
                        // Küçük ekranlarda daha az margin
                        if (width < 600)
                            grid.Margin = new Thickness(5, 5, 5, 5);
                        else
                            grid.Margin = new Thickness(10, 10, 0, 0);
                    }
                }
                
                // WrapPanel düzenini ayarla
                var wrapPanel = this.FindDescendantOfType<WrapPanel>();
                if (wrapPanel != null)
                {
                    if (width < 600)
                    {
                        wrapPanel.Orientation = Orientation.Vertical;
                        wrapPanel.HorizontalAlignment = HorizontalAlignment.Center;
                    }
                    else
                    {
                        wrapPanel.Orientation = Orientation.Horizontal;
                        wrapPanel.HorizontalAlignment = HorizontalAlignment.Center;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        private double CalculateScaledValue(double width, double minValue, double maxValue, double minWidth, double maxWidth)
        {
            // Ensure width is in range
            width = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Calculate scaling factor (0 to 1)
            double scaleFactor = (width - minWidth) / (maxWidth - minWidth);
            
            // Scale the value
            return minValue + (scaleFactor * (maxValue - minValue));
        }
        

        private void ProfileImagesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Responsive düzeni güncelle
            ApplyResponsiveLayout(currentWidth);
        }

        private DispatcherTimer timer;
        private async void CastPhotosPage_OnLoaded(object sender, RoutedEventArgs e)
        {
           
        }

        private async void Load()
        {
            await GetCastPhotos();
            
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
            
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            
            timer.Tick += TimerOnTick;
            
            timer.Start();
        }

        private void TimerOnTick(object? sender, EventArgs e)
        {
            try
            {
                currentWidth = MainView.Instance.Bounds.Width;
                ApplyResponsiveLayout(currentWidth);
                
                timer.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Timer içinde responsive layout uygulama hatası: {ex.Message}");
            }
        }

        private void CastPhotosPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Tick -= TimerOnTick;
            timer = null;
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
            
            if (castImages != null)
                castImages.Clear();

            if (ProfileImagesDisplay != null)
                ProfileImagesDisplay.ItemsSource = null;
        }
    }
} 