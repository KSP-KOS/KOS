using System.Collections.Generic;
using System.Linq;
using System;
using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Persistence
{
    public abstract class Volume : Structure
    {
        protected const int BASE_CAPACITY = 10000;
        protected const float BASE_POWER = 0.04f;

        protected Volume()
        {
            Renameable = true;
            Capacity = -1;
            Name = "";
            Files = new Dictionary<string, ProgramFile>();
            InitializeVolumeSuffixes();
        }

        private void InitializeVolumeSuffixes()
        {
            AddSuffix("FREESPACE" , new Suffix<float>(() => GetFreeSpace()));
            AddSuffix("CAPACITY" , new Suffix<float>(() => Capacity));
            AddSuffix("NAME" , new Suffix<string>(() => Name));
            AddSuffix("RENAMEABLE" , new Suffix<string>(() => Name));
            AddSuffix("FILES" , new Suffix<ListValue>(() => ListValue.CreateList(GetFileList())));
            AddSuffix("POWERREQUIREMENT" , new Suffix<float>(RequiredPower));
        }


        protected Dictionary<string, ProgramFile> Files { get; private set; }
        public string Name { get; set; }
        public int Capacity { get; protected set; }
        public bool Renameable { get; protected set; }


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

            if (file.StringContent.Length > 0 && !file.StringContent.EndsWith("\n"))
            {
                textToAppend = "\n" + textToAppend;
            }

            file.StringContent = file.StringContent + textToAppend;
            SaveFile(file);
        }

        public virtual void AppendToFile(string name, byte[] bytesToAppend)
        {
            ProgramFile file = GetByName(name) ?? new ProgramFile(name);

            file.BinaryContent = new byte[file.BinaryContent.Length + bytesToAppend.Length];
            Array.Copy(bytesToAppend, 0, file.BinaryContent, file.BinaryContent.Length, bytesToAppend.Length);
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
            ProgramFile newFile = new ProgramFile(fileNameOut);
            newFile.BinaryContent = CompiledObject.Pack(parts);
            SaveFile(newFile);
            return true;
        }

        public List<CodePart> LoadObjectFile(string filePath, string prefix, byte[] content)
        {
            List<CodePart> parts = CompiledObject.UnPack(filePath, prefix , content);
            return parts;
        }

        protected int GetUsedSpace()
        {
            return Files.Values.Sum(file => file.GetSize());
        }

        public virtual int GetFreeSpace() { return -1; }
        public virtual bool IsRoomFor(ProgramFile newFile) { return true; }
        public virtual void LoadPrograms(IEnumerable<ProgramFile> programsToLoad) { }
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
