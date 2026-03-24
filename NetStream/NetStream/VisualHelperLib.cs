using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using NetStream.Extensions;
using Serilog;

namespace NetStream;

public class VisualHelperLib
{
    public static async Task AnimatedScrollRight(ScrollViewer scrollViewer,int itemCount)
    {
        try
        {
            if (scrollViewer != null)
            {
                // Extent kontrolü - eğer tamamen yüklenmemişse işlemi iptal et
                if (scrollViewer.Extent.Width <= 0)
                {
                    Log.Warning("Cannot scroll right: ScrollViewer extent is zero or negative.");
                    return;
                }

                // Scrollable alan
                var ScrollableWidth = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
                
                // Öğe sayısına göre kaydırma mesafesi (daha güvenli optimizasyon)
                double itemWidth = 260; // Sabit item genişliği
                var visibleItemCount = Math.Max(1, Math.Floor(scrollViewer.Viewport.Width / itemWidth));
                var targetScrollAmount = visibleItemCount * itemWidth;
                
                // Mevcut kaydırma pozisyonu
                var currentScrollPos = scrollViewer.Offset.X;
                
                // Eğer sağa kaydırılabilecek alan varsa
                if (currentScrollPos < ScrollableWidth)
                {
                    // Hedef pozisyon, mevcut + kaydırma mesafesi (ScrollableWidth'i geçmeyecek şekilde)
                    var targetPos = Math.Min(ScrollableWidth, currentScrollPos + targetScrollAmount);
                    await scrollViewer.AnimatedScrollToHorizontalOffsetAsync(targetPos);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error scrolling right: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    
     
        
        
    public static async Task AnimatedScrollLeft(ScrollViewer scrollViewer,int itemCount)
    {
        try
        {
            if (scrollViewer != null)
            {
                // Extent kontrolü - eğer tamamen yüklenmemişse işlemi iptal et
                if (scrollViewer.Extent.Width <= 0)
                {
                    Log.Warning("Cannot scroll left: ScrollViewer extent is zero or negative.");
                    return;
                }
                
                // Mevcut kaydırma pozisyonu
                var currentScrollPos = scrollViewer.Offset.X;
                
                // Öğe sayısına göre kaydırma mesafesi (daha güvenli optimizasyon)
                double itemWidth = 260; // Sabit item genişliği
                var visibleItemCount = Math.Max(1, Math.Floor(scrollViewer.Viewport.Width / itemWidth));
                var targetScrollAmount = visibleItemCount * itemWidth;
                
                // Eğer sola kaydırılabilecek alan varsa
                if (currentScrollPos > 0)
                {
                    // Hedef pozisyon, mevcut - kaydırma mesafesi (0'ın altına düşmeyecek şekilde)
                    var targetPos = Math.Max(0, currentScrollPos - targetScrollAmount);
                    await scrollViewer.AnimatedScrollToHorizontalOffsetAsync(targetPos);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error scrolling left: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    
    
    public static async Task AnimatedScrollRight(ListBox listBox,int itemCount,double itemWidth)
    {
        try
        {
            var scrollViewer = listBox.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer != null)
            {
                // Extent kontrolü - eğer ListBox tam olarak yüklenmemişse işlemi iptal et
                if (scrollViewer.Extent.Width <= 0)
                {
                    Log.Warning("Cannot scroll right: ScrollViewer extent is zero or negative.");
                    return;
                }

                itemWidth = itemWidth + 22;

                // Scrollable alan
                var ScrollableWidth = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
                
                // Öğe sayısına göre kaydırma mesafesi (daha güvenli optimizasyon)
                var visibleItemCount = Math.Max(1, Math.Floor(scrollViewer.Viewport.Width / itemWidth));

                Console.WriteLine("scrollViewer.Viewport.Width: " + scrollViewer.Viewport.Width);
                Console.WriteLine("itemWidth: " + itemWidth);
                var targetScrollAmount = visibleItemCount * itemWidth;
                
                // Mevcut kaydırma pozisyonu
                var currentScrollPos = scrollViewer.Offset.X;
                
                // Eğer sağa kaydırılabilecek alan varsa
                if (currentScrollPos < ScrollableWidth)
                {
                    // Hedef pozisyon, mevcut + kaydırma mesafesi (ScrollableWidth'i geçmeyecek şekilde)
                    var targetPos = Math.Min(ScrollableWidth, currentScrollPos + targetScrollAmount);
                    await scrollViewer.AnimatedScrollToHorizontalOffsetAsync(targetPos);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error scrolling right: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    
    public static async Task AnimatedScrollRight(ScrollViewer scrollViewer,int itemCount,double itemWidth)
    {
        try
        {
            if (scrollViewer != null)
            {
                // Extent kontrolü - eğer ListBox tam olarak yüklenmemişse işlemi iptal et
                if (scrollViewer.Extent.Width <= 0)
                {
                    Log.Warning("Cannot scroll right: ScrollViewer extent is zero or negative.");
                    return;
                }

                itemWidth = itemWidth + 22;

                // Scrollable alan
                var ScrollableWidth = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
                
                // Öğe sayısına göre kaydırma mesafesi (daha güvenli optimizasyon)
                var visibleItemCount = Math.Max(1, Math.Floor(scrollViewer.Viewport.Width / itemWidth));

                Console.WriteLine("scrollViewer.Viewport.Width: " + scrollViewer.Viewport.Width);
                Console.WriteLine("itemWidth: " + itemWidth);
                var targetScrollAmount = visibleItemCount * itemWidth;
                
                // Mevcut kaydırma pozisyonu
                var currentScrollPos = scrollViewer.Offset.X;
                
                // Eğer sağa kaydırılabilecek alan varsa
                if (currentScrollPos < ScrollableWidth)
                {
                    // Hedef pozisyon, mevcut + kaydırma mesafesi (ScrollableWidth'i geçmeyecek şekilde)
                    var targetPos = Math.Min(ScrollableWidth, currentScrollPos + targetScrollAmount);
                    await scrollViewer.AnimatedScrollToHorizontalOffsetAsync(targetPos);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error scrolling right: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    
     
        
        
    public static async Task AnimatedScrollLeft(ListBox listBox,int itemCount,double itemWidth)
    {
        try
        {
            var scrollViewer = listBox.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer != null)
            {
                // Extent kontrolü - eğer ListBox tam olarak yüklenmemişse işlemi iptal et
                if (scrollViewer.Extent.Width <= 0)
                {
                    Log.Warning("Cannot scroll left: ScrollViewer extent is zero or negative.");
                    return;
                }
                itemWidth = itemWidth + 22;
                // Mevcut kaydırma pozisyonu
                var currentScrollPos = scrollViewer.Offset.X;
                
                // Öğe sayısına göre kaydırma mesafesi (daha güvenli optimizasyon)
                var visibleItemCount = Math.Max(1, Math.Floor(scrollViewer.Viewport.Width / itemWidth));
                var targetScrollAmount = visibleItemCount * itemWidth;
                
                // Eğer sola kaydırılabilecek alan varsa
                if (currentScrollPos > 0)
                {
                    // Hedef pozisyon, mevcut - kaydırma mesafesi (0'ın altına düşmeyecek şekilde)
                    var targetPos = Math.Max(0, currentScrollPos - targetScrollAmount);
                    await scrollViewer.AnimatedScrollToHorizontalOffsetAsync(targetPos);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error scrolling left: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    
    public static async Task AnimatedScrollLeft(ScrollViewer scrollViewer,int itemCount,double itemWidth)
    {
        try
        {
            if (scrollViewer != null)
            {
                // Extent kontrolü - eğer ListBox tam olarak yüklenmemişse işlemi iptal et
                if (scrollViewer.Extent.Width <= 0)
                {
                    Log.Warning("Cannot scroll left: ScrollViewer extent is zero or negative.");
                    return;
                }
                itemWidth = itemWidth + 22;
                // Mevcut kaydırma pozisyonu
                var currentScrollPos = scrollViewer.Offset.X;
                
                // Öğe sayısına göre kaydırma mesafesi (daha güvenli optimizasyon)
                var visibleItemCount = Math.Max(1, Math.Floor(scrollViewer.Viewport.Width / itemWidth));
                var targetScrollAmount = visibleItemCount * itemWidth;
                
                // Eğer sola kaydırılabilecek alan varsa
                if (currentScrollPos > 0)
                {
                    // Hedef pozisyon, mevcut - kaydırma mesafesi (0'ın altına düşmeyecek şekilde)
                    var targetPos = Math.Max(0, currentScrollPos - targetScrollAmount);
                    await scrollViewer.AnimatedScrollToHorizontalOffsetAsync(targetPos);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error scrolling left: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
}