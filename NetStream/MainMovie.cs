using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace NetStream
{
    public class MainMovie : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private string name;
        public string Name {
            get { return name; }
            set { SetField(ref name, value, "Name"); }
        }

        private string ratingNumber;
        public string RatingNumber { get { return (rating*2).ToString("0.00"); } set { SetField(ref ratingNumber, value, "RatingNumber"); } }
        
        
        private double rating;
        public double Rating
        {
            get
            {
                return rating;
            }
            set
            {
                SetField(ref rating, value, "Rating");
                rating = rating / 2;
            }
        }

        private string reviewCount;

        public string ReviewCount
        {
            get
            {
                return reviewCount;
            }
            set
            {
                SetField(ref reviewCount, value, "ReviewCount");
            }
        }


        private string releaseYear;

        public string ReleaseYear
        {
            get
            {
                return releaseYear;
            }
            set
            {
                SetField(ref releaseYear, value, "ReleaseYear");
            }
        }

        private string duration;

        public string Duration
        {
            get
            {
                return duration;
            }
            set
            {
                SetField(ref duration, value, "Duration");
            }
        }


        private string overview;

        public string Overview
        {
            get
            {
                return overview;
            }
            set
            {
                SetField(ref overview, value, "Overview");
            }
        }

        private string trailerLink;

        public string TrailerLink
        {
            get
            {
                return trailerLink;
            }
            set
            {
                SetField(ref trailerLink, value, "TrailerLink");
            }
        }

        private bool isFavorite;

        public bool IsFavorite
        {
            get
            {
                return isFavorite;
            }
            set
            {
                SetField(ref isFavorite, value, "IsFavorite");
            }
        }

        private bool isInWatchlist;

        public bool IsInWatchlist
        {
            get
            {
                return isInWatchlist;
            }
            set
            {
                SetField(ref isInWatchlist, value, "IsInWatchlist");
            }
        }

        //private BitmapSource _bitmap;
        //public BitmapSource Bitmap
        //{
        //    get { return _bitmap; }
        //    set { SetField(ref _bitmap, value, "Bitmap"); }
        //}

        private string poster;
        public string Poster
        {
            get { return poster; }
            set { SetField(ref poster, value, "Poster"); }
        }

        private int id;
        public int Id {
            get { return id; }

            set {
                    SetField(ref id, value, "Id");
                }
            }

        private ShowType showType;
        public ShowType ShowType
        {
            get { return showType; }

            set
            {
                SetField(ref showType, value, "ShowType");
            }
        }

        private double? myRating;
        public double? MyRating
        {
            get { return myRating; }

            set
            {
                SetField(ref myRating, value, "MyRating");
            }
        }
    }
}
