using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Serilog;
using TinifyAPI;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for SignUpPage.xaml
    /// </summary>
    public partial class SignUpPage : Page
    {
        public SignUpPage()
        {
            InitializeComponent();
            Log.Information("Loaded sign up page");
        }

        private void UIElement_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(this.NavigationService != null)
                this.NavigationService.Navigate(new LoginPage(true));
        }

        private async void BtnSignUp_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) && !String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                    !String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = App.Current.Resources["DisplayNameError"].ToString();
                }
                else if (!String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         !String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = App.Current.Resources["EmailErrorString"].ToString();
                }
                else if(!String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                        !String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                        String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = App.Current.Resources["PasswordErrorString"].ToString();
                }
                else if (String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         !String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = App.Current.Resources["DisplayEmailError"].ToString();
                }
                else if (!String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = App.Current.Resources["EmailPasswordErrorString"].ToString();
                }
                else if (String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                         !String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text =App.Current.Resources["DisplayPasswordError"].ToString();
                }
                else if (String.IsNullOrWhiteSpace(TextBoxDisplayName.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxEmail.Text) &&
                         String.IsNullOrWhiteSpace(TextBoxPassword.Password))
                {
                    DialogHost.IsOpen = true;
                    TextBlockDialogText.Text = App.Current.Resources["EmailPassworDisplayNameError"].ToString();
                }
                else
                {
                    var result = await FirestoreManager.SignUp(TextBoxEmail.Text,TextBoxPassword.Password,  TextBoxDisplayName.Text );
                    if (result != null)
                    {
                        if (result.Success)
                        {
                            AppSettingsManager.appSettings.FireStoreEmail = TextBoxEmail.Text;
                            AppSettingsManager.appSettings.FireStorePassword = TextBoxPassword.Password;
                            AppSettingsManager.appSettings.FireStoreDisplayName = TextBoxDisplayName.Text;
                            AppSettingsManager.appSettings.SignedOut = false;
                            AppSettingsManager.appSettings.FireStoreProfilePhotoName =
                                AppSettingsManager.appSettings.FireStoreEmail + ".jpg";
                            AppSettingsManager.SaveAppSettings();;
                            StackPanelError.Visibility = Visibility.Collapsed;
                            UploadProfilePhoto.Visibility = Visibility.Visible;
                            ProgressReport.Visibility = Visibility.Collapsed;
                            AssingProfilePhoto();
                            DialogHost.IsOpen = true;
                            await FirestoreManager.Register(new SubPlan() { PlanName = "lifetime" });
                        }
                        else
                        {
                            StackPanelError.Visibility = Visibility.Visible;
                            UploadProfilePhoto.Visibility = Visibility.Collapsed;
                            ProgressReport.Visibility = Visibility.Collapsed;
                            DialogHost.IsOpen = true;
                            TextBlockDialogText.Text = result.ErrorMessage;
                            //closeDialog = true;
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
            //if(closeDialog)
                DialogHost.IsOpen = false;
                if (verification)
                {
                    if (this.NavigationService != null)
                    {
                        this.NavigationService.Navigate(new LoginPage(false));
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
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = profilePhoto;
                bitmap.EndInit();
                bitmap.Freeze();
                ProfilePhoto.Source = bitmap;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ButtonKeepIt_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var result = await FirestoreManager.UploadProfilePhoto(profilePhoto, AppSettingsManager.appSettings.FireStoreEmail + ".jpg");
                if (result!=null&&result.Success)
                {
                    AppSettingsManager.appSettings.FireStoreProfilePhotoName = AppSettingsManager.appSettings.FireStoreEmail + ".jpg";
                    AppSettingsManager.SaveAppSettings();;
                    ProgressReport.Visibility = Visibility.Collapsed;
                    StackPanelError.Visibility = Visibility.Visible;
                    UploadProfilePhoto.Visibility = Visibility.Collapsed;
                    TextBlockDialogText.Text = App.Current.Resources["EmailVerificationNeededString"].ToString();
                    AppSettingsManager.appSettings.Verified = false;
                    AppSettingsManager.SaveAppSettings();;
                    verification = true;
              
               
                    //timer = new DispatcherTimer();
                    //timer.Interval = TimeSpan.FromSeconds(1);
                    //timer.Tick += TimerOnTick;
                    //timer.Start();
                    //closeDialog = false;
                    BtnSendVerification.Visibility = Visibility.Visible;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        //bool closeDialog = true;

        private DispatcherTimer timer;
        bool verification=false;
        private async void ButtonChangeProfilePhoto_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (selected && !String.IsNullOrWhiteSpace(Uncompresspath))
                {
                    ProgressReport.Visibility = Visibility.Visible;
                    StackPanelError.Visibility = Visibility.Collapsed;
                    UploadProfilePhoto.Visibility = Visibility.Collapsed;
                    ProgressInfo.Text = App.Current.Resources["Compressing"].ToString();
                    await Compress(Uncompresspath).ContinueWith((Action<Task>)(async task =>
                    {
                        if (!String.IsNullOrWhiteSpace(newImagePath))
                        {
                            await Application.Current.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(() =>
                                {
                                    ProgressInfo.Text = App.Current.Resources["Uploading"].ToString();
                                }));
                       
                            var result = await FirestoreManager.UploadProfilePhoto(newImagePath,
                                AppSettingsManager.appSettings.FireStoreEmail + ".jpg");
                            if (result != null && result.Success)
                            {
                                AppSettingsManager.appSettings.FireStoreProfilePhotoName =
                                    AppSettingsManager.appSettings.FireStoreEmail + ".jpg";
                                AppSettingsManager.SaveAppSettings();;
                                await Application.Current.Dispatcher.BeginInvoke(
                                    DispatcherPriority.Background,
                                    new Action(() =>
                                    {
                                        ProgressReport.Visibility = Visibility.Collapsed;
                                        StackPanelError.Visibility = Visibility.Visible;
                                        UploadProfilePhoto.Visibility = Visibility.Collapsed;
                                        TextBlockDialogText.Text = App.Current.Resources["EmailVerificationNeededString"].ToString();
                                        AppSettingsManager.appSettings.Verified = false;
                                        AppSettingsManager.SaveAppSettings();;

                                        verification = true;
                                    
                                        //timer = new DispatcherTimer();
                                        //timer.Interval=TimeSpan.FromSeconds(1);
                                        //timer.Tick += TimerOnTick;
                                        //timer.Start();
                                        //closeDialog = false;
                                        BtnSendVerification.Visibility = Visibility.Visible;
                                    }));
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

        //private async void TimerOnTick(object? sender, EventArgs e)
        //{
        //    var signInResult = await FirestoreManager.SignIn(AppSettingsManager.appSettings.FireStoreEmail,
        //        AppSettingsManager.appSettings.FireStorePassword);
        //    if (signInResult.Success)
        //    {
        //        timer.Stop();
        //        timer.Tick -= TimerOnTick;
        //        SubPlansPage subPlansPage = new SubPlansPage(true, true);
        //        subPlansPage.Show();
        //        var wnd = Window.GetWindow(this);
        //        if (wnd != null)
        //        {
        //            wnd.Close();
        //        }
        //    }
        //}

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

        private void ButtonSelect_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog()
                {
                    Multiselect = false,
                    Filter = "(*.jpg,*.png,*.jpeg,*.webp)|*.jpg;*.png;*.jpeg;*.webp;",
                    CheckFileExists = true
                };
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Uncompresspath = openFileDialog.FileName;
                    ProfilePhoto.Source = new BitmapImage(new Uri(Uncompresspath));
                    selected = true;
                    ButtonChangeProfilePhoto.IsEnabled = true;
                    ButtonKeepIt.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
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
            TextBoxPassword.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb( 73, 73, 73));
        }


        private void SignUpPage_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSignUp_OnPreviewMouseLeftButtonDown(sender, null);
            }
        }

        private async void BtnSendVerificationAgain(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var result =await FirestoreManager.SendEmailVerification(AppSettingsManager.appSettings.FireStoreEmail,
                    AppSettingsManager.appSettings.FireStorePassword);
                if (result.Success)
                {
                    new CustomMessageBox(App.Current.Resources["EmailVerificationSent"].ToString(), MessageType.Info, MessageButtons.Ok)
                        .ShowDialog();
                }
                else
                {
                    new CustomMessageBox(result.ErrorMessage,MessageType.Error,MessageButtons.Ok).ShowDialog();
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
