using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using Avalonia.Threading;
using Avalonia.Media;
using TinifyAPI;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for MainAccountPage.xaml
    /// </summary>
    public partial class MainAccountPage : UserControl
    {
        private SettingsPage settingsPage;
        
        public MainAccountPage(SettingsPage settingsPage)
        {
            InitializeComponent();
            this.settingsPage = settingsPage;
            
            // Attach Loaded event handler
            this.Loaded += MainAccountPage_Loaded;
        }

        public MainAccountPage()
        {
            InitializeComponent();
            
            // Attach Loaded event handler
            this.Loaded += MainAccountPage_Loaded;
        }
        
        private void MainAccountPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Call Load method when the control is fully loaded
            Task.Run(async () => await Load());
        }

        private async Task Load()
        {
            try
            {
                // Use the UIThread dispatcher to run FirestoreManager.ListenUsers
                await Dispatcher.UIThread.InvokeAsync(async () => 
                {
                    await FirestoreManager.ListenUsers(this);
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ListenUsers: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private string newImagePath;
        private async Task Compress(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(Tinify.Key))
                {
                    Tinify.Key = "VGpmKT6zk1v8gtnkK0p0lpPtMLwlHvXb";
                }
                var source = Tinify.FromFile(path);

                string savePath = path;

                string extension = Path.GetExtension(path).ToLower();
                string fileName = Path.GetFileName(path);
                string directoryName = Path.GetDirectoryName(path);
                string newPath = Path.Combine(directoryName,
                    Path.GetFileNameWithoutExtension(fileName) + "compressed" + extension);
                savePath = newPath;

                await source.ToFile(savePath);
                newImagePath = savePath;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
                newImagePath = path;
            }
        }

        private async void ButtonChangeProfileImage_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                // Avalonia'da dosya açma dialogu kullanımı
                var topLevel = TopLevel.GetTopLevel(this);
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Profile Image",
                    AllowMultiple = false,
                    FileTypeFilter = new[] 
                    { 
                        new FilePickerFileType("Image Files") 
                        { 
                            Patterns = new[] { "*.jpg", "*.png", "*.jpeg", "*.webp" },
                            MimeTypes = new[] { "image/jpeg", "image/png", "image/webp" }
                        }
                    }
                });

                if (files != null && files.Count > 0)
                {
                    // DialogHost kullanımı
                    settingsPage.ProgressReport.IsVisible = true;
                    settingsPage.StackPanelError.IsVisible = false;
                    settingsPage.StackPanelChangePassword.IsVisible = false;
                    settingsPage.StackPanelChangeUsername.IsVisible = false;
                    
                    settingsPage.MainDialogHost.IsOpen = true;
                    
                    settingsPage.ProgressInfo.Text = App.Current.Resources["Compressing"]?.ToString();
                    
                    // Asenkron işlemi farklı bir thread'de çalıştır
                    await Task.Run(async () => 
                    {
                        await Compress(files[0].Path.LocalPath);
                        
                        if (!String.IsNullOrWhiteSpace(newImagePath))
                        {
                            // UI thread'e geri dön
                            await Dispatcher.UIThread.InvokeAsync(() => 
                            {
                                settingsPage.ProgressInfo.Text = App.Current.Resources["Uploading"]?.ToString();
                            });
                            
                            var result = await FirestoreManager.ChangeProfilePhoto(newImagePath,
                                AppSettingsManager.appSettings.FireStoreEmail + ".jpg");
                                
                            if (result.Success)
                            {
                                // Success notification - Avalonia'da bir şekilde bildirim gösterimi
                                // (Avalonia'da doğrudan Growl eşdeğeri yok, bir mesaj gösterme mekanizması kullanılabilir)
                                
                                AppSettingsManager.appSettings.FireStoreProfilePhotoName =
                                    AppSettingsManager.appSettings.FireStoreEmail + ".jpg";
                                AppSettingsManager.SaveAppSettings();
                                
                                // UI thread'e geri dön
                                await Dispatcher.UIThread.InvokeAsync(async () => 
                                {
                                   settingsPage.MainDialogHost.IsOpen = false;
                                    FirestoreManager.MyProfilePhoto = null;
                                    if (Gravatar is Image imageControl)
                                    {
                                        var bitmap = await FirestoreManager.DownloadProfilePhoto(
                                            AppSettingsManager.appSettings.FireStoreProfilePhotoName, true);
                                        imageControl.Source = bitmap;
                                    }
                                    File.Delete(newImagePath);
                                });
                            }
                            else
                            {
                                // UI thread'e geri dön
                                await Dispatcher.UIThread.InvokeAsync(() => 
                                {
                                    settingsPage.ProgressReport.IsVisible = false;
                                    settingsPage.StackPanelError.IsVisible = true;
                                    settingsPage.StackPanelChangePassword.IsVisible = false;
                                    settingsPage.StackPanelChangeUsername.IsVisible = false;
                                    settingsPage.TextBlockDialogText.Text = result.ErrorMessage;
                                });
                            }
                        }
                    });
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonChangePassword_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            settingsPage.MainDialogHost.IsOpen = true;
            settingsPage.ProgressReport.IsVisible = false;
            settingsPage.StackPanelError.IsVisible = false;
            settingsPage.StackPanelChangePassword.IsVisible = true;
            settingsPage.StackPanelChangeUsername.IsVisible = false;
        }

        private void ButtonChangeUsername_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            settingsPage.MainDialogHost.IsOpen = true;
            settingsPage.ProgressReport.IsVisible = false;
            settingsPage.StackPanelError.IsVisible = false;
            settingsPage.StackPanelChangePassword.IsVisible = false;
            settingsPage.StackPanelChangeUsername.IsVisible = true;
        }

        private void ButtonChangeSignOut_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                App.isClosing = false;
                var loginWindow = new LoginPage(false);
                MainWindow.Instance.SetContent(loginWindow);
                AppSettingsManager.appSettings.SignedOut = true;
                AppSettingsManager.SaveAppSettings();
                
                // Avalonia'da pencere kapama
                if (this.VisualRoot is Window window)
                {
                    window.Close();
                }
                App.isClosing = true;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonRenew_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
           var renew = new SubPlansPage( false, this);
           MainWindow.Instance.SetContent(renew);
        }

        private async void MainAccountPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Eğer bir dinleyici varsa durdurma
            // await FirestoreManager.lisUsers.StopAsync();
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
        private bool isSmallScreen = false;
        private bool isExtraSmallScreen = false;

        // Threshold for small screen detection
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;
   
        
        private void AdjustLayoutForScreenSize(double width)
        {
            try
            {
                // Çok küçük ekranlar için (450px'den küçük), kullanıcı profili ve butonları yeniden düzenle
                if (isExtraSmallScreen)
                {
                    // Kullanıcı profili bileşenlerini dikey olarak düzenle
                    UserProfileStackPanel.Orientation = Avalonia.Layout.Orientation.Vertical;
                    StackPanelTextUsernameEmail.Margin = new Thickness(-10, BorderGravatar.Height/5.1, 0, 0);
                    
                    // Buton container'ı dikey yap
                    ButtonsContainerStackPanel.Orientation = Avalonia.Layout.Orientation.Vertical;
                    ProfileButtonsStackPanel.Margin = new Thickness(-10, 25, 0, 0);
                    SignOutButtonStackPanel.Margin = new Thickness(-10, 15, 0, 0);
                    
                    // Abonelik bilgisi için dikey düzen
                    SubscriptionStackPanel.Orientation = Avalonia.Layout.Orientation.Vertical;
                    ExpireTextBlock.Margin = new Thickness(5, 5, 0, 0);
                    ButtonRenew.Margin = new Thickness(5, 15, 0, 20);
                    SubscriptionText.Margin = new Thickness(5, 5, 0, 0);
                }
                // Küçük ekranlar için (750px'den küçük)
                else if (isSmallScreen)
                {
                    // Kullanıcı profili bileşenlerini yatay tut ama ayarla
                    UserProfileStackPanel.Orientation = Avalonia.Layout.Orientation.Horizontal;
                    StackPanelTextUsernameEmail.Margin = new Thickness(-5, BorderGravatar.Height/4.8, 0, 0);
                    
                    // Buton container'ını dikey yap
                    ButtonsContainerStackPanel.Orientation = Avalonia.Layout.Orientation.Vertical;
                    ProfileButtonsStackPanel.Margin = new Thickness(25, 0, 0, 0);
                    SignOutButtonStackPanel.Margin = new Thickness(25, 15, 0, 0);
                    
                    // Abonelik bilgisi için yatay düzen
                    SubscriptionStackPanel.Orientation = Avalonia.Layout.Orientation.Horizontal;
                    ExpireTextBlock.Margin = new Thickness(5, 0, 0, 0);
                    ButtonRenew.Margin = new Thickness(15, -5, 0, 20);
                    SubscriptionText.Margin = new Thickness(0, 0, 0, 0);
                }
                // Normal ekranlar için (750px'den büyük)
                else
                {
                    // Her şeyi varsayılan haline geri getir
                    UserProfileStackPanel.Orientation = Avalonia.Layout.Orientation.Horizontal;
                    StackPanelTextUsernameEmail.Margin = new Thickness(0, BorderGravatar.Height/4.4, 0, 0);
                    
                    // Buton container'ını yatay tut
                    ButtonsContainerStackPanel.Orientation = Avalonia.Layout.Orientation.Horizontal;
                    ProfileButtonsStackPanel.Margin = new Thickness(100, 0, 0, 0);
                    SignOutButtonStackPanel.Margin = new Thickness(100, 0, 0, 0);
                    
                    // Abonelik bilgisi için yatay düzen
                    SubscriptionStackPanel.Orientation = Avalonia.Layout.Orientation.Horizontal;
                    ExpireTextBlock.Margin = new Thickness(5, 0, 0, 0);
                    ButtonRenew.Margin = new Thickness(30, -7, 0, 20);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"AdjustLayoutForScreenSize Error: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private double CalculateTextSize(double width, double minSize, double maxSize)
        {
            return CalculateScaledValue(width, minSize, maxSize);
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
        private double CalculateGravatarWidthHeight(double width)
        {
            return CalculateScaledValue(width, 50, 200);
        }

        private void Control_OnLoaded(object? sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            double width = e.width;
            
            isSmallScreen = width <= SMALL_SCREEN_THRESHOLD;
            isExtraSmallScreen = width <= EXTRA_SMALL_SCREEN_THRESHOLD;

            var a = CalculateGravatarWidthHeight(width);
            BorderGravatar.Width = a;
            BorderGravatar.Height = a;
            BorderGravatar.CornerRadius = new CornerRadius(a/2);

            TextBlockAccount.FontSize = CalculateTextSize(width, 20, 40);
            TextBlockUsername.FontSize = CalculateTextSize(width, 18, 32);
            EmailTextBlock.FontSize = CalculateTextSize(width, 14, 23);
            StackPanelTextUsernameEmail.Margin = new Thickness(0,a/4.4,0,0);
            
            // Buton boyutlarını ve fontlarını güncelle
            var buttonFontSize = CalculateTextSize(width, 12, 18);
            ButtonChangeProfileImage.FontSize = buttonFontSize;
            ButtonChangePassword.FontSize = buttonFontSize;
            ButtonChangeUsername.FontSize = buttonFontSize;
            ButtonChangeSignOut.FontSize = buttonFontSize;
            ButtonRenew.FontSize = buttonFontSize;
            
            // Abonelik metni boyutunu güncelle
            SubscriptionText.FontSize = CalculateTextSize(width, 14, 20);
            ExpireTextBlock.FontSize = CalculateTextSize(width, 14, 20);
            
            AdjustLayoutForScreenSize(width);
        }
    }
} 