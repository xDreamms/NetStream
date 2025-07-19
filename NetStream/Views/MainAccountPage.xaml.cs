using HandyControl.Controls;
using HandyControl.Data;
using System;
using System.Collections.Generic;
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
using Path = System.IO.Path;
using Application = System.Windows.Application;
using Window = System.Windows.Window;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for MainAccountPage.xaml
    /// </summary>
    public partial class MainAccountPage : Page
    {
        private SettingsPage settingsPage;
        public MainAccountPage(SettingsPage settingsPage)
        {
            InitializeComponent();
            this.settingsPage = settingsPage;
            Load();
           
        }

        public MainAccountPage()
        {
            InitializeComponent();
            Load();

        }


        private async void Load()
        {
           await FirestoreManager.ListenUsers(this);

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

        private async void ButtonChangeProfileImage_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
                    settingsPage.ProgressReport.Visibility = Visibility.Visible;
                    settingsPage.StackPanelError.Visibility = Visibility.Collapsed;
                    settingsPage.StackPanelChangePassword.Visibility = Visibility.Collapsed;
                    settingsPage.StackPanelChangeUsername.Visibility = Visibility.Collapsed;
                    settingsPage.DialogHost.IsOpen = true;
                    settingsPage.ProgressInfo.Text = App.Current.Resources["Compressing"].ToString();
                    await Compress(openFileDialog.FileName).ContinueWith((Action<Task>)(async task =>
                    {
                        if (!String.IsNullOrWhiteSpace(newImagePath))
                        {
                            await Application.Current.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(() => { settingsPage.ProgressInfo.Text = App.Current.Resources["Uploading"].ToString(); }));
                            var result = await FirestoreManager.ChangeProfilePhoto(newImagePath,
                                AppSettingsManager.appSettings.FireStoreEmail + ".jpg");
                            if (result.Success)
                            {
                                Growl.SuccessGlobal(new GrowlInfo() { Message = App.Current.Resources["ProfilePhotoChangeMessage"].ToString(), WaitTime = 4, StaysOpen = false });
                                AppSettingsManager.appSettings.FireStoreProfilePhotoName =
                                    AppSettingsManager.appSettings.FireStoreEmail + ".jpg";
                                AppSettingsManager.SaveAppSettings();
                                await Application.Current.Dispatcher.BeginInvoke(
                                    DispatcherPriority.Background,
                                    new Action(async () =>
                                    {
                                        settingsPage.DialogHost.IsOpen = false;
                                        FirestoreManager.MyProfilePhoto = null;
                                        this.Gravatar.Source =
                                            await FirestoreManager.DownloadProfilePhoto(
                                                AppSettingsManager.appSettings.FireStoreProfilePhotoName, true);
                                        File.Delete(newImagePath);
                                    }));
                            
                            }
                            else
                            {
                                await Application.Current.Dispatcher.BeginInvoke(
                                    DispatcherPriority.Background,
                                    new Action(async () =>
                                    {
                                        settingsPage.ProgressReport.Visibility = Visibility.Collapsed;
                                        settingsPage.StackPanelError.Visibility = Visibility.Visible;
                                        settingsPage.StackPanelChangePassword.Visibility = Visibility.Collapsed;
                                        settingsPage.StackPanelChangeUsername.Visibility = Visibility.Collapsed;
                                        settingsPage.TextBlockDialogText.Text = result.ErrorMessage;
                                    }));
                           
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

        private void ButtonChangePassword_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            settingsPage.DialogHost.IsOpen = true;
            settingsPage.ProgressReport.Visibility = Visibility.Collapsed;
            settingsPage.StackPanelError.Visibility = Visibility.Collapsed;
            settingsPage.StackPanelChangePassword.Visibility = Visibility.Visible;
            settingsPage.StackPanelChangeUsername.Visibility = Visibility.Collapsed;
        }

        private void ButtonChangeUsername_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            settingsPage.DialogHost.IsOpen = true;
            settingsPage.ProgressReport.Visibility = Visibility.Collapsed;
            settingsPage.StackPanelError.Visibility = Visibility.Collapsed;
            settingsPage.StackPanelChangePassword.Visibility = Visibility.Collapsed;
            settingsPage.StackPanelChangeUsername.Visibility = Visibility.Visible;
        }

        private void ButtonChangeSignOut_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                App.isClosing = false;
                var LoginWindow = new LoginWindow(PageType.SignIn);
                LoginWindow.Show();
                AppSettingsManager.appSettings.SignedOut = true;
                AppSettingsManager.SaveAppSettings();;
                var wnd = Window.GetWindow(this);
                if (wnd != null)
                {
                    wnd.Close();
                }
                App.isClosing = true;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonRenew_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var renew = new SubPlansPage(false,false,this);
            renew.ShowDialog();
        }

        private async void MainAccountPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            //await FirestoreManager.lisUsers.StopAsync();
        }
    }
}
