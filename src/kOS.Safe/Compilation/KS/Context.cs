namespace kOS.Safe.Compilation.KS
{
    public class Context
    {
        public LockCollection Locks { get; private set; }
        public TriggerCollection Triggers { get; private set; }
        public SubprogramCollection Subprograms { get; private set; }
        public int LabelIndex { get; set; }
        public string LastSourceName { get; set; }

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
