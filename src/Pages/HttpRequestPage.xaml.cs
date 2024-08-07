using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32.Core;
using HttpCrawler.Lib;

namespace HttpCrawler.Pages;

/// <summary>
/// 网络请求页面
/// </summary>
public sealed partial class HttpRequestPage : Page
{
    private readonly HttpClient httpClient = new();
    private bool hasRequestTask = false;
    private DesktopWindow dwInstance;

    public HttpRequestPage()
    {
        this.InitializeComponent();
        RootPanel.Loaded += RootPanel_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (dwInstance is null)
        {
            dwInstance = e.Parameter as DesktopWindow;
            dwInstance.Closing += DwInstance_Closing;
        }
    }

    private void DwInstance_Closing(object sender, WindowClosingEventArgs e)
    {
        dwInstance.Closing -= DwInstance_Closing;
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

    private async Task GetAsync(string uri)
    {
        HttpRequestMessage msg;

        try
        {
            msg = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri)
            };
        }
        catch (UriFormatException)
        {
            // URI 对象建立失败
            RequestProgressRing.IsActive = false;
            Tip.Title = "無效請求";
            Tip.Content = "指定 URI 不可用";
            Tip.IsOpen = true;
            return;
        }

        if (msg is not null) await SendMsgAsync(msg);
    }

    private async Task PostAsync(string uri, string contentBody)
    {
        if (!JsonHelper.IsJsonStringValid(contentBody))
        {
            RequestProgressRing.IsActive = false;
            Tip.Title = "無效請求";
            Tip.Content = "預期一個 JSON 物件，但收到錯誤格式";
            Tip.IsOpen = true;
            return;
        }

        HttpRequestMessage msg;

        try
        {
            msg = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(uri),
                Content = new StringContent(contentBody)
            };
        }
        catch (UriFormatException)
        {
            // URI 对象建立失败
            RequestProgressRing.IsActive = false;
            Tip.Title = "無效請求";
            Tip.Content = "指定 URI 不可用";
            Tip.IsOpen = true;
            return;
        }

        if (msg is not null) await SendMsgAsync(msg);
    }

    private async Task SendMsgAsync(HttpRequestMessage msg)
    {
        // 对话框
        var dialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "複製",
            CloseButtonText = "關閉",
            Title = "網頁回應"
        };

        string responseBody = string.Empty;
        try
        {
            var response = await httpClient.SendAsync(msg);
            responseBody = await response.Content.ReadAsStringAsync();

            // 使用 ScrollViewer 显示过长文本
            dialog.Content = new ScrollViewer()
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new TextBlock() { Text = responseBody }
            };
        }
        catch (Exception e)
        {
            dialog.Content =
                $"存取指定 URI 時發生錯誤\n{e.Message}\n" +
                "\n== 呼叫堆疊 ==\n" +
                $"{e.StackTrace}";
        }

        RequestProgressRing.IsActive = false;
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var dtPack = new DataPackage();
            dtPack.SetText(responseBody);
            Clipboard.SetContent(dtPack);
            Tip.Title = "操作完成";
            Tip.Content = "已複製至剪貼板";
            Tip.IsOpen = true;
        }
    }
}
