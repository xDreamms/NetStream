using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetStream.Views;

namespace NetStream.Controls;

public partial class MainMovieTvShow : UserControl
{
    // FromMovieDetailsPage özelliği - MovieDetailsPage'den geliyorsa true olacak
    public static readonly StyledProperty<bool> FromMovieDetailsPageProperty =
        AvaloniaProperty.Register<MainMovieTvShow, bool>(nameof(FromMovieDetailsPage), defaultValue: false);

    public bool FromMovieDetailsPage
    {
        get => GetValue(FromMovieDetailsPageProperty);
        set => SetValue(FromMovieDetailsPageProperty, value);
    }
    
    public MainMovieTvShow()
    {
        InitializeComponent();
        
        if (Service.client.ActiveAccount == null || String.IsNullOrWhiteSpace(Service.client.SessionId))
        {
           
            FavoritesElipse.IsVisible = false;
            FavoritesIconBlock.IsVisible =false;
            WatchListElipse.IsVisible = false;
            WatchListIconBlock.IsVisible = false;
            BtnSetRating.IsVisible = false;
            RatingElipse.IsVisible = false;
        }
    }

    private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
    {
        ApplyResponsiveToMainMovie(e.width);
    }

    private void Animate()
        {
            // Opacity animasyonu (saydamlık)
            try
            {
                // Basitleştirilmiş animasyon
                MainMovieStackPanel.Opacity = 0;
                
                var opacityAnimation = new Avalonia.Animation.Animation
                {
                    Duration = TimeSpan.FromSeconds(0.3),
                    FillMode = Avalonia.Animation.FillMode.Forward // Animasyon bitince son durumda kal
                };
                
                // KeyFrame yerine direkt property'ler kullanılabilir
                var opacityFadeIn = new Avalonia.Animation.KeyFrame
                {
                    Cue = new Avalonia.Animation.Cue(0.0)
                };
                opacityFadeIn.Setters.Add(new Avalonia.Styling.Setter { Property = OpacityProperty, Value = 0.0 });
                
                var opacityFadeOut = new Avalonia.Animation.KeyFrame
                {
                    Cue = new Avalonia.Animation.Cue(1.0)
                };
                opacityFadeOut.Setters.Add(new Avalonia.Styling.Setter { Property = OpacityProperty, Value = 1.0 });
                
                opacityAnimation.Children.Add(opacityFadeIn);
                opacityAnimation.Children.Add(opacityFadeOut);
                
                // Animasyon tamamlandığında son durumunu manuel olarak ayarla
                var opacityTask = opacityAnimation.RunAsync(MainMovieStackPanel);
                opacityTask.ContinueWith(_ => 
                {
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        // Animasyon bittikten sonra opacity'i kesin olarak 1 yapıyoruz
                        MainMovieStackPanel.Opacity = 1;
                    });
                });
                
                // TranslateTransform kullanımı - manuel olarak ayarlama
                if (MainMovieStackPanel.RenderTransform is TranslateTransform translateTransform)
                {
                    translateTransform.Y = 50;
                    
                    // Avalonia'da dönüşüm doğrudan animasyonla hedef alınamaz
                    // Ana kontrol üzerinde RenderTransform.Y'yi animasyon hedefi olarak kullanıyoruz
                    var animation = new Avalonia.Animation.Animation
                    {
                        Duration = TimeSpan.FromSeconds(0.75),
                        Easing = new Avalonia.Animation.Easings.CubicEaseInOut(),
                        FillMode = Avalonia.Animation.FillMode.Forward
                    };
                    
                    var keyFrame1 = new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Avalonia.Animation.Cue(0.0)
                    };
                    keyFrame1.Setters.Add(new Avalonia.Styling.Setter
                    {
                        Property = TranslateTransform.YProperty,
                        Value = 50.0
                    });
                    
                    var keyFrame2 = new Avalonia.Animation.KeyFrame
                    {
                        Cue = new Avalonia.Animation.Cue(1.0)
                    };
                    keyFrame2.Setters.Add(new Avalonia.Styling.Setter
                    {
                        Property = TranslateTransform.YProperty,
                        Value = 0.0
                    });
                    
                    animation.Children.Add(keyFrame1);
                    animation.Children.Add(keyFrame2);
                    
                    // Animasyonu TranslateTransform'a uygula (TranslateTransform Animatable olduğu için)
                    animation.RunAsync(MainMovieStackPanel);
                    
