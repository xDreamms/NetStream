using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace NetStream.Views;

public partial class AutoUpdateControl : UserControl
{
    private const double ReferansGenislik = 750;
    private const double ReferansYukseklik = 450;
    
    private const double ReferansLogoGenislik = 50;
    private const double ReferansLogoYukseklik = 50;
    private const double ReferansAppNameFontSize = 28;
    private const double ReferansPanaGenislik = 320;
    private const double ReferansPanaYukseklik = 320;
    
    private const double ReferansTextUpdatingFontSize = 22;
    private const double ReferansDownloadingFilesTextFontSize = 16;
    private const double ReferansFileCountTextFontSize = 14;
    private const double ReferansCurrentFileTextFontSize = 13;
    
    private const double ReferansProgressBarWidth = 350;
    private const double ReferansProgressBarHeight = 8;

    public AutoUpdateControl()
    {
        InitializeComponent();
    }

    private void Control_OnSizeChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var sizeArgs = e as Avalonia.Controls.SizeChangedEventArgs;
        if (sizeArgs == null) return;
        
        var genislik = sizeArgs.NewSize.Width;
        var yukseklik = sizeArgs.NewSize.Height;
        
        double genislikOrani = genislik / ReferansGenislik;
        double yukseklikOrani = yukseklik / ReferansYukseklik;
        
        genislikOrani = Math.Max(0.5, Math.Min(genislikOrani, 3.5));
        yukseklikOrani = Math.Max(0.5, Math.Min(yukseklikOrani, 3.5));
        
        double uniformOran = Math.Min(genislikOrani, yukseklikOrani);
        
        if (LogoImage != null)
        {
            LogoImage.Width = ReferansLogoGenislik * uniformOran;
            LogoImage.Height = ReferansLogoYukseklik * uniformOran;
        }
        
        if (TextAppName != null)
        {
            TextAppName.FontSize = ReferansAppNameFontSize * uniformOran;
            TextAppName.Margin = new Avalonia.Thickness(15 * genislikOrani, 0, 0, 0);
        }
        
        if (PanaImage != null)
        {
            PanaImage.Width = ReferansPanaGenislik * uniformOran;
            PanaImage.Height = ReferansPanaYukseklik * uniformOran;
        }
        
        if (TextUpdating != null)
        {
            TextUpdating.FontSize = ReferansTextUpdatingFontSize * uniformOran;
            TextUpdating.Margin = new Avalonia.Thickness(0, 30 * genislikOrani, 0, 15 * genislikOrani);
        }
        
        if (FileProgressBar != null)
        {
            FileProgressBar.Width = ReferansProgressBarWidth * genislikOrani;
            FileProgressBar.Height = ReferansProgressBarHeight * uniformOran;
        }
        
        if (DownloadingFilesText != null)
        {
            DownloadingFilesText.FontSize = ReferansDownloadingFilesTextFontSize * uniformOran;
            DownloadingFilesText.Margin = new Avalonia.Thickness(0, 15 * genislikOrani, 0, 5 * genislikOrani);
        }
        
        if (FileCountText != null) FileCountText.FontSize = ReferansFileCountTextFontSize * uniformOran;
        if (DownloadSpeedText != null) DownloadSpeedText.FontSize = ReferansFileCountTextFontSize * uniformOran;
        
        if (CurrentFileText != null)
        {
            CurrentFileText.FontSize = ReferansCurrentFileTextFontSize * uniformOran;
            CurrentFileText.Margin = new Avalonia.Thickness(0, 5 * genislikOrani, 0, 0);
            CurrentFileText.MaxWidth = ReferansProgressBarWidth * genislikOrani;
        }
        
        if (ProgressDetailsGrid != null)
        {
            ProgressDetailsGrid.Width = ReferansProgressBarWidth * genislikOrani;
        }
    }

    public void UpdateProgress(UpdateProgressInfo info)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (FileProgressBar != null)
            {
                FileProgressBar.Value = info.ProgressPercentage;
            }

            if (FileCountText != null)
            {
                var formatString = ResourceProvider.GetString("FilesCountFormatString") ?? "{0} / {1} Dosya";
                FileCountText.Text = string.Format(formatString, info.DownloadingFileIndex, info.TotalFiles);
            }

            if (DownloadSpeedText != null)
            {
                DownloadSpeedText.Text = info.DownloadSpeed;
            }

            if (CurrentFileText != null)
            {
                var formatString = ResourceProvider.GetString("DownloadingFileString") ?? "İndiriliyor: {0}";
                CurrentFileText.Text = string.Format(formatString, info.FileName);
            }
        });
    }

    public void SetStatus(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (CurrentFileText != null)
            {
                CurrentFileText.Text = text;
            }
        });
    }
}
