using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using WinUI3.Dialogs;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;

namespace WinUI3.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private bool _isInitializing = true;

        public SettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUI();
            LoadAppInfo();
            _isInitializing = false;

            // 监听主题变化 → 统一调 AppThemeManager.OnActualThemeChanged（刷标题栏颜色）
            if (App.MainWindow?.Content is FrameworkElement root)
            {
                root.ActualThemeChanged -= AppThemeManager.OnActualThemeChanged;
                root.ActualThemeChanged += AppThemeManager.OnActualThemeChanged;
            }
        }

        private void LoadUI()
        {
            // ✅ 全部用 as string + SelectedIndex，AOT 安全
            string theme = localSettings.Values["AppTheme"] as string ?? "System";
            RbTheme.SelectedIndex = theme switch
            {
                "Light" => 1,
                "Dark" => 2,
                _ => 0
            };

            // 索引 0=Mica  1=MicaAlt  2=Acrylic
            string material = localSettings.Values["AppMaterial"] as string ?? "MicaAlt";
            RbMaterial.SelectedIndex = material switch
            {
                "MicaAlt" => 1,
                "Acrylic" => 2,
                _ => 0
            };

            string pos = localSettings.Values["PanePosition"] as string ?? "Left";
            PanePositionCombo.SelectedIndex = pos == "Top" ? 1 : 0;

            bool sound = localSettings.Values["EnableSound"] is bool b ? b : true;
            if (localSettings.Values["EnableSound"] == null)
                localSettings.Values["EnableSound"] = true;
            SoundToggle.IsOn = sound;
        }

        public void LoadAppInfo()
        {
            try
            {
                TxtAppName.Text = Package.Current.DisplayName;
                var v = Package.Current.Id.Version;
                TxtVersion.Text = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
                ImgAppIcon.Source = new BitmapImage(Package.Current.Logo);
                TxtCopyright.Text = $"©{DateTime.Now.Year} {Package.Current.PublisherDisplayName}。保留所有权利。";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadAppInfo 错误: {ex.Message}");
            }
        }

        public static string GetAppDisplayName() => Package.Current.DisplayName;
        public static BitmapImage GetAppLogo() => new BitmapImage(Package.Current.Logo);

        private async void OpenExternalLink(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton btn && btn.Tag is string url)
            {
                var dialog = new ExternalOpenDialog
                {
                    XamlRoot = this.XamlRoot  // WinUI 3 必须设置 XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                    await Launcher.LaunchUriAsync(new Uri(url));
            }
        }

        private void RbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            // ✅ 用 SelectedIndex，AOT 安全，不做任何类型转换
            string value = RbTheme.SelectedIndex switch
            {
                1 => "Light",
                2 => "Dark",
                _ => "System"
            };

            var theme = RbTheme.SelectedIndex switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            localSettings.Values["AppTheme"] = value;
            AppThemeManager.CurrentTheme = theme; // 同步静态状态

            if (App.MainWindow?.Content is FrameworkElement rootElement)
                rootElement.RequestedTheme = theme;

            AppThemeManager.UpdateTitleBarColors();
        }

        private void RbMaterial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            // 索引 0=Mica  1=MicaAlt  2=Acrylic
            string value = RbMaterial.SelectedIndex switch
            {
                1 => "MicaAlt",
                2 => "Acrylic",
                _ => "Mica"
            };

            localSettings.Values["AppMaterial"] = value;

            AppThemeManager.CurrentMaterial = value switch
            {
                "MicaAlt" => BackgroundMaterial.MicaAlt,
                "Acrylic" => BackgroundMaterial.Acrylic,
                _ => BackgroundMaterial.Mica
            };

            // ✅ WinUI 3 一行切换材质，系统自动处理动画和主题跟随
            AppThemeManager.ApplyMaterial();
        }

        private void PanePositionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            string selected = PanePositionCombo.SelectedIndex == 1 ? "Top" : "Left";
            localSettings.Values["PanePosition"] = selected;

            if (App.MainWindow is MainWindow mainWindow)
                mainWindow.ApplySettings();
        }

        private void SoundToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            bool isOn = SoundToggle.IsOn;
            localSettings.Values["EnableSound"] = isOn;
            ElementSoundPlayer.State = isOn
                ? ElementSoundPlayerState.On
                : ElementSoundPlayerState.Off;

            if (App.MainWindow is MainWindow mainWindow)
                mainWindow.ApplySettings();
        }
    }
}