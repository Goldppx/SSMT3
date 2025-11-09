using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSMT_Core;

namespace SSMT
{

    public class FrameAnalysisFileName
    {
        public string FileName { get; set; } = "";

        public string DedupedFileName { get; set; } = "";

        public string Index { get; set; } = "";

        public string Slot { get; set; } = "";
        public string VertexShaderHash { get; set; } = "";

        public string PixelShaderHash { get; set; } = "";

    }

    public static partial class HonkaiStarRail
    {

        /// <summary>
        /// d50694eedd2a8595里的UAV比较特殊，是摆过姿势之后的，所以要往前找是哪个DrawCall生成的这个UAV
        /// </summary>
        /// <param name="PointlistIndex"></param>
        /// <param name="PositionStride"></param>
        /// <param name="VertexCount"></param>
        /// <param name="Slot"></param>
        /// <returns></returns>
        private static FrameAnalysisFileName GetPrePositionUAVIndex(string PointlistIndex, int PositionStride, int VertexCount, string Slot)
        {




            //寻找Position的当前Hash值
            string CSU0BufferFileName = FrameAnalysisDataUtils.FilterFirstFile(PathManager.WorkFolder, PointlistIndex + "-" + Slot + "=", ".buf");
            string CSU0Hash = CSU0BufferFileName.Substring(10, 8);

            //这里不是查找第一个，而是查找最后一个出现的，并且Index要比PointlistIndex还小。
            string PrePositionBufferFileName = "";
            List<string> FileNameList = FrameAnalysisDataUtils.FilterFile(PathManager.WorkFolder, CSU0Hash, "-cs=4e03bd5b704abbdd.buf");
            if (FileNameList.Count >= 1)
            {
                PrePositionBufferFileName = FileNameList[FileNameList.Count - 1];
            }

            FrameAnalysisFileName FAFileName = new FrameAnalysisFileName();


            string PrePositionBufferFilePath = FrameAnalysisLogUtilsV2.Get_DedupedFilePath(PrePositionBufferFileName, PathManager.Path_LatestFrameAnalysisFolder, PathManager.Path_LatestFrameAnalysisLogTxt);

            int PrePositionSize = (int)DBMTFileUtils.GetFileSize(PrePositionBufferFilePath);
            int PRePositionVertexCount = PrePositionSize / PositionStride;

            if (PRePositionVertexCount == VertexCount)
            {
                string Index = PrePositionBufferFileName.Substring(0, 6);

                int start_pos = PrePositionBufferFileName.IndexOf("-");
                int end_pos = PrePositionBufferFileName.IndexOf("=");
                string SlotName = PrePositionBufferFileName.Substring(start_pos + 1, end_pos - start_pos - 1);

                FAFileName.Index = Index;
                FAFileName.Slot = SlotName;

                return FAFileName;
            }

            return FAFileName;
        }

