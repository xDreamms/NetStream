using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.VisualTree;
using NetStream.Navigation;
using NetStream.Views;
using TMDbLib.Objects.Discover;
using Avalonia.Threading;
using NetStream.Controls;

namespace NetStream
{
    public partial class DiscoverPageControl : UserControl, IDisposable
    {
        private List<int> selectedGenres = new List<int>();
        private int page = 1;

        public static DiscoverPageControl Instance;
        
        public DiscoverPageControl()
        {
            InitializeComponent();
            Instance  = this;
            // Kontroller başlatıldıktan sonra diğer metodları çağır
            Dispatcher.UIThread.Post(() =>
            {
                FillComboBoxes();
                Load();
                
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Kontrol referanslarını al
            HorizontalLayout = this.FindControl<Grid>("HorizontalLayout");
            MainScroll = this.FindControl<ScrollViewer>("MainScroll");
            GridMovies = this.FindControl<Grid>("GridMovies");
        }

        private async void Load()
        {
            try
            {
                var genresDisplay = this.FindControl<ItemsControl>("GenresDisplay");
                genresDisplay.ItemsSource = await Service.GetMovieGenres();
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void FillComboBoxes()
        {
            try
            {
                var showTypeComboBox = this.FindControl<ComboBox>("ShowTypeComboBox");
                var sortComboBox = this.FindControl<ComboBox>("SortComboBox");
                var comboBoxLanguages = this.FindControl<ComboBox>("ComboBoxLanguages");

                List<string> mediaTypes = new List<string>() { 
                    (ResourceProvider.GetString("MovieString")) as string,
                    (ResourceProvider.GetString("TvShowString") as string) 
                };
                showTypeComboBox.ItemsSource = mediaTypes;
                showTypeComboBox.SelectedIndex = 0;

                List<string> sortList = new List<string>()
                {
                    (ResourceProvider.GetString("PopularityDescendingString") as string),
                    (ResourceProvider.GetString("PopularityAscendingString") as string),
                    (ResourceProvider.GetString("RatingDescendingString") as string),
                    (ResourceProvider.GetString("RatingAscendingString") as string),
                    (ResourceProvider.GetString("ReleaseDateDescendingString") as string),
                    (ResourceProvider.GetString("ReleaseDateAscendingString") as string),
                    (ResourceProvider.GetString("TitleAZString") as string),
                    (ResourceProvider.GetString("TitleZAString") as string),
                };
                sortComboBox.ItemsSource = sortList;
                sortComboBox.SelectedIndex = -1;

                comboBoxLanguages.ItemsSource = await Service.GetLanguages();
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        /*
         *
         *  <Grid.ColumnDefinitions>
                <ColumnDefinition Width="0.02*"/>
                <ColumnDefinition Width="0.25*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
         */
        private void DiscoverPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                MainView.Instance.SizeChanged += InstanceOnSizeChanged;
                // Kontrolleri tekrar bul - bazen ilk yüklemede null kalabilirler
                if (HorizontalLayout == null) HorizontalLayout = this.FindControl<Grid>("HorizontalLayout");
                if (MainScroll == null) MainScroll = this.FindControl<ScrollViewer>("MainScroll");
                if (GridMovies == null) GridMovies = this.FindControl<Grid>("GridMovies");
                
                // Kontrollerden herhangi biri hala null ise, geciktirilmiş bir güncelleme planla
                if (HorizontalLayout == null || MainScroll == null || GridMovies == null)
                {
                    
                    // Biraz bekleyip tekrar dene
                    Dispatcher.UIThread.Post(() => 
                    {
                        // Kontrolleri tekrar bul
                        HorizontalLayout = this.FindControl<Grid>("HorizontalLayout");
                        MainScroll = this.FindControl<ScrollViewer>("MainScroll");
                        GridMovies = this.FindControl<Grid>("GridMovies");
                        
                        // Şimdi kontrollerin bulunmuş olması gerekiyor
                        if (HorizontalLayout != null && MainScroll != null && GridMovies != null)
                        {
                            // Mevcut genişliği kullanarak düzeni güncelle
                            UpdateLayout(this.Bounds.Width);
                        }
                    }, DispatcherPriority.Loaded);
                    
                    return;
                }
                
                // Mevcut genişliği kullanarak düzeni güncelle
                UpdateLayout(this.Bounds.Width);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DiscoverPage_OnLoaded hatası: {ex.Message}, {ex.StackTrace}");
            }
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            try
            {
                UpdateLayout(e.width);
                ApplyDirectStylesToMovieDisplayItems(e.width);
            }
            catch (Exception ex)
            {
                Log.Error($"DiscoverPage_SizeChanged hatası: {ex.Message}, {ex.StackTrace}");
            }
        }

        private void BorderGenre_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            try
            {
                var border = sender as Border;
                if (border.DataContext is Genre)
                {
                    Genre selectedGenre = (Genre)border.DataContext;

                    if (selectedGenres.Any(x => x == selectedGenre.Id))
                    {
                        border.Background = Avalonia.Media.Brushes.Transparent;
                        border.BorderBrush = Avalonia.Media.Brushes.White;
                        selectedGenre.IsSelected = false;
                        selectedGenres.Remove(selectedGenre.Id);
                    }
                    else
                    {
                        var colorDefault = (Color)App.Current.Resources["ColorDefault"];
                        var brush = new Avalonia.Media.SolidColorBrush(colorDefault);
                        border.Background = brush;
                        border.BorderBrush = brush;
                        selectedGenre.IsSelected = true;
                        selectedGenres.Add(selectedGenre.Id);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BorderGenre_OnMouseEnter(object sender, PointerEventArgs e)
        {
            try
            {
                var border = sender as Border;
                if (border.DataContext is Genre)
                {
                    var colorDefault =  (Color)App.Current.Resources["ColorDefault"];
                    var brush = new Avalonia.Media.SolidColorBrush(colorDefault);
                    border.Background = brush;
                    border.BorderBrush = brush;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BorderGenre_OnMouseLeave(object sender, PointerEventArgs e)
        {
            try
            {
                var border = sender as Border;
                if (border.DataContext is Genre)
                {
                    Genre selectedGenre = (Genre)border.DataContext;
                    if (!(selectedGenres.Any(x => x == selectedGenre.Id)))
                    {
                        border.Background = Avalonia.Media.Brushes.Transparent;
                        border.BorderBrush = Avalonia.Media.Brushes.White;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BorderGenre_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var border = sender as Border;
                if (border?.DataContext is Genre genre)
                {
                    if (selectedGenres.Contains(genre.Id))
                    {
                        var colorDefault = (Color)App.Current.Resources["ColorDefault"];
                        var brush = new Avalonia.Media.SolidColorBrush(colorDefault);
                        border.Background = brush;
                        border.BorderBrush = brush;
                    }
                    else
                    {
                        border.Background = Avalonia.Media.Brushes.Transparent;
                        border.BorderBrush = Avalonia.Media.Brushes.White;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"BorderGenre_OnLoaded error: {ex.Message}");
            }
        }

        private void DiscoverPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }

        private void MoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var moviesDisplay = this.FindControl<ItemsControl>("MoviesDisplay");
                var movie = e.AddedItems[0] as Movie;
                
                if (movie != null)
                {
                    // Navigate to movie details
                    var movieDetailsPage = new MovieDetailsPage(movie);
                    NavigationService.Instance.Navigate(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                var moviesDisplay = this.FindControl<ItemsControl>("MoviesDisplay");
                var showTypeComboBox = this.FindControl<ComboBox>("ShowTypeComboBox");
                
                if (moviesDisplay.ItemsSource != null)
                {
                    var scrollViewer = sender as ScrollViewer;
                    // Check if scrolled to bottom
                    if (scrollViewer.Offset.Y >= scrollViewer.ScrollBarMaximum.Y - 1)
                    {
                        if (Equals(showTypeComboBox.SelectedItem, ResourceProvider.GetString("MovieString")))
                        {
                            page++;
                            await Service.GetMorePagesDiscoverMovies(GetSelectedGenres(), GetAfterDateTime(), GetBeforeDateTime(),
                                GetMinVoteAverage(), GetMinVoteCounts(), GetMinRunTime(), GetMaxRunTime(), page,
                                GetSortType(), GetLanguage());
                        }
                        else if (Equals(showTypeComboBox.SelectedItem, ResourceProvider.GetString("TvShowString")))
                        {
                            page++;
                            await Service.GetMorePagesDiscoverTvShows(GetSelectedGenres(), GetAfterDateTime(),
                                GetBeforeDateTime(), GetMinVoteAverage(), GetMinVoteCounts(), GetMinRunTime(),
                                GetMaxRunTime(), page, GetSortTypeTv(), GetLanguage());
                        }
                    }
                }
                ApplyDirectStylesToMovieDisplayItems(this.Bounds.Width);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ShowTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var genresDisplay = this.FindControl<ItemsControl>("GenresDisplay");
                var showTypeComboBox = sender as ComboBox;
                
                genresDisplay.ItemsSource = null;
                selectedGenres.Clear();
                if (Equals(showTypeComboBox.SelectedItem, ResourceProvider.GetString("MovieString")))
                {
                    genresDisplay.ItemsSource = await Service.GetMovieGenres();
                }
                else if (Equals(showTypeComboBox.SelectedItem, ResourceProvider.GetString("TvShowString")))
                {
                    genresDisplay.ItemsSource = await Service.GetTvGenres();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void SortComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Implementation if needed
        }

        private DiscoverMovieSortBy GetSortType()
        {
            try
            {
                var sortComboBox = this.FindControl<ComboBox>("SortComboBox");
                var selectedSort = sortComboBox.SelectedItem;

                if (Equals(selectedSort, ResourceProvider.GetString("PopularityDescendingString")))
                {
                    return DiscoverMovieSortBy.PopularityDesc;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("PopularityAscendingString")))
                {
                    return DiscoverMovieSortBy.Popularity;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("RatingDescendingString")))
                {
                    return DiscoverMovieSortBy.VoteAverageDesc;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("RatingAscendingString")))
                {
                    return DiscoverMovieSortBy.VoteAverage;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("ReleaseDateDescendingString")))
                {
                    return DiscoverMovieSortBy.PrimaryReleaseDateDesc;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("ReleaseDateAscendingString")))
                {
                    return DiscoverMovieSortBy.PrimaryReleaseDate;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("TitleAZString")))
                {
                    return DiscoverMovieSortBy.OriginalTitleDesc;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("TitleZAString")))
                {
                    return DiscoverMovieSortBy.OriginalTitle;
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }

            return DiscoverMovieSortBy.Undefined;
        }

        private DiscoverTvShowSortBy GetSortTypeTv()
        {
            try
            {
                var sortComboBox = this.FindControl<ComboBox>("SortComboBox");
                var selectedSort = sortComboBox.SelectedItem;

                if (Equals(selectedSort, ResourceProvider.GetString("PopularityDescendingString")))
                {
                    return DiscoverTvShowSortBy.PopularityDesc;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("PopularityAscendingString")))
                {
                    return DiscoverTvShowSortBy.Popularity;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("RatingDescendingString")))
                {
                    return DiscoverTvShowSortBy.VoteAverageDesc;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("RatingAscendingString")))
                {
                    return DiscoverTvShowSortBy.VoteAverage;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("ReleaseDateDescendingString")))
                {
                    return DiscoverTvShowSortBy.PrimaryReleaseDateDesc;
                }
                else if (Equals(selectedSort, ResourceProvider.GetString("ReleaseDateAscendingString")))
                {
                    return DiscoverTvShowSortBy.PrimaryReleaseDate;
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }

            return DiscoverTvShowSortBy.Undefined;
        }

        private DateTime GetAfterDateTime()
        {
            var releaseDateFilterFrom = this.FindControl<DatePicker>("ReleaseDateFilterFrom");
            return releaseDateFilterFrom.SelectedDate.HasValue 
                ? releaseDateFilterFrom.SelectedDate.Value.DateTime 
                : DateTime.MinValue;
        }
        
        private DateTime GetBeforeDateTime()
        {
            var releaseDateFilterTo = this.FindControl<DatePicker>("ReleaseDateFilterTo");
            return releaseDateFilterTo.SelectedDate.HasValue 
                ? releaseDateFilterTo.SelectedDate.Value.DateTime 
                : DateTime.MaxValue;
        }

        private List<int> GetSelectedGenres()
        {
            return selectedGenres;
        }

        private double GetMinVoteAverage()
        {
            var minVoteAverageSlider = this.FindControl<Slider>("MinVoteAverageSlider");
            return minVoteAverageSlider.Value;
        }

        private int GetMinVoteCounts()
        {
            var minUserVotesSlider = this.FindControl<Slider>("MinUserVotesSlider");
            return (int)minUserVotesSlider.Value;
        }

        private int GetMaxRunTime()
        {
            var maxRuntimeSlider = this.FindControl<Slider>("MaxRuntimeSlider");
            return (int)maxRuntimeSlider.Value;
        }

        private int GetMinRunTime()
        {
            var minRuntimeSlider = this.FindControl<Slider>("MinRuntimeSlider");
            return (int)minRuntimeSlider.Value;
        }

        public string GetLanguage()
        {
            var comboBoxLanguages = this.FindControl<ComboBox>("ComboBoxLanguages");
            if (comboBoxLanguages.SelectedItem != null)
            {
                var selectedLanguage = comboBoxLanguages.SelectedItem.ToString();
                return Service.Languages.FirstOrDefault(x => x.EnglishName == selectedLanguage).Iso_639_1;
            }

            return null;
        }

        private async void SearchButton_OnPreviewMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            try
            {
                var moviesDisplay = this.FindControl<ItemsControl>("MoviesDisplay");
                var mainScroll = this.FindControl<ScrollViewer>("MainScroll");
                var showTypeComboBox = this.FindControl<ComboBox>("ShowTypeComboBox");
                
                if (Equals(showTypeComboBox.SelectedItem, ResourceProvider.GetString("MovieString")))
                {
                    page = 1;
                    moviesDisplay.ItemsSource = Service.DiscoveredMovies;
                    mainScroll.ScrollToHome();
                    await Service.DiscoverMovies(GetSelectedGenres(), GetAfterDateTime(), GetBeforeDateTime(),
                        GetMinVoteAverage(), GetMinVoteCounts(), GetMinRunTime(), GetMaxRunTime(), page,
                        GetSortType(), GetLanguage());

                }
                else if (Equals(showTypeComboBox.SelectedItem, ResourceProvider.GetString("TvShowString")))
                {
                    page = 1;
                    moviesDisplay.ItemsSource = Service.DiscoveredTvShows;
                    mainScroll.ScrollToHome();
                    await Service.DiscoverTvShows(GetSelectedGenres(), GetAfterDateTime(), GetBeforeDateTime(),
                        GetMinVoteAverage(), GetMinVoteCounts(), GetMinRunTime(), GetMaxRunTime(), page,
                        GetSortTypeTv(), GetLanguage());
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MainScroll_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Implementation if needed
        }

        public void Dispose()
        {
            try
            {
                // Unsubscribe from events
                if (MainView.Instance != null)
                {
                    MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
                }
                
                // AsyncImageControl'ları temizle
                var moviesDisplay = this.FindControl<ItemsControl>("MoviesDisplay");
                if (moviesDisplay != null)
                {
                    foreach (var visualDescendant in moviesDisplay.GetVisualDescendants())
                    {
                        if (visualDescendant is AsyncImageControl asyncImageControl)
                        {
                            asyncImageControl.Dispose();
                        }
                    }
                }
                
                // Collection'ları temizle
                Service.DiscoveredMovies.Clear();
                Service.DiscoveredTvShows.Clear();
                selectedGenres?.Clear();
                
                // ItemsSource'ları temizle
                if (moviesDisplay != null)
                {
                    moviesDisplay.ItemsSource = null;
                }
                
                // DataContext'i temizle
                this.DataContext = null;
                
                // Clear static instance
                Instance = null;
            }
            catch (Exception ex)
            {
                Log.Error($"DiscoverPageControl Dispose error: {ex.Message}");
            }
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
        
        // Kart genişliği için ölçekleme
        private double CalculateCardWidth(double width)
        {
            return CalculateScaledValue(width, 140, 400);
        }

        private void StackPanelMovie_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if(stackPanel == null) return;
            var selectedMovie = stackPanel.DataContext as Movie;
            if(selectedMovie == null) return;
            var movieDetailsPage = new MovieDetailsPage(selectedMovie);
            var mainView = this.FindAncestorOfType<MainView>();
            if (mainView != null)
            {
                mainView.SetContent(movieDetailsPage);
            }
        }
        private double CalculateCardHeight(double width)
        {
            return CalculateScaledValue(width, 210, 600);
        }
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
        // Ekran boyutu değiştiğinde düzeni güncelle
        private void ApplyDirectStylesToMovieDisplayItems(double width)
        {
            bool isExtraSmall = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
            bool isSmall = width <= SMALL_SCREEN_THRESHOLD && !isExtraSmall;
            
            // Ekran genişliğini maksimum 3840px ile sınırla
            double clampedWidth = Math.Min(width, 3840);
            
            // Kart boyutlarını hesapla
            double cardWidth = CalculateCardWidth(clampedWidth);
            double cardHeight = CalculateCardHeight(clampedWidth);
            
            // Font boyutlarını hesapla
            double titleFontSize = CalculateTextSize(clampedWidth, 12, 24);
            double ratingFontSize = CalculateTextSize(clampedWidth, 10, 18);
            double starSize = CalculateTextSize(clampedWidth, 15, 30);
            
            // MoviesDisplay ItemsControl için öğeleri güncelle
            var moviesDisplay = this.FindControl<ItemsControl>("MoviesDisplay");
            if (moviesDisplay == null || moviesDisplay.Items == null) return;
            
            foreach (var item in moviesDisplay.GetVisualDescendants())
            {
                // Kart sınırlarını güncelle
                if (item is Border border && (border.Name == "MovieCardBorder" || border.Classes.Contains("movie-card")))
                {
                    // Boyut güncelle
                    border.Width = cardWidth;
                    border.Height = cardHeight;
                    
                    // Sınıfları güncelle
                    border.Classes.Clear();
                    if (isExtraSmall)
                        border.Classes.Add("movie-card-small");
                    else if (isSmall)
                        border.Classes.Add("movie-card-medium");
                    else
                        border.Classes.Add("movie-card");
                }
                // Film başlıklarını güncelle
                else if (item is TextBlock textBlock && textBlock.Name == "MovieTitle")
                {
                    // Font boyutunu ve maksimum genişliği güncelle
                    textBlock.FontSize = titleFontSize;
                    textBlock.MaxWidth = cardWidth * 0.8;
                    
                    textBlock.Classes.Clear();
                    if (isExtraSmall)
                        textBlock.Classes.Add("movie-title-small");
                    else if (isSmall)
                        textBlock.Classes.Add("movie-title-medium");
                    else
                        textBlock.Classes.Add("movie-title");
                }
                // Derecelendirme çubuklarını güncelle
                else if (item is RatingBar ratingBar && ratingBar.Name == "MovieRating")
                {
                    // Yıldız boyutunu güncelle
                    ratingBar.StarSize = starSize;
                }
                // Derecelendirme metinlerini güncelle
                else if (item is TextBlock ratingText && ratingText.Name == "RatingText")
                {
                    // Derecelendirme metni boyutunu ve kenar boşluğunu güncelle
                    ratingText.FontSize = ratingFontSize;
                    
                    // Kenar boşluğunu kart genişliğine göre ayarla
                    double leftMargin = cardWidth * 0.06;
                    ratingText.Margin = new Thickness(leftMargin, 3.5, 0, 0);
                }
            }
        }
        
        // Düzen güncelleme metodunu ayrı bir metoda çıkardım - tekrar kullanımı kolaylaştırmak için
        private void UpdateLayout(double width)
        {
            try
            {
                // HorizontalLayout, MainScroll ve GridMovies'in null olmadığından emin ol
                if (HorizontalLayout == null || MainScroll == null || GridMovies == null)
                {
                    Console.WriteLine("DiscoverPageControl'un bazı bileşenleri henüz hazır değil.");
                    return;
                }
                
                // Grid'in mevcut tanımlarını temizle
                HorizontalLayout.ColumnDefinitions.Clear();
                HorizontalLayout.RowDefinitions.Clear();
                
                // Ekran genişliği 600px'den küçükse (dikey düzen)
                if (width < 600)
                {
                    // Dikey düzen için Row tanımları ekle
                    HorizontalLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.02, GridUnitType.Star) });
                    HorizontalLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.4, GridUnitType.Star) });
                    HorizontalLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.58, GridUnitType.Star) });
                    
