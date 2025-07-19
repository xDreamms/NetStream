using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NetStream.Annotations;

namespace NetStream
{
    public class VideoDetail : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            PropertyChanged(this, new PropertyChangedEventArgs("DisplayMember"));
        }

        private string name;
        public string Name { get; set; }


        private string videoType;
        public string VideoType { get; set; }

        private string poster;
        public string Poster
        {
            get; set;
        }
        private string videoLink;
        public string VideoLink { get; set; }
    }
}
