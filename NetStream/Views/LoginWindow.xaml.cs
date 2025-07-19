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
using MaterialDesignThemes.Wpf;
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : HandyControl.Controls.Window
    {
        PageType pageType;
        public LoginWindow(PageType pageType)
        {
            InitializeComponent();
            //SetPrimaryColor();
            this.pageType = pageType;
            Load();
        }

        //private void SetPrimaryColor()
        //{
        //    PaletteHelper paletteHelper = new PaletteHelper();
        //    var theme = paletteHelper.GetTheme();
        //    Color current = Color.FromArgb(255, 255, 255, 255);
        //    theme.SetPrimaryColor(current);
        //    paletteHelper.SetTheme(theme);
        //}

        async void Load()
        {
            try
            {
                if (pageType == PageType.SignUp && !(await FirestoreManager.IsComputerSignedUpBefore()))
                {
                    LoginFrame.Navigate(new SignUpPage());
                }
                else
                {
                    LoginFrame.Navigate(new LoginPage(false));
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void LoginWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void LoginWindow_OnClosed(object? sender, EventArgs e)
        {
            //App.SetPrimaryColor();
        }
    }

    public enum PageType
    {
        SignUp,
        SignIn
    }
}
