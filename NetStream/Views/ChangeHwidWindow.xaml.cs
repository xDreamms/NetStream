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
using System.Windows.Shapes;
using HandyControl.Controls;
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for ChangeHwidWindow.xaml
    /// </summary>
    public partial class ChangeHwidWindow : HandyControl.Controls.Window
    {
        public bool changedMachine = false;
        public ChangeHwidWindow()
        {
            InitializeComponent();
        }

        private async void ButtonYes_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var hwidChangeResult = await FirestoreManager.ChangeHwid();
                if (hwidChangeResult.Success)
                {
                    changedMachine = true;
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    Log.Information("Opened Main Window");
                    this.Close();
                }
                else
                {
                    new CustomMessageBox(hwidChangeResult.ErrorMessage, MessageType.Error, MessageButtons.Ok).ShowDialog();
                    changedMachine = false;
                    this.Close();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonNo_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown(0);
        }
    }
}
