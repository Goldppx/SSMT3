using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSMT_Core;
using SSMT_Core.IniParser;

namespace SSMT
{
    public class MigotoIniFile
    {
        public string IniFilePath = "";

        public List<IniSection> IniSectionList = new List<IniSection>();    

        public MigotoIniFile() { }

        public MigotoIniFile(string IniFilePath)
        {
            this.IniFilePath = IniFilePath;

            List<IniSection> iniSectionList = new List<IniSection>();

            bool MeetSectionNameLine = false;

            IniSection TmpIniSection = new IniSection();

            string[] IniLineList = File.ReadAllLines(IniFilePath);

            foreach (string IniLine in IniLineList)
            {
                string lowerIniLine = IniLine.ToLower().Trim();

                if (lowerIniLine.StartsWith("["))
                {
                    // 遇到新的Section了，要把旧的Section加入列表
                    if (MeetSectionNameLine)
                    {
                        iniSectionList.Add(TmpIniSection);
                        TmpIniSection = new IniSection();
                    }

                    string SectionName = IniLine.Trim().TrimStart('[').TrimEnd(']');
                    TmpIniSection.SectionName = SectionName;

                    MeetSectionNameLine = true;
                }
                else
                {
                    TmpIniSection.SectionLineList.Add(IniLine);
                }

            }

            // 结束时把最后一个Section也加入列表
            if (MeetSectionNameLine)
            {
                iniSectionList.Add(TmpIniSection);
            }

            this.IniSectionList = iniSectionList;
        }

        public void ReplaceSelf_IniSectionIniLine(string SectionNameLower,string IniEqualOriginalStr,string ReplaceStr)
        {
            IniEqual iniEqualOriginal = new IniEqual(IniEqualOriginalStr);
            string Key = iniEqualOriginal.LeftValueTrim.ToLower();
            string Value = iniEqualOriginal.RightValueTrim;

            List<IniSection> NewIniSectionList = new List<IniSection>();
            foreach (IniSection iniSection in this.IniSectionList)
            {
                if (iniSection.SectionName.ToLower().Trim() == SectionNameLower)
                {
                    List<string> NewIniLineList = new List<string>();
                    foreach (string iniLine in iniSection.SectionLineList)
                    {
                        IniEqual iniEqual = new IniEqual(iniLine);
                        if (iniEqual.RightValueTrim == Value && iniEqual.LeftValueTrim.ToLower() == Key)
                        {
                            NewIniLineList.Add(ReplaceStr);
                        }
                        else
                        {
                            NewIniLineList.Add(iniLine);
                        }
                    }
                    iniSection.SectionLineList = NewIniLineList;
                }
                NewIniSectionList.Add(iniSection);
            }
            this.IniSectionList = NewIniSectionList;
        }

        public void ReplaceSelf_AddNewLineIfNotExists(string SectionNameLower, string NewIniLine)
        {
            IniEqual iniEqualOriginal = new IniEqual(NewIniLine);
            string Key = iniEqualOriginal.LeftValueTrim.ToLower();
            string Value = iniEqualOriginal.RightValueTrim;

            List<IniSection> NewIniSectionList = new List<IniSection>();
            foreach (IniSection iniSection in this.IniSectionList)
            {
                if (iniSection.SectionName.ToLower().Trim() == SectionNameLower)
                {
                    List<string> NewIniLineList = new List<string>();
                    bool Exists = false;
                    foreach (string iniLine in iniSection.SectionLineList)
                    {
                        IniEqual iniEqual = new IniEqual(iniLine);
                        if (iniEqual.RightValueTrim == Value && iniEqual.LeftValueTrim.ToLower() == Key)
                        {
                            Exists = true;
                        }
                        NewIniLineList.Add(iniLine);
                    }

                    if (!Exists)
                    {
                        NewIniLineList.Add(NewIniLine);
                    }

                    iniSection.SectionLineList = NewIniLineList;
                }
                NewIniSectionList.Add(iniSection);
            }
            this.IniSectionList = NewIniSectionList;
        }


