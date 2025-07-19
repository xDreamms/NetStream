using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetStream
{
    public enum ShowType
    {
        Movie,
        TvShow
    }
    //public class NotifyPropertyChangedBase : INotifyPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;

    //    protected void _UpdatePropertyField<T>(
    //        ref T field, T value, [CallerMemberName] string propertyName = null)
    //    {
    //        if (EqualityComparer<T>.Default.Equals(field, value))
    //        {
    //            return;
    //        }

    //        field = value;
    //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //}
    public class Movie 
    {

        private string name;
        public string Name { get; set; }

        private int id;
        public int Id { get; set; }

        private string ratingNumber;

        public string RatingNumber
        {
            get
            {
                return (rating*2).ToString("0.00"); 
            }
        }

        //private BitmapSource _bitmap;
        //public BitmapSource Bitmap
        //{
        //    get; set;
        //}

        private double rating;
        public double Rating
        {
            get
            {
                return rating;
            }
            set
            {
                rating = value;
                rating = rating / 2;
            }
        }

        private ShowType showType;
        public ShowType ShowType { get; set; }

        public string Poster { get; set; }

    }
}
