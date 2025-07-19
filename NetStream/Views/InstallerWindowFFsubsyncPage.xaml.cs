using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
    /// Interaction logic for InstallerWindowFFsubsyncPage.xaml
    /// </summary>
    public partial class InstallerWindowFFsubsyncPage : Page
    {
        private InstallerWindow installerWindow;
        private BackgroundWorker worker;
        public InstallerWindowFFsubsyncPage(InstallerWindow installerWindow)
        {
            InitializeComponent();
            this.installerWindow = installerWindow;
            InstallerWindow.InstallerState = InstallerPage.FFSubsync;
            //AppSettingsManager.appSettings.InstallerPage = InstallerWindow.InstallerState.ToString();
            //AppSettingsManager.SaveAppSettings();
        }

        private void FFSUBsynInstall_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            installerWindow.DialogHost.IsOpen = true;
            installerWindow.TextBlockInfo.Text = App.Current.Resources["InstallingFFsubsync"].ToString();
            Log.Information("Installing FFsubsync and FFmpeg...");

            worker = new BackgroundWorker();
            worker.DoWork += WorkerOnDoWork;
            worker.RunWorkerCompleted += WorkerOnRunWorkerCompleted;
            worker.RunWorkerAsync();

        }

        private async void WorkerOnRunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            //if (Essentials.GetFFSubSyncInstallPath() != "")
            //{
            //    Log.Information("Installed FFsubsync and FFmpeg");
            //    if (!Essentials.IsQBittorrentInstalled())
            //    {
            //        Log.Information("Couldn't find qBittorrent. Opening qBittorrent installer...");
            //        if (this.NavigationService != null)
            //        {
            //            this.NavigationService.Navigate(new InstallerWindowQbittorrentPage(installerWindow));
            //            installerWindow.DialogHost.IsOpen = false;
            //        }
            //    }
            //    else
            //    {
            //        Log.Information("Found qBittorrent. Opening main window");
            //        string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "qBittorrent");
            //        if (Directory.Exists(path))
            //        {
            //            File.Copy("qBittorrent.ini", System.IO.Path.Combine(path, "qBittorrent.ini"), true);
            //        }
            //        else
            //        {
            //            Directory.CreateDirectory(path);
            //            File.Copy("qBittorrent.ini", System.IO.Path.Combine(path, "qBittorrent.ini"), true);
            //        }
            //        await Essentials.RunJacketAsync();
            //        Essentials.RunQBittorrent();
            //        JackettService.Init();
            //        App.Init();
            //        var mainWindow = new MainWindow();
            //        mainWindow.Show();
            //        var wnd = Window.GetWindow(this);
            //        if (wnd != null)
            //        {
            //            wnd.Close();
            //        }
            //    }
            //}
            //else
            //{
            //    Application.Current.Shutdown();
            //}
        }

        private void WorkerOnDoWork(object? sender, DoWorkEventArgs e)
        {
            //Essentials.InstallFFsubsync();
            //Essentials.InstallFFmpeg();
        }
    }
}
