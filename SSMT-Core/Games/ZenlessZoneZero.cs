using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSMT_Core;

namespace SSMT
{
    public static class ZenlessZoneZero
    {

        public static string FilterTrianglelistIndex_UnityVS(List<string> TrianglelistIndexList, D3D11GameType d3D11GameType)
        {
            string FinalTrianglelistIndex = "";
            foreach (string TrianglelistIndex in TrianglelistIndexList)
            {
                bool AllSlotExists = true;
                foreach (var item in d3D11GameType.CategoryTopologyDict)
                {
                    string Category = item.Key;
                    string Topology = item.Value;

                    if (Topology != "trianglelist")
                    {
                        continue;
                    }

                    //获取当前Category的Slot
                    string CategorySlot = d3D11GameType.CategorySlotDict[Category];

                    //寻找对应Buf文件
                    string CategoryBufFileName = FrameAnalysisDataUtils.FilterFirstFile(PathManager.WorkFolder, TrianglelistIndex + "-" + CategorySlot, ".buf");
                    if (CategoryBufFileName == "")
                    {
                        AllSlotExists = false;
                        break;
                    }
                }

                if (AllSlotExists)
                {
                    FinalTrianglelistIndex = TrianglelistIndex;
                    break;
                }
            }

            return FinalTrianglelistIndex;
        }


