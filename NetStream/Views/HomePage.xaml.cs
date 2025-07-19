using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using FontAwesome.Sharp;
using MaterialDesignThemes.Wpf;
using NetStream.Views;
using Serilog;
using TMDbLib.Client;
using TMDbLib.Objects.Reviews;
using Color = System.Windows.Media.Color;
using Image = System.Drawing.Image;
using Rectangle = System.Windows.Shapes.Rectangle;
using Timer = System.Timers.Timer;

namespace NetStream
{
    public partial class HomePage : Page
    {
        private Home home;
        private MoviesPage moviesPage;
        private TvShowsPage tvShowsPage;
        private SearchPage searchPage;
        private DiscoverPage discoverPage;
        public static HomePage GetHomePageInstance = null;
        public static NavigationService NavigationService;
        private SettingsPage settingsPage;
        private SelectedPage selectedPage;

        public HomePage()
        {
            InitializeComponent();
            HomePageNavigation.Navigated += HomePageNavigationOnNavigated;
            ButtonHome.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
            home = new Home();
            HomePageNavigation.Navigate(home);
            NavigationService = HomePageNavigation.NavigationService;
            selectedPage = SelectedPage.Home;
            this.OnBack += OnOnBack;
            Load();

            System.Timers.Timer timer = new System.Timers.Timer(100);
            timer.Elapsed += TimerOnElapsed;
            timer.AutoReset = true; 
            timer.Start();
        }

        private void OnOnBack(object? sender, EventArgs e)
        {
            if (HomePageNavigation.NavigationService != null && HomePageNavigation.NavigationService.CanGoBack)
                HomePageNavigation.NavigationService.GoBack();
        }

