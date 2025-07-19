using System;
using System.Collections.Generic;
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
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private bool fromSignUpPage = false;
        public LoginPage(bool fromSignUpPage)
        {
            InitializeComponent();
            this.fromSignUpPage = fromSignUpPage;
            Log.Information("Loaded Login page");
            if (!AppSettingsManager.appSettings.Verified)
            {
                if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.FireStoreEmail) &&
                    !String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.FireStorePassword))
                {
                    TextBoxEmail.Text = AppSettingsManager.appSettings.FireStoreEmail;
                    TextBoxPassword.Password = AppSettingsManager.appSettings.FireStorePassword;
                }
            }

            Load();
        }

        private async void Load()
        {
            try
            {
                if (fromSignUpPage)
                {
                    BtnSignUp.IsEnabled = true;
                }
                else
                {
                    var signedBefore = await FirestoreManager.IsComputerSignedUpBefore();
                    BtnSignUp.IsEnabled = !signedBefore;
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnSignUp_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (this.NavigationService.CanGoBack)
                {
                    this.NavigationService.GoBack();
                }
                else
                {
                    this.NavigationService.Navigate(new SignUpPage());
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnSignIn_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(TextBoxEmail.Text) && String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    DialogHost.IsOpen = true;
                    StackPanelError.Visibility = Visibility.Visible;
                    StackPanelResetPassword.Visibility = Visibility.Collapsed;
                    TextBlockDialogText.Text = App.Current.Resources["EmailPasswordErrorString"].ToString();
                }
                else if (String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         !String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    DialogHost.IsOpen = true;
                    StackPanelError.Visibility = Visibility.Visible;
                    StackPanelResetPassword.Visibility = Visibility.Collapsed;
                    TextBlockDialogText.Text = App.Current.Resources["EmailErrorString"].ToString();
                }
                else if (!String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    DialogHost.IsOpen = true;
                    StackPanelError.Visibility = Visibility.Visible;
                    StackPanelResetPassword.Visibility = Visibility.Collapsed;
                    TextBlockDialogText.Text = App.Current.Resources["PasswordErrorString"].ToString();
                }
                else
                {
                    var result = await FirestoreManager.SignIn(TextBoxEmail.Text, TextBoxPassword.Password );
                    if (result != null)
                    {
                        if (result.Success)
                        {
                            AppSettingsManager.appSettings.Verified = true;
                            AppSettingsManager.appSettings.SignedOut = false;
                            AppSettingsManager.appSettings.FireStoreEmail = TextBoxEmail.Text;
                            AppSettingsManager.appSettings.FireStorePassword = TextBoxPassword.Password;
                            AppSettingsManager.appSettings.FireStoreDisplayName = result.DisplayName;
                            AppSettingsManager.appSettings.FireStoreProfilePhotoName =
                                AppSettingsManager.appSettings.FireStoreEmail + ".jpg";
                            AppSettingsManager.SaveAppSettings();
                            var loginResult = await FirestoreManager.IsValidLogin();
                            if (loginResult.Success)
                            {
                                var mainWindow = new MainWindow();
                                mainWindow.Show();
                                var wnd = Window.GetWindow(this);
                                if (wnd != null)
                                {
                                    wnd.Close();
                                }
                                Log.Information("Opened Main Window");
                            }
                            else
                            {
                                if (loginResult.ErrorType == ErrorType.Expired)
                                {
                                    SubPlansPage subPlansPage = new SubPlansPage(true,false);
                                    subPlansPage.Show();
                                    new CustomMessageBox(loginResult.ErrorMessage, MessageType.Error, MessageButtons.Ok).ShowDialog();
                                }
                                else if (loginResult.ErrorType == ErrorType.Hwid)
                                {
                                    new CustomMessageBox(loginResult.ErrorMessage, MessageType.Error, MessageButtons.Ok).ShowDialog();
                                    var changeHwid = new ChangeHwidWindow();
                                    changeHwid.ShowDialog();
                                    changeHwid.Unloaded += (o, args) =>
                                    {
                                        if (changeHwid.changedMachine)
                                        {
                                            var wnd = Window.GetWindow(this);
                                            if (wnd != null)
                                            {
                                                wnd.Close();
                                            }
                                        }
                                    };
                                }
                                else if (loginResult.ErrorType == ErrorType.UserNotFound)
                                {
                                    new SubPlansPage(true, true).Show();
                                    var wnd = Window.GetWindow(this);
                                    if (wnd != null)
                                    {
                                        wnd.Close();
                                    }
                                }
                            }
                        }
                        else
                        {
                            DialogHost.IsOpen = true;
                            StackPanelError.Visibility = Visibility.Visible;
                            StackPanelResetPassword.Visibility = Visibility.Collapsed;
                            TextBlockDialogText.Text = result.ErrorMessage;
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


        private void BtnCloseDialog_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (isInPasswordReset)
                {
                    StackPanelError.Visibility = Visibility.Collapsed;
                    StackPanelResetPassword.Visibility = Visibility.Visible;
                }
                else
                {
                    DialogHost.IsOpen = false;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

       


        private void ResetPasswordButton_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ResetPasswordButton.TextDecorations = System.Windows.TextDecorations.Underline;
        }

        private void ResetPasswordButton_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ResetPasswordButton.TextDecorations = null;
        }

        private async void ResetPasswordButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StackPanelError.Visibility = Visibility.Collapsed;
            StackPanelResetPassword.Visibility = Visibility.Visible;
            DialogHost.IsOpen = true;
           
        }
        bool isInPasswordReset=false;
        private async void BtnResetPassword_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                isInPasswordReset = true;
                if (String.IsNullOrWhiteSpace(TextBoxEmailForgotPassword.Text))
                {
                    StackPanelError.Visibility = Visibility.Visible;
                    StackPanelResetPassword.Visibility = Visibility.Collapsed;
                    TextBlockDialogText.Text = App.Current.Resources["EmailErrorString"].ToString();
                }
                else
                {
                    var result = await FirestoreManager.ResetPassword(TextBoxEmailForgotPassword.Text);
                    if (result != null)
                    {
                        if (result.Success)
                        {
                            StackPanelError.Visibility = Visibility.Visible;
                            StackPanelResetPassword.Visibility = Visibility.Collapsed;
                            TextBlockDialogText.Text = result.ErrorMessage;
                            isInPasswordReset = false;
                            TextBoxEmailForgotPassword.Text = "";
                        }
                        else
                        {
                            StackPanelError.Visibility = Visibility.Visible;
                            StackPanelResetPassword.Visibility = Visibility.Collapsed;
                            TextBlockDialogText.Text = result.ErrorMessage;
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

        private void BtnCancel_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DialogHost.IsOpen = false;
            isInPasswordReset = false;
            TextBoxEmailForgotPassword.Text = "";
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
            TextBoxPassword.BorderBrush = new SolidColorBrush(Color.FromRgb( 73, 73, 73));
        }

        private void TextBoxPassword_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSignIn_OnPreviewMouseLeftButtonDown(sender, null);
            }
        }

        private async void BtnSendVerificationAgain(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var result = await FirestoreManager.SendEmailVerification(AppSettingsManager.appSettings.FireStoreEmail,
                    AppSettingsManager.appSettings.FireStorePassword);
                if (result.Success)
                {
                    new CustomMessageBox(App.Current.Resources["EmailVerificationSent"].ToString(), MessageType.Info, MessageButtons.Ok)
                        .ShowDialog();
                }
                else
                {
                    new CustomMessageBox(result.ErrorMessage, MessageType.Error, MessageButtons.Ok).ShowDialog();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
    }
}
