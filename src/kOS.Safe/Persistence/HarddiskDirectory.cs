using System;
using System.Collections.Generic;
using kOS.Safe.Persistence;
using System.Linq;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation;
using System.Collections;

namespace kOS.Safe.Persistence
{
    [kOS.Safe.Utilities.KOSNomenclature("VolumeFile", KOSToCSharp = false)]
    public class HarddiskDirectory : VolumeDirectory, IEnumerable<VolumeItem>
    {
        private Dictionary<string, Structure> items;

        public HarddiskDirectory(Harddisk harddisk, VolumePath path) : base(harddisk, path)
        {
            items = new Dictionary<string, Structure>(StringComparer.InvariantCultureIgnoreCase);
        }

        public void Clear()
        {
            items.Clear();
        }

        public VolumeItem Open(string name, bool ksmDefault = false)
        {
            return Search(name, ksmDefault);
        }

        public FileContent GetFileContent(string name)
        {
            if (!items.ContainsKey(name) || !(items[name] is FileContent))
            {
                throw new KOSFileException("File does not exist: " + name);
            }

            return items[name] as FileContent;
        }

        public HarddiskFile CreateFile(string name)
        {
            return CreateFile(name, new FileContent());
        }

        public HarddiskFile CreateFile(string name, FileContent fileContent)
        {
            if (items.ContainsKey(name)){
                throw new KOSPersistenceException("Already exists: " + name);
            }

            items[name] = new FileContent(fileContent.Bytes.Clone() as byte[]);

            return new HarddiskFile(this, name);
        }

        public HarddiskDirectory CreateDirectory(string name)
        {
            return CreateDirectory(name, new HarddiskDirectory(Volume as Harddisk, VolumePath.FromString(name, Path)));
        }

        public HarddiskDirectory CreateDirectory(string name, HarddiskDirectory directory)
        {
            try
            {
                items.Add(name, directory);
            }
            catch (ArgumentException)
            {
                throw new KOSPersistenceException("Already exists: " + name);
            }

            return directory;
        }

        public VolumeFile Save(string name, FileContent fileContent)
        {
            if (!items.ContainsKey(name))
            {
                return CreateFile(name, fileContent);
            }
            else if (items[name] is VolumeDirectory)
            {
                throw new KOSPersistenceException("Can't save file over a directory: " + name);
            }
            else
            {
                items[name] = new FileContent(fileContent.Bytes.Clone() as byte[]);

                return new HarddiskFile(this, name);
            }
        }

        public bool Exists(string name, bool ksmDefault)
        {
            return Search(name) != null;
        }

        public bool Delete(string name, bool ksmDefault)
        {
            var toDelete = Search(name);

            if (toDelete == null)
            {
                return false;
            }

            return items.Remove(toDelete.Name);
        }

        public new IEnumerator<VolumeItem> GetEnumerator()
        {
            return List().Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public HarddiskDirectory GetSubdirectory(VolumePath path, bool create = false)
        {
            if (Path.Equals(path))
            {
                return this;
            }

            if (!Path.IsParent(path))
            {
                throw new KOSException("This directory does not contain path: " + path.ToString());
            }

            string subdirectory = path.Segments[Path.Segments.Count];

            if (!items.ContainsKey(subdirectory))
            {
                if (create)
                {
                    CreateDirectory(subdirectory);
                }
                else
                {
                    return null;
                }
            }

            HarddiskDirectory directory = items[subdirectory] as HarddiskDirectory;

            if (directory == null)
            {
                throw new KOSPersistenceException("Directory does not exist: " + path.ToString());
            }

            return directory.GetSubdirectory(path, create);
        }

        public override IDictionary<string, VolumeItem> List()
        {
            var result = new Dictionary<string, VolumeItem>();

            foreach (var pair in items)
            {
                if (pair.Value is HarddiskDirectory)
                {
                    result.Add(pair.Key, pair.Value as HarddiskDirectory);
                }
                else
                {
                    result.Add(pair.Key, new HarddiskFile(this, pair.Key));
                }
            }

            return result;
        }

        public override int Size {
            get
            {
                return List().Aggregate(0, (acc, x) => acc + x.Value.Size);
            }
        }


        private VolumeItem Search(string name, bool ksmDefault = false)
        {
            object item = items.ContainsKey(name) ? items[name] : null;
            if (item is FileContent)
            {
                return new HarddiskFile(this, name);
            }
            else if (item is HarddiskDirectory)
            {
                return item as HarddiskDirectory;
            }
            else
            {
                var kerboscriptFilename = PersistenceUtilities.CookedFilename(name, Volume.KERBOSCRIPT_EXTENSION, true);
                var kosMlFilename = PersistenceUtilities.CookedFilename(name, Volume.KOS_MACHINELANGUAGE_EXTENSION, true);
                bool kerboscriptFileExists = items.ContainsKey(kerboscriptFilename) && items[kerboscriptFilename] is FileContent;
                bool kosMlFileExists = items.ContainsKey(kosMlFilename) && items[kosMlFilename] is FileContent;
                if (kerboscriptFileExists && kosMlFileExists)
                {
                    return ksmDefault ? new HarddiskFile(this, kosMlFilename) : new HarddiskFile(this, kerboscriptFilename);
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

