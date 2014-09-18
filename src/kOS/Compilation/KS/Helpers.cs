using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation;

namespace kOS.Compilation.KS
{
    #region Locks

    class LockCollection
    {
        private readonly Dictionary<string, Lock> locks = new Dictionary<string, Lock>();
        private readonly List<Lock> newLocks = new List<Lock>();

        public bool Contains(string lockIdentifier)
        {
            return locks.ContainsKey(lockIdentifier);
        }

        public Lock GetLock(string lockIdentifier)
        {
            if (locks.ContainsKey(lockIdentifier))
            {
                return locks[lockIdentifier];
            }
            var lockObject = new Lock(lockIdentifier);
            locks.Add(lockIdentifier, lockObject);
            newLocks.Add(lockObject);
            return lockObject;
        }

        public List<Lock> GetLockList()
        {
            return locks.Values.ToList();
        }

        public List<CodePart> GetParts(List<Lock> lockList)
        {
            return lockList.Select(lockObject => lockObject.GetCodePart()).ToList();
        }

        public List<CodePart> GetParts()
        {
            return GetParts(locks.Values.ToList());
        }

        public List<CodePart> GetNewParts()
        {
            // new locks
            List<CodePart> parts = GetParts(newLocks);

            // updated locks
            foreach (Lock lockObject in locks.Values)
            {
                // if the lock is new then clear the new functions list
                if (newLocks.Contains(lockObject))
                {
                    lockObject.ClearNewFunctions();
                }
                else if (lockObject.HasNewFunctions())
                {
                    // if the lock has new functions then create a new code part for them
                    parts.Add(lockObject.GetNewFunctionsCodePart());
                }
            }

            newLocks.Clear();

            return parts;
        }
    }

    class Lock
    {
        private static readonly List<string> systemLocks = new List<string> { "throttle", "steering", "wheelthrottle", "wheelsteering" };
        
        private readonly CodePart codePart;
        private readonly Dictionary<int, LockFunction> functions;
        private readonly List<LockFunction> newFunctions;

        public string Identifier;
        public string PointerIdentifier;
        public string DefaultLabel;

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

    class LockFunction
    {
        public List<Opcode> Code;

        public LockFunction()
        {
            Code = new List<Opcode>();
        }
    } 

    #endregion

    #region Triggers

    class TriggerCollection
    {
        private readonly Dictionary<string, Trigger> triggers = new Dictionary<string, Trigger>();
        private readonly List<Trigger> newTriggers = new List<Trigger>();

        public bool Contains(string triggerIdentifier)
        {
            return triggers.ContainsKey(triggerIdentifier);
        }

        public Trigger GetTrigger(string triggerIdentifier)
        {
            if (triggers.ContainsKey(triggerIdentifier))
            {
                return triggers[triggerIdentifier];
            }
            var triggerObject = new Trigger(triggerIdentifier);
            triggers.Add(triggerIdentifier, triggerObject);
            newTriggers.Add(triggerObject);
            return triggerObject;
        }

        public List<CodePart> GetParts(List<Trigger> triggerList)
        {
            return triggerList.Select(triggerObject => triggerObject.GetCodePart()).ToList();
        }

        public List<CodePart> GetParts()
        {
            return GetParts(triggers.Values.ToList());
        }

        public List<CodePart> GetNewParts()
        {
            List<CodePart> parts = GetParts(newTriggers);
            newTriggers.Clear();
            return parts;
        }
    }

    class Trigger
    {
        private readonly CodePart codePart;
        public string Identifier;
        public string VariableName;
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

    #endregion

    #region Subprograms

    class SubprogramCollection
    {
        private readonly Dictionary<string, Subprogram> subprograms = new Dictionary<string, Subprogram>();
        private readonly List<Subprogram> newSubprograms = new List<Subprogram>();

        public bool Contains(string subprogramName)
        {
            return subprograms.ContainsKey(subprogramName);
        }

        public Subprogram GetSubprogram(string subprogramName)
        {
            if (subprograms.ContainsKey(subprogramName))
            {
                return subprograms[subprogramName];
            }
            var subprogramObject = new Subprogram(subprogramName);
            subprograms.Add(subprogramName, subprogramObject);
            newSubprograms.Add(subprogramObject);
            return subprogramObject;
        }

        public List<CodePart> GetParts(List<Subprogram> subprogramList)
        {
            return subprogramList.Select(subprogramObject => subprogramObject.GetCodePart()).ToList();
        }

        public List<CodePart> GetParts()
        {
            return GetParts(subprograms.Values.ToList());
        }

        public List<CodePart> GetNewParts()
        {
            List<CodePart> parts = GetParts(newSubprograms);
            newSubprograms.Clear();
            return parts;
        }
    }

    class Subprogram
    {
        private readonly CodePart codePart;
        public string SubprogramName;
        public string PointerIdentifier;
        public string FunctionLabel;

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


    #endregion
}
