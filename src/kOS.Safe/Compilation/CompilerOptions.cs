using kOS.Safe.Function;
namespace kOS.Safe.Compilation
{
    public class CompilerOptions
    {
        public bool LoadProgramsInSameAddressSpace { get; set; }
        public IFunctionManager FuncManager { get; set; }
        public CompilerOptions()
        {
            LoadDefaults();
        }

        private void LoadDefaults()
        {
            LoadProgramsInSameAddressSpace = false;
            FuncManager = null;
        }
        
        public bool BuiltInExists(string identifier)
        {
            return (FuncManager == null ) ? false : FuncManager.Exists(identifier);
        }
    }
}
