using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NetStream
{
    public class Season
    {
        public int SeasonNumber { get; set; }
        public string Poster { get; set; }
        public string Year { get; set; }
        public string EpisodeCount { get; set; }
        public string EpisodeCountText { get; set; }
        public string Description { get; set; }
        public FastObservableCollection<Episode> Episodes { get; set; }
        public string SeasonNumberText { get; set; }
    }

    public class Episode
    {
        public string Poster { get; set; }
        public int EpisodeNumber { get; set; }
        public string EpisodeNumberText { get; set; }
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
            }
        }
        private string ratingNumber;

        public string RatingNumber
        {
            get
            {
                return (rating).ToString("0.00");
            }
        }
        public string Date { get; set; }
        private string durationTime;

        public string DurationTime
        {
            get
            {
                return durationTime + App.Current.Resources["MinString"];
            }
            set
            {
                durationTime = value;
            }
        }

        public string Description { get; set; }
        public int SeasonNumber { get; set; }
    }
}
