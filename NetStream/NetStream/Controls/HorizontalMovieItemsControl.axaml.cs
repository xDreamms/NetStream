using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using NetStream.Navigation;
using NetStream.Views;
using Projektanker.Icons.Avalonia;
using Serilog;

namespace NetStream.Controls;

public partial class HorizontalMovieItemsControl : UserControl
{
    public static readonly StyledProperty<ObservableCollection<Movie>> ItemsSourceProperty =
        AvaloniaProperty.Register<HorizontalMovieItemsControl, ObservableCollection<Movie>>(nameof(ItemsSource));
        
    public static readonly StyledProperty<bool> IsMovieProperty =
        AvaloniaProperty.Register<HorizontalMovieItemsControl, bool>(nameof(IsMovie), true);
        
    public static readonly StyledProperty<string> HeaderTextProperty =
        AvaloniaProperty.Register<HorizontalMovieItemsControl, string>(nameof(HeaderText), "");
        
    public static readonly StyledProperty<string> ExploreMoreTextProperty =
        AvaloniaProperty.Register<HorizontalMovieItemsControl, string>(nameof(ExploreMoreText), "");
    
    public static readonly StyledProperty<bool> IsSimilarMoviesProperty =
        AvaloniaProperty.Register<HorizontalMovieItemsControl, bool>(nameof(IsSimilarMovies), false);
    
    public bool IsSimilarMovies
    {
        get => GetValue(IsSimilarMoviesProperty);
        set => SetValue(IsSimilarMoviesProperty, value);
    }
        
    private ObservableCollection<Movie> _lastItemsSource;
    
