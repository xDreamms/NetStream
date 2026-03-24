using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
    public class Movie : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string name;
        public string Name 
        { 
            get => name; 
            set
            {
                if (name != value)
                {
                    name = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private int id;
        public int Id 
        { 
            get => id; 
            set
            {
                if (id != value)
                {
                    id = value;
                    NotifyPropertyChanged();
                }
            }
        }

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
                if (rating != value)
                {
                    rating = value;
                    rating = rating / 2;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(RatingNumber));
                }
            }
        }

        private ShowType showType;
        public ShowType ShowType 
        { 
            get => showType; 
            set
            {
                if (showType != value)
                {
                    showType = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string poster;
        public string Poster 
        { 
            get => poster; 
            set
            {
                if (poster != value)
                {
                    // URL geçerliliğini kontrol et
                    if (!string.IsNullOrEmpty(value))
                    {
                        // URL protokolünü kontrol et (http veya https ile başlamalı)
                        bool isValidUrl = Uri.TryCreate(value, UriKind.Absolute, out Uri uriResult)
                            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                        if (!isValidUrl)
                        {
                            Serilog.Log.Warning($"Geçersiz poster URL'si: {value}");
                            // Geçersiz URL'yi geri çevirmeden önce kontrol et
                            poster = null;
                            NotifyPropertyChanged();
                            return;
                        }
                    }
                    
                    poster = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool isSelected;
        public bool IsSelected 
        { 
            get => isSelected; 
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // ToString() metodu override ediliyor
        public override string ToString()
        {
            return Name ?? $"Film {Id}";
        }
    }
}
