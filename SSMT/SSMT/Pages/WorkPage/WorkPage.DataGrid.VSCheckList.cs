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

namespace SSMT
{
    public partial class WorkPage
    {
        public static Dictionary<string, List<string>> GetBuffHash_VSShaderHashValues_Dict(string frameAnalysisFolderPath)
        {

            Dictionary<string, List<string>> buffHash_vsShaderHashValues_Dict = new Dictionary<string, List<string>>();

            // 获取当前目录下的所有文件
            string[] files = Directory.GetFiles(frameAnalysisFolderPath);
            foreach (string fileName in files)
            {
                if (!fileName.EndsWith(".txt"))
                {
                    continue;
                }

                int vsIndex = fileName.IndexOf("-vs=");
                if (vsIndex != -1)
                {
                    string bufferHash = fileName.Substring(vsIndex - 8, 8);
                    string vsShaderHash = fileName.Substring(vsIndex + 4, 16);

                    List<string> tmpList = new List<string>();
                    if (buffHash_vsShaderHashValues_Dict.ContainsKey(bufferHash))
                    {
                        tmpList = buffHash_vsShaderHashValues_Dict[bufferHash];
                    }
                    tmpList.Add(vsShaderHash);
                    buffHash_vsShaderHashValues_Dict[bufferHash] = tmpList;
                }
                else
                {
                    continue;
                }

            }

            return buffHash_vsShaderHashValues_Dict;
        }

        public static void GenerateVSCheck(List<string> DrawIBList, string VSCheckIniFileName)
        {

            string outputContent = "";

            List<string> WritedHashList = new List<string>();

            foreach (string DrawIB in DrawIBList)
            {
                FrameAnalysisInfo FAInfo = new FrameAnalysisInfo(DrawIB);
                Dictionary<string, List<string>> buffHash_vsShaderHashValues_Dict = GetBuffHash_VSShaderHashValues_Dict(FAInfo.FolderPath);

                if (buffHash_vsShaderHashValues_Dict.ContainsKey(DrawIB))
                {
                    List<string> VSHashList = buffHash_vsShaderHashValues_Dict[DrawIB];
                    foreach (string hash in VSHashList)
                    {
                        if (WritedHashList.Contains(hash))
                        {
                            continue;
                        }
                        WritedHashList.Add(hash);
                        outputContent += "[ShaderOverride_" + hash + "]\r\n";
                        outputContent += "allow_duplicate_hash = overrule\r\n";
                        outputContent += "hash = " + hash + "\r\n";
                        outputContent += "if $costume_mods\r\n";
                        outputContent += "  checktextureoverride = ib\r\n";
                        outputContent += "endif\r\n\r\n";
                    }
                }

            }

            if (!File.Exists(GlobalConfig.Path_CurrentWorkSpaceGeneratedModFolder))
            {
                Directory.CreateDirectory(GlobalConfig.Path_CurrentWorkSpaceGeneratedModFolder);
            }

            string VSCheckIniFilePath = Path.Combine(GlobalConfig.Path_CurrentWorkSpaceGeneratedModFolder, VSCheckIniFileName + ".ini");
            File.WriteAllText(VSCheckIniFilePath, outputContent);
        }

        private void VSCheckAddBlankRow()
        {
            VSCheckItems.Add(new VSCheckItem { Enable = true, VSHash = "" });
        }


        private void Menu_DeleteVSCheckLine_Click(object sender, RoutedEventArgs e)
        {
            int index = DataGrid_VSCheck.SelectedIndex;
            if (index >= 0)
            {
                VSCheckItems.RemoveAt(index);

                //如果删除后行数为0则添加一行新的
                if (VSCheckItems.Count == 0)
                {
                    VSCheckAddBlankRow();
                }

            }
            else
            {
                _ = SSMTMessageHelper.Show("请先至少鼠标左键单机选中一个行");
            }
        }
        private void DataGrid_VSCheck_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // 检查是否是最后一行被编辑
            if (e.EditAction == CommunityToolkit.WinUI.UI.Controls.DataGridEditAction.Commit &&
                e.Row.GetIndex() == VSCheckItems.Count - 1)
            {
                // 如果是最后一行，则添加新的一行
                VSCheckAddBlankRow();
            }

            SaveVSCheckListConfigToFolder(GlobalConfig.Path_CurrentWorkSpaceFolder);
        }

        public void SaveVSCheckListConfigToFolder(string SaveFolderPath)
        {
            if (Directory.Exists(SaveFolderPath))
            {
                bool AllEmpry = true;

                JArray VSCheckJarrayList = new JArray();
                foreach (VSCheckItem item in VSCheckItems)
                {
                    if (item.VSHash.Trim() != "")
                    {
                        JObject jobj = new JObject();
                        jobj["Enable"] = item.Enable;
                        jobj["VSHash"] = item.VSHash;
                        VSCheckJarrayList.Add(jobj);


                        if (item.VSHash.Trim() != "")
                        {
                            AllEmpry = false;
                        }
                    }
                }

                //只有不为空时才保存。

                if (!AllEmpry)
                {
                    string json_string = VSCheckJarrayList.ToString(Formatting.Indented);
                    // 将JSON字符串写入文件
                    File.WriteAllText(GlobalConfig.Path_CurrentWorkSpace_VSCheckConfigJson, json_string);
                }
                else
                {
                    try
                    {
                        File.Delete(GlobalConfig.Path_CurrentWorkSpace_VSCheckConfigJson);

                    }
                    catch (Exception ex)
                    {
                        ex.ToString();
                    }
                }

            }

        }