        private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (MovieDetailsPage.VlcVideoSourceProviders.Count == 0) return;
                foreach (var sourceProviderInfo in MovieDetailsPage.VlcVideoSourceProviders.ToList())
                {
                    if (sourceProviderInfo.Id != MovieDetailsPage.current.Id)
                    {
                        if (sourceProviderInfo.VlcVideoSourceProvider != null)
                        {
                            ThreadPool.QueueUserWorkItem(_ =>
                            {
                                try
                                {
                                    var mediaPlayer = sourceProviderInfo.VlcVideoSourceProvider?.MediaPlayer;
                                    if (mediaPlayer != null)
                                    {
                                        if (mediaPlayer.IsPlaying())
                                        {
                                            mediaPlayer.Stop();
                                        }

                                        Thread.Sleep(100);
                                    }

                                    sourceProviderInfo.VlcVideoSourceProvider?.Dispose();
                                }
                                catch (Exception disposeEx)
                                {
                                    Log.Error($"Error disposing media player: {disposeEx.Message}");
                                }
                            });
                        }
                        else
                        {
                            MovieDetailsPage.VlcVideoSourceProviders.Remove(sourceProviderInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public async void Load()
        {
            try
            {
                if (await IsJackettRunningAsync())
                {
                    Essentials.GetSelectedIndexers();
                }
                else
                {
                    timerJackett = new Timer(2000);
                    timerJackett.Elapsed += TimerJackettOnElapsed;
                    timerJackett.AutoReset = true;
                    timerJackett.Start();
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private System.Timers.Timer timerJackett;
        private bool gotSelectedIndexer = false;
        private async void TimerJackettOnElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (!gotSelectedIndexer)
                {
                    if (await IsJackettRunningAsync())
                    {
                        timerJackett.Stop();
                        timerJackett.Dispose();
                        Essentials.GetSelectedIndexers();
                        gotSelectedIndexer = true;
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public async Task<bool> IsJackettRunningAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2); 
                    HttpResponseMessage response = await client.GetAsync(AppSettingsManager.appSettings.JacketApiUrl);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }




        public event EventHandler OnBack;

        protected void OnBackEvent()
        {
            if (this.OnBack != null)
                this.OnBack(this, EventArgs.Empty);
        }

        private async void HomePage_OnLoaded(object sender, RoutedEventArgs e)
        {

        }

        

        private async void NavigationButtonPressed(object sender, MouseButtonEventArgs e)
        {


            try
            {
                if (sender is IconBlock navigationButton)
                {
                    if (navigationButton.Name != "ButtonBack")
                    {
                        DisposePages();
                    }
                    var buttonActions = new Dictionary<string, (SelectedPage page, Func<Page> createPage)>
                    {
                        { "ButtonHome", (SelectedPage.Home, () => home ??= new Home()) },
                        { "BUttonSearch", (SelectedPage.Search, () => searchPage ??= new SearchPage()) },
                        { "ButtonDownload", (SelectedPage.Download, () => DownloadsPageQ.GetDownloadsPageInstance) },
                        { "ButtonDiscover", (SelectedPage.Discover, () => discoverPage ??=new DiscoverPage()) },
                        { "ButtonSettings", (SelectedPage.Settings, () => SettingsPage.GetSettingsPageInstance ??= new SettingsPage()) },
                        { "ButtonAccount", (SelectedPage.TmdbUserProfile, () => AccountPage.GetAccountPageInstance ?? new AccountLoginPage()) },
                        { "ButtonMovie", (SelectedPage.Movies, () => moviesPage ??= new MoviesPage()) },
                        { "BUttonTvShow", (SelectedPage.TvShows, () => tvShowsPage ??= new TvShowsPage()) },
                        { "BtnWatchHistory", (SelectedPage.WatchHistory, () => WatchHistoryPage.GetWatchHistoryPageInstance) }
                    };

                    if (buttonActions.TryGetValue(navigationButton.Name, out var action))
                    {
                        await UpdateUIAsync(navigationButton);
                        OnBackEvent();
                        selectedPage = action.page;
                        HomePageNavigation.Navigate(action.createPage());
                    }
                    else if (navigationButton.Name == "ButtonBack")
                    {
                        OnBackEvent();
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }

        }

        private void DisposePages()
        {
            try
            {
                home?.Dispose();
                moviesPage?.Dispose();
                tvShowsPage?.Dispose();
                searchPage?.Dispose();
                discoverPage?.Dispose();
                if(SettingsPage.GetSettingsPageInstance != null)
                    SettingsPage.GetSettingsPageInstance.Dispose();
                home = null;
                moviesPage = null;
                tvShowsPage = null;
                searchPage = null;
                discoverPage = null;
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async Task UpdateUIAsync(IconBlock activeButton)
        {
            try
            {
                var buttons = new List<IconBlock> { ButtonHome, ButtonMovie, BUttonTvShow, BUttonSearch, ButtonDownload, ButtonDiscover, ButtonSettings, ButtonAccount, BtnWatchHistory };
                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    foreach (var button in buttons)
                    {
                        button.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        if (button == BUttonTvShow)
                        {
                            button.IconFont = IconFont.Solid;
                        }
                        else
                        {
                            button.IconFont = IconFont.Regular;
                        }
                    }

                    activeButton.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                    activeButton.IconFont = activeButton == BUttonTvShow ? IconFont.Regular : IconFont.Solid;
                }));
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
       
        private void HomePageNavigationOnNavigated(object sender, NavigationEventArgs e)
        {
            try
            {
                if (MovieDetailsPage.current != null)
                {
                    MovieDetailsPage.current.Id = 0;
                }
                if (e.Content.GetType().Name == "Home"
                    || e.Content.GetType().Name == "MoviesPage"
                    || e.Content.GetType().Name == "TvShowsPage"
                    || e.Content.GetType().Name == "SearchPage"
                    || e.Content.GetType().Name == "DiscoverPage"
                    || e.Content.GetType().Name == "SettingsPage"
                    || e.Content.GetType().Name == "AccountPage"
                    || e.Content.GetType().Name == "AccountLoginPage"
                    || e.Content.GetType().Name == "WatchHistoryPage")
                {
                    ClearBackHistory();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        private void ClearBackHistory()
        {
            try
            {
                if (MovieDetailsPage.MovieDetailsPagess.Count > 0)
                {
                    foreach (var movieDetailsPage in MovieDetailsPage.MovieDetailsPagess.ToList())
                    {
                        movieDetailsPage.Dispose();
                        MovieDetailsPage.MovieDetailsPagess.Remove(movieDetailsPage);
                    }
                }
                while (HomePageNavigation.NavigationService.CanGoBack)
                {
                    HomePageNavigation.NavigationService.RemoveBackEntry();
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonHome_OnMouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                if (selectedPage != SelectedPage.Home)
                {
                    ButtonHome.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                    ButtonHome.IconFont = IconFont.Solid;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonHome_OnMouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if (selectedPage != SelectedPage.Home)
                {
                    ButtonHome.Foreground = new SolidColorBrush(Colors.White);
                    ButtonHome.IconFont = IconFont.Regular;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ButtonMovie_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.Movies)
            {
                ButtonMovie.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                ButtonMovie.IconFont = IconFont.Solid;
            }
        }

        private void ButtonMovie_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.Movies)
            {
                ButtonMovie.Foreground = new SolidColorBrush(Colors.White);
                ButtonMovie.IconFont = IconFont.Regular;
            }
        }

        private void BUttonTvShow_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.TvShows)
            {
                BUttonTvShow.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                BUttonTvShow.IconFont = IconFont.Regular;
            }
        }

        private void BUttonTvShow_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.TvShows)
            {
                BUttonTvShow.Foreground = new SolidColorBrush(Colors.White);
                BUttonTvShow.IconFont = IconFont.Solid;
            }
        }

        private void BUttonSearch_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.Search)
            {
                BUttonSearch.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                BUttonSearch.IconFont = IconFont.Solid;
            }
        }

        private void BUttonSearch_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.Search)
            {
                BUttonSearch.Foreground = new SolidColorBrush(Colors.White);
                BUttonSearch.IconFont = IconFont.Regular;
            }
        }

        private void ButtonDiscover_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.Discover)
            {
                ButtonDiscover.Foreground = new SolidColorBrush(Colors.White);
                ButtonDiscover.IconFont = IconFont.Regular;
            }
        }


        private void ButtonDiscover_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.Discover)
            {
                ButtonDiscover.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                ButtonDiscover.IconFont = IconFont.Solid;
            }
        }

        private void ButtonDownload_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.Download)
            {
                ButtonDownload.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                ButtonDownload.IconFont = IconFont.Solid;
            }
        }

