using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace NetStream
{
    /// <summary>
    /// Interaction logic for SearchPage.xaml
    /// </summary>
    public partial class SearchPage : Page,IDisposable
    {
        private SearchPageEmpty searchPageEmpty;
        
        public SearchPage()
        {
            InitializeComponent();
            searchPageEmpty = new SearchPageEmpty();
            SearchPageNavigation.Navigate(searchPageEmpty);
        }

        //private void SetPrimaryColor()
        //{
        //    PaletteHelper paletteHelper = new PaletteHelper();
        //    var theme = paletteHelper.GetTheme();
        //    theme.SetPrimaryColor(Color.FromArgb(255, 31, 32, 33));
        //    paletteHelper.SetTheme(theme);
        //}

        //private void SetDefaultPrimaryColor()
        //{
        //    PaletteHelper paletteHelper = new PaletteHelper();
        //    var theme = paletteHelper.GetTheme();
        //    theme.SetPrimaryColor(Color.FromArgb(255, 93, 191, 173));
        //    paletteHelper.SetTheme(theme);
        //}

        public void Dispose()
        {
            //SetDefaultPrimaryColor();
            if (searchPageResults != null)
            {
                searchPageResults.Dispose();
                searchPageResults = null;
            }
            
        }

        private SearchPageResults searchPageResults;
        private void NameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchPageResults != null)
            {
                searchPageResults.SearchKey = SearchTextBox.Text;
            }

            if (!String.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                if (searchPageResults == null)
                {
                    searchPageResults = new SearchPageResults();
                    SearchPageNavigation.Navigate(searchPageResults);
                    searchPageResults.SearchKey = SearchTextBox.Text;
                }
            }
            else
            {
                searchPageResults = null;
                SearchPageNavigation.Navigate(searchPageEmpty);
            }
        }
    }
}