                    // 3 sütun tanımla - MainScroll ortada kalacak
                    HorizontalLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.05, GridUnitType.Star) });
                    HorizontalLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    HorizontalLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.05, GridUnitType.Star) });
                    
                    // MainScroll'u ortadaki sütuna yerleştir
                    Grid.SetRow(MainScroll, 1);
                    Grid.SetColumn(MainScroll, 1);
                    Grid.SetColumnSpan(MainScroll, 1); // Sadece orta sütuna sığsın
                    
                    // GridMovies'i tüm satıra yay
                    Grid.SetRow(GridMovies, 2);
                    Grid.SetColumn(GridMovies, 0);
                    Grid.SetColumnSpan(GridMovies, 3); // Tüm sütunlara yayılsın
                }
                else
                {
                    // Yatay düzen için sütun tanımları
                    HorizontalLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.02, GridUnitType.Star) });
                    HorizontalLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.25, GridUnitType.Star) });
                    HorizontalLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    
                    // Tek bir satır tanımla
                    HorizontalLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    
                    // Kontrolleri yatay düzen için konumlandır
                    Grid.SetRow(MainScroll, 0);
                    Grid.SetColumn(MainScroll, 1);
                    Grid.SetColumnSpan(MainScroll, 1); // Sıfırla
                    
                    Grid.SetRow(GridMovies, 0);
                    Grid.SetColumn(GridMovies, 2);
                    Grid.SetColumnSpan(GridMovies, 1); // Sıfırla
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateLayout hatası: {ex.Message}, {ex.StackTrace}");
            }
        }
    }
} 