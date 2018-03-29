using System;
using System.Collections.Generic;

namespace kOS.Safe.Execution
{
    public interface IStack
    {
        /// <summary>
        /// Current size of argument stack
        /// </summary>
        int ArgumentCount { get; }
        /// <summary>
        /// Current size of scope stack
        /// </summary>
        int ScopeCount { get; }
        /// <summary>
        /// All arguments on the stack (from top to bottom)
        /// </summary>
        IEnumerable<object> Arguments { get; }
        /// <summary>
        /// All arguments on the stack from bottom to top
        /// </summary>
        IEnumerable<object> ArgumentsFromBottom { get; }
        /// <summary>
        /// All scopes on the stack (from top to bottom)
        /// </summary>
        IEnumerable<object> Scopes { get; }
        /// <summary>
        /// All scopes on the stack from bottom to top
        /// </summary>
        IEnumerable<object> ScopesFromBottom { get; }

        void PushArgument(object item);
        object PopArgument();
        object PeekArgument(int digDepth);
        bool PeekCheckArgument(int digDepth, out object item);
        object PeekScope(int digDepth);
        bool PeekCheckScope(int digDepth, out object item);
        void PushScope(object item);
        object PopScope();
        int GetArgumentStackSize();
        void Clear();
        string Dump();
        List<int> GetCallTrace();
        bool HasTriggerContexts();
        VariableScope FindScope(Int16 scopeId);
        VariableScope GetCurrentScope();
        SubroutineContext GetCurrentSubroutineContext();
        List<SubroutineContext> GetTriggerCallContexts(TriggerInfo trigger);
    }
}
