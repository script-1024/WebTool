using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WebTool.Pages;

/// <summary>
/// 设置页面
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
        ThemeColorComboBox.SelectedIndex = (int)App.ElementTheme;

        // 更改窗口主题
        ThemeColorComboBox.SelectionChanged += (_, _) =>
        {
            App.ElementTheme = (ElementTheme)ThemeColorComboBox.SelectedIndex;
        };

        ThemeBackdropComboBox.SelectionChanged += (_, _) =>
        {
            App.Backdrop = (App.AppBackdrop)ThemeBackdropComboBox.SelectedIndex;
        };
    }
}
