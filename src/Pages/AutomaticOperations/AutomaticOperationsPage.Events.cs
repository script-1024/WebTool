﻿using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using Windows.System;
using Windows.Foundation;
using Windows.Win32.Core;

namespace WebTool.Pages;

public sealed partial class AutomaticOperationsPage
{
    private void App_ThemeChanged(ElementTheme theme)
    {
        // 更改浏览器偏好色彩
        WebView.CoreWebView2.Profile.PreferredColorScheme = (CoreWebView2PreferredColorScheme)theme;
    }

    private void Window_Closing(object sender, WindowClosingEventArgs e)
    {
        SaveAndCloseFile();

        App.MainWindow.Closing -= Window_Closing;
        e.TryCancel();
    }

    private void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        SaveAndCloseFile();
    }

    #region WebView

    /// <summary>
    /// 当鼠标在 WebView 内移动时发生事件
    /// </summary>
    private void WebView_MouseMove(Point newPosition)
    {
        mousePosition = newPosition;
    }

    private void CoreWebView2_SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
    {
        if (status != WorkingStatus.Ready && !WebView.Source.AbsoluteUri.StartsWith("https://preview.orderkeystone.com/"))
        {
            status = WorkingStatus.Ready;
            StartButton.Content = "開始";
            SaveAndCloseFile();
        }
    }

    #endregion

    #region UriTextBox

    private void UriTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Enter:
                TryGoToUri(UriTextBox.Text.Trim());
                break;
        }
    }

    #endregion

    #region Runner

    private void Runner_Start(int rd, int ed, int cd, int c = 0, int f = 0)
    {
        if (xlsxFile is null) CreateNewFile();
        StartButton.Content = "暫停";
        status = WorkingStatus.Working;
        WebView.ExecuteScriptAsync($"Runner.runAsync({rd}, {ed}, {cd}, {c}, {f})");
    }

    private void Runner_Pause()
    {
        StartButton.Content = "繼續";
        status = WorkingStatus.Terminated;
        ShowTip("網頁通知", "已停止抓取操作");
        WebView.ExecuteScriptAsync($"Runner.stopAll()");
    }

    #endregion
}
