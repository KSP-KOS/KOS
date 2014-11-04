namespace kOS.Safe.Encapsulation
{
    public class FileInfo : Structure
    {
        public string Name { get; private set; }
        public int Size { get; private set; }

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
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
