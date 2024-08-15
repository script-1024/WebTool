using System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.System;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace WebTool.Pages;

public sealed partial class AutomaticOperationsPage
{
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
