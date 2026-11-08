using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SpaceEngineersOreRedistribution
{
    public partial class TextInputPopup : DialogWindow
    {
        public TextInputPopup()
        {
            InitializeComponent();
        }

        public string Text => MyText.Text;

        private void Button_Click(object sender, RoutedEventArgs e) => CloseWithResult(true);
    }
}
