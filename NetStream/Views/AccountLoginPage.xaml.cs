using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for AccountLoginPage.xaml
    /// </summary>
    public partial class AccountLoginPage : Page
    {
        public AccountLoginPage()
        {
            InitializeComponent();
        }

        private void AccountLoginPage_OnLoaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void AccountLoginPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
           
        }

        private async void UIElement_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(TextBoxUsername.Text) && !String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    TextBlockError.Visibility = Visibility.Visible;
                    TextBlockError.Text = App.Current.Resources["UsernameError"].ToString();
                }
                else if (!String.IsNullOrWhiteSpace(TextBoxUsername.Text) && String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    TextBlockError.Visibility = Visibility.Visible;
                    TextBlockError.Text = App.Current.Resources["PasswordErrorString"].ToString();
                }
                else if (String.IsNullOrWhiteSpace(TextBoxUsername.Text) && String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    TextBlockError.Visibility = Visibility.Visible;
                    TextBlockError.Text = App.Current.Resources["UsernamePasswordError"].ToString();
                }
                else
                {
                    TextBlockError.Visibility =Visibility.Collapsed;
                    var loginResult = await Service.Login(this, TextBoxUsername.Text, TextBoxPassword.Password);

                    if (loginResult)
                    {
                        var accountPage = new AccountPage();
                        AccountPage.GetAccountPageInstance = accountPage;
                        if(this.NavigationService != null)
                            this.NavigationService.Navigate(accountPage);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonSignUp_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://www.themoviedb.org/signup") { UseShellExecute = true });
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void TextBoxPassword_OnMouseEnter(object sender, MouseEventArgs e)
        {
            
        }

        private void TextBoxPassword_OnGotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                Color current = new Color();
                current.A = (byte)AppSettingsManager.appSettings.PrimaryColorAlpha;
                current.R = (byte)AppSettingsManager.appSettings.PrimaryColorRed;
                current.G = (byte)AppSettingsManager.appSettings.PrimaryColorGreen;
                current.B = (byte)AppSettingsManager.appSettings.PrimaryColorBlue;
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
            TextBoxPassword.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 73, 73, 73));
        }
    }
}
