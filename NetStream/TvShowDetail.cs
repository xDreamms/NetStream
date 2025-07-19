using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NetStream.Annotations;
using TMDbLib.Objects.General;

namespace NetStream
{
    public class TvShowDetail :INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            PropertyChanged(this, new PropertyChangedEventArgs("DisplayMember"));
        }


        private string overview;
        public string Overview { get; set; }

        private string director;
        public string Director { get; set; }
        public int DirectorId { get; set; }

        private string status;
        public string Status { get; set; }

        private string production;
        public string Production { get; set; }

        private string genre;
        public string Genre { get; set; }

        private string language;
        public string Language { get; set; }

        private string poster;
        public string Poster
        {
            get; set;
        }

        public ExternalIdsTvShow ExternalIdsTvShow { get; set; }
    }
}
