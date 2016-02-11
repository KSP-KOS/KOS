using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Encapsulation
{
    public abstract class VolumeFile : Structure
    {
        public string Name { get; private set; }

        public abstract int Size { get; }
        public string Extension { get {
                var fileParts = Name.Split('.');

                return fileParts.Count() > 1 ? fileParts.Last() : string.Empty;
            }
        }

        public VolumeFile(string name)
        {
            Name = name;

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => Name));
            AddSuffix("SIZE", new Suffix<ScalarIntValue>(() => new ScalarIntValue(Size)));
            AddSuffix("EXTENSION", new Suffix<StringValue>(() => Extension));

            AddSuffix("READALL", new Suffix<FileContent>(ReadAll));
            AddSuffix("WRITE", new OneArgsSuffix<BooleanValue, Structure>((str) => WriteObject(str)));
            AddSuffix("WRITELN", new OneArgsSuffix<BooleanValue, StringValue>((str) => new BooleanValue(WriteLn(str))));
            AddSuffix("CLEAR", new NoArgsVoidSuffix(Clear));
        }

        private bool WriteObject(Structure content)
        {
            if (content is StringValue)
            {
                return Write(content.ToString());
            } else if (content is FileContent)
            {
                FileContent fileContent = (FileContent)content;
                return Write(fileContent.Bytes);
            } else {
                throw new KOSException("Only instances of string and FileContent can be written");
            }
        }

        public abstract FileContent ReadAll();
        public abstract bool Write(byte[] content);
        public abstract bool WriteLn(string content);

        public bool Write(string content)
        {
            return Write(FileContent.EncodeString(content));
        }

        public abstract void Clear();

        public override string ToString()
        {
            return Name;
        }
    }
}
