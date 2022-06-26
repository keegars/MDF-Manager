using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;

namespace MDF_Manager.Classes
{
    public class MDFFile : INotifyPropertyChanged
    {
        private string _Header;
        public string Header { get => _Header; set { _Header = value; OnPropertyChanged("Header"); } }
        public DataTemplate HeaderTemplate { get; set; }
        public string FileName = "";
        static byte[] magic = { (byte)'M', (byte)'D', (byte)'F', 0x00 };
        UInt16 unkn = 1;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ObservableCollection<Material> Materials { get; set; }

        public MDFFile(string fileName, BinaryReader br, MDFTypes types)
        {
            Materials = new ObservableCollection<Material>();
            Header = fileName;
            FileName = fileName;
            byte[] mBytes = br.ReadBytes(4);
            if (Encoding.ASCII.GetString(mBytes) != Encoding.ASCII.GetString(magic))
            {
                MessageBox.Show("Not a valid MDF file!");
                return;
            }
            UInt16 unkn1 = br.ReadUInt16();
            if (unkn1 != unkn)
            {
                MessageBox.Show("Potentially bad MDF file.");
            }
            UInt16 MaterialCount = br.ReadUInt16();
            br.ReadUInt64();
            for (int i = 0; i < MaterialCount; i++)
            {
                Materials.Add(new Material(br, types, i));
            }

        }

        public List<byte> GenerateStringTable(ref List<int> offsets)
        {
            List<string> strings = new List<string>();
            for (int i = 0; i < Materials.Count; i++)
            {
                if (!strings.Contains(Materials[i].Name))
                {
                    strings.Add(Materials[i].Name);
                    Materials[i].NameOffsetIndex = strings.Count - 1;
                }
                else
                {
                    Materials[i].NameOffsetIndex = strings.FindIndex(name => name == Materials[i].Name);
                }
                if (!strings.Contains(Materials[i].MasterMaterial))
                {
                    strings.Add(Materials[i].MasterMaterial);
                    Materials[i].MMOffsetIndex = strings.Count - 1;
                }
                else
                {
                    Materials[i].MMOffsetIndex = strings.FindIndex(name => name == Materials[i].MasterMaterial);
                }
            }
            for (int i = 0; i < Materials.Count; i++)
            {
                for (int j = 0; j < Materials[i].Textures.Count; j++)
                {
                    if (!strings.Contains(Materials[i].Textures[j].name))
                    {
                        strings.Add(Materials[i].Textures[j].name);
                        Materials[i].Textures[j].NameOffsetIndex = strings.Count - 1;
                    }
                    else
                    {
                        Materials[i].Textures[j].NameOffsetIndex = strings.FindIndex(name => name == Materials[i].Textures[j].name);
                    }
                    if (!strings.Contains(Materials[i].Textures[j].path))
                    {
                        strings.Add(Materials[i].Textures[j].path);
                        Materials[i].Textures[j].PathOffsetIndex = strings.Count - 1;
                    }
                    else
                    {
                        Materials[i].Textures[j].PathOffsetIndex = strings.FindIndex(name => name == Materials[i].Textures[j].path);
                    }
                }
            }
            for (int i = 0; i < Materials.Count; i++)
            {
                for (int j = 0; j < Materials[i].Properties.Count; j++)
                {
                    if (!strings.Contains(Materials[i].Properties[j].name))
                    {
                        strings.Add(Materials[i].Properties[j].name);
                        Materials[i].Properties[j].NameOffsetIndex = strings.Count - 1;
                    }
                    else
                    {
                        Materials[i].Properties[j].NameOffsetIndex = strings.FindIndex(name => name == Materials[i].Properties[j].name);
                    }
                }
            }
            List<byte> outputBuff = new List<byte>();
            offsets.Add(0);
            for (int i = 0; i < strings.Count; i++)
            {
                byte[] inBytes = Encoding.Unicode.GetBytes(strings[i]);
                for (int j = 0; j < inBytes.Length; j++)
                {
                    outputBuff.Add(inBytes[j]);
                }
                outputBuff.Add(0);
                outputBuff.Add(0);
                offsets.Add(outputBuff.Count);//think this will end with the very last one being unused but that's fine

            }
            return outputBuff;
        }

