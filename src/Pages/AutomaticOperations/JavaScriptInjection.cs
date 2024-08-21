﻿using System;
using System.Text.Json;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using WebTool.Lib;

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

            function postMsg(type, data) {
                const msg = {Type: type, Data: data}
                window.chrome.webview?.postMessage(msg);
                const useWhiteList = dbgHelper.postMsg.useWhiteList;
                const inBlockList = (dbgHelper.postMsg.blockList.indexOf(type) > -1);
                if (!(useWhiteList ^ inBlockList)) console.log('PostMessage: ', msg);
            }

            document.addEventListener('mousemove', (e) => {
                postMsg('MouseEvent', {X: e.clientX, Y: e.clientY});
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
                var position = msg.Data.Deserialize<Point>(jsonSerializerOptions);
                UpdateMousePosition(position);
                break;

            case "ShowProgressBar":
            case "HideProgressBar":
                ProgressPanel.SetVisibility(msg.Type[0..4] == "Show");
                break;

            case "UpdateProgressBar":
                var info = msg.Data.Deserialize<ProgressInfo>(jsonSerializerOptions);
                ProgressCompletedLabel.Text = $"{info.Completed}";
                ProgressDetailLabel.Text = $"{info.Current}/{info.Total}";
                ProgressDetailBar.Value = info.Current / info.Total * 100;
                break;

            case "WriteToFile":
                if (xlsxFile != null) WriteToFile(msg.Data);
                xlsxFile?.Save();
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

    private struct ProgressInfo
    {
        public double Current { get; set; }
        public double Total { get; set; }
        public double Completed { get; set; }
    }

    private Point mousePosition = new(0, 0);

    #endregion
}
