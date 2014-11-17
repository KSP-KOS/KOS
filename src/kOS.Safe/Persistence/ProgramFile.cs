using System;
using System.Linq;

namespace kOS.Safe.Persistence
{
    public class ProgramFile
    {
        public string Filename
        {
            get { return filename; }
            set
            {
                filename = value;

                var fileParts = filename.Split('.');
                Extension = fileParts.Count() > 1 ? fileParts.Last() : string.Empty;
            }
        }

        public FileCategory Category { get; private set; }
        public string Extension { get; private set; }

        public string StringContent
        {
            get
            {
                if (Category != FileCategory.ASCII && Category != FileCategory.KERBOSCRIPT && Category != FileCategory.TOOSHORT)
                    throw new KOSFileException(string.Format("File {0} is {1}, not ASCII.  Should use BinaryContent instead.", Filename, Category));
                return stringContent;
            }
            set
            {
                Category = FileCategory.ASCII;
                stringContent = value;
            }
        }
        private string stringContent;

        public byte[] BinaryContent
        {
            get
            {
                if (Category == FileCategory.ASCII || Category == FileCategory.KERBOSCRIPT && Category != FileCategory.TOOSHORT)
                    throw new KOSFileException(string.Format("File {0} is {1}, not Binary. Should use StringContent instead.", Filename, Category));
                return binaryContent;
            }
            set
            {
                Category = FileCategory.KSM;
                binaryContent = value;
            }
        }
        private byte[] binaryContent;
        private string filename;

        public ProgramFile(ProgramFile copy)
        {
            Filename = copy.Filename;
            Category = copy.Category;
            if (Category == FileCategory.KSM)
                BinaryContent = copy.BinaryContent;
            else
                StringContent = copy.StringContent;
        }

        public ProgramFile(string filename)
        {
            Filename = filename;
            Category = FileCategory.TOOSHORT;
            stringContent = string.Empty;
            binaryContent = new byte[0];
        }

        public ProgramFile Copy()
        {
            return new ProgramFile(this);
        }

        public int GetSize()
        {
            switch (Category)
            {
                case FileCategory.TOOSHORT:
                    return 0;
                case FileCategory.KSM:
                    return BinaryContent.Length;
                case FileCategory.OTHER:
                case FileCategory.ASCII:
                case FileCategory.KERBOSCRIPT:
                    return StringContent.Length;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }
}
