namespace kOS.Suffixed
{
    public class FileInfo : SpecialValue
    {
        public string Name;
        public int Size;

        public FileInfo(string name, int size)
        {
            Name = name;
            Size = size;
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
