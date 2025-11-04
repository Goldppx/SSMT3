using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using SSMT_Core;


namespace SSMT
{
    public static class FrameAnalysisLogUtils
    {
        /// <summary>
        /// Cache data
        /// </summary>
        public static Dictionary<string,List<string>> FrameAnalysisFolderName_LogLineList_Dict = [];

        /// <summary>
        ///  Cache data
        /// </summary>
        public static Dictionary<string,Dictionary<string,string>> FrameAnalysisFolderName_TextureFileName_DedupedTextureFileName_Dict_Dict = [];

        public static Dictionary<string, Dictionary<string, string>> FrameAnalysisFolderName_SymlinkFileName_DedupedFileName_Dict = [];

        /// <summary>
        /// 所有方法都必须调用此方法判断是否已初始化最新的FrameAnalysis文件夹的内容
        /// 否则可能会读取到旧的FrameAnalysis文件夹的内容导致结果不正确
        /// </summary>
        public static List<string> Get_LatestLogLineList()
        {
            if (!FrameAnalysisFolderName_LogLineList_Dict.ContainsKey(GlobalConfig.LatestFrameAnalysisFolderName))
            {
                FrameAnalysisFolderName_LogLineList_Dict[GlobalConfig.LatestFrameAnalysisFolderName] = File.ReadAllLines(GlobalConfig.Path_LatestFrameAnalysisLogTxt).ToList();
            }
            List<string> LogLineList = FrameAnalysisFolderName_LogLineList_Dict[GlobalConfig.LatestFrameAnalysisFolderName];

            return LogLineList;
        }


        public static List<string> Get_DrawCallIndexList_ByHash(string DrawIB,bool OnlyMatchFirst)
        {
            Debug.WriteLine("Get_DrawCallIndexList_ByHash::" + DrawIB);
            List<string> LogLineList = Get_LatestLogLineList();

            HashSet<string> IndexSet = [];
            string CurrentIndex = "";
            foreach(string LogLine in LogLineList)
            {
                if (LogLine.StartsWith("00"))
                {
                    CurrentIndex = LogLine.Substring(0,6);
                }

                if (LogLine.Contains("hash=" + DrawIB))
                {
                    Debug.WriteLine("Find Hash: " + LogLine);
                    IndexSet.Add(CurrentIndex);

                    if (OnlyMatchFirst)
                    {
                        break;
                    }
                }
            }

            List<string> IndexList = [];
            foreach (string Index in IndexSet)
            {
                IndexList.Add(Index);
            }
            Debug.WriteLine("Get_DrawCallIndexList_ByHash::" + DrawIB + "  End");

            return IndexList;
        }


        /// <summary>
        /// 从FrameAnalysis文件夹中根据DrawIB和ComponentName读取DrawCall的Index列表
        /// 强依赖于FrameAnalysis文件夹这log.txt的存在
        /// </summary>
        /// <param name="DrawIB"></param>
        /// <param name="ComponentName"></param>
        /// <returns></returns>
        public static List<string> Get_DrawCallIndexList_ByMatchFirstIndex(string DrawIB, UInt64 MatchFirstIndex)
        {

            //找到所有与该DrawIB有关的DrawCallIndex
            List<string> IndexList = Get_DrawCallIndexList_ByHash(DrawIB,false);
            List<string> DrawCallIndexList = new List<string>();

            //Foreach every index to see it's match_first_index.
            foreach (string Index in IndexList)
            {
                string IBTxtFileName = FrameAnalysisDataUtils.FilterFirstFile(GlobalConfig.WorkFolder, Index + "-ib", ".txt");
                IndexBufferTxtFile IBTxtFile = new IndexBufferTxtFile(GlobalConfig.WorkFolder + IBTxtFileName, false);

                if (IBTxtFile.Topology != "trianglelist")
                {
                    continue;
                }

                UInt64 FileMatchFirstIndex = UInt64.Parse(IBTxtFile.FirstIndex);
                if (FileMatchFirstIndex == MatchFirstIndex)
                {
                    DrawCallIndexList.Add(IBTxtFile.Index);
                }
            }

            return DrawCallIndexList;
        }


