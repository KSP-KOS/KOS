using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Persistence
{
    public class Volume
    {
        protected const int BASE_CAPACITY = 10000;
        protected const float BASE_POWER = 0.02f;

        protected Volume()
        {
            Renameable = true;
            Capacity = -1;
            Name = "";
            _files = new Dictionary<string, ProgramFile>();
        }

        protected Dictionary<string, ProgramFile> _files;
        public string Name { get; set; }
        public int Capacity { get; set; }
        public bool Renameable { get; set; }


        public virtual ProgramFile GetByName(string name)
        {
            name = name.ToLower();
            if (_files.ContainsKey(name))
            {
                return _files[name];
            }
            else
            {
                return null;
            }
        }

        public virtual bool DeleteByName(string name)
        {
            name = name.ToLower();
            if (_files.ContainsKey(name))
            {
                _files.Remove(name);
                return true;
            }
            else
            {
                return false;
            }
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
            else
            {
                return false;
            }
        }

        public virtual void AppendToFile(string name, string textToAppend)
        {
            ProgramFile file = GetByName(name);
            if (file == null)
            {
                file = new ProgramFile(name);
            }

            if (file.Content.Length > 0 && !file.Content.EndsWith("\n"))
            {
                textToAppend = "\n" + textToAppend;
            }

            file.Content += textToAppend;
            SaveFile(file);
        }

        public virtual void Add(ProgramFile file)
        {
            _files.Add(file.Filename.ToLower(), file);
        }

        public virtual bool SaveFile(ProgramFile file)
        {
            DeleteByName(file.Filename);
            Add(file);
            return true;
        }

        public virtual int GetUsedSpace()
        {
            int usedSpace = 0;

            foreach (ProgramFile file in _files.Values)
            {
                usedSpace += file.GetSize();
            }

            return usedSpace;
        }

        public virtual int GetFreeSpace() { return -1; }
        public virtual bool IsRoomFor(ProgramFile newFile) { return true; }
        public virtual void LoadPrograms(List<ProgramFile> programsToLoad) { }
        public virtual ConfigNode Save(string nodeName) { return new ConfigNode(nodeName); }

        public virtual List<FileInfo> GetFileList()
        {
            List<FileInfo> retList = new List<FileInfo>();

            foreach (ProgramFile file in _files.Values)
            {
                retList.Add(new FileInfo(file.Filename, file.GetSize()));
            }

            return retList;
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
