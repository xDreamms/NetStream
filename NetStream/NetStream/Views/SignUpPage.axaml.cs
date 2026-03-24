using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Serilog;
using TinifyAPI;

namespace NetStream.Views
{
    public partial class SignUpPage : UserControl
    {
        public SignUpPage()
        {
            InitializeComponent();
        }

        private void UIElement_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            MainWindow.Instance.SetContent(new LoginPage(true));
        }

        private async void BtnSignUp_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) && !String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                    !String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("DisplayNameError");
                }
                else if (!String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         !String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("EmailErrorString");
                }
                else if(!String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                        !String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                        String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("PasswordErrorString");
                }
                else if (String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         !String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("DisplayEmailError");
                }
                else if (!String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("EmailPasswordErrorString");
                }
                else if (String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                         !String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("DisplayPasswordError");
                }
                else if (String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxPassword.Text))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("EmailPassworDisplayNameError");
                }
                else
                {
                    var result = await FirestoreManager.SignUp(TextBoxEmail.Text, TextBoxPassword.Text, TextBoxDisplayName.Text);
                    if (result != null)
                    {
                        if (result.Success)
                        {
                            AppSettingsManager.appSettings.FireStoreEmail = TextBoxEmail.Text;
                            AppSettingsManager.appSettings.FireStorePassword = TextBoxPassword.Text;
                            AppSettingsManager.appSettings.FireStoreDisplayName = TextBoxDisplayName.Text;
                            AppSettingsManager.appSettings.SignedOut = false;
                            AppSettingsManager.appSettings.FireStoreProfilePhotoName =
                                AppSettingsManager.appSettings.FireStoreEmail + ".jpg";
                            AppSettingsManager.SaveAppSettings();
                            StackPanelError.IsVisible = false;
                            UploadProfilePhoto.IsVisible = true;
                            ProgressReport.IsVisible = false;
                            AssingProfilePhoto();
                            DialogHost.IsOpen = true;
                            await FirestoreManager.Register(new SubPlan() { PlanName = "lifetime" });
                        }
                        else
                        {
                            StackPanelError.IsVisible = true;
                            UploadProfilePhoto.IsVisible = false;
                            ProgressReport.IsVisible = false;
                            DialogHost.IsOpen = true;
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
            DialogHost.IsOpen = false;
            if (verification)
            {
                var parent = this.Parent;
                if (parent is ContentControl contentControl)
                {
                    contentControl.Content = new LoginPage(false);
                }
            }
        }

        private MemoryStream profilePhoto;
        private void AssingProfilePhoto()
        {
            try
            {
                var displayName = AppSettingsManager.appSettings.FireStoreDisplayName;
                var list = displayName.Split(" ");
                profilePhoto = FirestoreManager.GenerateCircle(list.First(), list.Length == 1 ? "" : list.Last());
                var bitmap = new Bitmap(profilePhoto);
                ProfilePhoto.Source = bitmap;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ButtonKeepIt_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var result = await FirestoreManager.UploadProfilePhoto(profilePhoto, AppSettingsManager.appSettings.FireStoreEmail + ".jpg");
                if (result != null && result.Success)
                {
                    AppSettingsManager.appSettings.FireStoreProfilePhotoName = AppSettingsManager.appSettings.FireStoreEmail + ".jpg";
                    AppSettingsManager.SaveAppSettings();
                    ProgressReport.IsVisible = false;
                    StackPanelError.IsVisible = true;
                    UploadProfilePhoto.IsVisible = false;
                    TextBlockDialogText.Text = global::ResourceProvider.GetString("EmailVerificationNeededString");
                    AppSettingsManager.appSettings.Verified = false;
                    AppSettingsManager.SaveAppSettings();
                    verification = true;
                    BtnSendVerification.IsVisible = true;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        bool verification = false;
        
        private async void ButtonChangeProfilePhoto_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                if (selected && !String.IsNullOrWhiteSpace(Uncompresspath))
                {
                    ProgressReport.IsVisible = true;
                    StackPanelError.IsVisible = false;
                    UploadProfilePhoto.IsVisible = false;
                    ProgressInfo.Text = global::ResourceProvider.GetString("Compressing");
                    
                    await Compress(Uncompresspath).ContinueWith((Action<Task>)(async task =>
                    {
                        if (!String.IsNullOrWhiteSpace(newImagePath))
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                ProgressInfo.Text = global::ResourceProvider.GetString("Uploading");
                            });
                       
                            var result = await FirestoreManager.UploadProfilePhoto(newImagePath,
                                AppSettingsManager.appSettings.FireStoreEmail + ".jpg");
                                
                            if (result != null && result.Success)
                            {
                                AppSettingsManager.appSettings.FireStoreProfilePhotoName =
                                    AppSettingsManager.appSettings.FireStoreEmail + ".jpg";
                                AppSettingsManager.SaveAppSettings();
                                
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    ProgressReport.IsVisible = false;
                                    StackPanelError.IsVisible = true;
                                    UploadProfilePhoto.IsVisible = false;
                                    TextBlockDialogText.Text = global::ResourceProvider.GetString("EmailVerificationNeededString");
                                    AppSettingsManager.appSettings.Verified = false;
                                    AppSettingsManager.SaveAppSettings();
                                    verification = true;
                                    BtnSendVerification.IsVisible = true;
                                });
                                
                                File.Delete(newImagePath);
                            }
                        }
                    }));
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
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

        private string Uncompresspath;
        private bool selected = false;

        private async void ButtonSelect_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var fileDialog = new OpenFileDialog
                {
                    AllowMultiple = false,
                    Filters = new System.Collections.Generic.List<FileDialogFilter>
                    {
                        new FileDialogFilter
                        {
                            Name = "Image Files",
                            Extensions = new System.Collections.Generic.List<string> { "jpg", "jpeg", "png", "webp" }
                        }
                    }
                };
                
                var result = await fileDialog.ShowAsync(this.GetVisualRoot() as Window);
                if (result != null && result.Length > 0)
                {
                    Uncompresspath = result[0];
                    ProfilePhoto.Source = new Bitmap(Uncompresspath);
                    selected = true;
                    ButtonChangeProfilePhoto.IsEnabled = true;
                    ButtonKeepIt.IsVisible = false;
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

        private void SignUpPage_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSignUp_OnPreviewMouseLeftButtonDown(sender, null);
            }
        }

        private async void BtnSendVerificationAgain(object? sender, RoutedEventArgs routedEventArgs)
        {
            /*try
            {
                var result = await FirestoreManager.SendEmailVerification(AppSettingsManager.appSettings.FireStoreEmail,
                    AppSettingsManager.appSettings.FireStorePassword);
                if (result.Success)
                {
                    new CustomMessageBox(global::ResourceProvider.GetString("EmailVerificationSent"), MessageType.Info, MessageButtons.Ok)
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
            }*/
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
                TextBoxDisplayName.Width = 275;
                NetStreamLogo.Height = 100;
                TextWelcome.FontSize = 19;
                TextCreateAccount.FontSize = 12;
            }
            else if (with <= 400)
            {
                BtnSignIn.Width = 300;
                BtnSignUp.Width = 300;
                TextBoxEmail.Width = 300;
                TextBoxPassword.Width = 300;
                TextBoxDisplayName.Width = 300;
                NetStreamLogo.Height = 100;
                TextWelcome.FontSize = 19;
                TextCreateAccount.FontSize = 12;
            }
            else if (with <= 450)
            {
                BtnSignIn.Width = 350;
                BtnSignUp.Width = 350;
                TextBoxEmail.Width = 350;
                TextBoxPassword.Width = 350;
                TextBoxDisplayName.Width = 350;
                NetStreamLogo.Height = 125;
                TextWelcome.FontSize = 22;
                TextCreateAccount.FontSize = 14;
            }
            else if (with <= 500)
            {
                BtnSignIn.Width = 400;
                BtnSignUp.Width = 400;
                TextBoxEmail.Width = 400;
                TextBoxPassword.Width = 400;
                TextBoxDisplayName.Width = 400;
                NetStreamLogo.Height = 125;
                TextWelcome.FontSize = 22;
                TextCreateAccount.FontSize = 14;
            }
            else if (with <= 600)
            {
                BtnSignIn.Width = 450;
                BtnSignUp.Width = 450;
                TextBoxEmail.Width = 450;
                TextBoxPassword.Width = 450;
                TextBoxDisplayName.Width = 450;
                NetStreamLogo.Height = 125;
                TextWelcome.FontSize = 22;
                TextCreateAccount.FontSize = 14;
            }
            else if (with <= 1200)
            {
                BtnSignIn.Width = 500;
                BtnSignUp.Width = 500;
                TextBoxEmail.Width = 500;
                TextBoxPassword.Width = 500;
                TextBoxDisplayName.Width = 500;
                NetStreamLogo.Height = 135;
                TextWelcome.FontSize = 25;
                TextCreateAccount.FontSize = 16;
            }
            else
            {
                BtnSignIn.Width = 550;
                BtnSignUp.Width = 550;
                TextBoxEmail.Width = 550;
                TextBoxPassword.Width = 550;
                TextBoxDisplayName.Width = 550;
                NetStreamLogo.Height = 150;
                TextWelcome.FontSize = 28;
                TextCreateAccount.FontSize = 17;
            }
        }
    }
} 