using System;
using System.Web;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Win32.Core;
using HttpCrawler.Lib;

namespace HttpCrawler.Pages;

/// <summary>
/// 网络请求页面
/// </summary>
public sealed partial class HttpRequestPage : Page
{
    private readonly HttpClient httpClient = new();
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
        // 操作 `ArgsPanel` 的属性必须在对象实例化后再进行，避免 NRE
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
        var uri = UriTextBox.Text.Trim();
        var args = RequestArgsTextBox.Text.Trim();

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
    }

    private async Task GetAsync(string uri)
    {
        // 对话框
        var dialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            CloseButtonText = "確認",
            Title = "網頁回應"
        };

        try
        {
            // 访问失败时会抛出异常
            HttpRequestMessage msg = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri)
            };

            var response = await httpClient.SendAsync(msg);
            var responseBody = await response.Content.ReadAsStringAsync();
            dialog.Content = responseBody;
        }
        catch (Exception e)
        {
            dialog.Content =
                $"存取指定 Uri 時發生錯誤\n{e.Message}\n" +
                "\n== 呼叫堆疊 ==\n" +
                $"{e.StackTrace}";
        }

        RequestProgressRing.IsActive = false;
        await dialog.ShowAsync();
    }

    private async Task PostAsync(string uri, string contentBody)
    {
        // 对话框
        var dialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            CloseButtonText = "確認",
            Title = "網頁回應"
        };

        if (!Helpers.IsJsonStringValid(contentBody))
        {
            dialog.Content = $"預期一個JSON物件，但收到\n`{contentBody}`";
            RequestProgressRing.IsActive = false;
            await dialog.ShowAsync();
            return;
        }

        try
        {
            // 访问失败时会抛出异常
            HttpRequestMessage msg = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(uri),
                Content = new StringContent(contentBody)
            };

            var response = await httpClient.SendAsync(msg);
            var responseBody = await response.Content.ReadAsStringAsync();
            dialog.Content = responseBody;
        }
        catch (Exception e)
        {
            dialog.Content =
                $"存取指定 Uri 時發生錯誤\n{e.Message}\n" +
                "\n== 呼叫堆疊 ==\n" +
                $"{e.StackTrace}";
        }

        RequestProgressRing.IsActive = false;
        await dialog.ShowAsync();
    }
}
