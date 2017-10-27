using System;
using System.Collections.Generic;

namespace kOS.Safe.Execution
{
    /// <summary>
    /// A VariableScope Object is a dictionary mapping a variable name to
    /// a variable's contents.  It contains an id and a parent id that are
    /// used to remember the lexical scoping, which may differ from the
    /// runtime scoping when subroutines call other subroutines, jumping to
    /// sibling scoping levels.
    /// </summary>
    public class VariableScope
    {
        /// <summary>
        /// A unique ID of this variable scope object
        /// An ID of 0 means global.
        /// </summary>
        public Int16 ScopeId {get;set;}

        public VariableScope ParentScope {get;set;}
        
        /// <summary>
        /// Set this to true to indicate that this scope is part of a closure
        /// call.  That lets OpcodePushStack and OpcodePopStack to know it
        /// needs to be treated specially.
        /// </summary>
        public bool IsClosure {get;set;}

        private Dictionary<string, Variable>  Variables;
        
        public VariableScope(Int16 scopeId, VariableScope parentScope)
        {
            ScopeId = scopeId;
            ParentScope = parentScope;
            Variables = new Dictionary<string, Variable>(StringComparer.OrdinalIgnoreCase);
            IsClosure = false;
        }

        public IEnumerable<KeyValuePair<string, Variable>> Locals
        {
            get
            {
                return Variables;
            }
        }

        public void Clear()
        {
            Variables.Clear();
        }

        public void Add(string name, Variable value)
        {
            Variables[name] = value;
        }

        public bool Remove(string name)
        {
            return Variables.Remove(name);
        }

        public Variable RemoveNested(string name)
        {
            Variable res = null;

            if (!Variables.TryGetValue(name, out res))
            {
                return ParentScope.RemoveNested(name);
            }
            Variables.Remove(name);

            return res;
        }

        public bool Contains(string name)
        {
            return Variables.ContainsKey(name);
        }

        public Variable GetNested(string name)
        {
            Variable res;
            if (!Variables.TryGetValue(name, out res) && ParentScope != null)
            {
                res = ParentScope.GetNested(name);
            }
            return res;
        }

        public Variable GetLocal(string name)
        {
            Variable res = null;

            // Just return null if this doesn't fill it
            Variables.TryGetValue(name, out res);

            return res;
        }
    }
}
