using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetStream.Views;
using Projektanker.Icons.Avalonia;
using Serilog;

namespace NetStream.Controls;

public partial class HorizontalCastItemsControl : UserControl
{
    public static readonly StyledProperty<ObservableCollection<Cast>> ItemsSourceProperty =
        AvaloniaProperty.Register<HorizontalMovieItemsControl, ObservableCollection<Cast>>(nameof(ItemsSource));
        
  
    private ObservableCollection<Cast> _lastItemsSource;
    
    public ObservableCollection<Cast> ItemsSource
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
    private void UpdateItemsDisplay(ObservableCollection<Cast> items)
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
            Console.WriteLine($"UpdateItemsDisplay error: {ex.Message}");
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
    
    private double _cardWidth = 200;
     private void ApplyDirectStylesToItems(double width)
    {
        if (ItemsSource == null || ItemsSource.Count == 0)
        {
            return;
        }
        
        double clampedWidth = Math.Min(width, 3840);
        
        _cardWidth = CalculateCardWidth(clampedWidth);
        double cardHeight = _cardWidth * 1.5;
        
        // Font boyutlarını hesapla
        double titleFontSize = CalculateTextSize(width, 12, 24);
        double ratingFontSize = CalculateTextSize(width, 10, 18);
        
        try
        {
            var visibleDescendants = ItemsDisplay.GetVisualDescendants().ToList();
                
            var borders = visibleDescendants.OfType<Border>()
                .Where(b => b.Name == "MovieCardBorder" || b.Classes.Contains("movie-card"))
                .ToList();
                
            var titleBlocks = visibleDescendants.OfType<TextBlock>()
                .Where(t => t.Name == "CastName")
                .ToList();
                
            var ratingTexts = visibleDescendants.OfType<TextBlock>()
                .Where(t => t.Name == "CastRole")
                .ToList();
            
         
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
            
            foreach (var ratingText in ratingTexts)
            {
                ratingText.FontSize = ratingFontSize;
                ratingText.MaxWidth = _cardWidth;
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
        
        ItemsDisplay.Margin = new Thickness(10, 0, 0, 0);
    }
    
    public HorizontalCastItemsControl()
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
    
    private async void ScrollRightButton_OnClick(object sender, RoutedEventArgs e)
    {
        await AnimatedScrollRight();
    }
    
    private async void ScrollLeftButton_OnClick(object sender, RoutedEventArgs e)
    {
        await AnimatedScrollLeft();
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
    
    private void HandleItemsSourceChanged(ObservableCollection<Cast> items)
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

    private void StackPanelMain_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var stackPanel = sender as StackPanel;
        if(stackPanel == null) return;
        var cast = stackPanel.DataContext as Cast;
        if (cast == null) return;
        CastPageControl castPage = new CastPageControl(cast);
        MainView.Instance.SetContent(castPage);
    }
}