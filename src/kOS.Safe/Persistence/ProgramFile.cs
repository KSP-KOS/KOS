using System;
using kOS.Safe.Utilities;

namespace kOS.Safe.Persistence
{
    public class ProgramFile
    {
        private byte[] binaryContent;
        private string stringContent;

        public string Filename {get;set;}
        public DateTime ModifiedDate {get;set;}
        public DateTime CreatedDate {get;set;}
        public FileCategory Category {get; private set;}

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
                Debug.Logger.Log("ProgramFile: File: " + Filename + " Set StringContent: " + value.Length);
                Category = FileCategory.ASCII;
                stringContent = value;
            }
        }

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
                Debug.Logger.Log("ProgramFile: File: " + Filename + " Set BinaryContent: " + value.Length);
                Category = FileCategory.KSM;
                binaryContent = value;
            }
        }

        public ProgramFile(ProgramFile copy)
        {
            Debug.Logger.Log("ProgramFile: Copy Construct: " + copy.Filename);
            Filename = copy.Filename;
            Category = copy.Category;
            ModifiedDate = copy.ModifiedDate;
            CreatedDate = copy.CreatedDate;
            if (Category == FileCategory.KSM)
                BinaryContent = copy.BinaryContent;
            else
                StringContent = copy.StringContent;
        }

        public ProgramFile(string filename)
        {
            Debug.Logger.Log("ProgramFile: Construct: " + filename);
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
            switch (Category)
            {
                case FileCategory.ASCII:
                    return StringContent.Length;
                case FileCategory.KSM:
                    return BinaryContent.Length;
                case FileCategory.UNKNOWN:
                case FileCategory.KERBOSCRIPT:
                    throw new NotImplementedException("Cant Get Size for type: "+ Category);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
