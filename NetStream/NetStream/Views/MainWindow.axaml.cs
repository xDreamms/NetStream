using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using NetStream.Services;
using Serilog;
using SukiUI.Dialogs;

namespace NetStream.Views;

public partial class MainWindow : Window
{
    public static SukiDialogManager GlobalDialogManager = new SukiDialogManager();
    public static MainWindow Instance;
    protected virtual void OnSizeChanged(MySizeChangedEventArgs e)
    {
        SizeChanged?.Invoke(this, e);
    }
    public double screenWidth;
    public event EventHandler<MySizeChangedEventArgs> SizeChanged;
    public MainWindow()
    {
        InitializeComponent();
        //DialogHost.Manager = GlobalDialogManager;
        Instance = this;
        
        System.ObservableExtensions.Subscribe(
            this.GetObservable(Visual.BoundsProperty),
            bounds =>
            {
                OnSizeChanged(new MySizeChangedEventArgs(bounds.Width,bounds.Height));
                screenWidth = bounds.Width;
            }
        );
    }

    public void SetContent(UserControl content)
    {
        MainContentControl.Content = content;
    }
    
    public void ShowTitleBar()
    {
        if (MainGrid != null && MainGrid.RowDefinitions.Count > 0)
        {
            MainGrid.RowDefinitions[0].Height = new GridLength(30);
        }
    }
    
    public void HideTitleBar()
    {
        if (MainGrid != null && MainGrid.RowDefinitions.Count > 0)
        {
            MainGrid.RowDefinitions[0].Height = new GridLength(0);
        }
    }
    private bool _doClose = false;

    private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
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
            var youtubePath = AppSettingsManager.appSettings.YoutubeVideoPath;
            if (Directory.Exists(youtubePath))
            {
                var directoryInfo = new DirectoryInfo(youtubePath);
                foreach (var file in directoryInfo.GetFiles())
                {
                    if (MovieDetailsPage.currentMedia != null && file.FullName != MovieDetailsPage.currentMedia.Id+".mp4")
                    {
                        file.Delete();
                    }
                    else
                    {
                        file.Delete();
                    }
                }
            }
            // await FirestoreManager.UnSignApp();
            //Libtorrent.UnLoad();
            _doClose = true;
            this.Close();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown(); // Bu da uygulamayı düzgün kapatır
            }

        }
        catch (Exception e)
        {
            var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
            _doClose = true;
            this.Close();
            Log.Error(errorMessage);
        }
    }
}