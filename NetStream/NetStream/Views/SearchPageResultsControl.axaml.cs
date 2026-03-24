using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetStream.Views;
using Serilog;

namespace NetStream
{
    public partial class SearchPageResultsControl : UserControl, INotifyPropertyChanged, IDisposable
    {
        public string searchKey = "";
        private SearchType searchType;

        private int searchMoviesPage = 1;
        private int searchTvShowsPage = 1;
        private int searchPersonPage = 1;

        public int TotalResultMovies = 0;
        public int TotalResultTvShows = 0;
        public int TotalResultPersons = 0;
        
        // Responsive tasarım için değişkenler
        private double previousWidth = 0;
        private bool isSmallScreen = false;
        private bool isExtraSmallScreen = false;
        
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;

        public SearchPageResultsControl()
        {
            InitializeComponent();
            DataContext = this;
            searchType = SearchType.Movie;
            
            // SizeChanged olayına metot bağla
        }
        
        
        public void AdjustItemSizes(double width)
        {
            try
            {
                // Movies/Home sayfaları ile aynı ölçeklendirme değerlerini kullan
                const double minWidth = 320;   // En küçük ekran genişliği
                const double maxWidth = 3840;  // En büyük ekran genişliği
                
                const double minTitleFontSize = 12;
                const double maxTitleFontSize = 18;
                
                // Sekme başlıkları için font boyutu değerleri
                const double minTabFontSize = 14;
                const double maxTabFontSize = 22;
                
                // Header için font boyutu değerleri
                const double minHeaderFontSize = 12;
                const double maxHeaderFontSize = 18;
                
                
                // Ekran genişliğini sınırla
                double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
                
                // Doğrusal ölçeklendirme için ölçek faktörü
                double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
                
                // Font boyutları
                double titleFontSize = minTitleFontSize + scale * (maxTitleFontSize - minTitleFontSize);
                double tabFontSize = minTabFontSize + scale * (maxTabFontSize - minTabFontSize);
                double headerFontSize = minHeaderFontSize + scale * (maxHeaderFontSize - minHeaderFontSize);
                
                // Yıldız boyutu - MoviesPage ile aynı
                
              
                titleFontSize = Math.Round(titleFontSize);
                tabFontSize = Math.Round(tabFontSize);
                headerFontSize = Math.Round(headerFontSize);
                
                // Sekme başlıklarını güncelle
                if (MoviesTab != null) MoviesTab.FontSize = tabFontSize;
                if (TvShowsTab != null) TvShowsTab.FontSize = tabFontSize;
                if (PersonsTab != null) PersonsTab.FontSize = tabFontSize;
                
                // Tab boşluklarını ekran boyutuna göre ayarla
                AdjustTabMargins(width);
                
                // Arama sonuçları başlığını güncelle
                AdjustSearchKeyTextSize(width);
                if (ItemCountText != null) ItemCountText.FontSize = headerFontSize;
                
                ApplyItemStylesForMovieElements(width);
                ApplyItemStylesForCastElements(width);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustItemSizes: {ex.Message}");
            }
        }

        public void AdjustItemSizes()
        {
            try
            {
                // Movies/Home sayfaları ile aynı ölçeklendirme değerlerini kullan
                const double minWidth = 320;   // En küçük ekran genişliği
                const double maxWidth = 3840;  // En büyük ekran genişliği
                
                const double minTitleFontSize = 12;
                const double maxTitleFontSize = 18;
                
                // Sekme başlıkları için font boyutu değerleri
                const double minTabFontSize = 14;
                const double maxTabFontSize = 22;
                
                // Header için font boyutu değerleri
                const double minHeaderFontSize = 12;
                const double maxHeaderFontSize = 18;
                
                
                // Ekran genişliğini sınırla
                double clampedWidth = Math.Max(minWidth, Math.Min(this.Bounds.Width, maxWidth));
                
                // Doğrusal ölçeklendirme için ölçek faktörü
                double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
                
                // Font boyutları
                double titleFontSize = minTitleFontSize + scale * (maxTitleFontSize - minTitleFontSize);
                double tabFontSize = minTabFontSize + scale * (maxTabFontSize - minTabFontSize);
                double headerFontSize = minHeaderFontSize + scale * (maxHeaderFontSize - minHeaderFontSize);
                
                // Yıldız boyutu - MoviesPage ile aynı
                
              
                titleFontSize = Math.Round(titleFontSize);
                tabFontSize = Math.Round(tabFontSize);
                headerFontSize = Math.Round(headerFontSize);
                
                // Sekme başlıklarını güncelle
                if (MoviesTab != null) MoviesTab.FontSize = tabFontSize;
                if (TvShowsTab != null) TvShowsTab.FontSize = tabFontSize;
                if (PersonsTab != null) PersonsTab.FontSize = tabFontSize;
                
                // Arama sonuçları başlığını güncelle
                AdjustSearchKeyTextSize(this.Bounds.Width);
                if (ItemCountText != null) ItemCountText.FontSize = headerFontSize;
                
                ApplyItemStylesForMovieElements(this.Bounds.Width);
                ApplyItemStylesForCastElements(this.Bounds.Width);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustItemSizes: {ex.Message}");
            }
        }
        
