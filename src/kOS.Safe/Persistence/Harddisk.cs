using kOS.Safe.Encapsulation;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Persistence
{
    [kOS.Safe.Utilities.KOSNomenclature("LocalVolume")]
    public sealed class Harddisk : Volume
    {
        public HarddiskDirectory RootHarddiskDirectory { get; set; }

        public override VolumeDirectory Root {
            get
            {
                return RootHarddiskDirectory;
            }
        }

        public Harddisk(int size)
        {
            Capacity = size;
            RootHarddiskDirectory = new HarddiskDirectory(this, VolumePath.EMPTY);
        }

        private HarddiskDirectory ParentDirectoryForPath(VolumePath path, bool create = false)
        {
            HarddiskDirectory directory = RootHarddiskDirectory;
            if (path.Depth > 1)
            {
                directory = RootHarddiskDirectory.GetSubdirectory(path.GetParent(), create);
            }

            return directory;
        }

        public override VolumeItem Open(VolumePath path, bool ksmDefault = false)
        {
            if (path.Depth == 0) {
                return Root;
            }

            HarddiskDirectory directory = ParentDirectoryForPath(path);

            VolumeItem result = directory.Open(path.Name, ksmDefault);

            return result;
        }

        public override VolumeDirectory CreateDirectory(VolumePath path)
        {
            HarddiskDirectory directory = ParentDirectoryForPath(path, true);

            return directory.CreateDirectory(path.Name);
        }

        public override VolumeFile CreateFile(VolumePath path)
        {
            HarddiskDirectory directory = ParentDirectoryForPath(path, true);

            return directory.CreateFile(path.Name);
        }

        public override bool Exists(VolumePath path, bool ksmDefault = false)
        {
            HarddiskDirectory directory = ParentDirectoryForPath(path);

            return directory.Exists(path.Name, ksmDefault);
        }

        public override bool Delete(VolumePath path, bool ksmDefault = false)
        {
            HarddiskDirectory directory = ParentDirectoryForPath(path);

            return directory.Delete(path.Name, ksmDefault);
        }

        public override VolumeFile Save(VolumePath path, FileContent content)
        {
            if (!IsRoomFor(path, content))
            {
                return null;
            }

            HarddiskDirectory directory = ParentDirectoryForPath(path);
            directory.CreateFile(path.Name, content);

            return Open(path) as VolumeFile;
        }
    }
}
