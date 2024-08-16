using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using WebTool.Pages;
using Windows.Win32.Core;

namespace WebTool
{
    /// <summary>
    /// 应用主窗口
    /// </summary>
    public sealed partial class MainWindow : DesktopWindow
    {
        /// <summary>
        /// 窗口构造函数
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // 设置窗口最小尺寸
            this.MinHeight = 600;
            this.MinWidth = 800;

            // 外观样式
            this.ExtendsContentIntoTitleBar = true;
            this.SystemBackdrop = new MicaBackdrop();
            App.ThemeChanged += (theme) => RootGrid.RequestedTheme = theme;

            // 导航控件
            Navi.SelectionChanged += Navi_SelectionChanged;
            Navi.Loaded += (_, _) =>
            {
                Navi.SelectedItem = Navi.MenuItems[0];
                var settingsItem = Navi.SettingsItem as NavigationViewItem;
                settingsItem.Content = "設定";
            };
        }

        /// <summary>
        /// 导航列准备切换视图
        /// </summary>
        private void Navi_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // 导航到设置页
            if (args.IsSettingsSelected) ContentFrame.Navigate(typeof(SettingsPage));
            else
            {
                // 依据选中项决定被导航页面
                var invokedItem = args.SelectedItemContainer as NavigationViewItem;
                switch (invokedItem.Tag)
                {
                    case "HttpReq":
                        ContentFrame.Navigate(typeof(HttpRequestPage));
                        break;
                    case "AutoOps":
                        ContentFrame.Navigate(typeof(AutomaticOperationsPage));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
