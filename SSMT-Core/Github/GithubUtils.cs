using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMT
{
    public class RepositoryInfo
    {
        public string OwnerName { get; set; } = "";

        public string RepositoryName { get; set; } = "";
        public RepositoryInfo()
        {

        }

        public RepositoryInfo(string ownerName, string repositoryName)
        {
            OwnerName = ownerName;
            RepositoryName = repositoryName;
        }

    }

    public class GithubUtils
    {

        public static RepositoryInfo GetCurrentRepositoryInfo(string PackageName)
        {

            RepositoryInfo repositoryInfo = new RepositoryInfo();

            if (PackageName == LogicName.UnityVS || PackageName == LogicName.GIMI || PackageName == LogicName.UnityCPU)
            {
                repositoryInfo.OwnerName = "SilentNightSound";
                repositoryInfo.RepositoryName = "GIMI-Package";
            }
            else if (PackageName == LogicName.SRMI)
            {
                repositoryInfo.OwnerName = "SpectrumQT";
                repositoryInfo.RepositoryName = "SRMI-Package";
            }
            else if (PackageName == LogicName.ZZMI)
            {
                repositoryInfo.OwnerName = "leotorrez";
                repositoryInfo.RepositoryName = "ZZMI-Package";
            }
            else if (PackageName == LogicName.HIMI)
            {
                repositoryInfo.OwnerName = "leotorrez";
                repositoryInfo.RepositoryName = "HIMI-Package";
            }
            else if (PackageName == LogicName.WWMI)
            {
                repositoryInfo.OwnerName = "SpectrumQT";
                repositoryInfo.RepositoryName = "WWMI-Package";
            }
            else
            {
                //如果没有提前预设的话，就提供默认的MinBase-Package
                repositoryInfo.OwnerName = "StarBobis";
                repositoryInfo.RepositoryName = "MinBase-Package";
            }

            return repositoryInfo;
        }
    }
}
