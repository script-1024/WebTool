using System.IO;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Controls;
using WebTool.Controls;
using WebTool.Lib;

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
        OpenConfigClickableCard.Click += (_, _) => Process.Start(new ProcessStartInfo() { FileName="explorer", Arguments=$"\"{configPath}\"" });

        // 更改窗口主题
        ThemeColorComboBox.SelectionChanged += (_, _) =>
        {
            App.ElementTheme = (ElementTheme)ThemeColorComboBox.SelectedIndex;
        };

        // 更改背景材质
        ThemeBackdropComboBox.SelectionChanged += (_, _) =>
        {
            App.Backdrop = (App.AppBackdrop)ThemeBackdropComboBox.SelectedIndex;
        };

        // 更新配置文件时刷新列表
        AppConfig.Reloaded += ConfigReloaded;

        // 点击按钮手动刷新列表
        ReloadConfigButton.Click += (_, _) =>
        {
            // 加载时间较长时可见到 ProgressRing
            LoadingProgressRing.SetVisibility(true);
            AppConfig.Reload();
        };

        // 立刻更新一次配置文件列表
        // 应用启动时的首次更新先于设置页初始化
        // 因此更新不到设置页的列表
        ConfigReloaded();
    }


    private bool eventIgnored = false;
    private void ConfigReloaded()
    {
        // 防止重复呼叫
        if (eventIgnored)
        {
            LoadingProgressRing.SetVisibility(false);
            return;
        }

        // 清空卡片
        while (ConfigPanel.Children.Count > 1) ConfigPanel.Children.RemoveAt(1);

        // 产生一个新的卡片控件
        SettingsCard CreateNewConfigCard(string filename, bool isConfigEnable = true)
        {
            var info = new FileInfo(filename);

            var headerControl = new HeaderedContentControl()
            {
                Margin = new(20, 0, 20, 0),
                Header = info.Name.Replace(info.Extension, ""),
                Description = info.FullName
            };

            var enableSwitch = new ToggleSwitch() { OffContent = "禁用", OnContent = "啟用", IsOn = isConfigEnable };

            enableSwitch.Toggled += (_, _) =>
            {
                if (enableSwitch.IsOn) AppConfig.BlockedFiles.Remove(filename);
                else if (!AppConfig.BlockedFiles.Contains(filename)) AppConfig.BlockedFiles.Add(filename);
                
                eventIgnored = true;
                LoadingProgressRing.SetVisibility(true);

                AppConfig.Reload();
                eventIgnored = false;
            };

            return new SettingsCard()
            {
                Style = (Style)Resources["ExpanderContentCard"],
                Header = headerControl,
                Content = enableSwitch
            };
        }

        foreach (var file in AppConfig.LoadedFiles)
        {
            var isBlocked = AppConfig.BlockedFiles.Contains(file);
            var card = CreateNewConfigCard(file, !isBlocked);
            ConfigPanel.Children.Add(card);
        }

        LoadingProgressRing.SetVisibility(false);
    }
}
