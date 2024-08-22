using System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.System;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
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
        App.MainWindow.Closing -= Window_Closing;
        xlsxFile?.SaveAndCloseAsync();
        xlsxFile = null;
        e.TryCancel();
    }

    #region "WebView"

    /// <summary>
    /// 当鼠标在 WebView 内移动时发生事件
    /// </summary>
    private void WebView_MouseMove(Point newPosition)
    {
        mousePosition = newPosition;
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
}
