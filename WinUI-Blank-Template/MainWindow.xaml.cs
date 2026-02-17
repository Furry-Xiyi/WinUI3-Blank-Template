using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WinUI3.Pages;
using Windows.ApplicationModel;
using Windows.Storage;

namespace WinUI3
{
    public sealed partial class MainWindow : Window
    {
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static SUBCLASSPROC _subclassProc;

        public MainWindow()
        {
            this.InitializeComponent();

            if (Content is FrameworkElement root)
                root.RequestedTheme = AppThemeManager.CurrentTheme;

            TitleBarAppName.Text = Package.Current.DisplayName;
            ImgAppIcon.Source = new BitmapImage(Package.Current.Logo);

            this.SetTitleBar(TitleBarArea);

            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(HomePage));
            ContentFrame.Navigated += ContentFrame_Navigated;

            if (Content is FrameworkElement rootEl)
                rootEl.Loaded += Root_Loaded;

            // 设置窗口最小尺寸（逻辑像素，自动适配 DPI）
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetMinWindowSize(hwnd, minWidth: 800, minHeight: 520);
        }

        // ── Splash ───────────────────────────────────────────────────
        public async void ShowSplash()
        {
            // 等待 1.5 秒后淡出
            await Task.Delay(1000);

            SplashFadeOut.Completed += (s, e) =>
                SplashOverlay.Visibility = Visibility.Collapsed;

            SplashFadeOut.Begin();
        }
        // ─────────────────────────────────────────────────────────────

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
            if (uMsg == 0x0024) // WM_GETMINMAXINFO
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
            ApplySettings();
            UpdateBackButton();

            if (Content is FrameworkElement root)
            {
                root.ActualThemeChanged -= AppThemeManager.OnActualThemeChanged;
                root.ActualThemeChanged += AppThemeManager.OnActualThemeChanged;
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            string tag = args.IsSettingsInvoked ? "settings" : args.InvokedItemContainer.Tag?.ToString();
            Type targetPage = null;

            switch (tag)
            {
                case "home": targetPage = typeof(HomePage); break;
                case "settings": targetPage = typeof(SettingsPage); break;
            }

            if (targetPage != null && ContentFrame.CurrentSourcePageType != targetPage)
                ContentFrame.Navigate(targetPage);
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack) ContentFrame.GoBack();
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            UpdateBackButton();
            UpdateSelectedItem(e.SourcePageType);
        }

        private void UpdateBackButton() =>
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

        private void UpdateSelectedItem(Type pageType)
        {
            if (pageType == typeof(HomePage))
                NavView.SelectedItem = NavView.MenuItems[0];
            else if (pageType == typeof(SettingsPage))
                NavView.SelectedItem = NavView.SettingsItem;
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

                bool sound = localSettings.Values["EnableSound"] is bool b ? b : true;
                if (localSettings.Values["EnableSound"] == null)
                    localSettings.Values["EnableSound"] = true;

                ElementSoundPlayer.State = sound
                    ? ElementSoundPlayerState.On
                    : ElementSoundPlayerState.Off;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplySettings Error: {ex.Message}");
            }
        }
    }
}