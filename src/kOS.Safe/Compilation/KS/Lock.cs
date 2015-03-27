using System.Collections.Generic;
using System.Linq;

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
        
        /// <summary>
        /// A the thing this was defined in.  (it will be part of the parse tree of the compiler).
        /// null = global
        /// </summary>
        public ParseNode ScopeNode {get; set;}
        
        public bool IsFunction {get; set; }

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
            return (IsFunction || codePart.InitializationCode.Count > 0);
        }
        
        /// <summary>
        /// Get the label of the function body entry point,
        /// in other words the label of the very first Opcode
        /// instruction at the start of the function body.
        /// </summary>
        /// <returns>string label, i.e. K00001 </returns>
        public string GetFuncLabel()
        {
            if (functions.Count <= 0)
                return "undefined";
            else if (! IsFunction)
                return "not-a-function";
            else
                return functions.Values.FirstOrDefault().Code[0].Label;
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