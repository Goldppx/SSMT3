using Microsoft.UI.Xaml;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using SSMT_Core;

namespace SSMT
{
    public partial class WorkPage
    {
        public static async Task<string> Obfuscate_ModFileName(string obfusVersion = "Dev")
        {
            FileOpenPicker picker = SSMTCommandHelper.Get_FileOpenPicker(".ini");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                string readIniPath = file.Path;


                if (string.IsNullOrEmpty(readIniPath))
                {
                    _ = SSMTMessageHelper.Show("Please select a correct ini file.");
                    return "";
                }
                string readDirectoryPath = Path.GetDirectoryName(readIniPath);

                //Read every line and obfuscate every Resource section.
                //need a dict to store the old filename and the new filename.
                string[] readIniLines = File.ReadAllLines(readIniPath);
                List<string> newIniLines = new List<string>();
                Dictionary<string, string> fileNameUuidDict = new Dictionary<string, string>();
                foreach (string iniLine in readIniLines)
                {
                    string lowerIniLine = iniLine.ToLower();
                    if (lowerIniLine.StartsWith("filename") 
                        || (lowerIniLine.Contains("\\") && lowerIniLine.Contains("=") && lowerIniLine.Contains("."))
                        )
                    {
                        int firstEqualSignIndex = iniLine.IndexOf("=");
                        string valSection = iniLine.Substring(firstEqualSignIndex);
                        string resourceFileName = valSection.Substring(1).Trim();
                        //generate a uuid to replace this filename
                        string randomUUID = Guid.NewGuid().ToString();

                        //因为不能有重复键
                        if (!fileNameUuidDict.ContainsKey(resourceFileName))
                        {
                            fileNameUuidDict.Add(resourceFileName, randomUUID);
                        }
                        else
                        {
                            randomUUID = fileNameUuidDict[resourceFileName];
                        }

                        string newIniLine = "";
                        if (resourceFileName.EndsWith(".dds"))
                        {
                            if (obfusVersion == "Dev")
                            {
                                newIniLine = iniLine.Replace(resourceFileName, randomUUID + ".dds");
                            }
                            else
                            {
                                newIniLine = iniLine.Replace(resourceFileName, randomUUID + ".bundle");
                            }
                        }
                        else if (resourceFileName.EndsWith(".png"))
                        {
                            newIniLine = iniLine.Replace(resourceFileName, randomUUID + ".png");
                        }
                        else
                        {
                            newIniLine = iniLine.Replace(resourceFileName, randomUUID + ".assets");
                        }
                        newIniLines.Add(newIniLine);
                    }
                    else
                    {
                        newIniLines.Add(iniLine);

                    }
                }


                string parentDirectory = Directory.GetParent(readDirectoryPath).FullName;
                string ModFolderName = Path.GetFileName(readDirectoryPath);

                string newOutputDirectory = parentDirectory + "\\" + ModFolderName + "-Release\\";

                Directory.CreateDirectory(newOutputDirectory);

                //Create a new ini file.
                string newIniFilePath = newOutputDirectory + Guid.NewGuid().ToString() + ".ini";
                File.WriteAllLines(newIniFilePath, newIniLines);

                foreach (KeyValuePair<string, string> pair in fileNameUuidDict)
                {
                    string key = pair.Key;
                    string value = pair.Value;

                    string oldResourceFilePath = readDirectoryPath + "\\" + key;


                    string newResourceFilePath = "";
                    if (key.EndsWith(".dds"))
                    {
                        if (obfusVersion == "Dev")
                        {
                            newResourceFilePath = newOutputDirectory + value + ".dds";
                        }
                        else
                        {
                            newResourceFilePath = newOutputDirectory + value + ".bundle";
                        }
                    }
                    else if (key.EndsWith(".png"))
                    {
                        newResourceFilePath = newOutputDirectory + value + ".png";
                    }
                    else
                    {
                        newResourceFilePath = newOutputDirectory + value + ".assets";
                    }

                    if (File.Exists(oldResourceFilePath))
                    {
                        File.Copy(oldResourceFilePath, newResourceFilePath, true);
                    }

                }

                await SSMTMessageHelper.Show("混淆成功", "Obfuscated success.");

                return newIniFilePath;

            }


            return "";
        }



        public static bool DBMT_Encryption_RunCommand(string CommandString, string IniPath)
        {
       
            JObject jsonObject = new JObject();
            jsonObject["EncryptFilePath"] = IniPath;

            string ArmorSettingJsonPath = Path.Combine(ProtectConfig.Path_ConfigsFolder, "ArmorSetting.json");

            File.WriteAllText(ArmorSettingJsonPath, jsonObject.ToString());

            SSMTCommandHelper.RunPluginExeCommand(CommandString, "DBMT-Encryptor.exe");
            return true;
        }

        public async void Encryption_EncryptAll(object sender, RoutedEventArgs e)
        {
            //混淆并返回新的ini文件的路径
            string NewModInIPath = await Obfuscate_ModFileName("Play");
            if (NewModInIPath == "")
            {
                return;
            }
            //调用加密Buffer并加密ini文件
            DBMT_Encryption_RunCommand("encrypt_buffer_ini_v5", NewModInIPath);
        }


        public async void Encryption_EncryptBufferAndIni(object sender, RoutedEventArgs e)
        {
            string ini_file_path = await SSMTCommandHelper.ChooseFileAndGetPath(".ini");
            if (ini_file_path != "")
            {
                DBMT_Encryption_RunCommand("encrypt_buffer_ini_v5", ini_file_path);
            }
        }

        public async void Encryption_ObfuscatePlay(object sender, RoutedEventArgs e)
        {
            await Obfuscate_ModFileName("Play");
        }

        public async void Encryption_EncryptBuffer(object sender, RoutedEventArgs e)
        {
            string EncryptionCommand = "encrypt_buffer_acptpro_V4";

            string selected_folder_path = await SSMTCommandHelper.ChooseFolderAndGetPath();
            Debug.WriteLine("加密文件夹路径:" + selected_folder_path);

            if (selected_folder_path != "")
            {

                //判断目标路径下是否有ini文件
                // 使用Directory.GetFiles方法，并指定搜索模式为*.ini
                string[] iniFiles = Directory.GetFiles(selected_folder_path, "*.ini");
                if (iniFiles.Length == 0)
                {
                    await SSMTMessageHelper.Show("目标路径中无法找到mod的ini文件", "Target Path Can't find ini file.");
                    return;
                }


                JObject jsonObject = DBMTJsonUtils.CreateJObject();
                jsonObject["targetACLFile"] = selected_folder_path;
                DBMTJsonUtils.SaveJObjectToFile(jsonObject, ProtectConfig.Path_ACLSettingJson);

                SSMTCommandHelper.RunPluginExeCommand(EncryptionCommand, "DBMT-Encryptor.exe");

                _ = SSMTMessageHelper.Show("Buffer文件加密成功", "Buffer Files Encrypt Success.");
            }


        }

        public async void Encryption_EncryptIni(object sender, RoutedEventArgs e)
        {
            string ini_file_path = await SSMTCommandHelper.ChooseFileAndGetPath(".ini");
            if (ini_file_path != "")
            {
                DBMT_Encryption_RunCommand("encrypt_ini_acptpro_V5", ini_file_path);
            }
        }
    }
}