        private double CalculateCardHeight(double width)
        {
            return CalculateScaledValue(width, 210, 600);
        }
        
        private double CalculateCardWidth(double width)
        {
            return CalculateScaledValue(width, 140, 400);
        }
        
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
        
        private void ApplyItemStylesForMovieElements(double width)
        {
            try
            {
                bool isExtraSmall = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                bool isSmall = width <= SMALL_SCREEN_THRESHOLD && !isExtraSmall;
            
                // Kart boyutlarını hesapla
                double cardWidth = CalculateCardWidth(width);
                double cardHeight = CalculateCardHeight(width);
            
                // Font boyutlarını hesapla
                double titleFontSize = CalculateTextSize(width, 12, 24);
                double ratingFontSize = CalculateTextSize(width, 10, 18);
                double starSize = CalculateTextSize(width, 15, 30);
                
                foreach (var child in SearchedMoviesTvShowsDisplay.GetVisualDescendants())
                {
                    if (child is Border border && border.Name == "MovieBorder")
                    {
                        border.Width = cardWidth;
                        border.Height = cardHeight;
                        border.Classes.Clear();
                        if (isExtraSmall)
                            border.Classes.Add("movie-card-small");
                        else if (isSmall)
                            border.Classes.Add("movie-card-medium");
                        else
                            border.Classes.Add("movie-card");
                    }
                    else if (child is TextBlock textBlock)
                    {
                        if (textBlock.Name == "MovieTitle")
                        {
                            textBlock.Classes.Clear();
                    
                            // Ölçeklenmiş metin boyutunu ayarla
                            textBlock.FontSize = titleFontSize;
                            textBlock.MaxWidth = cardWidth * 0.8; // Kart genişliğine göre oran
                    
                            if (isExtraSmall)
                                textBlock.Classes.Add("movie-title-small");
                            else if (isSmall)
                                textBlock.Classes.Add("movie-title-medium");
                            else
                                textBlock.Classes.Add("movie-title");
                        }
                        else if (textBlock.Name == "RatingText")
                        {
                            textBlock.FontSize = ratingFontSize;
                    
                            double leftMargin = cardWidth * 0.06;
                            textBlock.Margin = new Thickness(leftMargin, 3.5, 0, 0);
                        }
                    }
                    else if (child is Controls.RatingBar ratingBar && ratingBar.Name == "MovieRating")
                    {
                        ratingBar.StarSize = starSize;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ApplyItemStylesForMovieElements: {ex.Message}");
            }
        }
        
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }
        
        private void ApplyItemStylesForCastElements(double width)
        {
            try
            {
                bool isExtraSmall = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                bool isSmall = width <= SMALL_SCREEN_THRESHOLD && !isExtraSmall;
            
                // Kart boyutlarını hesapla
                double cardWidth = CalculateCardWidth(width);
                double cardHeight = CalculateCardHeight(width);
            
                // Font boyutlarını hesapla
                double titleFontSize = CalculateTextSize(width, 12, 24);
                double ratingFontSize = CalculateTextSize(width, 10, 18);
                
                foreach (var child in SearchedCastsDisplay.GetVisualDescendants())
                {
                    if (child is Border border && border.Name == "CastBorder")
                    {
                        border.Width = cardWidth;
                        border.Height = cardHeight;
                                
                        border.Classes.Clear();
                        if (isExtraSmall)
                            border.Classes.Add("movie-card-small");
                        else if (isSmall)
                            border.Classes.Add("movie-card-medium");
                        else
                            border.Classes.Add("movie-card");
                    }
                    else if (child is TextBlock textBlock)
                    {
                        if (textBlock.Name == "CastName")
                        {
                            textBlock.Classes.Clear();
                    
                            textBlock.FontSize = titleFontSize;
                            textBlock.MaxWidth = cardWidth * 0.8; // Kart genişliğine göre oran
                    
                            if (isExtraSmall)
                                textBlock.Classes.Add("movie-title-small");
                            else if (isSmall)
                                textBlock.Classes.Add("movie-title-medium");
                            else
                                textBlock.Classes.Add("movie-title");
                        }
                        else if (textBlock.Name == "CastRole")
                        {
                            textBlock.FontSize = ratingFontSize;
                            textBlock.MaxWidth = cardWidth * 0.8;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ApplyItemStylesForCastElements: {ex.Message}");
            }
        }

        public string SearchKey
        {
            get
            {
                return "Search result for: " + searchKey;
            }
            set
            {
                searchKey = value;
                NotifyPropertyChanged("SearchKey");
            }
        }

        

        private async void MoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                var scrollViewer = (ScrollViewer)sender;
                if (scrollViewer.Offset.Y >= scrollViewer.Extent.Height - scrollViewer.Viewport.Height)
                {
                    switch (searchType)
                    {
                        case SearchType.Movie:
                            searchMoviesPage++;
                            await Service.GetMorePagesSearchedMovies(searchKey, searchMoviesPage);
                            // Yeni öğeler eklendiğinde boyutları güncelle - gecikmeli ve daha güvenilir
                            ScheduledRefreshItemSizes(false);
                            break;
                        case SearchType.TvShow:
                            searchTvShowsPage++;
                            await Service.GetMorePagesSearchedTvShows(searchKey, searchTvShowsPage);
                            // Yeni öğeler eklendiğinde boyutları güncelle - gecikmeli ve daha güvenilir
                            ScheduledRefreshItemSizes(false);
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error($"Error in MoviesDisplay_OnScrollChanged: {exception.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        DebounceHelper debounceHelper = new DebounceHelper();

        private async void FrameworkElement_OnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            try
            {
                if (e.Property == TextBlock.TextProperty)
                {
                    await debounceHelper.DebouncedAction(async () =>
                    {
                        switch (searchType)
                        {
                            case SearchType.Movie:
                                Service.SearchedTvShowsResult.Clear();
                                Service.SearchedCastsResult.Clear();
                                SearchedMoviesTvShowsDisplay.ItemsSource = Service.SearchedMoviesResult;
                                await Service.GetSearchedMovies(searchKey, 1, this);
                                // Ölçeklendirmeyi zorla - eski yöntem
                                // ForceRefreshItemSizes();
                                // Gecikmeli ve çoklu güncelleme ile daha güvenilir boyutlandırma
                                ScheduledRefreshItemSizes(false);
                                break;
                            case SearchType.TvShow:
                                Service.SearchedMoviesResult.Clear();
                                Service.SearchedCastsResult.Clear();
                                SearchedMoviesTvShowsDisplay.ItemsSource = Service.SearchedTvShowsResult;
                                await Service.GetSearchedTvShows(searchKey, 1, this);
                                // Ölçeklendirmeyi zorla - eski yöntem
                                // ForceRefreshItemSizes();
                                // Gecikmeli ve çoklu güncelleme ile daha güvenilir boyutlandırma
                                ScheduledRefreshItemSizes(false);
                                break;
                            case SearchType.Person:
                                Service.SearchedMoviesResult.Clear();
                                Service.SearchedTvShowsResult.Clear();
                                SearchedCastsDisplay.ItemsSource = Service.SearchedCastsResult;
                                await Service.GetSearchedCasts(searchKey, 1, this);
                                // Ölçeklendirmeyi zorla - eski yöntem
                                // ForceRefreshItemSizes();
                                // Gecikmeli ve çoklu güncelleme ile daha güvenilir boyutlandırma
                                ScheduledRefreshItemSizes(true);
                                break;
                        }
                    }, 300);
                }
            }
            catch (Exception exception)
            {
                Log.Error($"Error in FrameworkElement_OnPropertyChanged: {exception.Message}");
            }
        }

        private void SearchPageResults_OnLoaded(object sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged+= InstanceOnSizeChanged;
            // Yükleme sonrası responsive ayarları güncelle
            AdjustItemSizes(this.Bounds.Width);
            
            // ItemsControl PropertyChanged olayına abone ol
            SearchedMoviesTvShowsDisplay.PropertyChanged += (s, args) => 
            {
                if (args.Property.Name == "ItemsSource" && SearchedMoviesTvShowsDisplay.ItemsSource != null)
                {
                    // ItemsSource değiştiğinde boyutları ayarla
                    Dispatcher.UIThread.Post(() => AdjustItemSizes());
                }
            };
            
            SearchedCastsDisplay.PropertyChanged += (s, args) => 
            {
                if (args.Property.Name == "ItemsSource" && SearchedCastsDisplay.ItemsSource != null)
                {
                    // ItemsSource değiştiğinde boyutları ayarla
                    Dispatcher.UIThread.Post(() => AdjustItemSizes());
                }
            };
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            try
            {
                // Mevcut genişliği al
                double width = e.width;
                if (width != previousWidth)
                {
                    previousWidth = width;
                    
                    // Ekran boyutuna göre kategoriyi belirle
                    isSmallScreen = width <= SMALL_SCREEN_THRESHOLD;
                    isExtraSmallScreen = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                    
                    // Item elemanlarının boyutunu ayarla
                    AdjustItemSizes(width);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error in SearchPageResultsControl_OnSizeChanged: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        /*private void SearchedCastsDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Cast selectedCast = (Cast)SearchedCastsDisplay.SelectedItem;
                if (selectedCast != null)
                {
                    // In Avalonia, we'd need to use the appropriate navigation method
                    // HomePage.NavigationService.Navigate(new CastPage(selectedCast));
                }
            }
            catch (Exception exception)
            {
                // Log error using the appropriate logging mechanism
            }
        }*/

        private async void SearchedCastsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                var scrollViewer = (ScrollViewer)e.Source;
                if (scrollViewer.Offset.Y >= scrollViewer.Extent.Height - scrollViewer.Viewport.Height)
                {
                    searchPersonPage++;
                    await Service.GetMorePagesSearchedCasts(searchKey, searchPersonPage);
                    // Yeni öğeler eklendiğinde boyutları güncelle - gecikmeli ve daha güvenilir
                    ScheduledRefreshItemSizes(true);
                }
            }
            catch (Exception exception)
            {
                Log.Error($"Error in SearchedCastsDisplay_OnScrollChanged: {exception.Message}");
            }
        }

        private async void MoviesTab_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                SearchedMoviesTvShowsDisplay.ItemsSource = Service.SearchedMoviesResult;
                if (Service.SearchedMoviesResult.Count == 0)
                {
                    await Service.GetSearchedMovies(searchKey, 1, this);
                }
                
                searchType = SearchType.Movie;
                
                // Boyutları hemen güncelle
                ApplyItemStylesForMovieElements(this.Bounds.Width);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ItemCountText.Text = TotalResultMovies + " items";
                    SearchedMoviesTvShowsDisplayScroll.IsVisible = true;
                    SearchedCastsDisplayScrollViewer.IsVisible = false;
                    Grid.SetColumn(MenuLine, 0);
                    MoviesTab.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    TvShowsTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    PersonsTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                });
                
                // Render sonrası çoklu boyut güncellemeleri yaparak tüm öğelerin doğru boyutlarda olmasını sağla
                ScheduledRefreshItemSizes(false);
            }
            catch (Exception exception)
            {
                Log.Error($"Error in MoviesTab_OnPreviewMouseLeftButtonDown: {exception.Message}");
            }
        }

        private async void TvShowsTab_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                SearchedMoviesTvShowsDisplay.ItemsSource = Service.SearchedTvShowsResult;
                if (Service.SearchedTvShowsResult.Count == 0)
                {
                    await Service.GetSearchedTvShows(searchKey, 1, this);
                }
                
                searchType = SearchType.TvShow;
                
                // Boyutları hemen güncelle
                ApplyItemStylesForMovieElements(this.Bounds.Width);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ItemCountText.Text = TotalResultTvShows + " items";
                    SearchedMoviesTvShowsDisplayScroll.IsVisible = true;
                    SearchedCastsDisplayScrollViewer.IsVisible = false;
                    Grid.SetColumn(MenuLine, 1);
                    MoviesTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    TvShowsTab.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    PersonsTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                });
                
                // Render sonrası çoklu boyut güncellemeleri yaparak tüm öğelerin doğru boyutlarda olmasını sağla
                ScheduledRefreshItemSizes(false);
            }
            catch (Exception exception)
            {
                Log.Error($"Error in TvShowsTab_OnPreviewMouseLeftButtonDown: {exception.Message}");
            }
        }

