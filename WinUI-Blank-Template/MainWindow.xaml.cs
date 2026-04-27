using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
using WinUI3.Dialogs;
using WinUI3.Pages;

namespace WinUI3
{
    public sealed partial class MainWindow : Window
    {
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static SUBCLASSPROC _subclassProc;
        public static MainWindow Instance { get; private set; }
        public ObservableCollection<string> BreadcrumbItems { get; } = new ObservableCollection<string>();
        private readonly ResourceLoader _loader = new ResourceLoader();

        public async void OpenExternalLink(object sender, RoutedEventArgs e)
        {
            var root = (ContentFrame.Content as FrameworkElement)?.XamlRoot;
            if (root == null)
                return;

            if (sender is Button btn && btn.Tag is string url)
            {
                var dialog = new ExternalOpenDialog { XamlRoot = root };
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                    await Launcher.LaunchUriAsync(new Uri(url));
            }
            else if (sender is HyperlinkButton link && link.Tag is string linkUrl)
            {
                var dialog = new ExternalOpenDialog { XamlRoot = root };
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                    await Launcher.LaunchUriAsync(new Uri(linkUrl));
            }
        }

        public MainWindow()
        {
            this.InitializeComponent();
            Instance = this;

            if (Content is FrameworkElement root)
                root.RequestedTheme = AppThemeManager.CurrentTheme;

            this.SetTitleBar(TitleBarArea);

            // 移除原本在这里的 NavigateByTag("home")，让给 StartLoadingContent 以保证 Splash 优先渲染
            ContentFrame.Navigated += ContentFrame_Navigated;

            this.Activated += MainWindow_Activated;

            if (Content is FrameworkElement rootEl)
                rootEl.Loaded += Root_Loaded;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetMinWindowSize(hwnd, minWidth: 800, minHeight: 520);
        }

        // 专门提取出来的加载逻辑
        public void StartLoadingContent()
        {
            NavView.SelectedItem = NavView.MenuItems[0];
            NavigateByTag("home");
        }

        private static Type TagToPageType(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            string typeName = $"WinUI3.Pages.{char.ToUpper(tag[0])}{tag.Substring(1)}Page";
            return Assembly.GetExecutingAssembly().GetType(typeName);
        }

        private void NavigateByTag(string tag)
        {
            var pageType = TagToPageType(tag);
            if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
                ContentFrame.Navigate(pageType);
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                if (ContentFrame.CurrentSourcePageType != typeof(SettingsPage))
                    ContentFrame.Navigate(typeof(SettingsPage));
                return;
            }

            string tag = args.InvokedItemContainer?.Tag?.ToString();
            NavigateByTag(tag);
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack) ContentFrame.GoBack();
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            UpdateBackButton();
            UpdateSelectedNavItem(e.SourcePageType);
            UpdateBreadcrumb(e.SourcePageType);
        }

        private void UpdateBackButton() =>
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

        private void UpdateSelectedNavItem(Type pageType)
        {
            if (pageType == typeof(SettingsPage))
            {
                NavView.SelectedItem = NavView.SettingsItem;
                return;
            }

            string pageName = pageType?.Name;
            if (pageName == null) return;

            foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
            {
                string tag = item.Tag?.ToString();
                if (TagToPageType(tag) == pageType)
                {
                    NavView.SelectedItem = item;
                    return;
                }
            }
        }

        private void UpdateBreadcrumb(Type pageType)
        {
            BreadcrumbItems.Clear();
            if (pageType == typeof(SettingsPage))
            {
                BreadcrumbItems.Add(_loader.GetString("Settings_Breadcrumb"));
                BreadcrumbPanel.Visibility = Visibility.Visible;
            }
            else
            {
                BreadcrumbPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            bool isActive = args.WindowActivationState != WindowActivationState.Deactivated;
            TitleBarAppName.Opacity = isActive ? 1.0 : 0.5;
        }

        // 接收已加载完成的信号
        public async Task FinishLoadingAndHideSplashAsync()
        {
            // 收到信息后延迟 500ms
            await Task.Delay(500);

            SplashFadeOut.Completed += (s, e) =>
            {
                SplashOverlay.Visibility = Visibility.Collapsed;

                bool sound = localSettings.Values["EnableSound"] is bool b ? b : true;
                ElementSoundPlayer.State = sound
                    ? ElementSoundPlayerState.On
                    : ElementSoundPlayerState.Off;
            };

            SplashFadeOut.Begin();
        }

        static int _minW, _minH;

        static void SetMinWindowSize(IntPtr hwnd, int minWidth, int minHeight)
        {
            _minW = minWidth;
            _minH = minHeight;
            _subclassProc = SubclassProc;
            SetWindowSubclass(hwnd, _subclassProc, 0, 0);
        }

        static nuint SubclassProc(IntPtr hWnd, uint uMsg, nuint wParam, nint lParam,
                                   nuint uIdSubclass, nuint dwRefData)
        {
            if (uMsg == 0x0024)
            {
                double dpi = GetDpiForWindow(hWnd) / 96.0;
                var info = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                info.ptMinTrackSize.x = (int)(_minW * dpi);
                info.ptMinTrackSize.y = (int)(_minH * dpi);
                Marshal.StructureToPtr(info, lParam, true);
            }
            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        delegate nuint SUBCLASSPROC(IntPtr hWnd, uint uMsg, nuint wParam, nint lParam,
                                     nuint uIdSubclass, nuint dwRefData);

        [DllImport("comctl32.dll")]
        static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass,
                                              nuint uIdSubclass, nuint dwRefData);
        [DllImport("comctl32.dll")]
        static extern nuint DefSubclassProc(IntPtr hWnd, uint uMsg, nuint wParam, nint lParam);
        [DllImport("user32.dll")]
        static extern uint GetDpiForWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        struct MINMAXINFO
        {
            public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int x, y; }

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            string displayName = _loader.GetString("AppDisplayName");
            TitleBarAppName.Text = displayName;
            ImgAppIcon.Source = new BitmapImage(Package.Current.Logo);

            // 确保任务栏显示应用名称
            if (this.AppWindow != null && string.IsNullOrEmpty(this.AppWindow.Title))
            {
                this.AppWindow.Title = displayName;
            }

            ApplySettings();
            UpdateBackButton();

            if (Content is FrameworkElement root)
            {
                root.ActualThemeChanged -= AppThemeManager.OnActualThemeChanged;
                root.ActualThemeChanged += AppThemeManager.OnActualThemeChanged;
            }
        }

        public void ApplySettings()
        {
            try
            {
                string position = localSettings.Values["PanePosition"] as string ?? "Left";
                if (localSettings.Values["PanePosition"] == null)
                    localSettings.Values["PanePosition"] = "Left";

                NavView.PaneDisplayMode = position == "Top"
                    ? NavigationViewPaneDisplayMode.Top
                    : NavigationViewPaneDisplayMode.Left;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplySettings Error: {ex.Message}");
            }
        }
    }
}