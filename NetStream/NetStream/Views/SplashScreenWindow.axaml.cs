using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NetStream.Views
{
    public partial class SplashScreenWindow : UserControl
    {
        private const double ReferansGenislik = 750;
        private const double ReferansYukseklik = 450;
        
        private const double ReferansLogoGenislik = 50;
        private const double ReferansLogoYukseklik = 50;
        private const double ReferansAppNameFontSize = 28;
        private const double ReferansPanaGenislik = 320;
        private const double ReferansPanaYukseklik = 320;
        private const double ReferansTextLoadingFontSize = 17;
        private const double ReferansProgressBarLoadingWidth = 280;
        private const double ReferansProgressBarLoadingHeight = 5;
        
        private const double ReferansTextAppNameMarginLeft = 15;
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        public SplashScreenWindow()
        {
            InitializeComponent();
          //  AllocConsole();
        }

        

        private void Control_OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            var genislik = e.NewSize.Width;
            var yukseklik = e.NewSize.Height;
            
            double genislikOrani = genislik / ReferansGenislik;
            double yukseklikOrani = yukseklik / ReferansYukseklik;
            
            genislikOrani = Math.Max(0.5, Math.Min(genislikOrani, 2.0));
            yukseklikOrani = Math.Max(0.5, Math.Min(yukseklikOrani, 2.0));
            
            double uniformOran = Math.Min(genislikOrani, yukseklikOrani);
            
            LogoImage.Width = ReferansLogoGenislik * uniformOran;
            LogoImage.Height = ReferansLogoYukseklik * uniformOran;
            TextAppName.FontSize = ReferansAppNameFontSize * uniformOran;
            PanaImage.Width = ReferansPanaGenislik * uniformOran;
            PanaImage.Height = ReferansPanaYukseklik * uniformOran;
            
            if (TextLoading != null)
            {
                TextLoading.FontSize = ReferansTextLoadingFontSize * uniformOran;
            }
            
            if (ProgressBarLoading != null)
            {
                ProgressBarLoading.Width = ReferansProgressBarLoadingWidth * genislikOrani;
                ProgressBarLoading.Height = ReferansProgressBarLoadingHeight * uniformOran;
            }
                
            TextAppName.Margin = new Thickness(
                ReferansTextAppNameMarginLeft * genislikOrani, 
                0, 
                0, 
                0);
            
            TextLoading.Margin = new Thickness(
                0, 
                30 * genislikOrani, 
                0, 
                15 * genislikOrani);
        }
    }
} 