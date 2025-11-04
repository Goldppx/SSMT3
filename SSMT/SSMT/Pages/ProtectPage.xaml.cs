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
using System.Diagnostics;
using System.Collections.ObjectModel;
using SSMT_Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SSMT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProtectPage : Page
    {
        // 定义一个可观察的字符串集合
        public ObservableCollection<string> MyStringList { get; set; }

        public ProtectPage()
        {
            this.InitializeComponent();

            ProtectConfig.ReadConfig();
            ReadConfig();
        }




        private void Menu_OpenACLFolder_Click(object sender, RoutedEventArgs e)
        {
            string ACLFolderPath = TextBox_ACLFolderPath.Text;
            if (Directory.Exists(ACLFolderPath))
            {
                SSMTCommandHelper.ShellOpenFolder(ACLFolderPath);
            }
            else
            {
                _ = SSMTMessageHelper.Show("无法找到当前填写的ACL文件夹的路径", "Can't find current ACL folder path.");
            }
        }

        private void Menu_OpenTargetFolder_Click(object sender, RoutedEventArgs e)
        {
            string TargetModFolderPath = TextBox_TargetFolderPath.Text;
            if (Directory.Exists(TargetModFolderPath))
            {
                SSMTCommandHelper.ShellOpenFolder(TargetModFolderPath);
            }
            else
            {
                _ = SSMTMessageHelper.Show("无法找到当前填写的目标Mod文件夹的路径", "Can't find current target Mod folder path.");
            }
        }

        private async void Button_SelectACLFolderPath_Click(object sender, RoutedEventArgs e)
        {
            string ACLFolderPath = await SSMTCommandHelper.ChooseFolderAndGetPath();

            if (ACLFolderPath != "")
            {
                TextBox_ACLFolderPath.Text = ACLFolderPath;
                FlushKeyList();

                SaveConfig();
            }

        }

        private async void Button_SelectTargetFolderPath_Click(object sender, RoutedEventArgs e)
        {
            string TargetModFolderPath = await SSMTCommandHelper.ChooseFolderAndGetPath();

            if (TargetModFolderPath != "")
            {
                TextBox_TargetFolderPath.Text = TargetModFolderPath;

                SaveConfig();
            }
        }

        public void FlushKeyList()
        {
            MyListBox.Items.Clear();
            //遍历ACL 文件夹下面的ini文件

            string ACLFolderPath = TextBox_ACLFolderPath.Text; // 替换为指定目录的路径

            if (!Directory.Exists(ACLFolderPath))
            {
                return;
            }

            // 获取目录中以".key"结尾的所有文件
            string[] KeyFiles = Directory.GetFiles(ACLFolderPath, "*.key");

            // 遍历文件并进行处理
            foreach (string KeyFilePath in KeyFiles)
            {
                string KeyFileName = Path.GetFileName(KeyFilePath);
                MyListBox.Items.Add(KeyFileName);
            }
        }

        private void Button_FlushKeyList_Click(object sender, RoutedEventArgs e)
        {
            FlushKeyList();
            _ = SSMTMessageHelper.Show("刷新完成", "Flush Success");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // 执行你想要在这个页面被关闭或导航离开时运行的代码
            SaveConfig();
            // 如果需要，可以调用基类的 OnNavigatedFrom 方法
            base.OnNavigatedFrom(e);
        }

        public void ReadConfig()
        {
            TextBox_ACLFolderPath.Text = ProtectConfig.DBMT_Protect_ACLFolderPath;
            TextBox_TargetFolderPath.Text = ProtectConfig.DBMT_Protect_TargetModPath;
            FlushKeyList();
        }

        public void SaveConfig()
        {
            ProtectConfig.DBMT_Protect_ACLFolderPath = TextBox_ACLFolderPath.Text;
            ProtectConfig.DBMT_Protect_TargetModPath = TextBox_TargetFolderPath.Text;
            ProtectConfig.SaveConfig();
        }

        private void Button_AddProtect_Click(object sender, RoutedEventArgs e)
        {
            // 批量锁机器码： 递归遍历目录下所有的.ini文件加密
            // 先统计所有文件，ini文件改为.resS后缀，然后创建一个新的目录去生成
            // 这个过程在C++里执行就好了，这里只负责调用

            SaveConfig();

            string TargetModFolderPath = TextBox_TargetFolderPath.Text;
            if (!Directory.Exists(TargetModFolderPath))
            {
                _ = SSMTMessageHelper.Show("要锁机器码的目标Mod文件夹不存在: " + TargetModFolderPath, "Target Mod folder path doesn't exists: " + TargetModFolderPath);
                return;
            }



            //复制整个Mod文件夹到新的文件夹路径

            string TargetModFolderName = Path.GetFileName(TargetModFolderPath);
            string TargetModFolderParentFolderPath = Path.GetDirectoryName(TargetModFolderPath);
            //_ = MessageHelper.Show(TargetModFolderName);

            string NewTargetModFolderName = TargetModFolderName + "_Release";
            string NewTargetModFolderPath = Path.Combine(TargetModFolderParentFolderPath, NewTargetModFolderName + "\\");
            //_ = MessageHelper.Show(NewTargetModFolderPath);

            if (!Directory.Exists(NewTargetModFolderPath))
            {
                Directory.CreateDirectory(NewTargetModFolderPath);
            }

            //Mod文件复制到新目录后再加密，这样就不用每次都复制一次了。
            DBMTFileUtils.CopyDirectory(TargetModFolderPath, NewTargetModFolderPath, true);


            //把当前的ACL文件以及加密的目标文件夹目录存放到对应配置文件中。
            string ACLSettingJsonPath = ProtectConfig.Path_ACLSettingJson;

            JObject jobj = DBMTJsonUtils.CreateJObject();
            jobj["targetACLFile"] = NewTargetModFolderPath;

            // 获取目录中以".key"结尾的所有文件
            string ACLFolderPath = TextBox_ACLFolderPath.Text;
            if (!Directory.Exists(ACLFolderPath))
            {
                return;
            }
            string[] KeyFiles = Directory.GetFiles(ACLFolderPath, "*.key");
            jobj["AccessControlList"] = new JArray(KeyFiles);

            DBMTJsonUtils.SaveJObjectToFile(jobj, ACLSettingJsonPath);

            bool result = SSMTCommandHelper.RunPluginExeCommand("ACPTPRO_Batch_V4", "DBMT-Protect.exe");


            SSMTCommandHelper.ShellOpenFolder(NewTargetModFolderPath);

            //_ = MessageHelper.Show("批量锁机器码ACPT_PRO_V4算法执行成功! ");
        }

        private void Button_GenerateKeyFile_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = DBMTJsonUtils.CreateJObject();
            jsonObject["UserName"] = TextBox_UserName.Text;
            jsonObject["DeviceID"] = TextBox_UUID.Text;
            DBMTJsonUtils.SaveJObjectToFile(jsonObject, ProtectConfig.Path_DeviceKeySetting);
            bool result = SSMTCommandHelper.RunPluginExeCommand("generateKeyByDeviceID", "DBMT-Protect.exe");

            if (result)
            {
                SSMTCommandHelper.ShellOpenFolder(ProtectConfig.Path_GeneratedAESKeyFolder);
            }
        }



   






    }
}
