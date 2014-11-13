using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace kOS.Safe.Persistence
{
    public abstract class Volume : Structure
    {
        protected const int BASE_CAPACITY = 10000;
        protected const float BASE_POWER = 0.04f;
        private readonly Dictionary<string, ProgramFile> files;

        protected Volume()
        {
            Debug.Logger.Log("Volume: CONSTRUCT");
            Renameable = true;
            Capacity = -1;
            Name = "";
            files = new Dictionary<string, ProgramFile>(StringComparer.CurrentCultureIgnoreCase);
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

        public Dictionary<string, ProgramFile> FileList
        {
            get
            {
                Debug.Logger.Log("Volume: Get-FileList: " + files.Count);
                return files.ToDictionary(pair => pair.Key, pair => pair.Value);
            }
        }
        public string Name { get; set; }
        public int Capacity { get; protected set; }
        public bool Renameable { get; protected set; }


        public virtual ProgramFile GetByName(string name)
        {
            Debug.Logger.Log("Volume: GetByName: " + name);
            if (files.ContainsKey(name))
            {
                return files[name];
            }
            return null;
        }

        public virtual bool DeleteByName(string name)
        {
            Debug.Logger.Log("Volume: DeleteByName: " + name);
            if (files.ContainsKey(name))
            {
                files.Remove(name);
                return true;
            }
            return false;
        }

        public virtual bool RenameFile(string name, string newName)
        {
            Debug.Logger.Log("Volume: RenameFile: From: " + name + " To: " + newName);
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
            Debug.Logger.Log("Volume: AppendToFile: " + name);
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
            Debug.Logger.Log("Volume: AppendToFile: " + name);
            ProgramFile file = GetByName(name) ?? new ProgramFile(name);

            file.BinaryContent = new byte[file.BinaryContent.Length + bytesToAppend.Length];
            Array.Copy(bytesToAppend, 0, file.BinaryContent, file.BinaryContent.Length, bytesToAppend.Length);
            SaveFile(file);
        }

        public virtual void Add(ProgramFile file)
        {
            Debug.Logger.Log("Volume: Add: " + file.Filename);
            files.Add(file.Filename, file);
        }

        public virtual bool SaveFile(ProgramFile file)
        {
            Debug.Logger.Log("Volume: SafeFile: " + file.Filename);
            ProgramFile existing;
            if (!files.TryGetValue(file.Filename, out existing))
            {
                files.Add(file.Filename, file);
            }
            DeleteByName(file.Filename);
            return true;
        }
        
        public virtual bool SaveObjectFile(string fileNameOut, List<CodePart> parts)
        {
            Debug.Logger.Log("Volume: SaveObjectFile: " + fileNameOut);
            var newFile = new ProgramFile(fileNameOut) {BinaryContent = CompiledObject.Pack(parts)};
            SaveFile(newFile);
            return true;
        }

        public List<CodePart> LoadObjectFile(string filePath, string prefix, byte[] content)
        {
            Debug.Logger.Log("Volume: LoadObjectFile: " + filePath);
            List<CodePart> parts = CompiledObject.UnPack(filePath, prefix , content);
            return parts;
        }

        protected int GetUsedSpace()
        {
            return files.Values.Sum(file => file.GetSize());
        }

        public virtual int GetFreeSpace() { return -1; }
        public virtual bool IsRoomFor(ProgramFile newFile) { return true; }

        public virtual List<FileInfo> GetFileList()
        {
            Debug.Logger.Log("Volume: GetFileList: " + files.Count);
            return files.Values.Select(file => new FileInfo(file.Filename, file.GetSize(), file.CreatedDate, file.ModifiedDate)).ToList();
        }

        public virtual float RequiredPower()
        {
            var multiplier = ((float)Capacity) / BASE_CAPACITY;
            var powerRequired = BASE_POWER * multiplier;

            return powerRequired;
        }
    }    
}
