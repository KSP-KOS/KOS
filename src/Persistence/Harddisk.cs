﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace kOS.Persistence
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
            
            if (node.HasValue("capacity")) Capacity = int.Parse(node.GetValue("capacity"));
            if (node.HasValue("volumeName")) Name = node.GetValue("volumeName");
            
            foreach (ConfigNode fileNode in node.GetNodes("file"))
            {
                Add(new ProgramFile(fileNode));
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

        public override void LoadPrograms(List<ProgramFile> programsToLoad)
        {
            foreach (ProgramFile p in programsToLoad)
            {
                Add(p);
            }
        }

        public override ConfigNode Save(string nodeName)
        {
            ConfigNode node = new ConfigNode(nodeName);
            node.AddValue("capacity", Capacity);
            node.AddValue("volumeName", Name);

            foreach (ProgramFile file in _files.Values)
            {
                node.AddNode(file.Save("file"));
            }
            
            return node;
        }
    }
}
