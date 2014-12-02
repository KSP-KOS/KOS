using System.Collections.Generic;

namespace kOS.Safe.Compilation.KS
{
    public class Trigger
    {
        private readonly CodePart codePart;
        private string Identifier { get; set; }

        public string VariableName { get; private set; }
        public string VariableNameOldValue;
                
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
        }

        public bool IsInitialized()
        {
            return (codePart.FunctionsCode.Count > 0);
        }

        public void SetTriggerVariable(string triggerVariable)
        {
            VariableName = "$" + triggerVariable;
            VariableNameOldValue = "$old-" + triggerVariable.ToLower();
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