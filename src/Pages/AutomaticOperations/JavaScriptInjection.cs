using System;
using System.Text.Json;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace HttpCrawler.Pages;

public sealed partial class AutomaticOperationsPage
{
    private struct ReceivedMessage
    {
        public string Type { get; set; }
        public JsonDocument Data { get; set; }
        public bool IsMessageLogged { get; set; }
    }

    private async void WebView_NavigationCompletedAsync(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        // 更新导航栏状态
        UpdateWebViewNavigationBar();
        (ReloadButton.Icon as FontIcon).Glyph = "\uE72C"; // `Reload` icon

        // 注入 JavaScript 代码
        // 包含一些实用函数，并检测鼠标移动
        string script = @"
            var dbgHelper = {logMsg: false, logDownloadReq: false};

            function postMessage(type, data) {
                var msg = {Type: type, Data: data}
                window.chrome.webview.postMessage(msg);
                if (dbgHelper.logMsg === true) console.log('PostMessage: ', msg);
            }

            function download(filename, content) {
                var link = document.createElement('a');
                link.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(content));
                link.setAttribute('download', filename);

                link.style.display = 'none';
                document.body.appendChild(link);

                link.click();
                document.body.removeChild(link);
                if (dbgHelper.logDownloadReq === true) console.log('download: ', filename);
            }

            document.addEventListener('mousemove', function(e) {
                postMessage('MouseEvent', {X: e.clientX, Y: e.clientY});
            });";

        await WebView.ExecuteScriptAsync(script);
    }

    private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // 解析 JavaScript 发送回来的消息
        var msg = JsonSerializer.Deserialize<ReceivedMessage>(e.WebMessageAsJson, jsonSerializerOptions);
        switch (msg.Type)
        {
            case "MouseEvent":
                var position = JsonSerializer.Deserialize<Point>(msg.Data, jsonSerializerOptions);
                UpdateMousePosition(position);
                break;
        }
    }

    private void UpdateMousePosition(Point position)
    {
        // 更新 TextBlock 显示的鼠标位置
        CursorPositionTextBlock.Text = $"{position.X}, {position.Y}";
        if (position != mousePosition) WebView_MouseMove(position);
    }

    #region PrivateFields

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    private Point mousePosition = new(0, 0);

    #endregion
}
