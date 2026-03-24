using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NetStream.Navigation;
using NetStream.Views;
using Serilog;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;

namespace NetStream
{
    public partial class CastCreditsPageControl : UserControl,IDisposable
    {
        private Cast cast;
        
        public CastCreditsPageControl()
        {
            InitializeComponent();
        }
        
        public CastCreditsPageControl(Cast cast)
        {
            InitializeComponent();
            this.cast = cast;
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
            Load();
            OnPageLoad();
        }
        
        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.width);
        }

        private List<ActingCredits> actingCredits = new List<ActingCredits>();
        private List<ProductionCredits> productionCredits = new List<ProductionCredits>();

        private async Task GetActingCredits()
        {
            try
            {
                var language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;
                var person = await Service.client.GetPersonAsync(cast.Id, language, PersonMethods.MovieCredits | PersonMethods.TvCredits);

                if (person != null)
                {
                    if (person.MovieCredits.Cast.Count > 0)
                    {
                        foreach (var personMovieCredit in person.MovieCredits.Cast)
                        {
                            ActingCredits actingCredit = new ActingCredits()
                            {
                                Id = personMovieCredit.Id,
                                Name = personMovieCredit.Title,
                                ReleaseDate = personMovieCredit.ReleaseDate.HasValue
                                    ? personMovieCredit.ReleaseDate.Value.Year.ToString()
                                    : "     -",
                                Role = personMovieCredit.Character,
                                DateTime = personMovieCredit.ReleaseDate.HasValue ? personMovieCredit.ReleaseDate.Value : DateTime.MinValue,
                                ShowType = ShowType.Movie
                            };
                            actingCredits.Add(actingCredit);
                        }
                    }

                    if (person.TvCredits.Cast.Count > 0)
                    {
                        foreach (var tvRole in person.TvCredits.Cast)
                        {
                            ActingCredits actingCredit = new ActingCredits()
                            {
                                Id = tvRole.Id,
                                Name = tvRole.Name,
                                ReleaseDate = tvRole.FirstAirDate.HasValue
                                    ? tvRole.FirstAirDate.Value.Year.ToString()
                                    : "     -",
                                Role = tvRole.Character,
                                DateTime = tvRole.FirstAirDate.HasValue ? tvRole.FirstAirDate.Value : DateTime.MinValue,
                                ShowType = ShowType.TvShow
                            };
                            actingCredits.Add(actingCredit);
                        }
                    }

                    actingCredits = actingCredits.OrderByDescending(x => x.DateTime == DateTime.MinValue).ThenByDescending(x => x.DateTime).ToList();
                    CreditsActingDisplay.ItemsSource = actingCredits;

                    if (person.MovieCredits.Crew.Count > 0)
                    {
                        foreach (var movieJob in person.MovieCredits.Crew)
                        {
                            ProductionCredits productionCredit = new ProductionCredits()
                            {
                                Id = movieJob.Id,
                                Name = movieJob.Title,
                                DateTime = movieJob.ReleaseDate.HasValue ? movieJob.ReleaseDate.Value : DateTime.MinValue,
                                Job = movieJob.Job,
                                ReleaseDate = movieJob.ReleaseDate.HasValue ? movieJob.ReleaseDate.Value.Year.ToString() : "     -",
                                ShowType = ShowType.Movie
                            };
                            productionCredits.Add(productionCredit);
                        }
                    }

                    if (person.TvCredits.Crew.Count > 0)
                    {
                        foreach (var tvJob in person.TvCredits.Crew)
                        {
                            ProductionCredits productionCredit = new ProductionCredits()
                            {
                                Id = tvJob.Id,
                                DateTime = tvJob.FirstAirDate.HasValue ? tvJob.FirstAirDate.Value : DateTime.MinValue,
                                Job = tvJob.Job,
                                Name = tvJob.Name,
                                ReleaseDate = tvJob.FirstAirDate.HasValue ? tvJob.FirstAirDate.Value.Year.ToString() : "     -",
                                ShowType = ShowType.TvShow
                            };
                            productionCredits.Add(productionCredit);
                        }
                    }

                    productionCredits = productionCredits.OrderByDescending(x => x.DateTime == DateTime.MinValue)
                        .ThenByDescending(x => x.DateTime).ToList();
                    CreditsProductionDisplay.ItemsSource = productionCredits;
                    
                    // Responsive düzeni yükleme işlemi bittikten sonra uygula
                    await Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        ApplyResponsiveLayout(MainView.Instance.Bounds.Width);
                    });
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void Load()
        {
            await GetActingCredits();
        }

        private DispatcherTimer timer;
        private void OnPageLoad()
        {
            // UI yüklendikten sonra responsive düzeni uygula
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
                ApplyResponsiveLayout(MainView.Instance.Bounds.Width);
                timer.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Timer içinde responsive layout uygulama hatası: {ex.Message}");
            }
        }

        private void CastCreditsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
           
        }
        
        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                double titleFontSize = CalculateScaledValue(width, 18, 34, 320, 3810);
                double textFontSize = CalculateScaledValue(width, 14, 24, 320, 3810);
                
                if (ActingTitle != null)
                    ActingTitle.FontSize = titleFontSize;
                
                if (ProductionTitle != null)
                    ProductionTitle.FontSize = titleFontSize;
                
                foreach (var item in CreditsActingDisplay.GetVisualDescendants())
                {
                    if (item is TextBlock textBlock)
                    {
                        if (textBlock.Name == "ActingReleaseDate" || textBlock.Name == "ActingName" || textBlock.Name == "ActingRole")
                        {
                            textBlock.FontSize = textFontSize;
                        }
                    }
                }

                foreach (var item in CreditsProductionDisplay.GetVisualDescendants())
                {
                    if (item is TextBlock textBlock)
                    {
                        if (textBlock.Name == "ProductionReleaseDate" || textBlock.Name == "ProductionName" || textBlock.Name == "ProductionJob")
                        {
                            textBlock.FontSize = textFontSize;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Console.WriteLine(errorMessage);
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
        
        private void CreditsActingDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Responsive düzeni güncelle
            ApplyResponsiveLayout(MainView.Instance.Bounds.Width);
        }
        

        private void CreditsProductionDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Responsive düzeni güncelle
            ApplyResponsiveLayout(MainView.Instance.Bounds.Width);
        }

        private void CastCreditsPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var stackPanel = sender as StackPanel;
                if(stackPanel == null) return;
                var selectedActing = stackPanel.DataContext as ActingCredits;
                if (selectedActing != null)
                {
                    var movieDetailsPage = new MovieDetailsPage(selectedActing.Id,selectedActing.ShowType);
                    NavigationService.Instance.Navigate(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void StackPanelCreditsProduction_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var stackPanel = sender as StackPanel;
                if(stackPanel == null) return;
                var selectedProduction = stackPanel.DataContext as ProductionCredits;
                if (selectedProduction != null)
                {
                    var movieDetailsPage = new MovieDetailsPage(selectedProduction.Id,selectedProduction.ShowType);
                    NavigationService.Instance.Navigate(movieDetailsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        public void Dispose()
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
            timer.Tick -= TimerOnTick;
            timer.Stop();
            timer = null;
        }
    }
} 