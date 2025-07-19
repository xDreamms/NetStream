using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Path = System.IO.Path;

namespace NetStream
{
    /// <summary>
    /// Interaction logic for DownloadsFilesPage.xaml
    /// </summary>

    public class EpisodeFile
    {
        public string Name { get; set; }
        public string Poster { get; set; }
        public int EpisodeNumber { get; set; }
        public string FilePath { get; set; }
        public int SeasonNumber { get; set; }
        public int FileIndex { get; set; }
        public bool IsCompleted { get; set; }
    }
    public partial class DownloadsFilesPage : Page
    {
        //private TorrentManager torrentManager;
        private ShowType showType;
        private int selectedMovieId;
        private string movieName;
        private int imdbId;
        private Item selectedTorrent;
        //public DownloadsFilesPage(int selectedMovieId,string movieName,TorrentManager torrentManager,ShowType showType,int seasonNumber,int imdbId)
        //{
        //    InitializeComponent();
        //    this.torrentManager = torrentManager;
        //    this.showType = showType;
        //    this.seasonNumber = seasonNumber;
        //    this.selectedMovieId = selectedMovieId;
        //    this.movieName = movieName;
        //    this.imdbId = imdbId;
        //    HomePage.GetHomePageInstance.MyEvent += GetHomePageInstanceOnMyEvent;
        //}

        public DownloadsFilesPage(int selectedMovieId, string movieName, ShowType showType, int imdbId,
            Item selectedTorrent)
        {
            InitializeComponent();
            this.selectedMovieId = selectedMovieId;
            this.movieName = movieName;
            this.showType = showType;
            this.imdbId = imdbId;
            this.selectedTorrent = selectedTorrent;
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
            catch (System.Exception e)
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
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }

            return null;
        }

        public event Action OnLoadedCompleted;

        public FastObservableCollection<EpisodeFile> episodeFiles = new FastObservableCollection<EpisodeFile>();
        private async void DownloadsFilesPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var files = await Libtorrent.GetFiles(selectedTorrent);
            
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
                                    }
                                }
                            }
                        }

                        episodeFiles.Add(episodeFile);
                    }
                }

                OnLoadedCompleted?.Invoke();
            }
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }

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
            catch (System.Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
                return -1;
            }
        }

        private void GetHomePageInstanceOnMyEvent(object? sender, EventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        private void FilesDisplay_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            
        }
        List<string> openedFiles = new List<string>();
        public async void FilesDisplay_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedEpisode = FilesDisplay.SelectedItem as EpisodeFile;
                if (selectedEpisode != null)
                {
                    if (!(openedFiles.Any(x => x == selectedEpisode.FilePath)))
                    {
                        var playerWindow = new PlayerWindow(selectedMovieId, movieName, showType, selectedEpisode.SeasonNumber, selectedEpisode.EpisodeNumber,
                            new FileInfo(selectedEpisode.FilePath), selectedEpisode.IsCompleted, showType == ShowType.TvShow ? imdbId:-1,selectedTorrent,selectedEpisode.FileIndex,selectedEpisode.Poster,this);
                        playerWindow.Show();

                        if (selectedTorrent.ShowType == ShowType.TvShow)
                        {
                            await Libtorrent.ChangeEpisodeFileToMaximalPriority(selectedTorrent, selectedEpisode.SeasonNumber,
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
            catch (System.Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }


        }

        private void FilesDisplay_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }

        private void DownloadsFilesPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
