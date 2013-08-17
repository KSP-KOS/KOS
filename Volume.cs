using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kOS
{
    public class Volume
    {
        public int Capacity = -1;
        public String Name = "";
        public List<File> Files = new List<File>();
        public bool Renameable = true;

        public File GetByName(String name)
        {
            foreach (File p in Files)
            {
                if (p.Filename.ToUpper() == name.ToUpper()) return p;
            }

            return null;
        }

        public void DeleteByName(String name)
        {
            foreach (File p in Files)
            {
                if (p.Filename.ToUpper() == name.ToUpper())
                {
                    Files.Remove(p);
                    return;
                }
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
    }

    public class Archive : Volume
    {
        public Archive()
        {
            Renameable = false;
            Name = "Archive";

            loadAll();
        }

        public override bool IsRoomFor(File newFile)
        {
            return true;
        }

        private void loadAll()
        {
            Files.Clear();

            try
            {
                if (KSP.IO.File.Exists<File>(HighLogic.fetch.GameSaveFolder + "/arc"))
                {
                    var reader = KSP.IO.BinaryReader.CreateForType<File>(HighLogic.fetch.GameSaveFolder + "/arc");

                    int fileCount = reader.ReadInt32();

                    for (int i = 0; i < fileCount; i++)
                    {
                        String filename = reader.ReadString();
                        String body = reader.ReadString();

                        File file = new File(filename);
                        file.Deserialize(body);

                        Files.Add(file);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public override bool SaveFile(File file)
        {
            base.SaveFile(file);

            try
            {
                if (HighLogic.fetch != null)
                {
                    var writer = KSP.IO.BinaryWriter.CreateForType<File>(HighLogic.fetch.GameSaveFolder + "/arc");

                    writer.Write(Files.Count);

                    foreach (File f in Files)
                    {
                        writer.Write(f.Filename);
                        writer.Write(f.Serialize());
                    }

                    writer.Close();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }
}