        private async void PersonsTab_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                SearchedCastsDisplay.ItemsSource = Service.SearchedCastsResult;
                if (Service.SearchedCastsResult.Count == 0)
                {
                    await Service.GetSearchedCasts(searchKey, 1, this);
                }
                
                searchType = SearchType.Person;
                
                // Boyutları hemen güncelle
                ApplyItemStylesForCastElements(this.Bounds.Width);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ItemCountText.Text = TotalResultPersons + " items";
                    SearchedMoviesTvShowsDisplayScroll.IsVisible = false;
                    SearchedCastsDisplayScrollViewer.IsVisible = true;
                    Grid.SetColumn(MenuLine, 2);
                    MoviesTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    TvShowsTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    PersonsTab.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                });
                
                // Render sonrası çoklu boyut güncellemeleri yaparak tüm öğelerin doğru boyutlarda olmasını sağla
                ScheduledRefreshItemSizes(true);
            }
            catch (Exception exception)
            {
                Log.Error($"Error in PersonsTab_OnPreviewMouseLeftButtonDown: {exception.Message}");
            }
        }

        private void SearchPageResults_OnUnloaded(object sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }

        public void Dispose()
        {
            Service.SearchedMoviesResult.Clear();
            Service.SearchedTvShowsResult.Clear();
            Service.SearchedCastsResult.Clear();
            
            // Event'leri kaldır
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var stackPanel = sender as StackPanel;
                if(stackPanel == null) return;
                var selectedMov = stackPanel.DataContext as Movie;
                if (selectedMov != null)
                {
                    var movieDetailsPage = new MovieDetailsPage(selectedMov);
                    MainView.Instance.SetContent(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }     
        }
   
        private void CastDisplayItemOnPressed(object? sender, PointerPressedEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if(stackPanel == null) return;
            var cast = stackPanel.DataContext as Cast;
            if (cast != null)
            {
                var castPage = new CastPageControl(cast);
                MainView.Instance.SetContent(castPage);
            }
        }

        // Bu metot, veriler yüklendikten hemen sonra öğelerin boyutlarını düzeltmek için
        public void ForceRefreshItemSizes()
        {
            // UI Thread'inde çalıştır
            Dispatcher.UIThread.Post(() => 
            {
                ApplyItemStylesForMovieElements(this.Bounds.Width);
                ApplyItemStylesForCastElements(this.Bounds.Width);
            });
        }

        // Ekstra gecikme ile boyutlarını değiştirmek için metot
        private async void ScheduledRefreshItemSizes(bool isCastMode = false)
        {
            try 
            {
                // İlk güncelleme
                Dispatcher.UIThread.Post(() => 
                {
                    if (isCastMode)
                        ApplyItemStylesForCastElements(this.Bounds.Width);
                    else
                        ApplyItemStylesForMovieElements(this.Bounds.Width);
                });
                
                // Kısa gecikme sonrası tekrar güncelle - ItemsControl yeni öğeleri render ettikten sonra
                await Task.Delay(50);
                Dispatcher.UIThread.Post(() => 
                {
                    if (isCastMode)
                        ApplyItemStylesForCastElements(this.Bounds.Width);
                    else
                        ApplyItemStylesForMovieElements(this.Bounds.Width);
                });
                
                // Daha uzun gecikme sonrası son bir güncelleme yap
                await Task.Delay(200);
                Dispatcher.UIThread.Post(() => 
                {
                    if (isCastMode)
                        ApplyItemStylesForCastElements(this.Bounds.Width);
                    else
                        ApplyItemStylesForMovieElements(this.Bounds.Width);
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ScheduledRefreshItemSizes: {ex.Message}");
            }
        }

        // Tab'lar arasındaki boşluğu ekran boyutuna göre ayarla
        private void AdjustTabMargins(double width)
        {
            try
            {
                // Viewbox nesnelerini bul
                var moviesViewbox = this.FindControl<Viewbox>("MoviesTab")?.Parent as Viewbox;
                var tvShowsViewbox = this.FindControl<Viewbox>("TvShowsTab")?.Parent as Viewbox;
                var personsViewbox = this.FindControl<Viewbox>("PersonsTab")?.Parent as Viewbox;
                
                // TextBlock nesnelerini bul
                var moviesTab = this.FindControl<TextBlock>("MoviesTab");
                var tvShowsTab = this.FindControl<TextBlock>("TvShowsTab");
                var personsTab = this.FindControl<TextBlock>("PersonsTab");
                
                // TabsPanel Grid nesnesini bul
                var tabsPanel = this.FindControl<Grid>("TabsPanel");
                
                if (moviesViewbox == null || tvShowsViewbox == null || personsViewbox == null || 
                    moviesTab == null || tvShowsTab == null || personsTab == null)
                    return;
                
                // Ekran genişliğine göre margin değerleri hesapla
                double viewboxMargin = 0;
                double textBlockMargin = 0;
                
                if (width <= 400) // Çok küçük ekranlar
                {
                    viewboxMargin = 4;
                    textBlockMargin = 4;
                    
                    // Çok küçük ekranlarda Tab yazıları kısaltılabilir
                   
                }
                else if (width <= 600) // Küçük ekranlar
                {
                    viewboxMargin = 6;
                    textBlockMargin = 5;
                    
                    // Normal metinleri geri yükle
                   
                }
                else if (width <= 900) // Orta boy ekranlar
                {
                    viewboxMargin = 8;
                    textBlockMargin = 8;
                }
                else // Büyük ekranlar
                {
                    viewboxMargin = 10;
                    textBlockMargin = 12;
                }
                
                // Margin değerlerini uygula
                moviesViewbox.Margin = new Thickness(viewboxMargin, 0, viewboxMargin, 0);
                tvShowsViewbox.Margin = new Thickness(viewboxMargin, 0, viewboxMargin, 0);
                personsViewbox.Margin = new Thickness(viewboxMargin, 0, viewboxMargin, 0);
                
                moviesTab.Margin = new Thickness(textBlockMargin, 0, textBlockMargin, 0);
                tvShowsTab.Margin = new Thickness(textBlockMargin, 0, textBlockMargin, 0);
                personsTab.Margin = new Thickness(textBlockMargin, 0, textBlockMargin, 0);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustTabMargins: {ex.Message}");
            }
        }

        // SearchKeyText boyutunu ekran genişliğine göre ayarla
        private void AdjustSearchKeyTextSize(double width)
        {
            try
            {
                if (SearchKeyText == null) return;
                
                // Ekran genişliği eşik değerleri
                const double minWidth = 320;
                const double maxWidth = 3840;
                
                // Font boyutları (daha küçük değerlerle, küçük ekranlara uygun)
                const double minSearchTitleFontSize = 18;
                const double maxSearchTitleFontSize = 34;
                
                // Ekran genişliğini sınırla
                double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
                
                // Doğrusal ölçeklendirme
                double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
                double fontSize = minSearchTitleFontSize + scale * (maxSearchTitleFontSize - minSearchTitleFontSize);
                
                // Kesinlik için yuvarlama
                fontSize = Math.Round(fontSize);
                
                // Font boyutunu güncelle
                SearchKeyText.FontSize = fontSize;
                
                // MaxWidth değerini ekran genişliğine göre ayarla
                SearchKeyText.MaxWidth = Math.Min(width * 0.8, 800);
                
                // İlave büyük ekranlarda farklı margin ve font kalınlığı ayarla
                if (width <= 450)
                {
                    // Çok küçük ekranlar için
                   // SearchKeyText.Margin = new Thickness(5, 0, 5, 0);
                    SearchKeyText.FontWeight = FontWeight.Normal;
                }
                else if (width <= 750)
                {
                    // Küçük ekranlar için
                    //SearchKeyText.Margin = new Thickness(10, 0, 10, 0);
                    SearchKeyText.FontWeight = FontWeight.Normal;
                }
                else
                {
                    // Büyük ekranlar için
                    //SearchKeyText.Margin = new Thickness(10, 0, 10, 0);
                    SearchKeyText.FontWeight = FontWeight.Normal;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AdjustSearchKeyTextSize: {ex.Message}");
            }
        }
    }
    
    public enum SearchType
    {
        Movie,
        TvShow,
        Person
    }
    
    public class DebounceHelper
    {
        private CancellationTokenSource _debounceCancellationTokenSource;

        public async Task DebouncedAction(Func<Task> action, int delayMilliseconds)
        {
            _debounceCancellationTokenSource?.Cancel();  // Önceki görevi iptal et
            _debounceCancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Bekleyip sonra çağır
                await Task.Delay(delayMilliseconds, _debounceCancellationTokenSource.Token);
                await action(); // Gerçek eylemi gerçekleştir
            }
            catch (TaskCanceledException)
            {
                // Eğer işlem iptal edilirse burada herhangi bir şey yapmayabiliriz.
            }
        }
    }
} 