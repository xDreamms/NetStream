using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NetStream.Controls;

public partial class NotificationControl : UserControl
{
    public NotificationControl()
    {
        InitializeComponent();
    }

    public void SetContent(string title, string message)
    {
        var titleText = this.FindControl<TextBlock>("TitleText");
        var messageText = this.FindControl<TextBlock>("MessageText");

        if (titleText != null) titleText.Text = title;
        if (messageText != null) messageText.Text = message;
    }
}
