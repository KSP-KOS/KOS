using System;

namespace kOS.Safe.Encapsulation
{
    public class FileInfo : Structure
    {
        public string Name { get; private set; }
        public int Size { get; private set; }
        public DateTime Created { get; private set; }
        public DateTime Modified { get; private set; }

        public FileInfo(string name, int size, DateTime created, DateTime modified)
        {
            Name = name;
            Size = size;
            Created = created;
            Modified = modified;
        }

        public FileInfo(System.IO.FileInfo fileInfo)
        {
            Name = fileInfo.Name;
            Size = (int) fileInfo.Length;
            Created = fileInfo.CreationTime;
            Modified = fileInfo.LastWriteTime;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "NAME":
                    return Name;
                case "SIZE":
                    return Size;
                case "CREATED":
                    return Modified.ToString("s");
                case "MODIFIED":
                    return Modified.ToString("s");
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