    public ObservableCollection<Movie> ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set 
        {
            if (_lastItemsSource != value)
            {
                _lastItemsSource = value;
                SetValue(ItemsSourceProperty, value);
                
                // ItemsDisplay'i UI thread'de güncelle
                if (Dispatcher.UIThread.CheckAccess())
                {
                    UpdateItemsDisplay(value);
                }
                else
                {
                    Dispatcher.UIThread.InvokeAsync(() => UpdateItemsDisplay(value));
                }
            }
        }
    }
    
    private void UpdateItemsDisplay(ObservableCollection<Movie> items)
    {
        try
        {
            if (ItemsDisplay != null)
            {
                ItemsDisplay.ItemsSource = items;
                
                // ItemsSource değiştiğinde ApplyStyles metodunu çağır
                // ancak 50ms bekleyerek UI'ın önce ItemsSource değişikliğini işlemesini sağlayalım
                Task.Delay(50).ContinueWith(_ => 
                {
                    Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        if (this.IsLoaded)
                        {
                            ApplyStyles();
                        }
                    });
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error($"UpdateItemsDisplay error: {ex.Message}");
        }
    }
    
    public bool IsMovie
    {
        get => GetValue(IsMovieProperty);
        set => SetValue(IsMovieProperty, value);
    }
    
    public string HeaderText
    {
        get => GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }
    
    public string ExploreMoreText
    {
        get => GetValue(ExploreMoreTextProperty);
        set => SetValue(ExploreMoreTextProperty, value);
    }
    
    public event EventHandler<PointerPressedEventArgs> ExploreMorePressed;
    
    private double _cardWidth = 200;

    public HorizontalMovieItemsControl()
    {
        InitializeComponent();
        
        // Loaded event'i ekle - kontrol görünür hale geldiğinde stili uygula
        this.Loaded += (s, e) => 
        {
            Task.Delay(100).ContinueWith(_ => Dispatcher.UIThread.InvokeAsync(() => ApplyStyles()));
        };
        
        // Bağlamaları ayarla - performans için yeniden düzenle
        System.ObservableExtensions.Subscribe(this.GetObservable(ItemsSourceProperty), items => 
        {
            // Değişiklikler UI thread'de işlenmeli
            if (Dispatcher.UIThread.CheckAccess())
            {
                HandleItemsSourceChanged(items);
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() => HandleItemsSourceChanged(items));
            }
        });
            
        // Kaydırma butonlarını ayarla
        ScrollRightButton.Click += ScrollRightButton_OnClick;
        ScrollLeftButton.Click += ScrollLeftButton_OnClick;
        
        // Hover efektleri
        ScrollRightButton.PointerEntered += ScrollButton_OnPointerEntered;
        ScrollRightButton.PointerExited += ScrollButton_OnPointerExited;
        ScrollLeftButton.PointerEntered += ScrollButton_OnPointerEntered;
        ScrollLeftButton.PointerExited += ScrollButton_OnPointerExited;
    }
    
    private void HandleItemsSourceChanged(ObservableCollection<Movie> items)
    {
        try
        {
            if (ItemsDisplay != null)
            {
                // Önceden aboneliğimiz varsa, gerektiğinde event'i kaldır
                if (items != null)
                {
                    // ItemsSource'a doğrudan ata
                    ItemsDisplay.ItemsSource = items;
                    
                    // Eğer items koleksiyonu boş değilse ve kontrol yüklendiyse stil uygula
                    if (items.Count > 0 && this.IsLoaded)
                    {
                        // UI thread'in yüklenmesi için kısa bir gecikme ekleyelim
                        Task.Delay(50).ContinueWith(_ => Dispatcher.UIThread.InvokeAsync(() => ApplyStyles()));
                    }
                }
                else
                {
                    ItemsDisplay.ItemsSource = null;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"HandleItemsSourceChanged error: {ex.Message}");
        }
    }
    
    private void StackPanelMain_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        // Kullanıcı bir öğeye tıkladığında
        if (sender is StackPanel stackPanel && stackPanel.DataContext != null)
        {
            var movie = stackPanel.DataContext as Movie;
            if (movie != null)
            {
                NavigationService.Instance.Navigate(new MovieDetailsPage(movie));
            }
        }
    }
    
    private void ApplyStyles()
    {
        // UI thread'inde olduğumuzdan emin olalım
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.InvokeAsync(ApplyStyles);
            return;
        }
        
        // Ekran genişliğine göre kartları boyutlandır
        var width = this.Bounds.Width;
        
        if (width <= 0)
        {
            // Geçersiz genişlik, ölçekleme işlemini atlayalım
            return;
        }
        
        ApplyDirectStylesToItems(width);
        AdjustScrollButtonsPosition(width);
    }
    
    private void ApplyDirectStylesToItems(double width)
    {
        // ItemsSource boşsa gereksiz işlem yapmayalım
        if (ItemsSource == null || ItemsSource.Count == 0)
        {
            return;
        }
        
        // Ekran genişliğini maksimum 3840px ile sınırla
        double clampedWidth = Math.Min(width, 3840);
        
        // Kart boyutlarını hesapla
        _cardWidth = CalculateCardWidth(clampedWidth);
        double cardHeight = _cardWidth * 1.5;
        
        // Font boyutlarını hesapla
        double titleFontSize = CalculateTextSize(clampedWidth, 12, 24);
        double ratingFontSize = CalculateTextSize(clampedWidth, 12, 20);
        double starSize = CalculateTextSize(clampedWidth, 15, 30);
        
        try
        {
            // Görünür öğeleri optimize edin - tüm ağaç üzerinde traversal yapmak yerine sınırlandırın
            var visibleDescendants = ItemsDisplay.GetVisualDescendants()
                // En fazla 200 öğe veya gösterilen öğe sayısının 5 katı
                .ToList();
                
            // Öğeleri kategoriye göre grupla (daha az döngü için)
            var borders = visibleDescendants.OfType<Border>()
                .Where(b => b.Name == "MovieCardBorder" || b.Classes.Contains("movie-card"))
                .ToList();
                
            var titleBlocks = visibleDescendants.OfType<TextBlock>()
                .Where(t => t.Name == "MovieTitle")
                .ToList();
                
            var ratingBars = visibleDescendants.OfType<RatingBar>()
                .Where(r => r.Name == "MovieRating")
                .ToList();
                
            var ratingTexts = visibleDescendants.OfType<TextBlock>()
                .Where(t => t.Name == "RatingText")
                .ToList();
            
            // Topluca güncelleme yap
            foreach (var border in borders)
            {
                border.Width = _cardWidth;
                border.Height = cardHeight;
            }
            
            foreach (var textBlock in titleBlocks)
            {
                textBlock.FontSize = titleFontSize;
                textBlock.MaxWidth = _cardWidth ;
            }
            
            foreach (var ratingBar in ratingBars)
            {
                ratingBar.StarSize = starSize;
            }
            
            foreach (var ratingText in ratingTexts)
            {
                ratingText.FontSize = ratingFontSize;
                double leftMargin = _cardWidth * 0.06;
                ratingText.Margin = new Thickness(leftMargin, 3.5, 0, 0);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"HorizontalMovieItemsControl ApplyDirectStylesToItems error: {ex.Message}");
        }
    }
    
    private void AdjustScrollButtonsPosition(double width)
    {
        double clampedWidth = Math.Min(width, 3840);
        
        // Ekran boyutuna göre buton boyutlarını ölçekle
        double buttonSize = CalculateScaledValue(clampedWidth, 40, 60);
        double iconSize = CalculateScaledValue(clampedWidth, 20, 35);
        double buttonHeight = _cardWidth * 1.5; // Kart yüksekliğiyle aynı olsun
        
        // Butonları ayarla
        ScrollRightButton.Width = buttonSize;
        ScrollRightButton.Height = buttonHeight;
        ScrollLeftButton.Width = buttonSize;
        ScrollLeftButton.Height = buttonHeight;
        
        // İçerideki ikon nesnesini bul ve boyutlandır
        var rightIcon = ScrollRightButton.GetVisualDescendants().OfType<Icon>().FirstOrDefault();
        var leftIcon = ScrollLeftButton.GetVisualDescendants().OfType<Icon>().FirstOrDefault();
        
        if (rightIcon != null)
            rightIcon.FontSize = iconSize;
            
        if (leftIcon != null)
            leftIcon.FontSize = iconSize;
            
        // Ekran boyutuna göre margin ayarla
        bool isSmallScreen = width <= 750;
        double rightMargin = isSmallScreen ? 0 : 5;
        
        ScrollRightButton.Margin = new Thickness(0, 10, rightMargin, 0);
        
        double commonLeftMargin = isSmallScreen 
            ? 20  // Küçük ekranlar için sabit margin
            : CalculateScaledValue(clampedWidth, 40, 80); // Büyük ekranlar için ölçeklendirilmiş margin
            
        double leftMargin = commonLeftMargin - (!isSmallScreen ? 40 : 0);
        ScrollLeftButton.Margin = new Thickness(leftMargin, 10, 0, 0);
        
        ItemsDisplay.Margin = new Thickness(IsSimilarMovies ? 10: commonLeftMargin-10, 0, 0, 0);
    }
    
    private async void ScrollRightButton_OnClick(object sender, RoutedEventArgs e)
    {
        await AnimatedScrollRight();
    }
    
    private async void ScrollLeftButton_OnClick(object sender, RoutedEventArgs e)
    {
        await AnimatedScrollLeft();
    }
    
    private void ScrollButton_OnPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is Button button)
            button.Opacity = 1;
    }
    
    private void ScrollButton_OnPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is Button button)
            button.Opacity = 0.2;
    }
    
    private async Task AnimatedScrollRight()
    {
        await VisualHelperLib.AnimatedScrollRight(ContentScrollViewer, ItemsSource.Count, _cardWidth);
        await Task.Delay(300);
        ApplyStyles();
    }
    
    private async Task AnimatedScrollLeft()
    {
        await VisualHelperLib.AnimatedScrollLeft(ContentScrollViewer, ItemsSource.Count, _cardWidth);
        await Task.Delay(300);
        ApplyStyles();
    }
    

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == BoundsProperty)
        {
            ApplyStyles();
        }
        else if (change.Property == ItemsSourceProperty)
        {
            ItemsDisplay.ItemsSource = this.ItemsSource;
        }
    }
    
    // Yardımcı metotlar
    private double CalculateScaledValue(double width, double minValue, double maxValue)
    {
        const double minWidth = 320;
        const double maxWidth = 3840;
        
        double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
        
        double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
        double scaledValue = minValue + scale * (maxValue - minValue);
        
        return Math.Round(scaledValue);
    }
    
    private double CalculateCardWidth(double width)
    {
        return CalculateScaledValue(width, 120, 400);
    }
    
    private double CalculateTextSize(double width, double minSize, double maxSize)
    {
        return CalculateScaledValue(width, minSize, maxSize);
    }
}