        /// <summary>
        /// 通过DrawIndex来获取所有的日志行
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public static List<string> Get_LineList_ByIndex(string Index)
        {
            List<string> IndexLineList = [];
            
            int IndexNumber = int.Parse(Index);

            bool findIndex = false;
            List<string> LogLineList = Get_LatestLogLineList();
            foreach (string Line in LogLineList)
            {
                if (Line.StartsWith("00") && !findIndex)
                {
                    int CurrentIndexNumber = int.Parse(Line.Substring(0, 6));
                    if (CurrentIndexNumber == IndexNumber)
                    {
                        findIndex = true;
                        IndexLineList.Add(Line);
                        continue;
                    }
                }

                if (findIndex)
                {
                    if (Line.StartsWith("00"))
                    {
                        int CurrentIndexNumber = int.Parse(Line.Substring(0, 6));
                        if(CurrentIndexNumber > IndexNumber)
                        {
                            break;
                        }
                        else
                        {
                            IndexLineList.Add(Line);
                        }
                    }
                    else
                    {
                        IndexLineList.Add(Line);
                    }
                }

            }

            return IndexLineList;
        }
        public static Dictionary<string, string> Get_ComputeShaderSlotHashMap_FromCSSetShaderResources_ByIndex(string ExtractIndex)
        {
            string CommandStr = "CSSetShaderResources";
            LOG.Info("Get_ComputeShaderSlotHashMap_FromCSSetShaderResources_ByIndex:" + ExtractIndex);
            Dictionary<string, string> CategorySlot_Hash_Map = new Dictionary<string, string>();
            bool findCSSetShaderResources = false;

            List<string> LogLineList = Get_LineList_ByIndex(ExtractIndex);
            foreach (string CallLine in LogLineList)
            {
                string CallLineTrim = CallLine.Trim();
                if (findCSSetShaderResources)
                {
                    LOG.Info("FindCSSetShaderResources");
                    if (!CallLineTrim.StartsWith("00"))
                    {
                        LOG.Info("Processing " + CallLine);
                        ShaderResource shaderResource = new ShaderResource(CallLine);
                        string Slot = "cs-t" + shaderResource.Index;
                        LOG.Info("Slot: " + Slot + " Hash: " + shaderResource.Hash);
                        CategorySlot_Hash_Map[Slot] = shaderResource.Hash;

                    }else if (CallLine.Contains(CommandStr))
                    {

                    }
                    else
                    {
                        LOG.Info("Set FindCSSetShaderResources = False");
                        findCSSetShaderResources = false;
                    }
                }else if (CallLine.Contains(CommandStr))
                {
                    LOG.Info("Processing: " + CallLine);
                    //CSSetShaderResources CSSetSR(CallLine);
                    findCSSetShaderResources = true;
                }


            }


            LOG.NewLine();
            return CategorySlot_Hash_Map;
        }

        public static Dictionary<string,string> Get_VBCategoryHashMap_FromIASetVertexBuffer_ByIndex(string ExtractIndex)
        {

            Dictionary<string, string> CategorySlot_Hash_Map = new Dictionary<string, string>();
            bool findIASetVB = false;

            List<string> LineList = Get_LineList_ByIndex(ExtractIndex);
            foreach (string CallLine in LineList)
            {
                string TrimCallLine = CallLine.Trim();
                if (findIASetVB)
                {
                    if (!CallLine.StartsWith("00"))
                    {
                        ShaderResource shaderResource = new ShaderResource(CallLine);
                        string Slot = "vb" + shaderResource.Index;
                        CategorySlot_Hash_Map[Slot] = shaderResource.Hash;

                    }else if (CallLine.Contains("IASetVertexBuffers"))
                    {

                    }
                    else
                    {
                        findIASetVB = false;
                    }

                }
                else if (CallLine.Contains("IASetVertexBuffers"))
                {
                    //IASetVertexBuffers iASetVertexBuffers = new IASetVertexBuffers(CallLine);
                    findIASetVB = true;
                }
            }



            return CategorySlot_Hash_Map;
        }

    }
}
