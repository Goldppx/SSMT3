using Microsoft.UI.Xaml;
using Newtonsoft.Json.Linq;
using SSMT_Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMT
{
    public partial class ReversePage
    {


        private void Menu_ReversedFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(GlobalConfig.Path_ReversedFolder))
            {
                SSMTCommandHelper.ShellOpenFolder(GlobalConfig.Path_ReversedFolder);
            }
            else
            {
                _ = SSMTMessageHelper.Show("当前Reversed文件夹不存在，请先进行手动逆向生成此文件夹再来打开此文件夹。");
            }
        }


        private async void Menu_Textures_ConvertJpg_Click(object sender, RoutedEventArgs e)
        {
            string selected_folder_path = await SSMTCommandHelper.ChooseFolderAndGetPath();
            if (selected_folder_path == "")
            {
                return;
            }

            try
            {
                SSMTTextureHelper.ConvertAllTexturesIntoConvertedTexturesReverse(selected_folder_path, "jpg");

                SSMTCommandHelper.ShellOpenFolder(selected_folder_path + "\\ConvertedTextures\\");

            }
            catch (Exception ex)
            {
                _ = SSMTMessageHelper.Show(ex.ToString());
            }
        }

        private async void Menu_Textures_ConvertPng_Click(object sender, RoutedEventArgs e)
        {
            string selected_folder_path = await SSMTCommandHelper.ChooseFolderAndGetPath();
            if (selected_folder_path == "")
            {
                return;
            }

            try
            {
                SSMTTextureHelper.ConvertAllTexturesIntoConvertedTexturesReverse(selected_folder_path, "png");

                SSMTCommandHelper.ShellOpenFolder(selected_folder_path + "\\ConvertedTextures\\");

            }
            catch (Exception ex)
            {
                _ = SSMTMessageHelper.Show(ex.ToString());
            }
        }

        private async void Menu_Textures_ConvertTga_Click(object sender, RoutedEventArgs e)
        {
            string selected_folder_path = await SSMTCommandHelper.ChooseFolderAndGetPath();
            if (selected_folder_path == "")
            {
                return;
            }

            try
            {
                SSMTTextureHelper.ConvertAllTexturesIntoConvertedTexturesReverse(selected_folder_path, "tga");

                SSMTCommandHelper.ShellOpenFolder(selected_folder_path + "\\ConvertedTextures\\");

            }
            catch (Exception ex)
            {
                await SSMTMessageHelper.Show(ex.ToString());
            }
        }

        private void Menu_OpenPluginsFolder_Click(object sender, RoutedEventArgs e)
        {
            SSMTCommandHelper.ShellOpenFolder(GlobalConfig.Path_PluginsFolder);
        }

        private void Menu_OpenLogsFolder_Click(object sender, RoutedEventArgs e)
        {
            SSMTCommandHelper.ShellOpenFolder(GlobalConfig.Path_LogsFolder);
        }

        private async void Menu_OpenLatestLogFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SSMTCommandHelper.ShellOpenFile(GlobalConfig.Path_LatestDBMTLogFile);
            }
            catch (Exception ex)
            {
                await SSMTMessageHelper.Show("Error: " + ex.ToString());
            }
        }

        private void Menu_OpenConfigsFolder_Click(object sender, RoutedEventArgs e)
        {
            SSMTCommandHelper.ShellOpenFolder(GlobalConfig.Path_ConfigsFolder);
        }

        private void Menu_GameTypeFolder_Click(object sender, RoutedEventArgs e)
        {
            SSMTCommandHelper.ShellOpenFolder(GlobalConfig.Path_GameTypeConfigsFolder);
        }


        private void Menu_ManuallyReverseList_SaveCurrentList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject ManuallyReversePageConfigJOBJ = new JObject();

                //IndexBufferItemList
                JArray IndexBufferItemListJarray = new JArray();
                foreach (IndexBufferItem indexBufferItem in IndexBufferItemList)
                {
                    JObject jobj = new JObject();
                    jobj["Format"] = indexBufferItem.Format;
                    jobj["IBFilePath"] = indexBufferItem.IBFilePath;
                    IndexBufferItemListJarray.Add(jobj);
                }
                ManuallyReversePageConfigJOBJ["IndexBufferItemList"] = IndexBufferItemListJarray;

                //CategoryBufferItemList
                JArray CategoryBufferItemListJarray = new JArray();
                foreach (CategoryBufferItem categoryBufferItem in CategoryBufferItemList)
                {
                    JObject jobj = new JObject();
                    jobj["Category"] = categoryBufferItem.Category;
                    jobj["BufFilePath"] = categoryBufferItem.BufFilePath;
                    CategoryBufferItemListJarray.Add(jobj);
                }
                ManuallyReversePageConfigJOBJ["CategoryBufferItemList"] = CategoryBufferItemListJarray;

                //ShapeKeyPositionBufferItemList
                JArray ShapeKeyPositionBufferItemListJarray = new JArray();
                foreach (ShapeKeyPositionBufferItem shapeKeyPositionBufferItem in ShapeKeyPositionBufferItemList)
                {
                    JObject jobj = new JObject();
                    jobj["Category"] = shapeKeyPositionBufferItem.Category;
                    jobj["BufFilePath"] = shapeKeyPositionBufferItem.BufFilePath;
                    ShapeKeyPositionBufferItemListJarray.Add(jobj);
                }
                ManuallyReversePageConfigJOBJ["ShapeKeyPositionBufferItemList"] = ShapeKeyPositionBufferItemListJarray;


                string ManuallyReversePageConfigFilePath = Path.Combine(GlobalConfig.Path_ConfigsFolder, "ManuallyReversePageConfig.json");
                DBMTJsonUtils.SaveJObjectToFile(ManuallyReversePageConfigJOBJ, ManuallyReversePageConfigFilePath);

                _ = SSMTMessageHelper.Show("保存成功","Save Success");
            }
            catch (Exception ex) {
                _ = SSMTMessageHelper.Show(ex.ToString());
            }
        }

        private void Menu_ManuallyReverseList_ReadListFromConfig_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                string ManuallyReversePageConfigFilePath = Path.Combine(GlobalConfig.Path_ConfigsFolder, "ManuallyReversePageConfig.json");

                if (!File.Exists(ManuallyReversePageConfigFilePath))
                {
                    _ = SSMTMessageHelper.Show("当前暂无任何保存过的配置文件");
                    return;
                }

                JObject ManuallyReversePageConfigJOBJ = DBMTJsonUtils.ReadJObjectFromFile(ManuallyReversePageConfigFilePath);

                JArray IndexBufferItemListJarray = (JArray)ManuallyReversePageConfigJOBJ["IndexBufferItemList"];
                JArray CategoryBufferItemListJarray = (JArray)ManuallyReversePageConfigJOBJ["CategoryBufferItemList"];
                JArray ShapeKeyPositionBufferItemListJarray = (JArray)ManuallyReversePageConfigJOBJ["ShapeKeyPositionBufferItemList"];

                IndexBufferItemList.Clear();
                foreach (JObject jobj in IndexBufferItemListJarray)
                {
                    IndexBufferItem indexBufferItem = new IndexBufferItem();
                    indexBufferItem.Format = (string)jobj["Format"];
                    indexBufferItem.IBFilePath = (string)jobj["IBFilePath"];
                    IndexBufferItemList.Add(indexBufferItem);
                }

                CategoryBufferItemList.Clear();
                foreach (JObject jobj in CategoryBufferItemListJarray)
                {
                    CategoryBufferItem categoryBufferItem = new CategoryBufferItem();
                    categoryBufferItem.Category = (string)jobj["Category"];
                    categoryBufferItem.BufFilePath = (string)jobj["BufFilePath"];
                    CategoryBufferItemList.Add(categoryBufferItem);
                }

                ShapeKeyPositionBufferItemList.Clear();
                foreach (JObject jobj in ShapeKeyPositionBufferItemListJarray)
                {
                    ShapeKeyPositionBufferItem shapeKeyPositionBufferItem = new ShapeKeyPositionBufferItem();
                    shapeKeyPositionBufferItem.Category = (string)jobj["Category"];
                    shapeKeyPositionBufferItem.BufFilePath = (string)jobj["BufFilePath"];
                    ShapeKeyPositionBufferItemList.Add(shapeKeyPositionBufferItem);
                }

                _ = SSMTMessageHelper.Show("读取配置完成","Read Config Success");
            }
            catch(Exception ex)
            {
                _ = SSMTMessageHelper.Show(ex.ToString());
            }

        }


    }
}
