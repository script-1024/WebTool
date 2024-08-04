using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HttpCrawler.Pages;

/// <summary>
/// 设置页面
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
        SC0_ThemeComboBox.SelectedIndex = (int)App.ElementTheme;

        // 更改窗口主题
        SC0_ThemeComboBox.SelectionChanged += (_, _) =>
        {
            App.ElementTheme = (ElementTheme)SC0_ThemeComboBox.SelectedIndex;
        };
    }
}
