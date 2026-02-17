using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI3.Pages
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
        }

        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.OpenExternalLink(sender, e);
        }
    }
}
