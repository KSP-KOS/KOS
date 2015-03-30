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
        
        /// <summary>
        /// The unique ID of this variable scope's parent scope.
        /// An ID of 0 means global.
        /// </summary>
        public Int16 ParentScopeId {get;set;}

        /// <summary>
        /// Once the ParentId has been used once to detect which VaraibleScope
        /// is the parent of this scope, you can store the result here so you
        /// can jump there quicker next time without scanning the scope stack.
        /// </summary>
        public Int16 ParentSkipLevels {get;set;}

        public Dictionary<string, Variable>  Variables;
        
        public VariableScope(Int16 scopeId, Int16 parentScopeId)
        {
            ScopeId = scopeId;
            ParentScopeId = parentScopeId;
            ParentSkipLevels = 1; // the default case is to just move one stack level.
            Variables = new Dictionary<string, Variable>();
        }
    }
}
