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

    public class DrawIBPair
    {
        public string DrawIB { get; set; } = "";
        public string Alias { get; set; } = "";

    }

    public class WorkSpaceHelper
    {

        public static List<DrawIBPair> GetDrawIBPairListFromWorkSpaceConfig()
        {
            List<DrawIBPair> drawIBListValues = new List<DrawIBPair>();

            string Configpath = Path.Combine(PathManager.Path_CurrentWorkSpaceFolder, "Config.json");
            if (File.Exists(Configpath))
            {
                //切换到对应配置
                string jsonData = File.ReadAllText(Configpath);
                JArray DrawIBListJArray = JArray.Parse(jsonData);

                foreach (JObject jkobj in DrawIBListJArray)
                {
                    string DrawIB = (string)jkobj["DrawIB"];
                    string Alias = (string)jkobj["Alias"];
                    DrawIBPair drawIBPair = new DrawIBPair()
                    {
                        DrawIB = DrawIB,
                        Alias = Alias
                    };
                    drawIBListValues.Add(drawIBPair);
                }
            }

            return drawIBListValues;
        }

    }
}
