using System.Collections.Generic;

namespace kOS.Safe.Compilation.KS
{
    public class Subprogram
    {
        private readonly CodePart codePart;
        public string SubprogramName { get; private set; }
        public string PointerIdentifier { get; private set; }
        public string FunctionLabel { get; set; }

        public List<Opcode> FunctionCode
        {
            get { return codePart.FunctionsCode; }
        }

        public List<Opcode> InitializationCode
        {
            get { return codePart.InitializationCode; }
        }

        public Subprogram(string subprogramName)
        {
            codePart = new CodePart();
            SubprogramName = subprogramName;
            PointerIdentifier = string.Format("$program-{0}*", subprogramName);
        }

        public CodePart GetCodePart()
        {
            return codePart;
        }
    }
}
