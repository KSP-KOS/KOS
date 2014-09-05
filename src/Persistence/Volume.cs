using System.Collections.Generic;
using System.Linq;
using kOS.Suffixed;
using kOS.Compilation;

namespace kOS.Persistence
{
    public class Volume
    {
        protected const int BASE_CAPACITY = 10000;
        protected const float BASE_POWER = 0.04f;

        protected Volume()
        {
            Renameable = true;
            Capacity = -1;
            Name = "";
            Files = new Dictionary<string, ProgramFile>();
        }

        protected Dictionary<string, ProgramFile> Files;
        public string Name { get; set; }
        public int Capacity { get; set; }
        public bool Renameable { get; set; }


        public virtual ProgramFile GetByName(string name)
        {
            name = name.ToLower();
            if (Files.ContainsKey(name))
            {
                return Files[name];
            }
            return null;
        }

        public virtual bool DeleteByName(string name)
        {
            name = name.ToLower();
            if (Files.ContainsKey(name))
            {
                Files.Remove(name);
                return true;
            }
            return false;
        }

        public virtual bool RenameFile(string name, string newName)
        {
            ProgramFile file = GetByName(name);
            if (file != null)
            {
                DeleteByName(name);
                file.Filename = newName;
                Add(file);
                return true;
            }
            return false;
        }

        public virtual void AppendToFile(string name, string textToAppend)
        {
            ProgramFile file = GetByName(name) ?? new ProgramFile(name);

            if (file.Content.Length > 0 && !file.Content.EndsWith("\n"))
            {
                textToAppend = "\n" + textToAppend;
            }

            file.Content += textToAppend;
            SaveFile(file);
        }

        public virtual void Add(ProgramFile file)
        {
            Files.Add(file.Filename.ToLower(), file);
        }

        public virtual bool SaveFile(ProgramFile file)
        {
            DeleteByName(file.Filename);
            Add(file);
            return true;
        }
        
        public virtual bool SaveObjectFile(string fileNameOut, List<CodePart> parts)
        {
            UnityEngine.Debug.Log("Checkpoint B01");
            ProgramFile newFile = new ProgramFile(fileNameOut);
            UnityEngine.Debug.Log("Checkpoint B02");
            newFile.Content = Compilation.CompiledObject.Pack(fileNameOut, parts);
            UnityEngine.Debug.Log("Checkpoint B03");
            SaveFile(newFile);
            UnityEngine.Debug.Log("Checkpoint B04");
            return true;
        }

        public virtual int GetUsedSpace()
        {
            return Files.Values.Sum(file => file.GetSize());
        }

        public virtual int GetFreeSpace() { return -1; }
        public virtual bool IsRoomFor(ProgramFile newFile) { return true; }
        public virtual void LoadPrograms(List<ProgramFile> programsToLoad) { }
        public virtual ConfigNode Save(string nodeName) { return new ConfigNode(nodeName); }

        public virtual List<FileInfo> GetFileList()
        {
            return Files.Values.Select(file => new FileInfo(file.Filename, file.GetSize())).ToList();
        }

        public virtual bool CheckRange(Vessel vessel)
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
