using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using FileInfo = kOS.Safe.Encapsulation.FileInfo;

namespace kOS.Safe.Persistence
{
    public abstract class Volume : Structure
    {
        public const string KERBOSCRIPT_EXTENSION = "ks";
        public const string KOS_MACHINELANGUAGE_EXTENSION = "ksm";
        protected const int BASE_CAPACITY = 10000;
        protected const float BASE_POWER = 0.04f;
        private readonly Dictionary<string, ProgramFile> files;

        protected Volume()
        {
            Debug.Logger.SuperVerbose("Volume: CONSTRUCT");
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
                Debug.Logger.SuperVerbose("Volume: Get-FileList: " + files.Count);
                return files.ToDictionary(pair => pair.Key, pair => pair.Value);
            }
        }
        public string Name { get; set; }
        public int Capacity { get; protected set; }
        public bool Renameable { get; protected set; }


        public virtual ProgramFile GetByName(string name)
        {
            Debug.Logger.SuperVerbose("Volume: GetByName: " + name);
            var fullPath = FileSearch(name);
            if (fullPath == null)
            {
                return null;
            }
            if (files.ContainsKey(fullPath.Filename))
            {
                return files[fullPath.Filename];
            }
            return null;
        }

        public virtual bool DeleteByName(string name)
        {
            Debug.Logger.SuperVerbose("Volume: DeleteByName: " + name);
            var fullPath = FileSearch(name);
            if (fullPath == null)
            {
                return false;
            }
            if (files.ContainsKey(fullPath.Filename))
            {
                files.Remove(fullPath.Filename);
                return true;
            }
            return false;
        }

        public virtual bool RenameFile(string name, string newName)
        {
            Debug.Logger.SuperVerbose("Volume: RenameFile: From: " + name + " To: " + newName);
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
            Debug.Logger.SuperVerbose("Volume: AppendToFile: " + name);
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
            Debug.Logger.SuperVerbose("Volume: AppendToFile: " + name);
            ProgramFile file = GetByName(name) ?? new ProgramFile(name);

            file.BinaryContent = new byte[file.BinaryContent.Length + bytesToAppend.Length];
            Array.Copy(bytesToAppend, 0, file.BinaryContent, file.BinaryContent.Length, bytesToAppend.Length);
            SaveFile(file);
        }

        public virtual void Add(ProgramFile file)
        {
            Debug.Logger.SuperVerbose("Volume: Add: " + file.Filename);
            ProgramFile existing;

            if (files.TryGetValue(file.Filename, out existing))
            {
                files[file.Filename] = file;
            }
            else
            {
                files.Add(file.Filename, file);
            }
        }

        public virtual bool SaveFile(ProgramFile file)
        {
            Debug.Logger.SuperVerbose("Volume: SafeFile: " + file.Filename);
            Add(file);
            return true;
        }
        
        public virtual bool SaveObjectFile(string fileNameOut, List<CodePart> parts)
        {
            Debug.Logger.SuperVerbose("Volume: SaveObjectFile: " + fileNameOut);
            if (!fileNameOut.Contains('.'))
            {
                fileNameOut = string.Format("{0}.{1}", fileNameOut, KOS_MACHINELANGUAGE_EXTENSION);
            }
            var newFile = new ProgramFile(fileNameOut) {BinaryContent = CompiledObject.Pack(parts)};
            SaveFile(newFile);
            return true;
        }

        public List<CodePart> LoadObjectFile(string filePath, string prefix, byte[] content)
        {
            Debug.Logger.SuperVerbose("Volume: LoadObjectFile: " + filePath);
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
            Debug.Logger.SuperVerbose("Volume: GetFileList: " + files.Count);
            return files.Values.Select(file => new FileInfo(file.Filename, file.GetSize(), file.CreatedDate, file.ModifiedDate, file.Category)).ToList();
        }

        public virtual float RequiredPower()
        {
            var multiplier = ((float)Capacity) / BASE_CAPACITY;
            var powerRequired = BASE_POWER * multiplier;

            return powerRequired;
        }

        private ProgramFile FileSearch(string name)
        {
            Debug.Logger.SuperVerbose("Volume: FileSearch: " + files.Count);
            var kerboscriptFilename = string.Format("{0}.{1}", name, KERBOSCRIPT_EXTENSION);
            var kosMlFilename = string.Format("{0}.{1}", name, KOS_MACHINELANGUAGE_EXTENSION);

            ProgramFile kerboscriptFile;
            ProgramFile kosMlFile;
            bool kerboscriptFileExists = files.TryGetValue(kerboscriptFilename, out kerboscriptFile);
            bool kosMlFileExists = files.TryGetValue(kosMlFilename, out kosMlFile);
            if (kerboscriptFileExists && kosMlFileExists)
            {
                return kerboscriptFile.ModifiedDate > kosMlFile.ModifiedDate
                    ? kerboscriptFile : kosMlFile;
            }
            if (kerboscriptFile != null)
            {
                return kerboscriptFile;
            }
            if (kosMlFile != null)
            {
                return kosMlFile;
            }
            return null;
        }
    }    
}
