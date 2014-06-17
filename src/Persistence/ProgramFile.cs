using System;
using System.Text;
using System.IO;
using UnityEngine;
using ICSharpCode.SharpZipLib.GZip;
using kOS.Suffixed;

namespace kOS.Persistence
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
            Filename = filename;
            Content = string.Empty;
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
            var node = new ConfigNode(nodeName);
            node.AddValue("filename", Filename);

            node.AddValue("line", Config.Instance.UseCompressedPersistence ? EncodeBase64(Content) : EncodeLine(Content));

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
                Debug.Log(string.Format("Exception decoding: {0} | {1}", e, e.Message));
            }

            return decodedString;
        }

        private string EncodeBase64(string input)
        {
            using (var compressedStream = new MemoryStream())
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

            using (var inputStream = new MemoryStream(inputBuffer))
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
            return input
                .Replace("{", "&#123;")
                .Replace("}", "&#125;")
                .Replace(" ", "&#32;")
                .Replace("\n", "&#10");     // Stops universe from imploding
        }

        public static string DecodeLine(string input)
        {
            return input
                .Replace("&#123;", "{")
                .Replace("&#125;", "}")
                .Replace("&#32;", " ")
                .Replace("&#10", "\n");
        }
    }

    public struct FileInfo
    {
        public string Name;
        public int Size;

        public FileInfo(string name, int size)
        {
            Name = name;
            Size = size;
        }
    }
}
