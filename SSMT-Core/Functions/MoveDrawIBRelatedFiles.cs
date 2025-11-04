using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMT
{
    public partial class CoreFunctions
    {


        public static void MoveDrawIBRelatedFiles(List<string> DrawIBList, string IBRelatedFolderName)
        {
            //TODO 这个CTX也用不了

            string OutputDrawIBRelatedFolder = GlobalConfig.Path_CurrentWorkSpaceFolder + IBRelatedFolderName + "\\FrameAnalysis-2028-08-28-666666\\";
            Directory.CreateDirectory(OutputDrawIBRelatedFolder);

            string DedupedFolderPath = OutputDrawIBRelatedFolder + "deduped\\";
            Directory.CreateDirectory(DedupedFolderPath);

            List<string> CopyDedupedFiles = new List<string>();

            //获取所有的PointlistIndex
            foreach (string DrawIB in DrawIBList)
            {
                FrameAnalysisInfo FAInfo = new FrameAnalysisInfo(DrawIB);

                string PointlistIndex = FrameAnalysisLogUtilsV2.Get_PointlistIndex_ByHash(DrawIB, FAInfo.LogFilePath);
                if (PointlistIndex == "")
                {
                    continue;
                }


                List<string> PointlistRelatedFiles = FrameAnalysisDataUtils.FilterFrameAnalysisFile(FAInfo.FolderPath, PointlistIndex, "");
                foreach (string PointlistFileName in PointlistRelatedFiles)
                {
                    CopyDedupedFiles.Add(PointlistFileName);
                    File.Copy(GlobalConfig.WorkFolder + PointlistFileName, OutputDrawIBRelatedFolder + PointlistFileName, true);
                }

                List<string> DrawIBRelatedIndexList = new List<string>();

                List<string> IndexList = FrameAnalysisLogUtilsV2.Get_DrawCallIndexList_ByHash(DrawIB, false, FAInfo.LogFilePath);
                foreach (string Index in IndexList)
                {
                    if (!DrawIBRelatedIndexList.Contains(Index))
                    {
                        DrawIBRelatedIndexList.Add(Index);
                    }
                }

                foreach (string DrawIBIndex in DrawIBRelatedIndexList)
                {
                    List<string> DrawIBRelatedFiles = FrameAnalysisDataUtils.FilterFrameAnalysisFile(GlobalConfig.WorkFolder, DrawIBIndex, "");
                    foreach (string DrawIBIndexFileName in DrawIBRelatedFiles)
                    {
                        CopyDedupedFiles.Add(DrawIBIndexFileName);
                        File.Copy(GlobalConfig.WorkFolder + DrawIBIndexFileName, OutputDrawIBRelatedFolder + DrawIBIndexFileName, true);
                    }
                }

                foreach (string FileName in CopyDedupedFiles)
                {
                    string DedupedFilePath = FrameAnalysisLogUtilsV2.Get_DedupedFilePath(FileName, FAInfo.FolderPath, FAInfo.LogFilePath);
                    string DedupedFileName = FrameAnalysisLogUtilsV2.Get_DedupedFileName(FileName, FAInfo.FolderPath, FAInfo.LogFilePath);
                    File.Copy(GlobalConfig.WorkFolder + FileName, DedupedFolderPath + DedupedFileName, true);
                }

            }


            if (File.Exists(GlobalConfig.WorkFolder + "log.txt"))
            {
                File.Copy(GlobalConfig.WorkFolder + "log.txt", OutputDrawIBRelatedFolder + "log.txt", true);
            }

            if (File.Exists(GlobalConfig.WorkFolder + "ShaderUsage.txt"))
            {
                File.Copy(GlobalConfig.WorkFolder + "ShaderUsage.txt", OutputDrawIBRelatedFolder + "ShaderUsage.txt", true);
            }
        }

    }
}
