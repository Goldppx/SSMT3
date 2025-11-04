using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WinUI3Helper;
using SSMT_Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SSMT
{
    public class IBDrawIndexed
    {
        public string StartIndex { get; set; } = "";
        public string IndexCount { get; set; } = "";
    }

    public class IndexBufferItem
    {
        public string Format { get; set; } = "";
        public string IBFilePath { get; set; } = "";
    }

    public class CategoryBufferItem
    {
        public string BufFilePath { get; set; } = "";
        public string Category { get; set; } = "";

    }

    public class ShapeKeyPositionBufferItem
    {
        public string Category { get; set; } = "";

        public string BufFilePath { get; set; } = "";
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReversePage : Page
    {

        private ObservableCollection<IndexBufferItem> IndexBufferItemList = new ObservableCollection<IndexBufferItem>();
        private ObservableCollection<CategoryBufferItem> CategoryBufferItemList = new ObservableCollection<CategoryBufferItem>();
        private ObservableCollection<ShapeKeyPositionBufferItem> ShapeKeyPositionBufferItemList = new ObservableCollection<ShapeKeyPositionBufferItem>();
        private ObservableCollection<IBDrawIndexed> IBDrawIndexedList = new ObservableCollection<IBDrawIndexed>();


        public ReversePage()
        {
            this.InitializeComponent();

            ComboBox_GameName.Items.Add("GI");
            ComboBox_GameName.Items.Add("HI3");
            ComboBox_GameName.Items.Add("HSR");
            ComboBox_GameName.Items.Add("ZZZ");
            ComboBox_GameName.Items.Add("WWMI");
        
            ComboBox_GameName.Items.Add("IdentityV");
            ComboBox_GameName.Items.Add("IdentityV2");

            if (GlobalConfig.CurrentGameName != "" && ComboBox_GameName.Items.Contains(GlobalConfig.CurrentGameName))
            {
                ComboBox_GameName.SelectedItem = GlobalConfig.CurrentGameName;
            }
            else
            {
                ComboBox_GameName.SelectedIndex = 0;
            }

            ComboBox_IBFormat.SelectedIndex = 0;
            ComboBox_Category.SelectedIndex = 0;


            ComboBox_GameTypeName.Items.Clear();
            string[] GameTypeFolderPathList = Directory.GetDirectories(GlobalConfig.Path_GameTypeConfigsFolder);

            foreach (string GameTypeFolderPath in GameTypeFolderPathList)
            {
                string GameTypeFolderName = Path.GetFileName(GameTypeFolderPath);
                ComboBox_GameTypeName.Items.Add(GameTypeFolderName);
            }
            ComboBox_GameTypeName.SelectedIndex = 0;

        }

       

        


        private void ComboBox_GameName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string GameName = ComboBox_GameName.SelectedItem.ToString();

            if (GameName == "WWMI")
            {
                Menu_Reverse_DrawIndexedIni.Visibility = Visibility.Collapsed;
                Menu_Reverse_ToggleIni.Visibility = Visibility.Collapsed;
            }
            else
            {
                Menu_Reverse_DrawIndexedIni.Visibility = Visibility.Visible;
                Menu_Reverse_ToggleIni.Visibility = Visibility.Visible;
            }
        }


        public static async Task<string> RunReverseIniCommand(string commandStr,string GameName)
        {
            //逆向ini之前必须先选择执行的目标游戏
            if (string.IsNullOrEmpty(GlobalConfig.CurrentGameName))
            {
                _ = SSMTMessageHelper.Show("在逆向Mod之前请选择当前要进行格式转换的二创模型的所属游戏", "Please select your current game before reverse.");
                return "";
            }

            //选择Mod的ini文件
            string ReverseIniFilePath = await SSMTCommandHelper.ChooseFileAndGetPath(".ini");
            if (ReverseIniFilePath == null || ReverseIniFilePath == "")
            {
                return "";
            }

            JObject runInputJson = new JObject();
            if (File.Exists(GlobalConfig.Path_RunInputJson))
            {
                string json = File.ReadAllText(GlobalConfig.Path_RunInputJson); // 读取文件内容
                runInputJson = JObject.Parse(json);
            }
            runInputJson["GameName"] = GameName;
            runInputJson["ReverseFilePath"] = ReverseIniFilePath;
            File.WriteAllText(GlobalConfig.Path_RunInputJson, runInputJson.ToString());
            LOG.Info("RunReverseCommand::Start");
            bool RunResult = SSMTCommandHelper.RunPluginExeCommand(commandStr, "3Dmigoto-Sword-Lv5.exe",true,true);
            LOG.Info("RunReverseCommand::End");
            LOG.Info(RunResult.ToString());

            if (RunResult)
            {
                return ReverseIniFilePath;
            }
            else
            {
                return "";
            }

        }

        private async void Menu_Reverse_SingleIni_Click(object sender, RoutedEventArgs e)
        {
            LOG.Initialize(GlobalConfig.Path_LogsFolder);
            try
            {
                if (!File.Exists(GlobalConfig.Path_SwordLv5Exe))
                {
                    _ = SSMTMessageHelper.Show("您当前Plugins目录下的3Dmigoto-Sword-Lv5.exe不存在，请检查是否被杀软错误删除或关闭杀软后重新完整下载本软件使用。");
                    return;
                }
                LOG.Info("RunReverseIniCommand::Start");
                string CurrentSelectedGame = ComboBox_GameName.SelectedItem.ToString();
                string ModIniFilePath = await RunReverseIniCommand("ReverseSingleLv5", CurrentSelectedGame);
                LOG.Info(ModIniFilePath);
                LOG.Info("RunReverseIniCommand::End");

                if (ModIniFilePath == "")
                {
                    return;
                }
                if (File.Exists(ModIniFilePath))
                {
                    string ModFolderPath = Path.GetDirectoryName(ModIniFilePath);

                    //转换贴图
                    if (CheckBox_AutoConvertTexturesRecursivelyInOriginalModFolder.IsChecked == true)
                    {
                        SSMTTextureHelper.ConvertAllTexturesIntoConvertedTexturesReverse(ModFolderPath, "jpg");
                    }
                    string ModFolderName = Path.GetFileName(ModFolderPath);
                    string ModFolderParentPath = Path.GetDirectoryName(ModFolderPath);
                    string ModReverseFolderPath = ModFolderParentPath + "\\" + ModFolderName + "-SingleModIniReverse\\";

                    string iniFileName = Path.GetFileNameWithoutExtension(ModIniFilePath);
                    if (CurrentSelectedGame == "WWMI")
                    {
                        ModReverseFolderPath = Path.Combine(ModFolderParentPath, ModFolderName + "-" + iniFileName + "-SingleModIniReverse\\");
                    }

                    if (CheckBox_AutoConvertTexturesRecursively.IsChecked == true)
                    {
                        SSMTTextureHelper.ConvertAllTexturesIntoConvertedTexturesReverse(ModFolderPath, "jpg", ModReverseFolderPath);
                    }

                    
                    SaveReverseOutputFolderPathToConfig(ModReverseFolderPath);
                   
                    if (CheckBox_AutoOpenFolderAfterReverse.IsChecked == true)
                    {
                        SSMTCommandHelper.ShellOpenFolder(ModReverseFolderPath);
                    }
                    else
                    {
                        _ = SSMTMessageHelper.Show("逆向成功!", "Reverse Success!");
                    }
                }
                else
                {
                    _ = SSMTCommandHelper.ShellOpenFile(GlobalConfig.Path_LatestDBMTLogFile);
                }
            }
            catch (Exception ex)
            {

                _ = SSMTMessageHelper.Show("Error: " + ex.ToString());
            }


        }

        private async void Menu_Reverse_ToggleIni_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(GlobalConfig.Path_SwordLv5Exe))
                {
                    _ = SSMTMessageHelper.Show("您当前Plugins目录下的3Dmigoto-Sword-Lv5.exe不存在，请检查是否被杀软错误删除或关闭杀软后重新完整下载本软件使用。");
                    return;
                }

                string ModIniFilePath = await RunReverseIniCommand("ReverseMergedLv5", ComboBox_GameName.SelectedItem.ToString());
                if (ModIniFilePath == "")
                {
                    return;
                }
                if (File.Exists(ModIniFilePath))
                {
                    string ModFolderPath = Path.GetDirectoryName(ModIniFilePath);

                    //转换贴图
                    if (CheckBox_AutoConvertTexturesRecursivelyInOriginalModFolder.IsChecked == true)
                    {
                        SSMTTextureHelper.ConvertAllTexturesIntoConvertedTexturesReverse(ModFolderPath, "jpg");
                    }

                    string ModFolderName = Path.GetFileName(ModFolderPath);
                    string ModFolderParentPath = Path.GetDirectoryName(ModFolderPath);
                    string ModReverseFolderPath = ModFolderParentPath + "\\" + ModFolderName + "-BufferBasedToggleModIniReverse\\";
                    if (CheckBox_AutoConvertTexturesRecursively.IsChecked == true)
                    {
                        SSMTTextureHelper.ConvertAllTexturesIntoConvertedTexturesReverse(ModFolderPath, "jpg", ModReverseFolderPath);
                    }

                    SaveReverseOutputFolderPathToConfig(ModReverseFolderPath);

      
                    if (CheckBox_AutoOpenFolderAfterReverse.IsChecked == true)
                    {
                        SSMTCommandHelper.ShellOpenFolder(ModReverseFolderPath);
                    }
                    else
                    {
                        _ = SSMTMessageHelper.Show("逆向成功!", "Reverse Success!");
                    }
                }
                else
                {
                    _ = SSMTCommandHelper.ShellOpenFile(GlobalConfig.Path_LatestDBMTLogFile);
                }
            }
            catch (Exception ex)
            {

                _ = SSMTMessageHelper.Show("Error: " + ex.ToString());
            }



        }

        private async void Menu_Reverse_DrawIndexedIni_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(GlobalConfig.Path_SwordLv5Exe))
                {
                    _ = SSMTMessageHelper.Show("您当前Plugins目录下的3Dmigoto-Sword-Lv5.exe不存在，请检查是否被杀软错误删除或关闭杀软后重新完整下载本软件使用。");
                    return;
                }

                string ModIniFilePath = await RunReverseIniCommand("ReverseOutfitCompilerLv4", ComboBox_GameName.SelectedItem.ToString());
                if (ModIniFilePath == "")
                {
                    return;
                }

                if (File.Exists(ModIniFilePath))
                {
                    string ModFolderPath = Path.GetDirectoryName(ModIniFilePath);

                    //转换贴图
                    if (CheckBox_AutoConvertTexturesRecursivelyInOriginalModFolder.IsChecked == true)
                    {
                        SSMTTextureHelper.ConvertAllTexturesIntoConvertedTexturesReverse(ModFolderPath, "jpg");
                    }

                    string ModFolderName = Path.GetFileName(ModFolderPath);
                    string ModFolderParentPath = Path.GetDirectoryName(ModFolderPath);
                    string ModReverseFolderPath = ModFolderParentPath + "\\" + ModFolderName + "-DrawIndexedBasedToggleModIniReverse\\";

                    if (CheckBox_AutoConvertTexturesRecursively.IsChecked == true)
                    {
                        SSMTTextureHelper.ConvertAllTexturesIntoConvertedTexturesReverse(ModFolderPath, "jpg", ModReverseFolderPath);
                    }

                    SaveReverseOutputFolderPathToConfig(ModReverseFolderPath);
                    
                    if (CheckBox_AutoOpenFolderAfterReverse.IsChecked == true)
                    {
                        SSMTCommandHelper.ShellOpenFolder(ModReverseFolderPath);
                    }
                    else
                    {
                        _ = SSMTMessageHelper.Show("逆向成功!", "Reverse Success!");
                    }
                }
                else
                {
                    _ = SSMTCommandHelper.ShellOpenFile(GlobalConfig.Path_LatestDBMTLogFile);
                }
            }
            catch (Exception ex)
            {

                _ = SSMTMessageHelper.Show("Error: " + ex.ToString());
            }


        }


        private void Menu_ReverseIniNew_Click(object sender, RoutedEventArgs e)
        {

        }


        /// <summary>
        /// 
        /// 逆向结果文件夹写到ReverseResult.json
        /// 这样在我们的Blender面板里就能一键导入了
        /// 
        /// </summary>
        /// <param name="ModReverseFolderPath"></param>
        private void SaveReverseOutputFolderPathToConfig(string ModReverseFolderPath)
        {
            JObject jobj = DBMTJsonUtils.CreateJObject();
            jobj["ReverseOutputFolder"] = ModReverseFolderPath;
            DBMTJsonUtils.SaveJObjectToFile(jobj, GlobalConfig.Path_ReverseResultConfig);
        }


    }
}
