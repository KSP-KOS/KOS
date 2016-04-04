using System;
using kOS.Safe.Persistence;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Persistence
{
    public class ArchiveDirectory : VolumeDirectory
    {
        private Archive archive;
        private string archivePath;

        public ArchiveDirectory(Archive archive, VolumePath path) : base(archive, path)
        {
            this.archive = archive;
            this.archivePath = archive.GetArchivePath(path);
        }

        public override IDictionary<string, VolumeItem> List()
        {
            string[] files = Directory.GetFiles(archivePath);
            var filterHid = files.Where(f => (File.GetAttributes(f) & FileAttributes.Hidden) != 0);
            var filterSys = files.Where(f => (File.GetAttributes(f) & FileAttributes.System) != 0);
            var visFiles = files.Except(filterSys).Except(filterHid).ToArray();
            string[] directories = Directory.GetDirectories(archivePath);

            Array.Sort(directories);
            Array.Sort(visFiles);

            var result = new Dictionary<string, VolumeItem>();

            foreach (string directory in directories)
            {
                string directoryName = System.IO.Path.GetFileName(directory);
                result.Add(directoryName, new ArchiveDirectory(archive, VolumePath.FromString(directoryName, Path)));
            }

            foreach (string file in visFiles)
            {
                string fileName = System.IO.Path.GetFileName(file);
                result.Add(fileName, new ArchiveFile(archive, new FileInfo(file), VolumePath.FromString(fileName, Path)));
            }

            return result;
        }

        public override int Size {
            get {
                return List().Values.Aggregate(0, (acc, x) => acc + x.Size);
            }
        }
    }
}

