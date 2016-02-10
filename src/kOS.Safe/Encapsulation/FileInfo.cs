using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public class FileInfo : Structure
    {
        private string name;

        public string Name
        {
            get { return name; }
            private set
            {
                name = value;

                var fileParts = name.Split('.');
                Extension = fileParts.Length > 1 ? fileParts.Last() : string.Empty;
            }
        }

        public int Size { get; private set; }
        public string Extension { get; private set; }

        public FileInfo(string name, int size)
        {
            Name = name;
            Size = size;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => Name));
            AddSuffix("SIZE", new Suffix<ScalarValue>(() => Size));
            AddSuffix("FILETYPE", new Suffix<StringValue>(() => Extension));
        }

        public FileInfo(System.IO.FileInfo fileInfo)
        {
            Name = fileInfo.Name;
            Size = (int) fileInfo.Length;
            InitializeSuffixes();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
