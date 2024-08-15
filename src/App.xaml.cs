using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;

namespace WebTool
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            // 还原应用设置
            ElementTheme = (ElementTheme)(ApplicationData.Current.LocalSettings.Values["RequestedTheme"] ?? 0);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

            // 手动广播事件，更新窗口主题
            OnThemeChanged?.Invoke(m_theme);
        }

        private static Window m_window;
        private static ElementTheme m_theme;

        public delegate void ThemeChanged(ElementTheme theme);
        public static event ThemeChanged OnThemeChanged;
        public static ElementTheme ElementTheme
        {
            get => m_theme;
            set
            {
                m_theme = value;
                ApplicationData.Current.LocalSettings.Values["RequestedTheme"] = (int)value;
                OnThemeChanged?.Invoke(value);
            }
        }

        public static new App Current { get => Application.Current as App; }
        public static MainWindow MainWindow
        {
            get => (m_window is null || m_window is not MainWindow mainWindow) ? null : mainWindow;
        }

        public static readonly MicaBackdrop MicaBackdrop = new();
        public static readonly MicaBackdrop MicaAltBackdrop = new() { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt };
        public static readonly DesktopAcrylicBackdrop AcrylicBackdrop = new();
    }
}
