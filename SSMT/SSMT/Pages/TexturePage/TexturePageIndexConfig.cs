using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSMT_Core;

namespace SSMT
{
    internal class TexturePageIndexConfig
    {

        public int DrawIBIndex { get; set; } = -1;
        public int ComponentIndex { get; set; } = -1;
        public int DrawCallIndex { get; set; } = -1;

        public void ReadConfig()
        {
            //读取配置时优先读取全局的
            if (File.Exists(GlobalConfig.Path_TexturePageIndexConfig))
            {
                JObject SettingsJsonObject = DBMTJsonUtils.ReadJObjectFromFile(GlobalConfig.Path_TexturePageIndexConfig);

                //古法读取
                if (SettingsJsonObject.ContainsKey("DrawIBIndex"))
                {
                    DrawIBIndex = (int)SettingsJsonObject["DrawIBIndex"];
                }

                if (SettingsJsonObject.ContainsKey("ComponentIndex"))
                {
                    ComponentIndex = (int)SettingsJsonObject["ComponentIndex"];
                }

                if (SettingsJsonObject.ContainsKey("DrawCallIndex"))
                {
                    DrawCallIndex = (int)SettingsJsonObject["DrawCallIndex"];
                }

            }

        }

        public void SaveConfig()
        {

            //古法保存
            JObject SettingsJsonObject = new JObject();

            SettingsJsonObject["DrawIBIndex"] = DrawIBIndex;
            SettingsJsonObject["ComponentIndex"] = ComponentIndex;
            SettingsJsonObject["DrawCallIndex"] = DrawCallIndex;

            DBMTJsonUtils.SaveJObjectToFile(SettingsJsonObject, GlobalConfig.Path_TexturePageIndexConfig);
        }

    }
}
