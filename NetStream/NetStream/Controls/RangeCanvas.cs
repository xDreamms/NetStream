using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace NetStream.Controls
{
    public class RangeCanvas : Control
    {
        // Arka plan rengi
        public static readonly StyledProperty<IBrush> BackgroundColorProperty =
            AvaloniaProperty.Register<RangeCanvas, IBrush>(
                nameof(BackgroundColor),
                new SolidColorBrush(Colors.Gray));

        // Bölge rengi
        public static readonly StyledProperty<IBrush> RegionColorProperty =
            AvaloniaProperty.Register<RangeCanvas, IBrush>(
                nameof(RegionColor),
                new SolidColorBrush(Colors.LightBlue));

        // Hover durumunda büyütme faktörü
        public static readonly StyledProperty<double> HoverScaleProperty =
            AvaloniaProperty.Register<RangeCanvas, double>(
                nameof(HoverScale),
                2.0);

        // Render'ı zorla tetiklemek için versiyon sayacı
        public static readonly StyledProperty<int> RegionVersionProperty =
            AvaloniaProperty.Register<RangeCanvas, int>(nameof(RegionVersion));

        static RangeCanvas()
        {
            AffectsRender<RangeCanvas>(RegionVersionProperty, BackgroundColorProperty, RegionColorProperty);
        }

        public int RegionVersion
        {
            get => GetValue(RegionVersionProperty);
            private set => SetValue(RegionVersionProperty, value);
        }

        // Aralık listesi
        private List<(double Start, double End, IBrush Color)> _regions =
            new List<(double Start, double End, IBrush Color)>();

        // Animasyon için transform
        private ScaleTransform _scaleTransform;

        // Önceki animasyonu iptal etmek için
        private CancellationTokenSource _animationCts;

        public IBrush BackgroundColor
        {
            get => GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        public IBrush RegionColor
        {
            get => GetValue(RegionColorProperty);
            set => SetValue(RegionColorProperty, value);
        }

        public double HoverScale
        {
            get => GetValue(HoverScaleProperty);
            set => SetValue(HoverScaleProperty, value);
        }

        public RangeCanvas()
        {
            // Transform'u ayarla
            _scaleTransform = new ScaleTransform(1, 1);
            RenderTransform = _scaleTransform;
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        }

        private void AnimateScale(double targetScale)
        {
            try
            {
                // Önceki animasyonu iptal et
                _animationCts?.Cancel();
                _animationCts?.Dispose();
                _animationCts = new CancellationTokenSource();
                var token = _animationCts.Token;

                double currentScale = _scaleTransform.ScaleY;

                var animation = new Animation
                {
                    Duration = TimeSpan.FromSeconds(0.15),
                    FillMode = FillMode.None // Forward kullanma - değeri manuel set edeceğiz
                };

                animation.Children.Add(new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(ScaleTransform.ScaleYProperty, currentScale) }
                });

                animation.Children.Add(new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(ScaleTransform.ScaleYProperty, targetScale) }
                });

                // Animasyonu başlat, bitince değeri kalıcı olarak ayarla
                animation.RunAsync(this, token).ContinueWith(_ =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        Dispatcher.UIThread.Post(() => _scaleTransform.ScaleY = targetScale);
                    }
                });
            }
            catch (Exception)
            {
                // Hata olursa doğrudan transform'u ayarla
                _scaleTransform.ScaleY = targetScale;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            try
            {
                _animationCts?.Cancel();
                _animationCts?.Dispose();
                _animationCts = null;
            }
            catch (Exception)
            {
                // Herhangi bir hata oluşursa sessizce yok say
            }
        }

        // Bölge eklemek için metot
        public void AddRegion(double start, double end, IBrush color = null)
        {
            start = Math.Max(0, Math.Min(1, start));
            end = Math.Max(0, Math.Min(1, end));
            if (start >= end) return;

            _regions.Add((start, end, color ?? RegionColor));
            RegionVersion++;
        }

        // Tüm bölgeleri temizle
        public void ClearRegions()
        {
            _regions.Clear();
            RegionVersion++;
        }

        // Bölge listesini ayarla
        public void SetRegions(List<(double Start, double End, IBrush Color)> regions)
        {
            _regions.Clear();

            foreach (var region in regions)
            {
                double start = Math.Max(0, Math.Min(1, region.Start));
                double end = Math.Max(0, Math.Min(1, region.End));
                if (start >= end) continue;

                _regions.Add((start, end, region.Color));
            }

            RegionVersion++;
        }

        // Sadece start ve end değerlerini alıp varsayılan rengi kullan
        public void SetRegions(List<(double Start, double End)> regions)
        {
            _regions.Clear();

            foreach (var region in regions)
            {
                double start = Math.Max(0, Math.Min(1, region.Start));
                double end = Math.Max(0, Math.Min(1, region.End));
                if (start >= end) continue;

                _regions.Add((start, end, RegionColor));
            }

            RegionVersion++;
        }

        public override void Render(DrawingContext context)
        {
            // Arka planı çiz
            context.FillRectangle(BackgroundColor, new Rect(0, 0, Bounds.Width, Bounds.Height));

            // Bölgeleri çiz
            foreach (var region in _regions)
            {
                double startX = region.Start * Bounds.Width;
                double width = (region.End - region.Start) * Bounds.Width;
                
                context.FillRectangle(
                    region.Color, 
                    new Rect(startX, 0, width, Bounds.Height)
                );
            }
        }
        
        // Ölçeği doğrudan büyütmek için public metot
        public void ScaleUp()
        {
            AnimateScale(HoverScale);
        }
        
        // Ölçeği doğrudan küçültmek için public metot
        public void ScaleDown()
        {
            AnimateScale(1.0);
        }
    }
} 