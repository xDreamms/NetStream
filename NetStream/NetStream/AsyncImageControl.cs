using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Serilog;

namespace NetStream
{
    public class AsyncImageControl : UserControl, IDisposable
    {
        private readonly Image _image;
        private static Bitmap _fallbackImage;
        private int _retryCount = 0;
        private const int MaxRetries = 3;
        private Bitmap _currentBitmap;
        
        public static readonly StyledProperty<string> SourceProperty = 
            AvaloniaProperty.Register<AsyncImageControl, string>(nameof(Source));
            
        public static readonly StyledProperty<Stretch> StretchProperty = 
            AvaloniaProperty.Register<AsyncImageControl, Stretch>(nameof(Stretch), Stretch.UniformToFill);
        
        public static readonly StyledProperty<bool> ShowFallbackOnErrorProperty =
            AvaloniaProperty.Register<AsyncImageControl, bool>(nameof(ShowFallbackOnError), true);
            
        public string Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        
        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }
        
        public bool ShowFallbackOnError
        {
            get => GetValue(ShowFallbackOnErrorProperty);
            set => SetValue(ShowFallbackOnErrorProperty, value);
        }
        
        
        // Web platformunda çalışıp çalışmadığını kontrol eden özellik
        private static bool IsRunningInBrowser => 
            System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Browser") ||
            AppContext.GetData("IsRunningInBrowser") is bool isInBrowser && isInBrowser;
            
        // Android platformunda çalışıp çalışmadığını kontrol eden özellik
        private static bool IsRunningOnAndroid =>
            System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Android") ||
            AppContext.GetData("IsRunningOnAndroid") is bool isOnAndroid && isOnAndroid ||
            // Android'in bazı sürümleri için System.Environment.OSVersion.Platform kontrolü
            Environment.OSVersion.Platform.ToString().Contains("Unix") && 
            System.IO.Directory.Exists("/system/app");
        
        public AsyncImageControl()
        {
            _image = new Image();
            Content = _image;
            
            // Görünüm özelliklerini bu Image'e bağlayalım
            _image.Bind(Image.StretchProperty, this.GetObservable(StretchProperty));
            
            // Yedek resmi yükle (sadece bir kez)
            //InitializeFallbackImage();
            
            // Debug için platform bilgisi
        }
        
