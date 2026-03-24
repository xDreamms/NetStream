using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace NetStream.Views;

public partial class TestPlayer : UserControl
{
    public TestPlayer()
    {
        InitializeComponent();
       
    }
    
    private void InitMediaPlayer()
    {
        try
        {
            Control mediaPlayerControl = App.AppNativeVideoPlayerService.CreateControl();
            VideoContainer.Children.Clear();
            VideoContainer.Children.Add(mediaPlayerControl);
            App.AppNativeVideoPlayerService.Play("https://github.com/rafaelreis-hotmart/Audio-Sample-files/raw/refs/heads/master/sample.mp4");
        }
        catch (Exception e)
        {
            Console.WriteLine("TESTPlayer" + e);
            
        }
    }

    private void VideoContainer_OnLoaded(object? sender, RoutedEventArgs e)
    {
        InitMediaPlayer();
    }
}