using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace NetStream.Views;

public partial class DownloadsPageMobile : UserControl
{
    public static DownloadsPageMobile Instance { get; private set; }
    public static ObservableCollection<Item> torrents = new ObservableCollection<Item>();
    public DownloadsPageMobile()
    {
        InitializeComponent();
        OnLoad();
        Instance = this;
    }

    private async void OnLoad()
    {
        DownloadsDisplay.ItemsSource = torrents;
    }
    private void DownloadsPage_OnLoaded(object? sender, RoutedEventArgs e)
    {
        
    }

    private async void DownloadsItem_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
               
            }
    }

    private void ContextMenuPause_OnLoaded(object? sender, RoutedEventArgs e)
    {
        
    }

    private void MenuItem_OnClickPause(object? sender, RoutedEventArgs e)
    {
        
    }

    private void MenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }

    private void MenuItem_ContextMenuDetailsOnClick(object? sender, RoutedEventArgs e)
    {
        
    }
}