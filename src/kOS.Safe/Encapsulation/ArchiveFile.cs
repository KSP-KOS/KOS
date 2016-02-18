using kOS.Safe.Persistence;
using System;
using System.IO;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("VolumeFile", KOSToCSharp = false)]
    public class ArchiveFile : VolumeFile
    {
        private readonly FileInfo fileInfo;
        public override int Size { get { fileInfo.Refresh(); return (int)fileInfo.Length; } }

        public ArchiveFile(FileInfo fileInfo) : base(fileInfo.Name)
        {
            this.fileInfo = fileInfo;
        }

        public override FileContent ReadAll()
        {
            byte[] bytes = File.ReadAllBytes(fileInfo.FullName);

            bytes = Archive.ConvertFromWindowsNewlines(bytes);

            return new FileContent(bytes);
        }

        public override bool Write(byte[] content)
        {
            if (!fileInfo.Exists)
            {
                throw new KOSFileException("File does not exist: " + fileInfo.Name);
            }

            byte[] bytes = Archive.ConvertToWindowsNewlines(content);
            using (FileStream stream = fileInfo.Open(FileMode.Append, FileAccess.Write))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }

            return true;
        }

        public override bool WriteLn(string content)
        {
            return Write(content + Environment.NewLine);
        }

        public override void Clear()
        {
            File.WriteAllText(fileInfo.FullName, string.Empty);
        }
    }
}