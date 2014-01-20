using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Persistance
{
    public class Volume
    {
        public int Capacity = -1;
        public String Name = "";

        public bool Renameable = true;

        public Volume()
        {
            Files = new List<File>();
        }

        public List<File> Files { get; set; }

        public virtual File GetByName(String name)
        {
            return Files.FirstOrDefault(p => p.Filename.ToUpper() == name.ToUpper());
        }

        public virtual void AppendToFile(string name, string str) 
        {
            var file = GetByName(name) ?? new File(name);

            file.Add(str);

            SaveFile(file);
        }

        public virtual void DeleteByName(String name)
        {
            foreach (var p in Files.Where(p => p.Filename.ToUpper() == name.ToUpper()))
            {
                Files.Remove(p);
                return;
            }
        }

        public virtual bool SaveFile(File file)
        {
            DeleteByName(file.Filename);
            Files.Add(file);

            return true;
        }

        public virtual int GetFreeSpace() { return -1; }
        public virtual bool IsRoomFor(File newFile) { return true; }
        public virtual void LoadPrograms(List<File> programsToLoad) { }
        public virtual ConfigNode Save(string nodeName) { return new ConfigNode(nodeName); }

        public virtual List<FileInfo> GetFileList()
        {
            return Files.Select(file => new FileInfo(file.Filename, file.GetSize())).ToList();
        }

        public virtual bool CheckRange()
        {
            return true;
        }
    }
}
