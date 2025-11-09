using Microsoft.UI.Xaml.Controls.Primitives;
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
    public class ModManageConfig
    {
        public string CategoryPrimaryName { get; set; } = "";
        public string CategorySecondaryName { get; set; } = "";
        public string ModItemName { get; set; } = "";

        public void ReadConfig() {
            //读取配置时优先读取全局的

            if (File.Exists(PathManager.Path_ModManageConfig))
            {
                JObject SettingsJsonObject = DBMTJsonUtils.ReadJObjectFromFile(PathManager.Path_ModManageConfig);

                //古法读取
                if (SettingsJsonObject.ContainsKey("CategoryPrimaryName"))
                {
                    CategoryPrimaryName = (string)SettingsJsonObject["CategoryPrimaryName"];
                }

                if (SettingsJsonObject.ContainsKey("CategorySecondaryName"))
                {
                    CategorySecondaryName = (string)SettingsJsonObject["CategorySecondaryName"];
                }

                if (SettingsJsonObject.ContainsKey("ModItemName"))
                {
                    ModItemName = (string)SettingsJsonObject["ModItemName"];
                }

            }

        }

        public void SaveConfig() {

            //古法保存
            JObject SettingsJsonObject = new JObject();

            SettingsJsonObject["CategoryPrimaryName"] = CategoryPrimaryName;
            SettingsJsonObject["CategorySecondaryName"] = CategorySecondaryName;
            SettingsJsonObject["ModItemName"] = ModItemName;

            DBMTJsonUtils.SaveJObjectToFile(SettingsJsonObject, PathManager.Path_ModManageConfig);
            
        }

    }
}
