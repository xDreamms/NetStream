using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace NetStream.Views;

public partial class ImageViewerOverlay : UserControl
{
    private string _imageUrl;
    private string _originalImageUrl;

    public static ImageViewerOverlay Instance { get; private set; }

    public ImageViewerOverlay()
    {
        InitializeComponent();
        Instance = this;
    }

    public async Task ShowImage(string imageUrl)
    {
        _imageUrl = imageUrl;
        // TMDB: w500 -> original for full resolution
        _originalImageUrl = imageUrl?.Replace("/w500/", "/original/");

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            var bytes = await client.GetByteArrayAsync(_originalImageUrl ?? _imageUrl);
            using var stream = new MemoryStream(bytes);
            FullImage.Source = new Bitmap(stream);
            IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading full image: {ex.Message}");
            // Fallback: try w500 version
            try
            {
                using var client = new HttpClient();
                var bytes = await client.GetByteArrayAsync(_imageUrl);
                using var stream = new MemoryStream(bytes);
                FullImage.Source = new Bitmap(stream);
                IsVisible = true;
            }
            catch { }
        }
    }

    private void Close()
    {
        IsVisible = false;
        FullImage.Source = null;
    }

    private void BtnClose_OnPointerPressed(object sender, PointerPressedEventArgs e) => Close();

    private void Background_OnPointerPressed(object sender, PointerPressedEventArgs e) => Close();

    private async void BtnDownload_OnClick(object sender, PointerPressedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Image",
                SuggestedFileName = "image.jpg",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpg" } },
                    new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (file != null)
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var bytes = await client.GetByteArrayAsync(_originalImageUrl ?? _imageUrl);
                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(bytes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving image: {ex.Message}");
        }
    }
}
