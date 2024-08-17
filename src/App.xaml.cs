using System;
using System.IO;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition.SystemBackdrops;
using System.Diagnostics.CodeAnalysis;

namespace WebTool
{
    public partial class App : Application
    {
        public static readonly string SpecificDirectory = @"%USERPROFILE%\Documents\WebTool";
        public static readonly string FullVersion = "Beta 1.0 build 0816";
        public static readonly string ShortVersion = "1.0";
        public static readonly string Language = "zh-TW";

        public App()
        {
            this.InitializeComponent();
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", $"--lang={Language}");

            var path = Environment.ExpandEnvironmentVariables(SpecificDirectory);

            // 应用专用目录
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            Directory.SetCurrentDirectory(path);
            Directory.CreateDirectory("Config");
            Directory.CreateDirectory("Downloads");
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();

            // 还原应用设置
            ElementTheme = GetAppData("RequestedTheme", ElementTheme.Default);
            Backdrop = GetAppData("AppBackdrop", AppBackdrop.Mica);

            m_window.Activate();
        }

        public static void StoreAppData(string key, object value)
        {
            if (value is Enum) value = (int)value;
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }

        public static T GetAppData<T>(string key, [NotNull] T fallbackValue) where T : struct
        {
            var value = ApplicationData.Current.LocalSettings.Values[key];
            return value is null ? fallbackValue : (T)value;
        }

        private static Window m_window;

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

        private static ElementTheme m_theme;

        public static ElementTheme ElementTheme
        {
            get => m_theme;
            set
            {
                m_theme = value;
                StoreAppData("RequestedTheme", value);
                ThemeChanged?.Invoke(value);
            }
        }

        public static readonly MicaBackdrop MicaBackdrop = new();
        
        public static readonly MicaBackdrop MicaAltBackdrop = new() { Kind = MicaKind.BaseAlt };
        
        public static readonly DesktopAcrylicBackdrop AcrylicBackdrop = new();

        public enum AppBackdrop { Mica, MicaALt, Acrylic };

        private static AppBackdrop m_backdrop;

        public static AppBackdrop Backdrop
        {
            get => m_backdrop;
            set
            {
                m_backdrop = value;
                StoreAppData("AppBackdrop", value);
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
    }
}
