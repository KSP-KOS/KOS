using System;
using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// Represents a user of a closure and its contained variables.
    /// </summary>
    public interface IClosureVariableUser
    {
        /// <summary>
        /// Gets the collection of external variables that are read
        /// within the closure.
        /// </summary>
        HashSet<string> ExternalReads { get; set; }
        /// <summary>
        /// Gets the collection of external variables that may be written
        /// to by this instance.
        /// </summary>
        HashSet<string> ExternalWrites { get; }
        /// <summary>
        /// Gets the collection of external variables that may be unset by this instance.
        /// </summary>
        HashSet<(string Name, IRUnset Instruction)> ExternalUnsets { get; }
        /// <summary>
        /// Gets the collection of triggers that could be created from this instance.
        /// </summary>
        HashSet<IRTrigger> TriggersCreated { get; }
        /// <summary>
        /// Gets the functions called from within this instance's body.
        /// </summary>
        HashSet<IInterimFunction> FunctionCalls { get; }
        /// <summary>
        /// Gets the function call sites that could not be
        /// resolved to a specific function.
        /// </summary>
        HashSet<IRInstruction> UnresolvedCallSites { get; }
        /// <summary>
        /// Gets the scope of the closure this instance uses.
        /// </summary>
        IRScope ClosureScope { get; }
        BasicBlock TerminalBlock { get; }
    }
}
