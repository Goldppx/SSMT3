using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSMT_Core;

namespace SSMT
{
    public partial class WorkPage
    {

        private void SkipIBDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // 检查是否是最后一行被编辑
            if (e.EditAction == CommunityToolkit.WinUI.UI.Controls.DataGridEditAction.Commit &&
                e.Row.GetIndex() == SkipIBItems.Count - 1)
            {
                // 如果是最后一行，则添加新的一行
                SkipIBListAddBlankRow();
            }

            SaveSkipIBList();
        }


        private void SkipIBListAddBlankRow()
        {
            SkipIBItems.Add(new DrawIBItem { DrawIB = "", Alias = "" });
        }

        private void Menu_DeleteSkipIBLine_Click(object sender, RoutedEventArgs e)
        {
            int index = SkipIBDataGrid.SelectedIndex;
            if (index >= 0)
            {
                SkipIBItems.RemoveAt(index);

                //如果删除后行数为0则添加一行新的
                if (SkipIBItems.Count == 0)
                {
                    SkipIBItems.Add(new DrawIBItem { DrawIB = "", Alias = "" });
                }

            }
            else
            {
                _ = SSMTMessageHelper.Show("请先至少鼠标左键单机选中一个行");
            }
        }

        void ReadSkipIBListFromWorkSpace()
        {
            Debug.WriteLine("读取SkipIB列表::Start");
            if (GlobalConfig.CurrentWorkSpace != "")
            {
                SkipIBItems.Clear();


                string Configpath = PathManager.Path_CurrentWorkSpace_SkipIBConfigJson;
                Debug.WriteLine("读取配置文件路径: " + Configpath);

                if (File.Exists(Configpath))
                {
                    //切换到对应配置
                    string jsonData = File.ReadAllText(Configpath);
                    JArray DrawIBListJArray = JArray.Parse(jsonData);

                    foreach (JObject jkobj in DrawIBListJArray)
                    {
                        DrawIBItem item = new DrawIBItem();
                        item.DrawIB = (string)jkobj["DrawIB"];
                        item.Alias = (string)jkobj["Alias"];
                        SkipIBItems.Add(item);
                    }

                }

                //保底确保用户可以编辑
                //确保永远有一个新的空行可供编辑
                SkipIBListAddBlankRow();
            }
            Debug.WriteLine("读取SkipIB列表::End");
        }

        public async void SaveSkipIBList()
        {
            Debug.WriteLine("保存当前SkipIB列表::Start");
            //(1) 检查游戏类型是否设置
            if (GlobalConfig.CurrentGameName == "")
            {
                await SSMTMessageHelper.Show("请先选择游戏类型", "Please select a game before this.");
                return;
            }

            if (GlobalConfig.CurrentWorkSpace == "")
            {
                await SSMTMessageHelper.Show("请先选择工作空间", "Please select a workspace before this.");
                return;
            }

            Debug.WriteLine("当前工作空间: " + GlobalConfig.CurrentWorkSpace);


            //(3) 接下来把所有的drawIBList中的DrawIB保留下来存储到对应配置文件。
            SaveSkipIBListConfigToFolder(PathManager.Path_CurrentWorkSpaceFolder);

            Debug.WriteLine("保存当前SkipIB列表::End");
            Debug.WriteLine("----------------------------------");
        }

        public void SaveSkipIBListConfigToFolder(string SaveFolderPath)
        {
            if (Directory.Exists(SaveFolderPath))
            {
                bool AllEmpry = true;

                JArray DrawIBJarrayList = new JArray();
                foreach (DrawIBItem item in SkipIBItems)
                {
                    if (item.DrawIB.Trim() != "")
                    {
                        JObject jobj = new JObject();
                        jobj["DrawIB"] = item.DrawIB;
                        jobj["Alias"] = item.Alias;
                        DrawIBJarrayList.Add(jobj);


                        if (item.DrawIB.Trim() != "")
                        {
                            AllEmpry = false;
                        }
                        else if (item.Alias.Trim() != "")
                        {
                            AllEmpry = false;
                        }
                    }
                }

                //只有不为空时才保存。

                if (!AllEmpry)
                {
                    string json_string = DrawIBJarrayList.ToString(Formatting.Indented);
                    // 将JSON字符串写入文件
                    File.WriteAllText(PathManager.Path_CurrentWorkSpace_SkipIBConfigJson, json_string);
                }
                else
                {
                    try
                    {
                        File.Delete(PathManager.Path_CurrentWorkSpace_SkipIBConfigJson);

                    }
                    catch (Exception ex)
                    {
                        ex.ToString();
                    }
                }

            }

        }



        public static void GenerateSkipIB(List<DrawIBItem> DrawIBList)
        {
            string outputContent = "";
            List<string> WritedHashList = new List<string>();

            foreach (DrawIBItem DrawIBItem in DrawIBList)
            {
                //FrameAnalysisInfo FAInfo = new FrameAnalysisInfo(DrawIB);
                //Dictionary<string, List<string>> buffHash_vsShaderHashValues_Dict = DrawIBHelper.GetBuffHash_VSShaderHashValues_Dict(FAInfo.FolderPath);
                outputContent = outputContent + "; " + DrawIBItem.Alias + " \r\n";
                outputContent = outputContent + "[TextureOverride_IB_" + DrawIBItem.DrawIB + "]\r\n";
                outputContent = outputContent + "hash = " + DrawIBItem.DrawIB + "\r\n";
                outputContent = outputContent + "handling = skip\r\n";
                outputContent = outputContent + "\r\n";
            }

            if (!File.Exists(PathManager.Path_CurrentWorkSpaceGeneratedModFolder))
            {
                Directory.CreateDirectory(PathManager.Path_CurrentWorkSpaceGeneratedModFolder);
            }

            string outputPath = Path.Combine(PathManager.Path_CurrentWorkSpaceGeneratedModFolder, "IBSkip.ini");

            File.WriteAllText(outputPath, outputContent);
        }




    }
}
