using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Persistance
{
    public class Harddisk : Volume
    {
        public Harddisk(int size)
        {
            Capacity = size;
        }

        public Harddisk(ConfigNode node)
        {
            Capacity = 10000;

            foreach (string s in node.GetValues("capacity"))
            {
                Capacity = Int32.Parse(s);
            }

            foreach (string s in node.GetValues("volumeName"))
            {
                Name = s;
            }

            foreach (var fileNode in node.GetNodes("file"))
            {
                Files.Add(new File(fileNode));
            }
        }

        public override bool SaveFile(File file)
        {
            return IsRoomFor(file) && base.SaveFile(file);
        }

        public override int GetFreeSpace()
        {
            var totalOccupied = Files.Sum(p => p.GetSize());

            return Math.Max(Capacity - totalOccupied, 0);
        }

        public override bool IsRoomFor(File newFile)
        {
            // Consider only existing files that don't share a name with the proposed new file
            // Because this could be an overwrite situation
            var totalOccupied = newFile.GetSize() + Files.
                                                        Where(existingFile => existingFile.Filename != newFile.Filename)
                                                         .
                                                        Sum(existingFile => existingFile.GetSize());

            return (Capacity - totalOccupied >= 0);
        }

        public override void LoadPrograms(IList<File> programsToLoad)
        {
            foreach (var p in programsToLoad)
            {
                Files.Add(p);
            }
        }

        public override ConfigNode Save(string nodeName)
        {
            var node = new ConfigNode(nodeName);
            node.AddValue("capacity", Capacity);
            node.AddValue("volumeName", Name);

            foreach (var file in Files)
            {
                node.AddNode(file.Save("file"));
            }

            return node;
        }
    }
}