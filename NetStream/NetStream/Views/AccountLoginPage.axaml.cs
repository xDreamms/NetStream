using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for AccountLoginPage.xaml
    /// </summary>
    public partial class AccountLoginPage : UserControl,IDisposable
    {
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
        public static AccountLoginPage Instance;
        
        public AccountLoginPage()
        {
            InitializeComponent();
            Instance = this;
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
            ApplyDirectStylesToLoginItems(MainView.Instance.Bounds.Width);
        }

        private void AccountLoginPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyDirectStylesToLoginItems(e.width);
        }

        private void AccountLoginPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            
        }

        private async void UIElement_OnPointerPressed(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
               
                    if (String.IsNullOrWhiteSpace(TextBoxUsername.Text) && !String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                    {
                        TextBlockError.IsVisible = true;
                        TextBlockError.Text = ResourceProvider.GetString("UsernameError");
                    }
                    else if (!String.IsNullOrWhiteSpace(TextBoxUsername.Text) && String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                    {
                        TextBlockError.IsVisible = true;
                        TextBlockError.Text = ResourceProvider.GetString("PasswordErrorString");
                    }
                    else if (String.IsNullOrWhiteSpace(TextBoxUsername.Text) && String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                    {
                        TextBlockError.IsVisible = true;
                        TextBlockError.Text = ResourceProvider.GetString("UsernamePasswordError");
                    }
                    else
                    {
                        TextBlockError.IsVisible = false;
                        var loginResult = await Service.Login(TextBoxUsername.Text, TextBoxPassword.Text);

                        if (loginResult)
                        {
                            var accountPage = new AccountPage();
                            AccountPage.GetAccountPageInstance = accountPage;
                            MainView.Instance.SetContent(accountPage);
                        }
                    }
                
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonSignUp_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                {
                    Process.Start(new ProcessStartInfo("https://www.themoviedb.org/signup") { UseShellExecute = true });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxPassword_OnGotFocus(object sender, GotFocusEventArgs e)
        {
            try
            {
                var current = Color.Parse($"#{AppSettingsManager.appSettings.PrimaryColorAlpha:X2}{AppSettingsManager.appSettings.PrimaryColorRed:X2}{AppSettingsManager.appSettings.PrimaryColorGreen:X2}{AppSettingsManager.appSettings.PrimaryColorBlue:X2}");
                TextBoxPassword.BorderBrush = new SolidColorBrush(current);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxPassword_OnLostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxPassword.BorderBrush = new SolidColorBrush(Color.Parse("#494949"));
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
        
        // Logo boyutu hesaplar
        private double CalculateLogoSize(double width)
        {
            return CalculateScaledValue(width, 133, 250);
        }
        
        // TextBox genişliği hesaplar
        private double CalculateTextBoxWidth(double width)
        {
            return CalculateScaledValue(width, 280, 1000);
        }
        
        // Yazı boyutu hesaplar
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
        }
        
        // Buton yüksekliği hesaplar
        private double CalculateButtonHeight(double width)
        {
            return CalculateScaledValue(width, 35, 53);
        }
        
        // Ekran boyutuna göre giriş sayfası öğelerini günceller
        private void ApplyDirectStylesToLoginItems(double width)
        {
            try
            {
                bool isExtraSmall = width <= EXTRA_SMALL_SCREEN_THRESHOLD;
                bool isSmall = width <= SMALL_SCREEN_THRESHOLD && !isExtraSmall;
                
                // Ekran genişliğini maksimum 3840px ile sınırla
                double clampedWidth = Math.Min(width, 3840);
                
                // Öğe boyutlarını hesapla
                double logoSize = CalculateLogoSize(clampedWidth);
                double textBoxWidth = CalculateTextBoxWidth(clampedWidth);
                double buttonHeight = CalculateButtonHeight(clampedWidth);
                
                // Font boyutlarını hesapla
                double textBoxFontSize = CalculateTextSize(clampedWidth, 12, 18);
                double errorFontSize = CalculateTextSize(clampedWidth, 10, 14);
                double buttonFontSize = CalculateTextSize(clampedWidth, 12, 18);
                
                // Logo boyutunu güncelle
                var logo = this.FindControl<Image>("Logo");
                if (logo != null)
                {
                    logo.Width = logoSize;
                    logo.Margin = new Thickness(0, 0, 0, isExtraSmall ? 20 : (isSmall ? 30 : 0));
                }
                
                // TextBox'ları güncelle
                TextBoxUsername.Width = textBoxWidth;
                TextBoxUsername.FontSize = textBoxFontSize;
                TextBoxUsername.Margin = new Thickness(0, isExtraSmall ? 30 : (isSmall ? 40 : 50), 0, 0);
                
                TextBoxPassword.Width = textBoxWidth;
                TextBoxPassword.FontSize = textBoxFontSize;
                TextBoxPassword.Margin = new Thickness(0, isExtraSmall ? 10 : (isSmall ? 15 : 20), 0, 0);
                
                // Hata mesajını güncelle
                TextBlockError.FontSize = errorFontSize;
                
                // Butonları güncelle
                var loginButton = this.FindControl<Button>("LoginButton");
                if (loginButton != null)
                {
                    loginButton.Width = textBoxWidth;
                    loginButton.Height = buttonHeight;
                    loginButton.FontSize = buttonFontSize;
                    loginButton.Margin = new Thickness(0, isExtraSmall ? 10 : (isSmall ? 15 : 20), 0, 0);
                }
                
                // Kayıt butonunu güncelle
                ButtonSignUp.Width = textBoxWidth;
                ButtonSignUp.Height = buttonHeight;
                ButtonSignUp.FontSize = buttonFontSize;
                ButtonSignUp.Margin = new Thickness(0, isExtraSmall ? 10 : (isSmall ? 15 : 20), 0, 0);
            }
            catch (Exception ex)
            {
                Log.Error($"ApplyDirectStylesToLoginItems hatası: {ex.Message}, {ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
    }
} 