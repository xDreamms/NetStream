using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using subtitle_downloader.downloader;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace NetStream
{
    public class Subtitle : INotifyPropertyChanged
    {
        public int MovieId { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeNumber { get; set; }
        public string Name { get; set; }
        public string Fullpath { get; set; }
        public bool HashDownload { get; set; }
        public string Language { get; set; }
        public bool Synchronized { get; set; }
        public int? SubtitleId { get; set; }
        
        public IImage? Country { get; set; }

        public int? FileId { get; set; }
        public string? FileName { get; set; }

        public string? DownloadCount { get; set; }
        public string? Votes { get; set; }
        public string? Ratings { get; set; }

        public string Name2 { get; set; }
        public DateTime PublishDate { get; set; }
        public bool IsOrg { get; set; }

        public string ImdbId { get; set; }

        public string DownloadUrl { get; set; }
        
        public bool CustomIsUserAdded { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
