namespace kOS.Compilation.KS
{
    class Context
    {
        public LockCollection Locks;
        public TriggerCollection Triggers;
        public SubprogramCollection Subprograms;
        public int LabelIndex;
        public int InstructionId;

        public Context()
        {
            Locks = new LockCollection();
            Triggers = new TriggerCollection();
            Subprograms = new SubprogramCollection();
            LabelIndex = 0;
            InstructionId = 0;
        }
    }
}
