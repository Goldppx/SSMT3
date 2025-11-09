using Microsoft.UI.Xaml.Controls;
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
        private void ReadD3D11ElementListFromFile(string FilePath)
        {
            D3D11GameType d3D11GameType = new D3D11GameType(FilePath);

            D3D11ElementList.Clear();
            foreach (D3D11Element d3D11Element in d3D11GameType.D3D11ElementList)
            {
                D3D11ElementList.Add(d3D11Element);
            }
        }

        private void ComboBox_GameTypeNameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoading)
            {
                return;
            }

            string GameTypeName = ComboBox_GameTypeNameList.SelectedItem.ToString();
            
            string GameTypeFilePath = Path.Combine(PathManager.Path_CurrentGame_GameTypeFolder, GameTypeName + ".json");
            if (File.Exists(GameTypeFilePath))
            {
                ReadD3D11ElementListFromFile(GameTypeFilePath);
            }
            else
            {
                _ = SSMTMessageHelper.Show("选中的数据类型文件不存在");
            }

        }

    }
}
