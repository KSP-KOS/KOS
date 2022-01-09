using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using System.Linq;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Persistence
{
    [kOS.Safe.Utilities.KOSNomenclature("VolumeFile")]
    public abstract class VolumeFile : VolumeItem
    {
        protected VolumeFile(Volume volume, VolumePath path) : base(volume, path)
        {
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
            return Write(content + FileContent.NewLine);
        }

        public abstract void Clear();

        /*
        public override string ToString()
        {
            return Name;
        }
        */

        private void InitializeSuffixes()
        {
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