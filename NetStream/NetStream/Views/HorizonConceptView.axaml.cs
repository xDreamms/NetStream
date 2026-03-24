using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NetStream.Views
{
    public partial class HorizonConceptView : UserControl
    {
        public HorizonConceptView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
