using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetStream.Controls;
using Serilog;
using TMDbLib.Objects.Account;
using TMDbLib.Objects.General;
using TMDbLib.Objects.TvShows;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for AccountFavoritesPage.xaml
    /// </summary>
    public partial class AccountFavoritesPage : UserControl,IDisposable
    {
        private int pageMovies = 1;
        private int pageTvShow = 1;
        private FavoriteType favoriteType;
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
        
        public AccountFavoritesPage()
        {
            InitializeComponent();
            pageMovies = 1;
            pageTvShow = 1;
            favoriteType = FavoriteType.Movie;
            Load();
        }

        private async void LoadFavoriteMovies()
        {
            try
            {
                favoriteType = FavoriteType.Movie;

                MoviesDisplay.ItemsSource = Service.AccountFavoritesMovies;
                TvShowsDisplayScrollViewer.IsVisible = false;

                await Service.GetFavoritesMovies(1, AccountSortBy.CreatedAt, SortOrder.Descending);

                if (Service.AccountFavoritesMovies.Count > 0)
                {
                    ViewboxNoMoviesFound.IsVisible = false;
                    MoviesDisplayScrollViewer.IsVisible = true;
                }
                else
                {
                    NoMOviesFound.Text = ResourceProvider.GetString("NoMovieFoundString");
                    ViewboxNoMoviesFound.IsVisible = true;
                    MoviesDisplayScrollViewer.IsVisible = false;
                }
                
                // UI güncellemesi için biraz gecikme ekleyelim
                await Task.Delay(100);
                ApplyDirectStylesToFavoritesItems(this.Bounds.Width);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void LoadFavoriteTvShows()
        {
            try
            {
                favoriteType = FavoriteType.TvShow;

                TvShowsDisplay.ItemsSource = Service.AccountFavoritesTvShows;
                MoviesDisplayScrollViewer.IsVisible = false;

                await Service.GetFavoritesTvShows(1, AccountSortBy.CreatedAt, SortOrder.Descending);
            
                if (Service.AccountFavoritesTvShows.Count > 0)
                {
                    ViewboxNoMoviesFound.IsVisible = false;
                    TvShowsDisplayScrollViewer.IsVisible = true;
                }
                else
                {
                    NoMOviesFound.Text = ResourceProvider.GetString("NoTvFoundString");
                    ViewboxNoMoviesFound.IsVisible = true;
                    TvShowsDisplayScrollViewer.IsVisible = false;
                }
                
                // UI güncellemesi için biraz gecikme ekleyelim
                await Task.Delay(100);
                ApplyDirectStylesToFavoritesItems(this.Bounds.Width);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MoviesDisplay_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                {
                    var stackPanel = sender as StackPanel;
                    if(stackPanel == null) return;
                    var selectedMovie = stackPanel.DataContext as Movie;
                    if(selectedMovie == null) return;
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMovie);
                    MainView.Instance.SetContent(movieDetailsPage);
                               
                }
                else if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
                {
                    var stackPanel = sender as StackPanel;
                    if(stackPanel == null) return;
                    var selectedMovie = stackPanel.DataContext as Movie;
                    if(selectedMovie == null) return;
                    selectedMovieIndex = Service.AccountFavoritesMovies.IndexOf(selectedMovie);
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
                var scrollViewer = (ScrollViewer)sender;
                if (Math.Abs(scrollViewer.Offset.Y + scrollViewer.Viewport.Height - scrollViewer.Extent.Height) < 1)
                {
                    if (pageMovies <= Service.MaxFavoritesMoviePage)
                    {
                        pageMovies++;
                        await Service.GetFavoritesMoviesMorePages(pageMovies, AccountSortBy.CreatedAt, SortOrder.Descending);
                        
                        // Yeni öğeler eklendiğinde stili güncelle
                        
                    }
                }
                ApplyDirectStylesToFavoritesItems(this.Bounds.Width);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        

        private async void TvShowsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                var scrollViewer = (ScrollViewer)sender;
                if (Math.Abs(scrollViewer.Offset.Y + scrollViewer.Viewport.Height - scrollViewer.Extent.Height) < 1)
                {
                    if (pageTvShow <= Service.MaxFavoritesTvShowPage)
                    {
                        pageTvShow++;
                        await Service.GetFavoritesTvShowsMorePages(pageTvShow, AccountSortBy.CreatedAt, SortOrder.Descending);
                        
                        // Yeni öğeler eklendiğinde stili güncelle
                        ApplyDirectStylesToFavoritesItems(this.Bounds.Width);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TvShowsDisplay_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                {
                    var stackPanel = sender as StackPanel;
                    if (stackPanel == null) return;
                    var selectedTvShow = stackPanel.DataContext as Movie;
                    if (selectedTvShow == null) return;
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedTvShow);
                    MainView.Instance.SetContent(movieDetailsPage);
                }
                else if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
                {
                    var stackPanel = sender as StackPanel;
                    if (stackPanel == null) return;
                    var selectedTvShow = stackPanel.DataContext as Movie;
                    if (selectedTvShow == null) return;
                    selectedTvIndex = Service.AccountFavoritesTvShows.IndexOf(selectedTvShow);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            // Movie remove
            try
            {
                if (selectedMovieIndex >= 0 && selectedMovieIndex < Service.AccountFavoritesMovies.Count)
                {
                    var selectedMovie = Service.AccountFavoritesMovies[selectedMovieIndex];

                    if (selectedMovie != null)
                    {
                        var result = await Service.client.AccountChangeFavoriteStatusAsync(MediaType.Movie, selectedMovie.Id, false);
                        if (result)
                        {
                            Service.AccountFavoritesMovies.Remove(selectedMovie);
                            if (Service.AccountFavoritesMovies.Count == 0)
                            {
                                MoviesDisplayScrollViewer.IsVisible = false;
                                ViewboxNoMoviesFound.IsVisible = true;
                                NoMOviesFound.Text = ResourceProvider.GetString("NoMovieFoundString");
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private int selectedMovieIndex = -1;
        private int selectedTvIndex = -1;

        private async void RemoveFavoriteTvShow(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedTvIndex >= 0 && selectedTvIndex < Service.AccountFavoritesTvShows.Count)
                {
                    var selectedMovie = Service.AccountFavoritesTvShows[selectedTvIndex];

                    if (selectedMovie != null)
                    {
                        var result = await Service.client.AccountChangeFavoriteStatusAsync(MediaType.Tv, selectedMovie.Id, false);
                        if (result)
                        {
                            Service.AccountFavoritesTvShows.Remove(selectedMovie);
                            if (Service.AccountFavoritesTvShows.Count == 0)
                            {
                                TvShowsDisplayScrollViewer.IsVisible = false;
                                ViewboxNoMoviesFound.IsVisible = true;
                                NoMOviesFound.Text = ResourceProvider.GetString("NoTvFoundString");
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void AccountFavoritesPage_OnLoaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void Load()
        {
            try
            {
                if (favoriteType == FavoriteType.Movie)
                {
                    LoadFavoriteMovies();
                }
                else
                {
                    LoadFavoriteTvShows();
                }
                
                MainView.Instance.SizeChanged += InstanceOnSizeChanged;
                ApplyDirectStylesToFavoritesItems(this.Bounds.Width);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyDirectStylesToFavoritesItems(e.width);
        }

        private async void MenuButtonMovie_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                {
                    LoadFavoriteMovies();
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.MenuLine2.SetValue(Grid.ColumnProperty, 0);
                        MenuButtonMovie.Foreground = new SolidColorBrush(Color.Parse("#FFFFFF"));
                        MenuButtonTvShow.Foreground = new SolidColorBrush(Color.Parse("#414141"));
                    });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MenuButtonTvShow_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                {
                    LoadFavoriteTvShows();
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.MenuLine2.SetValue(Grid.ColumnProperty, 2);
                        MenuButtonMovie.Foreground = new SolidColorBrush(Color.Parse("#414141"));
                        MenuButtonTvShow.Foreground = new SolidColorBrush(Color.Parse("#FFFFFF"));
                    });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        // Ekran boyutu değiştiğinde çağrılır
      
        
        // Ekran genişliğine göre ölçeklendirilmiş değer hesaplar
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
        
        // Kart genişliği hesaplar
        private double CalculateCardWidth(double width)
        {
            return CalculateScaledValue(width, 140, 400);
        }
        
        // Kart yüksekliği için ölçekleme
        private double CalculateCardHeight(double width)
        {
            return CalculateScaledValue(width, 210, 600);
        }
        
        // Yazı boyutu hesaplar
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }
        
        // Menü boyutu hesaplar
        private double CalculateMenuTextSize(double width)
        {
            return CalculateScaledValue(width, 14, 20);
        }
        
        // Menü çizgisi yüksekliği hesaplar
        private double CalculateMenuLineHeight(double width)
        {
            return CalculateScaledValue(width, 1, 2);
        }
        
        // Ekran boyutuna göre favori öğelerini günceller
        private void ApplyDirectStylesToFavoritesItems(double width)
        {
            try
            {
                bool isExtraSmall = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                bool isSmall = width <= SMALL_SCREEN_THRESHOLD && !isExtraSmall;
                
                // Ekran genişliğini maksimum 3840px ile sınırla
                double clampedWidth = Math.Min(width, 3840);
                
                // Öğe boyutlarını hesapla
                double cardWidth = CalculateCardWidth(clampedWidth);
                double cardHeight = CalculateCardHeight(clampedWidth);
                
                // Font boyutlarını hesapla
                double menuFontSize = CalculateMenuTextSize(clampedWidth);
                double titleFontSize = CalculateTextSize(clampedWidth, 12, 24);
                double ratingFontSize = CalculateTextSize(clampedWidth, 10, 18);
                double starSize = CalculateTextSize(clampedWidth, 15, 30);

                // Menü çizgisi boyutlarını hesapla
                double menuLineHeight = CalculateMenuLineHeight(clampedWidth);
                
                // Alt menü öğelerini güncelle
                MenuButtonMovie.FontSize = menuFontSize;
                MenuButtonTvShow.FontSize = menuFontSize;
                
                // Menüler arasındaki boşluğu ayarla
                if (SubMenu.ColumnDefinitions.Count >= 3)
                {
                    SubMenu.ColumnDefinitions[1].Width = new GridLength(isExtraSmall ? 10 : (isSmall ? 15 : 20));
                }
                
                // Menü çizgisini güncelle
                MenuLine2.Height = menuLineHeight;
                
                // Film ve dizi kartlarını güncelle
                
                // Filmleri güncelle
                if (MoviesDisplay.ItemsSource != null)
                {
                    foreach (var item in MoviesDisplay.GetVisualDescendants())
                    {
                        UpdateUIElement(item, cardWidth, cardHeight, titleFontSize, ratingFontSize, starSize, isSmall, isExtraSmall);
                    }
                }
                
                // Dizileri güncelle
                if (TvShowsDisplay.ItemsSource != null)
                {
                    foreach (var item in TvShowsDisplay.GetVisualDescendants())
                    {
                        UpdateUIElement(item, cardWidth, cardHeight, titleFontSize, ratingFontSize, starSize, isSmall, isExtraSmall);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ApplyDirectStylesToFavoritesItems hatası: {ex.Message}, {ex.StackTrace}");
            }
        }
        
        // Ortak UI elemanlarını güncelleyen yardımcı metot
        private void UpdateUIElement(Visual item, double cardWidth, double cardHeight, double titleFontSize, double ratingFontSize, double starSize, bool isSmall, bool isExtraSmall)
        {
            try
            {
                // Kart sınırlarını güncelle
                if (item is Border border && border.Child is AsyncImageControl)
                {
                    border.Width = cardWidth;
                    border.Height = cardHeight;
                }
                // Film/dizi başlıklarını güncelle
                else if (item is TextBlock textBlock)
                {
                    if (textBlock.Name == "TextBlockName")
                    {
                        textBlock.FontSize = titleFontSize;
                        textBlock.MaxWidth = cardWidth * 0.8;
                        textBlock.Margin = new Thickness(0, isExtraSmall ? 10 : (isSmall ? 15 : 20), 0, 0);
                    }
                    else if (textBlock.Name == "TextBlockRatingNumber")
                    {
                        textBlock.FontSize = ratingFontSize;
                        textBlock.Margin = new Thickness(isExtraSmall ? 8 : (isSmall ? 12 : 15), 3.5, 0, 0);
                    }
                }
                // Derecelendirme çubuklarını güncelle
                else if (item is RatingBar ratingBar)
                {
                    ratingBar.StarSize = starSize;
                }
                // Stackpanel'leri güncelle (kart container'ları)
                else if (item is StackPanel stackPanel)
                {
                    if (stackPanel.Orientation == Avalonia.Layout.Orientation.Vertical && stackPanel.Children.Count > 0)
                    {
                        stackPanel.Margin = new Thickness(10, 10, isExtraSmall ? 3 : (isSmall ? 5 : 7), 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateUIElement hatası: {ex.Message}");
            }
        }

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
           
        }

        public void Dispose()
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
    }

    public enum FavoriteType
    {
        Movie,
        TvShow
    }
} 