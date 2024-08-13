using System;
using System.Text.Json;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System.Text;
using System.Web;
using HttpCrawler.Lib;
using Microsoft.UI.Xaml;

namespace HttpCrawler.Pages
{
    /// <summary>
    /// 自动操作页面
    /// </summary>
    public sealed partial class AutomaticOperationsPage : Page
    {
        public AutomaticOperationsPage()
        {
            this.InitializeComponent();
            WebView.NavigationStarting += (_, _) => UpdateWebViewNavigationBar();
            WebView.NavigationCompleted += WebView_NavigationCompletedAsync;
            UriTextBox.KeyDown += UriTextBox_KeyDown;

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

        private bool _isUpdateDisabled = false;
        private void UpdateWebViewNavigationBar()
        {
            (ReloadButton.Icon as FontIcon).Glyph = "\uE711"; // `Cancel` icon
            if (_isUpdateDisabled) { _isUpdateDisabled = false; return; }

            // 更新导航栏控件状态
            UriTextBox.Text = WebView.Source.AbsoluteUri;
            GoBackButton.IsEnabled = WebView.CanGoBack;
            GoForwardButton.IsEnabled = WebView.CanGoForward;
        }
        private void UriTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                // 暫时禁止导航列更新，避免网址列瞬间闪回旧网址
                _isUpdateDisabled = true;

                // 自动聚焦到浏览器，使输入框失去焦点
                WebView.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

                // 若网址格式错误，将其视为关键字从谷歌搜索
                try { WebView.Source = new Uri(UriTextBox.Text); }
                catch (UriFormatException)
                {
                    var param = HttpUtility.UrlEncode(UriTextBox.Text);
                    WebView.Source = new Uri($"https://www.google.com/search?q={param}");
                }
            }
        }

        #region "DetectMouseMovement"

        private Point _mousePosition = new(0, 0);
        private async void WebView_NavigationCompletedAsync(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            // 更新状态
            UpdateWebViewNavigationBar();
            (ReloadButton.Icon as FontIcon).Glyph = "\uE72C"; // `Reload` icon

            // 注入 JavaScript 用于捕捉鼠标移动，返回一个 C# Point 结构体
            string script = @"
                var logHelper = {
                    logEvent: false
                }

                document.addEventListener('mousemove', function(e) {
                    window.chrome.webview.postMessage({X: e.clientX, Y: e.clientY});
                    if (logHelper.logEvent === true) console.log(e);
                });
            ";

            await WebView.ExecuteScriptAsync(script);

            // 处理从 JavaScript 传回的消息
            WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        }
        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // 解析 JavaScript 发送的消息
            var position = JsonSerializer.Deserialize<Point>(e.WebMessageAsJson);

            // 更新 TextBlock 显示的鼠标位置
            CursorPositionTextBlock.Text = $"{position.X}, {position.Y}";

            if (position != _mousePosition) WebView_MouseMove(position);
        }

        #endregion

        /// <summary>
        /// 当鼠标在 WebView 内移动时发生事件
        /// </summary>
        private void WebView_MouseMove(Point newPosition)
        {
            _mousePosition = newPosition;
        }
    }
}
