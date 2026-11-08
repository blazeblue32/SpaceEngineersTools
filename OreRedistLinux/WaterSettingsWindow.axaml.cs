using Avalonia.Interactivity;

namespace SpaceEngineersOreRedistribution
{
    public partial class WaterSettingsWindow : DialogWindow
    {
        public WaterSettingsWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => CloseWithResult(true);
    }
}
