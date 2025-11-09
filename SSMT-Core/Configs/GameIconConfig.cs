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
    public class GameIconSetting
    {
        public string GameName { get; set; } = "";
        public bool Show { get; set; } = false;

        public GameIconSetting(string gameName, bool show)
        {
            GameName = gameName;
            Show = show;
        }


    }

    public class GameIconConfig
    {
        List<GameIconSetting> GameIconSettingList = new List<GameIconSetting>();
        public Dictionary<string, bool> GameName_Show_Dict = new Dictionary<string, bool>();

        public GameIconConfig()
        {
            //读取并设置当前3Dmigoto路径
            if (File.Exists(PathManager.Path_GamesIconConfigJson))
            {
                JObject jobj = DBMTJsonUtils.ReadJObjectFromFile(PathManager.Path_GamesIconConfigJson);

                //LaunchItems
                if (jobj.ContainsKey("GameIconSettingList"))
                {
                    JArray jobjArray = jobj["GameIconSettingList"] as JArray ?? new JArray();

                    if (jobjArray != null && jobjArray.Count > 0)
                    {
                        this.GameIconSettingList.Clear();

                        foreach (JObject launchItemJobj in jobjArray)
                        {
                            string GameName = launchItemJobj["GameName"]?.ToString() ?? "";
                            bool Show = (bool)launchItemJobj["Show"];
                            GameIconSetting newLaunchItem = new GameIconSetting(GameName, Show);
                            this.GameIconSettingList.Add(newLaunchItem);
                            this.GameName_Show_Dict[GameName] = Show;
                        }

                    }
                }

            }
        }

        public void SaveConfig()
        {
            JArray jobjArray = new JArray();

            foreach (var item in this.GameName_Show_Dict)
            {
                string gamename = item.Key;
                bool show = item.Value;

                JObject launchItemJObj = DBMTJsonUtils.CreateJObject();
                launchItemJObj["GameName"] = gamename;
                launchItemJObj["Show"] = show;
                jobjArray.Add(launchItemJObj);
            }

            JObject jobj = DBMTJsonUtils.CreateJObject();

            if (File.Exists(PathManager.Path_GamesIconConfigJson))
            {
                jobj = DBMTJsonUtils.ReadJObjectFromFile(PathManager.Path_GamesIconConfigJson);
            }

            jobj["GameIconSettingList"] = jobjArray;
            DBMTJsonUtils.SaveJObjectToFile(jobj, PathManager.Path_GamesIconConfigJson);
        }


    }


}
