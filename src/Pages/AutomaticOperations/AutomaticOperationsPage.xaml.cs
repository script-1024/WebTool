using System;
using System.Web;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using WebTool.Lib;

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
            SubscribeToWebViewEvents();

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

        private async Task SubscribeToWebViewEvents()
        {
            // 浏览器导航事件
            WebView.NavigationStarting += (_, _) => UpdateWebViewNavigationBar();
            WebView.NavigationCompleted += WebView_NavigationCompletedAsync;

            // 负责处理从 JavaScript 传回的消息
            // 必须先确保 CoreWebView2 已实例化
            await WebView.EnsureCoreWebView2Async();
            WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
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

                WebView.Source = new Uri(userInput);
            }
            catch (UriFormatException)
            {
                var param = HttpUtility.UrlEncode(UriTextBox.Text);
                WebView.Source = new Uri($"https://www.google.com/search?q={param}");
            }
        }

        #region PrivateFields

        private bool isUpdateDisabled = false;

        #endregion
    }
}