        public List<IniResource> GetTextureResourceList()
        {
            List<IniResource> TextureResourceList = new List<IniResource>();
            List<IniResource> TotalResourceList = ParseSelfResourceList();
            foreach (IniResource resource in TotalResourceList) {
                if (resource.Stride == "" && resource.Format == "" && resource.FileName != "" && resource.Type == "")
                {
                    TextureResourceList.Add(resource);
                }
            }
            return TextureResourceList;
        }

        public List<IniResource> ParseSelfResourceList()
        {
            List<IniResource> iniResourceList = new List<IniResource>();

            foreach (IniSection iniSection in this.IniSectionList)
            {
                if (!iniSection.SectionName.ToLower().StartsWith("resource"))
                {
                    continue;
                }
                //LOG.Info(iniSection.SectionName);

                IniResource iniResource = new IniResource();
                foreach (string iniLine in iniSection.SectionLineList)
                {
                    string lowerIniLine = iniLine.ToLower().Trim();
                    if (lowerIniLine.StartsWith("type"))
                    {
                        IniEqual iniEqual = new IniEqual(iniLine);
                        iniResource.Type = iniEqual.RightValueTrim;
                    }
                    else if (lowerIniLine.StartsWith("stride"))
                    {
                        IniEqual iniEqual = new IniEqual(iniLine);
                        iniResource.Stride = iniEqual.RightValueTrim;

                    }
                    else if (lowerIniLine.StartsWith("format"))
                    {
                        IniEqual iniEqual = new IniEqual(iniLine);
                        iniResource.Format = iniEqual.RightValueTrim;
                    }
                    else if (lowerIniLine.StartsWith("filename"))
                    {
                        IniEqual iniEqual = new IniEqual(iniLine);
                        iniResource.FileName = iniEqual.RightValueTrim;
                    }
                }

                if (iniResource.FileName != "")
                {
                    string iniLocation = Path.GetDirectoryName(this.IniFilePath);
                    string ResourceFilePath = Path.Combine(iniLocation, iniResource.FileName);
                    iniResource.FilePath = ResourceFilePath;
                }

                iniResourceList.Add(iniResource);

            }

            return iniResourceList;
        }
     

        public List<ModKey> ParseSelf_ModKeyList()
        {
            List<ModKey> modKeyList = new List<ModKey>();

            foreach (IniSection iniSection in this.IniSectionList)
            {
                if (!iniSection.SectionName.ToLower().StartsWith("key"))
                {
                    continue;
                }
                //LOG.Info(iniSection.SectionName);

                ModKey modKey = new ModKey();
                foreach (string iniLine in iniSection.SectionLineList)
                {
                    string lowerIniLine = iniLine.ToLower().Trim();
                    if (lowerIniLine.StartsWith("key"))
                    {
                        IniEqual iniEqual = new IniEqual(iniLine);
                        modKey.KeyNameList.Add(iniEqual.RightValue);
                    }else if (lowerIniLine.StartsWith("type"))
                    {
                        IniEqual iniEqual = new IniEqual(iniLine);
                        modKey.KeyType = iniEqual.RightValue;
                    }
                    else if (lowerIniLine.StartsWith("$"))
                    {
                        IniEqual iniEqual = new IniEqual(iniLine);
                        modKey.KeyValue = iniEqual.RightValue;
                    }
                }

                foreach (string KeyName in modKey.KeyNameList)
                {
                    if (modKey.KeyName == "")
                    {
                        modKey.KeyName = KeyName;
                    }
                    else
                    {
                        modKey.KeyName = modKey.KeyName + " 或 " + KeyName;
                    }
                }
                //LOG.Info(modKey.KeyName);
                modKeyList.Add(modKey);

            }

            LOG.Info(modKeyList.Count.ToString());

            return modKeyList;
        }

        public void SaveSelf()
        {
            SaveToIniFile(this.IniFilePath);
        }

        public void SaveToIniFile(string TargetIniFilePath)
        {
            List<string> NewIniLineList = new List<string>();
            foreach (IniSection iniSection in this.IniSectionList)
            {
                NewIniLineList.Add("[" + iniSection.SectionName + "]");

                foreach (string iniLine in iniSection.SectionLineList)
                {
                    NewIniLineList.Add(iniLine);
                }

            }

            File.WriteAllLines(TargetIniFilePath, NewIniLineList);
        }

    }
}
