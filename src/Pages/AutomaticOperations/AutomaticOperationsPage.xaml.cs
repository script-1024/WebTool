﻿using System;
using System.IO;
using System.Web;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using WebTool.Lib;
using WebTool.Lib.IO;

namespace WebTool.Pages
{
    /// <summary>
    /// 自动操作页面
    /// </summary>
    public sealed partial class AutomaticOperationsPage : Page
    {
        public AutomaticOperationsPage()
        {
            this.InitializeComponent();

            // 订阅浏览器事件
            PrepareWebViewAsync();

            // 适用于 orderkeystone.com 的特定功能
            CustomizeFunctions_orderkeystone();

            // 应用程序事件
            App.ThemeChanged += App_ThemeChanged;
            App.MainWindow.Closing += Window_Closing;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            // 按键导航事件
            UriTextBox.KeyDown += UriTextBox_KeyDown;

            // 导航栏按钮
            GoBackButton.Click += (_, _) => WebView.GoBack();

            GoForwardButton.Click += (_, _) => WebView.GoForward();

            ReloadButton.Click += (_, _) => WebView.Reload();

            GoHomeButton.Click += (_, _) =>
            {
                try
                {
                    WebView.Source = new Uri(AppConfig.DefaultUri);
                }
                catch (Exception)
                {
                    WebView.Source = new Uri("about:blank");
                }
            };

            OpenPanelButton.Click += (_, _) =>
            {
                var visible = (bool)OpenPanelButton.IsChecked;
                AdvancedPanel.SetVisibility(visible);
                
                // 重置列宽度
                var colDef = RootGrid.ColumnDefinitions[1];
                if (!visible)
                {
                    colDef.Width = new();
                    colDef.MinWidth = 0;
                }
                else
                {
                    UpdateLayout();
                    colDef.MinWidth = AdvancedPanel.ActualWidth;
                }
            };
        }

        private async void PrepareWebViewAsync()
        {
            // 浏览器导航事件
            WebView.NavigationStarting += (_, _) => UpdateWebViewNavigationBar();
            WebView.NavigationCompleted += WebView_NavigationCompletedAsync;

            // 负责处理从 JavaScript 传回的消息
            // 必须先确保 CoreWebView2 已实例化
            await WebView.EnsureCoreWebView2Async();
            var coreWebView2 = WebView.CoreWebView2;

            coreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            coreWebView2.Settings.UserAgent =
               @$"Mozilla/5.0 (Windows NT 10.0; Win64; x64)
                  AppleWebKit/537.36 (KHTML, like Gecko)
                  Chrome/127.0.0.0 Safari/537.36 Edg/127.0.0.0 WebTool/{App.ShortVersion}";
        }

        private void CustomizeFunctions_orderkeystone()
        {
            Uri HOME_URI = new("https://portal.lkqcorp.com/login");
            WebView.Source = HOME_URI;

            SearchButton.Click += async (_, _) => await WebView.ExecuteScriptAsync($"OrderKeystone.search('{SearchBox.Text.Trim()}')");
            
            UseDefaultButton.Click += (_, _) =>
            {
                RDTextBox.Text = "400";
                EDTextBox.Text = "800";
                CDTextBox.Text = "400";
            };

            SkipButton.Click += async (_, _) => await WebView.ExecuteScriptAsync($"Runner.skip()");
            
            StopAllButton.Click += async (_, _) =>
            {
                if (xlsxFile is null) return;

                await WebView.ExecuteScriptAsync($"Runner.stopAll()");

                xlsxFile?.SaveAndClose();
                xlsxFile = null;

                ShowTip("操作提示", "文件已保存");

                // 更新状态
                ProgressDetailBar.IsIndeterminate = false;
                status = WorkingStatus.Ready;
                StartButton.Content = "開始";
            };
            
            ResumeButton.Click += async (_, _) =>
            {
                var stackPanel = new StackPanel() { Padding = new(16), MinWidth = 400 };
                var tip = new InfoBar()
                {
                    Content = "可由下方進度條得知抓取進度",
                    Severity = InfoBarSeverity.Informational,
                    IsOpen = true, IsClosable = false,
                    Margin = new(0, 0, 0, 24)
                };

                var horizontalGrid = new Grid() { Margin = new(16) };
                horizontalGrid.ColumnDefinitions.Add(new());
                horizontalGrid.ColumnDefinitions.Add(new());
                horizontalGrid.ColumnDefinitions.Add(new());

                var completedTextBox = new TextBox() { Header = "完成頁面", Margin = new(8), PlaceholderText = "0", Text = $"{completed}" };
                var fetchedTextBox = new TextBox() { Header = "已抓取項", Margin = new(8), PlaceholderText = "0", Text = $"{fetched}" };
                var selectFileButton = new Button()
                {
                    Content = "選擇檔案", Margin = new(8),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Style = (Style)Resources["AccentButtonStyle"]
                };

                horizontalGrid.Children.Add(completedTextBox);
                horizontalGrid.Children.Add(fetchedTextBox);
                horizontalGrid.Children.Add(selectFileButton);

                Grid.SetColumn(completedTextBox, 0);
                Grid.SetColumn(fetchedTextBox, 1);
                Grid.SetColumn(selectFileButton, 2);

                stackPanel.Children.Add(tip);
                stackPanel.Children.Add(horizontalGrid);

                var dialog = new ContentDialog()
                {
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary,
                    IsPrimaryButtonEnabled = false,
                    PrimaryButtonText = "開始執行",
                    CloseButtonText = "取消",
                    Title = "恢復上個工作階段",
                    Content = stackPanel
                };

                selectFileButton.Click += async (_, _) =>
                {
                    var picker = new FileOpenPicker();

                    // 取得当前窗口句柄，将选择器的拥有者设为此窗口
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, App.MainWindow.Hwnd);

                    // 选择器的预设路径
                    picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                    // 文件类型
                    picker.FileTypeFilter.Add(".xlsx");

                    // 选择文件
                    xlsxFile?.SaveAndClose();
                    StorageFile file = await picker.PickSingleFileAsync();
                    if (file != null)
                    {
                        dialog.IsPrimaryButtonEnabled = true;
                        xlsxFile = XlsxFile.Open(file.Path, "product_list");
                    }
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    if (!int.TryParse(RDTextBox.Text.Trim(), out int rd)) rd = 500;
                    if (!int.TryParse(EDTextBox.Text.Trim(), out int ed)) ed = 500;
                    if (!int.TryParse(CDTextBox.Text.Trim(), out int cd)) cd = 500;
                    if (!int.TryParse(completedTextBox.Text.Trim(), out int c)) c = 0;
                    if (!int.TryParse(fetchedTextBox.Text.Trim(), out int f)) f = 0;

                    StartButton.Content = "暫停";
                    status = WorkingStatus.Working;
                    await WebView.ExecuteScriptAsync($"Runner.runAsync({rd}, {ed}, {cd}, {c}, {f})");
                }
                else
                {
                    xlsxFile?.Close();
                    xlsxFile = null;
                }
            };

