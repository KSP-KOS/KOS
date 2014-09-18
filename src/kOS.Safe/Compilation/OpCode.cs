using kOS.Safe.Execution;

namespace kOS.Safe.Compilation
{
    public class Opcode
    {
        private static int lastId;
        private readonly int id = ++lastId;
        
        public virtual string Name { get { return string.Empty; } }
        public int Id { get { return id; } }
        public int DeltaInstructionPointer = 1;
        public string Label = string.Empty;
        public string DestinationLabel;
        public string SourceName;
        public int SourceLine = 0; // line number in the source code that this was compiled from.
        public int SourceColumn = 0; // column number of the token nearest the cause of this Opcode.
        
        public virtual void Execute(ICpu cpu)
        {
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