        public static List<D3D11GameType> GetPossibleGameTypeList_UnityVS(string DrawIB, string PointlistIndex, List<string> TrianglelistIndexList, D3D11GameTypeLv2 d3D11GameTypeLv2)
        {
            List<D3D11GameType> PossibleGameTypeList = new List<D3D11GameType>();

            bool findAtLeastOneGPUType = false;
            foreach (D3D11GameType d3D11GameType in d3D11GameTypeLv2.Ordered_GPU_CPU_D3D11GameTypeList)
            {
                if (findAtLeastOneGPUType && !d3D11GameType.GPUPreSkinning)
                {
                    LOG.Info("自动优化:已经找到了满足条件的GPU类型，所以这个CPU类型就不用判断了");
                    continue;
                }

                LOG.Info("当前数据类型:" + d3D11GameType.GameTypeName);

                //传递过来一堆TrianglelistIndex，但是我们要找到满足条件的那个,即Buffer文件都存在的那个
                string TrianglelistIndex = FilterTrianglelistIndex_UnityVS(TrianglelistIndexList, d3D11GameType);
                LOG.Info("TrianglelistIndex: " + TrianglelistIndex);


                if (TrianglelistIndex == "")
                {
                    LOG.Info("当前GameType无法找到符合槽位存在条件的TrianglelistIndex，跳过此项");
                    continue;
                }

                //获取每个Category的Buffer文件
                Dictionary<string, string> CategoryBufFileMap = new Dictionary<string, string>();
                Dictionary<string, int> CategoryBufFileSizeMap = new Dictionary<string, int>();
                bool AllFileExists = true;
                foreach (var item in d3D11GameType.CategoryTopologyDict)
                {
                    string CategoryName = item.Key;
                    string ExtractIndex = TrianglelistIndex;
                    if (item.Value == "pointlist" && PointlistIndex != "")
                    {
                        ExtractIndex = PointlistIndex;
                    }
                    string CategorySlot = d3D11GameType.CategorySlotDict[CategoryName];
                    LOG.Info("当前分类:" + CategoryName + " 提取Index: " + ExtractIndex + " 提取槽位:" + CategorySlot);
                    //获取文件名存入对应Dict
                    string CategoryBufFileName = FrameAnalysisDataUtils.FilterFirstFile(PathManager.WorkFolder, ExtractIndex + "-" + CategorySlot, ".buf");
                    CategoryBufFileMap[item.Key] = CategoryBufFileName;
                    LOG.Info("CategoryBufFileName: " + CategoryBufFileName);

                    //获取文件大小存入对应Dict
                    string CategoryBufFilePath = FrameAnalysisLogUtilsV2.Get_DedupedFilePath(CategoryBufFileName, PathManager.Path_LatestFrameAnalysisFolder, PathManager.Path_LatestFrameAnalysisLogTxt);
                    LOG.Info("Category: " + item.Key + " File:" + CategoryBufFilePath);
                    if (!File.Exists(CategoryBufFilePath))
                    {
                        LOG.Error("对应Buffer文件未找到,此数据类型无效。");
                        AllFileExists = false;
                        break;
                    }

                    long FileSize = DBMTFileUtils.GetFileSize(CategoryBufFilePath);
                    CategoryBufFileSizeMap[item.Key] = (int)FileSize;
                }

                if (!AllFileExists)
                {
                    LOG.Info("当前数据类型的部分槽位文件无法找到，跳过此数据类型识别。");
                    continue;
                }

                //校验顶点数是否在各Buffer中保持一致
                //TODO 通过校验顶点数的方式并不能100%确定，因为如果只有一个Category的话就会无法匹配步长
                int VertexNumber = 0;
                bool AllMatch = true;

                foreach (string CategoryName in d3D11GameType.OrderedCategoryNameList)
                {
                    int CategoryStride = d3D11GameType.CategoryStrideDict[CategoryName];
                    int FileSize = CategoryBufFileSizeMap[CategoryName];
                    int TmpNumber = FileSize / CategoryStride;

                    if (TmpNumber == 0)
                    {
                        LOG.Info("槽位的文件大小不能为0，槽位匹配失败，跳过此数据类型");
                        AllMatch = false;
                        break;
                    }

                    //如果不是GPU PreSkinning的话，就要去校验顶点数
                    if (!d3D11GameType.GPUPreSkinning)
                    {
                        LOG.Info("不是GPU-PreSkinning，现在开始校验余数：");
                        //IdentityV: 使用精准匹配机制来过滤数据类型，如果有余数，说明此分类不匹配。
                        int YuShu = FileSize % CategoryStride;
                        if (YuShu != 0)
                        {
                            LOG.Error("余数不为0: " + YuShu.ToString() + "  ，文件步长除以类别步长，不能含有余数，否则为不支持的匹配方式，比如PatchNull，或者数据类型匹配错误，类型错误时自然会产生余数。");
                            AllMatch = false;
                            break;
                        }

                        LOG.Info("FileSize: " + FileSize.ToString());
                        LOG.Info("CategoryStride: " + CategoryStride.ToString());
                        LOG.Info("YuShu: " + YuShu.ToString());
                        LOG.Info("余数校验通过");
                    }





                    if (VertexNumber == 0)
                    {
                        VertexNumber = TmpNumber;
                    }
                    else if (VertexNumber != TmpNumber)
                    {
                        LOG.Info("VertexNumber: " + VertexNumber.ToString() + " 当前槽位数量: " + TmpNumber.ToString());
                        LOG.Info("槽位匹配失败");
                        LOG.NewLine();

                        AllMatch = false;
                        break;
                    }
                    else
                    {


                        LOG.Info(CategoryName + " Match!");
                        LOG.NewLine();

                        ////如果是GPU-PreSkinning，并且槽位是Texcoord，那么需要去读取txt文件进行额外的校验。
                        if (d3D11GameType.GPUPreSkinning && CategoryName == "Texcoord")
                        {
                            string CategorySlot = d3D11GameType.CategorySlotDict[CategoryName];
                            string CategoryTxtFileName = FrameAnalysisDataUtils.FilterFirstFile(PathManager.WorkFolder, PointlistIndex + "-" + CategorySlot, ".txt");
                            if (CategoryTxtFileName == "")
                            {
                                LOG.Info("槽位的txt文件不存在，跳过此数据类型。");
                                AllMatch = false;
                                break;
                            }
                            else
                            {
                                string CategoryTxtFilePath = FrameAnalysisLogUtilsV2.Get_DedupedFilePath(CategoryTxtFileName, PathManager.Path_LatestFrameAnalysisFolder, PathManager.Path_LatestFrameAnalysisLogTxt);
                                //需要VertexBufferTxtFile的解析
                                //要想想更简单的方法
                                VertexBufferTxtFile VBTxtFile = new VertexBufferTxtFile(CategoryTxtFilePath);

                                //获取当前数据类型对应Category的所有Element，过滤保留Texcoord进行处理判断
                                bool AllByteWidthMatch = true;
                                foreach (var item in d3D11GameType.ElementNameD3D11ElementDict)
                                {
                                    string ElementName = item.Key;
                                    D3D11Element originalD3D11Element = item.Value;

                                    //如果不是Texcoord就不处理了
                                    if (originalD3D11Element.Category != "Texcoord")
                                    {
                                        continue;
                                    }

                                    //如果是，就从txt文件里读取，判断ByteWidth是否相同
                                    if (VBTxtFile.ElementName_D3D11Element_Dict.ContainsKey(ElementName))
                                    {
                                        D3D11Element d3D11Element = VBTxtFile.ElementName_D3D11Element_Dict[ElementName];
                                        if (originalD3D11Element.ByteWidth != d3D11Element.ByteWidth)
                                        {
                                            LOG.Info("进行Txt校验发现步长不相等");
                                            AllByteWidthMatch = false;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        //不包含这个Element也不代表就一定是错误的，我们只在包含的时候进行判断即可
                                        //AllByteWidthMatch = false;
                                        //break;
                                    }

                                }

                                if (!AllByteWidthMatch)
                                {
                                    LOG.Info("读取Txt校验完成，数据类型不符合。");

                                    //此时已经知道不符合的情况下，我们再去进行校验Element的总长度是否符合

                                    int VBTxtDataElementLength = 0;
                                    int VBTxtElementNumber = 0;
                                    foreach (string ElementName in VBTxtFile.VertexDataShowElementList)
                                    {
                                        LOG.Info("ElementName:" + ElementName);
                                        if (VBTxtFile.ElementName_D3D11Element_Dict.ContainsKey(ElementName))
                                        {
                                            D3D11Element d3D11Element = VBTxtFile.ElementName_D3D11Element_Dict[ElementName];
                                            VBTxtDataElementLength = VBTxtDataElementLength + d3D11Element.ByteWidthInt;
                                            VBTxtElementNumber = VBTxtElementNumber + 1;
                                        }
                                    }

                                    LOG.Info("VBTxtDataElementLength::" + VBTxtDataElementLength.ToString());
                                    LOG.Info("VBTxtElementNumber::" + VBTxtElementNumber.ToString());

                                    int GameTypeElementLength = 0;
                                    int GameTypeElementNumber = 0;
                                    foreach (var item in d3D11GameType.ElementNameD3D11ElementDict)
                                    {
                                        string ElementName = item.Key;
                                        D3D11Element originalD3D11Element = item.Value;

                                        //如果不是Texcoord就不处理了
                                        if (originalD3D11Element.Category != "Texcoord")
                                        {
                                            continue;
                                        }
                                        GameTypeElementLength = GameTypeElementLength + originalD3D11Element.ByteWidthInt;
                                        GameTypeElementNumber = GameTypeElementNumber + 1;
                                    }

                                    LOG.Info("GameTypeElementLength::" + GameTypeElementLength.ToString());
                                    LOG.Info("GameTypeElementNumber::" + GameTypeElementNumber.ToString());

                                    if (VBTxtDataElementLength == GameTypeElementLength && VBTxtElementNumber == GameTypeElementNumber)
                                    {
                                        LOG.Info("虽然Element匹配失败，但是计算Txt文件的总步长和我们Texcoord总步长一致，且Element数量相同，所以认为很大概率可以匹配");
                                    }
                                    else
                                    {
                                        AllMatch = false;
                                        break;
                                    }

                                }
                            }
                        }
                        //}
                        //else
                        //{
                        //    LOG.Info(CategoryName + " Match!");
                        //    LOG.NewLine();
                        //}

                    }
                }

                //LOG.Info("VertexNumber: " + VertexNumber.ToString());

                //上面是对每个分类进行校验，现在检查如果只有一个分类的话
                if (!d3D11GameType.GPUPreSkinning && d3D11GameType.CategorySlotDict.Count == 1)
                {
                    string CategorySlot = d3D11GameType.CategorySlotDict[d3D11GameType.OrderedCategoryNameList[0]];
                    string CategoryTxtFileName = FrameAnalysisDataUtils.FilterFirstFile(PathManager.WorkFolder, TrianglelistIndex + "-" + CategorySlot, ".txt");
                    if (CategoryTxtFileName == "")
                    {
                        LOG.Info("槽位的txt文件不存在，跳过此数据类型。");
                        AllMatch = false;
                    }

                    //如果是CPU并且只有一个分类，则进行校验步长。
                    int GameTypeStride = d3D11GameType.GetSelfStride();
                    LOG.Info("CategoryTxtFileName: " + CategoryTxtFileName);
                    string CategoryTxtFilePath = FrameAnalysisLogUtilsV2.Get_DedupedFilePath(CategoryTxtFileName, PathManager.Path_LatestFrameAnalysisFolder, PathManager.Path_LatestFrameAnalysisLogTxt);
                    LOG.Info("CategoryTxtFilePath: " + CategoryTxtFilePath);

                    string VertexCountTxtShow = DBMTFileUtils.FindMigotoIniAttributeInFile(CategoryTxtFilePath, "vertex count");
                    string ShowStride = DBMTFileUtils.FindMigotoIniAttributeInFile(CategoryTxtFilePath, "stride");
                    if (ShowStride.Trim() != "")
                    {
                        int ShowStrideCount = int.Parse(ShowStride);
                        
                        LOG.Info("ShowStrideCount: " + ShowStrideCount + " 数据类型Stride: " + GameTypeStride);
                        if (ShowStrideCount != GameTypeStride)
                        {
                            LOG.Info("槽位的txt文件Stride与Buffer数据类型统Stride不符，跳过此数据类型。");
                            AllMatch = false;
                        }
                    }

                }


                if (AllMatch)
                {
                    LOG.NewLine("MatchGameType: " + d3D11GameType.GameTypeName);
                    PossibleGameTypeList.Add(d3D11GameType);
                }

                //如果找到了一个GPUPreSkinning就标记一下，这样后面就不会匹配CPU类型了。
                if (!findAtLeastOneGPUType)
                {
                    foreach (D3D11GameType d3d11GameType in PossibleGameTypeList)
                    {
                        if (d3d11GameType.GPUPreSkinning)
                        {
                            findAtLeastOneGPUType = true;
                            break;
                        }
                    }
                }

            }

            //如果未识别到任何数据类型，则尝试全自动数据类型识别？
            if (PossibleGameTypeList.Count == 0)
            {

            }


            bool AllCPUType = true;
            int MaxCategoryNumber = 0;
            if (PossibleGameTypeList.Count != 0)
            {
                LOG.Info("All Matched GameType:");
                foreach (D3D11GameType d3d11GameType in PossibleGameTypeList)
                {
                    if (d3d11GameType.GPUPreSkinning)
                    {
                        AllCPUType = false;
                    }

                    if (d3d11GameType.CategorySlotDict.Count > MaxCategoryNumber)
                    {
                        MaxCategoryNumber = d3d11GameType.CategorySlotDict.Count;
                    }
                    LOG.Info(d3d11GameType.GameTypeName);
                }
            }


            if (AllCPUType)
            {
                LOG.Info("由于全部都是CPU类型，所以现在过滤最大Category数量的");
                List<D3D11GameType> FilteredGameTypeList = new List<D3D11GameType>();

                foreach (D3D11GameType d3d11GameType in PossibleGameTypeList)
                {
                    if (d3d11GameType.CategorySlotDict.Count != MaxCategoryNumber)
                    {
                        continue;
                    }

                    FilteredGameTypeList.Add(d3d11GameType);
                }

                PossibleGameTypeList = FilteredGameTypeList;
            }




            //如果自动识别
            if (PossibleGameTypeList.Count == 0)
            {
                ErrorUtils.ShowCantFindDataTypeError(DrawIB);
            }

            return PossibleGameTypeList;
        }

        private static bool Extract_fee307b98a965c16(string DrawIB, D3D11GameTypeLv2 d3D11GameTypeLv2, string PointlistIndex, List<string> TrianglelistIndexList)
        {

            //接下来开始识别可能的数据类型。
            //此时需要先读取所有存在的数据类型。
            //此时需要我们先去生成几个数据类型用于测试。
            //还有就是数据类型的文件夹是存在哪里的
            List<D3D11GameType> PossibleD3D11GameTypeList = GetPossibleGameTypeList_UnityVS(DrawIB, PointlistIndex, TrianglelistIndexList, d3D11GameTypeLv2);

            if (PossibleD3D11GameTypeList.Count == 0)
            {
                return false;
            }


            //接下来提取出每一种可能性
            //读取一个MatchFirstIndex_IBFileName_Dict
            SortedDictionary<int, string> MatchFirstIndex_IBTxtFileName_Dict = new SortedDictionary<int, string>();
            foreach (string TrianglelistIndex in TrianglelistIndexList)
            {
                string IBFileName = FrameAnalysisDataUtils.FilterFirstFile(PathManager.WorkFolder, TrianglelistIndex + "-ib", ".txt");
                if (IBFileName == "")
                {
                    continue;
                }
                string IBFilePath = FrameAnalysisLogUtilsV2.Get_DedupedFilePath(IBFileName, PathManager.Path_LatestFrameAnalysisFolder, PathManager.Path_LatestFrameAnalysisLogTxt);
                IndexBufferTxtFile IBTxtFile = new IndexBufferTxtFile(IBFilePath, false);
                MatchFirstIndex_IBTxtFileName_Dict[int.Parse(IBTxtFile.FirstIndex)] = IBFileName;
            }

            foreach (var item in MatchFirstIndex_IBTxtFileName_Dict)
            {
                LOG.Info("MatchFirstIndex: " + item.Key.ToString() + " IBFileName: " + item.Value);
            }
            LOG.NewLine();

            foreach (D3D11GameType d3D11GameType in PossibleD3D11GameTypeList)
            {
                string TrianglelistIndex = d3D11GameTypeLv2.FilterTrianglelistIndex_UnityVS(TrianglelistIndexList, d3D11GameType);

                Dictionary<string, string> CategoryBufFileMap = new Dictionary<string, string>();
                foreach (var item in d3D11GameType.CategoryTopologyDict)
                {
                    string ExtractIndex = TrianglelistIndex;
                    if (item.Value == "pointlist" && PointlistIndex != "")
                    {
                        ExtractIndex = PointlistIndex;
                    }
                    string CategorySlot = d3D11GameType.CategorySlotDict[item.Key];

                    //获取文件名存入对应Dict
                    string CategoryBufFileName = FrameAnalysisDataUtils.FilterFirstFile(PathManager.WorkFolder, ExtractIndex + "-" + CategorySlot, ".buf");
                    CategoryBufFileMap[item.Key] = CategoryBufFileName;
                }

                string GameTypeFolderName = "TYPE_" + d3D11GameType.GameTypeName;
                string DrawIBFolderPath = Path.Combine(PathManager.Path_CurrentWorkSpaceFolder, DrawIB + "\\");
                string GameTypeOutputPath = Path.Combine(DrawIBFolderPath, GameTypeFolderName + "\\");
                if (!Directory.Exists(GameTypeOutputPath))
                {
                    Directory.CreateDirectory(GameTypeOutputPath);
                }

                LOG.Info("开始从各个Buffer文件中读取数据:");
                //接下来从各个Buffer中读取并且拼接为FinalVB0

                List<Dictionary<int, byte[]>> BufDictList = new List<Dictionary<int, byte[]>>();
                foreach (var item in CategoryBufFileMap)
                {
                    string CategoryName = item.Key;
                    string CategoryBufFileName = item.Value;
                    string CategoryBufFilePath = FrameAnalysisLogUtilsV2.Get_DedupedFilePath(CategoryBufFileName, PathManager.Path_LatestFrameAnalysisFolder, PathManager.Path_LatestFrameAnalysisLogTxt);
                    int CategoryStride = d3D11GameType.CategoryStrideDict[CategoryName];

                    Dictionary<int, byte[]> BufDict = DBMTBinaryUtils.ReadBinaryFileByStride(CategoryBufFilePath, CategoryStride);
                    BufDictList.Add(BufDict);
                }
                LOG.NewLine();

                Dictionary<int, byte[]> MergedVB0Dict = DBMTBinaryUtils.MergeByteDicts(BufDictList);
                int OriginalVertexCount = MergedVB0Dict.Count;
                byte[] FinalVB0 = DBMTBinaryUtils.MergeDictionaryValues(MergedVB0Dict);

                //接下来遍历MatchFirstIndex_IBFileName的Map，对于每个MarchFirstIndex
                //都读取IBTxt文件里的数值，然后进行分割并输出。
                int OutputCount = 1;
                foreach (var item in MatchFirstIndex_IBTxtFileName_Dict)
                {
                    int MatchFirstIndex = item.Key;
                    string IBTxtFileName = item.Value;
                    //拼接出一个IBBufFileName
                    string IBBufFileName = Path.GetFileNameWithoutExtension(IBTxtFileName) + ".buf";
                    LOG.Info(IBBufFileName);


                    string IBTxtFilePath = FrameAnalysisLogUtilsV2.Get_DedupedFilePath(IBTxtFileName, PathManager.Path_LatestFrameAnalysisFolder, PathManager.Path_LatestFrameAnalysisLogTxt);
                    string IBBufFilePath = FrameAnalysisLogUtilsV2.Get_DedupedFilePath(IBBufFileName, PathManager.Path_LatestFrameAnalysisFolder, PathManager.Path_LatestFrameAnalysisLogTxt);

                    IndexBufferTxtFile IBTxtFile = new IndexBufferTxtFile(IBTxtFilePath, true);
                    LOG.Info(IBTxtFilePath);
                    LOG.Info("FirstIndex: " + IBTxtFile.FirstIndex);
                    LOG.Info("IndexCount: " + IBTxtFile.IndexCount);

                    string NamePrefix = DrawIB + "-" + OutputCount.ToString();

                    string OutputIBBufFilePath = Path.Combine(GameTypeOutputPath, NamePrefix + ".ib");
                    string OutputVBBufFilePath = Path.Combine(GameTypeOutputPath, NamePrefix + ".vb");
                    string OutputFmtFilePath = Path.Combine(GameTypeOutputPath, NamePrefix + ".fmt");

                    //通过D3D11GameType合成一个FMT文件并且输出
                    FmtFile fmtFile = new FmtFile(d3D11GameType);
                    fmtFile.RotateAngle = true;
                    fmtFile.OutputFmtFile(OutputFmtFilePath);

                    //写出IBBufFile
                    IndexBufferBufFile IBBufFile = new IndexBufferBufFile(IBBufFilePath, IBTxtFile.Format);


                    //这里使用IndexNumberCount的话，只能用于正向提取
                    //如果要兼容逆向提取，需要换成IndexCount
                    //但是还有个问题，那就是即使换成IndexCount，如果IB文件的替换不是一个整体的Buffer，而是各个独立分开的Buffer
                    //则这里的SelfDivide是不应该存在的步骤，所以这里是无法逆向提取的。
                    //综合来看，逆向提取其实是一种适用性不强，并且很容易受到ini中各种因素干扰的提取方式
                    //但是如果能获取到DrawIndexed的具体数值呢？可以通过解析log.txt的方式进行获取
                    //但是解析很玛法，而且就算能获取到，那如果有复杂的CommandList混淆，投入与产出不成正比了就
                    //使用逆向Mod的ini的方式更加优雅。

                    if (IBBufFile.MinNumber != 0)
                    {
                        IBBufFile.SaveToFile_UInt32(OutputIBBufFilePath, -1 * (int)IBBufFile.MinNumber);
                    }
                    else
                    {
                        IBBufFile.SelfDivide(int.Parse(IBTxtFile.FirstIndex), (int)IBTxtFile.IndexNumberCount);
                        IBBufFile.SaveToFile_UInt32(OutputIBBufFilePath, -1 * (int)IBBufFile.MinNumber);
                    }

                    //写出VBBufFile
                    VertexBufferBufFile VBBufFile = new VertexBufferBufFile(FinalVB0);
                    if (IBBufFile.MinNumber > IBBufFile.MaxNumber)
                    {
                        LOG.Error("当前IB文件最小值大于IB文件中的最大值，跳过vb文件输出，因为无法SelfDivide");
                        continue;
                    }

                    if (IBBufFile.MinNumber != 0)
                    {
                        VBBufFile.SelfDivide((int)IBBufFile.MinNumber, (int)IBBufFile.MaxNumber, d3D11GameType.GetSelfStride());
                    }
                    VBBufFile.SaveToFile(OutputVBBufFilePath);

                    OutputCount += 1;
                }

                //TODO 每个数据类型文件夹下面都需要生成一个tmp.json，但是新版应该改名为Import.json
                //为了兼容旧版Catter，暂时先不改名

                ImportJson importJson = new ImportJson();
                string VB0FileName = FrameAnalysisDataUtils.FilterFirstFile(PathManager.WorkFolder, TrianglelistIndex + "-vb0", ".txt");
                
                LOG.Info("VB0FileName: " + VB0FileName);
                importJson.DrawIB = DrawIB;
                importJson.OriginalVertexCount = OriginalVertexCount;
                importJson.VertexLimitVB = VB0FileName.Substring(11, 8);
                importJson.d3D11GameType = d3D11GameType;
                importJson.Category_BufFileName_Dict = CategoryBufFileMap;
                importJson.MatchFirstIndex_IBTxtFileName_Dict = MatchFirstIndex_IBTxtFileName_Dict;

                //TODO 暂时叫tmp.json，后面再改
                string ImportJsonSavePath = Path.Combine(GameTypeOutputPath, "tmp.json");
                importJson.SaveToFile(ImportJsonSavePath);
            }

            LOG.NewLine();

            return true;
        }

        public static bool ExtractModel(List<DrawIBItem> DrawIBItemList)
        {
            GameConfig gameConfig = new GameConfig();
            D3D11GameTypeLv2 d3D11GameTypeLv2 = new D3D11GameTypeLv2(gameConfig.GameTypeName);

            LOG.Info("开始ZZZ提取:");
            foreach (DrawIBItem drawIBItem in DrawIBItemList)
            {
                string DrawIB = drawIBItem.DrawIB;

                if (DrawIB.Trim() == "")
                {
                    continue;
                }
                else
                {
                    LOG.Info("当前DrawIB: " + DrawIB);
                }
                LOG.NewLine();

                string PointlistIndex = FrameAnalysisLogUtilsV2.Get_PointlistIndex_ByHash(DrawIB, PathManager.Path_LatestFrameAnalysisLogTxt);
                LOG.Info("当前识别到的PointlistIndex: " + PointlistIndex);
                if (PointlistIndex == "")
                {
                    LOG.Info("当前识别到的PointlistIndex为空，此DrawIB对应的模型可能为CPU-PreSkinning类型。");
                }
                LOG.NewLine();


                List<string> TrianglelistIndexList = FrameAnalysisLogUtilsV2.Get_DrawCallIndexList_ByHash(DrawIB, false, PathManager.Path_LatestFrameAnalysisLogTxt);
                foreach (string TrianglelistIndex in TrianglelistIndexList)
                {
                    LOG.Info("TrianglelistIndex: " + TrianglelistIndex);
                }
                LOG.NewLine();

                bool result = Extract_fee307b98a965c16(DrawIB, d3D11GameTypeLv2, PointlistIndex, TrianglelistIndexList);
                if (!result)
                {
                    return false;
                }
            }

            LOG.Info("提取正常执行完成");
            return true;
        }

    }
}
