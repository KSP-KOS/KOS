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

        public override void Clear()
        {
            RootHarddiskDirectory.Clear();
        }

        private HarddiskDirectory ParentDirectoryForPath(VolumePath path, bool create = false)
        {
            HarddiskDirectory directory = RootHarddiskDirectory;
            if (path.Depth > 0)
            {
                return RootHarddiskDirectory.GetSubdirectory(path.GetParent(), create);
            } else
            {
                throw new Exception("This directory does not have a parent");
            }
        }

        public override VolumeItem Open(VolumePath path, bool ksmDefault = false)
        {
            if (path.Depth == 0) {
                return Root;
            }

            HarddiskDirectory directory = ParentDirectoryForPath(path);

            return directory == null ? null : directory.Open(path.Name, ksmDefault);
        }

        public override VolumeDirectory CreateDirectory(VolumePath path)
        {
            if (path.Depth == 0)
            {
                throw new KOSPersistenceException("Can't create a directory over root directory");
            }

            HarddiskDirectory directory = ParentDirectoryForPath(path, true);

            return directory.CreateDirectory(path.Name);
        }

        public override VolumeFile CreateFile(VolumePath path)
        {
            if (path.Depth == 0)
            {
                throw new KOSPersistenceException("Can't create a file over root directory");
            }

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

        public override VolumeFile SaveFile(VolumePath path, FileContent content)
        {
            try {
                if (!IsRoomFor(path, content))
                {
                    return null;
                }
            } catch (KOSPersistenceException)
            {
                throw new KOSPersistenceException("Can't save file over a directory: " + path);
            }

            HarddiskDirectory directory = ParentDirectoryForPath(path, true);

            return directory.Save(path.Name, content) as VolumeFile;
        }
    }
}
