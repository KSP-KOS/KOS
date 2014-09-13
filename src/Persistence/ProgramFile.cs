using System;
using System.Text;
using System.IO;
using UnityEngine;
using ICSharpCode.SharpZipLib.GZip;
using kOS.Suffixed;

namespace kOS.Persistence
{
    /// <summary>
    /// Identifies the type of file it is,
    /// (By scanning over the file's first few bytes).
    /// (NOTE: This was called "FileType", but I didn't like the
    /// overloaded meaning of "Type" which also meas a C# Type.)
    /// </summary>
    public enum FileCategory
    {
        /// <summary>
        /// either can't be identified, or file couldn't be opened to try to identify it.
        /// </summary>
        UNKNOWN = 0,

        /// <summary>
        /// The default the type identifier will always assume as long<br/>
        /// as the first few characters are printable ascii.
        /// </summary>
        ASCII, 

        /// <summary>
        /// At the moment we won't be able to detect this<br/>
        /// and it will call scripts just ASCII, but this<br/>
        /// may change later and be used.
        /// </summary>
        KERBOSCRIPT,
                      
        /// <summary>
        /// The ML compiled and packed file that came from a KerboScript.
        /// </summary>
        KEXE
    }

    public class ProgramFile
    {
        
        public string Filename {get;set;}
        public FileCategory Category {get;private set;}
        public string Content
        {
            get { return content; }
            // if changing content, re-check the category:
            set { content = value; IdentifyCategory(); }
        }
        private string content;

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
            LoadEncoded(fileNode);
        }

        public ProgramFile Copy()
        {
            return new ProgramFile(this);
        }

        public int GetSize()
        {
            return Content.Length;
        }

        public ConfigNode SaveEncoded(string nodeName)
        {
            var node = new ConfigNode(nodeName);
            node.AddValue("filename", Filename);

            if (Config.Instance.UseCompressedPersistence || Category == FileCategory.KEXE)
                node.AddValue("iine", EncodeBase64(Content));
            else
                node.AddValue("iine", EncodeLine(Content));
                
            return node;
        }

        internal void LoadEncoded(ConfigNode fileNode)
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
            // This could probably be re-coded into something more algorithmic and less hardcoded,
            // as in "For any of the characters in this blacklist, if the character exists then
            // replace it with [ampersand,hash,unicode_num,semicolon] for the char's unicode value."
            return input
                .Replace("{", "&#123;")
                .Replace("}", "&#125;")
                .Replace(" ", "&#32;")
                .Replace(@"\", "&#92;") 
                .Replace("//", "&#47;&#47;") // a double slash is also a comment in the persistence file's syntax.
                .Replace("\t", "&#8;") // protect tabs, if there are any
                .Replace("\n", "&#10");     // Stops universe from imploding
        }

        public static string DecodeLine(string input)
        {
            // This could probably be re-coded into something more algorithmic and less hardcoded,
            // as in "any time there is [ampersand,hash,digits,semicolon], replace with the unicode
            // char for the digits' number."
            return input
                .Replace("&#123;", "{")
                .Replace("&#125;", "}")
                .Replace("&#32;", " ")
                .Replace("&#92;", @"\") 
                .Replace("&#47;", "/")
                .Replace("&#8;", "\t")
                .Replace("&#10", "\n");
        }
        
        /// <summary>
        /// Read the current Content and decide what the FileCategory
        /// should be, based on what's in the Content.<br/>
        /// This should be called every time the Content changes.
        /// </summary>
        private void IdentifyCategory()
        {
            Category = FileCategory.UNKNOWN; // default if none of the conditions pass
            
            // Annoyingly, .Net's Substring won't automatically just return a shorter string
            // if length is too big.   Thus the need for the extra check for short contents:
            int atMostFour = Math.Min(4,Content.Length);
            string firstFour = Content.Substring(0, atMostFour);
            
            string kEXEMagicIdString =
                System.Text.Encoding.ASCII.GetString(Compilation.CompiledObject.MagicId);
            
            if (firstFour == kEXEMagicIdString)
            {
                Category = FileCategory.KEXE;
            }
            else
            {
                bool isAscii = true;
                foreach (char ch in firstFour)
                {
                    if (ch != '\n' && ch != '\t' && ch != '\r' && (ch < 32 || ch > 127))
                    {
                        isAscii = false;
                        break;
                    }
                }
                if (isAscii)
                    Category = FileCategory.ASCII;
            }
            // There is not currently an explicit test for KERBOSRIPT versus other types of ASCII.
        }
    }
}
