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

namespace SSMT
{
    public partial class WorkPage
    {
        private void MyDataGrid_CellEditEnding(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridCellEditEndingEventArgs e)
        {
            // 检查是否是最后一行被编辑
            if (e.EditAction == CommunityToolkit.WinUI.UI.Controls.DataGridEditAction.Commit &&
                e.Row.GetIndex() == DrawIBItems.Count - 1)
            {
                // 如果是最后一行，则添加新的一行
                DrawIBListAddBlankRow();
            }

            SaveDrawIBList(false);
        }

        private void DrawIBListAddBlankRow()
        {
            DrawIBItems.Add(new DrawIBItem { DrawIB = "", Alias = "" });
        }


        private void Menu_DeleteDrawIBLine_Click(object sender, RoutedEventArgs e)
        {
            int index = myDataGrid.SelectedIndex;
            if (index >= 0)
            {
                DrawIBItems.RemoveAt(index);

                //如果删除后行数为0则添加一行新的
                if (DrawIBItems.Count == 0)
                {
                    DrawIBItems.Add(new DrawIBItem { DrawIB = "", Alias = "" });
                }
            }
            else
            {
                _ = SSMTMessageHelper.Show("请先至少鼠标左键单机选中一个行");
            }
        }

        void ReadDrawIBListFromWorkSpace()
        {
            Debug.WriteLine("读取DrawIB列表::Start");
            if (GlobalConfig.CurrentWorkSpace != "")
            {
                DrawIBItems.Clear();


                string Configpath = GlobalConfig.Path_CurrentWorkSpace_ConfigJson;
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
                        DrawIBItems.Add(item);
                    }

                }

                //保底确保用户可以编辑
                //确保永远有一个新的空行可供编辑
                DrawIBListAddBlankRow();
            }
            Debug.WriteLine("读取DrawIB列表::End");
        }

        /// <summary>
        /// 这个DrawIB列表必须得保存到配置文件，因为后面Blender导入模型的时候会用到。
        /// 除非更改Blender逻辑导入所有的模型。
        /// </summary>
        public async void SaveDrawIBList(bool DeleteConfigJsonIfAllEmpty = true)
        {
            Debug.WriteLine("保存当前DrawIB列表::Start");
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

            //(2) 接下来要把当前的游戏名称+类型保存到MainSetting.json里
            GlobalConfig.SaveConfig();

            //(3) 接下来把所有的drawIBList中的DrawIB保留下来存储到对应配置文件。
            SaveDrawIBListConfigToFolder(GlobalConfig.Path_CurrentWorkSpaceFolder, DeleteConfigJsonIfAllEmpty);

            Debug.WriteLine("保存当前DrawIB列表::End");
            Debug.WriteLine("----------------------------------");
        }


        public void SaveDrawIBListConfigToFolder(string SaveFolderPath, bool DeleteConfigJsonIfAllEmpty = true)
        {
            if (Directory.Exists(SaveFolderPath))
            {
                bool AllEmpry = true;

                JArray DrawIBJarrayList = new JArray();
                foreach (DrawIBItem item in DrawIBItems)
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
                    //DBMTJsonUtils.SaveJObjectToFile(DrawIBJarrayList, SaveFolderPath + "Config.Json");
                    string json_string = DrawIBJarrayList.ToString(Formatting.Indented);
                    // 将JSON字符串写入文件
                    File.WriteAllText(SaveFolderPath + "Config.Json", json_string);
                }
                else
                {
                    if (DeleteConfigJsonIfAllEmpty)
                    {
                        //这个会导致保存配置时，把当前工作空间中的内容删光
                        //如果切换游戏导致工作空间发生变化，必须第一时间更新Path_CurrentWorkSpace_ConfigJson内容，否则就会导致文件被清空
                        try
                        {
                            File.Delete(GlobalConfig.Path_CurrentWorkSpace_ConfigJson);
                        }
                        catch (Exception ex)
                        {
                            ex.ToString();
                        }
                    }
                }

            }

        }




    }
}
