using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using System.Linq;
using kOS.Safe.Persistence;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("VolumeFile")]
    public abstract class VolumeFile : Structure
    {
        public string Name { get; private set; }

        public abstract int Size { get; }

        public string Extension
        {
            get
            {
                var fileParts = Name.Split('.');

                return fileParts.Length > 1 ? fileParts.Last() : string.Empty;
            }
        }

        protected VolumeFile(string name)
        {
            Name = name;

            InitializeSuffixes();
        }

        public abstract FileContent ReadAll();

        public abstract bool Write(byte[] content);

        public bool Write(string content)
        {
            return Write(FileContent.EncodeString(content));
        }

        public bool WriteLn(string content)
        {
            return Write(content + FileContent.NEW_LINE);
        }

        public abstract void Clear();

        public override string ToString()
        {
            return Name;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => Name));
            AddSuffix("SIZE", new Suffix<ScalarIntValue>(() => new ScalarIntValue(Size)));
            AddSuffix("EXTENSION", new Suffix<StringValue>(() => Extension));

            AddSuffix("READALL", new Suffix<FileContent>(ReadAll));
            AddSuffix("WRITE", new OneArgsSuffix<BooleanValue, Structure>(str => WriteObject(str)));
            AddSuffix("WRITELN", new OneArgsSuffix<BooleanValue, StringValue>(str => new BooleanValue(WriteLn(str))));
            AddSuffix("CLEAR", new NoArgsVoidSuffix(Clear));
        }

        private bool WriteObject(Structure content)
        {
            if (content is StringValue)
            {
                return Write(content.ToString());
            }

            var stringValue = content as FileContent;
            if (stringValue != null)
            {
                FileContent fileContent = stringValue;
                return Write(fileContent.Bytes);
            }

            throw new KOSException("Only instances of string and FileContent can be written");
        }
    }
}