﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using HttpCrawler.Pages;
using Windows.Win32.Core;

namespace HttpCrawler
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
            App.OnThemeChanged += (theme) => RootGrid.RequestedTheme = theme;

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
                // 依据被点击选项卡决定被导航页面
                var invokedItem = args.SelectedItemContainer as NavigationViewItem;
                switch (invokedItem.Tag)
                {
                    case "HttpRequest":
                        ContentFrame.Navigate(typeof(HttpRequestPage));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
