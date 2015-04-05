using System;

namespace kOS.Safe.Compilation.KS
{
    public class Context
    {
        public UserFunctionCollection UserFunctions { get; private set; }
        public TriggerCollection Triggers { get; private set; }
        public SubprogramCollection Subprograms { get; private set; }
        public int NumCompilesSoFar {get; set;}
        public int LabelIndex { get; set; }
        public string LastSourceName { get; set; }
        
        // This has to live inside context because of the fact that more than one program
        // can be compiled into the same memory space.  If it was reset to zero by the
        // Compile action, then when one program ran another program, it would give 
        // ScopeId clashes between them as they'd both live in memory together and one
        // might call the functions of the other.
        public Int16 MaxScopeIdSoFar { get; set; }

        public Context()
        {
            UserFunctions = new UserFunctionCollection();
            Triggers = new TriggerCollection();
            Subprograms = new SubprogramCollection();
            LabelIndex = 0;
            LastSourceName = "";
            MaxScopeIdSoFar = 0;
            NumCompilesSoFar = 0;
        }
        
    }
}
