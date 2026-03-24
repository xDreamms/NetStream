using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material.Icons.Avalonia;
using NetStream.Controls;
using Serilog;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;

namespace NetStream.Views;

public partial class MoviesPage : UserControl, IDisposable
{
    public static MoviesPage Instance;
   // private bool _isLoading;
    
    // public bool IsLoading
    // {
    //     get => _isLoading;
    //     set
    //     {
    //         _isLoading = value;
    //         if (LoadingIndicator != null)
    //         {
    //             Dispatcher.UIThread.InvokeAsync(() => 
    //             {
    //                 LoadingIndicator.IsVisible = value;
    //             });
    //         }
    //     }
    // }
    //
    public MoviesPage()
    {
        InitializeComponent();
        Instance = this;
        this.DataContext = this;
        MainMovieTvShow.DataContext = Service.MainMovieee;
        
        MainView.Instance.SizeChanged+= InstanceOnSizeChanged;

        // Başlangıçta veri yükleme işlemini async olarak başlat
        Task.Run(() => InitializeDataAsync());
    }
    
    private async Task InitializeDataAsync()
    {
        try
        {
            // UI yüklenmeden önce kullanıcıya yükleme göstergesini göster
           // await Dispatcher.UIThread.InvokeAsync(() => IsLoading = true);
            
            // Ana thread'in işlemleri tamamlaması için kısa bir gecikme
            await Task.Delay(50);
            
            // Veri listeleri için başlangıç kontrolü
            await CheckAndInitializeCollections();
            
            // UI thread'in güncellenmesi için bir tane daha kısa gecikme ekleyelim
            await Task.Delay(50);
            
            // MainMovieTvShow'u hemen ayarla, böylece banner görüntülenecek
            if (MainMovieTvShow != null && Service.MainMovieee != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    MainMovieTvShow.DataContext = Service.MainMovieee;
                    MainMovieTvShow.UpdateLayout();
                });
            }
            
            // Veri yükleme işlemi
            await OnLoad();
            
            // UI'ın güncel UI öğeleriyle yenilenmesi için kısa bir gecikme
            await Task.Delay(100);
            
