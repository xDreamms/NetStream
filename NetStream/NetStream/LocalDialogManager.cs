using Avalonia.Controls.Notifications;
using NetStream.Views;
using SukiUI.Dialogs;

namespace NetStream;

public class LocalDialogManager
{
    public static void ShowDialog(string title, string content,NotificationType notificationType)
    {
        MainWindow.GlobalDialogManager.CreateDialog()
            .WithTitle(title)
            .WithContent(content)
            .Dismiss().ByClickingBackground()
            .WithActionButton("Tamam", _ => { }, true)
            .OfType(notificationType)
            .TryShow();
    }
}