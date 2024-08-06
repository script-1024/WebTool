using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace HttpCrawler.Pages;

/// <summary>
/// 网络请求页面
/// </summary>
public sealed partial class HttpRequestPage : Page
{
    public HttpRequestPage()
    {
        this.InitializeComponent();

        RootPanel.Loaded += RootPanel_Loaded;
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

    private void RequestButton_Click(object sender, RoutedEventArgs e)
    {

    }
}
