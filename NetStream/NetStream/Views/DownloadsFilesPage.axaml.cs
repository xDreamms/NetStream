using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NetStream.Views;
using Serilog;
using Path = System.IO.Path;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for DownloadsFilesPage.xaml
    /// </summary>

    public class EpisodeFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name { get; set; }
        public string Poster { get; set; }
        public int EpisodeNumber { get; set; }
        public string FilePath { get; set; }
        public int SeasonNumber { get; set; }
        public int FileIndex { get; set; }

        private bool _isCompleted;
        public bool IsCompleted 
        { 
            get => _isCompleted; 
            set 
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _progress;
        public double Progress 
        { 
            get => _progress; 
            set 
            {
                if (Math.Abs(_progress - value) > 0.01)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }
    }
    
    public partial class DownloadsFilesPage : UserControl
    {
        private ShowType showType;
        private int selectedMovieId;
        private string movieName;
        private int imdbId;
        private Item selectedTorrent;
        private DispatcherTimer _progressTimer;

        public DownloadsFilesPage()
        {
            InitializeComponent();
        }
        
        public DownloadsFilesPage(int selectedMovieId, string movieName, ShowType showType, int imdbId,
            Item selectedTorrent)
        {
            InitializeComponent();
            this.selectedMovieId = selectedMovieId;
            this.movieName = movieName;
            this.showType = showType;
            this.imdbId = imdbId;
            this.selectedTorrent = selectedTorrent;
            
            _progressTimer = new DispatcherTimer(DispatcherPriority.Background);
            _progressTimer.Interval = TimeSpan.FromSeconds(1);
            _progressTimer.Tick += ProgressTimer_Tick;
            
            Load();
        }

        private async void Load()
        {
            try
            {
                var files = await Libtorrent.GetFiles(selectedTorrent.Hash);
            
                if (showType == ShowType.TvShow)
                {
                    FilesDisplay.ItemsSource = episodeFiles;
                    foreach (var torrentManagerFile in files.Where(x=> x.IsMediaFile 
                                                                       && GetSeasonNumberFromFileName(Path.GetFileNameWithoutExtension(x.Name)) != null
                                                                       && GetEpisodeNumberFromFileName(Path.GetFileNameWithoutExtension(x.Name))!=null).OrderBy(x=> GetSeasonNumberFromFileName(Path.GetFileNameWithoutExtension(x.Name))).ThenBy(x=> GetEpisodeNumberFromFileName(Path.GetFileNameWithoutExtension(x.Name))))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(torrentManagerFile.Name);
                        var episodeNumber = GetEpisodeNumberFromFileName(fileName);
                        if (episodeNumber != null)
                        {
                            var currentSeasonNumber = GetSeasonNumberFromFileName(fileName);
                            if (currentSeasonNumber != null)
                            {
                                var episode = await Service.client.GetTvEpisodeAsync(selectedMovieId, currentSeasonNumber.Value, episodeNumber.Value);
                                if (episode != null)
                                {
                                    EpisodeFile episodeFile = new EpisodeFile()
                                    {
                                        Name = movieName + " S" + currentSeasonNumber.Value + "E" + episodeNumber.Value,
                                        Poster = (Service.client.GetImageUrl("w500", episode.StillPath).AbsoluteUri),
                                        EpisodeNumber = episodeNumber.Value,
                                        FilePath = torrentManagerFile.FullPath,
                                        SeasonNumber = currentSeasonNumber.Value,
                                        FileIndex = torrentManagerFile.Index
                                    };

                                    if (selectedTorrent.IsCompleted)
                                    {
                                        episodeFile.IsCompleted = true;
                                    }
                                    else
                                    {
                                        var content = await Libtorrent.GetFiles(selectedTorrent.Hash);
                                        if (content != null)
                                        {
                                            foreach (var torrentContent in content)
                                            {
                                                if (torrentContent.Index == episodeFile.FileIndex)
                                                {
                                                    episodeFile.IsCompleted = torrentContent.Progress >= 1;
                                                    episodeFile.Progress = torrentContent.Progress * 100;
                                                }
                                            }
                                        }
                                    }

                                    episodeFiles.Add(episodeFile);
                                }
                            }
                       
                        }
                    }
                }
                else
                {
                    FilesDisplay.ItemsSource = episodeFiles;
                    foreach (var torrentManagerFile in files.Where(x => x.IsMediaFile)
                                 .OrderBy(x => ExtractNumber(Path.GetFileNameWithoutExtension(x.Name))))
                    {
                        EpisodeFile episodeFile = new EpisodeFile()
                        {
                            Name = Path.GetFileNameWithoutExtension(torrentManagerFile.Name),
                            Poster = selectedTorrent.Poster,
                            EpisodeNumber = 0,
                            FilePath = torrentManagerFile.FullPath,
                            SeasonNumber = 0,
                            FileIndex = torrentManagerFile.Index
                        };

                        if (selectedTorrent.IsCompleted)
                        {
                            episodeFile.IsCompleted = true;
                        }
                        else
                        {
                            var content = await Libtorrent.GetFiles(selectedTorrent.Hash);
                            if (content != null)
                            {
                                foreach (var torrentContent in content)
                                {
                                    if (torrentContent.Index == episodeFile.FileIndex)
                                    {
                                        episodeFile.IsCompleted = torrentContent.Progress >= 1;
                                        episodeFile.Progress = torrentContent.Progress * 100;
                                    }
                                }
                            }
                        }

                        episodeFiles.Add(episodeFile);
                    }
                }

                OnLoadedCompleted?.Invoke();
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void ProgressTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (selectedTorrent == null || episodeFiles == null) return;

                var files = await Libtorrent.GetFiles(selectedTorrent.Hash);
                if (files == null) return;

                foreach (var episodeFile in episodeFiles)
                {
                    var file = files.FirstOrDefault(x => x.Index == episodeFile.FileIndex);
                    if (file != null)
                    {
                        episodeFile.Progress = file.Progress * 100;
                        episodeFile.IsCompleted = file.Progress >= 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating progress: {ex.Message}");
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _progressTimer?.Start();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _progressTimer?.Stop();
        }


        public static List<string> Regexes
        {
            get
            {
                List<string> regexes = new List<string>();
                regexes.Add("[sS][0-9]+[eE][0-9]+-*[eE]*[0-9]*");
                regexes.Add("[0-9]+[xX][0-9]+");

                return regexes;
            }
        }

        public static int? GetSeasonNumberFromFileName(string fileName)
        {
            try
            {
                foreach (string regex in Regexes)
                {
                    Match match = Regex.Match(fileName, regex);
                    if (match.Success)
                    {
                        string matched = match.Value.ToLower();
                        if (regex.Contains("e")) //SDDEDD
                        {
                            matched = matched.Replace("s", "");
                            matched = matched.Substring(0, matched.IndexOf("e"));
                            return int.Parse(matched);
                        }
                        else if (regex.Contains("x")) //DDXDD
                        {
                            matched = matched.Substring(0, matched.IndexOf("x"));
                            return int.Parse(matched);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }

            return null;
        }
        
        public static int? GetEpisodeNumberFromFileName(string fileName)
        {
            try
            {
                foreach (string regex in Regexes)
                {
                    Match match = Regex.Match(fileName, regex);
                    if (match.Success)
                    {
                        string matched = match.Value.ToLower();
                        if (regex.Contains("e")) //SDDEDD
                        {
                            matched = matched.Substring(matched.IndexOf("e") + 1);

                            if (matched.Contains("e") || matched.Contains("-"))
                            {
                                matched = matched.Substring(0, matched.IndexOf(matched.Contains("e") ? "e" : "-")).Replace("-", "");
                            }

                            return int.Parse(matched);
                        }
                        else if (regex.Contains("x")) //DDXDD
                        {
                            matched = matched.Substring(matched.IndexOf("x") + 1);
                            return int.Parse(matched);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }

            return null;
        }

        public event Action OnLoadedCompleted;

        public ObservableCollection<EpisodeFile> episodeFiles = new ObservableCollection<EpisodeFile>();
        
        private async void DownloadsFilesPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        public int ExtractNumber(string input)
        {
            try
            {
                string pattern = @"\d+";  
                Match match = Regex.Match(input, pattern);

                if (match.Success)
                {
                    int number = int.Parse(match.Value); 
                    return number;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
                return -1;
            }
        }

        List<string> openedFiles = new List<string>();
        
  
        private void DownloadsFilesPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Clean up code if needed
        }

        private async void GridFiles_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var grid = sender as Grid;
                if(grid == null) return;
                var selectedEpisode = grid.DataContext as EpisodeFile;
                if (selectedEpisode != null)
                {
                    if (!(openedFiles.Any(x => x == selectedEpisode.FilePath)))
                    {
                        var playerWindow = new PlayerWindow(selectedMovieId, movieName, showType, selectedEpisode.SeasonNumber, selectedEpisode.EpisodeNumber,
                            new FileInfo(selectedEpisode.FilePath), selectedEpisode.IsCompleted, showType == ShowType.TvShow ? imdbId:-1,selectedTorrent,selectedEpisode.FileIndex,selectedEpisode.Poster,this);
                        
                        MainWindow.Instance.SetContent(playerWindow);

                        if (selectedTorrent.ShowType == ShowType.TvShow)
                        {
                            await Libtorrent.ChangeEpisodeFileToMaximalPriority(selectedTorrent.Hash, selectedEpisode.SeasonNumber,
                                selectedEpisode.EpisodeNumber);
                            openedFiles.Add(selectedEpisode.FilePath);

                            playerWindow.Unloaded += (o, args) =>
                            {
                                var itemToRemove = openedFiles.FirstOrDefault(x => x == selectedEpisode.FilePath);
                                openedFiles.Remove(itemToRemove);
                                Libtorrent.currentEpisodeFile = null;
                            };
                        }
                        else
                        {
                            if(!selectedEpisode.IsCompleted)
                                await Libtorrent.ChangeMovieCollectionFilePriorityToMaximal(selectedTorrent, selectedEpisode.FileIndex);

                            openedFiles.Add(selectedEpisode.FilePath);

                            playerWindow.Unloaded += (o, args) =>
                            {
                                var itemToRemove = openedFiles.FirstOrDefault(x => x == selectedEpisode.FilePath);
                                openedFiles.Remove(itemToRemove);
                            };
                        }
                    }
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