using System;

namespace kOS.Safe.Persistence
{
    public class ProgramFile
    {
        public string Filename {get;set;}
        public DateTime ModifiedDate {get;set;}
        public DateTime CreatedDate {get;set;}
        public FileCategory Category {get;private set;}
        public string StringContent
        {
            get 
            {
                if (Category != FileCategory.ASCII && Category != FileCategory.KERBOSCRIPT)
                    throw new KOSFileException("File " + Filename + " is not ASCII.  Should use BinaryContent instead.");
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
                if (Category == FileCategory.ASCII || Category == FileCategory.KERBOSCRIPT)
                    throw new KOSFileException("File " + Filename + " is not Binary. Should use StringContent instead.");
                return binaryContent;
            }
            set 
            {
                Category = FileCategory.KEXE;
                binaryContent = value;
            }
        }
        private byte[] binaryContent;

        public ProgramFile(ProgramFile copy)
        {
            Filename = copy.Filename;
            Category = copy.Category;
            ModifiedDate = copy.ModifiedDate;
            CreatedDate = copy.CreatedDate;
            if (Category == FileCategory.KEXE)
                BinaryContent = copy.BinaryContent;
            else
                StringContent = copy.StringContent;
        }

        public ProgramFile(string filename)
        {
            Filename = filename;
            Category = FileCategory.UNKNOWN;
            stringContent = string.Empty;
            binaryContent = new byte[0];
        }

        public ProgramFile Copy()
        {
            return new ProgramFile(this);
        }

        public int GetSize()
        {
            return Category == FileCategory.KEXE ? BinaryContent.Length : StringContent.Length;
        }
    }
}
