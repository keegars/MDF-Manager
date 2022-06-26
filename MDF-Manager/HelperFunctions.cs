﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Murmur;

namespace MDF_Manager
{
    static public class StringExtensions
    {
        static public string ReplaceInsensitive(this string str, string from, string to)
        {
            str = Regex.Replace(str, from, to, RegexOptions.IgnoreCase);
            return str;
        }
    }

    class HelperFunctions
    {
        public static HashAlgorithm mm3h = MurmurHash.Create32(seed: 0xFFFFFFFF);
        public static string ReadUniNullTerminatedString(BinaryReader br)
        {
            var stringC = new List<char>();
            var newByte = br.ReadChar();
            while (newByte != 0)
            {
                stringC.Add(newByte);
                newByte = br.ReadChar();
            }
            return new string(stringC.ToArray());
        }

        public static void WriteUniNullTerminatedString(BinaryWriter bw, string str)
        {
            bw.Write(Encoding.Unicode.GetBytes(str.ToCharArray()));
            bw.Write((short)0);
        }

        public static Brush GetBrushFromHex(string hexColor)
        {
            var bc = new BrushConverter();
            var newBrush = (Brush)bc.ConvertFrom(hexColor);
            return newBrush;
        }

        public static Brush GetBrushFromColor(Color c)
        {
            var bc = new BrushConverter();
            var nb = (Brush)bc.ConvertFrom(c.ToString());
            return nb;
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            var bytes = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            return bytes;
        }

        public static uint Murmur3Hash(byte[] str)
        {
            return BitConverter.ToUInt32(mm3h.ComputeHash(str), 0);
        }

        public static float Clamp(float input, float min, float max)
        {
            if (input > max)
            {
                return max;
            }
            else
            {
                if (input < min)
                {
                    return min;
                }
                else
                {
                    return input;
                }
            }
        }

        public static void Swap<T>(IList<T> list, int indexA, int indexB)
        {
            var tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        public static BinaryReader OpenFileR(string filename, Encoding encoding = null, FileMode mode = FileMode.Open)//surprised it doesn't get too mad at me here
        {
            if (encoding is null)
            {
                encoding = Encoding.ASCII;
            }

            try
            {
                var br = new BinaryReader(new FileStream(filename, mode), encoding);
                return br;
            }
            catch (IOException)
            {
                MessageBox.Show("File could not be opened, likely because it is being used by another process.");
                return null;
            }
        }

        public static BinaryWriter OpenFileW(string filename, Encoding encoding = null, FileMode mode = FileMode.Create)
        {
            if (encoding is null)
            {
                encoding = Encoding.ASCII;
            }

            try
            {
                var bw = new BinaryWriter(new FileStream(filename, mode), encoding);
                return bw;
            }
            catch (IOException)
            {
                MessageBox.Show("File could not be opened, likely because it is being used by another process.");
                return null;
            }
        }

        public static FileStream OpenFileStream(string filename, FileMode mode = FileMode.Create)
        {
            try
            {
                var fs = new FileStream(filename, mode);
                return fs;
            }
            catch (IOException)
            {
                MessageBox.Show("File could not be opened, likely because it is being used by another process.");
                return null;
            }
        }
    }
}