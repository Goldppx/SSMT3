using CommunityToolkit.WinUI.UI.Controls;
using WinUI3Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using SSMT_Core;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SSMT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GameTypePage : Page
    {
        private ObservableCollection<D3D11Element> D3D11ElementList = new ObservableCollection<D3D11Element>();

        private bool IsLoading = false;

        public GameTypePage()
        {
            this.InitializeComponent();

            InitializeD3D11ElementDataGrid();
            InitializeGameTypeFolder();

            ComboBox_SemanticName.SelectedIndex = 0;
            ComboBox_Format.SelectedIndex = 0;
            ComboBox_ExtractSlot.SelectedIndex = 0;
            ComboBox_ExtractTechnique.SelectedIndex = 0;
            ComboBox_Category.SelectedIndex = 0;
            ComboBox_DrawCategory.SelectedIndex = 0;

            InitializeComboBoxGameTypeNameList();
        }




        private void InitializeD3D11ElementDataGrid()
        {
            //设置数据源
            DataGrid_GameType.ItemsSource = D3D11ElementList;

            //添加空行确保用户可编辑
            AddBlankD3D11ElementLine();
        }

        public void AddBlankD3D11ElementLine()
        {
            D3D11Element d3D11Element = new D3D11Element();
            D3D11ElementList.Add(d3D11Element);
        }

        private void DataGrid_GameType_CellEditEnding(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridCellEditEndingEventArgs e)
        {

            if (D3D11ElementList.Last().SemanticName != "")
            {
                AddBlankD3D11ElementLine();
            }

            CalculateAndShowTotalStride();
         
        }

        private void CalculateAndShowTotalStride()
        {

            Dictionary<string,int> CategoryStrideDict = new Dictionary<string,int>();


            try
            {

                foreach (D3D11Element d3D11Element in D3D11ElementList)
                {
                    if (d3D11Element.SemanticName.Trim() == "")
                    {
                        continue;
                    }

                    if (CategoryStrideDict.ContainsKey(d3D11Element.Category))
                    {
                        CategoryStrideDict[d3D11Element.Category] = CategoryStrideDict[d3D11Element.Category] + int.Parse(d3D11Element.ByteWidth);
                    }
                    else
                    {
                        CategoryStrideDict[d3D11Element.Category] = int.Parse(d3D11Element.ByteWidth);

                    }

                }

                string StrideStr = "";
                foreach (var categorypair in CategoryStrideDict)
                {

                    StrideStr = StrideStr + categorypair.Key + " : " + categorypair.Value + "  ";
                }


                TextBlock_TotalStride.Text = StrideStr;
            }
            catch (Exception ex) {
                ex.ToString();
            }
           
        }


        private void Menu_ClearD3D11ElementList_Click(object sender, RoutedEventArgs e)
        {
            D3D11ElementList.Clear();
            AddBlankD3D11ElementLine();
        }

        private void Menu_OpenGameTypeFolder_Click(object sender, RoutedEventArgs e)
        {

            string CurrentGameTypeFolder = Path.Combine(GlobalConfig.Path_GameTypeConfigsFolder,GlobalConfig.CurrentGameName + "\\");
            SSMTCommandHelper.ShellOpenFolder(CurrentGameTypeFolder);
        }

        private D3D11Element GetCurrentFilledD3D11Element()
        {
            D3D11Element d3D11Element = new D3D11Element();
            d3D11Element.SemanticName = ComboBox_SemanticName.Text;
            d3D11Element.Format = ComboBox_Format.Text;
            d3D11Element.ExtractSlot = ComboBox_ExtractSlot.Text;
            d3D11Element.ExtractTechnique = ComboBox_ExtractTechnique.Text;
            d3D11Element.Category = ComboBox_Category.Text;
            d3D11Element.DrawCategory = ComboBox_DrawCategory.Text;

            d3D11Element.ByteWidth = NumberBox_ByteWidth.Value.ToString();
            return d3D11Element;
        }

        private void AddNewD3D11ElementLine()
        {
            D3D11Element d3D11Element = GetCurrentFilledD3D11Element();
          
            D3D11ElementList.Add(d3D11Element);

            CalculateAndShowTotalStride();
        }

        private void Button_AddNewD3D11ElementLine_Click(object sender, RoutedEventArgs e)
        {
            if (D3D11ElementList.Count == 1)
            {
                if (D3D11ElementList[0].SemanticName == "")
                {
                    D3D11ElementList.Clear();
                }
                AddNewD3D11ElementLine();

            }
            else
            {
                AddNewD3D11ElementLine();
            }
        }

        private void Menu_DeleteD3D11ElementLine_Click(object sender, RoutedEventArgs e)
        {
            int index = DataGrid_GameType.SelectedIndex;
            if (index >= 0)
            {
                if (D3D11ElementList.Count >= 2)
                {
                    D3D11ElementList.RemoveAt(index);
                }
                else
                {
                    D3D11ElementList.Clear();
                    AddBlankD3D11ElementLine();
                }
            }
            else
            {
                _ = SSMTMessageHelper.Show("请先至少鼠标左键单机选中一个行");
            }

        }

        private void Menu_SaveD3D11ElementList_Click(object sender, RoutedEventArgs e)
        {
            JObject jObject = DBMTJsonUtils.CreateJObject();

            List<D3D11Element> elementList = D3D11ElementList.ToList();

            //移除其中SemanticName为空的项目
            List<D3D11Element> FilterElementList = [];
            foreach(D3D11Element d3D11Element in elementList)
            {
                if (d3D11Element.SemanticName.Trim() != "")
                {
                    FilterElementList.Add(d3D11Element);
                }
            }
            elementList = FilterElementList;

            // 将 List 转换为 JArray
            JArray jArray = new JArray();
            foreach (D3D11Element element in elementList)
            {
                // 假设 D3D11Element 可以直接转换为 JObject，或者你需要手动构造 JObject
                jArray.Add(JObject.FromObject(element));
            }

            // 将 JArray 赋值给 JObject 的属性
            jObject["D3D11ElementList"] = jArray;

            //组装名称
            string GameTypeName = "";

            int TexcoordNumber = 0;
            bool GPUPreSkinning = false;
            foreach (D3D11Element element in elementList)
            {
                if (element.SemanticName == "POSITION")
                {
                    GameTypeName = GameTypeName + "P";
                }else if (element.SemanticName == "NORMAL")
                {
                    GameTypeName = GameTypeName + "N";
                }
                else if (element.SemanticName == "TANGENT")
                {
                    GameTypeName = GameTypeName + "TA";
                }
                else if (element.SemanticName == "BINORMAL")
                {
                    GameTypeName = GameTypeName + "BN";
                }
                else if (element.SemanticName == "COLOR")
                {
                    GameTypeName = GameTypeName + "C";
                }
                else if (element.SemanticName == "BLENDWEIGHTS" || element.SemanticName == "BLENDWEIGHT")
                {
                    GameTypeName = GameTypeName + "BW";
                }
                else if (element.SemanticName == "BLENDINDICES")
                {
                    GPUPreSkinning = true;
                    GameTypeName = GameTypeName + "BI";
                }
                else if (element.SemanticName == "BLENDWEIGHTSEXT" || element.SemanticName == "BLENDWEIGHTEXT")
                {
                    GameTypeName = GameTypeName + "BWEXT";
                }
                else if (element.SemanticName == "BLENDINDICESEXT")
                {
                    GameTypeName = GameTypeName + "BIEXT";
                }
                else if (element.SemanticName == "TEXCOORD")
                {
                    GameTypeName = GameTypeName + "T";
                    if (TexcoordNumber != 0)
                    {
                        GameTypeName = GameTypeName + TexcoordNumber.ToString() + "-";
                    }

                    TexcoordNumber += 1;
                }
                
                int FormatByteWidth = element.ByteWidthInt;
                GameTypeName = GameTypeName + FormatByteWidth.ToString();
                GameTypeName = GameTypeName + "_";
            }


            if (GPUPreSkinning)
            {
                GameTypeName = "GPU_" + GameTypeName;
            }
            else
            {
                GameTypeName = "CPU_" + GameTypeName;
            }
            string GameTypeFilePath = Path.Combine(GlobalConfig.Path_CurrentGame_GameTypeFolder, GameTypeName + ".json");
            DBMTJsonUtils.SaveJObjectToFile(jObject, GameTypeFilePath);

            SSMTCommandHelper.ShellOpenFolder(GlobalConfig.Path_CurrentGame_GameTypeFolder);
        }



        private async void Menu_OpenGameTypeFile_Click(object sender, RoutedEventArgs e)
        {

            string FilePath = await SSMTCommandHelper.ChooseFileAndGetPath(".json");
            D3D11GameType d3D11GameType = new D3D11GameType(FilePath);

            D3D11ElementList.Clear();
            foreach (D3D11Element d3D11Element in d3D11GameType.D3D11ElementList)
            {
                D3D11ElementList.Add(d3D11Element);
            }
        }

        private void Button_RecalculateTotalStride_Click(object sender, RoutedEventArgs e)
        {
            CalculateAndShowTotalStride();


            List<D3D11Element> NewD3D11ElementList = new List<D3D11Element>();
            foreach (D3D11Element d3D11Element in D3D11ElementList)
            {
                NewD3D11ElementList.Add(d3D11Element);
            }
            DataGrid_GameType.ItemsSource = new List<D3D11Element>();

            D3D11ElementList.Clear();
            foreach (D3D11Element d3D11Element in  NewD3D11ElementList)
            {
                D3D11ElementList.Add(d3D11Element);
            }

            DataGrid_GameType.ItemsSource = D3D11ElementList;
        }


        private void Button_AddNewD3D11ElementLineAfterChooseLine_Click(object sender, RoutedEventArgs e)
        {
            int SelectedIndex = DataGrid_GameType.SelectedIndex;
            if (SelectedIndex < 0)
            {
                _ = SSMTMessageHelper.Show("请先至少鼠标左键单机选中一个行");
                return;
            }

            D3D11Element NewD3D11Element = GetCurrentFilledD3D11Element();

            List<D3D11Element> NewD3D11ElementList = new List<D3D11Element>();

            int index = 0;
            foreach (D3D11Element d3d11Element in D3D11ElementList)
            {
                NewD3D11ElementList.Add(d3d11Element);
                if (index == SelectedIndex)
                {
                    NewD3D11ElementList.Add(NewD3D11Element);
                }
                index++;
            }

            D3D11ElementList.Clear();
            foreach (D3D11Element d3d11Element in NewD3D11ElementList)
            {
                D3D11ElementList.Add(d3d11Element);
            }

            DataGrid_GameType.SelectedIndex = SelectedIndex;

            CalculateAndShowTotalStride();
        }

        private void Button_ClearD3D11ElementLine_Click(object sender, RoutedEventArgs e)
        {
            D3D11ElementList.Clear();

            //添加空行确保用户可编辑
            AddBlankD3D11ElementLine();
        }


    }
}