        ~AsyncImageControl()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearCurrentBitmap();
            }
        }
        
        private void ClearCurrentBitmap()
        {
            if (_currentBitmap != null)
            {
                _image.Source = null;
                // Don't dispose the bitmap - it may be shared from the memory cache
                // Just clear the reference so GC can collect it when no longer in use
                _currentBitmap = null;
            }
        }
        
        static AsyncImageControl()
        {
            SourceProperty.Changed.Subscribe(OnSourceChanged);
        }
        
        private static void OnSourceChanged(AvaloniaPropertyChangedEventArgs<string> e)
        {
            if (e.Sender is AsyncImageControl control)
            {
                control.LoadImage();
            }
        }
        
        private async void LoadImage()
        {
            _retryCount = 0;
            await LoadImageWithRetry();
        }
        
        private async Task LoadImageWithRetry()
        {
            try
            {
                if (string.IsNullOrEmpty(Source))
                {
                    // Önce mevcut bitmap'i temizle
                    ClearCurrentBitmap();
                    
                    if (ShowFallbackOnError && _fallbackImage != null)
                    {
                        _image.Source = _fallbackImage;
                    }
                    return;
                }
                
                // Yükleme başladığını göstermek için loading gösterilebilir
                
                // Android için özel işlem
                string processedSource = Source;
                if (IsRunningOnAndroid)
                {
                    if (processedSource.StartsWith("http://"))
                    {
                        processedSource = "https://" + processedSource.Substring(7);
                        Log.Debug($"Android için URL HTTPS'e dönüştürüldü: {Source} -> {processedSource}");
                    }
                }
                
                // AsyncImageLoader'ı kullanarak resmi yükle
                var bitmap = await AsyncImageLoader.LoadFromWeb(processedSource);
                
                // UI thread üzerinde Image özelliğini güncelle
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (bitmap != null)
                    {
                        // Önceki bitmap'i temizle
                        ClearCurrentBitmap();
                        
                        // Yeni bitmap'i ayarla
                        _currentBitmap = bitmap;
                        _image.Source = _currentBitmap;
                    }
                    else if (ShowFallbackOnError && _fallbackImage != null)
                    {
                        // Önceki bitmap'i temizle
                        ClearCurrentBitmap();
                        
                        // Resim yüklenemezse ve fallback gösterilmesi isteniyorsa
                        _image.Source = _fallbackImage;
                    }
                    else
                    {
                        // Önceki bitmap'i temizle
                        ClearCurrentBitmap();
                        
                        // Android için yeniden deneme
                        if (IsRunningOnAndroid && _retryCount < MaxRetries)
                        {
                            _retryCount++;
                            Task.Delay(1000 * _retryCount).ContinueWith(_ => LoadImageWithRetry());
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error($"AsyncImageControl resim yüklerken hata: {ex.Message} - URL: {Source}, Platform: {(IsRunningOnAndroid ? "Android" : IsRunningInBrowser ? "Web" : "Desktop")}");
                
                // Android için yeniden deneme
                if (IsRunningOnAndroid && _retryCount < MaxRetries)
                {
                    _retryCount++;
                    Log.Warning($"Android'de hata nedeniyle yeniden deneniyor ({_retryCount}/{MaxRetries}): {Source}");
                    await Task.Delay(1000 * _retryCount);
                    await LoadImageWithRetry();
                    return;
                }
                
                // Hata durumunda yedek resmi göster
                if (ShowFallbackOnError && _fallbackImage != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Önceki bitmap'i temizle
                        ClearCurrentBitmap();
                        
                        _image.Source = _fallbackImage;
                    });
                }
            }
        }
    }
    
    
    
    public class AsyncImageControl2 : UserControl, IDisposable
    {
        private readonly Image _image;
        private static Bitmap _fallbackImage;
        private int _retryCount = 0;
        private const int MaxRetries = 3;
        private Bitmap _currentBitmap;
        
        public static readonly StyledProperty<string> SourceProperty = 
            AvaloniaProperty.Register<AsyncImageControl, string>(nameof(Source));
            
        public static readonly StyledProperty<Stretch> StretchProperty = 
            AvaloniaProperty.Register<AsyncImageControl, Stretch>(nameof(Stretch), Stretch.UniformToFill);
        
        public static readonly StyledProperty<bool> ShowFallbackOnErrorProperty =
            AvaloniaProperty.Register<AsyncImageControl, bool>(nameof(ShowFallbackOnError), true);
            
        public string Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        
        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }
        
        public bool ShowFallbackOnError
        {
            get => GetValue(ShowFallbackOnErrorProperty);
            set => SetValue(ShowFallbackOnErrorProperty, value);
        }
        
        // Web platformunda çalışıp çalışmadığını kontrol eden özellik
        private static bool IsRunningInBrowser => 
            System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Browser") ||
            AppContext.GetData("IsRunningInBrowser") is bool isInBrowser && isInBrowser;
            
        // Android platformunda çalışıp çalışmadığını kontrol eden özellik
        private static bool IsRunningOnAndroid =>
            System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Android") ||
            AppContext.GetData("IsRunningOnAndroid") is bool isOnAndroid && isOnAndroid ||
            // Android'in bazı sürümleri için System.Environment.OSVersion.Platform kontrolü
            Environment.OSVersion.Platform.ToString().Contains("Unix") && 
            System.IO.Directory.Exists("/system/app");
        
        public AsyncImageControl2()
        {
            _image = new Image();
            
            // Görünüm özelliklerini bu Image'e bağlayalım
            _image.Bind(Image.StretchProperty, this.GetObservable(StretchProperty));
            
            // Soldan sağa doğru şeffaflaşan gradyan mask uygula
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 0, RelativeUnit.Relative)
            };
            gradientBrush.GradientStops.Add(new GradientStop(Colors.Black, 0));
            gradientBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
            
            _image.OpacityMask = gradientBrush;
            
            // Gölge efekti
            this.Effect = new DropShadowDirectionEffect
            {
                Color = Colors.Black,
                BlurRadius = 10,
                ShadowDepth = 0
            };
            
            Content = _image;
        }
        
        ~AsyncImageControl2()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearCurrentBitmap();
            }
        }
        
        private void ClearCurrentBitmap()
        {
            if (_currentBitmap != null)
            {
                _image.Source = null;
                // Don't dispose the bitmap - it may be shared from the memory cache
                // Just clear the reference so GC can collect it when no longer in use
                _currentBitmap = null;
            }
        }
        
        static AsyncImageControl2()
        {
            SourceProperty.Changed.Subscribe(OnSourceChanged);
        }
        
        private static void OnSourceChanged(AvaloniaPropertyChangedEventArgs<string> e)
        {
            if (e.Sender is AsyncImageControl2 control)
            {
                control.LoadImage();
            }
        }
        
        private async void LoadImage()
        {
            _retryCount = 0;
            await LoadImageWithRetry();
        }
        
        private async Task LoadImageWithRetry()
        {
            try
            {
                if (string.IsNullOrEmpty(Source))
                {
                    // Önce mevcut bitmap'i temizle
                    ClearCurrentBitmap();
                    
                    if (ShowFallbackOnError && _fallbackImage != null)
                    {
                        _image.Source = _fallbackImage;
                    }
                    return;
                }
                
                // Yükleme başladığını göstermek için loading gösterilebilir
                
                // Android için özel işlem
                string processedSource = Source;
                if (IsRunningOnAndroid)
                {
                    if (processedSource.StartsWith("http://"))
                    {
                        processedSource = "https://" + processedSource.Substring(7);
                        Log.Debug($"Android için URL HTTPS'e dönüştürüldü: {Source} -> {processedSource}");
                    }
                }
                
                // AsyncImageLoader'ı kullanarak resmi yükle
                var bitmap = await AsyncImageLoader.LoadFromWeb(processedSource);
                
                // UI thread üzerinde Image özelliğini güncelle
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (bitmap != null)
                    {
                        // Önceki bitmap'i temizle
                        ClearCurrentBitmap();
                        
                        // Yeni bitmap'i ayarla
                        _currentBitmap = bitmap;
                        _image.Source = _currentBitmap;
                    }
                    else if (ShowFallbackOnError && _fallbackImage != null)
                    {
                        // Önceki bitmap'i temizle
                        ClearCurrentBitmap();
                        
                        // Resim yüklenemezse ve fallback gösterilmesi isteniyorsa
                        _image.Source = _fallbackImage;
                    }
                    else
                    {
                        // Önceki bitmap'i temizle
                        ClearCurrentBitmap();
                        
                        // Android için yeniden deneme
                        if (IsRunningOnAndroid && _retryCount < MaxRetries)
                        {
                            _retryCount++;
                            Task.Delay(1000 * _retryCount).ContinueWith(_ => LoadImageWithRetry());
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error($"AsyncImageControl resim yüklerken hata: {ex.Message} - URL: {Source}, Platform: {(IsRunningOnAndroid ? "Android" : IsRunningInBrowser ? "Web" : "Desktop")}");
                
                // Android için yeniden deneme
                if (IsRunningOnAndroid && _retryCount < MaxRetries)
                {
                    _retryCount++;
                    Log.Warning($"Android'de hata nedeniyle yeniden deneniyor ({_retryCount}/{MaxRetries}): {Source}");
                    await Task.Delay(1000 * _retryCount);
                    await LoadImageWithRetry();
                    return;
                }
                
                // Hata durumunda yedek resmi göster
                if (ShowFallbackOnError && _fallbackImage != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Önceki bitmap'i temizle
                        ClearCurrentBitmap();
                        
                        _image.Source = _fallbackImage;
                    });
                }
            }
        }
    }
} 