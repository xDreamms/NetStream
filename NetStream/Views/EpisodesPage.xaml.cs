using MovieCollection.OpenSubtitles.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Serilog;
using TMDbLib.Objects.Search;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for EpisodesPage.xaml
    /// </summary>
    public partial class EpisodesPage : Page
    {
        private Movie selectedTvShow;
        private Frame frame;
        public EpisodesPage(Movie selectedTvShow, Frame frame)
        {
            InitializeComponent();
            this.selectedTvShow = selectedTvShow;
            this.frame = frame;
        }

        private async void SeasonsDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var Season = SeasonsDisplay.SelectedItem as Season;
            if (Season != null)
            {
                await Service.GetSeasonEpisodes(selectedTvShow.Id, Season.SeasonNumber);

            }
        }

        private void SeasonsDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }

        private void EpisodesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiUrl) ||
                    String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiKey))
                {
                    return;
                }

                var selectedEpisode = EpisodesDisplay.SelectedItem as Episode;
                if (selectedEpisode != null)
                {
                    var torrentsPage = new TorrentsPage(selectedTvShow,selectedEpisode.SeasonNumber,selectedEpisode.EpisodeNumber);
                    frame.Navigate(torrentsPage);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void EpisodesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }
        
        private async void EpisodesPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Service.TvShowSeasons.Clear();
                Service.TvSeasonEpisodes.Clear();
                SeasonsDisplay.ItemsSource = Service.TvShowSeasons;
                EpisodesDisplay.ItemsSource = Service.TvSeasonEpisodes;

                await Service.GetTvShowSeasons(selectedTvShow.Id);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        public readonly string[] SizeSuffixes =
            { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

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

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        private void UIElement_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var Button =sender as RepeatButton;
                if (Button.DataContext is Season)
                {
                    Season s = (Season)Button.DataContext;
                    var torrentPage = new TorrentsPage(selectedTvShow, s.SeasonNumber);
                    frame.Navigate(torrentPage);
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
                var Button = sender as RepeatButton;
                if (String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiUrl) ||
                    String.IsNullOrWhiteSpace(AppSettingsManager.appSettings.JacketApiKey))
                {
                    Button.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
    }
}
