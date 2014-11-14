using System;
using kOS.Safe.Persistence;

namespace kOS.Safe.Encapsulation
{
    public class FileInfo : Structure
    {
        public string Name { get; private set; }
        public int Size { get; private set; }
        public FileCategory Category { get; private set; }

        public FileInfo(string name, int size, FileCategory category)
        {
            Name = name;
            Size = size;
            Category = category;
        }

        public FileInfo(System.IO.FileInfo fileInfo)
        {
            Name = fileInfo.Name;
            Size = (int) fileInfo.Length;
            switch (fileInfo.Extension)
            {
                case Volume.KERBOSCRIPT_EXTENSION:
                    Category = FileCategory.ASCII;
                    break;
                case Volume.KOS_MACHINELANGUAGE_EXTENSION:
                    Category = FileCategory.KSM;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unsupported File Extension" + fileInfo.Extension);
            }
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
                    return Category;
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
