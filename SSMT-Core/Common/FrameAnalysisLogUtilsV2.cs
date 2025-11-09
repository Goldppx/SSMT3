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
    public static class FrameAnalysisLogUtilsV2
    {
        /// <summary>
        /// 缓存数据
        /// </summary>
        public static Dictionary<string, List<string>> FrameAnalysisLogFilePath_LogLineList_Dict = [];
        /// <summary>
        /// 从缓存中读取，多次读取时减少处理时间
        /// </summary>
        public static List<string> Get_LogLineList(string FrameAnalysisLogFilePath)
        {
            if (!FrameAnalysisLogFilePath_LogLineList_Dict.ContainsKey(FrameAnalysisLogFilePath))
            {
                FrameAnalysisLogFilePath_LogLineList_Dict[FrameAnalysisLogFilePath] = File.ReadAllLines(FrameAnalysisLogFilePath).ToList();
            }
            List<string> LogLineList = FrameAnalysisLogFilePath_LogLineList_Dict[FrameAnalysisLogFilePath];

            return LogLineList;
        }


        /// <summary>
        ///  缓存数据
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> FrameAnalysisFolderPath_TextureFileName_DedupedTextureFileName_Dict_Dict = [];
        /// <summary>
        /// 获取Deduped的文件名
        /// </summary>
        /// <param name="FrameAnalysisFileName"></param>
        /// <returns></returns>
        public static string Get_DedupedFileName(string FrameAnalysisFileName,string FrameAnalysisFolderPath,string FrameAnalysisLogFilePath)
        {
            //LOG.Info("Get_DedupedFileName::Start");
            List<string> LogLineList = Get_LogLineList(FrameAnalysisLogFilePath);
            //LOG.Info(LogLineList.Count.ToString());

            //老规矩，如果不包含最新的，就填充它
            if (!FrameAnalysisFolderPath_TextureFileName_DedupedTextureFileName_Dict_Dict.ContainsKey(FrameAnalysisFolderPath))
            {
                FrameAnalysisFolderPath_TextureFileName_DedupedTextureFileName_Dict_Dict[FrameAnalysisFolderPath] = new Dictionary<string, string>();

                string DumpingTexture2DStr = "Dumping Texture2D";
                string DumpingBufferStr = "Dumping Buffer";

                foreach (string logLine in LogLineList)
                {

                    if (!logLine.Contains("->"))
                    {
                        continue;
                    }

                    int findStrIndex = logLine.IndexOf(DumpingTexture2DStr);
                    if (findStrIndex >= 0)
                    {
                        string[] pathSplits = logLine.Substring(findStrIndex + DumpingTexture2DStr.Length).Split("->");
                        string originalFileName = Path.GetFileName(pathSplits[pathSplits.Length-2].Trim());
                        string dedupedFileName = Path.GetFileName(pathSplits[pathSplits.Length-1].Trim());
                        FrameAnalysisFolderPath_TextureFileName_DedupedTextureFileName_Dict_Dict[FrameAnalysisFolderPath][originalFileName] = dedupedFileName;

                    }
                    else
                    {
                        int findStr2Index = logLine.IndexOf(DumpingBufferStr);
                        if (findStr2Index >= 0)
                        {
                            string[] pathSplits = logLine.Substring(findStr2Index + DumpingBufferStr.Length).Split("->");
                            string originalFileName = Path.GetFileName(pathSplits[pathSplits.Length - 2].Trim());
                            string dedupedFileName = Path.GetFileName(pathSplits[pathSplits.Length-1].Trim());
                            FrameAnalysisFolderPath_TextureFileName_DedupedTextureFileName_Dict_Dict[FrameAnalysisFolderPath][originalFileName] = dedupedFileName;
                        }
                    }
                }
            }

            //从最新的里面获取然后返回对应值
            Dictionary<string, string> SymlinkFileName_DedupedFileName_Dict = FrameAnalysisFolderPath_TextureFileName_DedupedTextureFileName_Dict_Dict[FrameAnalysisFolderPath];
            //LOG.Info("Get_DedupedFileName::End");

            if (SymlinkFileName_DedupedFileName_Dict.ContainsKey(FrameAnalysisFileName))
            {
                return SymlinkFileName_DedupedFileName_Dict[FrameAnalysisFileName];
            }
            else
            {
                return "";
            }
        }



        /// <summary>
        /// 更方便的版本直接获取到路径，能节省在外面再包装一次的代码。
        /// </summary>
        /// <param name="FrameAnalysisFileName"></param>
        /// <returns></returns>
        public static string Get_DedupedFilePath(string FrameAnalysisFileName, string FrameAnalysisFolderPath, string FrameAnalysisLogFilePath)
        {
            //LOG.Info("Get_DedupedFilePath::Start");
            //LOG.Info("FrameAnalysisFileName: " + FrameAnalysisFileName);
            string DedupedFileName = Get_DedupedFileName(FrameAnalysisFileName,FrameAnalysisFolderPath,FrameAnalysisLogFilePath);
            //LOG.Info("DedupedFileName: " + DedupedFileName);
            if (DedupedFileName == "")
            {
                //如果无法获取Deduped文件，就把咱们普通的文件放过来
                string OriginalFrameAnalysisFilePath = Path.Combine(FrameAnalysisFolderPath, FrameAnalysisFileName);
                //LOG.Info("OriginalFrameAnalysisFilePath: " + OriginalFrameAnalysisFilePath);
                return OriginalFrameAnalysisFilePath;
            }
            else
            {
                //这里Deduped文件夹用的是最新的是因为不管是CTX还是普通架构，Deduped文件夹的位置都是固定的。
                return Path.Combine(PathManager.Path_LatestFrameAnalysisDedupedFolder, DedupedFileName);
            }

        }


        /// <summary>
        /// 通过DrawIndex来获取所有的日志行
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public static List<string> Get_LineList_ByIndex(string Index, string FrameAnalysisLogFilePath)
        {
            List<string> IndexLineList = [];

            int IndexNumber = int.Parse(Index);

            bool findIndex = false;
            List<string> LogLineList = Get_LogLineList(FrameAnalysisLogFilePath);
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
                        if (CurrentIndexNumber > IndexNumber)
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

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="DrawIB"></param>
        /// <returns></returns>
        public static string Get_PointlistIndex_ByHash(string DrawIB, string FrameAnalysisLogFilePath)
        {

            List<string> DrawCallIndexList = Get_DrawCallIndexList_ByHash(DrawIB, true ,FrameAnalysisLogFilePath);
            if (DrawCallIndexList.Count == 0)
            {
                return "";
            }

            string FirstTrianglelistIndex = DrawCallIndexList[0];
            List<string> TrianglelistIndexLileList = Get_LineList_ByIndex(FirstTrianglelistIndex,FrameAnalysisLogFilePath);

            string vb0Hash = "";
            bool findIASetVB = false;
            foreach (string CallLine in TrianglelistIndexLileList)
            {
                if (CallLine.Contains("IASetVertexBuffers") && !findIASetVB)
                {
                    IASetVertexBuffers IASetVB = new IASetVertexBuffers(CallLine);
                    findIASetVB = true;
                    continue;
                }

                if (findIASetVB)
                {
                    if (!CallLine.StartsWith("00"))
                    {
                        ShaderResource iaResource = new ShaderResource(CallLine);

                        string Slot = "vb" + iaResource.Index;

                        if (Slot == "vb0")
                        {
                            vb0Hash = iaResource.Hash;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (vb0Hash == "")
            {
                return "";
            }

            string FindStr = "hash=" + vb0Hash;
            string CurrentIndex = "";
            int TrianglelistIndexNumber = int.Parse(FirstTrianglelistIndex);

            List<string> PossibleIndexList = [];

            List<string> LogLineList = Get_LogLineList(FrameAnalysisLogFilePath);
            foreach (string Line in LogLineList)
            {
                if (Line.StartsWith("00"))
                {
                    CurrentIndex = Line.Substring(0, 6);
                }

                if (Line.Contains(FindStr))
                {
                    int PointlistIndexNumber = int.Parse(CurrentIndex);
                    if (PointlistIndexNumber < TrianglelistIndexNumber)
                    {
                        if (!PossibleIndexList.Contains(CurrentIndex))
                        {
                            PossibleIndexList.Add(CurrentIndex);
                        }
                    }

                }
            }

            if (PossibleIndexList.Count != 0)
            {
                return PossibleIndexList[PossibleIndexList.Count - 1];
            }

            return "";
        }

        public static List<string> Get_DrawCallIndexList_ByHash(string DrawIB, bool OnlyMatchFirst,string FrameAnalysisLogFilePath)
        {
            LOG.Info("Get_DrawCallIndexList_ByHash::" + DrawIB);
            List<string> LogLineList = Get_LogLineList(FrameAnalysisLogFilePath);

            HashSet<string> IndexSet = [];
            string CurrentIndex = "";
            foreach (string LogLine in LogLineList)
            {
                if (LogLine.StartsWith("00"))
                {
                    CurrentIndex = LogLine.Substring(0, 6);
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









    }
}
