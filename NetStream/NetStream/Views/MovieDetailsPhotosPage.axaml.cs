using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Serilog;
using TMDbLib.Objects.General;
using Avalonia.Media;

namespace NetStream.Views;

public partial class MovieDetailsPhotosPage : UserControl
{
    public MovieDetailsPhotosPage()
    {
        InitializeComponent();
    }
    
    private Movie selectedMovie;
    public MovieDetailsPhotosPage(Movie selectedMovie)
    {
        InitializeComponent();
        this.selectedMovie = selectedMovie;
        this.DataContext = this;
        Service.PhotoDetailsBackdrop.Clear();
        Service.PhotoDetailsPoster.Clear();
        PhotosDisplay.ItemsSource = Service.PhotoDetailsBackdrop;
        PostersDisplay.ItemsSource = Service.PhotoDetailsPoster;
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        MainView.Instance.SizeChanged += InstanceOnSizeChanged;
        await GetMoviePhotos(selectedMovie,this);
        ApplyResponsiveLayout(MainView.Instance.Bounds.Width);
    }

    private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
    {
        var width = MainView.Instance.Bounds.Width;
        ApplyResponsiveLayout(width);
    }

    public async Task GetMoviePhotos(Movie selectedMovie, MovieDetailsPhotosPage movieDetailsPhotosPage)
 {
     try
     {
         if (!Service.client.HasConfig)
         {
             await Service.client.GetConfigAsync();
         }

         ImagesWithId MovieImages = null;

         // Verileri asenkron yükle
         if (selectedMovie.ShowType == ShowType.Movie)
         {
             MovieImages = await Service.client.GetMovieImagesAsync(selectedMovie.Id);
         }
         else if (selectedMovie.ShowType == ShowType.TvShow)
         {
             MovieImages = await Service.client.GetTvShowImagesAsync(selectedMovie.Id);
         }

         // UI güncellemelerini Dispatcher ile yap
         await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
         {
                 movieDetailsPhotosPage.BackDropsImageCounter.Text = $"{MovieImages.Backdrops.Count} " + ResourceProvider.GetString("ImagesString");
                 movieDetailsPhotosPage.PosterImageCounter.Text = $"{MovieImages.Posters.Count} " +  ResourceProvider.GetString("ImagesString");
             });

         // Arka planda verileri yükle
         var backdropTask = LoadImagesAsync(MovieImages.Backdrops, Service.PhotoDetailsBackdrop);
         var posterTask = LoadImagesAsync(MovieImages.Posters, Service.PhotoDetailsPoster);

         // Hem Backdrops hem de Posters için işlemleri bekle
         await Task.WhenAll(backdropTask, posterTask);
     }
     catch (Exception e)
     {
         var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
         Log.Error(errorMessage);
     }
 }
 private static async Task LoadImagesAsync(List<ImageData> images, ObservableCollection<PhotoDetail> collection)
 {
     try
     {
         foreach (var image in images)
         {
             var mov = new PhotoDetail();
             var url = Service.client.GetImageUrl("w500", image.FilePath);
             mov.Poster = url.AbsoluteUri;

             // Koleksiyona eklerken UI'yi güncelle
             await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
             {
                     collection.Add(mov);  // Koleksiyona ekle
                 });

             // Burada DelayNotifications() kullanabilirsiniz
         }
     }
     catch (Exception e)
     {
         var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
         Log.Error(errorMessage);
     }
 }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Service.PhotoDetailsBackdrop.Clear();
        Service.PhotoDetailsPoster.Clear();
        MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
    }

    private async void PhotosDisplay_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            var photoDetail = PhotosDisplay.SelectedItem as PhotoDetail;
            if (photoDetail != null)
            {
                await ImageViewerOverlay.Instance.ShowImage(photoDetail.Poster);
                PhotosDisplay.SelectedItem = null;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in PhotosDisplay_OnSelectionChanged: {ex.Message}");
        }
    }

    private void PhotosDisplay_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        
    }

    private async void PostersDisplay_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            var photoDetail = PostersDisplay.SelectedItem as PhotoDetail;
            if (photoDetail != null)
            {
                await ImageViewerOverlay.Instance.ShowImage(photoDetail.Poster);
                PostersDisplay.SelectedItem = null;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in PostersDisplay_OnSelectionChanged: {ex.Message}");
        }
    }

    void ApplyResponsiveLayout(double width)
    {
        // Başlık ve sayaç metin boyutlarını ayarla
        TextBlockBackdrop.FontSize = CalculateTextSize(width, 18, 32);
        BackDropsImageCounter.FontSize = CalculateTextSize(width, 14, 24);
        TextBlockPosters.FontSize = CalculateTextSize(width, 18, 32);
        PosterImageCounter.FontSize = CalculateTextSize(width, 14, 24);
        
        // Sayaçların margin değerlerini ayarla
        BackDropsImageCounter.Margin = CalculateCounterMargin(width);
        PosterImageCounter.Margin = CalculateCounterMargin(width);
        
        // Fontweight ve renk ayarları
        AdjustFontWeightAndColor(width);

        // Border boyutları için var olan kod
        foreach (var item in PhotosDisplay.GetVisualDescendants().OfType<Border>().Where(b => b.Name == "BorderBackdrop"))
        {
            var heightBorder = CalculateBackdropHeight(width);
            var widthBorder = heightBorder*1.77;

            item.Width = widthBorder;
            item.Height = heightBorder;
        }
        
        foreach (var item in PostersDisplay.GetVisualDescendants().OfType<Border>().Where(b => b.Name == "BorderPoster"))
        {
            var heightBorder = CalculatePosterHeight(width);
            var widthBorder = heightBorder/1.5;

            item.Width = widthBorder;
            item.Height = heightBorder;
        }
    }
    
    private double CalculateTextSize(double width, double minSize, double maxSize)
    {
        // Responsive tasarım için daha hassas font boyutu hesaplaması
        double baseValue = CalculateScaledValue(width, minSize, maxSize);
        
        // Çok küçük ekranlarda minimum değer koruması
        if (width < 600) 
        {
            return Math.Max(minSize, baseValue * 0.9);
        }
        // Çok büyük ekranlarda maximum değer koruması
        else if (width > 2500)
        {
            return Math.Min(maxSize, baseValue * 1.05);
        }
        
        return baseValue;
    }

    private void PostersDisplay_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        
    }

    private void Control_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
       
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
        
    private double CalculateBackdropHeight(double width)
    {
        return CalculateScaledValue(width, 150, 350);
    }
    
    private double CalculatePosterHeight(double width)
    {
        return CalculateScaledValue(width, 210, 700);
    }

    // Sayaçlar için margin hesaplama metodu
    private Thickness CalculateCounterMargin(double width)
    {
        double left = CalculateScaledValue(width, 6, 16);
        double top = CalculateScaledValue(width, 4, 9);
        
        return new Thickness(left, top, 0, 0);
    }

    // Ekran boyutuna göre font ağırlığını ve renk değerlerini ayarla
    private void AdjustFontWeightAndColor(double width)
    {
        // Ekran boyutuna göre font ağırlığı ayarla
        var titleFontWeight = width < 900 ? FontWeight.Normal : FontWeight.SemiBold;
        var counterFontWeight = width < 800 ? FontWeight.Light : FontWeight.Normal;
        
        TextBlockBackdrop.FontWeight = titleFontWeight;
        TextBlockPosters.FontWeight = titleFontWeight;
        BackDropsImageCounter.FontWeight = counterFontWeight;
        PosterImageCounter.FontWeight = counterFontWeight;
        
        // Ekran boyutuna göre renk yoğunluğunu ayarlama
        string counterColor = width < 700 ? "#999999" : "#888888";
        BackDropsImageCounter.Foreground = new SolidColorBrush(Color.Parse(counterColor));
        PosterImageCounter.Foreground = new SolidColorBrush(Color.Parse(counterColor));
    }
}