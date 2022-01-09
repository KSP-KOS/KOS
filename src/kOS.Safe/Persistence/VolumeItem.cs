using System;
using kOS.Safe.Persistence;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Persistence
{
    [kOS.Safe.Utilities.KOSNomenclature("VolumeItem")]
    public abstract class VolumeItem : Structure
    {
        public Volume Volume { get; set; }
        public VolumePath Path { get; set; }

        public string Name
        {
            get
            {
                return Path.Name;
            }
        }

        public string Extension
        {
            get
            {
                return Path.Extension;
            }
        }

        public VolumeItem(Volume volume, VolumePath path)
        {
            Volume = volume;
            Path = path;

            InitializeSuffixes();
        }

        public VolumeItem(Volume volume, VolumePath parentPath, String name)
        {
            Volume = volume;
            Path = VolumePath.FromString(name, parentPath);

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => Name));
            AddSuffix("SIZE", new Suffix<ScalarIntValue>(() => new ScalarIntValue(Size)));
            AddSuffix("EXTENSION", new Suffix<StringValue>(() => Extension));
            AddSuffix("ISFILE", new Suffix<BooleanValue>(() => this is VolumeFile));
        }

        /*
        public override string ToString()
        {
            return string.IsNullOrEmpty(Path.Name) ? "Root directory" : Path.Name;
        }
        */

        public abstract int Size { get; }
    }
}

