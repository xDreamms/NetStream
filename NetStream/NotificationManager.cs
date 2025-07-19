using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;

namespace NetStream
{
    class NotificationManager
    {
        //public static void SendNotification()
        //{
        //    new ToastContentBuilder()
        //        .AddArgument("action", "viewConversation")
        //        .AddArgument("conversationId", 9813)
        //        .AddText("Andrew sent you a picture")
        //        .AddText("Check this out, The Enchantments in Washington!")
        //        .Show(); 
        //}


        public static void SendNotification2(string message="Message")
        {
            //Notifier notifier = new Notifier(cfg =>
            //{
            //    cfg.PositionProvider = new PrimaryScreenPositionProvider(
            //        corner: Corner.BottomRight,
            //        offsetX: 10,
            //        offsetY: 10);

            //    cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
            //        notificationLifetime: TimeSpan.FromSeconds(20),
            //        maximumNotificationCount: MaximumNotificationCount.FromCount(5));

            //    cfg.Dispatcher = Application.Current.Dispatcher;
            //});
            //var options = new MessageOptions
            //{
            //    FontSize = 30, // set notification font size
            //    ShowCloseButton = false, // set the option to show or hide notification close button
            //    Tag = "Any object or value which might matter in callbacks",
            //    FreezeOnMouseEnter = true, // set the option to prevent notification dissapear automatically if user move cursor on it
            //    NotificationClickAction = n => // set the callback for notification click event
            //    {
            //        n.Close(); // call Close method to remove notification

            //    },
            //};
            //notifier.ShowInformation(message, options);



            //var notificationManager = new Notifications.Wpf.NotificationManager();

            //notificationManager.Show(new NotificationContent
            //{
            //    Title = "Sample notification",
            //    Message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
            //    Type = NotificationType.Information
            //},"",TimeSpan.FromSeconds(5),onClick:(() =>
            //{
            //    MessageBox.Show("1");
            //}));

            

        }
    }
}
