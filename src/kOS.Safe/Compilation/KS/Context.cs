namespace kOS.Safe.Compilation.KS
{
    public class Context
    {
        public LockCollection Locks;
        public TriggerCollection Triggers;
        public SubprogramCollection Subprograms;
        public int LabelIndex;
        public string LastSourceName;

        public Context()
        {
            Locks = new LockCollection();
            Triggers = new TriggerCollection();
            Subprograms = new SubprogramCollection();
            LabelIndex = 0;
            LastSourceName = "";
        }
        
    }
}
