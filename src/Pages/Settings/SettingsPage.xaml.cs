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
        SC0_ThemeColorComboBox.SelectedIndex = (int)App.ElementTheme;

        // 更改窗口主题
        SC0_ThemeColorComboBox.SelectionChanged += (_, _) =>
        {
            App.ElementTheme = (ElementTheme)SC0_ThemeColorComboBox.SelectedIndex;
        };

        SC1_ThemeBackdropComboBox.SelectionChanged += (_, _) =>
        {
            switch (SC1_ThemeBackdropComboBox.SelectedIndex)
            {
                case 0:
                    App.MainWindow.SystemBackdrop = App.MicaBackdrop;
                    break;
                case 1:
                    App.MainWindow.SystemBackdrop = App.MicaAltBackdrop;
                    break;
                case 2:
                    App.MainWindow.SystemBackdrop = App.AcrylicBackdrop;
                    break;
            }
        };
    }
}
