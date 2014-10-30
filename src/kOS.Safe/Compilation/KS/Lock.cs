using System.Collections.Generic;

namespace kOS.Safe.Compilation.KS
{
    public class Lock
    {
        private static readonly List<string> systemLocks = new List<string> { "throttle", "steering", "wheelthrottle", "wheelsteering" };
        
        private readonly CodePart codePart;
        private readonly Dictionary<int, LockFunction> functions;
        private readonly List<LockFunction> newFunctions;

        public string Identifier { get; private set; }
        public string PointerIdentifier{ get; private set; }
        public string DefaultLabel{ get; private set; }

        public List<Opcode> InitializationCode
        {
            get { return codePart.InitializationCode; }
        }

        public List<Opcode> MainCode
        {
            get { return codePart.MainCode; }
        }

        
        public Lock()
        {
            codePart = new CodePart();
            functions = new Dictionary<int, LockFunction>();
            newFunctions = new List<LockFunction>();
        }

        public Lock(string lockIdentifier)
            : this()
        {
            Identifier = lockIdentifier;
            PointerIdentifier = "$" + Identifier + "*";
            DefaultLabel = Identifier + "-default";
        }


        public bool IsInitialized()
        {
            return (codePart.InitializationCode.Count > 0);
        }

        public List<Opcode> GetLockFunction(int expressionHash)
        {
            if (functions.ContainsKey(expressionHash))
            {
                return functions[expressionHash].Code;
            }
            var newLockFunction = new LockFunction();
            functions.Add(expressionHash, newLockFunction);
            newFunctions.Add(newLockFunction);
            return newLockFunction.Code;
        }

        public CodePart GetCodePart()
        {
            var mergedPart = new CodePart
            {
                InitializationCode = codePart.InitializationCode,
                MainCode = codePart.MainCode
            };

            foreach (LockFunction function in functions.Values)
            {
                mergedPart.FunctionsCode.AddRange(function.Code);
            }

            return mergedPart;
        }

        public bool HasNewFunctions()
        {
            return (newFunctions.Count > 0);
        }

        public void ClearNewFunctions()
        {
            newFunctions.Clear();
        }

        public CodePart GetNewFunctionsCodePart()
        {
            var newFunctionsPart = new CodePart();

            foreach (LockFunction function in newFunctions)
            {
                newFunctionsPart.FunctionsCode.AddRange(function.Code);
            }

            ClearNewFunctions();
            return newFunctionsPart;
        }

        public bool IsSystemLock()
        {
            return systemLocks.Contains(Identifier.ToLower());
        }
    }
}