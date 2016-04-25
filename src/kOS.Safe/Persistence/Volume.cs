using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Persistence
{
    [kOS.Safe.Utilities.KOSNomenclature("Volume")]
    public abstract class Volume : Structure
    {
        public const string TEXT_EXTENSION = "txt";
        public const string KERBOSCRIPT_EXTENSION = "ks";
        public const string KOS_MACHINELANGUAGE_EXTENSION = "ksm";
        protected const long BASE_CAPACITY = 10000;
        protected const float BASE_POWER = 0.04f;
        public const int INFINITE_CAPACITY = -1;

        public abstract Dictionary<string, VolumeFile> FileList { get; }
        public string Name { get; set; }
        public long Capacity { get; protected set; }
        public abstract long Size { get; }
        public long FreeSpace {
            get {
                return Capacity == INFINITE_CAPACITY ? INFINITE_CAPACITY : Capacity - Size;
            }
        }
        public bool Renameable { get; protected set; }

        protected Volume()
        {
            Renameable = true;
            Capacity = -1;
            Name = "";
            InitializeVolumeSuffixes();
        }

        /// <summary>
        /// Get a file given its name
        /// </summary>
        /// <param name="name">filename to get.  if it has no filename extension, one will be guessed at, ".ks" usually.</param>
        /// <param name="ksmDefault">in the scenario where there is no filename extension, do we prefer the .ksm over the .ks?  The default is to prefer .ks</param>
        /// <returns>the file</returns>
        public abstract VolumeFile Open(string name, bool ksmDefault = false);

        public abstract VolumeFile Create(string name);

        public abstract bool Exists(string name);

        public VolumeFile OpenOrCreate(string name, bool ksmDefault = false)
        {
            var volumeFile = Open(name, ksmDefault);

            if (volumeFile != null)
            {
                return volumeFile;
            }

            return Create(name);
        }

        public abstract bool Delete(string name);

        public abstract bool RenameFile(string name, string newName);

        //public abstract bool AppendToFile(string name, string textToAppend);

        //public abstract bool AppendToFile(string name, byte[] bytesToAppend);

        public VolumeFile Save(VolumeFile volumeFile)
        {
            return Save(volumeFile.Name, volumeFile.ReadAll());
        }

        public abstract VolumeFile Save(string name, FileContent content);

        public bool IsRoomFor(string name, FileContent fileContent)
        {
            VolumeFile existingFile = Open(name);

            int usedByThisFile = 0;

            if (existingFile != null)
            {
                usedByThisFile = existingFile.ReadAll().Size;
            }

            return INFINITE_CAPACITY == FreeSpace || FreeSpace + usedByThisFile >= fileContent.Size;
        }

        public virtual float RequiredPower()
        {
            var multiplier = (float)Capacity / BASE_CAPACITY;
            var powerRequired = BASE_POWER * multiplier;

            return powerRequired;
        }

        public override string ToString()
        {
            return "Volume( " + Name + ", " + Capacity + ")";
        }

        private void InitializeVolumeSuffixes()
        {
            AddSuffix("FREESPACE" , new Suffix<ScalarValue>(() => FreeSpace));
            AddSuffix("CAPACITY" , new Suffix<ScalarValue>(() => Capacity));
            AddSuffix("NAME" , new Suffix<StringValue>(() => Name));
            AddSuffix("RENAMEABLE" , new Suffix<BooleanValue>(() => Renameable));
            AddSuffix("POWERREQUIREMENT" , new Suffix<ScalarValue>(() => RequiredPower()));

            AddSuffix("EXISTS" , new OneArgsSuffix<BooleanValue, StringValue>(name => Exists(name)));
            AddSuffix("FILES" , new Suffix<Lexicon>(BuildFileLexicon));
            AddSuffix("CREATE" , new OneArgsSuffix<VolumeFile, StringValue>(name => Create(name)));
            AddSuffix("OPEN" , new OneArgsSuffix<VolumeFile, StringValue>(name => Open(name)));
            AddSuffix("DELETE" , new OneArgsSuffix<BooleanValue, StringValue>(name => Delete(name)));
        }

        private Lexicon BuildFileLexicon()
        {
            return new Lexicon(FileList.ToDictionary(item => FromPrimitiveWithAssert(item.Key), item => (Structure) item.Value));
        }


        private int FileInfoComparer(VolumeFile a, VolumeFile b)
        {
            return string.CompareOrdinal(a.Name, b.Name);
        }
    }    
}
