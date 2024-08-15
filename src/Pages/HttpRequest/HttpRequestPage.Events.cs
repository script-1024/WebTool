using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Win32.Core;

namespace WebTool.Pages;

public sealed partial class HttpRequestPage
{
    private void Window_Closing(object sender, WindowClosingEventArgs e)
    {
        App.MainWindow.Closing -= Window_Closing;
        httpClient.Dispose(); // 销毁对象释放内存
        e.TryCancel();
    }

    private void RootPanel_Loaded(object sender, RoutedEventArgs e)
    {
        // 操作属性必须在对象实例化后再进行，避免 NRE
        ModeSwitcher.SelectionChanged += ModeSwitcher_SelectionChanged;
    }

    private void ModeSwitcher_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ArgsPanel.Visibility = ModeSwitcher.SelectedIndex switch
        {
            1 => Visibility.Visible,
            _ => Visibility.Collapsed
        };
    }

    private async void RequestButton_Click(object sender, RoutedEventArgs e)
    {
        // 防止重复呼叫
        if (hasRequestTask) return;
        else hasRequestTask = true;

        var uri = UriTextBox.Text.Trim();
        var args = RequestArgsTextBox.Text.Trim();

        if (CorrectFormatTipPanel.Visibility == Visibility.Collapsed)
        {
            // URI 未通过正则检查
            Tip.Title = "無效請求";
            Tip.Content = "指定的 URI 內容為空或格式錯誤";
            Tip.IsOpen = true;
            hasRequestTask = false;
            return;
        }
        else if (ModeSwitcher.SelectedIndex == 1 && args == "")
        {
            // 请求参数为空
            Tip.Title = "無效請求";
            Tip.Content = "未提供請求參數";
            Tip.IsOpen = true;
            hasRequestTask = false;
            return;
        }

        // 开始转圈展示等待动画
        RequestProgressRing.IsActive = true;

        switch (ModeSwitcher.SelectedIndex)
        {
            // GET
            case 0:
                await GetAsync(uri);
                break;

            // POST
            case 1:
                await PostAsync(uri, args);
                break;
        }

        UriTextBox.Text = uri;
        RequestArgsTextBox.Text = args;
        hasRequestTask = false;
    }
}
