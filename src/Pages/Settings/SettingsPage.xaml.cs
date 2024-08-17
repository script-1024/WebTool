using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.IO;
using WebTool.Controls;

namespace WebTool.Pages;

/// <summary>
/// 设置页面
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();

        // 加载数据
        AppVersionTag.Description = App.FullVersion;
        ThemeColorComboBox.SelectedIndex = (int)App.ElementTheme;
        ThemeBackdropComboBox.SelectedIndex = (int)App.Backdrop;

        var configPath = Directory.GetCurrentDirectory() + @"\Config\";
        (OpenConfigClickableCard.Header as HeaderedContentControl).Description = configPath;
        OpenConfigClickableCard.Click += (_, _) => Process.Start(new ProcessStartInfo() { FileName="explorer", Arguments=$"\"{configPath}\"" });

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
