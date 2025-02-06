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
using System.Linq;

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
            Bytes = content;
        }

        public FileContent(List<CodePart> parts) : this()
        {
            Bytes = CompiledObject.Pack(parts);
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static FileContent CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new FileContent();
            newObj.LoadDump(d);
            return newObj;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("LENGTH", new Suffix<ScalarIntValue>(() => Size));
            AddSuffix("EMPTY", new Suffix<BooleanValue>(() => Size == 0));
            AddSuffix("TYPE", new Suffix<StringValue>(() => Category.ToString()));
            AddSuffix("STRING", new Suffix<StringValue>(() => String));
            AddSuffix("BINARY", new Suffix<ListValue>(() => new ListValue(Bytes.Select(x => new ScalarIntValue(x)))));
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