        private void ButtonDownload_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.Download)
            {
                ButtonDownload.Foreground = new SolidColorBrush(Colors.White);
                ButtonDownload.IconFont = IconFont.Regular;
            }
        }

        private void ButtonAccount_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.TmdbUserProfile)
            {
                ButtonAccount.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                ButtonAccount.IconFont = IconFont.Solid;
            }
        }

        private void ButtonAccount_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.TmdbUserProfile)
            {
                ButtonAccount.Foreground = new SolidColorBrush(Colors.White);
                ButtonAccount.IconFont = IconFont.Regular;
            }
        }

        private void ButtonSettings_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.Settings)
            {
                ButtonSettings.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                ButtonSettings.IconFont = IconFont.Solid;
            }
        }

        private void ButtonSettings_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.Settings)
            {
                ButtonSettings.Foreground = new SolidColorBrush(Colors.White);
                ButtonSettings.IconFont = IconFont.Regular;
            }
        }

        private void BtnWatchHistory_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.WatchHistory)
            {
                BtnWatchHistory.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                BtnWatchHistory.IconFont = IconFont.Solid;
            }
        }

        private void BtnWatchHistory_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (selectedPage != SelectedPage.WatchHistory)
            {
                BtnWatchHistory.Foreground = new SolidColorBrush(Colors.White);
                BtnWatchHistory.IconFont = IconFont.Regular;
            }
        }

    }

    public enum SelectedPage
    {
        Home,
        Movies,
        TvShows,
        Search,
        Discover,
        Download,
        TmdbUserProfile,
        Settings,
        WatchHistory,
        None
    }
}
