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
using Serilog;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for CastCreditsPage.xaml
    /// </summary>
    public partial class CastCreditsPage : Page
    {
        private Cast cast;
        public CastCreditsPage(Cast cast)
        {
            InitializeComponent();
            this.cast = cast;
            Load();
        }

        private List<ActingCredits> actingCredits = new List<ActingCredits>();
        private List<ProductionCredits> productionCredits = new List<ProductionCredits>();

        private async Task GetActingCredits()
        {
            try
            {
                var language = AppSettingsManager.appSettings.IsoTmdbResultLanguage;
                var person = await Service.client.GetPersonAsync(cast.Id,language,PersonMethods.MovieCredits|PersonMethods.TvCredits);

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

                    actingCredits = actingCredits.OrderByDescending(x => x.DateTime == DateTime.MinValue).ThenByDescending(x=> x.DateTime).ToList();
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
                                ReleaseDate = movieJob.ReleaseDate.HasValue ? movieJob.ReleaseDate.Value.Year.ToString():"     -",
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

        private async void CastCreditsPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void CreditsActingDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedActing = CreditsActingDisplay.SelectedItem as ActingCredits;
                if (selectedActing != null)
                {
                    MovieDetailsPage movieDetailsPage =
                        new MovieDetailsPage(selectedActing.Id, selectedActing.ShowType);
                    HomePage.GetHomePageInstance.HomePageNavigation.Navigate(movieDetailsPage);
                    CreditsActingDisplay.UnselectAll();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CreditsActingDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }

        private void CreditsProductionDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedProduction = CreditsProductionDisplay.SelectedItem as ProductionCredits;
                if (selectedProduction != null)
                {
                    MovieDetailsPage movieDetailsPage =
                        new MovieDetailsPage(selectedProduction.Id, selectedProduction.ShowType);
                    HomePage.GetHomePageInstance.HomePageNavigation.Navigate(movieDetailsPage);
                    CreditsProductionDisplay.UnselectAll();
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void CreditsProductionDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }

        private void CastCreditsPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (actingCredits != null)
                actingCredits.Clear();

            if (productionCredits != null)
                productionCredits.Clear();

            if (CreditsActingDisplay != null)
                CreditsActingDisplay.ItemsSource = null;

            if (CreditsProductionDisplay != null)
                CreditsProductionDisplay.ItemsSource = null;
        }
    }
}
