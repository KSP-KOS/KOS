namespace kOS.Safe.Persistence
{
    public class ProgramFile
    {
        public string Filename { get; set; }
        public FileCategory Category { get; private set; }
        public string StringContent
        {
            get
            {
                if (Category != FileCategory.ASCII && Category != FileCategory.KERBOSCRIPT && Category != FileCategory.TOOSHORT)
                    throw new KOSFileException("File " + Filename + " is " + Category.ToString() + ", not ASCII.  Should use BinaryContent instead.");
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
                    throw new KOSFileException("File " + Filename + " is " + Category.ToString() + ", not Binary. Should use StringContent instead.");
                return binaryContent;
            }
            set
            {
                Category = FileCategory.KSM;
                binaryContent = value;
            }
        }
        private byte[] binaryContent;

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
            return Category == FileCategory.KSM ? BinaryContent.Length : StringContent.Length;
        }
    }
}
