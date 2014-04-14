using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Compilation.KS
{
    #region Locks

    class LockCollection
    {
        private Dictionary<string, Lock> _locks = new Dictionary<string, Lock>();
        private List<Lock> _newLocks = new List<Lock>();

        public bool Contains(string lockIdentifier)
        {
            return _locks.ContainsKey(lockIdentifier);
        }

        public Lock GetLock(string lockIdentifier)
        {
            if (_locks.ContainsKey(lockIdentifier))
            {
                return _locks[lockIdentifier];
            }
            else
            {
                Lock lockObject = new Lock(lockIdentifier);
                _locks.Add(lockIdentifier, lockObject);
                _newLocks.Add(lockObject);
                return lockObject;
            }
        }

        public List<Lock> GetLockList()
        {
            return _locks.Values.ToList();
        }

        public List<CodePart> GetParts(List<Lock> locks)
        {
            List<CodePart> parts = new List<CodePart>();

            foreach (Lock lockObject in locks)
            {
                parts.Add(lockObject.GetCodePart());
            }

            return parts;
        }

        public List<CodePart> GetParts()
        {
            return GetParts(_locks.Values.ToList<Lock>());
        }

        public List<CodePart> GetNewParts()
        {
            // new locks
            List<CodePart> parts = GetParts(_newLocks);

            // updated locks
            foreach (Lock lockObject in _locks.Values)
            {
                // if the lock is new then clear the new functions list
                if (_newLocks.Contains(lockObject))
                {
                    lockObject.ClearNewFunctions();
                }
                else if (lockObject.HasNewFunctions())
                {
                    // if the lock has new functions then create a new code part for them
                    parts.Add(lockObject.GetNewFunctionsCodePart());
                }
            }

            _newLocks.Clear();

            return parts;
        }
    }

    class Lock
    {
        private static readonly List<string> _systemLocks = new List<string>() { "throttle", "steering", "wheelthrottle", "wheelsteering" };
        
        private CodePart _codePart;
        private Dictionary<int, LockFunction> _functions;
        private List<LockFunction> _newFunctions;

        public string Identifier;
        public string PointerIdentifier;
        public string DefaultLabel;

        public List<Opcode> InitializationCode
        {
            get { return _codePart.InitializationCode; }
        }

        public List<Opcode> MainCode
        {
            get { return _codePart.MainCode; }
        }

        
        public Lock()
        {
            _codePart = new CodePart();
            _functions = new Dictionary<int, LockFunction>();
            _newFunctions = new List<LockFunction>();
        }

        public Lock(string lockIdentifier)
            : this()
        {
            this.Identifier = lockIdentifier;
            this.PointerIdentifier = "$" + this.Identifier + "*";
            this.DefaultLabel = this.Identifier + "-default";
        }


        public bool IsInitialized()
        {
            return (_codePart.InitializationCode.Count > 0);
        }

        public List<Opcode> GetLockFunction(int expressionHash)
        {
            if (_functions.ContainsKey(expressionHash))
            {
                return _functions[expressionHash].Code;
            }
            else
            {
                LockFunction newLockFunction = new LockFunction();
                _functions.Add(expressionHash, newLockFunction);
                _newFunctions.Add(newLockFunction);
                return newLockFunction.Code;
            }
        }

        public CodePart GetCodePart()
        {
            CodePart mergedPart = new CodePart();
            mergedPart.InitializationCode = _codePart.InitializationCode;
            mergedPart.MainCode = _codePart.MainCode;
            
            foreach (LockFunction function in _functions.Values)
            {
                mergedPart.FunctionsCode.AddRange(function.Code);
            }

            return mergedPart;
        }

        public bool HasNewFunctions()
        {
            return (_newFunctions.Count > 0);
        }

        public void ClearNewFunctions()
        {
            _newFunctions.Clear();
        }

        public CodePart GetNewFunctionsCodePart()
        {
            CodePart newFunctionsPart = new CodePart();

            foreach (LockFunction function in _newFunctions)
            {
                newFunctionsPart.FunctionsCode.AddRange(function.Code);
            }

            ClearNewFunctions();
            return newFunctionsPart;
        }

        public bool IsSystemLock()
        {
            return _systemLocks.Contains(Identifier.ToLower());
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
        private Dictionary<string, Trigger> _triggers = new Dictionary<string, Trigger>();
        private List<Trigger> _newTriggers = new List<Trigger>();

        public bool Contains(string triggerIdentifier)
        {
            return _triggers.ContainsKey(triggerIdentifier);
        }

        public Trigger GetTrigger(string triggerIdentifier)
        {
            if (_triggers.ContainsKey(triggerIdentifier))
            {
                return _triggers[triggerIdentifier];
            }
            else
            {
                Trigger triggerObject = new Trigger(triggerIdentifier);
                _triggers.Add(triggerIdentifier, triggerObject);
                _newTriggers.Add(triggerObject);
                return triggerObject;
            }
        }

        public List<CodePart> GetParts(List<Trigger> triggers)
        {
            List<CodePart> parts = new List<CodePart>();

            foreach (Trigger triggerObject in triggers)
            {
                parts.Add(triggerObject.GetCodePart());
            }

            return parts;
        }

        public List<CodePart> GetParts()
        {
            return GetParts(_triggers.Values.ToList<Trigger>());
        }

        public List<CodePart> GetNewParts()
        {
            List<CodePart> parts = GetParts(_newTriggers);
            _newTriggers.Clear();
            return parts;
        }
    }

    class Trigger
    {
        private CodePart _codePart;
        public string Identifier;
        public string VariableName;
        public string VariableNameOldValue;
                
        public List<Opcode> Code
        {
            get { return _codePart.FunctionsCode; }
        }

        public Trigger()
        {
            _codePart = new CodePart();
        }

        public Trigger(string triggerIdentifier)
            : this()
        {
            this.Identifier = triggerIdentifier;
        }

        public bool IsInitialized()
        {
            return (_codePart.FunctionsCode.Count > 0);
        }

        public void SetTriggerVariable(string triggerVariable)
        {
            VariableName = "$" + triggerVariable;
            VariableNameOldValue = "$old-" + triggerVariable.ToLower();
        }

        public string GetFunctionLabel()
        {
            if (Code.Count > 0)
            {
                return Code[0].Label;
            }
            else
            {
                return string.Empty;
            }
        }

        public CodePart GetCodePart()
        {
            return _codePart;
        }
    }

    #endregion
}
