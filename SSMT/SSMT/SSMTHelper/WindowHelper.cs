using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SSMT_Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;

namespace WinUI3Helper
{
    public class WindowHelper
    {
        public static void SetTheme(Window window, ElementTheme theme)
        {
            if (window.Content is FrameworkElement root)
            {
                root.RequestedTheme = theme;
            }
        }

    
        public static void SetSmartSizeAndMoveToCenter(AppWindow appWindow,
            double currentWidthDip, double currentHeightDip,
            double minRatio = 0.6, double maxRatio = 0.9)
        {
            // 获取当前缩放比例
            double scale = DPIUtils.GetScale();

            //获取屏幕大小
            var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest);
            int screenW = displayArea.OuterBounds.Width;
            int screenH = displayArea.OuterBounds.Height;

            //计算窗口允许的最小宽度和最小高度，以及最大的宽度和高度
            double minW = screenW * minRatio;
            double minH = screenH * minRatio;
            double maxW = screenW * maxRatio;
            double maxH = screenH * maxRatio;

            //如果当前窗口宽度高于最大值，则调整为最大值，小于最小值则调整为最小值
            double desiredW = currentWidthDip;
            if (desiredW < minW)
            {
                desiredW = minW;
            }

            if (desiredW > maxW)
            {
                desiredW = maxW;
            }

            double desiredH = currentHeightDip;
            if (desiredH < minH)
            {
                desiredH = minH;
            }
            if (desiredH > maxH)
            {
                desiredH = maxH;
            }

            // 最后四舍五入并应用
            ApplySize(appWindow, desiredW, desiredH, screenW, screenH);
        }

        private static void ApplySize(AppWindow appWindow, double w, double h, int screenW, int screenH)
        {
            int finalW = (int)Math.Round(w);
            int finalH = (int)Math.Round(h);

            appWindow.Resize(new SizeInt32(finalW, finalH));

            // 可选：居中
            int x = (screenW - finalW) / 2;
            int y = (screenH - finalH) / 2;
            appWindow.Move(new PointInt32(x, y));
        }

        public static void SetWindowSizeWithNavigationView(AppWindow appWindow,int Width,int Height)
        {
            appWindow.Resize(new SizeInt32(Width, Height));
        }

        public static void MoveWindowToCenter(AppWindow appWindow)
        {
            // 获取与窗口关联的DisplayArea
            var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest);
            // 获取窗口当前的尺寸
            var windowSize = appWindow.Size;

            // 确保我们获取的是正确的显示器信息
            if (displayArea != null)
            {
                // 计算窗口居中所需的左上角坐标，考虑显示器的实际工作区（排除任务栏等）
                int x = (int)(displayArea.WorkArea.X + (displayArea.WorkArea.Width - windowSize.Width) / 2);
                int y = (int)(displayArea.WorkArea.Y + (displayArea.WorkArea.Height - windowSize.Height) / 2);

                // 设置窗口位置
                appWindow.Move(new PointInt32 { X = x, Y = y });
            }

            int window_pos_x = 0;
            int window_pos_y = 0;

            if (displayArea != null)
            {
                window_pos_x = (int)(displayArea.WorkArea.X + (displayArea.WorkArea.Width - windowSize.Width) / 2);
                window_pos_y = (int)(displayArea.WorkArea.Y + (displayArea.WorkArea.Height - windowSize.Height) / 2);
            }

            if (window_pos_x != -1 && window_pos_y != -1)
            {
                appWindow.Move(new PointInt32(window_pos_x, window_pos_y));
            }
        }
    }
}