        public void Export(BinaryWriter bw, MDFTypes type)
        {
            ////Add ice_PanoramaMap and reflectionMap
            //var ice_PanoramaMap = new TextureBinding
            //{
            //    name = "ice_PanoramaMap",
            //    path = "MasterMaterial/Textures/panorama01.tex"
            //};
            //var reflectionMap = new TextureBinding
            //{
            //    name = "reflectionMap",
            //    path = "MasterMaterial/Textures/IBL_small.tex"
            //};

            //for (var i = 0; i < Materials.Count; i++) {
            //    var material = Materials[i];

            //    if (material.Textures.Any(z=> z.name == "BaseDielectricMap"))
            //    {
            //        var ice = material.Textures.Any(z => z.name == ice_PanoramaMap.name);
            //        var reflection = material.Textures.Any(z => z.name == reflectionMap.name);

            //        if (!ice) {
            //            material.Textures.Add(ice_PanoramaMap);

            //            //Check to see if property also exists, else add
            //            if (!material.Properties.Any(z=> z.name == "ice_color"))
            //            {
            //                material.Properties.Add(new Float4Property("ice_color", new Float4(0.345098048f, 0.349019617f, 0.3529412f, 0.09019608f), i , material.Properties.Count - 1));
            //                //{
            //                //    mBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9F9FA0")),
            //                //    mColor = (Color)ColorConverter.ConvertFromString("#179F9FA0"),
            //                //    w = 0.09019608f,
            //                //    x = 0.345098048f,
            //                //    y = 0.349019617f,
            //                //    z = 0.3529412f
            //                //}, 0 , 3));
            //            }
            //        }
            //        if (!reflection)
            //        {
            //            material.Textures.Add(reflectionMap);

            //            //Check to see if property also exists, else add
            //            if (!material.Properties.Any(z => z.name == "refrection_col"))
            //            {
            //                material.Properties.Add(new Float4Property("refrection_col", new Float4(0.6039216f, 0.6745098f, 0.7137255f, 0.0196078438f), i, material.Properties.Count - 1));
            //                //{
            //                //    mBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9F9FA0")),
            //                //    mColor = (Color)ColorConverter.ConvertFromString("#179F9FA0"),
            //                //    w = 0.09019608f,
            //                //    x = 0.345098048f,
            //                //    y = 0.349019617f,
            //                //    z = 0.3529412f
            //                //}, 0 , 3));
            //            }
            //        }
            //    }
            //}

            //----------------------------------------

            //Add missing materials and properties.....else need to try just merging into existing mdf2
            //var fileName = @"D:\SteamLibrary\steamapps\common\MONSTER HUNTER RISE SUNBREAK DEMO\re_chunk_000\natives\STM\player\mod\f\pl353\f_body353.mdf2.19";
            //var readFile = HelperFunctions.OpenFileR(fileName, Encoding.Unicode);
            //var exampleMDF = new MDFFile(fileName, readFile, type);

            //var iceMaterial = exampleMDF.Materials[0]; //Should be ice example

            //var propertiesModified = false;

            //foreach (var texture in iceMaterial.Textures)
            //{
            //    for (var i = 0; i < Materials.Count; i++)
            //    {
            //        var material = Materials[i];

            //        if (material.Textures.Any(z => z.name == "BaseDielectricMap"))
            //        {
            //            if (!material.Textures.Any(z=> z.name == texture.name))
            //            {
            //                material.Textures.Add(texture);
            //            }
            //        }
            //    }
            //}

            //foreach (var property in iceMaterial.Properties)
            //{
            //    for (var i = 0; i < Materials.Count; i++)
            //    {
            //        var material = Materials[i];

            //        if (material.Textures.Any(z => z.name == "BaseDielectricMap"))
            //        {
            //            if (!material.Properties.Any(z => z.name == property.name))
            //            {
            //                material.Properties.Add(property);
            //                propertiesModified = true;
            //            }
            //        }
            //    }
            //}

            //if (propertiesModified)
            //{
            //    for (var i = 0; i < Materials.Count; i++)
            //    {
            //        var material = Materials[i];

            //        for (var j = 0; j < material.Properties.Count; j++)
            //        {
            //            material.Properties[j].indexes[0] = i; //Material index
            //            material.Properties[j].indexes[1] = j; //Property index
            //        }
            //    }
            //}

            //------------------------------------------------
            //Convert over the the ice format....sigh
            //Convert to existing mdf2...
            //var fileName = @"D:\SteamLibrary\steamapps\common\MONSTER HUNTER RISE SUNBREAK DEMO\re_chunk_000\natives\STM\player\mod\f\pl353\f_body353.mdf2.19";
            //var readFile = HelperFunctions.OpenFileR(fileName, Encoding.Unicode);
            //var exampleMDF = new MDFFile(fileName, readFile, type);

            //var iceMaterial = exampleMDF.Materials[0]; //Should be ice example

            //for (var i = 0; i < Materials.Count; i++)
            //{
            //    var material = Materials[i];

            //    if (material.Textures.Any(z => z.name == "BaseDielectricMap"))
            //    {
            //        for (var j = 0; j < iceMaterial.Textures.Count; j++)
            //        {
            //            var tmpIceTexture = iceMaterial.Textures[j];

            //            var materialTexture = material.Textures.FirstOrDefault(z => z.name == tmpIceTexture.name);

            //            if(materialTexture != null)
            //            {
            //                tmpIceTexture.path = materialTexture.path;
            //            }
            //        }

            //        for (var j = 0; j < iceMaterial.Properties.Count; j++)
            //        {
            //            var tmpIceProperty = iceMaterial.Properties[j];

            //            var materialProperty = material.Properties.FirstOrDefault(z => tmpIceProperty.name == z.name);

            //            if (materialProperty != null)
            //            {
            //                //TO DO - IF THIS DOESN'T work check types, if they don't match, then stick with ICEMATERIAL
            //                materialProperty.indexes = tmpIceProperty.indexes;

            //                iceMaterial.Properties[j] = materialProperty;
            //            }
            //        }

            //        //material.Textures = iceMaterial.Textures;
            //        //material.Properties = iceMaterial.Properties;
            //        //material.flags = iceMaterial.flags;

            //        break; //Pretty sure it only needs one material with ice on it
            //    }  
            //}

            //------------------------------------------------

            //DISABLE BASEALPHATEST

            //for (var i = 0; i < Materials.Count; i++)
            //{
            //    var material = Materials[i];

            //    for (int j = 0; j < material.flags.Count; j++)
            //    {
            //        var flag = material.flags[j];
            //        if (flag.Name == "BaseAlphaTestEnable")
            //        {
            //            flag.Selected = false;
            //        }
            //    }
            //}


            //------------------------------------------------

            //Convert old mdf2 to an already working mdf2....
            //There is a working skin, and alpha body....

            var fileName = Path.Combine(Environment.CurrentDirectory, "MDF/example/f_body302.mdf2.23");

            var bodyDetector = "BaseDielectricMap";
            var skinDetector = "SkinMap";

            for (var i = 0; i < Materials.Count; i++)
            {
                var binary = HelperFunctions.OpenFileR(fileName, Encoding.Unicode);

                var exampleMDF = new MDFFile(fileName, binary, type);

                var bodyMaterial = exampleMDF.Materials[0];
                var alphaBodyMaterial = exampleMDF.Materials[1];
                var skinMaterial = exampleMDF.Materials[2];

                var material = Materials[i];
                var isAlphaCheck = material.flags.Any(z => z.Name == "BaseAlphaTestEnable");

                Material newMaterial = null;

                //Assign the correct material
                if (material.Textures.Any(z => z.name == skinDetector))
                {
                    newMaterial = skinMaterial;
                }
                else if (material.Textures.Any(z => z.name == bodyDetector))
                {
                    newMaterial = isAlphaCheck ? alphaBodyMaterial : bodyMaterial;
                }

                //If detect, lets merge the values...
                if (newMaterial != null)
                {
                    for (var j = 0; j < newMaterial.Textures.Count; j++)
                    {
                        //Get the current new material texture
                        var tmpMaterial = newMaterial.Textures[j];

                        //Try and find the matching texture in the old material
                        var materialTexture = material.Textures.FirstOrDefault(z => z.name == tmpMaterial.name);

                        //If match found, lets update it's path
                        if (materialTexture != null)
                        {
                            tmpMaterial.path = materialTexture.path;
                        }
                    }

                    for (var j = 0; j < newMaterial.Properties.Count; j++)
                    {
                        //Get the current new material property
                        var tmpProperty = newMaterial.Properties[j];

                        //Try and find the matching property in the old material
                        var materialProperty = material.Properties.FirstOrDefault(z => tmpProperty.name == z.name);

                        if (materialProperty != null)
                        {
                            materialProperty.indexes = tmpProperty.indexes;

                            newMaterial.Properties[j] = materialProperty;
                        }
                    }

                    //Update textures, properties and flags to match
                    material.Textures = newMaterial.Textures;
                    material.Properties = newMaterial.Properties;
                    //material.flags = newMaterial.flags;
                }

                binary.Close();
            }

            bw.Write(magic);
            bw.Write((short)1);
            bw.Write((short)Materials.Count);
            bw.Write((long)0);
            //before going further, we need accurate lengths for 4 of the 5 main sections of the mdf
            /*
             * header -set size
             * materials - set size
             * textures - set size
             * propHeaders - set size
             * stringtable - generate in a separate function
             * prop values - based off of prop headers
             */
            List<int> strTableOffsets = new List<int>();
            List<byte> stringTable = GenerateStringTable(ref strTableOffsets);
            //this function handles the biggest problem of writing materials, getting the name offsets
            long materialOffset = bw.BaseStream.Position;
            while ((materialOffset % 16) != 0)
            {
                materialOffset++;
            }
            long textureOffset = materialOffset;
            for (int i = 0; i < Materials.Count; i++)
            {
                textureOffset += Materials[i].GetSize(type);
            }
            while ((textureOffset % 16) != 0)
            {
                textureOffset++;
            }
            long propHeadersOffset = textureOffset;
            for (int i = 0; i < Materials.Count; i++)
            {
                for (int j = 0; j < Materials[i].Textures.Count; j++)
                {
                    propHeadersOffset += Materials[i].Textures[j].GetSize(type);
                }
            }
            while ((propHeadersOffset % 16) != 0)
            {
                propHeadersOffset++;
            }
            long stringTableOffset = propHeadersOffset;
            for (int i = 0; i < Materials.Count; i++)
            {
                for (int j = 0; j < Materials[i].Properties.Count; j++)
                {
                    stringTableOffset += Materials[i].Properties[j].GetPropHeaderSize();
                }
            }
            while ((stringTableOffset % 16) != 0)
            {
                stringTableOffset++;
            }
            long propertiesOffset = stringTableOffset + stringTable.Count;
            while ((propertiesOffset % 16) != 0)
            {
                propertiesOffset++;
            }
            bw.BaseStream.Seek(stringTableOffset, SeekOrigin.Begin);
            for (int i = 0; i < stringTable.Count; i++)
            {
                bw.Write(stringTable[i]);
            }
            for (int i = 0; i < Materials.Count; i++)
            {
                Materials[i].Export(bw, type, ref materialOffset, ref textureOffset, ref propHeadersOffset, stringTableOffset, strTableOffsets, ref propertiesOffset);
            }
        }

        public static Material Clone(Material source)
        {
            var serialized = JsonConvert.SerializeObject(source,new JsonSerializerSettings()
              {
                            TypeNameHandling = TypeNameHandling.Auto
              });
            return JsonConvert.DeserializeObject<Material>(serialized);
        }

        public static IList<ShadingType> ShadingTypes
        {
            get
            {
                return Enum.GetValues(typeof(ShadingType)).Cast<ShadingType>().ToList<ShadingType>();
            }
        }
    }
}
