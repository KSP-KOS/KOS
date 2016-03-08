using kOS.Safe.Encapsulation;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Persistence
{
    [kOS.Safe.Utilities.KOSNomenclature("LocalVolume")]
    public sealed class Harddisk : Volume
    {
        private readonly Dictionary<string, FileContent> files;

        public override Dictionary<string, VolumeFile> FileList
        {
            get
            {
                return files.ToDictionary(arg => arg.Key, arg => (VolumeFile)new HarddiskFile(this, arg.Key));
            }
        }

        public override long Size
        {
            get
            {
                return files.Values.Sum(x => x.Size);
            }
        }

        public Harddisk(int size)
        {
            Capacity = size;
            files = new Dictionary<string, FileContent>(StringComparer.OrdinalIgnoreCase);
        }

        public FileContent GetFileContent(string name)
        {
            if (!files.ContainsKey(name))
            {
                throw new KOSFileException("File does not exist: " + name);
            }

            return files[name];
        }

        public override VolumeFile Open(string name, bool ksmDefault = false)
        {
            return FileSearch(name, ksmDefault);
        }

        public override bool Delete(string name)
        {
            var fullPath = FileSearch(name);
            if (fullPath == null)
            {
                return false;
            }
            return files.Remove(fullPath.Name);
        }

        public override bool RenameFile(string name, string newName)
        {
            VolumeFile file = Open(name);
            if (file != null)
            {
                // Add the original file content under the new name
                files.Add(newName, files[file.Name]);
                // Then remove the old file content under the old name
                files.Remove(file.Name);
                return true;
            }
            return false;
        }

        public override VolumeFile Create(string name)
        {
            SafeHouse.Logger.Log("Creating file on harddisk " + name);

            if (files.ContainsKey(name))
            {
                throw new KOSFileException("File already exists: " + name);
            }

            files[name] = new FileContent();

            SafeHouse.Logger.Log("Created file on harddisk " + name);

            return new HarddiskFile(this, name);
        }

        public override VolumeFile Save(string name, FileContent content)
        {
            if (!IsRoomFor(name, content))
            {
                return null;
            }

            files[name] = content;

            return new HarddiskFile(this, name);
        }

        public override bool Exists(string name)
        {
            return FileSearch(name) != null;
        }

        private VolumeFile FileSearch(string name, bool ksmDefault = false)
        {
            if (files.ContainsKey(name))
            {
                return new HarddiskFile(this, name);
            }
            else
            {
                var kerboscriptFilename = PersistenceUtilities.CookedFilename(name, KERBOSCRIPT_EXTENSION, true);
                var kosMlFilename = PersistenceUtilities.CookedFilename(name, KOS_MACHINELANGUAGE_EXTENSION, true);
                bool kerboscriptFileExists = files.ContainsKey(kerboscriptFilename);
                bool kosMlFileExists = files.ContainsKey(kosMlFilename);
                if (kerboscriptFileExists && kosMlFileExists)
                {
                    if (ksmDefault)
                    {
                        return new HarddiskFile(this, kosMlFilename);
                    }
                    else
                    {
                        return new HarddiskFile(this, kerboscriptFilename);
                    }
                }
                if (kerboscriptFileExists)
                {
                    return new HarddiskFile(this, kerboscriptFilename);
                }
                if (kosMlFileExists)
                {
                    return new HarddiskFile(this, kosMlFilename);
                }
            }
            return null;
        }
    }
}
