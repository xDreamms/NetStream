using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Serilog;

namespace NetStream.Views;

public partial class RecommendationsPage : UserControl
{
    public static RecommendationsPage Instance;

    public RecommendationsPage()
    {
        InitializeComponent();
        Instance = this;
    }

    private async void RecommendationsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
            await LoadRecommendations();
            ApplyResponsiveLayout(MainView.Instance.Bounds.Width);
        }
        catch (Exception ex)
        {
            Log.Error($"Error in RecommendationsPage_OnLoaded: {ex.Message}");
        }
    }

    private async Task LoadRecommendations()
    {
        try
        {
            LoadingText.IsVisible = true;
            NoRecommendationsText.IsVisible = false;
            RecommendedMoviesSection.IsVisible = false;
            RecommendedTvShowsSection.IsVisible = false;

            await Service.GetRecommendations();

            LoadingText.IsVisible = false;

            if (Service.RecommendedMovies.Count == 0 && Service.RecommendedTvShows.Count == 0)
            {
                NoRecommendationsText.IsVisible = true;
            }
            else
            {
                if (Service.RecommendedMovies.Count > 0)
                    RecommendedMoviesSection.IsVisible = true;

                if (Service.RecommendedTvShows.Count > 0)
                    RecommendedTvShowsSection.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error loading recommendations: {ex.Message}");
            LoadingText.IsVisible = false;
            NoRecommendationsText.IsVisible = true;
        }
    }

    private void InstanceOnSizeChanged(object sender, MySizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(e.width);
    }

    private void ApplyResponsiveLayout(double width)
    {
        try
        {
            double titleSize = CalculateScaledValue(width, 20, 28);
            double subTitleSize = CalculateScaledValue(width, 12, 16);
            double headerSize = CalculateScaledValue(width, 18, 24);

            PageTitle.FontSize = titleSize;
            SubTitle.FontSize = subTitleSize;
            RecommendedMoviesTitle.FontSize = headerSize;
            RecommendedTvShowsTitle.FontSize = headerSize;
        }
        catch (Exception ex)
        {
            Log.Error($"Error in ApplyResponsiveLayout: {ex.Message}");
        }
    }

    private double CalculateScaledValue(double width, double minValue, double maxValue)
    {
        const double minWidth = 320;
        const double maxWidth = 3840;
        double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
        double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
        return Math.Round(minValue + scale * (maxValue - minValue));
    }

    private void RecommendationsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
    }
}
