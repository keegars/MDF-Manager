﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace MDF_Manager.Classes
{
    public class BooleanHolder : INotifyPropertyChanged
    {
        private string _name;
        private bool _selected;
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public bool Selected
        {
            get
            {
                return _selected;
            }

            set
            {
                _selected = value;
                OnPropertyChanged(nameof(Selected));
            }
        }

        public BooleanHolder(string name, bool selected)
        {
            Name = name;
            Selected = selected;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class Material : INotifyPropertyChanged
    {
        public int NameOffsetIndex;
        //applied and used only on export
        public int MMOffsetIndex;
        public int materialIndex;
        private string _Name;
        private uint _Hash;
        public string Name { get { return _Name; } set { _Name = value; OnPropertyChanged("Name"); UpdateHash(); } }
        public uint UTF16Hash
        {
            get
            {
                return _Hash;
            }

            set
            {
                _Hash = value;
            }
        }
        public string MasterMaterial { get; set; }
        public ShadingType ShaderType { get; set; }
        public byte TessFactor { get; set; }
        public byte PhongFactor { get; set; }
        //shrug bytes are unsigned by default in C#
        public ObservableCollection<BooleanHolder> flags { get; set; }

        public List<TextureBinding> Textures { get; set; }
        public List<IVariableProp> Properties { get; set; }
        public Material(BinaryReader br, MDFTypes type, int matIndex)
        {
            flags = new ObservableCollection<BooleanHolder>();
            materialIndex = matIndex;
            var MatNameOffset = br.ReadInt64();
            var MatNameHash = br.ReadInt32();//not storing, since it'll just be easier to export proper
            if (type == MDFTypes.RE7)
            {
                var unknRE7 = br.ReadInt64();//I don't own RE7 so I'm just gonna hope these are 0s
            }
            var PropBlockSize = br.ReadInt32();
            var PropertyCount = br.ReadInt32();
            var TextureCount = br.ReadInt32();
            if (type == MDFTypes.MHRiseRE8 || type == MDFTypes.MHRiseSunbreak)
            {
                br.ReadInt64();
            }
            ShaderType = (ShadingType)br.ReadInt32();
            ReadFlagsSection(br);
            var PropHeadersOff = br.ReadInt64();
            var TexHeadersOff = br.ReadInt64();
            if (type == MDFTypes.MHRiseRE8 || type == MDFTypes.MHRiseSunbreak)
            {
                var StringTableOff = br.ReadInt64();//not at all useful, given everything uses absolute offsets
                //it's possible that this is an offset for something that is not used by most mdfs, this will need to be looked into
                //given the extra Int64, I find this very likely
            }
            var PropDataOff = br.ReadInt64();
            var MMTRPathOff = br.ReadInt64();
            var EOM = br.BaseStream.Position;//to return to after reading the rest of the parameters
            Textures = new List<TextureBinding>();
            Properties = new List<IVariableProp>();
            //now we'll go grab names and values
            br.BaseStream.Seek(MatNameOffset, SeekOrigin.Begin);
            Name = HelperFunctions.ReadUniNullTerminatedString(br);
            br.BaseStream.Seek(MMTRPathOff, SeekOrigin.Begin);
            MasterMaterial = HelperFunctions.ReadUniNullTerminatedString(br);

            //read textures
            br.BaseStream.Seek(TexHeadersOff, SeekOrigin.Begin);
            for (var i = 0; i < TextureCount; i++)
            {
                Textures.Add(ReadTextureBinding(br, type));
            }

            //read properties
            br.BaseStream.Seek(PropHeadersOff, SeekOrigin.Begin);
            for (var i = 0; i < PropertyCount; i++)
            {
                Properties.Add(ReadProperty(br, PropDataOff, type, matIndex, i));
            }
            br.BaseStream.Seek(EOM, SeekOrigin.Begin);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public TextureBinding ReadTextureBinding(BinaryReader br, MDFTypes type)
        {
            var TextureTypeOff = br.ReadInt64();
            var UTF16MMH3Hash = br.ReadUInt32();
            var ASCIIMMH3Hash = br.ReadUInt32();
            var FilePathOff = br.ReadInt64();
            if (type >= MDFTypes.RE3)
            {
                var Unkn0 = br.ReadInt64(); //value of 0 in most mdf, possible alignment?
            }
            var EOT = br.BaseStream.Position;
            br.BaseStream.Seek(TextureTypeOff, SeekOrigin.Begin);
            var TextureType = HelperFunctions.ReadUniNullTerminatedString(br);
            br.BaseStream.Seek(FilePathOff, SeekOrigin.Begin);
            var FilePath = HelperFunctions.ReadUniNullTerminatedString(br);
            br.BaseStream.Seek(EOT, SeekOrigin.Begin);
            var tb = new TextureBinding(TextureType, FilePath);
            return tb;
        }

        public IVariableProp ReadProperty(BinaryReader br, long dataOff, MDFTypes type, int matIndex, int propIndex)
        {
            var PropNameOff = br.ReadInt64();
            var UTF16MMH3Hash = br.ReadUInt32();
            var ASCIIMMH3Hash = br.ReadUInt32();
            var PropDataOff = 0;
            var ParamCount = 0;
            if (type >= MDFTypes.RE3)
            {
                PropDataOff = br.ReadInt32();
                ParamCount = br.ReadInt32();
            }
            else
            {
                ParamCount = br.ReadInt32();
                PropDataOff = br.ReadInt32();
            }

            var EOP = br.BaseStream.Position;
            br.BaseStream.Seek(PropNameOff, SeekOrigin.Begin);
            var PropName = HelperFunctions.ReadUniNullTerminatedString(br);
            br.BaseStream.Seek(dataOff + PropDataOff, SeekOrigin.Begin);
            switch (ParamCount)
            {
                case 1:
                    var fData = new Float(br.ReadSingle());
                    br.BaseStream.Seek(EOP, SeekOrigin.Begin);
                    return new FloatProperty(PropName, fData, matIndex, propIndex);
                case 4:
                    var f4Data = new Float4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    br.BaseStream.Seek(EOP, SeekOrigin.Begin);
                    return new Float4Property(PropName, f4Data, matIndex, propIndex);
                default:
                    br.BaseStream.Seek(EOP, SeekOrigin.Begin);
                    return new FloatProperty(Name, new Float((float)1.0), matIndex, propIndex);//shouldn't really come up ever
            }
        }

        public int GetSize(MDFTypes type)
        {
            var baseVal = 64;
            if (type == MDFTypes.RE7)
            {
                baseVal += 8;
            }
            else if (type == MDFTypes.MHRiseRE8 || type == MDFTypes.MHRiseSunbreak)
            {
                baseVal += 16;
            }
            return baseVal;
        }

        public void ReadFlagsSection(BinaryReader br)
        {
            var alphaFlags = (AlphaFlags)br.ReadByte();
            if ((alphaFlags & AlphaFlags.BaseTwoSideEnable) == AlphaFlags.BaseTwoSideEnable)
            {
                flags.Add(new BooleanHolder("BaseTwoSideEnable", true));
            }
            else
            {
                flags.Add(new BooleanHolder("BaseTwoSideEnable", false));
            }
            if ((alphaFlags & AlphaFlags.BaseAlphaTestEnable) == AlphaFlags.BaseAlphaTestEnable)
            {
                flags.Add(new BooleanHolder("BaseAlphaTestEnable", true));
            }
            else
            {
                flags.Add(new BooleanHolder("BaseAlphaTestEnable", false));
            }
            if ((alphaFlags & AlphaFlags.ShadowCastDisable) == AlphaFlags.ShadowCastDisable)
            {
                flags.Add(new BooleanHolder("ShadowCastDisable", true));
            }
            else
            {
                flags.Add(new BooleanHolder("ShadowCastDisable", false));
            }
            if ((alphaFlags & AlphaFlags.VertexShaderUsed) == AlphaFlags.VertexShaderUsed)
            {
                flags.Add(new BooleanHolder("VertexShaderUsed", true));
            }
            else
            {
                flags.Add(new BooleanHolder("VertexShaderUsed", false));
            }
            if ((alphaFlags & AlphaFlags.EmissiveUsed) == AlphaFlags.EmissiveUsed)
            {
                flags.Add(new BooleanHolder("EmissiveUsed", true));
            }
            else
            {
                flags.Add(new BooleanHolder("EmissiveUsed", false));
            }
            if ((alphaFlags & AlphaFlags.TessellationEnable) == AlphaFlags.TessellationEnable)
            {
                flags.Add(new BooleanHolder("TessellationEnable", true));
            }
            else
            {
                flags.Add(new BooleanHolder("TessellationEnable", false));
            }
            if ((alphaFlags & AlphaFlags.EnableIgnoreDepth) == AlphaFlags.EnableIgnoreDepth)
            {
                flags.Add(new BooleanHolder("EnableIgnoreDepth", true));
            }
            else
            {
                flags.Add(new BooleanHolder("EnableIgnoreDepth", false));
            }
            if ((alphaFlags & AlphaFlags.AlphaMaskUsed) == AlphaFlags.AlphaMaskUsed)
            {
                flags.Add(new BooleanHolder("AlphaMaskUsed", true));
            }
            else
            {
                flags.Add(new BooleanHolder("AlphaMaskUsed", false));
            }
            var flagByte = br.ReadByte();
            var flagVals = (Flags2)flagByte;
            if ((flagVals & Flags2.ForcedTwoSideEnable) == Flags2.ForcedTwoSideEnable)
            {
                flags.Add(new BooleanHolder("ForcedTwoSideEnable", true));
            }
            else
            {
                flags.Add(new BooleanHolder("ForcedTwoSideEnable", false));
            }
            if ((flagVals & Flags2.TwoSideEnable) == Flags2.TwoSideEnable)
            {
                flags.Add(new BooleanHolder("TwoSideEnable", true));
            }
            else
            {
                flags.Add(new BooleanHolder("TwoSideEnable", false));
            }
            TessFactor = (byte)(flagByte >> 2);
            PhongFactor = br.ReadByte();
            var Flags3 = (Flags3)br.ReadByte();
            if ((Flags3 & Flags3.RoughTransparentEnable) == Flags3.RoughTransparentEnable)
            {
                flags.Add(new BooleanHolder("RoughTransparentEnable", true));
            }
            else
            {
                flags.Add(new BooleanHolder("RoughTransparentEnable", false));
            }
            if ((Flags3 & Flags3.ForcedAlphaTestEnable) == Flags3.ForcedAlphaTestEnable)
            {
                flags.Add(new BooleanHolder("ForcedAlphaTestEnable", true));
            }
            else
            {
                flags.Add(new BooleanHolder("ForcedAlphaTestEnable", false));
            }
            if ((Flags3 & Flags3.AlphaTestEnable) == Flags3.AlphaTestEnable)
            {
                flags.Add(new BooleanHolder("AlphaTestEnable", true));
            }
            else
            {
                flags.Add(new BooleanHolder("AlphaTestEnable", false));
            }
            if ((Flags3 & Flags3.SSSProfileUsed) == Flags3.SSSProfileUsed)
            {
                flags.Add(new BooleanHolder("SSSProfileUsed", true));
            }
            else
            {
                flags.Add(new BooleanHolder("SSSProfileUsed", false));
            }
            if ((Flags3 & Flags3.EnableStencilPriority) == Flags3.EnableStencilPriority)
            {
                flags.Add(new BooleanHolder("EnableStencilPriority", true));
            }
            else
            {
                flags.Add(new BooleanHolder("EnableStencilPriority", false));
            }
            if ((Flags3 & Flags3.RequireDualQuaternion) == Flags3.RequireDualQuaternion)
            {
                flags.Add(new BooleanHolder("RequireDualQuaternion", true));
            }
            else
            {
                flags.Add(new BooleanHolder("RequireDualQuaternion", false));
            }
            if ((Flags3 & Flags3.PixelDepthOffsetUsed) == Flags3.PixelDepthOffsetUsed)
            {
                flags.Add(new BooleanHolder("PixelDepthOffsetUsed", true));
            }
            else
            {
                flags.Add(new BooleanHolder("PixelDepthOffsetUsed", false));
            }
            if ((Flags3 & Flags3.NoRayTracing) == Flags3.NoRayTracing)
            {
                flags.Add(new BooleanHolder("NoRayTracing", true));
            }
            else
            {
                flags.Add(new BooleanHolder("NoRayTracing", false));
            }
        }

        public byte[] GenerateFlagsSection()
        {
            AlphaFlags flags1 = 0;
            var flags2 = (Flags2)(TessFactor << 2);
            Flags3 flags3 = 0;
            for (var i = 0; i < flags.Count; i++)
            {
                switch (flags[i].Name)
                {
                    case "BaseTwoSideEnable":
                        if (flags[i].Selected)
                        {
                            flags1 |= AlphaFlags.BaseTwoSideEnable;
                        }
                        break;
                    case "BaseAlphaTestEnable":
                        if (flags[i].Selected)
                        {
                            flags1 |= AlphaFlags.BaseAlphaTestEnable;
                        }
                        break;
                    case "ShadowCastDisable":
                        if (flags[i].Selected)
                        {
                            flags1 |= AlphaFlags.ShadowCastDisable;
                        }
                        break;
                    case "VertexShaderUsed":
                        if (flags[i].Selected)
                        {
                            flags1 |= AlphaFlags.VertexShaderUsed;
                        }
                        break;
                    case "EmissiveUsed":
                        if (flags[i].Selected)
                        {
                            flags1 |= AlphaFlags.EmissiveUsed;
                        }
                        break;
                    case "TessellationEnable":
                        if (flags[i].Selected)
                        {
                            flags1 |= AlphaFlags.TessellationEnable;
                        }
                        break;
                    case "EnableIgnoreDepth":
                        if (flags[i].Selected)
                        {
                            flags1 |= AlphaFlags.EnableIgnoreDepth;
                        }
                        break;
                    case "AlphaMaskUsed":
                        if (flags[i].Selected)
                        {
                            flags1 |= AlphaFlags.AlphaMaskUsed;
                        }
                        break;
                    case "ForcedTwoSideEnable":
                        if (flags[i].Selected)
                        {
                            flags2 |= Flags2.ForcedTwoSideEnable;
                        }
                        break;
                    case "TwoSideEnable":
                        if (flags[i].Selected)
                        {
                            flags2 |= Flags2.TwoSideEnable;
                        }
                        break;
                    case "RoughTransparentEnable":
                        if (flags[i].Selected)
                        {
                            flags3 |= Flags3.RoughTransparentEnable;
                        }
                        break;
                    case "ForcedAlphaTestEnable":
                        if (flags[i].Selected)
                        {
                            flags3 |= Flags3.ForcedAlphaTestEnable;
                        }
                        break;
                    case "AlphaTestEnable":
                        if (flags[i].Selected)
                        {
                            flags3 |= Flags3.AlphaTestEnable;
                        }
                        break;
                    case "SSSProfileUsed":
                        if (flags[i].Selected)
                        {
                            flags3 |= Flags3.SSSProfileUsed;
                        }
                        break;
                    case "EnableStencilPriority":
                        if (flags[i].Selected)
                        {
                            flags3 |= Flags3.EnableStencilPriority;
                        }
                        break;
                    case "RequireDualQuaternion":
                        if (flags[i].Selected)
                        {
                            flags3 |= Flags3.RequireDualQuaternion;
                        }
                        break;
                    case "PixelDepthOffsetUsed":
                        if (flags[i].Selected)
                        {
                            flags3 |= Flags3.PixelDepthOffsetUsed;
                        }
                        break;
                    case "NoRayTracing":
                        if (flags[i].Selected)
                        {
                            flags3 |= Flags3.NoRayTracing;
                        }
                        break;
                }
            }
            var returnBytes = new byte[4];
            returnBytes[0] = (byte)flags1;
            returnBytes[1] = (byte)flags2;
            returnBytes[2] = PhongFactor;
            returnBytes[3] = (byte)flags3;
            return returnBytes;
        }

        public void UpdateMaterialIndex(int index)
        {
            materialIndex = index;
            for (var i = 0; i < Properties.Count; i++)
            {
                Properties[i].indexes[0] = index;
            }
        }

        public void Export(BinaryWriter bw, MDFTypes type, ref long materialOffset, ref long textureOffset, ref long propHeaderOffset, long stringTableOffset, List<int> strTableOffsets, ref long propertiesOffset)
        {
            bw.BaseStream.Seek(materialOffset, SeekOrigin.Begin);
            bw.Write(stringTableOffset + strTableOffsets[NameOffsetIndex]);
            bw.Write(HelperFunctions.Murmur3Hash(Encoding.Unicode.GetBytes(Name)));
            if (type == MDFTypes.RE7)
            {
                bw.Write((long)0);
            }
            var propSize = 0;
            for (var i = 0; i < Properties.Count; i++)
            {
                propSize += Properties[i].GetSize();
            }
            while ((propSize % 16) != 0)
            {
                propSize += 1;
            }
            bw.Write(propSize);
            bw.Write(Properties.Count);
            bw.Write(Textures.Count);
            if (type == MDFTypes.MHRiseRE8 || type == MDFTypes.MHRiseSunbreak)
            {
                bw.Write((long)0);
            }
            bw.Write((uint)ShaderType);
            bw.Write(GenerateFlagsSection());
            bw.Write(propHeaderOffset);
            bw.Write(textureOffset);
            if (type == MDFTypes.MHRiseRE8 || type == MDFTypes.MHRiseSunbreak)
            {
                bw.Write(stringTableOffset);
            }
            bw.Write(propertiesOffset);
            bw.Write(stringTableOffset + strTableOffsets[MMOffsetIndex]);
            //end of actual material file, now update material offset and write textures/properties
            materialOffset += GetSize(type);
            for (var i = 0; i < Textures.Count; i++)
            {
                Textures[i].Export(bw, type, ref textureOffset, stringTableOffset, strTableOffsets);
            }
            var basePropOffset = propertiesOffset;//subtract by current prop offset to make inner offset
            for (var i = 0; i < Properties.Count; i++)
            {
                Properties[i].Export(bw, type, ref propHeaderOffset, ref propertiesOffset, basePropOffset, stringTableOffset, strTableOffsets);
            }
            if ((basePropOffset + propSize) != propertiesOffset)
            {
                var diff = basePropOffset + propSize - propertiesOffset;
                propertiesOffset += diff;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        //used for deleting and adding new
        private void UpdateHash()
        {
            _Hash = HelperFunctions.Murmur3Hash(Encoding.Unicode.GetBytes(_Name));
            OnPropertyChanged("UTF16Hash");
        }
    }

    [Flags]
    public enum AlphaFlags
    {
        None = 0,
        BaseTwoSideEnable = 1,
        BaseAlphaTestEnable = 2,
        ShadowCastDisable = 4,
        VertexShaderUsed = 8,
        EmissiveUsed = 16,
        TessellationEnable = 32,
        EnableIgnoreDepth = 64,
        AlphaMaskUsed = 128
    }
    [Flags]
    public enum Flags2
    {
        None = 0,
        ForcedTwoSideEnable = 1,
        TwoSideEnable = 2
    }
    [Flags]
    public enum Flags3
    {
        None = 0,
        RoughTransparentEnable = 1,
        ForcedAlphaTestEnable = 2,
        AlphaTestEnable = 4,
        SSSProfileUsed = 8,
        EnableStencilPriority = 16,
        RequireDualQuaternion = 32,
        PixelDepthOffsetUsed = 64,
        NoRayTracing = 128
    }

    public enum ShadingType
    {
        Standard = 0,
        Decal = 1,
        DecalWithMetallic = 2,
        DecalNRMR = 3,
        Transparent = 4,
        Distortion = 5,
        PrimitiveMesh = 6,
        PrimitiveSolidMesh = 7,
        Water = 8,
        SpeedTree = 9,
        GUI = 10,
        GUIMesh = 11,
        GUIMeshTransparent = 12,
        ExpensiveTransparent = 13,
        Forward = 14,
        RenderTarget = 15,
        PostProcess = 16,
        PrimitiveMaterial = 17,
        PrimitiveSolidMaterial = 18,
        SpineMaterial = 19
    }

    public enum MDFTypes
    {
        RE7 = 6,
        RE2DMC5 = 10,
        RE3 = 13,
        MHRiseRE8 = 19,
        MHRiseSunbreak = 23
    }
}