using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageProcessor2.Helpers
{
    /// <summary>
    /// 工具类，用于处理图像相关的操作
    /// </summary>
    public static class Item
    {
        #region Win32 API
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr ptr);
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(
        IntPtr hdc, // handle to DC
        int nIndex // index of capability
        );
        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);
        #endregion

        #region DeviceCaps常量
        const int HORZRES = 8;
        const int VERTRES = 10;
        const int LOGPIXELSX = 88;
        const int LOGPIXELSY = 90;
        const int DESKTOPVERTRES = 117;
        const int DESKTOPHORZRES = 118;
        #endregion

        /// <summary>
        /// 获取真实设置的桌面分辨率大小
        /// </summary>
        public static Size DESKTOP
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                Size size = new Size();
                size.Width = GetDeviceCaps(hdc, DESKTOPHORZRES);
                size.Height = GetDeviceCaps(hdc, DESKTOPVERTRES);
                ReleaseDC(IntPtr.Zero, hdc);
                return size;
            }
        }

        /// <summary>
        /// 截图
        /// </summary>
        /// <returns></returns>
        public static Bitmap GetScreen()
        {
            Screen main = Screen.PrimaryScreen;//获取主显示屏
            var ScreenArea = DESKTOP;
            var ret = new Bitmap(ScreenArea.Width, ScreenArea.Height);
            using (Graphics g = Graphics.FromImage(ret))
            {
                g.CopyFromScreen(0, 0, 0, 0, new Size(ScreenArea.Width, ScreenArea.Height));
            }
            return ret;
        }

        /// <summary>
        /// 截图
        /// </summary>
        /// <param name="screen"></param>
        /// <returns></returns>
        public static Bitmap GetScreen(Screen screen)
        {
            var ScreenArea = screen.Bounds;
            var ret = new Bitmap(ScreenArea.Width % 2 == 0 ? ScreenArea.Width : ScreenArea.Width - 1, ScreenArea.Height % 2 == 0 ? ScreenArea.Height : ScreenArea.Height - 1);
            using (Graphics g = Graphics.FromImage(ret))
            {
                g.CopyFromScreen(ScreenArea.X, ScreenArea.Y, 0, 0, new Size(ScreenArea.Width, ScreenArea.Height));
            }
            return ret;
        }
    }
}
