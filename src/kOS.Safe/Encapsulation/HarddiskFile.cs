using System;
using kOS.Safe.Persistence;

namespace kOS.Safe.Encapsulation
{
    public class HarddiskFile : VolumeFile
    {
        private Harddisk harddisk;

        public override int Size { get { return ReadAll().Size; } }

        public HarddiskFile(Harddisk harddisk, string name) : base(name)
        {
            this.harddisk = harddisk;
        }

        private FileContent GetFileContent()
        {
            return harddisk.GetFileContent(Name);
        }

        public override FileContent ReadAll()
        {
            return new FileContent((byte[])GetFileContent().Bytes.Clone());
        }

        public override bool Write(byte[] content)
        {
            if (harddisk.FreeSpace > content.Length)
            {
                GetFileContent().Write(content);
                return true;
            }

            return false;
        }

        public override bool WriteLn(string content)
        {
            return Write(content + "\n");
        }

        public override void Clear()
        {
            GetFileContent().Clear();
        }
    }
}

