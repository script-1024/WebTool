using HttpCrawler.Core;
using Microsoft.UI.Xaml;

namespace HttpCrawler
{
    /// <summary>
    /// 应用的主要窗口
    /// </summary>
    public sealed partial class MainWindow : DesktopWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // 设置窗口最小尺寸
            this.MinHeight = 600;
            this.MinWidth = 800;
        }
    }
}
