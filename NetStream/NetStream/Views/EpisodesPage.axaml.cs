using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Serilog;
using TMDbLib.Objects.Search;
using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Avalonia.Threading;
using System.Collections.Specialized;

namespace NetStream.Views
{
    public partial class EpisodesPage : UserControl
    {
        private Movie selectedTvShow;
        private double currentWidth;

        public EpisodesPage()
        {
            InitializeComponent();
        }

        public EpisodesPage(Movie selectedTvShow)
        {
            InitializeComponent();
            this.selectedTvShow = selectedTvShow;
            
            // Service.TvShowSeasons koleksiyonu değiştiğinde otomatik olarak responsive layout uygula
            Service.TvShowSeasons.CollectionChanged += (sender, args) => 
            {
                // Koleksiyon değiştiğinde responsive layout uygula (100ms gecikme ile)
                Dispatcher.UIThread.Post(() => 
                {
                    try 
                    {
                        currentWidth = MainView.Instance.Bounds.Width;
                        ApplyResponsiveLayout(currentWidth);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Koleksiyon değişimi sonrası responsive layout hatası: {ex.Message}");
                    }
                }, Avalonia.Threading.DispatcherPriority.Render);
            };
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.width);
        }


        private async void EpisodesPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                MainView.Instance.SizeChanged += InstanceOnSizeChanged;
                Service.TvShowSeasons.Clear();
                Service.TvSeasonEpisodes.Clear();
                SeasonsDisplay.ItemsSource = Service.TvShowSeasons;
                EpisodesDisplay.ItemsSource = Service.TvSeasonEpisodes;

                await Service.GetTvShowSeasons(selectedTvShow.Id);

