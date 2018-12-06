using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace kOS.Safe.Persistence
{
    [kOS.Safe.Utilities.KOSNomenclature("FileContent")]
    public class FileContent : SerializableStructure, IEnumerable<string>
    {
        private static readonly Encoding fileEncoding = Encoding.UTF8;
        private const string DumpContent = "content";
        public const string NewLine = "\n";

        public byte[] Bytes { get; private set; }
        public string String { get { return fileEncoding.GetString(Bytes); } }

        public int Size { get { return Bytes.Length; } }
        public FileCategory Category { get { return PersistenceUtilities.IdentifyCategory(Bytes); } }

        public FileContent()
        {
            Bytes = new byte[0];

            InitializeSuffixes();
        }

        public FileContent(string content) : this()
        {
            Bytes = fileEncoding.GetBytes(content);
        }

        public FileContent(byte[] content) : this()
        {
            Bytes = CopyBytesSkippingBOM(content);
        }

        public FileContent(List<CodePart> parts) : this()
        {
            Bytes = CompiledObject.Pack(parts);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("LENGTH", new Suffix<ScalarIntValue>(() => Size));
            AddSuffix("EMPTY", new Suffix<BooleanValue>(() => Size == 0));
            AddSuffix("TYPE", new Suffix<StringValue>(() => Category.ToString()));
            AddSuffix("STRING", new Suffix<StringValue>(() => String));
            AddSuffix("ITERATOR", new Suffix<Enumerator>(() => new Enumerator(GetEnumerator())));
        }

        public override Dump Dump()
        {
            return new Dump { { DumpContent, PersistenceUtilities.EncodeBase64(Bytes) } };
        }

        public override void LoadDump(Dump dump)
        {
            string contentString = dump[DumpContent] as string;

            if (contentString == null)
            {
                throw new KOSSerializationException("'content' field not found or invalid");
            }

            Bytes = PersistenceUtilities.DecodeBase64ToBinary(contentString);
        }

        public List<CodePart> AsParts(GlobalPath path, string prefix)
        {
            return CompiledObject.UnPack(path, prefix, Bytes);
        }

        public static byte[] EncodeString(string content)
        {
            return fileEncoding.GetBytes(content);
        }

        public static string DecodeString(byte[] content)
        {
            return fileEncoding.GetString(content);
        }

        // If the raw bytes content has the unneessary BOM marker that some editors (*cough*, Notepad) put
        // in UTF-8 files, then make a copy that skips over it: (google "UTF-8 BOM" to see what this means)
        // If it does not contain the BOM, then this just makes a copy as-is of the bytes.
        private static byte[] CopyBytesSkippingBOM(byte[] content)
        {
            int sourceStart = 0;
            int sourceLength = content.Length;

            // If it starts with the magic 3-byte code for the BOM, then skip over those 3 bytes when copying:
            if (sourceLength >= 3 && (content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF))
            {
                sourceStart += 3;
                sourceLength -= 3;
            }
            byte[] returnVal = new byte[sourceLength];
            Array.Copy(content, sourceStart, returnVal, 0, sourceLength);
            return returnVal;
        }

        public void Write(string contentToWrite)
        {
            Write(EncodeString(contentToWrite));
        }

        public void Write(byte[] contentToWrite)
        {
            byte[] newContent = new byte[Bytes.Length + contentToWrite.Length];
            Buffer.BlockCopy(Bytes, 0, newContent, 0, Bytes.Length);
            Buffer.BlockCopy(contentToWrite, 0, newContent, Bytes.Length, contentToWrite.Length);
            Bytes = newContent;
        }

        public void WriteLn(string content)
        {
            Write(content + NewLine);
        }

        public void Clear()
        {
            Bytes = new byte[0];
        }

        public IEnumerator<string> GetEnumerator()
        {
            var reader = new StringReader(String);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            return obj is FileContent && Bytes.Equals((obj as FileContent).Bytes);
        }

        public override int GetHashCode()
        {
            return Bytes.GetHashCode();
        }

        public override string ToString()
        {
            return "File content";
        }
    }
}