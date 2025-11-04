using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMT
{
    public partial class HomePage
    {

        private void TextBox_3DmigotoPath_LostFocus(object sender, RoutedEventArgs e)
        {
            DoAfter3DmigotoPathChanged();
        }

        private void TextBox_TargetPath_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveTargetPathToConfig();
        }


        private void DoAfter3DmigotoPathChanged()
        {
            //如果3Dmigoto路径被改变了，就触发这个方法
            //(1)首先把改变同步到GameConfig中
            GameConfig gameConfig = new GameConfig();
            gameConfig.MigotoPath = TextBox_3DmigotoPath.Text;
            gameConfig.SaveConfig();

            //(2)然后更新当前的3Dmigoto下的d3dx.ini中能够提供的信息：
            LoadD3DxIniConfigIfNoteConfigured();
        }


        private void LoadD3DxIniConfigIfNoteConfigured()
        {
            IsLoading = true;

            //target,launch,launch_args,show_warnings,symlink
            string d3dxini_path = Path.Combine(TextBox_3DmigotoPath.Text, "d3dx.ini");
            if (File.Exists(d3dxini_path))
            {
                //如果当前的target = 为空的话，就尝试读取
                if (TextBox_TargetPath.Text.Trim() == "")
                {
                    TextBox_TargetPath.Text = D3dxIniConfig.ReadAttributeFromD3DXIni(d3dxini_path, "target");
                }

                if (TextBox_LaunchPath.Text.Trim() == "")
                {
                    TextBox_LaunchPath.Text = D3dxIniConfig.ReadAttributeFromD3DXIni(d3dxini_path, "launch");
                }

                if (TextBox_LaunchArgsPath.Text.Trim() == "")
                {
                    TextBox_LaunchArgsPath.Text = D3dxIniConfig.ReadAttributeFromD3DXIni(d3dxini_path, "launch_args");
                }


            }


            IsLoading = false;

        }


        private void SaveTargetPathToConfig()
        {
            GameConfig gameConfig = new GameConfig();
            gameConfig.TargetPath = TextBox_TargetPath.Text;
            gameConfig.SaveConfig();
        }

        private void SaveLaunchPathToConfig()
        {
            GameConfig gameConfig = new GameConfig();
            gameConfig.LaunchPath = TextBox_LaunchPath.Text;
            gameConfig.SaveConfig();
        }

        private void TextBox_LaunchArgsPath_LostFocus(object sender, RoutedEventArgs e)
        {
            GameConfig gameConfig = new GameConfig();
            gameConfig.LaunchArgs = TextBox_LaunchArgsPath.Text;
            gameConfig.SaveConfig();
        }

        private void TextBox_LaunchPath_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveLaunchPathToConfig();
        }

        private async void Button_ChooseLaunchFile_Click(object sender, RoutedEventArgs e)
        {
            string filepath = await SSMTCommandHelper.ChooseFileAndGetPath(".exe");
            if (filepath != "")
            {
                TextBox_LaunchPath.Text = filepath;
                SaveLaunchPathToConfig();
            }
        }

        private async void Button_Choose3DmigotoPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folderPath = await SSMTCommandHelper.ChooseFolderAndGetPath();
                if (folderPath != "")
                {
                    TextBox_3DmigotoPath.Text = folderPath;

                    DoAfter3DmigotoPathChanged();
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _ = SSMTMessageHelper.Show(ex.ToString());
            }

        }



        private async void Button_ChooseProcessFile_Click(object sender, RoutedEventArgs e)
        {
            string filepath = await SSMTCommandHelper.ChooseFileAndGetPath(".exe");
            if (filepath != "")
            {
                TextBox_TargetPath.Text = filepath;

                SaveTargetPathToConfig();
            }
        }

    }
}
