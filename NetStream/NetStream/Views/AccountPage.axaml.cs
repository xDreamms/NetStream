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
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for AccountPage.xaml
    /// </summary>
    public partial class AccountPage : UserControl,IDisposable
    {
        public static AccountPage GetAccountPageInstance;
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
        private AccountFavoritesPage accountFavoritesPage;
        public AccountPage()
        {
            InitializeComponent();
            accountFavoritesPage = new AccountFavoritesPage();
            AccountNavigation.Content = accountFavoritesPage;
            GetAccountPageInstance = this;
            
            // Ekran boyutu değiştiğinde düzeni güncelle
            
            // Başlangıç durumunda ekrana göre düzeni ayarla
            Dispatcher.UIThread.Post(() => {
                ApplyDirectStylesToAccountItems(this.Bounds.Width);
            });
            
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
        }
        AccountWatchListPage accountWatchListPage;
        private async void NavigationButtonPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                var current = sender as TextBlock;
                if (current != null)
                {
                    switch (current.Name)
                    {
                        case "FavoritesButton":
                            if (accountFavoritesPage == null)
                            {
                                accountFavoritesPage= new AccountFavoritesPage();
                            }
                            AccountNavigation.Content = accountFavoritesPage;
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                this.MenuLine1.SetValue(Grid.ColumnProperty, 1);
                                FavoritesButton.Foreground = new SolidColorBrush(Color.Parse("#FFFFFF"));
                                WatchListButton.Foreground = new SolidColorBrush(Color.Parse("#414141"));
                            });
                            break;
                        case "WatchListButton":
                            if (accountWatchListPage == null)
                            {
                                accountWatchListPage = new AccountWatchListPage();
                            }
                            AccountNavigation.Content = accountWatchListPage;
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                this.MenuLine1.SetValue(Grid.ColumnProperty, 3);
                                FavoritesButton.Foreground = new SolidColorBrush(Color.Parse("#414141"));
                                WatchListButton.Foreground = new SolidColorBrush(Color.Parse("#FFFFFF"));
                            });
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ButtonLogOut_OnPointerPressed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (await Service.LogOut())
                {
                    GetAccountPageInstance = null;
                    MainView.Instance.SetContent(new AccountLoginPage());
                    this.Dispose();
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
        
        // Yazı boyutu hesaplar
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }
        
        // Buton boyutu hesaplar
        private double CalculateButtonWidth(double width)
        {
            return CalculateScaledValue(width, 80, 120);
        }
        
        // Menü çizgisi boyutlarını hesaplar
        private double CalculateMenuLineHeight(double width)
        {
            return CalculateScaledValue(width, 1, 3);
        }
        
        // Ekran boyutuna göre hesap sayfası öğelerini günceller
        private void ApplyDirectStylesToAccountItems(double width)
        {
            try
            {
                bool isExtraSmall = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                bool isSmall = width <= SMALL_SCREEN_THRESHOLD && !isExtraSmall;
                
                // Ekran genişliğini maksimum 3840px ile sınırla
                double clampedWidth = Math.Min(width, 3840);
                
                // Yazı boyutlarını hesapla
                double menuFontSize = CalculateTextSize(clampedWidth, 18, 32);
                double buttonFontSize = CalculateTextSize(clampedWidth, 12, 16);
                
                // Buton boyutunu hesapla
                double buttonWidth = CalculateButtonWidth(clampedWidth);
                
                // Menü çizgisi boyutlarını hesapla
                double menuLineHeight = CalculateMenuLineHeight(clampedWidth);
                
                // Çıkış butonunu güncelle
                ButtonLogOut.FontSize = buttonFontSize;
                ButtonLogOut.Margin = new Thickness(0, isExtraSmall ? 2 : (isSmall ? 3 : 5), isExtraSmall ? 2 : (isSmall ? 3 : 5), 0);
                
                // Menü butonlarını güncelle
                FavoritesButton.FontSize = menuFontSize;
                WatchListButton.FontSize = menuFontSize;
                
                // Menüler arasındaki boşluğu ayarla
                var menuSpacing = GridTab.ColumnDefinitions[2];
                if (menuSpacing != null)
                {
                    menuSpacing.Width = new GridLength(isExtraSmall ? 20 : (isSmall ? 35 : 50));
                }
                
                
                // Menü çizgisini güncelle
                MenuLine1.Height = menuLineHeight;
            }
            catch (Exception ex)
            {
                Log.Error($"ApplyDirectStylesToAccountItems hatası: {ex.Message}, {ex.StackTrace}");
            }
        }

        private void Control_OnLoaded(object? sender, RoutedEventArgs e)
        {
           
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyDirectStylesToAccountItems(e.width);
        }

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            
        }

        public void Dispose()
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
            if (accountFavoritesPage != null)
            {
                accountFavoritesPage.Dispose();
            }

            if (accountWatchListPage != null)
            {
                accountWatchListPage.Dispose();
            }
        }
    }
} 