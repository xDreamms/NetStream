using MaterialDesignThemes.Wpf;
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
using System.Xml.Linq;
using Serilog;
using TMDbLib.Objects.Discover;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for DiscoverPage.xaml
    /// </summary>
    public partial class DiscoverPage : Page,IDisposable
    {
        private List<int> selectedGenres = new List<int>();
        private int page = 1;
        
        public DiscoverPage()
        {
            InitializeComponent();
            FillComboBoxes();
            Load();
        }

        private async void Load()
        {
            GenresDisplay.ItemsSource = await Service.GetMovieGenres();
        }


        private async void FillComboBoxes()
        {
            try
            {
                List<string> mediaTypes = new List<string>() { Application.Current.Resources["MovieString"].ToString()
                    , Application.Current.Resources["TvShowString"].ToString() };
                ShowTypeComboBox.ItemsSource = mediaTypes;
                ShowTypeComboBox.SelectedIndex = 0;

                List<string> sortList = new List<string>()
                {
                    Application.Current.Resources["PopularityDescendingString"].ToString(),
                    Application.Current.Resources["PopularityAscendingString"].ToString(),
                    Application.Current.Resources["RatingDescendingString"].ToString(),
                    Application.Current.Resources["RatingAscendingString"].ToString(),
                    Application.Current.Resources["ReleaseDateDescendingString"].ToString(),
                    Application.Current.Resources["ReleaseDateAscendingString"].ToString(),
                    Application.Current.Resources["TitleAZString"].ToString(),
                    Application.Current.Resources["TitleZAString"].ToString(),
                };
                SortComboBox.ItemsSource = sortList;
                SortComboBox.SelectedIndex = -1;

                ComboBoxLanguages.ItemsSource = await Service.GetLanguages();
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        

        private async void DiscoverPage_OnLoaded(object sender, RoutedEventArgs e)
        {

            try
            {
                if (MovieDetailsPage.current == null)
                {
                    MovieDetailsPage.current = new SourceProviderInfo()
                    {
                        Id = 0
                    };
                }
                else
                {
                    MovieDetailsPage.current.Id = 0;
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
            
        }

        private void BorderGenre_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var border = sender as Border;
                if (border.DataContext is Genre)
                {
                    Genre selectedGenre = (Genre)border.DataContext;

                    if (selectedGenres.Any(x=> x==selectedGenre.Id))
                    {
                        border.Background = new SolidColorBrush(Colors.Transparent);
                        border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        selectedGenre.IsSelected = false;
                        selectedGenres.Remove(selectedGenre.Id);
                    }
                    else
                    {
                        border.Background = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                        border.BorderBrush = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                        selectedGenre.IsSelected = true;
                        selectedGenres.Add(selectedGenre.Id);
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BorderGenre_OnMouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                var border = sender as Border;
                if (border.DataContext is Genre)
                {
                    border.Background = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                    border.BorderBrush = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BorderGenre_OnMouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                var border = sender as Border;
                if (border.DataContext is Genre)
                {
                    Genre selectedGenre = (Genre)border.DataContext;
                    if (!(selectedGenres.Any(x=> x == selectedGenre.Id)))
                    {
                        border.Background = new SolidColorBrush(Colors.Transparent);
                        border.BorderBrush = new SolidColorBrush(Colors.White);
                    }
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void DiscoverPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void MoviesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedMovie = MoviesDisplay.SelectedItem as Movie;
                if (selectedMovie != null)
                {
                    MovieDetailsPage movieDetailsPage = new MovieDetailsPage(selectedMovie);
                    HomePage.NavigationService.Navigate(movieDetailsPage);
                    MoviesDisplay.UnselectAll();
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MoviesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            try
            {
                if (MoviesDisplay.ItemsSource != null)
                {
                    if  (e.VerticalOffset == e.ExtentHeight - e.ViewportHeight)
                    {
                        if (Equals(ShowTypeComboBox.SelectionBoxItem, Application.Current.Resources["MovieString"].ToString()))
                        {
                            page++;
                            await Service.GetMorePagesDiscoverMovies(GetSelectedGenres(), GetAfterDateTime(), GetBeforeDateTime(),
                                GetMinVoteAverage(), GetMinVoteCounts(), GetMinRunTime(), GetMaxRunTime(), /*GetKeywords(),*/  page,
                                GetSortType(), GetLanguage());

                        }
                        else if (Equals(ShowTypeComboBox.SelectionBoxItem, Application.Current.Resources["TvShowString"].ToString()))
                        {
                            page++;
                            await Service.GetMorePagesDiscoverTvShows(GetSelectedGenres(), GetAfterDateTime(),
                                GetBeforeDateTime(), GetMinVoteAverage(), GetMinVoteCounts(), GetMinRunTime(),
                                GetMaxRunTime(), /*GetKeywords(),*/  page, GetSortTypeTv(), GetLanguage());
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {

                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ShowTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                GenresDisplay.ItemsSource = null;
                selectedGenres.Clear();
                if (Equals(ShowTypeComboBox.SelectedItem, Application.Current.Resources["MovieString"].ToString()))
                {
                    GenresDisplay.ItemsSource = await Service.GetMovieGenres();

                }
                else if (Equals(ShowTypeComboBox.SelectedItem, Application.Current.Resources["TvShowString"].ToString()))
                {
                    GenresDisplay.ItemsSource = await Service.GetTvGenres();
                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void SortComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private DiscoverMovieSortBy GetSortType()
        {
            try
            {
                var selectedSort = SortComboBox.SelectedItem;

                if (Equals(selectedSort, Application.Current.Resources["PopularityDescendingString"].ToString()))
                {
                    return DiscoverMovieSortBy.PopularityDesc;
                }
                else if (Equals(selectedSort, Application.Current.Resources["PopularityAscendingString"].ToString()))
                {
                    return DiscoverMovieSortBy.Popularity;
                }
                else if (Equals(selectedSort, Application.Current.Resources["RatingDescendingString"].ToString()))
                {
                    return DiscoverMovieSortBy.VoteAverageDesc;
                }
                else if (Equals(selectedSort, Application.Current.Resources["RatingAscendingString"].ToString()))
                {
                    return DiscoverMovieSortBy.VoteAverage;
                }
                else if (Equals(selectedSort, Application.Current.Resources["ReleaseDateDescendingString"].ToString()))
                {
                    return DiscoverMovieSortBy.PrimaryReleaseDateDesc;
                }
                else if (Equals(selectedSort, Application.Current.Resources["ReleaseDateAscendingString"].ToString()))
                {
                    return DiscoverMovieSortBy.PrimaryReleaseDate;
                }
                else if (Equals(selectedSort, Application.Current.Resources["TitleAZString"].ToString()))
                {
                    return DiscoverMovieSortBy.OriginalTitleDesc;
                }
                else if (Equals(selectedSort, Application.Current.Resources["TitleZAString"].ToString()))
                {
                    return DiscoverMovieSortBy.OriginalTitle;
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }

            return DiscoverMovieSortBy.Undefined;
        }

        private DiscoverTvShowSortBy GetSortTypeTv()
        {
            try
            {
                var selectedSort = SortComboBox.SelectedItem;

                if (Equals(selectedSort, Application.Current.Resources["PopularityDescendingString"].ToString()))
                {
                    return DiscoverTvShowSortBy.PopularityDesc;
                }
                else if (Equals(selectedSort, Application.Current.Resources["PopularityAscendingString"].ToString()))
                {
                    return DiscoverTvShowSortBy.Popularity;
                }
                else if (Equals(selectedSort, Application.Current.Resources["RatingDescendingString"].ToString()))
                {
                    return DiscoverTvShowSortBy.VoteAverageDesc;
                }
                else if (Equals(selectedSort, Application.Current.Resources["RatingAscendingString"].ToString()))
                {
                    return DiscoverTvShowSortBy.VoteAverage;
                }
                else if (Equals(selectedSort, Application.Current.Resources["ReleaseDateDescendingString"].ToString()))
                {
                    return DiscoverTvShowSortBy.PrimaryReleaseDateDesc;
                }
                else if (Equals(selectedSort, Application.Current.Resources["ReleaseDateAscendingString"].ToString()))
                {
                    return DiscoverTvShowSortBy.PrimaryReleaseDate;
                }
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }

            return DiscoverTvShowSortBy.Undefined;
        }

        private DateTime GetAfterDateTime()
        {
            return ReleaseDateFilterFrom.SelectedDate.HasValue ? ReleaseDateFilterFrom.SelectedDate.Value : DateTime.MinValue;
        }
        private DateTime GetBeforeDateTime()
        {
            return ReleaseDateFilterTo.SelectedDate.HasValue ? ReleaseDateFilterTo.SelectedDate.Value : DateTime.MaxValue;
        }

        private List<int> GetSelectedGenres()
        {
            return selectedGenres;
        }

        private double GetMinVoteAverage()
        {
            return MinVoteAverageSlider.Value;
        }

        private int GetMinVoteCounts()
        {
            return (int)MinUserVotesSlider.Value;
        }

        private int GetMaxRunTime()
        {
            return (int)MaxRuntimeSlider.Value;
        }

        private int GetMinRunTime()
        {
            return (int)MinRuntimeSlider.Value;
        }

        public string GetLanguage()
        {
            if (ComboBoxLanguages.SelectedItem != null)
            {
                var selectedLanguage = ComboBoxLanguages.SelectedItem.ToString();
                return Service.Languages.FirstOrDefault(x => x.EnglishName == selectedLanguage).Iso_639_1;
            }

            return null;
        }

        //private List<TMDbLib.Objects.General.Genre> GetKeywords()
        //{
        //    List<TMDbLib.Objects.General.Genre> result = new List<TMDbLib.Objects.General.Genre>();
        //    if (SelectedKeyword != null)
        //    {
        //        result.Add(SelectedKeyword);
        //    }
        //    return result;
        //}

        private async void SearchButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Equals(ShowTypeComboBox.SelectionBoxItem, Application.Current.Resources["MovieString"].ToString()) )
                {
                    page = 1;
                    MoviesDisplay.ItemsSource = Service.DiscoveredMovies;
                    this.Dispatcher.Invoke(() => MainScroll.ScrollToTop());
                    await Service.DiscoverMovies(GetSelectedGenres(), GetAfterDateTime(), GetBeforeDateTime(),
                        GetMinVoteAverage(), GetMinVoteCounts(), GetMinRunTime(), GetMaxRunTime(), /*GetKeywords(),*/  page,
                        GetSortType(),GetLanguage());

                }
                else if(Equals(ShowTypeComboBox.SelectionBoxItem, Application.Current.Resources["TvShowString"].ToString()))
                {
                    page = 1;
                    MoviesDisplay.ItemsSource = Service.DiscoveredTvShows;
                    this.Dispatcher.Invoke(() => MainScroll.ScrollToTop());
                    await Service.DiscoverTvShows(GetSelectedGenres(), GetAfterDateTime(), GetBeforeDateTime(),
                        GetMinVoteAverage(), GetMinVoteCounts(), GetMinRunTime(), GetMaxRunTime(), /*GetKeywords(),*/  page,
                        GetSortTypeTv(), GetLanguage());


                }
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void MainScroll_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            

           
        }

        //private List<TMDbLib.Objects.General.Genre> SearchedKeywords = new List<TMDbLib.Objects.General.Genre>();

        //private async void KeyWordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        //{
        //    List<string> keywordsList = new List<string>();
        //    SearchedKeywords.Clear();

        //    var query = KeyWordTextBox.Text;

        //    if (String.IsNullOrWhiteSpace(query))
        //    {
        //        SelectedKeyword = null;
        //    }
        //    else
        //    {
        //        var keywords = await Service.client.SearchKeywordAsync(query, 1);
        //        foreach (var keywordsResult in keywords.Results)
        //        {
        //            keywordsList.Add(keywordsResult.Name);
        //            SearchedKeywords.Add(new TMDbLib.Objects.General.Genre()
        //            {
        //                Id = keywordsResult.Id,
        //                Name = keywordsResult.Name
        //            });
        //        }

        //        KeyWordTextBox.ItemsSource = keywordsList;
        //    }

        //}

        //private TMDbLib.Objects.General.Genre SelectedKeyword;
        //private void KeyWordTextBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (KeyWordTextBox.SelectedItem != null)
        //    {
        //        if (SearchedKeywords != null && SearchedKeywords.Count > 0)
        //        {
        //            SelectedKeyword =SearchedKeywords.FirstOrDefault(x => x.Name == KeyWordTextBox.SelectedItem);
        //        }
        //    }
        //}
        public void Dispose()
        {
            Service.DiscoveredMovies.Clear();
            Service.DiscoveredTvShows.Clear();
        }
    }
}