            // Yükleme tamamlandı, göstergeyi gizle
            //await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
        catch (Exception ex)
        {
            Log.Error($"Error initializing data: {ex.Message}\nStackTrace: {ex.StackTrace}");
           // await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }
    
    private async Task OnLoad()
    {
        try
        {
            // İlk olarak UI öğelerini temizleyelim ve başlangıç değerlerini atayalım
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Koleksiyonları temizle
                Service.PopularMovies.Clear();
                Service.TopRatedMovies.Clear();
                Service.UpComingMovies.Clear();
                Service.NowPlayingMovies.Clear();
                
                // ItemsControl nesnelerine koleksiyonları bağla - böylece HorizontalMovieItemsControl içinde lazım olduğunda
                // HandleItemsSourceChanged metodunun gereğinden fazla çağrılmasını engelleriz
                PopularMoviesControl.ItemsSource = null;
                TopRatedMoviesControl.ItemsSource = null;
                UpComingMoviesControl.ItemsSource = null;
                NowPlayingMoviesControl.ItemsSource = null;
                
                // 100ms bekle - UI'ın güncellenmesine izin ver
                Dispatcher.UIThread.RunJobs();
                
                // Şimdi boş koleksiyonları bağla ki, veri gelince anında gösterilsin
                PopularMoviesControl.ItemsSource = Service.PopularMovies;
                TopRatedMoviesControl.ItemsSource = Service.TopRatedMovies;
                UpComingMoviesControl.ItemsSource = Service.UpComingMovies;
                NowPlayingMoviesControl.ItemsSource = Service.NowPlayingMovies;
            });
            
            // Veri yükleme işlemini başlat
            await LoadMoviesInChunksOptimized();
        
            // DataContext'i güncelle
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.DataContext = null;
                this.DataContext = this;
            });
        }
        catch (System.Exception e)
        {
            var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
            Log.Error(errorMessage);
        }
    }
 
 
    public async Task LoadMoviesInChunksOptimized()
    {
        try
        {
            var categories = new List<string>()
            {
                "popular",
                "topRated",
                "upComing",
                "nowPlaying"
            };
            
            // İlk önce "popular" kategorisini yükle
            // Bu en önemli kategori olduğundan kullanıcıya hemen bir içerik göstermek istiyoruz
            await LoadMoviesChunk("popular", 1);
            
            // UI'ın tepki vermesi için kısa bir gecikme ekleyelim
            await Task.Delay(50);
            
            // Diğer kategorileri paralel olarak yükle
            // Burada Task.WhenAll yerine bir döngü kullanarak
            // her kategori arasında kısa bir gecikme ekleyelim
            // Bu UI'ın daha akıcı yanıt vermesini sağlayacak
            foreach (var category in categories.Where(c => c != "popular"))
            {
                // Her kategori arasında 100ms gecikme
                await Task.Delay(100);
                
                // Kategoriyi arka planda yükle
                _ = Task.Run(async () => 
                {
                    try 
                    {
                        await LoadMoviesChunk(category, 1);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Kategori yükleme hatası ({category}): {ex.Message}");
                    }
                });
            }
        }
        catch (System.Exception e)
        {
            var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
            Log.Error(errorMessage);
        }
    }
    
    
    private async Task LoadMoviesChunk(string category, int page)
    {
        try
        {
            // Her kategori yüklendiğinde UI'a bildir
            // await Dispatcher.UIThread.InvokeAsync(() => 
            // {
            //     UpdateLoadingMessage(category);
            // });
            
            var movieList = await GetMoviesChunk(page, category).ConfigureAwait(false);

            if (movieList != null && movieList.Results.Any())
            {
                // Liste üzerinde işlem yap - arka planda dönüştürme yap
                var movies = await Task.Run(() => movieList.Results
                    .Select(movie => CreateMovieFromSearchMovie(movie))
                    .ToList());
                
                // UI thread'de güncelleme yap
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var mov in movies)
                    {
                        AddMovieToCollection(category, mov);
                    }
                   
                    UpdateMovieControl(category);
                });
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
            Log.Error(errorMessage);
        }
    }
    
    // private void UpdateLoadingMessage(string category)
    // {
    //     string message = category switch
    //     {
    //         "popular" => "Popüler filmler yükleniyor...",
    //         "topRated" =>  "En çok oy alan filmler yükleniyor...",
    //         "upComing" => "Yakında gelecek filmler yükleniyor...",
    //         "nowPlaying" =>"Şimdi gösterimde olan filmler yükleniyor...",
    //         _ => ResourceProvider.GetString("LoadingString") ?? "Yükleniyor..."
    //     };
    //     TextBlockLoading.Text = message;
    // }
    
    private Movie CreateMovieFromSearchMovie(SearchMovie movie)
    {
        return new Movie()
        {
            Poster = (Service.client.GetImageUrl("w500", movie.PosterPath)?.AbsoluteUri ?? string.Empty),
            Id = movie.Id,
            Name = movie.Title,
            Rating = movie.VoteAverage,
            ShowType = ShowType.Movie,
        };
    }
    
    private void UpdateMovieControl(string category)
    {
        try
        {
            // UI thread'de olduğumuzdan emin olalım
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.InvokeAsync(() => UpdateMovieControl(category));
                return;
            }
            
            // ItemsSource zaten atanmışsa tekrar atamayalım, sadece koleksiyonu güncellemiş olalım
            // Bu yüzden bu metotta hiçbir şey yapmamıza gerek yok, zaten koleksiyonları güncelliyoruz
            // ve ItemsControl'ler bu koleksiyonlara zaten bağlı
            
            // Not: HorizontalMovieItemsControl'de ItemsSource'a bir ObservableCollection atadığımızda,
            // bu koleksiyona yapılan değişiklikler UI'da otomatik olarak gösterilir.
            // Bu nedenle, koleksiyonları temizleyip yeniden atamak yerine,
            // mevcut koleksiyona öğeleri eklemeyi tercih ediyoruz.
            
            // Ancak, koleksiyona öğeleri ekledikten sonra, UI'ın yanıt vermesini sağlamak için
            // RunJobs çağrısı yapabiliriz (opsiyonel)
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            NowPlayingMoviesControl.Margin = new Thickness(0,0,0,MainScroll.Extent.Height * 0.030);
        }
        catch (Exception ex)
        {
            Log.Error($"UpdateMovieControl hata: {ex.Message}");
        }
    }
    
    private void AddMovieToCollection(string category, Movie movie)
    {
        switch (category)
        {
            case "popular":
                if (!Service.PopularMovies.Any(x => x.Id == movie.Id)) Service.PopularMovies.Add(movie);
                break;
            case "topRated":
                if (!Service.TopRatedMovies.Any(x => x.Id == movie.Id)) Service.TopRatedMovies.Add(movie);
                break;
            case "upComing":
                if (!Service.UpComingMovies.Any(x => x.Id == movie.Id)) Service.UpComingMovies.Add(movie);
                break;
            case "nowPlaying":
                if (!Service.NowPlayingMovies.Any(x => x.Id == movie.Id)) Service.NowPlayingMovies.Add(movie);
                break;
        }
    }
    
    // API isteği için yeniden deneme parametreleri
    private const int MAX_RETRY_COUNT = 3;
    private const int RETRY_DELAY_MS = 1000;
    
    public async Task<TMDbLib.Objects.General.SearchContainer<TMDbLib.Objects.Search.SearchMovie>> GetMoviesChunk(int page, string category)
    {
        int retryCount = 0;
        
        while (retryCount < MAX_RETRY_COUNT)
        {
            try
            {
                // API isteği yapmadan önce kısa bir gecikme ekleyelim (rate limit'e takmamak için)
                if (retryCount > 0)
                {
                    await Task.Delay(RETRY_DELAY_MS * retryCount);
                }
                
                switch (category)
                {
                    case "popular":
                        return await Service.client.GetMoviePopularListAsync(Service.language, page).ConfigureAwait(false);
                    case "topRated":
                        return await Service.client.GetMovieTopRatedListAsync(Service.language, page).ConfigureAwait(false);
                    case "upComing":
                        return await Service.client.GetMovieUpcomingListAsync(Service.language, page).ConfigureAwait(false);
                    case "nowPlaying":
                        return await Service.client.GetMovieNowPlayingListAsync(Service.language, page).ConfigureAwait(false);
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                retryCount++;
                
                // Son denemede hata alırsak, hata mesajını loglayalım
                if (retryCount >= MAX_RETRY_COUNT)
                {
                    Log.Error($"Film verileri alınamadı (kategori: {category}, sayfa: {page}): {ex.Message}");
                    return null;
                }
                
                // API'dan hata aldık, yeniden deneyelim
                Log.Warning($"API isteği başarısız oldu (kategori: {category}, sayfa: {page}) - Yeniden deneme {retryCount}/{MAX_RETRY_COUNT}: {ex.Message}");
                
                // Hata fırlatmadan önce UI thread'e bilgi verelim
                // await Dispatcher.UIThread.InvokeAsync(() => 
                // {
                //     UpdateLoadingMessage($"API hatası! Yeniden deneme {retryCount}/{MAX_RETRY_COUNT}...");
                // });
            }
        }
        
        return null;
    }
   
    
    
    
    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            previousWidth = this.Bounds.Width;
            isSmallScreen = previousWidth <= SMALL_SCREEN_THRESHOLD;
            isExtraSmallScreen = previousWidth <= EXTRA_SMALL_SCREEN_THRESHOLD;
            
            ApplyResponsiveLayout(previousWidth);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Home_OnLoaded Exception: {ex.Message}");
        }
    }

    private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
    {
        try
        {
            // Get the current width
            double width = e.width;
            
            if (width != previousWidth)
            {
                previousWidth = width;
            
                // Determine screen size category for responsive design
                isSmallScreen = width <= SMALL_SCREEN_THRESHOLD;
                isExtraSmallScreen = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
            
                // Apply responsive layout based on screen size
                ApplyResponsiveLayout(width);
                // Scroll butonlarının konumunu ayarla
               
                // Dinamik film öğelerinin stillerini ayarla
                UpdateLayoutBasedOnSize(width);
            }
        }
        catch (Exception exception)
        {
            var errorMessage = $"Error in Control_OnSizeChanged: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

   
    
  
        // Add fields for responsiveness
        private double previousWidth = 0;
        private bool isSmallScreen = false;
        private bool isExtraSmallScreen = false;

        // Threshold for small screen detection
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

        // Herhangi bir öğe için ölçeklenebilir değer hesaplama metodu
        private double CalculateScaledValue(double width, double minValue, double maxValue)
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
        
        // Kart genişliği için ölçekleme

        private double cardWidth;
        private double CalculateCardWidth(double width)
        {
            cardWidth = CalculateScaledValue(width, 120, 400);
            return cardWidth;
        }
        
        // Kart yüksekliği için ölçekleme
        private double CalculateCardHeight(double width)
        {
            return CalculateScaledValue(width, 210, 600);
        }
        
        // Metin boyutu için ölçekleme
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }
  
    
    
    
   
    
     
    
     private void UpdateLayoutBasedOnSize(double width)
        {
            try
            {
                // Ekran genişliğini maksimum 3840px ile sınırla
                double clampedWidth = Math.Min(width, 3840);
                
                bool isSmallScreen = clampedWidth <= SMALL_SCREEN_THRESHOLD;
                bool isExtraSmallScreen = clampedWidth <= EXTRA_SMALL_SCREEN_THRESHOLD;
                
                // Record screen state changes
                bool screenSizeChanged = this.isSmallScreen != isSmallScreen;
                
                this.isSmallScreen = isSmallScreen;
                this.isExtraSmallScreen = isExtraSmallScreen;
                
                // Ana başlık boyutları için ölçeklendirme değerleri
                double headerFontSize = CalculateTextSize(clampedWidth, 18, 36);
                
                // WrapPanel'daki öğelerin boyutları için ölçeklendirme
                double viewboxMaxHeight = CalculateScaledValue(clampedWidth, 16, 40);
                double ellipseSize = viewboxMaxHeight * 0.2;
                
                
                // Başlıkları boyutlandır ve marjinleri ayarla
                var headerMargin = CalculateScaledValue(clampedWidth, 20, 100);
                double commonLeftMargin = isSmallScreen ? 20 : CalculateScaledValue(clampedWidth, 40, 80); 

                // Configure section titles - using the correct element names from XAML
                if (PopularMoviesTitle != null)
                {
                    PopularMoviesTitle.FontSize = headerFontSize;
                    
                    if (PopularMoviesHeaderGrid != null)
                    {
                        PopularMoviesHeaderGrid.Margin = new Thickness(commonLeftMargin, 20, 20, 0);
                    }
                }
                
                
                if (TopRatedTitle != null)
                {
                    TopRatedTitle.FontSize = headerFontSize;
                    
                    if (TopRatedHeaderGrid != null)
                    {
                        TopRatedHeaderGrid.Margin = new Thickness(commonLeftMargin, 20, 20, 0);
                    }
                }
                
                if (UpComingMoviesTitle != null)
                {
                    UpComingMoviesTitle.FontSize = headerFontSize;
                    
                    if (UpcomingMoviesHeaderGrid != null)
                    {
                        UpcomingMoviesHeaderGrid.Margin = new Thickness(commonLeftMargin, 20, 20, 0);
                    }
                }
                
                if (NowPlayingMoviesTitle != null)
                {
                    NowPlayingMoviesTitle.FontSize = headerFontSize;
                    
                    if (NowPlayingMoviesHeaderGrid != null)
                    {
                        NowPlayingMoviesHeaderGrid.Margin = new Thickness(commonLeftMargin, 20, 20, 0);
                    }
                }
                
                
                // Configure main movie info
                if (MainMovieTvShow != null)
                {
                    if (screenSizeChanged || Math.Abs(clampedWidth - previousWidth) > 50)
                    {
                        double mainMovieMargin = CalculateScaledValue(clampedWidth, 10, 100);
                        
                        if (isSmallScreen)
                        {
                            MainMovieTvShow.Margin = new Thickness(10);
                        }
                        else
                        {
                            MainMovieTvShow.Margin = new Thickness(mainMovieMargin, 20, mainMovieMargin, 20);
                        }
                    }
                }
                
                
                // RatingBar yıldız boyutunu güncelle
                var ratingControl = this.FindControl<RatingBar>("RatingControl");
                if (ratingControl != null)
                {
                    double starSize = CalculateScaledValue(clampedWidth, 12, 30);
                    ratingControl.StarSize = starSize;
                }
                
                // Scroll butonlarını ayarla
              
                
                // NowPlayingMoviesDisplay margin'lerini ayarla
               
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateLayoutBasedOnSize Exception: {ex.Message}");
            }
        }
     
      
    
    private void ApplyResponsiveLayout(double width)
    {
        try
        {
            UpdateLayoutBasedOnSize(width);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ApplyResponsiveLayout Exception: {ex.Message}");
        }
    }
    
    

    private void ExploreMoreTopRatedMovies_OnMouseEnter(object? sender, PointerEventArgs e)
    {
        exploreMoreTopRatedMovies.Foreground = new SolidColorBrush(App.Current.Resources["ColorDefault"] as Color? ?? Colors.Blue);
    }

    private void ExploreMoreTopRatedMovies_OnMouseLeave(object? sender, PointerEventArgs e)
    {
        exploreMoreTopRatedMovies.Foreground = new SolidColorBrush(Color.Parse("#828282"));
    }

    

    public void Dispose()
    {
        try
        {
            foreach (var visualDescendant in PopularMoviesControl.ItemsDisplay.GetVisualDescendants())
            {
                if (visualDescendant is AsyncImageControl asyncImageControl)
                {
                    asyncImageControl.Dispose();
                }
            }
            foreach (var visualDescendant in TopRatedMoviesControl.ItemsDisplay.GetVisualDescendants())
            {
                if (visualDescendant is AsyncImageControl asyncImageControl)
                {
                    asyncImageControl.Dispose();
                }
            }
            foreach (var visualDescendant in UpComingMoviesControl.ItemsDisplay.GetVisualDescendants())
            {
                if (visualDescendant is AsyncImageControl asyncImageControl)
                {
                    asyncImageControl.Dispose();
                }
            }
            foreach (var visualDescendant in NowPlayingMoviesControl.ItemsDisplay.GetVisualDescendants())
            {
                if (visualDescendant is AsyncImageControl asyncImageControl)
                {
                    asyncImageControl.Dispose();
                }
            }
          
            
            this.DataContext = null;
            this.MainMovieTvShow.DataContext = null;
            
            if (TopRatedMoviesControl != null) TopRatedMoviesControl.ItemsSource = null;
            if (UpComingMoviesControl != null) UpComingMoviesControl.ItemsSource = null;
            if (NowPlayingMoviesControl != null) NowPlayingMoviesControl.ItemsSource = null;
            if (PopularMoviesControl != null) PopularMoviesControl.ItemsSource = null;
            
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
            
            // Bellek temizliği
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        catch (Exception ex)
        {
            Log.Error($"Dispose işlemi sırasında hata oluştu: {ex.Message}");
        }
        
        GC.SuppressFinalize(this);
    }

   

    private void ExploreMorePopularMovies_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            var textBlock = sender as TextBlock;
            if(textBlock == null) return;
            if (textBlock.Name == "exploreMoreNowPlayingMovies")
            {
                MainView.Instance.SetContent(new ExploreMorePage(Service.NowPlayingMovies, ExploreMore.Movie,
                    ExploreMoreList.NowPlaying, ResourceProvider.GetString("NowPlayingMoviesString")));
            }
            else if (textBlock.Name == "exploreMoreUpcomingMovies")
            {
                MainView.Instance.SetContent(new ExploreMorePage(Service.UpComingMovies, ExploreMore.Movie,
                    ExploreMoreList.UpComing, ResourceProvider.GetString("UpcomingMoviesString")));
            }
            else if (textBlock.Name == "exploreMoreTopRatedMovies")
            {
                MainView.Instance.SetContent(new ExploreMorePage(Service.TopRatedMovies, ExploreMore.Movie,
                    ExploreMoreList.TopRated, ResourceProvider.GetString("TopRatedMoviesString")));
            }
            else if (textBlock.Name == "exploreMorePopularMovies")
            {
                MainView.Instance.SetContent(new ExploreMorePage(Service.PopularMovies, ExploreMore.Movie,
                    ExploreMoreList.Popular, ResourceProvider.GetString("PopularMoviesString")));
            }
        }
        catch (Exception exception)
        {
            var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
            Log.Error(errorMessage);
        }
    }

    private void ExploreMorePopularMovies_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        exploreMorePopularMovies.Foreground = new SolidColorBrush(App.Current.Resources["ColorDefault"] as Color? ?? Colors.Blue);
    }

    private void ExploreMorePopularMovies_OnPointerExited(object? sender, PointerEventArgs e)
    {
        exploreMorePopularMovies.Foreground = new SolidColorBrush(Color.Parse("#828282"));
    }
    
    private void ExploreMoreUpcomingMovies_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        exploreMoreUpcomingMovies.Foreground = new SolidColorBrush(App.Current.Resources["ColorDefault"] as Color? ?? Colors.Blue);
    }

    private void ExploreMoreUpcomingMovies_OnPointerExited(object? sender, PointerEventArgs e)
    {
        exploreMoreUpcomingMovies.Foreground = new SolidColorBrush(Color.Parse("#828282"));
    }
    
    private void ExploreMoreNowPlayingMovies_OnPointerExited(object? sender, PointerEventArgs e)
    {
        exploreMoreNowPlayingMovies.Foreground = new SolidColorBrush(Color.Parse("#828282"));
    }

    private void ExploreMoreNowPlayingMovies_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        exploreMoreNowPlayingMovies.Foreground = new SolidColorBrush(App.Current.Resources["ColorDefault"] as Color? ?? Colors.Blue);
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
    }

    private async Task CheckAndInitializeCollections()
    {
        // Koleksiyonlar null veya boş ise başlat
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Service koleksiyonlarının null olup olmadığını kontrol et
            // PopularMovies'i daha önce başlatılmamışsa başlat
            if (Service.PopularMovies == null)
            {
                Service.PopularMovies = new ObservableCollection<Movie>();
            }
            
            // TopRatedMovies'i daha önce başlatılmamışsa başlat
            if (Service.TopRatedMovies == null)
            {
                Service.TopRatedMovies = new ObservableCollection<Movie>();
            }
            
            // UpComingMovies'i daha önce başlatılmamışsa başlat
            if (Service.UpComingMovies == null)
            {
                Service.UpComingMovies = new ObservableCollection<Movie>();
            }
            
            // NowPlayingMovies'i daha önce başlatılmamışsa başlat
            if (Service.NowPlayingMovies == null)
            {
                Service.NowPlayingMovies = new ObservableCollection<Movie>();
            }
        });
    }

    private void MainScroll_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
    }
}