using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSMT_Core;

namespace SSMT
{
    public class MetaDataJson
    {
        public string vb0_hash { get; set; } = "";
        public string cb4_hash { get; set; } = "";
        public int vertex_count { get; set; } = 0;
        public int index_count { get; set; } = 0;

        public List<Component> ComponentList { get; set; } = [];
        public ShapeKeys shapekeys { get; set; } = new ShapeKeys();

        public MetaDataJson()
        {

        }

        public JObject GetAttributeJobj(string Name, string Index, string Format, string Stride)
        {
            JObject index_jobj = new JObject();
            index_jobj["name"] = Name;
            index_jobj["index"] = int.Parse(Index);
            index_jobj["format"] = Format;
            index_jobj["stride"] = int.Parse(Stride);
            return index_jobj;
        }

        public JObject GetSpecificJobject(string Name,string Index,string Format,string Stride)
        {
            JObject index_jobj = GetAttributeJobj(Name,Index,Format,Stride);

            List<JObject> index_semantics_list = new List<JObject>();
            index_semantics_list.Add(index_jobj);

            JObject index_category_obj = new JObject();
            index_category_obj["semantics"] = JToken.FromObject(index_semantics_list);
            return index_category_obj;
        }

        public void SaveToFile(string SaveFolderPath,D3D11GameType d3D11GameType)
        {
            JObject metadataJsonJObject = DBMTJsonUtils.CreateJObject();
            metadataJsonJObject["vb0_hash"] = this.vb0_hash;
            metadataJsonJObject["cb4_hash"] = this.cb4_hash;
            metadataJsonJObject["vertex_count"] = this.vertex_count;
            metadataJsonJObject["index_count"] = this.index_count;

            JObject shapekeysJObject = DBMTJsonUtils.CreateJObject();
            shapekeysJObject["offsets_hash"] = this.shapekeys.offsets_hash;
            shapekeysJObject["scale_hash"] = this.shapekeys.scale_hash;
            shapekeysJObject["vertex_count"] = this.shapekeys.vertex_count;
            shapekeysJObject["dispatch_y"] = this.shapekeys.dispatch_y;
            shapekeysJObject["checksum"] = this.shapekeys.checksum;
            metadataJsonJObject["shapekeys"] = shapekeysJObject;

            //每个Component

            List<JObject> ComponentJObjectList = [];
            foreach (Component component in this.ComponentList)
            {
                JObject component_json = DBMTJsonUtils.CreateJObject();
                component_json["vertex_offset"] = component.vertex_offset;
                component_json["vertex_count"] = component.vertex_count;
                component_json["index_offset"] = component.index_offset;
                component_json["index_count"] = component.index_count;
                component_json["vg_offset"] = component.vg_offset;
                component_json["vg_count"] = component.vg_count;
                component_json["vg_map"] = JToken.FromObject(component.vg_map);

                ComponentJObjectList.Add(component_json);
            }

            metadataJsonJObject["components"] = JToken.FromObject(ComponentJObjectList);



            JObject export_format_obj = new JObject();

            //新增Index分类
            export_format_obj["Index"] = GetSpecificJobject("INDEX","0","R32_UINT","12");



            //export_format WWMIToolsV1.3.5新增内容
            foreach (string CategoryName in d3D11GameType.OrderedCategoryNameList)
            {

                List<JObject> SemanticObjectList = new List<JObject>();
                foreach (string d3d11ElementName in d3D11GameType.OrderedFullElementList)
                {
                    D3D11Element d3D11Element = d3D11GameType.ElementNameD3D11ElementDict[d3d11ElementName];
                    if (d3D11Element.Category != CategoryName)
                    {
                        continue;
                    }

                    //到这里就得到了这个CategoryName下的所有d3d11Element了，可以组装了

                    if (d3D11Element.SemanticName == "NORMAL")
                    {
                        SemanticObjectList.Add(GetAttributeJobj("NORMAL","0","R8G8B8_SNORM","3"));
                        SemanticObjectList.Add(GetAttributeJobj("BITANGENTSIGN","0","R8_SNORM","1"));
                    }
                    else if (d3D11Element.SemanticName.StartsWith("BLENDWEIGHT") && d3D11Element.Format == "R8_UNORM")
                    {
                        SemanticObjectList.Add(GetAttributeJobj(d3D11Element.SemanticName, d3D11Element.SemanticIndex.ToString(), "R8_UINT", "8"));
                    }
                    else if (d3D11Element.SemanticName.StartsWith("BLENDWEIGHT") && d3D11Element.Format == "R8G8B8A8_UNORM")
                    {
                        SemanticObjectList.Add(GetAttributeJobj(d3D11Element.SemanticName, d3D11Element.SemanticIndex.ToString(), "R8_UINT", "8"));
                    }
                    else if (d3D11Element.SemanticName.StartsWith("BLENDINDICES") && d3D11Element.Format == "R8G8B8A8_UINT")
                    {
                        SemanticObjectList.Add(GetAttributeJobj(d3D11Element.SemanticName, d3D11Element.SemanticIndex.ToString(), "R8_UINT", "8"));
                    }
                    else
                    {
                        JObject elementObject = GetAttributeJobj(
                           d3D11Element.SemanticName,
                           d3D11Element.SemanticIndex.ToString(),
                           d3D11Element.Format,
                           d3D11Element.ByteWidth
                       );
                        SemanticObjectList.Add(elementObject);
                    }
                       

                }

                JObject semantics_jobj = new JObject();
                semantics_jobj["semantics"] = JToken.FromObject(SemanticObjectList);

                export_format_obj[CategoryName] = semantics_jobj;
            }

            //ShapeKeyOffset
            export_format_obj["ShapeKeyOffset"] = GetSpecificJobject("SHAPEKEY", "0", "R32G32B32A32_UINT", "16");
            export_format_obj["ShapeKeyVertexId"] = GetSpecificJobject("SHAPEKEY", "1", "R32_UINT", "4");
            export_format_obj["ShapeKeyVertexOffset"] = GetSpecificJobject("SHAPEKEY", "2", "R16_FLOAT", "2");

            metadataJsonJObject["export_format"] = export_format_obj;

            string SavePath = Path.Combine(SaveFolderPath,"Metadata.json");

            DBMTJsonUtils.SaveJObjectToFile(metadataJsonJObject,SavePath);
        }

    }
}
