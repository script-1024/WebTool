using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using HttpCrawler.Lib;

namespace HttpCrawler.Pages;

/// <summary>
/// 网络请求页面
/// </summary>
public sealed partial class HttpRequestPage : Page
{
    private readonly HttpClient httpClient = new();
    private bool hasRequestTask = false;

    public HttpRequestPage()
    {
        this.InitializeComponent();
        App.MainWindow.Closing += Window_Closing;
        RootPanel.Loaded += RootPanel_Loaded;
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
