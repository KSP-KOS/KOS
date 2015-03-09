namespace kOS.Safe.Compilation
{
    public class CompilerOptions
    {
        public bool LoadProgramsInSameAddressSpace { get; set; }
        
        /// <summary>
        /// True if the compile should act as if there was an outer wrapping local
        /// block scope around the whole thing being compiled.  In other words
        /// DECLARE statements inside the thing being compiled are local to just
        /// the thing being compiled and cannot be seen outside the thing being
        /// compiled.  If false, then it assumes the outermost scope of the
        /// compilation unit is identical to the entire computer's global scope.
        /// (such that variables declared there are available everywhere).
        /// </summary>
        public bool WrapImplicitBlockScope {get; set;}

        public CompilerOptions()
        {
            LoadDefaults();
        }

        private void LoadDefaults()
        {
            LoadProgramsInSameAddressSpace = false;
            WrapImplicitBlockScope = false;
            
        }
    }
}
