using System.Linq;

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
                Extension = fileParts.Count() > 1 ? fileParts.Last() : string.Empty;
            }
        }

        public int Size { get; private set; }
        public string Extension { get; private set; }

        public FileInfo(string name, int size)
        {
            Name = name;
            Size = size;
        }

        public FileInfo(System.IO.FileInfo fileInfo)
        {
            Name = fileInfo.Name;
            Size = (int) fileInfo.Length;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "NAME":
                    return Name;
                case "SIZE":
                    return Size;
                case "FILETYPE":
                    return Extension;
            }

            return base.GetSuffix(suffixName);
        }

        public override bool KOSEquals(object other)
        {
            FileInfo otherFileInfo = other as FileInfo;
            if (otherFileInfo == null) return false;
            return this.Name == otherFileInfo.Name && this.Extension == otherFileInfo.Extension && this.Size == otherFileInfo.Size;
        } 

        public override string ToString()
        {
            return Name;
        }
    }
}