            StartButton.Click += async (_, _) =>
            {
                var reqParameters = new TipMessage() { Title = "網頁通知", Content = "未提供必要參數", IsLightDismiss = true };
                if (!int.TryParse(RDTextBox.Text.Trim(), out int rd)) { ShowTip(reqParameters); return; }
                if (!int.TryParse(EDTextBox.Text.Trim(), out int ed)) { ShowTip(reqParameters); return; }
                if (!int.TryParse(CDTextBox.Text.Trim(), out int cd)) { ShowTip(reqParameters); return; }

                switch (status)
                {
                    case WorkingStatus.Ready:
                        CreateNewFile();
                        StartButton.Content = "暫停";
                        status = WorkingStatus.Working;
                        await WebView.ExecuteScriptAsync($"Runner.runAsync({rd}, {ed}, {cd})");
                        break;

                    case WorkingStatus.Working:
                        await WebView.ExecuteScriptAsync($"Runner.stopAll()");
                        StartButton.Content = "繼續";
                        status = WorkingStatus.Terminated;
                        break;

                    case WorkingStatus.Terminated:
                        StartButton.Content = "暫停";
                        status = WorkingStatus.Working;
                        await WebView.ExecuteScriptAsync($"Runner.runAsync({rd}, {ed}, {cd}, {completed}, {fetched})");
                        break;
                }
            };
        }

        private void CreateNewFile()
        {
            int count = 0;
            string date = $"{DateTime.Now:yyyy-MM-dd}", name, path;

            do
            {
                // 检查重名文件
                name = $"{date}_#{count++}";
                path = @$"Downloads\{name}.xlsx";
            }
            while (File.Exists(path));

            // 保存旧文件
            xlsxFile?.SaveAndClose();

            // 创建新文件
            xlsxFile = XlsxFile.Create(path, "product_list");
        }

        #region NavigationBar
        private void UpdateWebViewNavigationBar()
        {
            (ReloadButton.Icon as FontIcon).Glyph = "\uE711"; // `Cancel` icon
            if (isUpdateDisabled) { isUpdateDisabled = false; return; }

            // 更新导航栏控件状态
            UriTextBox.Text = WebView.Source.AbsoluteUri;
            GoBackButton.IsEnabled = WebView.CanGoBack;
            GoForwardButton.IsEnabled = WebView.CanGoForward;
        }

        private void TryGoToUri(string userInput)
        {
            // 暫时禁止导航列更新，避免网址列瞬间闪回旧网址
            isUpdateDisabled = true;

            // 自动聚焦到浏览器，使输入框失去焦点
            WebView.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

            if (userInput == "about:blank")
            {
                WebView.Source = new("about:blank");
                return;
            }

            void fallBackSearchOnGoogle()
            {
                var param = HttpUtility.UrlEncode(UriTextBox.Text.Trim());
                WebView.Source = new Uri($"https://www.google.com/search?q={param}");
            }

            // 尝试浏览到目标网址
            // 若网址格式错误，将其视为关键字从谷歌搜索
            try
            {
                // 检查是否已包含 HTTP 协议头
                if (!userInput.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !userInput.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    userInput = "https://" + userInput;
                }

                if (HttpRegex().IsMatch(userInput)) WebView.Source = new Uri(userInput);
                else fallBackSearchOnGoogle();
            }
            catch (UriFormatException)
            {
                fallBackSearchOnGoogle();
            }
        }
        #endregion

        #region PrivateFields

        private bool isUpdateDisabled = false;

        [GeneratedRegex(@"^http(s?)://([a-zA-Z0-9-]+\.)+[a-zA-Z0-9-]+(/.*)?$")]
        private static partial Regex HttpRegex();

        private XlsxFile xlsxFile;

        private WorkingStatus status = WorkingStatus.Ready;

        private int completed = 0;

        private int fetched = 0;

        #endregion
    }
}
