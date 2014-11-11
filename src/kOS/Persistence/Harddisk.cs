using System;
using System.Collections.Generic;
using kOS.Safe.Persistence;

namespace kOS.Persistence
{
    public sealed class Harddisk : Volume
    {
        public Harddisk(int size)
        {
            Capacity = size;
        }

        public Harddisk(ConfigNode node)
        {
            Capacity = 10000;
            
            if (node.HasValue("capacity")) Capacity = int.Parse(node.GetValue("capacity"));
            if (node.HasValue("volumeName")) Name = node.GetValue("volumeName");
            
            foreach (ConfigNode fileNode in node.GetNodes("file"))
            {
                Add(fileNode.ToProgramFile());
            }
        }

        public override bool SaveFile(ProgramFile file)
        {
            return IsRoomFor(file) && base.SaveFile(file);
        }

        public override int GetFreeSpace()
        {
            return Math.Max(Capacity - GetUsedSpace(), 0);
        }

        public override bool IsRoomFor(ProgramFile newFile)
        {
            int usedSpace = GetUsedSpace();
            ProgramFile existingFile = GetByName(newFile.Filename);

            if (existingFile != null)
            {
                usedSpace -= existingFile.GetSize();
            }

            return ((Capacity - usedSpace) >= newFile.GetSize());
        }

        public override void LoadPrograms(IEnumerable<ProgramFile> programsToLoad)
        {
            foreach (ProgramFile p in programsToLoad)
            {
                Add(p);
            }
        }

        public override ConfigNode Save(string nodeName)
        {
            var node = new ConfigNode(nodeName);
            node.AddValue("capacity", Capacity);
            node.AddValue("volumeName", Name);

            foreach (ProgramFile file in Files.Values)
            {
                node.AddNode(file.ToConfigNode("file"));
            }
            
            return node;
        }
    }
}
