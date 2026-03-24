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
using Serilog;
using Avalonia.Media;
using NetStream.Views;
using NetStream.Navigation;

namespace NetStream.Controls;

public partial class VerticalMovieItemsControl : UserControl
{
    public static readonly StyledProperty<ObservableCollection<Movie>> ItemsSourceProperty =
        AvaloniaProperty.Register<VerticalMovieItemsControl, ObservableCollection<Movie>>(
            nameof(ItemsSource), defaultValue: new ObservableCollection<Movie>());
    

    public ObservableCollection<Movie> ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    
   
    public event EventHandler<EventArgs> ScrollReachedBottom;

    public VerticalMovieItemsControl()
    {
        InitializeComponent();
        
        this.GetObservable(ItemsSourceProperty).Subscribe(items =>
        {
            MoviesDisplay.ItemsSource = items;
        });
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == BoundsProperty)
        {
            // Boyutlar değiştiğinde responsive düzeni uygula
            if (MainView.Instance != null)
            {
                ApplyResponsiveLayout(MainView.Instance.Bounds.Width);
            }
        }
        else if (change.Property == ItemsSourceProperty)
        {
            // ItemsSource değiştiğinde MoviesDisplay'e uygula
            MoviesDisplay.ItemsSource = this.ItemsSource;
        }
    }
    
    private async void MoviesDisplay_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        try
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;
            
            if (scrollViewer.Extent.Height > 0 && 
                Math.Abs(scrollViewer.Offset.Y + scrollViewer.Viewport.Height - scrollViewer.Extent.Height) < 5)
            {
                // Kaydırma alt kısma ulaştığında olayı tetikle
                ScrollReachedBottom?.Invoke(this, EventArgs.Empty);
            }

            await Task.Delay(200);
            ApplyResponsiveLayout(MainView.Instance.screenWidth);
        }
        catch (Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            var stackPanel = sender as StackPanel;
            if(stackPanel == null) return;
            var selectedMovie = stackPanel.DataContext as Movie;
            if (selectedMovie != null)
            {
                NavigationService.Instance.Navigate(new MovieDetailsPage(selectedMovie));
            }
        }
        catch (Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }
    
    public void ApplyResponsiveLayout(double width)
    {
        try
        {
            double cardWidth = CalculateScaledValue(width, 120, 400, 320, 3840);
            double cardHeight = cardWidth * 1.5; // Oran koru
            double ratingFontSize = CalculateScaledValue(width, 12, 20, 320, 3840);
            double starSize = CalculateScaledValue(width, 15, 30, 320, 3840);
            
            
            foreach (var item in MoviesDisplay.GetVisualDescendants())
            {
                if (item is Border border && border.Name == "MovieCardBorder")
                {
                    border.Width = cardWidth;
                    border.Height = cardHeight;
                }
                else if (item is TextBlock textBlock)
                {
                    if (textBlock.Name == "MovieTitle")
                    {
                        textBlock.FontSize = CalculateScaledValue(width, 14, 26, 320, 3840);
                        textBlock.MaxWidth = cardWidth*0.8;
                    }
                    else if (textBlock.Name == "RatingText")
                    {
                        textBlock.FontSize = ratingFontSize;
                        double leftMargin = cardWidth * 0.06;
                        textBlock.Margin = new Thickness(leftMargin, 3.5, 0, 0);
                    }
                }
                else if (item is RatingBar ratingBar && ratingBar.Name == "MovieRating")
                {
                    ratingBar.StarSize = starSize;
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
}