                    // Animasyon sonunda kesin değeri ayarla
                    Task.Delay(750).ContinueWith(_ =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            translateTransform.Y = 0;
                        });
                    });
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void MainMovieTvShow_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                // Eğer zaten MovieDetailsPage içindeyse tıklamayı işleme, çünkü zaten o sayfadayız
                if (FromMovieDetailsPage)
                    return;
                    
                var main_movie = this.DataContext as MainMovie;

                if (main_movie != null)
                {
                    var movieDetailsPage = new MovieDetailsPage(main_movie.Id, main_movie.ShowType);
                    var mainView = this.FindAncestorOfType<MainView>();
                    if (mainView != null)
                    {
                        mainView.SetContent(movieDetailsPage);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }
        
          private void AdjustWrapPanelMovieDetailsSize(double width)
        {
            try
            {
                if (WrapPanelMovieDetails == null) return;
                
                // Ekran genişliği sınırları
                const double minWidth = 320;   // En küçük ekran genişliği (piksel)
                const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
                const double minHeightValue = 16; // En küçük yükseklik değeri
                const double maxHeightValue = 36; // En büyük yükseklik değeri
                
                // Ekran genişliğini sınırla
                double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
                
                // Doğrusal ölçeklendirme ile maxHeight değerini hesapla
                double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
                double dynamicHeightValue = minHeightValue + scale * (maxHeightValue - minHeightValue);
                
                // Değeri yuvarla
                dynamicHeightValue = Math.Round(dynamicHeightValue);
                
                // Döngüsel yeniden boyutlandırmaları önlemek için değişikliği takip et
                bool madeChanges = false;
                
                // WrapPanel içindeki tüm Viewbox'ları bul ve boyutlarını ayarla
                foreach (var child in WrapPanelMovieDetails.Children)
                {
                    if (child is Viewbox viewbox)
                    {
                        // Sadece değer değiştiyse güncelle
                        if (Math.Abs(viewbox.MaxHeight - dynamicHeightValue) > 0.1)
                        {
                            viewbox.MaxHeight = dynamicHeightValue;
                            madeChanges = true;
                        }
                    }
                    else if (child is Ellipse ellipse)
                    {
                        // Ellipse'lerin boyutunu oran olarak hesapla (yüksekliğin %20'si)
                        double ellipseSize = dynamicHeightValue * 0.2;
                        
                        // Sadece değer değiştiyse güncelle
                        if (Math.Abs(ellipse.Width - ellipseSize) > 0.1 || 
                            Math.Abs(ellipse.Height - ellipseSize) > 0.1)
                        {
                            ellipse.Width = ellipseSize;
                            ellipse.Height = ellipseSize;
                            madeChanges = true;
                        }
                    }
                }
                
                // RatingBar yıldız boyutunu güncelle (özel sınıf olduğu için ayrıca ele alıyoruz)
                var ratingControl = this.FindControl<RatingBar>("RatingControl");
                if (ratingControl != null)
                {
                    // Yıldız boyutunu ekran genişliğine göre hesapla
                    double minStarSize = 12;
                    double maxStarSize = 24;
                    double starSize = minStarSize + scale * (maxStarSize - minStarSize);
                    starSize = Math.Round(starSize);
                    
                    // Sadece değer değiştiyse güncelle
                    if (Math.Abs(ratingControl.StarSize - starSize) > 0.1)
                    {
                        ratingControl.StarSize = starSize;
                        madeChanges = true;
                    }
                }
                
                // Eğer hiçbir değişiklik yapılmadıysa, loglama yap
                if (!madeChanges)
                {
                    Console.WriteLine("AdjustWrapPanelMovieDetailsSize: No changes needed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AdjustWrapPanelMovieDetailsSize: {ex.Message}");
            }
        }
          
          void ApplyResponsiveToMainMovie(double clampedWidth)
        {
            double commonLeftMargin = isSmallScreen 
                ? 20  // Küçük ekranlar için sabit margin
                : CalculateScaledValue(MainView.Instance.screenWidth, 40, 80); // Büyük ekranlar için ölçeklendirilmiş margin
            if (PosterImage != null)
            {
                var posterGrid = PosterImage.Parent as Grid;
                if (posterGrid != null)
                {
                    if (clampedWidth <= 600)
                    {
                        posterGrid.Width = clampedWidth;
                        var parentGrid = posterGrid.Parent as Grid;
                        if (parentGrid != null && parentGrid.ColumnDefinitions.Count > 1)
                        {
                            // Mobil görünümde Grid.Column 0'a getir ve ColumnSpan 2 yap
                            Grid.SetColumn(posterGrid as Control, 0);
                            Grid.SetColumnSpan(posterGrid as Control, 2);
                        }
                    }
                    else
                    {
                        posterGrid.Width = double.NaN; // Auto

                        var parentGrid = posterGrid.Parent as Grid;
                        if (parentGrid != null && parentGrid.ColumnDefinitions.Count > 1)
                        {
                            Grid.SetColumn(posterGrid as Control, 1);
                            Grid.SetColumnSpan(posterGrid as Control, 1);
                        }
                    }

                    PosterImage.Stretch = global::Avalonia.Media.Stretch.UniformToFill;
                }

                var height = CalculateScaledValue(300, 1100);

                PosterImage.Height = height;
                this.MinHeight = height;
                
            }

            if (SmallScreenBackground != null)
            {
                SmallScreenBackground.IsVisible = clampedWidth <= 600;
                SmallScreenBackground.Opacity = 0.7;
            }

            if (MainMovieStackPanel != null)
            {
                MainMovieStackPanel.Margin = new Thickness(commonLeftMargin, 0, commonLeftMargin, 0);

                if (MainMovieTitle != null)
                {
                    MainMovieTitle.Margin = new Thickness(0, 0, 0, 0); // İç boşluk sıfır, dış panel zaten hizalıyor
                }

                if (OverviewTextBlock != null)
                {
                    OverviewTextBlock.Margin = new Thickness(0, 20, 0, 0); // Sadece üst margin bırak
                }
            }

            if (OverviewTextBlock != null)
            {
                double overviewFontSize = CalculateTextSize(clampedWidth, 12, 27);
                OverviewTextBlock.FontSize = overviewFontSize;

                // Ekran boyutuna göre maksimum genişliği ayarla
                double maxWidth = CalculateScaledValue(clampedWidth, clampedWidth - 40, 1200);
                OverviewTextBlock.MaxWidth = clampedWidth <= SMALL_SCREEN_THRESHOLD ? clampedWidth - 40 : maxWidth;
            }

            // Configure main movie title
            if (MainMovieTitle != null)
            {
                // Doğrudan ekran genişliğine göre ölçeklendirme yapalım, converter'a güvenmek yerine
                var titleFontSize = CalculateFontSizeForTitle(clampedWidth);
                MainMovieTitle.FontSize = titleFontSize;
            }

            // Configure rating text - check if this element exists
            var mainMovieRatingText = MainMovieStackPanel?.GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(t => t.Text?.Contains("Rating") == true || t.Name?.Contains("Rating") == true);

            if (mainMovieRatingText != null)
            {
                double ratingTextSize = CalculateTextSize(clampedWidth, 12, 20);
                mainMovieRatingText.FontSize = ratingTextSize;
            }

            double viewboxMaxHeight = CalculateScaledValue(clampedWidth, 16, 40);
            double ellipseSize = viewboxMaxHeight * 0.2;
            if (WrapPanelMovieDetails != null)
            {
                foreach (var child in WrapPanelMovieDetails.Children)
                {
                    if (child is Viewbox viewbox)
                    {
                        viewbox.MaxHeight = viewboxMaxHeight;
                    }
                    else if (child is Ellipse ellipse)
                    {
                        ellipse.Width = ellipseSize;
                        ellipse.Height = ellipseSize;
                    }
                }
            }

            AdjustWrapPanelMovieDetailsSize(clampedWidth);
        }
          
        private bool isSmallScreen = false;
        private bool isExtraSmallScreen = false;

        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
        private double CalculateFontSizeForTitle(double width)
        {
            // Doğrusal ölçeklendirme formülü: y = mx + b
            // Ekran boyutu arttıkça font boyutu da artacak şekilde
            const double minWidth = 320;   // En küçük ekran genişliği (piksel)
            const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
            const double minFontSize = 18; // En küçük font boyutu
            const double maxFontSize = 90; // En büyük font boyutu
            
            // Ekran genişliğini sınırla
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Doğrusal ölçeklendirme formülü
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double fontSize = minFontSize + scale * (maxFontSize - minFontSize);
            
            // Font boyutunu yuvarla
            return Math.Round(fontSize);
        }
        private double CalculateScaledValue(double minValue, double maxValue)
        {
            // Ekran genişliği sınırları
            const double minWidth = 320;   // En küçük ekran genişliği (piksel)
            const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
            
            // Ekran genişliğini sınırla
            double clampedWidth = Math.Max(minWidth, Math.Min(MainView.Instance.screenWidth, maxWidth));
            
            // Doğrusal ölçeklendirme formülü
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);
            
            // Değeri yuvarla
            return Math.Round(scaledValue);
        }
        
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }
        
        private double CalculateScaledValue(double width,double minValue, double maxValue)
        {
            // Ekran genişliği sınırları
            const double minWidth = 320;   // En küçük ekran genişliği (piksel)
            const double maxWidth = 3840;  // En büyük ekran genişliği (piksel)
            
            // Ekran genişliğini sınırla
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Doğrusal ölçeklendirme formülü
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);
            
            // Değeri yuvarla
            return Math.Round(scaledValue);
        }

        private void Control_OnLoaded(object? sender, RoutedEventArgs e)
        {
            SetupMainMovie();
            Animate();
            SetVisibility();
            ApplyResponsiveToMainMovie(MainView.Instance.screenWidth);
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
        }
        
        private void SetupMainMovie()
        {
            if (Service.MainMovieee != null)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await Task.Delay(500);
                    
                    MainMovieStackPanel.Opacity = 1;
                    var translateTransform = (TranslateTransform)MainMovieStackPanel.RenderTransform;
                    translateTransform.Y = 0;
                });
            }
        }
        
        private void SetVisibility()
        {
            try
            {
                if (Service.MainMovieee != null)
                {
                    if (String.IsNullOrWhiteSpace(Service.MainMovieee.Overview))
                    {
                        OverviewTextBlock.IsVisible = false;
                    }
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Console.WriteLine(errorMessage);
            }
        }

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
        
        // MovieDetailsPage'e özgü olay yöneticileri
        
        // Rating butonları için olay yöneticileri
        private void BtnSetRating_OnMouseEnter(object? sender, PointerEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.BtnSetRating_OnMouseEnter(sender, e);
            }
        }
        
        private void BtnSetRating_OnMouseLeave(object? sender, PointerEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.BtnSetRating_OnMouseLeave(sender, e);
            }
        }
        
        private void BtnSetRating_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.BtnSetRating_OnPreviewMouseLeftButtonDown(sender, e);
            }
        }
        
        // Favori butonları için olay yöneticileri
        private void FavoritesIconBlock_OnMouseEnter(object? sender, PointerEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.FavoritesIconBlock_OnMouseEnter(sender, e);
            }
        }
        
        private void FavoritesIconBlock_OnMouseLeave(object? sender, PointerEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.FavoritesIconBlock_OnMouseLeave(sender, e);
            }
        }
        
        private void FavoritesIconBlock_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.FavoritesIconBlock_OnPreviewMouseLeftButtonDown(sender, e);
            }
        }
        
        // İzleme listesi butonları için olay yöneticileri
        private void WatchListIconBlock_OnMouseEnter(object? sender, PointerEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.WatchListIconBlock_OnMouseEnter(sender, e);
            }
        }
        
        private void WatchListIconBlock_OnMouseLeave(object? sender, PointerEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.WatchListIconBlock_OnMouseLeave(sender, e);
            }
        }
        
        private void WatchListIconBlock_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.WatchListIconBlock_OnPreviewMouseLeftButtonDown(sender, e);
            }
        }
        
        // Fragman izleme butonları için olay yöneticileri
        private void WatchTrailerButton_OnClick(object? sender, RoutedEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.WatchTrailerButton_OnClick(sender, e);
            }
        }
        
        private void WatchButton_OnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.WatchButton_OnPreviewMouseLeftButtonDown(sender, e);
            }
        }
        
        // Watch Now butonu için olay yöneticisi - Otomatik en iyi torrenti seç ve oynat
        private void WatchNowButton_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            // MovieDetailsPage sınıfındaki handler'ı çağır
            var parent = this.FindAncestorOfType<MovieDetailsPage>();
            if (parent != null)
            {
                parent.WatchNowButton_OnPreviewMouseLeftButtonDown(sender, routedEventArgs);
            }
        }
}