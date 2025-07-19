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
    public class Cast : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            PropertyChanged(this, new PropertyChangedEventArgs("DisplayMember"));
        }

        private int id;
        public int Id { get; set; }

        private string name;
        public string Name { get; set; }

        private string role;
        public string Role { get; set; }

        private string poster;
        public string Poster { get; set; }

        private string biography;

        public string Biography
        {
            get { return biography; }
            set { biography = value; }
        }

        private string knownFor;

        public string KnownFor
        {
            get { return knownFor; }
            set { knownFor = value; }
        }

        private string birthPlace;

        public string BirthPlace
        {
            get { return birthPlace; }
            set { birthPlace = value; }
        }

        private string birthDate;

        public string BirthDate
        {
            get { return birthDate; }
            set { birthDate = value; }
        }
    }

    public class CastImage
    {
        public string Poster { get; set; }
    }

    public class ActingCredits
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string ReleaseDate { get; set; }
        public DateTime DateTime { get; set; }
        public ShowType ShowType { get; set; }
    }

    public class ProductionCredits
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Job { get; set; }
        public string ReleaseDate { get; set; }
        public DateTime DateTime { get; set; }
        public ShowType ShowType { get; set; }
    }
}
