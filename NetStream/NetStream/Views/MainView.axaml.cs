using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Serilog;
using NetStream.Navigation;
using Projektanker.Icons.Avalonia;
using Avalonia;
using System.Linq;
using System.ComponentModel;

namespace NetStream.Views;

public partial class MainView : UserControl, INotifyPropertyChanged
{
    public string HomeIconValue => SelectedPage == SelectedPage.Home || (SelectedPage != SelectedPage.Home && MouseOverPage == SelectedPage.Home)
        ? "fa-solid fa-house"
        : "fa-light fa-house";

    public IBrush HomeIconColor => SelectedPage == SelectedPage.Home || (SelectedPage != SelectedPage.Home && MouseOverPage == SelectedPage.Home)
        ? new SolidColorBrush((Color)App.Current.Resources["ColorDefault"])
        : new SolidColorBrush(Colors.White);
    
    
    public string MoviesIconValue => SelectedPage == SelectedPage.Movies || (SelectedPage != SelectedPage.Movies && MouseOverPage == SelectedPage.Movies)
        ? "fa-solid fa-clapperboard"
        : "fa-light fa-clapperboard";
    
    public IBrush MoviesIconColor => SelectedPage == SelectedPage.Movies || (SelectedPage != SelectedPage.Movies && MouseOverPage == SelectedPage.Movies)
        ? new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]) 
        : new SolidColorBrush(Colors.White);
    
    
    public string TvShowsIconValue => SelectedPage == SelectedPage.TvShows || (SelectedPage != SelectedPage.TvShows && MouseOverPage == SelectedPage.TvShows)
        ? "fa-solid fa-display"
        : "fa-light fa-display";
    public IBrush TvShowsIconColor => SelectedPage == SelectedPage.TvShows  || (SelectedPage != SelectedPage.TvShows && MouseOverPage == SelectedPage.TvShows)
        ?new SolidColorBrush((Color)App.Current.Resources["ColorDefault"])
        : new SolidColorBrush(Colors.White);
        
    
    public string SearchIconValue => SelectedPage == SelectedPage.Search || (SelectedPage != SelectedPage.Search && MouseOverPage == SelectedPage.Search)
        ? "fa-solid fa-magnifying-glass"
        : "fa-light fa-magnifying-glass";
    public IBrush SearchIconColor => SelectedPage == SelectedPage.Search  || (SelectedPage != SelectedPage.Search && MouseOverPage == SelectedPage.Search)
        ? new SolidColorBrush((Color)App.Current.Resources["ColorDefault"])
        : new SolidColorBrush(Colors.White);
    
    public string DownloadIconValue => SelectedPage == SelectedPage.Download || (SelectedPage != SelectedPage.Download && MouseOverPage == SelectedPage.Download)
        ? "fa-solid fa-download"
        : "fa-light fa-download";
        
    public IBrush DownloadIconColor => SelectedPage == SelectedPage.Download  || (SelectedPage != SelectedPage.Download && MouseOverPage == SelectedPage.Download)
        ? new SolidColorBrush((Color)App.Current.Resources["ColorDefault"])
        : new SolidColorBrush(Colors.White);
    
    public string WatchHistoryIconValue => SelectedPage == SelectedPage.WatchHistory || (SelectedPage != SelectedPage.WatchHistory && MouseOverPage == SelectedPage.WatchHistory)
        ? "fa-solid fa-clock-rotate-left"
        : "fa-light fa-clock-rotate-left";
        
    public IBrush WatchHistoryIconColor => SelectedPage == SelectedPage.WatchHistory  || (SelectedPage != SelectedPage.WatchHistory && MouseOverPage == SelectedPage.WatchHistory)
        ? new SolidColorBrush((Color)App.Current.Resources["ColorDefault"])
        : new SolidColorBrush(Colors.White);
    
    public string DiscoverIconValue => SelectedPage == SelectedPage.Discover || (SelectedPage != SelectedPage.Discover && MouseOverPage == SelectedPage.Discover)
        ? "fa-solid fa-globe"
        : "fa-light fa-globe";
    
    public IBrush DiscoverIconColor => SelectedPage == SelectedPage.Discover || (SelectedPage != SelectedPage.Discover && MouseOverPage == SelectedPage.Discover)
        ? new SolidColorBrush((Color)App.Current.Resources["ColorDefault"])
        : new SolidColorBrush(Colors.White);
    
    public string AccountIconValue => SelectedPage == SelectedPage.TmdbUserProfile || (SelectedPage != SelectedPage.TmdbUserProfile && MouseOverPage == SelectedPage.TmdbUserProfile)
        ? "fa-solid fa-user"
        : "fa-light fa-user";
    
    public IBrush AccountIconColor => SelectedPage == SelectedPage.TmdbUserProfile  || (SelectedPage != SelectedPage.TmdbUserProfile && MouseOverPage == SelectedPage.TmdbUserProfile)
        ? new SolidColorBrush((Color)App.Current.Resources["ColorDefault"])
        : new SolidColorBrush(Colors.White);
    
    public string RecommendationsIconValue => SelectedPage == SelectedPage.Recommendations || (SelectedPage != SelectedPage.Recommendations && MouseOverPage == SelectedPage.Recommendations)
        ? "fa-solid fa-wand-magic-sparkles"
        : "fa-light fa-wand-magic-sparkles";

    public IBrush RecommendationsIconColor => SelectedPage == SelectedPage.Recommendations || (SelectedPage != SelectedPage.Recommendations && MouseOverPage == SelectedPage.Recommendations)
        ? new SolidColorBrush((Color)App.Current.Resources["ColorDefault"])
        : new SolidColorBrush(Colors.White);

    public string SettingsIconValue => SelectedPage == SelectedPage.Settings || (SelectedPage != SelectedPage.Settings && MouseOverPage == SelectedPage.Settings)
        ? "fa-solid fa-gear"
        : "fa-light fa-gear";

    public IBrush SettingsIconColor => SelectedPage == SelectedPage.Settings  || (SelectedPage != SelectedPage.Settings && MouseOverPage == SelectedPage.Settings)
        ? new SolidColorBrush((Color)App.Current.Resources["ColorDefault"])
        : new SolidColorBrush(Colors.White);

    // Selected menu
    public SelectedPage SelectedPage { get; set; } = SelectedPage.Home;
    
    // Screen mode properties with notification
    public bool IsDesktopMode 
    { 
        get => _isDesktopMode; 
        set 
        { 
            if (_isDesktopMode != value)
            {
                _isDesktopMode = value; 
                OnPropertyChanged(nameof(IsDesktopMode));
                OnPropertyChanged(nameof(IsMobileMode));
                
                // No need to set margins since we're using Grid columns now
            }
        } 
    }
    
    public bool IsMobileMode 
    { 
        get => !_isDesktopMode;
    }
    
    private bool _isDesktopMode = true;
    private Home home;
    public static MainView Instance;
    
    // Implement INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    protected void UpdateMobileIconColors()
    {
        OnPropertyChanged(nameof(HomeIconColor));
        OnPropertyChanged(nameof(HomeIconValue));
        OnPropertyChanged(nameof(MoviesIconColor));
        OnPropertyChanged(nameof(MoviesIconValue));
        OnPropertyChanged(nameof(TvShowsIconColor));
        OnPropertyChanged(nameof(TvShowsIconValue));
        OnPropertyChanged(nameof(SearchIconColor));
        OnPropertyChanged(nameof(SearchIconValue));
        OnPropertyChanged(nameof(DownloadIconColor));
        OnPropertyChanged(nameof(DownloadIconValue));
        OnPropertyChanged(nameof(WatchHistoryIconColor));
        OnPropertyChanged(nameof(WatchHistoryIconValue));
        OnPropertyChanged(nameof(DiscoverIconColor));
        OnPropertyChanged(nameof(DiscoverIconValue));
        OnPropertyChanged(nameof(RecommendationsIconColor));
        OnPropertyChanged(nameof(RecommendationsIconValue));
        OnPropertyChanged(nameof(AccountIconColor));
        OnPropertyChanged(nameof(AccountIconValue));
        OnPropertyChanged(nameof(SettingsIconColor));
        OnPropertyChanged(nameof(SettingsIconValue));
    }
    
    public MainView()
    {
        InitializeComponent();
        screenWidth = this.Bounds.Width;
        Instance = this;
        System.ObservableExtensions.Subscribe(
            this.GetObservable(Visual.BoundsProperty),
            bounds =>
            {
                UpdateLayoutBasedOnSize();
                OnSizeChanged(new MySizeChangedEventArgs(bounds.Width,bounds.Height));
                screenWidth = bounds.Width;
            }
        );
        DataContext = this;
        
        home = new Home();
        SelectedPage = SelectedPage.Home;
        
        NavigationService.Instance.Initialize(MainContentControl);
        NavigationService.Instance.Navigate(home);
        
        DownloadsPage downloadsPage = new DownloadsPage();
        WatchHistoryPage watchHistoryPage = new WatchHistoryPage();
        
        Load();
    }
    public double screenWidth;
    public event EventHandler<MySizeChangedEventArgs> SizeChanged;

    protected virtual void OnSizeChanged(MySizeChangedEventArgs e)
    {
        SizeChanged?.Invoke(this, e);
    }
   
  
    
    private void MainView_OnLoaded(object sender, RoutedEventArgs e)
    {
       
    }

    
    private void UpdateLayoutBasedOnSize()
    {
        IsDesktopMode = this.Bounds.Width > 600;
    }
    
    private System.Timers.Timer timerJackett;

    public void Load()
    {
        try
        {
            Essentials.GetSelectedIndexers();
        }
        catch (Exception e)
        {
            Log.Error(e.Message + " " + e.StackTrace);
            timerJackett = new Timer(2000);
            timerJackett.Elapsed += TimerJackettOnElapsed;
            timerJackett.AutoReset = true;
            timerJackett.Start();
        }
    }
    private bool gotSelectedIndexer = false;

    private async void TimerJackettOnElapsed(object sender, ElapsedEventArgs e)
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

    public void SetContent(UserControl content)
    {
        if (content != null)
        {
            NavigationService.Instance.Navigate(content);
        }
    }
    

    // Mobile "More" button handler
    private void MobileButtonMore_OnTapped(object sender, RoutedEventArgs e)
    {
        try
        {
            // Toggle popup menu
            MobileMoreMenu.IsOpen = !MobileMoreMenu.IsOpen;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MobileButtonMore_OnTapped Exception: {ex.Message}");
        }
    }
    
    
    SelectedPage MouseOverPage = SelectedPage.None;

   
    private async void NavigationButtonTapped(object sender, RoutedEventArgs e)
    {
        try
        {
            string controlName = "";
            
            if (sender is Border navigationButton)
            {
                controlName = navigationButton.Name;
            }
            else if (sender is Button button)
            {
                controlName = button.Name;
                MobileMoreMenu.IsOpen = false;
            }
            
            if (controlName.StartsWith("Mobile"))
            {
                controlName = controlName.Replace("Mobile", "");
            }
            else if (controlName.StartsWith("Popup"))
            {
                controlName = controlName.Replace("Popup", "");
            }

            if (controlName == "ButtonBack")
            {
                NavigationService.Instance.GoBack();
            }
            
            if (controlName != "ButtonMore" && controlName != "ButtonBack")
            {
                if (AccountLoginPage.Instance != null)
                {
                    AccountLoginPage.Instance.Dispose();
                }
                
                // if (Home.Instance != null)
                // {
                //     Home.Instance.Dispose();
                //     Home.Instance = null;
                // }
                // if (MoviesPage.Instance != null)
                // {
                //     MoviesPage.Instance.Dispose();
                //     MoviesPage.Instance = null;
                // }
                // if (TvShowsPage.Instance != null)
                // {
                //     TvShowsPage.Instance.Dispose();
                //     TvShowsPage.Instance = null;
                // }
                // if (ExploreMorePage.Instance != null)
                // {
                //     ExploreMorePage.Instance.Dispose();
                //     ExploreMorePage.Instance = null;
                // }
                // if (MovieDetailsPage.Instance != null)
                // {
                //     MovieDetailsPage.Instance.Dispose();
                //     MovieDetailsPage.Instance = null;
                // }
            }
            

            var buttonActions = new Dictionary<string, (SelectedPage page, Func<UserControl> createPage)>
            {
                { "ButtonHome", (SelectedPage.Home, () =>
                {
                    return new Home();
                }) },
                { "ButtonSearch", (SelectedPage.Search, () =>
                {
                    if (SearchPageControl.Instance == null)
                    {
                        return new SearchPageControl();
                    }
                    return SearchPageControl.Instance; 
                }) },
                { "ButtonDownload", (SelectedPage.Download, () =>
                {
                    return DownloadsPage.Instance;
                }) },
                { "ButtonDiscover", (SelectedPage.Discover, () =>
                {
                    if (DiscoverPageControl.Instance == null)
                    {
                        return new DiscoverPageControl();
                    }
                    return DiscoverPageControl.Instance;
                }) },
                { "ButtonSettings", (SelectedPage.Settings, () => SettingsPage.GetSettingsPageInstance  != null ? SettingsPage.GetSettingsPageInstance : new SettingsPage()) },
                { "ButtonAccount", (SelectedPage.TmdbUserProfile, () => {
                    // Check if the user is already logged in and AccountPage instance exists
                    if (AccountPage.GetAccountPageInstance != null)
                    {
                        return AccountPage.GetAccountPageInstance;
                    }
                    
                    // Check if credentials exist but instance hasn't been created yet
                    if (!String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.TmdbUsername) &&
                        !String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.TmdbPassword))
                    {
                        // Create and return a new AccountPage
                        return new AccountPage();
                    }
                    
                    // If not logged in, show login page
                    return new AccountLoginPage();
                }) },
                { "ButtonMovie", (SelectedPage.Movies, () =>
                {
                   
                        return new MoviesPage();
                    
                }) },
                { "ButtonTvShow", (SelectedPage.TvShows, () =>
                {
                   
                        return new TvShowsPage();
                   
                }) },
                { "ButtonWatchHistory", (SelectedPage.WatchHistory, () =>
                {
                    if (WatchHistoryPage.GetWatchHistoryPageInstance != null)
                    {
                        return WatchHistoryPage.GetWatchHistoryPageInstance;
                    }
                    return new WatchHistoryPage();
                }) },
                { "ButtonRecommendations", (SelectedPage.Recommendations, () =>
                {
                    return new RecommendationsPage();
                }) }
            };
            
            if (buttonActions.TryGetValue(controlName, out var action))
            {
                SelectedPage = action.page;
                UpdateMobileIconColors(); // Update colors for mobile navigation
                NavigationService.Instance.Navigate(action.createPage());
            }
            
           
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NavigationButtonTapped Exception: {ex.Message}");
        }
    }
    

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        
    }

    private void PointerEnteredOnNavigationButton(object? sender, PointerEventArgs e)
    {
        var border = sender as Border;
        if (border != null)
        {
            if (border.Name == "ButtonBack")
            {
                MouseOverPage = SelectedPage.None;
            }
            else if (border.Name == "ButtonHome")
            {
                MouseOverPage = SelectedPage.Home;
            }
            else if (border.Name == "ButtonMovie")
            {
                MouseOverPage = SelectedPage.Movies;
            }
            else if (border.Name == "ButtonTvShow")
            {
                MouseOverPage = SelectedPage.TvShows;
            }
            else if (border.Name == "ButtonSearch")
            {
                MouseOverPage = SelectedPage.Search;
            }
            else if (border.Name == "ButtonWatchHistory")
            {
                MouseOverPage = SelectedPage.WatchHistory;
            }
            else if (border.Name == "ButtonAccount")
            {
                MouseOverPage = SelectedPage.TmdbUserProfile;
            }
            else if (border.Name == "ButtonSettings")
            {
                MouseOverPage = SelectedPage.Settings;
            }
            else if (border.Name == "ButtonDiscover")
            {
                MouseOverPage = SelectedPage.Discover;
            }
            else if (border.Name == "ButtonRecommendations")
            {
                MouseOverPage = SelectedPage.Recommendations;
            }
            else if (border.Name == "ButtonDownload")
            {
                MouseOverPage = SelectedPage.Download;
            }
            
            UpdateMobileIconColors();
        }
    }

    private void PointerExitedOnNavigationButton(object? sender, PointerEventArgs e)
    {
        var border = sender as Border;
        if (border != null)
        {
            MouseOverPage = SelectedPage.None;
            UpdateMobileIconColors();
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
    Recommendations,
    Download,
    TmdbUserProfile,
    Settings,
    WatchHistory,
    None
}



public class MySizeChangedEventArgs : EventArgs
{

    public double width { get; set; }
    public double height { get; set; }
    public MySizeChangedEventArgs(double width,double height)
    {
        this.width = width;
        this.height = height;
    }
}