        public static List<D3D11GameTypeWrapper> AutoGameTypeDetect_d50694eedd2a8595(string DrawIB, D3D11GameTypeLv2 d3D11GameTypeLv2, string PointlistIndex, List<string> TrianglelistIndexList)
        {
            List<D3D11GameTypeWrapper> PossibleD3d11GameTypeList = new List<D3D11GameTypeWrapper>();


            //先匹配出正确的数据类型，顺便得到从哪个Slot中提取的。
            foreach (D3D11GameType d3D11GameType in d3D11GameTypeLv2.Ordered_GPU_CPU_D3D11GameTypeList)
            {
                if (!d3D11GameType.GPUPreSkinning)
                {
                    continue;
                }
                string PositionSlot = "";
                string BlendSlot = "";

                D3D11GameTypeWrapper d3D11GameTypeWrapper = new D3D11GameTypeWrapper(d3D11GameType);


                LOG.Info("当前匹配数据类型: " + d3D11GameType.GameTypeName);
                //首先肯定得有vb1槽位，否则无法提取
                bool ExistsVB1Slot = false;
                foreach (var item in d3D11GameType.CategorySlotDict)
                {
                    if (item.Value == "vb1")
                    {
                        ExistsVB1Slot = true;
                        break;
                    }
                }

                if (!ExistsVB1Slot)
                {
                    continue;
                }


                //获取满足条件的TrianglelistIndex
                string TrianglelistIndex = d3D11GameTypeLv2.FilterTrianglelistIndex_UnityVS(TrianglelistIndexList, d3D11GameType);

                //获取Buffer文件
                string VB1BufferFileName = FrameAnalysisDataUtils.FilterFirstFile(PathManager.WorkFolder, TrianglelistIndex + "-vb1=", ".buf");
                string VB1BufferFilePath = FrameAnalysisLogUtilsV2.Get_DedupedFilePath(VB1BufferFileName, PathManager.Path_LatestFrameAnalysisFolder, PathManager.Path_LatestFrameAnalysisLogTxt);
                int VB1Size = (int)DBMTFileUtils.GetFileSize(VB1BufferFilePath);

                //求出预期顶点数
                int VertexCount = VB1Size / d3D11GameType.CategoryStrideDict["Texcoord"];

                int PositionStride = d3D11GameType.CategoryStrideDict["Position"];
                int BlendStride = d3D11GameType.CategoryStrideDict["Blend"];

                //随后依次判断t0到t6的Position的顶点数,对应u0到u6
                bool BlendT0 = IsBlendSlotMatch(PointlistIndex, BlendStride, VertexCount, "cs-t0");
                if (BlendT0)
                {
                    BlendSlot = "cs-t0";
                    PositionSlot = "u0";

                    //寻找Position的上一个Hash
                    FrameAnalysisFileName FAFileName = GetPrePositionUAVIndex(PointlistIndex, PositionStride, VertexCount, "u0");
                    if (FAFileName.Index != "")
                    {
                        d3D11GameTypeWrapper.PositionExtractSlot = FAFileName.Slot;
                        d3D11GameTypeWrapper.PositionExtractIndex = FAFileName.Index;

                        d3D11GameTypeWrapper.BlendExtractSlot = BlendSlot;
                        d3D11GameTypeWrapper.BlendExtractIndex = PointlistIndex;
                        PossibleD3d11GameTypeList.Add(d3D11GameTypeWrapper);
                        continue;
                    }
                }

                bool BlendT1 = IsBlendSlotMatch(PointlistIndex, BlendStride, VertexCount, "cs-t1");
                if (BlendT1)
                {
                    BlendSlot = "cs-t1";
                    PositionSlot = "u1";

                    //寻找Position的上一个Hash
                    FrameAnalysisFileName FAFileName = GetPrePositionUAVIndex(PointlistIndex, PositionStride, VertexCount, "u1");
                    if (FAFileName.Index != "")
                    {
                        d3D11GameTypeWrapper.PositionExtractSlot = FAFileName.Slot;
                        d3D11GameTypeWrapper.PositionExtractIndex = FAFileName.Index;

                        d3D11GameTypeWrapper.BlendExtractSlot = BlendSlot;
                        d3D11GameTypeWrapper.BlendExtractIndex = PointlistIndex;
                        PossibleD3d11GameTypeList.Add(d3D11GameTypeWrapper);
                        continue;
                    }
                }

                bool BlendT2 = IsBlendSlotMatch(PointlistIndex, BlendStride, VertexCount, "cs-t2");
                if (BlendT2)
                {
                    BlendSlot = "cs-t2";
                    PositionSlot = "u2";

                    //寻找Position的上一个Hash
                    FrameAnalysisFileName FAFileName = GetPrePositionUAVIndex(PointlistIndex, PositionStride, VertexCount, "u2");
                    if (FAFileName.Index != "")
                    {
                        d3D11GameTypeWrapper.PositionExtractSlot = FAFileName.Slot;
                        d3D11GameTypeWrapper.PositionExtractIndex = FAFileName.Index;

                        d3D11GameTypeWrapper.BlendExtractSlot = BlendSlot;
                        d3D11GameTypeWrapper.BlendExtractIndex = PointlistIndex;
                        PossibleD3d11GameTypeList.Add(d3D11GameTypeWrapper);
                        continue;
                    }
                }

                bool BlendT3 = IsBlendSlotMatch(PointlistIndex, BlendStride, VertexCount, "cs-t3");
                if (BlendT3)
                {
                    BlendSlot = "cs-t3";
                    PositionSlot = "u3";

                    //寻找Position的上一个Hash
                    FrameAnalysisFileName FAFileName = GetPrePositionUAVIndex(PointlistIndex, PositionStride, VertexCount, "u3");
                    if (FAFileName.Index != "")
                    {
                        d3D11GameTypeWrapper.PositionExtractSlot = FAFileName.Slot;
                        d3D11GameTypeWrapper.PositionExtractIndex = FAFileName.Index;

                        d3D11GameTypeWrapper.BlendExtractSlot = BlendSlot;
                        d3D11GameTypeWrapper.BlendExtractIndex = PointlistIndex;
                        PossibleD3d11GameTypeList.Add(d3D11GameTypeWrapper);
                        continue;
                    }
                }

                bool BlendT4 = IsBlendSlotMatch(PointlistIndex, BlendStride, VertexCount, "cs-t4");
                if (BlendT4)
                {
                    BlendSlot = "cs-t4";
                    PositionSlot = "u4";

                    //寻找Position的上一个Hash
                    FrameAnalysisFileName FAFileName = GetPrePositionUAVIndex(PointlistIndex, PositionStride, VertexCount, "u4");
                    if (FAFileName.Index != "")
                    {
                        d3D11GameTypeWrapper.PositionExtractSlot = FAFileName.Slot;
                        d3D11GameTypeWrapper.PositionExtractIndex = FAFileName.Index;

                        d3D11GameTypeWrapper.BlendExtractSlot = BlendSlot;
                        d3D11GameTypeWrapper.BlendExtractIndex = PointlistIndex;
                        PossibleD3d11GameTypeList.Add(d3D11GameTypeWrapper);
                        continue;
                    }
                }

                bool BlendT5 = IsBlendSlotMatch(PointlistIndex, BlendStride, VertexCount, "cs-t5");
                if (BlendT5)
                {
                    BlendSlot = "cs-t5";
                    PositionSlot = "u5";

                    //寻找Position的上一个Hash
                    FrameAnalysisFileName FAFileName = GetPrePositionUAVIndex(PointlistIndex, PositionStride, VertexCount, "u5");
                    if (FAFileName.Index != "")
                    {
                        d3D11GameTypeWrapper.PositionExtractSlot = FAFileName.Slot;
                        d3D11GameTypeWrapper.PositionExtractIndex = FAFileName.Index;

                        d3D11GameTypeWrapper.BlendExtractSlot = BlendSlot;
                        d3D11GameTypeWrapper.BlendExtractIndex = PointlistIndex;
                        PossibleD3d11GameTypeList.Add(d3D11GameTypeWrapper);
                        continue;
                    }
                }

                bool BlendT6 = IsBlendSlotMatch(PointlistIndex, BlendStride, VertexCount, "cs-t6");
                if (BlendT6)
                {
                    BlendSlot = "cs-t6";
                    PositionSlot = "u6";

                    //寻找Position的上一个Hash
                    FrameAnalysisFileName FAFileName = GetPrePositionUAVIndex(PointlistIndex, PositionStride, VertexCount, "u6");
                    if (FAFileName.Index != "")
                    {
                        d3D11GameTypeWrapper.PositionExtractSlot = FAFileName.Slot;
                        d3D11GameTypeWrapper.PositionExtractIndex = FAFileName.Index;

                        d3D11GameTypeWrapper.BlendExtractSlot = BlendSlot;
                        d3D11GameTypeWrapper.BlendExtractIndex = PointlistIndex;
                        PossibleD3d11GameTypeList.Add(d3D11GameTypeWrapper);
                        continue;
                    }
                }



                LOG.Info("PositionSlot: " + PositionSlot);
                LOG.Info("BlendSlot: " + BlendSlot);

                LOG.NewLine();

            }
            return PossibleD3d11GameTypeList;
        }


    }
}
