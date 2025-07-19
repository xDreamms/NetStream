using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using RestSharp;
using System.Xml.Serialization;
using RestSharp.Serializers;
using System.Xml.Linq;
using System.ComponentModel;
using System.IO;
using NetStream.Views;
using NetStream.Language;
using Serilog;
using System.Runtime;
using Path = System.IO.Path;
using HandyControl.Tools;

namespace NetStream
{
    
    public partial class MainWindow : HandyControl.Controls.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Load();
        }


        private DispatcherTimer timer;
        private void Load()
        {
            HomePage homePage = new HomePage();
            MyFrame.Navigate(homePage);
            HomePage.GetHomePageInstance = homePage;

            DownloadsPageQ downloadsPage = new DownloadsPageQ();
            DownloadsPageQ.GetDownloadsPageInstance = downloadsPage;

            WatchHistoryPage watchHistoryPage = new WatchHistoryPage();
            WatchHistoryPage.GetWatchHistoryPageInstance = watchHistoryPage;


            //if (FirestoreManager.ExpiryDate - FirestoreManager.CurrentTime <= TimeSpan.FromDays(1))
            //{
            //    timer = new DispatcherTimer();
            //    timer.Interval = TimeSpan.FromMinutes(10);
            //    timer.Tick += TimerOnTick;
            //    timer.Start();
            //}
        }

        private bool messageReceived = false;
        private async void TimerOnTick(object? sender, EventArgs e)
        {
            try
            {
                if (!messageReceived)
                {
                    var time = await FirestoreManager.GetNistTime();
                    var currentTime = time.ToUniversalTime();
                    if (FirestoreManager.ExpiryDate < currentTime)
                    {
                        if (WindowsManager.OpenedWindows.Count > 0)
                        {
                            foreach (var openedWindow in WindowsManager.OpenedWindows)
                            {
                                openedWindow.Close();
                            }
                        }
                        messageReceived = true;
                        timer.Stop();
                        timer.Tick -= TimerOnTick;
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                CustomMessageBox customMessage = new CustomMessageBox(App.Current.Resources["SubExpired"].ToString(), MessageType.Error, MessageButtons.Ok);
                                customMessage.ShowDialog();
                                new SubPlansPage(true, false).Show();
                                this.Close();
                            }));
                    }
                    else if (FirestoreManager.ExpiryDate - currentTime > TimeSpan.FromDays(1))
                    {
                        timer.Stop();
                        timer.Tick -= TimerOnTick;
                        messageReceived = true;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        private bool _doClose = false;
        private async void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            if (!_doClose && App.isClosing)
            {
                e.Cancel = true;
                this.Hide();
                await ClosingTasks();
            }
        }

        private async Task ClosingTasks()
        {
            try
            {
                Libtorrent.CancelAll();
                Libtorrent.sessionManager.StopListeningAlerts();
                Libtorrent.client.Clear();
                var youtubePath = AppSettingsManager.appSettings.YoutubeVideoPath;
                if (Directory.Exists(youtubePath))
                {
                    var directoryInfo = new DirectoryInfo(youtubePath);
                    foreach (var file in directoryInfo.GetFiles())
                    {
                        if (file.FullName != MovieDetailsPage.currentMediaFile)
                        {
                            file.Delete();
                        }
                    }
                }
                // await FirestoreManager.UnSignApp();
                _doClose = true;
                Application.Current.Shutdown(0);
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
    }

   
}