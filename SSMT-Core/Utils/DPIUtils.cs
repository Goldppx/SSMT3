using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SSMT_Core
{
    public class DPIUtils
    {
        [DllImport("User32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("User32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("Gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int LOGPIXELSX = 88;

        public static double GetScale()
        {
            IntPtr hDC = GetDC(IntPtr.Zero);
            int dpi = GetDeviceCaps(hDC, LOGPIXELSX);
            ReleaseDC(IntPtr.Zero, hDC);

            return dpi / 96.0; // 96 DPI = 100%
        }
    }
}
