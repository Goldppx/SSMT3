using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSMT_Core;

namespace SSMT
{
    public partial class GameTypePage
    {
        private void InitializeGameTypeFolder()
        {
            if (!Directory.Exists(GlobalConfig.Path_GameTypeConfigsFolder))
            {
                Directory.CreateDirectory(GlobalConfig.Path_GameTypeConfigsFolder);
            }

            //在切换到数据类型页面时，如果对应游戏的数据类型文件夹不存在则创建
            List<string> GameNameList = SSMTResourceUtils.GetGameNameList();

            foreach (string FolderName in GameNameList)
            {
                string GameTypeFolderPath = Path.Combine(GlobalConfig.Path_GameTypeConfigsFolder, FolderName);
                if (!Directory.Exists(GameTypeFolderPath))
                {
                    Directory.CreateDirectory(GameTypeFolderPath);
                }
            }
        }

        private void InitializeComboBoxGameTypeNameList()
        {
            IsLoading = true;

            ComboBox_GameTypeNameList.Items.Clear();

            if (Directory.Exists(GlobalConfig.Path_CurrentGame_GameTypeFolder))
            {
                string[] GameTypeJsonFilePathList = Directory.GetFiles(GlobalConfig.Path_CurrentGame_GameTypeFolder);

                foreach (string GameTypeJsonFilePath in GameTypeJsonFilePathList)
                {
                    string FileName = Path.GetFileNameWithoutExtension(GameTypeJsonFilePath);
                    ComboBox_GameTypeNameList.Items.Add(FileName);
                }
            }

            IsLoading = false;
        }




    }
}
