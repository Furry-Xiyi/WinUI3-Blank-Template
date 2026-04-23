using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.Linq;
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
        // Win32 常量
        private const uint WM_GETMINMAXINFO = 0x0024;
        private const int SW_RESTORE = 9;

        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private static SUBCLASSPROC? _subclassProc;
        private readonly AppWindow _appWindow;
        private readonly IntPtr _hwnd;
        
        public static MainWindow? Instance { get; private set; }

        /// <summary>
        /// 公共打开链接弹出对话框方法
        /// </summary>
        public async void OpenExternalLink(object sender, RoutedEventArgs e)
        {
            var root = (ContentFrame.Content as FrameworkElement)?.XamlRoot;
            if (root == null)
                return;

            string? url = null;
            
            if (sender is Button btn && btn.Tag is string btnUrl)
                url = btnUrl;
            else if (sender is HyperlinkButton link && link.Tag is string linkUrl)
                url = linkUrl;

            if (string.IsNullOrEmpty(url))
                return;

            var dialog = new ExternalOpenDialog { XamlRoot = root };
            var result = await dialog.ShowAsync();

            // Secondary 按钮表示"是，打开"
            if (result == ContentDialogResult.Secondary)
            {
                try
                {
                    await Launcher.LaunchUriAsync(new Uri(url));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to launch URL: {ex.Message}");
                }
            }
        }

        public MainWindow()
        {
            this.InitializeComponent();
            Instance = this;

            if (Content is FrameworkElement root)
                root.RequestedTheme = AppThemeManager.CurrentTheme;

            // ✅ 获取 AppWindow 实例和窗口句柄
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            _appWindow = GetAppWindowForCurrentWindow();

            // ✅ 使用官方推荐方式设置窗口图标（立即生效）
            _appWindow.SetIcon("Assets/AppIcon.ico");

            // ✅ 设置标题栏文本
            TitleBarAppName.Text = Package.Current.DisplayName;

            this.SetTitleBar(TitleBarArea);

            NavView.SelectedItem = NavView.MenuItems[0];
            NavigateByTag("home");
            ContentFrame.Navigated += ContentFrame_Navigated;

            this.Activated += MainWindow_Activated;
            this.Closed += MainWindow_Closed;

            if (Content is FrameworkElement rootEl)
                rootEl.Loaded += Root_Loaded;

            SetMinWindowSize(_hwnd, minWidth: 800, minHeight: 520);
        }

        // ── 获取 AppWindow 实例 ──────────────────────────────────────
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        // ── 导航核心：tag → 页面类型映射 ──
        private static readonly System.Collections.Generic.Dictionary<string, Type> _pageTypeMap = new()
        {
            { "home", typeof(HomePage) },
            { "settings", typeof(SettingsPage) }
        };

        private static Type? TagToPageType(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) 
                return null;
            
            _pageTypeMap.TryGetValue(tag.ToLowerInvariant(), out var pageType);
            return pageType;
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

        // ── 窗口关闭清理 ────────────────────────────────────────────
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            // 清理 Win32 subclass
            if (_subclassProc != null)
            {
                RemoveWindowSubclass(_hwnd, _subclassProc, 0);
                _subclassProc = null;
            }

            // 清理事件订阅
            if (Content is FrameworkElement root)
            {
                root.ActualThemeChanged -= AppThemeManager.OnActualThemeChanged;
            }

            Instance = null;
        }

        // ── Splash ───────────────────────────────────────────────────
        public async void ShowSplash()
        {
            SplashOverlay.Visibility = Visibility.Visible;
            SplashOverlay.Opacity = 1;

            // 等待短暂时间后开始淡入动画（纯色 -> 启动屏幕）
            await Task.Delay(100);
            SplashFadeIn.Begin();

            // 等待淡入完成 + 显示时间
            await Task.Delay(1500);

            // 使用 TaskCompletionSource 等待动画完全完成
            var tcs = new TaskCompletionSource<bool>();
            
            SplashFadeOut.Completed += (s, e) =>
            {
                tcs.SetResult(true);
            };

            SplashFadeOut.Begin();
            
            // 等待淡出动画完成
            await tcs.Task;
            
            // 确保启动屏幕完全隐藏
            SplashOverlay.Visibility = Visibility.Collapsed;

            bool sound = _localSettings.Values["EnableSound"] is bool b ? b : true;
            ElementSoundPlayer.State = sound
                ? ElementSoundPlayerState.On
                : ElementSoundPlayerState.Off;
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
            if (uMsg == WM_GETMINMAXINFO)
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
        static extern bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, nuint uIdSubclass);
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
                string position = _localSettings.Values["PanePosition"] as string ?? "Left";
                if (_localSettings.Values["PanePosition"] == null)
                    _localSettings.Values["PanePosition"] = "Left";

                NavView.PaneDisplayMode = position == "Top"
                    ? NavigationViewPaneDisplayMode.Top
                    : NavigationViewPaneDisplayMode.Left;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplySettings Error: {ex.Message}");
            }
        }
    }
}