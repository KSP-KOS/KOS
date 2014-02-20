using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;

namespace kOS
{
    public class Volume
    {
        protected Dictionary<string, ProgramFile> _files = new Dictionary<string, ProgramFile>();

        public int Id = 0;
        public int Capacity = -1;
        public string Name = "";
        public bool Renameable = true;


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

        public string GetBestIdentifier()
        {
            if (!string.IsNullOrEmpty(Name)) return string.Format("#{0}: \"{1}\"", Id, Name);
            else return "#" + Id;
        }
    }
    
    public class Archive : Volume
    {
        private string _archiveFolder = GameDatabase.Instance.PluginDataFolder + "/Plugins/PluginData/Archive/";

        public Archive()
        {
            Renameable = false;
            Name = "Archive";
        }

        public override bool IsRoomFor(ProgramFile newFile)
        {
            return true;
        }

        public override ProgramFile GetByName(string name)
        {
            try
            {
                using (StreamReader infile = new StreamReader(_archiveFolder + name + ".txt", true))
                {
                    string fileBody = infile.ReadToEnd().Replace("\r\n", "\n") ;

                    ProgramFile retFile = new ProgramFile(name);
                    retFile.Content = fileBody;
                    base.DeleteByName(name);
                    base.Add(retFile);
                    
                    return retFile;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public override bool SaveFile(ProgramFile file)
        {
            base.SaveFile(file);

            Directory.CreateDirectory(_archiveFolder);

            try
            {
                using (StreamWriter outfile = new StreamWriter(_archiveFolder + file.Filename + ".txt", false))
                {
                    string fileBody = file.Content;

                    if (Application.platform == RuntimePlatform.WindowsPlayer)
                    {
                        // Only evil windows gets evil windows line breaks
                        fileBody = fileBody.Replace("\n", "\r\n");
                    }

                    outfile.Write(fileBody);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            return true;
        }

        public override bool DeleteByName(string name)
        {
            try
            {
                base.DeleteByName(name);
                System.IO.File.Delete(_archiveFolder + name + ".txt");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override List<FileInfo> GetFileList()
        {
            var retList = new List<FileInfo>();

            try
            {
                foreach (var file in Directory.GetFiles(_archiveFolder, "*.txt"))
                {
                    var sysFileInfo = new System.IO.FileInfo(file);
                    var fileInfo = new FileInfo(sysFileInfo.Name.Substring(0, sysFileInfo.Name.Length - 4), (int)sysFileInfo.Length);

                    retList.Add(fileInfo);
                }
            }
            catch (DirectoryNotFoundException)
            {
            }

            return retList;
        }

        public override bool CheckRange(Vessel vessel)
        {
            if (vessel != null)
            {
                return (VesselUtils.GetDistanceToKerbinSurface(vessel) < VesselUtils.GetCommRange(vessel));
            }
            else
            {
                return false;
            }
        }
    }
}
