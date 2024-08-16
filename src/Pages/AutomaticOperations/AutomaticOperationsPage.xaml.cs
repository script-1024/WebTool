﻿using System;
using System.Web;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using WebTool.Lib;
using System.Text.RegularExpressions;

namespace WebTool.Pages
{
    /// <summary>
    /// 自动操作页面
    /// </summary>
    public sealed partial class AutomaticOperationsPage : Page
    {
        public AutomaticOperationsPage()
        {
            this.InitializeComponent();

            // 订阅浏览器事件
            PrepareWebViewAsync();

            // 应用主题更新事件
            App.ThemeChanged += App_ThemeChanged;

            // 按键导航事件
            UriTextBox.KeyDown += UriTextBox_KeyDown;

            // 导航栏按钮
            GoBackButton.Click += (_, _) => WebView.GoBack();

            GoForwardButton.Click += (_, _) => WebView.GoForward();

            ReloadButton.Click += (_, _) => WebView.Reload();

            OpenPanelButton.Click += (_, _) =>
            {
                var visible = (bool)OpenPanelButton.IsChecked;
                Splitter.Visibility = AdvancedPanel.SetVisibility(visible);
                if (!visible) RootGrid.ColumnDefinitions[1].Width = new(); // 重置宽度
            };
        }

        private async void PrepareWebViewAsync()
        {
            // 浏览器导航事件
            WebView.NavigationStarting += (_, _) => UpdateWebViewNavigationBar();
            WebView.NavigationCompleted += WebView_NavigationCompletedAsync;

            // 负责处理从 JavaScript 传回的消息
            // 必须先确保 CoreWebView2 已实例化
            await WebView.EnsureCoreWebView2Async();
            WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            WebView.CoreWebView2.Settings.UserAgent =
               @$"Mozilla/5.0 (Windows NT 10.0; Win64; x64)
                  AppleWebKit/537.36 (KHTML, like Gecko)
                  Chrome/127.0.0.0 Safari/537.36 Edg/127.0.0.0 WebTool/{App.ShortVersion}";
        }

        private void UpdateWebViewNavigationBar()
        {
            (ReloadButton.Icon as FontIcon).Glyph = "\uE711"; // `Cancel` icon
            if (isUpdateDisabled) { isUpdateDisabled = false; return; }

            // 更新导航栏控件状态
            UriTextBox.Text = WebView.Source.AbsoluteUri;
            GoBackButton.IsEnabled = WebView.CanGoBack;
            GoForwardButton.IsEnabled = WebView.CanGoForward;
        }

        private void TryGoToUri(string userInput)
        {
            // 暫时禁止导航列更新，避免网址列瞬间闪回旧网址
            isUpdateDisabled = true;

            // 自动聚焦到浏览器，使输入框失去焦点
            WebView.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

            if (userInput == "about:blank")
            {
                WebView.Source = new("about:blank");
                return;
            }

            void fallBackSearchOnGoogle()
            {
                var param = HttpUtility.UrlEncode(UriTextBox.Text.Trim());
                WebView.Source = new Uri($"https://www.google.com/search?q={param}");
            }

            // 尝试浏览到目标网址
            // 若网址格式错误，将其视为关键字从谷歌搜索
            try
            {
                // 检查是否已包含 HTTP 协议头
                if (!userInput.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !userInput.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    userInput = "http://" + userInput;
                }

                if (HttpRegex().IsMatch(userInput)) WebView.Source = new Uri(userInput);
                else fallBackSearchOnGoogle();
            }
            catch (UriFormatException)
            {
                fallBackSearchOnGoogle();
            }
        }

        #region PrivateFields

        private bool isUpdateDisabled = false;

        [GeneratedRegex(@"^http(s?)://([a-zA-Z0-9-]+\.)+[a-zA-Z0-9-]+(/.*)?$")]
        private static partial Regex HttpRegex();

        #endregion
    }
}