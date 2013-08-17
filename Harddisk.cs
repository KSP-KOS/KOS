using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kOS
{
    public class Harddisk : Volume
    {
        public Harddisk(int size)
        {
            this.Capacity = size;
        }

        public Harddisk(ConfigNode node)
        {
            Capacity = 10000;

            foreach (String s in node.GetValues("capacity"))
            {
                Capacity = Int32.Parse(s);
            }

            foreach (String s in node.GetValues("volumeName"))
            {
                Name = s;
            }

            foreach (ConfigNode fileNode in node.GetNodes("file"))
            {
                Files.Add(new File(fileNode));
            }
        }

        public override bool SaveFile(File file)
        {
            if (!IsRoomFor(file)) return false;

            return base.SaveFile(file);
        }

        public override int GetFreeSpace()
        {
            int totalOccupied = 0;
            foreach (File p in Files)
            {
                totalOccupied += p.GetSize();
            }

            return Capacity - totalOccupied;
        }

        public override bool IsRoomFor(File newFile)
        {
            int totalOccupied = newFile.GetSize();
            foreach (File existingFile in Files)
            {
                // Consider only existing files that don't share a name with the proposed new file
                // Because this could be an overwrite situation
                if (existingFile.Filename != newFile.Filename)
                {
                    totalOccupied += existingFile.GetSize();
                }
            }

            return (Capacity - totalOccupied > 0);
        }

        public override void LoadPrograms(List<File> programsToLoad)
        {
            foreach (File p in programsToLoad)
            {
                Files.Add(p);
            }
        }

        public override ConfigNode Save(string nodeName)
        {
            ConfigNode node = new ConfigNode(nodeName);
            node.AddValue("capacity", Capacity);
            node.AddValue("volumeName", Name);

            foreach (File file in Files)
            {
                node.AddNode(file.Save("file"));
            }
            
            return node;
        }
    }
}
