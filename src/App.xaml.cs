using System;
using System.IO;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition.SystemBackdrops;
using WebTool.Lib;

namespace WebTool
{
    public partial class App : Application
    {
        public static string WorkingDirectory { get; private set; } = @"%USERPROFILE%\Documents\WebTool";
        public static string FullVersion { get; private set; } = "Beta 1.4 build 0828";
        public static string ShortVersion { get; private set; } = "1.4";
        public static string Language { get; private set; } = "zh-TW";

        public App()
        {
            this.InitializeComponent();
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", $"--lang={Language}");

            var expandedPath = Environment.ExpandEnvironmentVariables(WorkingDirectory);

            // 应用专用目录
            if (!Directory.Exists(expandedPath)) Directory.CreateDirectory(expandedPath);
            Directory.SetCurrentDirectory(expandedPath);
            Directory.CreateDirectory("Config");
            Directory.CreateDirectory("Downloads");
            Directory.CreateDirectory("Scripts");

            WorkingDirectory = expandedPath;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // 窗口实例
            m_window = new MainWindow();

            // 还原应用设置
            ElementTheme = GetAppData("RequestedTheme", ElementTheme.Default);
            Backdrop = GetAppData("AppBackdrop", AppBackdrop.Mica);

            // 呼出主窗口
            m_window.Activate();

            // 加载配置文件
            AppConfig.Reload();
        }

        public static void RemoveAppData(string key)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(key)) localSettings.Values.Remove(key);
        }

        public static void SetAppData(string key, object value)
        {
            if (value is Enum) value = (int)value;
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }

        public static T GetAppData<T>(string key, T fallbackValue)
        {
            var value = ApplicationData.Current.LocalSettings.Values[key];
            return value is null ? fallbackValue : (T)value;
        }

        public static new App Current
        {
            get => Application.Current as App;
        }

        public static MainWindow MainWindow
        {
            get => (m_window is null || m_window is not MainWindow mainWindow) ? null : mainWindow;
        }

        public delegate void ThemeChangedEvent(ElementTheme theme);

        public static event ThemeChangedEvent ThemeChanged;

        public static ElementTheme ElementTheme
        {
            get => m_theme;
            set
            {
                m_theme = value;
                SetAppData("RequestedTheme", value);
                ThemeChanged?.Invoke(value);
            }
        }

        public static readonly MicaBackdrop MicaBackdrop = new();
        
        public static readonly MicaBackdrop MicaAltBackdrop = new() { Kind = MicaKind.BaseAlt };
        
        public static readonly DesktopAcrylicBackdrop AcrylicBackdrop = new();

        public enum AppBackdrop { Mica, MicaALt, Acrylic };

        public static AppBackdrop Backdrop
        {
            get => m_backdrop;
            set
            {
                m_backdrop = value;
                SetAppData("AppBackdrop", value);
                switch (value)
                {
                    case AppBackdrop.Mica:
                        m_window.SystemBackdrop = MicaBackdrop;
                        break;
                    case AppBackdrop.MicaALt:
                        m_window.SystemBackdrop = MicaAltBackdrop;
                        break;
                    case AppBackdrop.Acrylic:
                        m_window.SystemBackdrop = AcrylicBackdrop;
                        break;
                }
            }
        }

        #region PrivateFields

        private static Window m_window;
        private static ElementTheme m_theme;
        private static AppBackdrop m_backdrop;

        #endregion
    }
}
