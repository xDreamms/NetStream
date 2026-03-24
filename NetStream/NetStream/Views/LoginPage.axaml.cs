using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Serilog;

namespace NetStream.Views
{
    public partial class LoginPage : UserControl
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
                    TextBoxPassword.Text = AppSettingsManager.appSettings.FireStorePassword;
                }
            }

            Load();
        }
        
        public LoginPage()
        {
            InitializeComponent();
            Load();
        }

        private async void Load()
        {
            
        }

        private void BtnSignUp_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                MainWindow.Instance.SetContent(new SignUpPage());
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnSignIn_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(TextBoxEmail.Text) && String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                {
                    DialogHost.IsOpen = true;
                    StackPanelError.IsVisible = true;
                    StackPanelResetPassword.IsVisible = false;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("EmailPasswordErrorString");
                }
                else if (String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         !String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                {
                    DialogHost.IsOpen = true;
                    StackPanelError.IsVisible = true;
                    StackPanelResetPassword.IsVisible = false;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("EmailErrorString");
                }
                else if (!String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                {
                    DialogHost.IsOpen = true;
                    StackPanelError.IsVisible = true;
                    StackPanelResetPassword.IsVisible = false;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("PasswordErrorString");
                }
                else
                {
                    var result = await FirestoreManager.SignIn(TextBoxEmail.Text, TextBoxPassword.Text);
                    if (result != null)
                    {
                        if (result.Success)
                        {
                            AppSettingsManager.appSettings.Verified = true;
                            AppSettingsManager.appSettings.SignedOut = false;
                            AppSettingsManager.appSettings.FireStoreEmail = TextBoxEmail.Text;
                            AppSettingsManager.appSettings.FireStorePassword = TextBoxPassword.Text;
                            AppSettingsManager.appSettings.FireStoreDisplayName = result.DisplayName;
                            AppSettingsManager.appSettings.FireStoreProfilePhotoName = AppSettingsManager.appSettings.FireStoreEmail + ".jpg";
                            AppSettingsManager.SaveAppSettings();
                            
                            var loginResult = await FirestoreManager.IsValidLogin();
                            if (loginResult.Success)
                            {
                                MainView mainView = new MainView();
                                MainWindow.Instance.SetContent(mainView);
                            }
                            else
                            {
                                /*if (loginResult.ErrorType == ErrorType.Expired)
                                {
                                    var subPlansPage = new SubPlansPage(true, false);
                                    subPlansPage.Show();
                                    new CustomMessageBox(loginResult.ErrorMessage, MessageType.Error, MessageButtons.Ok).ShowDialog();
                                }
                                else if (loginResult.ErrorType == ErrorType.Hwid)
                                {
                                    new CustomMessageBox(loginResult.ErrorMessage, MessageType.Error, MessageButtons.Ok).ShowDialog();
                                    var changeHwid = new ChangeHwidWindow();
                                    changeHwid.Show();
                                    changeHwid.Closed += (o, args) =>
                                    {
                                        if (changeHwid.changedMachine)
                                        {
                                            var hostWindow = this.GetVisualRoot() as Window;
                                            if (hostWindow != null)
                                            {
                                                hostWindow.Close();
                                            }
                                        }
                                    };
                                }
                                else if (loginResult.ErrorType == ErrorType.UserNotFound)
                                {
                                    new SubPlansPage(true, true).Show();
                                    var hostWindow = this.GetVisualRoot() as Window;
                                    if (hostWindow != null)
                                    {
                                        hostWindow.Close();
                                    }
                                }*/
                            }
                        }
                        else
                        {
                            DialogHost.IsOpen = true;
                            StackPanelError.IsVisible = true;
                            StackPanelResetPassword.IsVisible = false;
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

        private void BtnCloseDialog_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                if (isInPasswordReset)
                {
                    StackPanelError.IsVisible = false;
                    StackPanelResetPassword.IsVisible = true;
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

        private void ResetPasswordButton_OnMouseEnter(object sender, PointerEventArgs e)
        {
            ResetPasswordButton.TextDecorations = TextDecorations.Underline;
        }

        private void ResetPasswordButton_OnMouseLeave(object sender, PointerEventArgs e)
        {
            ResetPasswordButton.TextDecorations = null;
        }

        private async void ResetPasswordButton_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            StackPanelError.IsVisible = false;
            StackPanelResetPassword.IsVisible = true;
            DialogHost.IsOpen = true;
        }
        
        bool isInPasswordReset = false;
        
        private async void BtnResetPassword_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                isInPasswordReset = true;
                if (String.IsNullOrWhiteSpace(TextBoxEmailForgotPassword.Text))
                {
                    StackPanelError.IsVisible = true;
                    StackPanelResetPassword.IsVisible = false;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("EmailErrorString");
                }
                else
                {
                    var result = await FirestoreManager.ResetPassword(TextBoxEmailForgotPassword.Text);
                    if (result != null)
                    {
                        if (result.Success)
                        {
                            StackPanelError.IsVisible = true;
                            StackPanelResetPassword.IsVisible = false;
                            TextBlockDialogText.Text = result.ErrorMessage;
                            isInPasswordReset = false;
                            TextBoxEmailForgotPassword.Text = "";
                        }
                        else
                        {
                            StackPanelError.IsVisible = true;
                            StackPanelResetPassword.IsVisible = false;
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

        private void BtnCancel_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            DialogHost.IsOpen = false;
            isInPasswordReset = false;
            TextBoxEmailForgotPassword.Text = "";
        }

        private void TextBoxPassword_OnGotFocus(object sender, GotFocusEventArgs e)
        {
            try
            {
                Color current = Color.FromArgb(
                    (byte)AppSettingsManager.appSettings.PrimaryColorAlpha,
                    (byte)AppSettingsManager.appSettings.PrimaryColorRed,
                    (byte)AppSettingsManager.appSettings.PrimaryColorGreen,
                    (byte)AppSettingsManager.appSettings.PrimaryColorBlue);
                
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
            TextBoxPassword.BorderBrush = new SolidColorBrush(Color.FromRgb(73, 73, 73));
        }

        private void TextBoxPassword_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSignIn_OnPreviewMouseLeftButtonDown(sender, null);
            }
        }

        private async void BtnSendVerificationAgain(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var result = await FirestoreManager.SendEmailVerification(AppSettingsManager.appSettings.FireStoreEmail,
                    AppSettingsManager.appSettings.FireStorePassword);
                if (result.Success)
                {
                    DialogHost.IsOpen = true;
                    StackPanelError.IsVisible = true;
                    StackPanelResetPassword.IsVisible = false;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("EmailVerificationSent");
                }
                else
                {
                    DialogHost.IsOpen = true;
                    StackPanelError.IsVisible = true;
                    StackPanelResetPassword.IsVisible = false;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("EmailVerificationSent");
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void Control_OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            var with = e.NewSize.Width;

            if (with <= 350)
            {
                BtnSignIn.Width = 275;
                BtnSignUp.Width = 275;
                TextBoxEmail.Width = 275;
                TextBoxPassword.Width = 275;
                NetStreamLogo.Height = 100;
                TextWelcomeBack.FontSize = 19;
                TextLoginExisting.FontSize = 12;
                ResetPasswordButton.FontSize = 12;
            }
            else if (with <= 400)
            {
                BtnSignIn.Width = 300;
                BtnSignUp.Width = 300;
                TextBoxEmail.Width = 300;
                TextBoxPassword.Width = 300;
                NetStreamLogo.Height = 100;
                TextWelcomeBack.FontSize = 19;
                TextLoginExisting.FontSize = 12;
                ResetPasswordButton.FontSize = 12;
            }
            else if (with <= 450)
            {
                BtnSignIn.Width = 350;
                BtnSignUp.Width = 350;
                TextBoxEmail.Width = 350;
                TextBoxPassword.Width = 350;
                NetStreamLogo.Height = 125;
                TextWelcomeBack.FontSize = 22;
                TextLoginExisting.FontSize = 14;
                ResetPasswordButton.FontSize = 14;
            }
            else if (with <= 500)
            {
                BtnSignIn.Width = 400;
                BtnSignUp.Width = 400;
                TextBoxEmail.Width = 400;
                TextBoxPassword.Width = 400;
                NetStreamLogo.Height = 125;
                TextWelcomeBack.FontSize = 22;
                TextLoginExisting.FontSize = 14;
                ResetPasswordButton.FontSize = 14;
            }
            else if (with <= 600)
            {
                BtnSignIn.Width = 450;
                BtnSignUp.Width = 450;
                TextBoxEmail.Width = 450;
                TextBoxPassword.Width = 450;
                NetStreamLogo.Height = 125;
                TextWelcomeBack.FontSize = 22;
                TextLoginExisting.FontSize = 14;
                ResetPasswordButton.FontSize = 14;
            }
            else if (with <= 1200)
            {
                BtnSignIn.Width = 500;
                BtnSignUp.Width = 500;
                TextBoxEmail.Width = 500;
                TextBoxPassword.Width = 500;
                NetStreamLogo.Height = 135;
                TextWelcomeBack.FontSize = 25;
                TextLoginExisting.FontSize = 16;
                ResetPasswordButton.FontSize = 15;
            }
            else
            {
                BtnSignIn.Width = 550;
                BtnSignUp.Width = 550;
                TextBoxEmail.Width = 550;
                TextBoxPassword.Width = 550;
                NetStreamLogo.Height = 150;
                TextWelcomeBack.FontSize = 28;
                TextLoginExisting.FontSize = 17;
                ResetPasswordButton.FontSize = 16;
            }
           
        }
    }
    
    public enum PageType
    {
        SignUp,
        SignIn
    }
} 