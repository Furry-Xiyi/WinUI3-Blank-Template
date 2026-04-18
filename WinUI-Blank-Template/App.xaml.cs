using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI;

namespace WinUI3
{
    public partial class App : Application
    {
        public static Window? MainWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // ── 单实例 ──────────────────────────────────────────────
            var mainInstance = AppInstance.FindOrRegisterForKey("main-instance");

            if (!mainInstance.IsCurrent)
            {
                // 使用 Task.Run 避免阻塞主线程
                var redirectTask = mainInstance.RedirectActivationToAsync(
                    AppInstance.GetCurrent().GetActivatedEventArgs()
                );
                // 等待重定向完成后退出
                redirectTask.AsTask().Wait(TimeSpan.FromSeconds(5));
                Environment.Exit(0);
                return;
            }

            mainInstance.Activated += (_, _) =>
            {
                if (MainWindow != null)
                {
                    MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
                        BringWindowToFront(hwnd);
                    });
                }
            };
            // ────────────────────────────────────────────────────────

            AppThemeManager.LoadSettings();

            MainWindow = new MainWindow();

            // ✅ SetupTitleBar 只操作 AppWindow.TitleBar，不依赖 XAML 资源
            // 必须在 Activate() 之前调用，否则窗口弹出瞬间会闪原生标题栏
            AppThemeManager.SetupTitleBar();

            // ✅ 立刻激活窗口，Splash 遮罩第一帧就可见，用户感知秒开
            MainWindow.Activate();

            // ✅ 其余初始化全部延后到下一帧，Splash 完全遮住，用户无感知
            MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                try { MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico"); } catch { }
                AppThemeManager.ApplyMaterial();

                var loader = new ResourceLoader();
                MainWindow.AppWindow.Title = loader.GetString("AppTitle");

                ((MainWindow)MainWindow).ShowSplash();
            });
        }

        // ── Win32：把窗口拉到前台 ────────────────────────────────────
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);

        static void BringWindowToFront(IntPtr hwnd)
        {
            if (IsIconic(hwnd)) ShowWindow(hwnd, 9); // SW_RESTORE
            SetForegroundWindow(hwnd);
        }
    }

    public static class AppThemeManager
    {
        public static ElementTheme CurrentTheme = ElementTheme.Default;
        public static BackgroundMaterial CurrentMaterial = BackgroundMaterial.Mica;

        public static void LoadSettings()
        {
            var s = ApplicationData.Current.LocalSettings;

            try
            {
                string theme = s.Values["AppTheme"] as string ?? "System";
                CurrentTheme = theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSettings - Theme error: {ex.Message}");
                CurrentTheme = ElementTheme.Default;
            }

            try
            {
                string material = s.Values["AppMaterial"] as string ?? "MicaAlt";
                CurrentMaterial = material switch
                {
                    "MicaAlt" => BackgroundMaterial.MicaAlt,
                    "Acrylic" => BackgroundMaterial.Acrylic,
                    _ => BackgroundMaterial.Mica
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSettings - Material error: {ex.Message}");
                CurrentMaterial = BackgroundMaterial.Mica;
            }

            try
            {
                bool sound = s.Values["EnableSound"] is bool b ? b : true;
                if (s.Values["EnableSound"] == null)
                    s.Values["EnableSound"] = true;
                ElementSoundPlayer.State = sound
                    ? ElementSoundPlayerState.On
                    : ElementSoundPlayerState.Off;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSettings - Sound error: {ex.Message}");
                ElementSoundPlayer.State = ElementSoundPlayerState.On;
            }
        }

        public static void ApplyMaterial()
        {
            if (App.MainWindow == null) return;
            try
            {
                if (App.MainWindow.SystemBackdrop is MicaBackdrop mica)
                {
                    if (CurrentMaterial == BackgroundMaterial.Mica && mica.Kind == MicaKind.Base) return;
                    if (CurrentMaterial == BackgroundMaterial.MicaAlt && mica.Kind == MicaKind.BaseAlt) return;
                }
                else if (App.MainWindow.SystemBackdrop is DesktopAcrylicBackdrop &&
                         CurrentMaterial == BackgroundMaterial.Acrylic)
                {
                    return;
                }

                App.MainWindow.SystemBackdrop = CurrentMaterial switch
                {
                    BackgroundMaterial.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                    BackgroundMaterial.Acrylic => new DesktopAcrylicBackdrop(),
                    _ => new MicaBackdrop { Kind = MicaKind.Base }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyMaterial failed: {ex.Message}");
                App.MainWindow.SystemBackdrop = null;
            }
        }

        public static void SetupTitleBar()
        {
            if (App.MainWindow == null) return;
            try
            {
                if (!AppWindowTitleBar.IsCustomizationSupported()) return;
                var titleBar = App.MainWindow.AppWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                UpdateTitleBarColors();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetupTitleBar failed: {ex.Message}");
            }
        }

        public static void UpdateTitleBarColors()
        {
            if (App.MainWindow == null) return;
            try
            {
                if (!AppWindowTitleBar.IsCustomizationSupported()) return;
                var titleBar = App.MainWindow.AppWindow.TitleBar;
                bool isDark = GetIsDarkTheme();

                var fg = isDark ? Colors.White : Colors.Black;
                var inactiveFg = isDark
                    ? Color.FromArgb(255, 128, 128, 128)
                    : Color.FromArgb(255, 160, 160, 160);
                var hoverBg = isDark
                    ? Color.FromArgb(20, 255, 255, 255)
                    : Color.FromArgb(20, 0, 0, 0);

                titleBar.ButtonForegroundColor = fg;
                titleBar.ButtonInactiveForegroundColor = inactiveFg;
                titleBar.ButtonHoverBackgroundColor = hoverBg;
                titleBar.ButtonHoverForegroundColor = fg;
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(30, hoverBg.R, hoverBg.G, hoverBg.B);
                titleBar.ButtonPressedForegroundColor = fg;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateTitleBarColors failed: {ex.Message}");
            }
        }

        public static void OnActualThemeChanged(FrameworkElement sender, object args)
        {
            UpdateTitleBarColors();
        }

        public static bool GetIsDarkTheme()
        {
            if (App.MainWindow?.Content is FrameworkElement root)
            {
                var actual = root.ActualTheme;
                if (actual != ElementTheme.Default)
                    return actual == ElementTheme.Dark;
            }
            if (CurrentTheme == ElementTheme.Default)
                return Application.Current.RequestedTheme == ApplicationTheme.Dark;
            return CurrentTheme == ElementTheme.Dark;
        }
    }

    public enum BackgroundMaterial
    {
        Mica,
        MicaAlt,
        Acrylic
    }
}