using System;
using System.Text;
using System.IO;
using System.Linq;
using UnityEngine;
using ICSharpCode.SharpZipLib.GZip;
using kOS.Safe.Compilation;
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
        private const string FILENAME_VALUE_STRING = "filename";
        private const string CREATED_DATE_VALUE_STRING = "createdDate";
        private const string MODIFIED_DATE_VALUE_STRING = "modifiedDate";
        public string Filename {get;set;}
        public DateTime ModifiedDate {get;set;}
        public DateTime CreatedDate {get;set;}
        public FileCategory Category {get;private set;}
        public string StringContent
        {
            get 
            {
                if (Category != FileCategory.ASCII && Category != FileCategory.KERBOSCRIPT)
                    throw new KOSFileException("File " + Filename + " is not ASCII.  Should use BinaryContent instead.");
                return stringContent; 
            }
            set
            {
                Category = FileCategory.ASCII;
                stringContent = value;
            }
        }
        private string stringContent;

        public byte[] BinaryContent
        {
            get
            {
                if (Category == FileCategory.ASCII || Category == FileCategory.KERBOSCRIPT)
                    throw new KOSFileException("File " + Filename + " is not Binary. Should use StringContent instead.");
                return binaryContent;
            }
            set 
            {
                Category = FileCategory.KEXE;
                binaryContent = value;
            }
        }
        private byte[] binaryContent;

        public ProgramFile(ProgramFile copy)
        {
            Filename = copy.Filename;
            Category = copy.Category;
            ModifiedDate = copy.ModifiedDate;
            CreatedDate = copy.CreatedDate;
            if (Category == FileCategory.KEXE)
                BinaryContent = copy.BinaryContent;
            else
                StringContent = copy.StringContent;
        }

        public ProgramFile(string filename)
        {
            Filename = filename;
            Category = FileCategory.UNKNOWN;
            stringContent = string.Empty;
            binaryContent = new byte[0];
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
            return Category == FileCategory.KEXE ? BinaryContent.Length : StringContent.Length;
        }

        public ConfigNode SaveEncoded(string nodeName)
        {
            var node = new ConfigNode(nodeName);
            node.AddValue(FILENAME_VALUE_STRING, Filename);
            node.AddValue(MODIFIED_DATE_VALUE_STRING, ModifiedDate.ToString("s"));
            node.AddValue(CREATED_DATE_VALUE_STRING, CreatedDate.ToString("s"));

            if (Category == FileCategory.KEXE)
            {
                node.AddValue("line", EncodeBase64(BinaryContent));
            }
            else
            {
                if (Config.Instance.UseCompressedPersistence)
                {
                    node.AddValue("line", EncodeBase64(StringContent));
                }
                else
                {
                    node.AddValue("line", EncodeLine(StringContent));
                }
            }
                
            return node;
        }

        internal void LoadEncoded(ConfigNode fileNode)
        {
            Filename = fileNode.GetValue(FILENAME_VALUE_STRING);
            Decode(fileNode.GetValue("line"));

            if (fileNode.HasValue(MODIFIED_DATE_VALUE_STRING))
            {
                ModifiedDate = Convert.ToDateTime(fileNode.GetValue(MODIFIED_DATE_VALUE_STRING));
            }
            else
            {
                ModifiedDate = DateTime.MinValue;
            }

            if (fileNode.HasValue(CREATED_DATE_VALUE_STRING))
            {
                ModifiedDate = Convert.ToDateTime(fileNode.GetValue(CREATED_DATE_VALUE_STRING));
            }
            else
            {
                CreatedDate = DateTime.MinValue;
            }
        }

        private void Decode(string input)
        {
            try
            {
                string decodedString;
                try
                {
                    // base64 encoding
                    byte[] decodedBuffer = DecodeBase64ToBinary(input);
                    FileCategory whatKind = IdentifyCategory(decodedBuffer);
                    if (whatKind == FileCategory.ASCII || whatKind == FileCategory.KERBOSCRIPT)
                    {
                        decodedString = Encoding.ASCII.GetString(decodedBuffer);
                        StringContent = decodedString;
                    }
                    else
                    {
                        BinaryContent = decodedBuffer;
                    }
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
        }

        private string EncodeBase64(string input)
        {
            return EncodeBase64(Encoding.ASCII.GetBytes(input));
        }

        private string EncodeBase64(byte[] input)
        {
            using (var compressedStream = new MemoryStream())
            {
                // mono requires an installed zlib library for GZipStream to work :(
                // using (Stream csStream = new GZipStream(compressedStream, CompressionMode.Compress))
                using (Stream csStream = new GZipOutputStream(compressedStream))
                {
                    csStream.Write(input, 0, input.Length);
                }

                return Convert.ToBase64String(compressedStream.ToArray());
            }
        }


        private byte[] DecodeBase64ToBinary(string input)
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

                return decompressedStream.ToArray();
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
        /// Given the first few bytes of content, decide what the FileCategory
        /// should be, based on what's in the Content.<br/>
        /// This should be called before deciding how to set the content.
        /// </summary>
        /// <param name="firstBytes">At least the first four bytes of the file read in binary form - can be longer if you wish</param>
        /// <returns>The type that should be used to store this file.</returns>
        public static FileCategory IdentifyCategory(byte[] firstBytes)
        {
            var returnCat  = FileCategory.UNKNOWN; // default if none of the conditions pass
            var firstFour = new Byte[4];
            int atMostFour = Math.Min(4,firstBytes.Length);
            Array.Copy(firstBytes,0,firstFour,0,atMostFour);
                        
            if (firstFour.SequenceEqual(CompiledObject.MagicId))
            {
                returnCat = FileCategory.KEXE;
            }
            else
            {
                bool isAscii = true;
                foreach (byte b in firstFour)
                {
                    if (b != (byte)'\n' && b != (byte)'\t' && b != (byte)'\r' && (b < (byte)32 || b > (byte)127))
                    {
                        isAscii = false;
                        break;
                    }
                }
                if (isAscii)
                    returnCat = FileCategory.ASCII;
            }
            return returnCat;

            // There is not currently an explicit test for KERBOSRIPT versus other types of ASCII.
            // At current, any time you want to test for is Kerboscript, make sure to test for is Ascii too,
            // since nothing causes a file to become type kerboscript yet.
        }
    }
}
