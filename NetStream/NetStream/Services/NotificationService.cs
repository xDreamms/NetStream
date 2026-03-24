using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using NetStream.Controls;
using NetStream.Views;

namespace NetStream.Services;

public class NotificationService
{
    private static NotificationService _instance;
    public static NotificationService Instance => _instance ??= new NotificationService();

    public async Task ShowNotification(string title, string message, TimeSpan duration, bool isGlobal = false)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (isGlobal)
            {
                var window = new GlobalNotificationWindow();
                await window.ShowNotificationAsync(title, message, duration);
            }
            else
            {
                var mainWindow = MainWindow.Instance;
                if (mainWindow == null) return;

                var container = mainWindow.FindControl<StackPanel>("InAppNotificationContainer");
                if (container == null) return;

                var notification = new NotificationControl();
                notification.SetContent(title, message);

                notification.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var actualWidth = notification.DesiredSize.Width > 0 ? notification.DesiredSize.Width : 350;

                // Start from the right
                var transform = new TranslateTransform() { X = actualWidth };
                notification.RenderTransform = transform;

                container.Children.Add(notification);

                // Slide in animation
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

                await slideIn.RunAsync(notification);

                // Wait for duration
                await Task.Delay(duration);

                // Slide out animation
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

                await slideOut.RunAsync(notification);

                // Remove from container
                container.Children.Remove(notification);
            }
        });
    }
}
