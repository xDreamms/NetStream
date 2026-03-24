using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Projektanker.Icons.Avalonia;
using ObservableExtensions = System.ObservableExtensions;


namespace NetStream.Controls
{
    public partial class RatingBar : UserControl
    {
        private readonly StackPanel _starsPanel;
        private readonly IBrush _filledStarBrush = new SolidColorBrush(Color.Parse("#E50914"));
        private readonly IBrush _emptyStarBrush = new SolidColorBrush(Color.Parse("#808080"));
        
        public static readonly StyledProperty<double> RatingProperty =
            AvaloniaProperty.Register<RatingBar, double>(nameof(Rating), defaultValue: 0);
        
        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<RatingBar, bool>(nameof(IsReadOnly), defaultValue: false);
        
        public static readonly StyledProperty<int> MaxStarsProperty =
            AvaloniaProperty.Register<RatingBar, int>(nameof(MaxStars), defaultValue: 5);
        
        public static readonly StyledProperty<double> StarSizeProperty =
            AvaloniaProperty.Register<RatingBar, double>(nameof(StarSize), defaultValue: 24.0);
        
        public static readonly StyledProperty<double> SpacingProperty =
            AvaloniaProperty.Register<RatingBar, double>(nameof(Spacing), defaultValue: 4.0);
        
        public static readonly RoutedEvent<RoutedEventArgs> RatingChangedEvent =
            RoutedEvent.Register<RatingBar, RoutedEventArgs>(nameof(RatingChanged), RoutingStrategies.Bubble);
        
        public event EventHandler<RoutedEventArgs> RatingChanged
        {
            add => AddHandler(RatingChangedEvent, value);
            remove => RemoveHandler(RatingChangedEvent, value);
        }
        
        public double Rating
        {
            get => GetValue(RatingProperty);
            set => SetValue(RatingProperty, value);
        }
        
        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }
        
        public int MaxStars
        {
            get => GetValue(MaxStarsProperty);
            set => SetValue(MaxStarsProperty, value);
        }
        
        public double StarSize
        {
            get => GetValue(StarSizeProperty);
            set => SetValue(StarSizeProperty, value);
        }
        
        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }
        
        public RatingBar()
        {
            _starsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = Spacing
            };
            
            Content = _starsPanel;
            
            ObservableExtensions.Subscribe(this.GetObservable(RatingProperty), _ => UpdateStars());
            ObservableExtensions.Subscribe(this.GetObservable(MaxStarsProperty), _ => CreateStars());
            ObservableExtensions.Subscribe(this.GetObservable(StarSizeProperty), _ => UpdateStarSize());
            ObservableExtensions.Subscribe(this.GetObservable(SpacingProperty), spacing => _starsPanel.Spacing = spacing);
            ObservableExtensions.Subscribe(this.GetObservable(IsReadOnlyProperty), _ => UpdateInteractivity());
            
            CreateStars();
        }
        
        private void CreateStars()
        {
            _starsPanel.Children.Clear();
            
            for (int i = 0; i < MaxStars; i++)
            {
                var star = new Icon
                {
                    Value = "fa-light fa-star",
                    FontSize = StarSize,
                    Tag = i + 1
                };
                
                star.PointerPressed += Star_PointerPressed;
                star.PointerEntered += Star_PointerEntered;
                star.PointerExited += Star_PointerExited;
                
                _starsPanel.Children.Add(star);
            }
            
            UpdateStars();
            UpdateInteractivity();
        }
        
        public static double RoundToNearestHalf(double value)
        {
            return Math.Round(value * 2, MidpointRounding.AwayFromZero) / 2.0;
        }
        
        private void UpdateStars()
        {
            double rounded = RoundToNearestHalf(Rating);

            for (int i = 0; i < _starsPanel.Children.Count; i++)
            {
                if (_starsPanel.Children[i] is Icon star)
                {
                    if (i + 1 <= rounded)
                    {
                        // Tam dolu yıldız
                        star.Value = "fa-solid fa-star";
                        star.Foreground = _filledStarBrush;
                    }
                    else if (i + 0.5 == rounded)
                    {
                        // Yarım yıldız
                        star.Value = "fa-solid fa-star-half-stroke";
                        star.Foreground = _filledStarBrush;
                    }
                    else
                    {
                        // Boş yıldız
                        star.Value = "fa-light fa-star";
                        star.Foreground = _emptyStarBrush;
                    }
                }
            }
        }
        
        private void Star_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (IsReadOnly) return;
            
            if (sender is Icon star && star.Tag is int rating)
            {
                Rating = rating;
                RaiseEvent(new RoutedEventArgs(RatingChangedEvent));
            }
        }
        
        private void Star_PointerEntered(object sender, PointerEventArgs e)
        {
            if (IsReadOnly) return;
            
            if (sender is Icon currentStar && currentStar.Tag is int rating)
            {
                // Highlight stars up to the current one
                foreach (var child in _starsPanel.Children)
                {
                    if (child is Icon star && star.Tag is int starRating)
                    {
                        if (starRating <= rating)
                        {
                            star.Foreground = _filledStarBrush;
                            star.Value = "fa-solid fa-star";
                        }
                        else
                        {
                            star.Foreground = _emptyStarBrush;
                            star.Value = "fa-light fa-star";
                        }
                    }
                }
            }
        }
        
        private void Star_PointerExited(object sender, PointerEventArgs e)
        {
            if (IsReadOnly) return;
            
            UpdateStars();
        }
        
        private void UpdateStarSize()
        {
            foreach (var child in _starsPanel.Children)
            {
                if (child is Icon star)
                {
                    star.FontSize = StarSize;
                }
            }
        }
        
        private void UpdateInteractivity()
        {
            foreach (var child in _starsPanel.Children)
            {
                if (child is Icon star)
                {
                    star.Cursor = IsReadOnly ? new Cursor(StandardCursorType.Arrow) : new Cursor(StandardCursorType.Hand);
                }
            }
        }
    }
} 