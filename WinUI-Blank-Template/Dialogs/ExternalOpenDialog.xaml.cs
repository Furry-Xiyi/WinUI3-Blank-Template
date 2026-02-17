using Microsoft.UI.Xaml.Controls;

namespace WinUI3.Dialogs
{
    public sealed partial class ExternalOpenDialog : ContentDialog
    {
        public bool UserConfirmed { get; private set; }

        public ExternalOpenDialog()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            UserConfirmed = false; // Primary = 否
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            UserConfirmed = true; // Secondary = 是
        }
    }
}
