namespace kOS.Compilation
{
    public class CompilerOptions
    {
        public bool LoadProgramsInSameAddressSpace { get; set; }

        public CompilerOptions()
        {
            LoadDefaults();
        }

        private void LoadDefaults()
        {
            LoadProgramsInSameAddressSpace = false;
        }
    }
}
