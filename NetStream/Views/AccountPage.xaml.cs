using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for AccountPage.xaml
    /// </summary>
    public partial class AccountPage : Page
    {
        public static Page GetAccountPageInstance;
        public AccountPage()
        {
            InitializeComponent();
            AccountFavoritesPage accountFavoritesPage = new AccountFavoritesPage();
            AccountNavigation.Navigate(accountFavoritesPage);
        }

        //public event EventHandler<MediaTypeChangedEventArgs> MediaTypeChanged;

        //protected virtual void OnMediaTypeChanged(MediaTypeChangedEventArgs e)
        //{
        //    MediaTypeChanged?.Invoke(this, e);
        //}

        private async void NavigationButtonPressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var current = sender as TextBlock;
                if (current != null)
                {
                    switch (current.Name)
                    {
                        case "FavoritesButton":
                            //OnMediaTypeChanged(new MediaTypeChangedEventArgs(this.sender,MediaTypeX.Movie));
                            AccountFavoritesPage accountFavoritesPage = new AccountFavoritesPage();
                            AccountNavigation.Navigate(accountFavoritesPage);
                            await Application.Current.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(() =>
                                {
                                    this.MenuLine1.SetValue(Grid.ColumnProperty, 1);
                                    FavoritesButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                                    WatchListButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                                }));
                            break;
                        case "WatchListButton":
                            //OnMediaTypeChanged(new MediaTypeChangedEventArgs(this.sender, MediaTypeX.Movie));
                            AccountWatchListPage accountWatchListPage = new AccountWatchListPage();
                            AccountNavigation.Navigate(accountWatchListPage);
                            await Application.Current.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(() =>
                                {
                                    this.MenuLine1.SetValue(Grid.ColumnProperty, 3);
                                    FavoritesButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 65, 65, 65));
                                    WatchListButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                                }));
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ButtonLogOut_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (await Service.LogOut())
                {
                    //HomePage.GetHomePageInstance.HomePageNavigation.Navigated += NavServiceOnNavigated;
                    GetAccountPageInstance = null;
                    HomePage.GetHomePageInstance.HomePageNavigation.Navigate(new AccountLoginPage());
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void NavServiceOnNavigated(object sender, NavigationEventArgs e)
        {
            HomePage.GetHomePageInstance.HomePageNavigation.RemoveBackEntry();
            HomePage.GetHomePageInstance.HomePageNavigation.Navigated -= NavServiceOnNavigated;
        }
    }

    //public enum MediaTypeX
    //{
    //    Movie,
    //    TvShow
    //}

    //public enum Sender
    //{
    //    Favorites,
    //    WatchList
    //}

    //public class MediaTypeChangedEventArgs : EventArgs
    //{
    //    public Sender sender { get; set; }
    //    public MediaTypeX MediaType { get; set; }

    //    public MediaTypeChangedEventArgs(Sender sender, MediaTypeX MediaType)
    //    {
    //        this.sender = sender;
    //        this.MediaType = MediaType;
    //    }
    //}
}
