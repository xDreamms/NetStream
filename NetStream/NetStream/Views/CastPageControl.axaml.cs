using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Serilog;
using TMDbLib.Objects.General;
using TMDbLib.Objects.People;
using Avalonia.Layout;
using Avalonia.VisualTree;
using NetStream.Views;

namespace NetStream
{
    public partial class CastPageControl : UserControl,IDisposable
    {
        private Cast cast;
        private CastKnownForPageControl castKnownForPage;
        private double currentWidth;
        
        public CastPageControl()
        {
            InitializeComponent();
        }
        
        public CastPageControl(Cast cast)
        {
            InitializeComponent();
            this.cast = cast;
            castKnownForPage = new CastKnownForPageControl(cast);
            CastPageNavigation.Content = castKnownForPage;
            currentWidth = MainView.Instance.Bounds.Width;
            Load();
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.width);
            if (ClickedTabItem == ClickedTabItem.Photos)
            {
                if (e.width <= 800)
                {
                    Grid.SetColumn(MenuLine, 0);
                    Grid.SetRow(MenuLine, 2);
                }
                else
                {
                    Grid.SetColumn(MenuLine, 4);
                    Grid.SetRow(MenuLine, 0);
                }
            }
        }

        private ExternalIdsPerson externalIdsPerson;
        private async Task<Cast> GetCastInfo()
        {
            try
            {
                var language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;
                var person = await Service.client.GetPersonAsync(cast.Id, language, PersonMethods.Images |PersonMethods.ExternalIds);

                if (person != null)
                {
                    var Cast = new Cast()
                    {
                        Biography = person.Biography,
                        BirthDate = person.Birthday != null ? person.Birthday.Value.ToShortDateString():"",
                        BirthPlace = person.PlaceOfBirth,
                        Poster = Service.client.GetImageUrl("w500", person.ProfilePath).AbsoluteUri,
                        Id = person.Id,
                        KnownFor = person.KnownForDepartment,
                        Name = person.Name
                    };
                    externalIdsPerson = person.ExternalIds;
                    return Cast;
                }

            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
            return null;
        }

        private async void SetVisibilityForLinks()
        {
            try
            {
                if(externalIdsPerson == null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        BtnFacebook.IsVisible = false;
                        BtnInstagram.IsVisible = false;
                        BtnImadb.IsVisible = false;
                        BtnTwitter.IsVisible = false;
                    });
                }
                else
                {
                    if (String.IsNullOrWhiteSpace(externalIdsPerson.FacebookId))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            BtnFacebook.IsVisible = false;
                        });
                    }
                    if (String.IsNullOrWhiteSpace(externalIdsPerson.InstagramId))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            BtnInstagram.IsVisible = false;
                        });
                    }
                    if (String.IsNullOrWhiteSpace(externalIdsPerson.ImdbId))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            BtnImadb.IsVisible = false;
                        });
                    }
                    if (String.IsNullOrWhiteSpace(externalIdsPerson.TwitterId))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            BtnTwitter.IsVisible = false;
                        });
                    }
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private DispatcherTimer timer;
        private async void Load()
        {
            CastInfo.DataContext = await GetCastInfo();
            SetVisibilityForLinks();
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
            currentWidth = MainView.Instance.screenWidth;
            ApplyResponsiveLayout(currentWidth);
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            
            timer.Tick += TimerOnTick;
            
            timer.Start();
        }

        private void TimerOnTick(object? sender, EventArgs e)
        {
            try
            {
                currentWidth = MainView.Instance.screenWidth;
                ApplyResponsiveLayout(currentWidth);
                timer.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Timer içinde responsive layout uygulama hatası: {ex.Message}");
            }
        }

        private async void CastPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }
        
   
        
        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                double titleFontSize = CalculateScaledValue(width, 20, 28, 320, 3810);
                double normalFontSize = CalculateScaledValue(width, 10, 18, 320, 3810);
                double descriptionFontSize = CalculateScaledValue(width, 12, 16, 320, 3810);
                double iconSize = CalculateScaledValue(width, 16, 24, 320, 3810);
                double menuTabFontSize = CalculateScaledValue(width, 14, 28, 320, 3810);

                // CastInfo Grid
                if (PosterImage != null)
                {
                    if (width < 800)
                    {
                        Grid.SetColumn(PosterImage, 0);
                        Grid.SetColumnSpan(PosterImage, 1);
                        Grid.SetColumn(PosterGrid, 0);
                        Grid.SetColumnSpan(PosterGrid, 3);
                    }
                    else
                    {
                            Grid.SetColumn(PosterImage, 0);
                            Grid.SetColumnSpan(PosterImage, 1);
                            
                            
                            Grid.SetColumn(PosterGrid, 1);
                            Grid.SetColumnSpan(PosterGrid, 1);
                        
                    }
                }
                
                // Biyografi genişliğini ayarla
                if (BiographyPanel != null)
                {
                    BiographyPanel.Width = CalculateScaledValue(width, 300, 650, 320, 3810);
                }

                PosterImage.Width = CalculateScaledValue(width, 100, 550, 320, 3810);



                if (width <= 800)
                {
                    
                    Grid.SetRow(PhotosMenuTab,2);
                    Grid.SetColumn(PhotosMenuTab,0);
                }
                else
                {
                
                    Grid.SetRow(PhotosMenuTab,0);
                    Grid.SetColumn(PhotosMenuTab,4);
                }
                
                if (KnownForMenuTab != null)
                    KnownForMenuTab.FontSize = menuTabFontSize;
                
                if (CreditsMenuTab != null)
                    CreditsMenuTab.FontSize = menuTabFontSize;
                
                if (PhotosMenuTab != null)
                    PhotosMenuTab.FontSize = menuTabFontSize;
                
                GridTab.ColumnDefinitions[1].Width = new GridLength(CalculateScaledValue(width, 10, 80, 320, 3810));
                GridTab.ColumnDefinitions[3].Width = new GridLength(CalculateScaledValue(width, 10, 80, 320, 3810));
                // Yazı boyutlarını ayarla
                if (CastName != null)
                    CastName.FontSize = titleFontSize;
                
                if (Biography != null)
                    Biography.FontSize = descriptionFontSize;
                
                if (KnownForLabel != null)
                    KnownForLabel.FontSize = normalFontSize;
                
                if (PlaceOfBirthLabel != null)
                    PlaceOfBirthLabel.FontSize = normalFontSize;
                
                if (BirthdayLabel != null)
                    BirthdayLabel.FontSize = normalFontSize;
                
                if (KnownForValue != null)
                    KnownForValue.FontSize = normalFontSize;
                
                if (PlaceOfBirthValue != null)
                    PlaceOfBirthValue.FontSize = normalFontSize;
                
                if (BirthdayValue != null)
                    BirthdayValue.FontSize = normalFontSize;
                
                // Sosyal medya ikonlarını boyutlandır
                if (BtnFacebook != null)
                    BtnFacebook.FontSize = iconSize;
                
                if (BtnInstagram != null)
                    BtnInstagram.FontSize = iconSize;
                
                if (BtnImadb != null)
                    BtnImadb.FontSize = iconSize;
                
                if (BtnTwitter != null)
                    BtnTwitter.FontSize = iconSize;
                
                // Menü tasarımını ekran boyutuna göre ayarla
                if (MenuTabsGrid != null)
                {
                    if (width < 800)
                    {
                        MenuTabsGrid.Margin = new Thickness(0, 30, 0, 0);
                    }
                    else
                    {
                        MenuTabsGrid.Margin = new Thickness(0);
                    }
                }
                
                // Content grid kenarlarını ayarla
                if (CastPageContentGrid != null)
                {
                    if (width < 800)
                    {
                        CastPageContentGrid.Margin = new Thickness(0, 30, 0, 0);
                        
                        var column1 = CastPageContentGrid.ColumnDefinitions[0];
                        var column3 = CastPageContentGrid.ColumnDefinitions[2];
                        
                        column1.Width = new GridLength(0.05, GridUnitType.Star);
                        column3.Width = new GridLength(0.05, GridUnitType.Star);
                    }
                    else
                    {
                        CastPageContentGrid.Margin = new Thickness(0, 50, 0, 0);
                        
                        var column1 = CastPageContentGrid.ColumnDefinitions[0];
                        var column3 = CastPageContentGrid.ColumnDefinitions[2];
                        
                        column1.Width = new GridLength(0.1, GridUnitType.Star);
                        column3.Width = new GridLength(0.1, GridUnitType.Star);
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        private double CalculateScaledValue(double width, double minValue, double maxValue, double minWidth, double maxWidth)
        {
            // Ensure width is in range
            width = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Calculate scaling factor (0 to 1)
            double scaleFactor = (width - minWidth) / (maxWidth - minWidth);
            
            // Scale the value
            return minValue + (scaleFactor * (maxValue - minValue));
        }

        private void GetHomePageInstanceOnMyEvent(object? sender, EventArgs e)
        {
            // Avalonia'da Navigation service olmadığı için farklı bir yaklaşım
            // App.Current.MainWindow.Content = previousPage veya router kullanılabilir
        }

        private ClickedTabItem ClickedTabItem = ClickedTabItem.KnownFor;
        private async void KnownForMenuTab_OnTapped(object sender, RoutedEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Grid.SetRow(MenuLine, 0);
                    Grid.SetColumn(MenuLine, 0);
                    KnownForMenuTab.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    CreditsMenuTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    PhotosMenuTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    ClickedTabItem = ClickedTabItem.KnownFor;
                });

                castKnownForPage = new CastKnownForPageControl(cast);
                CastPageNavigation.Content = castKnownForPage;
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private CastCreditsPageControl castCreditsPage;
        private async void CreditsMenuTab_OnTapped(object sender, RoutedEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Grid.SetRow(MenuLine, 0);
                    Grid.SetColumn(MenuLine, 2);
                    KnownForMenuTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    CreditsMenuTab.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    PhotosMenuTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    ClickedTabItem = ClickedTabItem.Credits;
                });

                castCreditsPage = new CastCreditsPageControl(cast);
                CastPageNavigation.Content = castCreditsPage;
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private CastPhotosPageControl castPhotosPage;
        private async void PhotosMenuTab_OnTapped(object sender, RoutedEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (MainView.Instance.Bounds.Width <= 800)
                    {
                        Grid.SetColumn(MenuLine, 0);
                        Grid.SetRow(MenuLine, 2);
                    }
                    else
                    {
                        Grid.SetColumn(MenuLine, 4);
                        Grid.SetRow(MenuLine, 0);
                    }
                    
                    KnownForMenuTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    CreditsMenuTab.Foreground = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    PhotosMenuTab.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    ClickedTabItem = ClickedTabItem.Photos;
                });

                castPhotosPage = new CastPhotosPageControl(cast);
                CastPageNavigation.Content = castPhotosPage;
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnFacebook_OnTapped(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(externalIdsPerson.FacebookId))
                {
                    Process.Start(new ProcessStartInfo("https://www.facebook.com/" + externalIdsPerson.FacebookId) { UseShellExecute = true });
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnInstagram_OnTapped(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(externalIdsPerson.InstagramId))
                {
                    Process.Start(new ProcessStartInfo("https://instagram.com/" + externalIdsPerson.InstagramId) { UseShellExecute = true });
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnImadb_OnTapped(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(externalIdsPerson.ImdbId))
                {
                    Process.Start(new ProcessStartInfo("https://www.imdb.com/name/" + externalIdsPerson.ImdbId) { UseShellExecute = true });
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnTwitter_OnTapped(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(externalIdsPerson.TwitterId))
                {
                    Process.Start(new ProcessStartInfo("https://x.com/" + externalIdsPerson.TwitterId) { UseShellExecute = true });
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnFacebook_OnPointerEnter(object sender, PointerEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BtnFacebook.Foreground = new SolidColorBrush(App.Current.Resources["ColorDefault"] as Color? ?? Colors.Blue);
                });
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnFacebook_OnPointerLeave(object sender, PointerEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BtnFacebook.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                });
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnInstagram_OnPointerEnter(object sender, PointerEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BtnInstagram.Foreground = new SolidColorBrush(App.Current.Resources["ColorDefault"] as Color? ?? Colors.Blue);
                });
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnInstagram_OnPointerLeave(object sender, PointerEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BtnInstagram.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                });
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnImadb_OnPointerEnter(object sender, PointerEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BtnImadb.Foreground = new SolidColorBrush(App.Current.Resources["ColorDefault"] as Color? ?? Colors.Blue);
                });
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnImadb_OnPointerLeave(object sender, PointerEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BtnImadb.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                });
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnTwitter_OnPointerEnter(object sender, PointerEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BtnTwitter.Foreground = new SolidColorBrush(App.Current.Resources["ColorDefault"] as Color? ?? Colors.Blue);
                });
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnTwitter_OnPointerLeave(object sender, PointerEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BtnTwitter.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                });
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                var scrollViewer = sender as ScrollViewer;
                if(scrollViewer == null) return;
                if (castKnownForPage?.isItemLoadingFinished != true) return;
                if (scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height - 50)
                {
                    if (castKnownForPage.MoviesPageIndex <= castKnownForPage.MaxMoviesPage || castKnownForPage.TvShowsPageIndex <= castKnownForPage.MaxTvShowsPage)
                        await castKnownForPage.GetKnownForMovies();
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CastPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
           
        }

        public void Dispose()
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
            timer.Stop();
            timer.Tick -= TimerOnTick;
            timer = null;

            if (castKnownForPage != null)
            {
                castKnownForPage.Dispose();
            }

            if (castCreditsPage != null)
            {
                castCreditsPage.Dispose();
            }

            if (castPhotosPage != null)
            {
                castPhotosPage.Dispose();
            }
        }
    }


    public enum ClickedTabItem
    {
        KnownFor,
        Credits,
        Photos
    }
} 