using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using Avalonia;

namespace NetStream.Extensions
{
    public static class ScrollViewerExtensions
    {
        /// <summary>
        /// Animasyonlu yatay kaydırma yapan uzantı metodu
        /// </summary>
        /// <param name="scrollViewer">Kaydırma yapılacak ScrollViewer</param>
        /// <param name="offset">Hedef kaydırma konumu</param>
        /// <param name="duration">Animasyon süresi (ms)</param>
        /// <returns>Animasyon tamamlandığında dönen görev</returns>
        public static async Task AnimatedScrollToHorizontalOffsetAsync(this ScrollViewer scrollViewer, double offset, int duration = 300)
        {
            if (scrollViewer == null)
                return;

            double startOffset = scrollViewer.Offset.X;
            double distance = offset - startOffset;
            
            // Minimum 10 ms adımlarla kaydırma
            int steps = Math.Max(duration / 10, 1);
            int stepDuration = duration / steps;
            
            for (int i = 0; i < steps; i++)
            {
                // Easing fonksiyonu: ease-out cubic
                double progress = i / (double)steps;
                double easedProgress = 1 - Math.Pow(1 - progress, 3);
                double newOffset = startOffset + (distance * easedProgress);
                
                // UI thread üzerinde kaydırma yapılmalı - Offset özelliğini kullan
                await Dispatcher.UIThread.InvokeAsync(() => 
                    scrollViewer.Offset = new Vector(newOffset, scrollViewer.Offset.Y));
                
                await Task.Delay(stepDuration);
            }
            
            // Son konuma tam olarak kaydır
            await Dispatcher.UIThread.InvokeAsync(() => 
                scrollViewer.Offset = new Vector(offset, scrollViewer.Offset.Y));
        }
    }
} 