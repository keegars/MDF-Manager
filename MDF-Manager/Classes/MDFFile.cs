using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace MDF_Manager.Classes
{
    public class MDFFile : INotifyPropertyChanged
    {
        public string FileName = "";
        static byte[] magic = { (byte)'M', (byte)'D', (byte)'F', 0x00 };
        private string _Header;
        ushort unkn = 1;
        public static IList<ShadingType> ShadingTypes
        {
            get
            {
                return Enum.GetValues(typeof(ShadingType)).Cast<ShadingType>().ToList<ShadingType>();
            }
        }

        public string Header { get { return _Header; } set { _Header = value; OnPropertyChanged("Header"); } }
        public DataTemplate HeaderTemplate { get; set; }
        public ObservableCollection<Material> Materials { get; set; }

        public MDFFile(string fileName, BinaryReader br, MDFTypes types)
        {
            Materials = new ObservableCollection<Material>();
            Header = fileName;
            FileName = fileName;
            var mBytes = br.ReadBytes(4);
            if (Encoding.ASCII.GetString(mBytes) != Encoding.ASCII.GetString(magic))
            {
                MessageBox.Show("Not a valid MDF file!");
                return;
            }
            var unkn1 = br.ReadUInt16();
            if (unkn1 != unkn)
            {
                MessageBox.Show("Potentially bad MDF file.");
            }
            var MaterialCount = br.ReadUInt16();
            br.ReadUInt64();
            for (var i = 0; i < MaterialCount; i++)
            {
                Materials.Add(new Material(br, types, i));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public List<byte> GenerateStringTable(ref List<int> offsets)
        {
            var strings = new List<string>();
            for (var i = 0; i < Materials.Count; i++)
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
            for (var i = 0; i < Materials.Count; i++)
            {
                for (var j = 0; j < Materials[i].Textures.Count; j++)
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
            for (var i = 0; i < Materials.Count; i++)
            {
                for (var j = 0; j < Materials[i].Properties.Count; j++)
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
            var outputBuff = new List<byte>();
            offsets.Add(0);
            for (var i = 0; i < strings.Count; i++)
            {
                var inBytes = Encoding.Unicode.GetBytes(strings[i]);
                for (var j = 0; j < inBytes.Length; j++)
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
            var strTableOffsets = new List<int>();
            var stringTable = GenerateStringTable(ref strTableOffsets);
            //this function handles the biggest problem of writing materials, getting the name offsets
            var materialOffset = bw.BaseStream.Position;
            while ((materialOffset % 16) != 0)
            {
                materialOffset++;
            }
            var textureOffset = materialOffset;
            for (var i = 0; i < Materials.Count; i++)
            {
                textureOffset += Materials[i].GetSize(type);
            }
            while ((textureOffset % 16) != 0)
            {
                textureOffset++;
            }
            var propHeadersOffset = textureOffset;
            for (var i = 0; i < Materials.Count; i++)
            {
                for (var j = 0; j < Materials[i].Textures.Count; j++)
                {
                    propHeadersOffset += Materials[i].Textures[j].GetSize(type);
                }
            }
            while ((propHeadersOffset % 16) != 0)
            {
                propHeadersOffset++;
            }
            var stringTableOffset = propHeadersOffset;
            for (var i = 0; i < Materials.Count; i++)
            {
                for (var j = 0; j < Materials[i].Properties.Count; j++)
                {
                    stringTableOffset += Materials[i].Properties[j].GetPropHeaderSize();
                }
            }
            while ((stringTableOffset % 16) != 0)
            {
                stringTableOffset++;
            }
            var propertiesOffset = stringTableOffset + stringTable.Count;
            while ((propertiesOffset % 16) != 0)
            {
                propertiesOffset++;
            }
            bw.BaseStream.Seek(stringTableOffset, SeekOrigin.Begin);
            for (var i = 0; i < stringTable.Count; i++)
            {
                bw.Write(stringTable[i]);
            }
            for (var i = 0; i < Materials.Count; i++)
            {
                Materials[i].Export(bw, type, ref materialOffset, ref textureOffset, ref propHeadersOffset, stringTableOffset, strTableOffsets, ref propertiesOffset);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}