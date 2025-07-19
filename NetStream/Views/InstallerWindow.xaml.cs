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
using System.Windows.Shapes;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for InstallerWindow.xaml
    /// </summary>
    public partial class InstallerWindow : HandyControl.Controls.Window
    {
        public static InstallerPage InstallerState;
        public InstallerWindow(InstallerPage installerPage)
        {
            InitializeComponent();
            switch (installerPage)
            {
                case InstallerPage.Main:
                    InstallerFrame.Navigate(new InstallerWindowMainPage(this));
                    break;
                case InstallerPage.Python:
                    InstallerFrame.Navigate(new InstallerWindowPythonPage(this));
                    break;
                case InstallerPage.FFSubsync:
                    InstallerFrame.Navigate(new InstallerWindowFFsubsyncPage(this));
                    break;
                case InstallerPage.Qbittorrent:
                    InstallerFrame.Navigate(new InstallerWindowQbittorrentPage(this));
                    break;
            }
        }
    }

    public enum InstallerPage
    {
        Main,
        Python,
        FFSubsync,
        Qbittorrent,
        Done
    }
}
