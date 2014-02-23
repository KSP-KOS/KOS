using System.Collections.Generic;
using System.Linq;

namespace kOS.Persistance
{
    public abstract class Volume : IVolume
    {
        protected const int BASE_CAPACITY = 10000;
        protected const float BASE_POWER = 0.01f;

        protected Volume()
        {
            Renameable = true;
            Capacity = -1;
            Name = "";
            Files = new List<File>();
        }

        public IList<File> Files { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
        public bool Renameable { get; set; }

        public virtual File GetByName(string name)
        {
            return Files.FirstOrDefault(p => p.Filename.ToUpper() == name.ToUpper());
        }

        public virtual void AppendToFile(string name, string str)
        {
            var file = GetByName(name) ?? new File(name);

            file.Add(str);

            SaveFile(file);
        }

        public virtual void DeleteByName(string name)
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

        public virtual int GetFreeSpace()
        {
            return -1;
        }

        public virtual bool IsRoomFor(File newFile)
        {
            return true;
        }

        public virtual void LoadPrograms(IList<File> programsToLoad)
        {
        }

        public virtual ConfigNode Save(string nodeName)
        {
            return new ConfigNode(nodeName);
        }

        public virtual IList<FileInfo> GetFileList()
        {
            return Files.Select(file => new FileInfo(file.Filename, file.GetSize())).ToList();
        }

        public virtual bool CheckRange()
        {
            return true;
        }

        public virtual float RequiredPower()
        {
            var multiplier = ((float)Capacity) / BASE_CAPACITY;
            var powerRequired = BASE_POWER * multiplier;

            return powerRequired;
        }
    }
}