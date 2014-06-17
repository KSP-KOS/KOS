namespace kOS.Compilation
{
    public class CompilerOptions
    {
        public bool LoadProgramsInSameAddressSpace;

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
