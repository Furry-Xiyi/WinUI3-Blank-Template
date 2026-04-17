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
            // ActualThemeChanged 已在 MainWindow.Root_Loaded 统一注册，此处无需重复
        }

        private void LoadUI()
        {
            string theme = localSettings.Values["AppTheme"] as string ?? "System";
            RbTheme.SelectedIndex = theme switch
            {
                "Light" => 1,
                "Dark" => 2,
                _ => 0
            };

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
                // 优先使用 Package 信息
                TxtAppName.Text = Package.Current.DisplayName;
                var v = Package.Current.Id.Version;
                TxtVersion.Text = $"Version {v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
                ImgAppIcon.Source = new BitmapImage(Package.Current.Logo);
                TxtCopyright.Text = $"© {DateTime.Now.Year} {Package.Current.PublisherDisplayName}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadAppInfo 错误: {ex.Message}");
                // 非打包应用时使用资源文件中的本地化名称
                var resourceLoader = new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader();
                try
                {
                    TxtAppName.Text = resourceLoader.GetString("AppDisplayName");
                }
                catch
                {
                    TxtAppName.Text = "WinUI 3 Template";
                }
                
                TxtVersion.Text = "Version 1.0.0.0";
                TxtCopyright.Text = $"© {DateTime.Now.Year} Developer";
                
                try
                {
                    ImgAppIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Square44x44Logo.scale-200.png"));
                }
                catch
                {
                    // 如果图标也加载失败，就不显示
                }
            }
        }

        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.OpenExternalLink(sender, e);
        }

        private void RbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

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
            AppThemeManager.CurrentTheme = theme;

            if (App.MainWindow?.Content is FrameworkElement root)
                root.RequestedTheme = theme;

            AppThemeManager.UpdateTitleBarColors();
        }

        private void RbMaterial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

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

            AppThemeManager.ApplyMaterial();
        }

        private void PanePositionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            localSettings.Values["PanePosition"] = PanePositionCombo.SelectedIndex == 1 ? "Top" : "Left";

            if (App.MainWindow is MainWindow mainWindow)
                mainWindow.ApplySettings();
        }

        private void SoundToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            bool isOn = SoundToggle.IsOn;
            localSettings.Values["EnableSound"] = isOn;

            // 直接设置，不再调 ApplySettings（避免重复执行导航栏逻辑）
            ElementSoundPlayer.State = isOn
                ? ElementSoundPlayerState.On
                : ElementSoundPlayerState.Off;
        }
    }
}
