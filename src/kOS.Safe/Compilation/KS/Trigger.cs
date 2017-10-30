using System.Collections.Generic;

namespace kOS.Safe.Compilation.KS
{
    public class Trigger
    {
        private readonly CodePart codePart;
        private string Identifier { get; set; }

        public string OldValueIdentifier { get; private set; }

        public List<Opcode> Code
        {
            get { return codePart.FunctionsCode; }
        }

        public Trigger()
        {
            codePart = new CodePart();
        }

        public Trigger(string triggerIdentifier)
            : this()
        {
            Identifier = triggerIdentifier;
            OldValueIdentifier = "$old-"+Identifier;
        }

        public bool IsInitialized()
        {
            return (codePart.FunctionsCode.Count > 0);
        }

        public string GetFunctionLabel()
        {
            return Code.Count > 0 ? Code[0].Label : string.Empty;
        }

        public CodePart GetCodePart()
        {
            return codePart;
        }
    }
}