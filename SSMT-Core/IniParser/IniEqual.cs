using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMT
{
    public class IniEqual
    {
        public string LeftValue { get; set; } = "";
        public string RightValue { get; set; } = "";

        public string LeftValueTrim { get; set; } = "";
        public string RightValueTrim { get; set; } = "";

        public IniEqual() { }
        public IniEqual(string IniLine)
        {
            if (string.IsNullOrEmpty(IniLine))
            {
                return;
            }

            // 查找第一个等号的位置
            int equalSignIndex = IniLine.IndexOf('=');

            if (equalSignIndex < 0)
            {
                // 如果没有找到等号，将整行作为 LeftValue
                LeftValue = IniLine;
                LeftValueTrim = IniLine.Trim();
                RightValue = "";
                RightValueTrim = "";
            }
            else
            {
                // 截取等号左边的部分
                LeftValue = IniLine.Substring(0, equalSignIndex);
                LeftValueTrim = LeftValue.Trim();

                // 截取等号右边的部分（从等号位置+1开始到字符串末尾）
                if (equalSignIndex + 1 < IniLine.Length)
                {
                    RightValue = IniLine.Substring(equalSignIndex + 1);
                    RightValueTrim = RightValue.Trim();
                }
                else
                {
                    // 等号在字符串末尾的情况
                    RightValue = "";
                    RightValueTrim = "";
                }
            }
        }
    }
}
