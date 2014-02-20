using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using ICSharpCode.SharpZipLib.GZip;

namespace kOS
{
    public class ProgramFile
    {
        public string Filename;
        public string Content;

        public ProgramFile(ProgramFile copy)
        {
            Filename = copy.Filename;
            Content = copy.Content;
        }

        public ProgramFile(string filename)
        {
            this.Filename = filename;
            this.Content = string.Empty;
        }

        public ProgramFile(ConfigNode fileNode)
        {
            Load(fileNode);
        }

        public ProgramFile Copy()
        {
            return new ProgramFile(this);
        }

        public int GetSize()
        {
            return Content.Length;
        }

        public ConfigNode Save(string nodeName)
        {
            ConfigNode node = new ConfigNode(nodeName);
            node.AddValue("filename", Filename);

            if (Config.GetInstance().UseNewPersistenceFormat)
            {
                node.AddValue("line", EncodeBase64(Content));
            }
            else
            {
                node.AddValue("line", EncodeLine(Content));
            }

            return node;
        }

        internal void Load(ConfigNode fileNode)
        {
            Filename = fileNode.GetValue("filename");
            Content = Decode(fileNode.GetValue("line"));
        }

        private string Decode(string input)
        {
            string decodedString = string.Empty;

            try
            {
                try
                {
                    // base64 encoding
                    decodedString = DecodeBase64(input);
                }
                catch (FormatException)
                {
                    // standard encoding
                    decodedString = DecodeLine(input);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Exception decoding: " + e.ToString() + " | " + e.Message);
            }

            return decodedString;
        }

        private string EncodeBase64(string input)
        {
            using (MemoryStream compressedStream = new MemoryStream())
            {
                // mono requires an installed zlib library for GZipStream to work :(
                // using (Stream csStream = new GZipStream(compressedStream, CompressionMode.Compress))
                using (Stream csStream = new GZipOutputStream(compressedStream))
                {
                    byte[] buffer = Encoding.ASCII.GetBytes(input);
                    csStream.Write(buffer, 0, buffer.Length);
                }

                return Convert.ToBase64String(compressedStream.ToArray());
            }
        }

        private string DecodeBase64(string input)
        {
            byte[] inputBuffer = Convert.FromBase64String(input);

            using (MemoryStream inputStream = new MemoryStream(inputBuffer))
            // mono requires an installed zlib library for GZipStream to work :(
            //using (var zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var zipStream = new GZipInputStream(inputStream))
            using (var decompressedStream = new MemoryStream())
            {
                var buffer = new byte[4096];
                int read;

                while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    decompressedStream.Write(buffer, 0, read);
                }

                return Encoding.ASCII.GetString(decompressedStream.ToArray());
            }
        }

        public static string EncodeLine(string input)
        {
            return input.Replace("{", "&#123;").Replace("}", "&#125;").Replace(" ", "&#32;");     // Stops universe from imploding
        }

        public static string DecodeLine(string input)
        {
            return input.Replace("&#123;", "{").Replace("&#125;", "}").Replace("&#32;", " ");
        }
    }

    public struct FileInfo
    {
        public string Name;
        public int Size;

        public FileInfo(string Name, int Size)
        {
            this.Name = Name;
            this.Size = Size;
        }
    }
}
