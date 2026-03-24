using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Material.Icons.Avalonia;
using Serilog;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Projektanker.Icons.Avalonia;

namespace NetStream.Views
{
    public partial class MovieDetailsOverViewPage : UserControl
    {
        private Movie selectedMovie;
        private MovieDetail movieDetail;
        private double previousWidth = 0;
        
        private const double SMALL_SCREEN_THRESHOLD = 750;
        private const double EXTRA_SMALL_SCREEN_THRESHOLD = 450;

        public MovieDetailsOverViewPage()
        {
            InitializeComponent();
        }
        
        public MovieDetailsOverViewPage(Movie selectedMovie)
        {
            InitializeComponent();
            this.selectedMovie = selectedMovie;
        }

        private async void MovieDetailsOverViewPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged += InstanceOnSizeChanged;
            movieDetail = await Service.GetMovieDetails(selectedMovie);
            MovieDetailArea.DataContext = movieDetail;
            SetVisibilityIcons();
            SetVisibilityInfoStrings();
            
            // İlk yükleme sırasında da boyuta göre ölçeklendirme yap
            ApplyResponsiveLayout(this.Bounds.Width);
        }

        private void InstanceOnSizeChanged(object? sender, MySizeChangedEventArgs e)
        {
            try
            {
                // Get current width
                double width = e.width;
                Console.WriteLine(width);
                if (width != previousWidth)
                {
                    previousWidth = width;
                    ApplyResponsiveLayout(width);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in MovieDetailsOverViewPage_SizeChanged: {ex.Message}");
            }
        }


        private void ApplyResponsiveLayout(double width)
        {
            try
            {
                // Ekran genişliğini maksimum 3840px ile sınırla
                double clampedWidth = Math.Min(width, 3840);
                
                bool isSmallScreen = clampedWidth <= SMALL_SCREEN_THRESHOLD;
                bool isExtraSmallScreen = clampedWidth <= EXTRA_SMALL_SCREEN_THRESHOLD;
                
               
                    // Ekran çok küçükse dikey yerleşim, değilse yatay yerleşim
                    MovieDetailArea.Orientation = isExtraSmallScreen ? Orientation.Vertical : Orientation.Horizontal;
                    StackPanelMovieDetails.Orientation = isExtraSmallScreen || width <= 1260 ? Orientation.Vertical : Orientation.Horizontal;
                   
                    // Marjin değerlerini ekran boyutuna göre ayarla
                    double margin = CalculateScaledValue(clampedWidth, 10, 50);
                    MovieDetailArea.Margin = isExtraSmallScreen 
                        ? new Thickness(margin, 50, margin, 30) 
                        : new Thickness(0, 50, 0, 30);
                    
                    // Marjin değerlerini ekran boyutuna göre ayarla
                   
                   

                    StackPanelOverview.Width = !isExtraSmallScreen ? clampedWidth / 1.9 : clampedWidth / 1.2;
                // Poster boyutunu ayarla
                var posterBorder = this.FindControl<Border>("PosterBorder");
                if (posterBorder != null)
                {
                    double posterWidth = CalculateScaledValue(clampedWidth, 120, 400);
                    if (isExtraSmallScreen)
                    {
                        posterWidth = 200;
                    }

                    double posterHeight = posterWidth * 1.5; // Aspect ratio koruyarak yükseklik hesapla
                    
                    posterBorder.Width = posterWidth;
                    posterBorder.Height = posterHeight;
                    
                    // Poster için marjin ayarla
                    if (isExtraSmallScreen)
                    {
                        posterBorder.Margin = new Thickness(0, 0, 0, 20);
                        posterBorder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                    }
                    else
                    {
                        posterBorder.Margin = new Thickness(0, 9, 0, 0);
                        posterBorder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                    }
                }
                
                // Bilgi kısmındaki üst StackPanel için düzenleme
                var infoPanel = this.FindControl<StackPanel>("InfoPanel");
                if (infoPanel != null)
                {
                    // Marjin değerlerini ekran boyutuna göre ayarla
                    double leftMargin = CalculateScaledValue(clampedWidth, 10, 35);
                    infoPanel.Margin = isExtraSmallScreen 
                        ? new Thickness(0, 20, 0, 0) 
                        : new Thickness(leftMargin, 35, 0, 0);
                }
                
                // Storyline başlığı için font boyutu
                var storyLineText = this.FindControl<TextBlock>("StoryLineString");
                if (storyLineText != null)
                {
                    storyLineText.FontSize = CalculateScaledValue(clampedWidth, 22, 35);
                }
                
                // Özet metni için font boyutu

                
                OverviewString.FontSize = CalculateScaledValue(clampedWidth, 14, 25);
                
                
                // Detayların olduğu bölümün düzeni için
                var detailsMainPanel = this.FindControl<StackPanel>("DetailsMainPanel");
                if (detailsMainPanel != null)
                {
                    var detailsRow = detailsMainPanel.Children[0] as StackPanel;
                    if (detailsRow != null)
                    {
                        if (isExtraSmallScreen ||width <= 1260)
                        {
                            foreach (var child in detailsRow.Children)
                            {
                                if (child is StackPanel columnPanel)
                                {
                                    columnPanel.Margin = new Thickness(0, 0, 15, 15);
                                }
                            }
                            StackPanel2details.Margin = new Thickness(0, 0, 30, 0);
                        }
                        else
                        {
                            double wideSpacing = CalculateScaledValue(clampedWidth, 50, 450);
                            StackPanel2details.Margin = new Thickness(wideSpacing, 0, 15, 0);
                        }
                    }
                }
                
                // Tüm etiket ve değer TextBlock'ları için font boyutu ayarı
                double labelFontSize = CalculateScaledValue(clampedWidth, 12, 15);
                AdjustTextBlockFontSizes(this, labelFontSize);
                
                // Sosyal medya ikonları için boyut ve aralık ayarı
                double iconSize = CalculateScaledValue(clampedWidth, 18, 30);
                double iconSpacing = CalculateScaledValue(clampedWidth, 8, 15);
                
                var socialIcons = new[] {
                    BtnFacebook,
                    BtnInstagram,
                    BtnImdb,
                    BtnTwitter
                };
                
                foreach (var icon in socialIcons)
                {
                    if (icon != null)
                    {
                        icon.FontSize = iconSize;
                        icon.Margin = new Thickness(0, 0, iconSpacing, 0);
                    }
                }
                
                // Sosyal medya ikonları panel ayarı
                var socialPanel = this.FindControl<StackPanel>("SocialPanel");
                if (socialPanel != null)
                {
                    double topMargin = CalculateScaledValue(clampedWidth, 20, 40);
                    socialPanel.Margin = new Thickness(0, topMargin, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ApplyResponsiveLayout: {ex.Message}");
            }
        }
        
        private void AdjustTextBlockFontSizes(Control parent, double fontSize)
        {
            // Tüm alt kontrollerinde TextBlock ara
            foreach (var child in parent.GetVisualChildren())
            {
                if (child is TextBlock textBlock && 
                    textBlock.Name != "StoryLineString" && 
                    textBlock.Name != "OverviewString")
                {
                    textBlock.FontSize = fontSize;
                }
                
                if (child is Control control)
                {
                    AdjustTextBlockFontSizes(control, fontSize);
                }
            }
        }
        
        private double CalculateScaledValue(double width, double minValue, double maxValue)
        {
            // Ekran genişliği sınırları
            const double minWidth = 320;    // En küçük ekran genişliği (piksel)
            const double maxWidth = 3840;   // En büyük ekran genişliği (piksel)
            
            // Ekran genişliğini sınırla
            double clampedWidth = Math.Max(minWidth, Math.Min(width, maxWidth));
            
            // Doğrusal ölçeklendirme formülü
            double scale = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double scaledValue = minValue + scale * (maxValue - minValue);
            
            // Değeri yuvarla
            return Math.Round(scaledValue);
        }
        
         private void SetVisibilityInfoStrings()
        {
            try
            {
                if (movieDetail != null)
                {
                    if (String.IsNullOrWhiteSpace(movieDetail.Overview))
                    {
                        StoryLineString.IsVisible = false;
                        OverviewString.IsVisible = false;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.ReleaseDate))
                    {
                        ReleasedString.IsVisible = false;
                        ReleaseDateText.IsVisible = false;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Director))
                    {
                        DirectorString.IsVisible = false;
                        DirectorText.IsVisible = false;
                    }
                    
                    if (String.IsNullOrWhiteSpace(movieDetail.Status))
                    {
                        StatusString.IsVisible = false;
                        StatusText.IsVisible = false;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Production))
                    {
                        ProductionString.IsVisible = false;
                        ProductionText.IsVisible = false;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Runtime))
                    {
                        RuntimeString.IsVisible = false;
                        RuntimeText.IsVisible = false;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Genre))
                    {
                        GenreString.IsVisible = false;
                        GenreText.IsVisible = false;
                    }

                    if (String.IsNullOrWhiteSpace(movieDetail.Language))
                    {
                        LanguageString.IsVisible = false;
                        LanguageText.IsVisible = false;
                    }

                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void SetVisibilityIcons()
        {
            try
            {
                if (movieDetail != null)
                {
                    if (movieDetail.ExternalIdsMovie == null || String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.TwitterId))
                    {
                        BtnTwitter.IsVisible = false;
                    }
                    if (movieDetail.ExternalIdsMovie == null || String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.FacebookId))
                    {
                        BtnFacebook.IsVisible = false;
                    }
                    if (movieDetail.ExternalIdsMovie == null || String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.InstagramId))
                    {
                        BtnInstagram.IsVisible = false;
                    }
                    if (movieDetail.ExternalIdsMovie == null || String.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.ImdbId))
                    {
                        BtnImdb.IsVisible = false;
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        #region Director Text Events
        private void DirectorText_OnPointerEnter(object sender, PointerEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                textBlock.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("White"));
            }
        }

        private void DirectorText_OnPointerLeave(object sender, PointerEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                textBlock.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#C5C5C5"));
            }
        }

        private void DirectorText_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Handle director click event
            // Example: AppService.Current.NavigateToDirector(DirectorText.Text);
        }
        #endregion

        #region Social Media Button Events
        private void BtnFacebook_OnPointerEnter(object sender, PointerEventArgs e)
        {
            BtnFacebook.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
        }

        
        
        private void BtnFacebook_OnPointerLeave(object sender, PointerEventArgs e)
        {
            BtnFacebook.Foreground = new SolidColorBrush(Color.FromArgb(255,255,255,255));
        }

        private void BtnFacebook_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (movieDetail?.ExternalIdsMovie != null && !string.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.FacebookId))
                {
                    string url = "https://facebook.com/" + movieDetail.ExternalIdsMovie.FacebookId;
                    OpenBrowser(url);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnInstagram_OnPointerEnter(object sender, PointerEventArgs e)
        {
            BtnInstagram.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
        }

        private void BtnInstagram_OnPointerLeave(object sender, PointerEventArgs e)
        {
            BtnInstagram.Foreground = new SolidColorBrush(Color.FromArgb(255,255,255,255));
        }

        private void BtnInstagram_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (movieDetail?.ExternalIdsMovie != null && !string.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.InstagramId))
                {
                    string url = "https://instagram.com/" + movieDetail.ExternalIdsMovie.InstagramId;
                    OpenBrowser(url);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnImdb_OnPointerEnter(object sender, PointerEventArgs e)
        {
            BtnImdb.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
        }

        private void BtnImdb_OnPointerLeave(object sender, PointerEventArgs e)
        {
            BtnImdb.Foreground = new SolidColorBrush(Color.FromArgb(255,255,255,255));
        }

        private void BtnImdb_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (movieDetail?.ExternalIdsMovie != null && !string.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.ImdbId))
                {
                    string url = "https://www.imdb.com/title/" + movieDetail.ExternalIdsMovie.ImdbId;
                    OpenBrowser(url);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private void BtnTwitter_OnPointerEnter(object sender, PointerEventArgs e)
        {
            BtnTwitter.Foreground = new SolidColorBrush((Color)App.Current.Resources["ColorDefault"]);
        }

        private void BtnTwitter_OnPointerLeave(object sender, PointerEventArgs e)
        {
            BtnTwitter.Foreground = new SolidColorBrush(Color.FromArgb(255,255,255,255));
        }

        private void BtnTwitter_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                if (movieDetail?.ExternalIdsMovie != null && !string.IsNullOrWhiteSpace(movieDetail.ExternalIdsMovie.TwitterId))
                {
                    string url = "https://x.com/" + movieDetail.ExternalIdsMovie.TwitterId;
                    OpenBrowser(url);
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        // Sadece desteklenen platformlarda URL açan basit metod
        private void OpenBrowser(string url)
        {
            try
            {
                // Windows platformu
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    return;
                }
                
                // Linux platformu
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                    return;
                }
                
                // macOS platformu
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                    return;
                }

                // Desteklenmeyen platform - sadece log kaydı tut
                Log.Information($"Platform URL açmayı desteklemiyor: {RuntimeInformation.OSDescription}");
            }
            catch (Exception ex)
            {
                Log.Error($"URL açılırken hata oluştu: {ex.Message}");
                
                try 
                {
                    // Alternatif yöntem - sadece HTML dosya ile açma denemesi
                    var tempPath = Path.GetTempPath() + "tempHtmlOpener_" + Guid.NewGuid().ToString() + ".html";
                    using (var writer = new StreamWriter(tempPath))
                    {
                        writer.WriteLine($"<html><head><meta http-equiv=\"refresh\" content=\"0;url={url}\"></head></html>");
                    }
                    
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true
                    });
                    
                    // Dosyayı silmek için biraz bekle ve ardından sil
                    Task.Delay(5000).ContinueWith(_ => 
                    {
                        try { File.Delete(tempPath); } 
                        catch { /* Dosya silinemezse yoksay */ }
                    });
                }
                catch (Exception innerEx)
                {
                    Log.Error($"Alternatif yöntem hata: {innerEx.Message}");
                }
            }
        }
        
        #endregion

       

        private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
        {
            MainView.Instance.SizeChanged -= InstanceOnSizeChanged;
        }
    }
} 