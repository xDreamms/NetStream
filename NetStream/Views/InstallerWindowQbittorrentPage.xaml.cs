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
    /// Interaction logic for InstallerWindowQbittorrentPage.xaml
    /// </summary>
    public partial class InstallerWindowQbittorrentPage : Page
    {
        private InstallerWindow installerWindow;
        private BackgroundWorker worker;
        public InstallerWindowQbittorrentPage(InstallerWindow installerWindow)
        {
            InitializeComponent();
            this.installerWindow = installerWindow;
            InstallerWindow.InstallerState = InstallerPage.Qbittorrent;
            //AppSettingsManager.appSettings.InstallerPage = InstallerWindow.InstallerState.ToString();
            AppSettingsManager.SaveAppSettings();
        }

        private void StartInstallationButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Log.Information("Started installation of qBittorrent");
            installerWindow.DialogHost.IsOpen = true;
            installerWindow.TextBlockInfo.Text = App.Current.Resources["InstallingqBittorrent"].ToString();
            worker = new BackgroundWorker();
            worker.DoWork += WorkerOnDoWork;
            worker.RunWorkerCompleted += WorkerOnRunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        private async void WorkerOnRunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            //if (Essentials.IsQBittorrentInstalled())
            //{
            //    installerWindow.DialogHost.IsOpen = false;
            //    Log.Information("Ended installation of qBittorrent");
            //    await Essentials.RunJacketAsync();
            //    Essentials.RunQBittorrent();
            //    JackettService.Init();
            //    App.Init();
            //    var mainWindow = new MainWindow();
            //    mainWindow.Show();
            //    var wnd = Window.GetWindow(this);
            //    if (wnd != null)
            //    {
            //        wnd.Close();
            //    }
            //}
            //else
            //{
            //    Application.Current.Shutdown();
            //}
        }

        private void WorkerOnDoWork(object? sender, DoWorkEventArgs e)
        {
            //Essentials.InstallQBittorrent();
        }
    }
}
