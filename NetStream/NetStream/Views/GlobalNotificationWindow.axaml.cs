using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace NetStream.Views;

public partial class GlobalNotificationWindow : Window
{
    public GlobalNotificationWindow()
    {
        InitializeComponent();
    }

    public async Task ShowNotificationAsync(string title, string message, TimeSpan duration)
    {
        NotificationPresenter.SetContent(title, message);
        
        // Measure to get the size based on the content dynamically
        this.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var actualWidth = this.DesiredSize.Width > 0 ? this.DesiredSize.Width : 350;

        // Position top right
        var screen = Screens.Primary;
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            var scaling = screen.Scaling;
            var physicalWidth = (int)(actualWidth * scaling);
            this.Position = new PixelPoint(workingArea.Right - physicalWidth - (int)(15 * scaling), workingArea.Y + (int)(15 * scaling));
        }

        this.Show();

        // Animate Slide In
        var transform = new TranslateTransform() { X = actualWidth };
        NotificationPresenter.RenderTransform = transform;

        var slideIn = new Avalonia.Animation.Animation
        {
            Duration = TimeSpan.FromSeconds(0.4),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter(TranslateTransform.XProperty, actualWidth) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(TranslateTransform.XProperty, 0d) }
                }
            }
        };

        await slideIn.RunAsync(NotificationPresenter);

        await Task.Delay(duration);

        // Animate Slide Out
        var slideOut = new Avalonia.Animation.Animation
        {
            Duration = TimeSpan.FromSeconds(0.4),
            Easing = new CubicEaseIn(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter(TranslateTransform.XProperty, 0d) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(TranslateTransform.XProperty, actualWidth) }
                }
            }
        };

        await slideOut.RunAsync(NotificationPresenter);

        this.Close();
    }
}
