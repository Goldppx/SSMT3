using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMT_Core.IniParser
{
    public class IniResource
    {
        public string ResourceName { get; set; } = "";
        public string Type { get; set; } = "";
        public string Stride { get; set; } = "";
        public string Format { get; set; } = "";
        public string FileName { get; set; } = "";

        //根据ini文件所在位置和FileName拼接出来的属性
        public string FilePath { get; set; } = "";



    }
}
