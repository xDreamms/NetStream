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
    public class MovieDetail : INotifyPropertyChanged
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

        private string releaseData;
        public string ReleaseDate { get; set; }

        private string director;
        public string Director { get; set; }

        private int directorId;
        public int DirectorID { get; set; }

        private string revenue;
        public string Revenue { get; set; }

        private string status;
        public string Status { get; set; }

        private string production;
        public string Production { get; set; }

        private string runtime;

        public string Runtime { get; set; }

        private string budget;
        public string Budget { get; set; }

        private string genre;
        public string Genre { get; set; }

        private string language;
        public string Language { get; set; }

        private string poster;
        public string Poster
        {
            get; set;
        }

        public ExternalIdsMovie ExternalIdsMovie { get; set; }
    }
}
