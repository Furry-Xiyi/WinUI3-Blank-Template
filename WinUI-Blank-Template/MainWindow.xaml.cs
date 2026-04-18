using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
using WinRT.Interop;
using WinUI3.Dialogs;
using WinUI3.Pages;

namespace WinUI3
{
    public sealed partial class MainWindow : Window
    {
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static SUBCLASSPROC _subclassProc = null!;
        private AppWindow m_AppWindow;
        public static MainWindow Instance { get; private set; } = null!;

        //公共打开链接弹出对话框方法
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

            // ✅ 获取 AppWindow 实例
            m_AppWindow = GetAppWindowForCurrentWindow();

            // ✅ 使用官方推荐方式设置窗口图标（立即生效）
            m_AppWindow.SetIcon("Assets/AppIcon.ico");

            // ✅ 设置标题栏文本
            TitleBarAppName.Text = Package.Current.DisplayName;

            this.SetTitleBar(TitleBarArea);

            NavView.SelectedItem = NavView.MenuItems[0];
            NavigateByTag("home");
            ContentFrame.Navigated += ContentFrame_Navigated;

            this.Activated += MainWindow_Activated;

            if (Content is FrameworkElement rootEl)
                rootEl.Loaded += Root_Loaded;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetMinWindowSize(hwnd, minWidth: 800, minHeight: 520);
        }

        // ── 获取 AppWindow 实例 ──────────────────────────────────────
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        // ── 导航核心：tag → 页面类型（约定：tag首字母大写 + "Page"）──
        // 例：home → WinUI3.Pages.HomePage，about → WinUI3.Pages.AboutPage
        private static Type? TagToPageType(string tag)
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

            string? tag = args.InvokedItemContainer?.Tag?.ToString();
            if (!string.IsNullOrEmpty(tag))
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
        }

        private void UpdateBackButton() =>
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

        // 反向查找：当前页类型 → 找到对应 Tag 的 NavItem 并选中
        private void UpdateSelectedNavItem(Type pageType)
        {
            if (pageType == typeof(SettingsPage))
            {
                NavView.SelectedItem = NavView.SettingsItem;
                return;
            }

            string? pageName = pageType?.Name;
            if (pageName == null) return;

            foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
            {
                string? tag = item.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag) && TagToPageType(tag) == pageType)
                {
                    NavView.SelectedItem = item;
                    return;
                }
            }
        }

        // ── 失焦标题文字变灰 ────────────────────────────────────────
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            bool isActive = args.WindowActivationState != WindowActivationState.Deactivated;
            TitleBarAppName.Opacity = isActive ? 1.0 : 0.5;
        }

        // ── Splash ───────────────────────────────────────────────────
        public async void ShowSplash()
        {
            // 显示 Splash
            SplashOverlay.Visibility = Visibility.Visible;
            SplashOverlay.Opacity = 1;

            await Task.Delay(1500);

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

        // ── 最小尺寸：Win32 Subclass ─────────────────────────────────
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
        // ─────────────────────────────────────────────────────────────

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            // ✅ 图标和标题已在构造函数中设置，这里只处理其他初始化
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