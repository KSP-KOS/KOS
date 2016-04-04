namespace kOS.Safe.Persistence
{
    [kOS.Safe.Utilities.KOSNomenclature("VolumeFile", KOSToCSharp = false)]
    public class HarddiskFile : VolumeFile
    {
        private readonly HarddiskDirectory hardiskDirectory;

        public override int Size { get { return ReadAll().Size; } }

        public HarddiskFile(HarddiskDirectory harddiskDirectory, string name) : base(harddiskDirectory.Volume as Harddisk,
            VolumePath.FromString(name, harddiskDirectory.Path))
        {
            this.hardiskDirectory = harddiskDirectory;
        }

        private FileContent GetFileContent()
        {
            return hardiskDirectory.GetFileContent(Name);
        }

        public override FileContent ReadAll()
        {
            return new FileContent((byte[])GetFileContent().Bytes.Clone());
        }

        public override bool Write(byte[] content)
        {
            if ((hardiskDirectory.Volume as Harddisk).FreeSpace <= content.Length) return false;

            GetFileContent().Write(content);
            return true;
        }

        public override void Clear()
        {
            GetFileContent().Clear();
        }
    }
}