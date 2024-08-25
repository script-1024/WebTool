using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using WebTool.Lib;

namespace WebTool.Pages;

public sealed partial class AutomaticOperationsPage
{
    private async void WebView_NavigationCompletedAsync(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        // 更新导航栏状态
        UpdateWebViewNavigationBar();
        (ReloadButton.Icon as FontIcon).Glyph = "\uE72C"; // `Reload` icon

        // 重置右侧面板状态
        ProgressDetailBar.IsIndeterminate = false;
        if (!WebView.Source.AbsoluteUri.StartsWith("https://preview.orderkeystone.com/")
            || StartButton.Content.ToString() != "繼續")
        {
            status = WorkingStatus.Ready;
            StartButton.Content = "開始";
            xlsxFile?.SaveAndClose();
            xlsxFile = null;
        }

        // 注入 JavaScript 代码
        // 包含一些实用函数，并检测鼠标移动
        string script = @"
            class WebTool {
                static #useWhiteList = false;
                static #blockList = new Set(['MouseEvent', 'ShowProgressBar', 'HideProgressBar', 'UpdateProgressBar']);

                static useWhiteList(value) {
                    if (typeof value === 'boolean') this.#useWhiteList = value;
                    else throw new TypeError('`WebTool.useWhiteList` must be a boolean value.');
                }

                static addToBlockList(typeName) {
                    if (typeof typeName === 'string') return this.#blockList.add(typeName), true;
                    else return false;
                }

                static removeFromBlockList(typeName) {
                    if (typeof typeName === 'string') return this.#blockList.delete(typeName);
                    else return false;
                }

                static postMsg(type, data = null) {
                    const msg = {Type: type, Data: data}
                    window.chrome.webview?.postMessage(msg);
                    const inBlockList = this.#blockList.has(type);
                    if (!(this.#useWhiteList ^ inBlockList)) console.log('Post: ', msg);
                }

                static showTip(title, content, isLightDismiss = true) {
                    this.postMsg('ShowTip', {
                        Title: title, Content: content,
                        IsLightDismiss: isLightDismiss
                    });
                }

                static showProgressBar = () => this.postMsg('ShowProgressBar');

                static hideProgressBar = () => this.postMsg('HideProgressBar');

                static updateProgressBar(current, total, completed, iconGlyph = '\uEBD3', isIndeterminate = false) {
                    this.postMsg('UpdateProgressBar', {
                        Current: current, Total: total, Completed: completed,
                        IconGlyph: iconGlyph, IsIndeterminate: isIndeterminate
                    });
                }
            }

            document.addEventListener('mousemove', (e) => {
                WebTool.postMsg('MouseEvent', {X: e.clientX, Y: e.clientY});
            });";

        await WebView.ExecuteScriptAsync(script);

        foreach (var file in AppConfig.UsedScripts)
        {
            var path = @$"Scripts\{file}";
            if (!File.Exists(path)) continue;
            script = File.ReadAllText(path);
            await WebView.ExecuteScriptAsync(script);
        }

        foreach (var objKvp in AppConfig.AdditionalData)
        {
            var objProperties = objKvp.Value;
            foreach (var propKvp in objProperties)
            {
                script = $"{objKvp.Key}.{propKvp.Key} = {propKvp.Value.GetRawText()};";
                await WebView.ExecuteScriptAsync(script);
            }
        }
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

            case "ShowTip":
                var tip = msg.Data.Deserialize<TipMessage>(jsonSerializerOptions);
                ShowTip(tip);
                break;

            case "ShowProgressBar":
            case "HideProgressBar":
                ProgressPanel.SetVisibility(msg.Type[0..4] == "Show");
                break;

            case "UpdateProgressBar":
                var info = msg.Data.Deserialize<ProgressInfo>(jsonSerializerOptions);
                ProgressDetailIcon.Glyph = info.IconGlyph;
                ProgressCompletedLabel.Text = $"{info.Completed}";
                ProgressDetailLabel.Text = $"{info.Current}/{info.Total}";

                // 设置外观类型是否为 `不确定`，再决定是否需要设置进度值
                if (!(ProgressDetailBar.IsIndeterminate = info.IsIndeterminate))
                {
                    this.completed = info.Completed;
                    this.fetched = info.Current;
                    ProgressDetailBar.Value = info.Current / (info.Total * 0.01);
                }
                break;

            case "WriteToFile":
                // 异步处理事件，不阻塞消息接收
                if (xlsxFile != null) Task.Run(() =>
                {
                    WriteToFile(msg.Data);
                    xlsxFile.Save();
                });
                break;

            case "Terminated":
                xlsxFile?.Save();
                break;

            case "Finished":
                status = WorkingStatus.Ready;
                StartButton.Content = "開始";
                xlsxFile?.SaveAndClose();
                xlsxFile = null;
                break;

            default:
                WebView.ExecuteScriptAsync($"console.warn('未知消息: {msg.Type}', {msg.Data.GetRawText()})");
                break;
        }
    }

    private void UpdateMousePosition(Point position)
    {
        // 更新 TextBlock 显示的鼠标位置
        CursorPositionTextBlock.Text = $"{position.X}, {position.Y}";
        if (position != mousePosition) WebView_MouseMove(position);
    }

    private void ShowTip(TipMessage tip) => ShowTip(tip.Title, tip.Content, tip.IsLightDismiss);
    
    private async void ShowTip(string title, string content, bool isLightDismiss = true)
    {
        int retries = 0, maxRetries = 30; // 容许 15 秒内关闭弹窗
        while (WebMsgTip.IsOpen && retries++ < maxRetries) await Task.Delay(500);
        if (retries >= maxRetries) return;

        WebMsgTip.Title = title;
        (WebMsgTip.Content as TextBlock).Text = content;
        WebMsgTip.IsLightDismissEnabled = isLightDismiss;
        WebMsgTip.IsOpen = true;

        if (!isLightDismiss) return;

        // 根据字数决定显示时长
        var delay = content.Length * 180;
        if (delay < 2700) delay = 2700;
        if (delay >= 18000) WebMsgTip.IsLightDismissEnabled = false;
        else
        {
            await Task.Delay(delay);
            WebMsgTip.IsOpen = false;
        }
    }

    private void WriteToFile(JsonElement jsonData)
    {
        switch (jsonData.ValueKind)
        {
            case JsonValueKind.Array:
                var list = jsonData.Deserialize<List<Dictionary<string, JsonElement>>>(jsonSerializerOptions);
                xlsxFile.AppendData(list);
                break;
            case JsonValueKind.Object:
                var dict = jsonData.Deserialize<Dictionary<string, JsonElement>>(jsonSerializerOptions);
                xlsxFile.AppendData(dict);
                break;
        }
    }

    #region PrivateFields

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    private Point mousePosition = new(0, 0);

    #endregion

    #region DataPackageType

    private struct ReceivedMessage
    {
        public string Type { get; set; }
        public JsonElement Data { get; set; }
    }

    private struct ProgressInfo
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public int Completed { get; set; }
        public string IconGlyph { get; set; }
        public bool IsIndeterminate { get; set; }
    }

    private struct TipMessage
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public bool IsLightDismiss { get; set; }
    }

    private enum WorkingStatus
    {
        Ready = 0,
        Working = 1,
        Terminated = 2
    }

    #endregion
}