        void ReadVSCheckListFromWorkSpace()
        {
            if (GlobalConfig.CurrentWorkSpace != "")
            {
                VSCheckItems.Clear();


                string Configpath = GlobalConfig.Path_CurrentWorkSpace_VSCheckConfigJson;
                Debug.WriteLine("读取配置文件路径: " + Configpath);

                if (File.Exists(Configpath))
                {
                    //切换到对应配置
                    string jsonData = File.ReadAllText(Configpath);
                    JArray DrawIBListJArray = JArray.Parse(jsonData);

                    foreach (JObject jkobj in DrawIBListJArray)
                    {
                        VSCheckItem item = new VSCheckItem();
                        item.Enable = (bool)jkobj["Enable"];
                        item.VSHash = (string)jkobj["VSHash"];
                        VSCheckItems.Add(item);
                    }

                }

                //保底确保用户可以编辑
                //确保永远有一个新的空行可供编辑
                VSCheckAddBlankRow();
            }
        }


        private void Button_UpdateVSCheckList_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                List<string> TotalIBList = new List<string>();

                foreach (DrawIBItem item in SkipIBItems)
                {
                    if (item.DrawIB.Trim() != "")
                    {
                        TotalIBList.Add(item.DrawIB);
                    }
                }

                foreach (DrawIBItem item in DrawIBItems)
                {
                    if (item.DrawIB.Trim() != "")
                    {
                        TotalIBList.Add(item.DrawIB);
                    }
                }

                List<string> TotalVSHashList = new List<string>();

                foreach (string DrawIB in TotalIBList)
                {
                    FrameAnalysisInfo FAInfo = new FrameAnalysisInfo(DrawIB);
                    Dictionary<string, List<string>> buffHash_vsShaderHashValues_Dict = GetBuffHash_VSShaderHashValues_Dict(FAInfo.FolderPath);

                    if (buffHash_vsShaderHashValues_Dict.ContainsKey(DrawIB))
                    {
                        List<string> VSHashList = buffHash_vsShaderHashValues_Dict[DrawIB];
                        foreach (string hash in VSHashList)
                        {
                            if (!TotalVSHashList.Contains(hash))
                            {
                                TotalVSHashList.Add(hash);
                            }
                        }
                    }

                }

                List<string> CurrentVSHashList = new List<string>();
                foreach (VSCheckItem item in VSCheckItems)
                {
                    if (item.VSHash.Trim() != "")
                    {
                        CurrentVSHashList.Add(item.VSHash);
                    }
                }

                List<VSCheckItem> NewVSCheckItemList = new List<VSCheckItem>();

                foreach(string Hash in CurrentVSHashList)
                {
                    VSCheckItem newItem = new VSCheckItem
                    {
                        Enable = true,
                        VSHash = Hash
                    };
                    NewVSCheckItemList.Add(newItem);
                }

                foreach (string hash in TotalVSHashList)
                {
                    if (!CurrentVSHashList.Contains(hash))
                    {
                        VSCheckItem newItem = new VSCheckItem
                        {
                            Enable = true,
                            VSHash = hash
                        };
                        NewVSCheckItemList.Add(newItem);
                    }
                    else {
                        
                    }
                }

                VSCheckItems.Clear();
                foreach(VSCheckItem item in NewVSCheckItemList)
                {
                    VSCheckItems.Add(item);
                }

                VSCheckAddBlankRow();


                //保存到当前工作空间
                SaveVSCheckListConfigToFolder(GlobalConfig.Path_CurrentWorkSpaceFolder);
            }
            catch (Exception ex)
            {
                _ = SSMTMessageHelper.Show(ex.ToString());
            }

        }

        private void Button_GenerateVSCheckIni_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> TotalVSHashList = new List<string>();

                foreach(VSCheckItem vSCheckItem in VSCheckItems)
                {
                    if (vSCheckItem.Enable && !string.IsNullOrEmpty(vSCheckItem.VSHash.Trim()))
                    {
                        TotalVSHashList.Add(vSCheckItem.VSHash.Trim());
                    }
                }

                string outputContent = "";
                List<string> WritedHashList = new List<string>();

                foreach (string hash in TotalVSHashList)
                {
                    if (WritedHashList.Contains(hash))
                    {
                        continue;
                    }
                    WritedHashList.Add(hash);
                    outputContent += "[ShaderOverride_" + hash + "]\r\n";
                    outputContent += "allow_duplicate_hash = overrule\r\n";
                    outputContent += "hash = " + hash + "\r\n";
                    outputContent += "if $costume_mods\r\n";
                    outputContent += "  checktextureoverride = ib\r\n";
                    outputContent += "endif\r\n\r\n";
                }

                if (!File.Exists(GlobalConfig.Path_CurrentWorkSpaceGeneratedModFolder))
                {
                    Directory.CreateDirectory(GlobalConfig.Path_CurrentWorkSpaceGeneratedModFolder);
                }

                string VSCheckIniFilePath = Path.Combine(GlobalConfig.Path_CurrentWorkSpaceGeneratedModFolder,  "VSCheck.ini");
                File.WriteAllText(VSCheckIniFilePath, outputContent);

                SSMTCommandHelper.ShellOpenFolder(GlobalConfig.Path_CurrentWorkSpaceGeneratedModFolder);
            }
            catch (Exception ex)
            {
                _ = SSMTMessageHelper.Show(ex.ToString());
            }
        }

        private void Button_ClearVSCheckList_Click(object sender, RoutedEventArgs e)
        {
            VSCheckItems.Clear();
            VSCheckAddBlankRow();
        }



    }
}
