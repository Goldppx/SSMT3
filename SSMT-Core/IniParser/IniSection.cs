using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMT
{
    public class IniSection
    {
        /// <summary>
        /// SectionName 不包含中括号
        /// </summary>
        public string SectionName { get; set; } = "";

        /// <summary>
        /// 每一行SectionLine，这里的是分大小写的，使用时要自己转换小写
        /// </summary>
        public List<string> SectionLineList = new List<string>();

    }
}
