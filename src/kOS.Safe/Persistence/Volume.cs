using System;
using System.Collections.Generic;
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
        public const string TEXT_EXTENSION = "txt";
        public const string KERBOSCRIPT_EXTENSION = "ks";
        public const string KOS_MACHINELANGUAGE_EXTENSION = "ksm";
        protected const int BASE_CAPACITY = 10000;
        protected const float BASE_POWER = 0.04f;
        private readonly Dictionary<string, ProgramFile> files;

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

        /// <summary>
        /// Get a file given its name
        /// </summary>
        /// <param name="name">filename to get.  if it has no filename extension, one will be guessed at, ".ks" usually.</param>
        /// <param name="ksmDefault">in the scenario where there is no filename extension, do we prefer the .ksm over the .ks?  The default is to prefer .ks</param>
        /// <returns>the file</returns>
        public virtual ProgramFile GetByName(string name, bool ksmDefault = false )
        {
            // TODO for @erendrake:
            // ----------------------
            // ksmDefault is currently unused here because local volumes don't
            // seem to have analogies to what's in Archive volumes.  The archive
            // class has a FileSearch() method that does the logic needed to
            // use the ksmDefault argument, and the Volume class doesn't.
            // That's why I didn't implement this but just left that argument in
            // there as a stub, so you can fill this out when the Volume class 
            // gets some of that logic put back into it again.
            
            Debug.Logger.SuperVerbose("Volume: GetByName: " + name);
            name = name.ToLower();
            if (files.ContainsKey(name))
            {
                return files[name];
            }
            return null;
        }

        public virtual bool DeleteByName(string name)
        {
            Debug.Logger.SuperVerbose("Volume: DeleteByName: " + name);
            name = name.ToLower();
            Debug.Logger.SuperVerbose("Volume:  eraseme:  DeleteByName modified name: " + name);
            if (files.ContainsKey(name))
            {
                Debug.Logger.SuperVerbose("Volume:  eraseme:  will remove name.");
                files.Remove(name);
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
            files.Add(file.Filename.ToLower(), file);
        }

        public virtual bool SaveFile(ProgramFile file)
        {
            Debug.Logger.SuperVerbose("Volume: SaveFile: " + file.Filename);
            
            //TODO:Chris eraseme
            //  |
            //  |
            //  |
            //  `---- Actually I got it working with this DeleteByName in place, and I think it became integral to the design - Steven
            //
            DeleteByName(file.Filename);
            
            Add(file);
            return true;
        }
        
        public virtual bool SaveObjectFile(string fileNameOut, List<CodePart> parts)
        {
            var newFile = new ProgramFile(fileNameOut) {BinaryContent = CompiledObject.Pack(parts)};
            SaveFile(newFile);
            return true;
        }

        public List<CodePart> LoadObjectFile(string filePath, string prefix, byte[] content)
        {
            Debug.Logger.SuperVerbose("Volume: LoadObjectFile: " + filePath);
            List<CodePart> parts = CompiledObject.UnPack(filePath, prefix, content);
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
            return files.Values.Select(file => new FileInfo(file.Filename, file.GetSize(), file.Category)).ToList();
        }

        public virtual float RequiredPower()
        {
            var multiplier = ((float)Capacity) / BASE_CAPACITY;
            var powerRequired = BASE_POWER * multiplier;

            return powerRequired;
        }
    }    
}