                // UI'nin tamamen yüklenmesi için DispatcherTimer kullanarak 300ms bekle
                DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };
                
                timer.Tick += (s, args) =>
                {
                    try
                    {
                        currentWidth = MainView.Instance.Bounds.Width;
                        ApplyResponsiveLayout(currentWidth);
                        
                        // Tek seferlik çalışması için timer'ı durdur
                        timer.Stop();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Timer içinde responsive layout uygulama hatası: {ex.Message}");
                    }
                };
                
                timer.Start();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0)
            {
                throw new ArgumentOutOfRangeException("decimalPlaces");
            }

            if (value < 0)
            {
                return "-" + SizeSuffix(-value, decimalPlaces);
            }

            if (value == 0)
            {
                return string.Format("{0:n" + decimalPlaces + "} bytes", 0);
            }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        private void UIElement_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var button = sender as Button;
                if (button.DataContext is Season s)
                {
                    var torrentPage = new TorrentsPage(selectedTvShow, s.SeasonNumber);
                    MovieDetailsPage.Instance.MovieDetailsNavigation.Content = torrentPage;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiUrl) ||
                    String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiKey))
                {
                    button.IsVisible = false;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void EpisodesPage_OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            try
            {
                currentWidth = e.NewSize.Width;
                ApplyResponsiveLayout(currentWidth);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                double titleFontSize = CalculateScaledValue(width, 20, 28, 320, 3810);
                double seasonFontSize = CalculateScaledValue(width, 16, 20, 320, 3810);
                double episodeFontSize = CalculateScaledValue(width, 14, 18, 320, 3810);
                double descriptionFontSize = CalculateScaledValue(width, 11, 14, 320, 3810);
                
                // Null kontrolü
                if (SeasonsTitle != null) 
                    SeasonsTitle.FontSize = titleFontSize;
                
                if (EpisodesTitle != null)
                    EpisodesTitle.FontSize = titleFontSize;

                if (SeasonsDisplay == null) return;

                try
                {
                    foreach (var item in SeasonsDisplay.GetVisualDescendants())
                    {
                        if (item is Border border && border.Name == "MovieCardBorder")
                        {
                            double cardWidth = CalculateScaledValue(width, 150, 300, 320, 3810);
                            double cardHeight = cardWidth * 1.447; // Maintain aspect ratio
                            border.Width = cardWidth;
                            border.Height = cardHeight;
                        }
                        else if (item is TextBlock textBlock)
                        {
                            if (textBlock.Name == "SeasonDescription")
                            {
                                textBlock.FontSize = descriptionFontSize;
                                textBlock.Width = CalculateScaledValue(width,150,700, 320, 3810);
                            }
                            else
                                textBlock.FontSize = textBlock.FontSize > 15 ? seasonFontSize : descriptionFontSize;
                        }
                        else if (item is StackPanel stackPanel)
                        {
                            if (stackPanel.Name == "SeasonInfoStackPanel")
                            {
                                stackPanel.Margin = new Thickness(CalculateScaledValue(width,5,20,320,3810),0,0,0);
                            }
                            else if(stackPanel.Name == "StackPanelInfo")
                            {
                                if (width <= 474)
                                {
                                    stackPanel.Orientation = Orientation.Vertical;
                                }
                                else
                                {
                                    stackPanel.Orientation = Orientation.Horizontal;
                                }
                            }
                        }
                        else if (item is Button button)
                        {
                            if (button.Name == "ButtonDownloadAllSeasons")
                            {
                                if (width <= 474)
                                {
                                    button.Width = CalculateScaledValue(width,150,200, 320, 3810);
                                    button.Margin = new Thickness(-20,10,0,0);
                                }
                                else
                                {
                                    button.Width = CalculateScaledValue(width,150,200, 320, 3810);
                                    button.Margin = new Thickness(15,-5,0,0);
                                }
                            }
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"SeasonsDisplay responsive ayarları uygulanırken hata: {ex.Message}");
                }
                
                if (EpisodesDisplay == null) return;

                try
                {
                    foreach (var item in EpisodesDisplay.GetVisualDescendants())
                    {
                        if (item is Border border && border.Name == "MovieCardBorder")
                        {
                            double cardWidth = CalculateScaledValue(width, 150, 434, 320, 3810);
                            double cardHeight = cardWidth * 0.581; // Maintain aspect ratio
                            border.Width = cardWidth;
                            border.Height = cardHeight;
                        }
                        else if (item is TextBlock textBlock)
                        {
                            if (textBlock.Name == "EpisodeDescription")
                            {
                                textBlock.FontSize = descriptionFontSize;
                                textBlock.Width = CalculateScaledValue(width,150,700, 320, 3810);
                            }
                            else
                                textBlock.FontSize = textBlock.FontSize > 15 ? episodeFontSize : descriptionFontSize;
                        }
                        else if (item is StackPanel stackPanel)
                        {
                            if (stackPanel.Name == "EpisodeInfoStackPanel")
                            {
                                stackPanel.Margin = new Thickness(CalculateScaledValue(width,5,20,320,3810),0,0,0);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"EpisodesDisplay responsive ayarları uygulanırken hata: {ex.Message}");
                }
                

                // Adjust WrapPanel orientation based on width
                try
                {
                    var seasonsWrapPanel = SeasonsDisplay?.FindDescendantOfType<WrapPanel>();
                    var episodesWrapPanel = EpisodesDisplay?.FindDescendantOfType<WrapPanel>();

                    if (width < 800)
                    {
                        if (seasonsWrapPanel != null)
                            seasonsWrapPanel.Orientation = Orientation.Vertical;

                        if (episodesWrapPanel != null)
                            episodesWrapPanel.Orientation = Orientation.Vertical;
                    }
                    else
                    {
                        if (seasonsWrapPanel != null)
                            seasonsWrapPanel.Orientation = Orientation.Horizontal;

                        if (episodesWrapPanel != null)
                            episodesWrapPanel.Orientation = Orientation.Horizontal;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"WrapPanel ayarları uygulanırken hata: {ex.Message}");
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private double CalculateScaledValue(double width, double minValue, double maxValue, double minWidth,
            double maxWidth)
        {
            // Ensure width is in range
            width = Math.Max(minWidth, Math.Min(width, maxWidth));

            // Calculate scaling factor (0 to 1)
            double scaleFactor = (width - minWidth) / (maxWidth - minWidth);

            // Scale the value
            return minValue + (scaleFactor * (maxValue - minValue));
        }


        private void EpisodeOnPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiUrl) ||
                    String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiKey))
                {
                    return;
                }
                
                var stackPanel = sender as StackPanel;
                if(stackPanel == null) return;
                var selectedEpisode = stackPanel.DataContext as Episode;
                if (selectedEpisode != null)
                {
                    var torrentsPage = new TorrentsPage(selectedTvShow, selectedEpisode.SeasonNumber,
                        selectedEpisode.EpisodeNumber);
                    MovieDetailsPage.Instance.MovieDetailsNavigation.Content = torrentsPage;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void SeasonOnPressed(object? sender, PointerPressedEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if(stackPanel == null) return;
            var Season = stackPanel.DataContext as Season;
            if (Season != null)
            {
                await Service.GetSeasonEpisodes(selectedTvShow.Id, Season.SeasonNumber);
                
                // Bölümler yüklendikten sonra responsive düzeni güncelle
                Dispatcher.UIThread.Post(() => 
                {
                    try 
                    {
                        currentWidth = MainView.Instance.Bounds.Width;
                        ApplyResponsiveLayout(currentWidth);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Responsive layout uygulama hatası: {ex.Message}");
                    }
                }, Avalonia.Threading.DispatcherPriority.Render);
            }
        }

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
    }


 }
 