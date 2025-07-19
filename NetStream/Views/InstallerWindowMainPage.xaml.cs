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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for InstallerWindowMainPage.xaml
    /// </summary>
    public partial class InstallerWindowMainPage : Page
    {
        private InstallerWindow installerWindow;
        public InstallerWindowMainPage(InstallerWindow installerWindow)
        {
            InitializeComponent();
            this.installerWindow = installerWindow;
            Log.Information("Opened Main Installer page");
            InstallerWindow.InstallerState = InstallerPage.Main;
            //AppSettingsManager.appSettings.InstallerPage = InstallerWindow.InstallerState.ToString();
            AppSettingsManager.SaveAppSettings();
        }

        private async void UIElement_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //if (this.NavigationService != null)
            //{
            //    if (!Essentials.IsPythonReadyToUse())
            //    {
            //        Log.Information("Coudln't find python. Opening python installer...");
            //        this.NavigationService.Navigate(new InstallerWindowPythonPage(installerWindow));
            //    }
            //    else if(Essentials.GetFFSubSyncInstallPath() == "")
            //    {
            //        Log.Information("Coudln't find FFsubsync. Opening FFsubsync installer...");
            //        this.NavigationService.Navigate(new InstallerWindowFFsubsyncPage(installerWindow));
            //    }
            //    else if (!Essentials.IsQBittorrentInstalled())
            //    {
            //        Log.Information("Coudln't find Libtorrent. Opening Libtorrent installer...");
            //        this.NavigationService.Navigate(new InstallerWindowQbittorrentPage(installerWindow));
            //    }
            //    else
            //    {
            //        installerWindow.DialogHost.IsOpen = false;
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
        }
    }
}
