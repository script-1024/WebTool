using System;
using System.Text.Json;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace WebTool.Pages;

public sealed partial class AutomaticOperationsPage
{
    private struct ReceivedMessage
    {
        public string Type { get; set; }
        public JsonDocument Data { get; set; }
    }

    private async void WebView_NavigationCompletedAsync(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        // 更新导航栏状态
        UpdateWebViewNavigationBar();
        (ReloadButton.Icon as FontIcon).Glyph = "\uE72C"; // `Reload` icon

        // 注入 JavaScript 代码
        // 包含一些实用函数，并检测鼠标移动
        string script = @"
            const dbgHelper = {
                postMsg: {useWhiteList: false, blockList: ['MouseEvent']}
            };

            function postMessage(type, data) {
                const msg = {Type: type, Data: data}
                window.chrome.webview?.postMessage(msg);
                const useWhiteList = dbgHelper.postMsg.useWhiteList;
                const inBlockList = (dbgHelper.postMsg.blockList.indexOf(type) > -1);
                if (!(useWhiteList ^ inBlockList)) console.log('PostMessage: ', msg);
            }

            document.addEventListener('mousemove', (e) => {
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
            case "WriteToFile":
                if (xlsxFile != null) WriteToFile(msg.Data);
                break;
            case "SaveFile":
                xlsxFile?.SaveAndClose();
                xlsxFile = null;
                break;
        }
    }

    private void UpdateMousePosition(Point position)
    {
        // 更新 TextBlock 显示的鼠标位置
        CursorPositionTextBlock.Text = $"{position.X}, {position.Y}";
        if (position != mousePosition) WebView_MouseMove(position);
    }

    private void WriteToFile(JsonDocument jsonData)
    {
        var list = jsonData.Deserialize<List<Dictionary<string, JsonElement>>>(jsonSerializerOptions);
        xlsxFile.AppendData(list);
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
