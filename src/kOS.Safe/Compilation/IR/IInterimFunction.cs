using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This interface provides common accessibility for
    /// User Functions and Anonymous Functions when in
    /// interim representation.
    /// </summary>
    /// <seealso cref="IRFunction"/>
    /// <seealso cref="IRAnonymousFunction"/>
    /// <seealso cref="IClosureVariableUser" />
    public interface IInterimFunction : IClosureVariableUser
    {
        string Identifier { get; }
        bool IsRecursive { get; }
        PhiOperand Returns { get; }
        /// <summary>
        /// Gets a value indicating whether this instance is invariant.
        /// A user function must also be inert to be considered invariant.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is invariant; otherwise, <c>false</c>.
        /// </value>
        bool IsInvariant { get; }
        /// <summary>
        /// Gets a value indicating whether this instance is inert.
        /// A user function that contains any calls to non-inert functions is,
        /// itself, not inert. Any assignments or unsets to the enclosing scope or
        /// setting any suffixes or indexes also makes a function non-inert.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is inert; otherwise, <c>false</c>.
        /// </value>
        bool IsInert { get; }
        /// <summary>
        /// Gets a value indicating whether this instance is inert by
        /// its own content, nested calls to other user functions notwithstanding.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is, itself, inert; otherwise, <c>false</c>.
        /// </value>
        bool IsSelfInert { get; }
        HashSet<IRCall> CallSites { get; }
    }

    public readonly struct InterimUserFunction : IInterimOperand
    {
        public IInterimVariableReference VariableReference { get; }
        public string Name { get; }
        public IInterimFunction Function { get; }
        public bool IsRecursive =>Function?.IsRecursive ?? true;
        public bool IsInvariant => Function?.IsInvariant ?? false;
        public bool IsInert => Function?.IsInert ?? false;
        public Type Type => Function?.Returns.Type ?? typeof(Encapsulation.Structure);
        public short SourceLine { get; }
        public short SourceColumn { get; }


        public InterimUserFunction(IInterimVariableReference variableReference, IInterimFunction function, IRInstruction call)
        {
            VariableReference = variableReference;
            Name = variableReference.Name;
            Function = function;
            SourceLine = call.SourceLine;
            SourceColumn = call.SourceColumn;
        }
        public InterimUserFunction(IInterimFunction function, IRInstruction call)
        {
            VariableReference = null;
            Name = function?.Identifier;
            Function = function;
            SourceLine = call.SourceLine;
            SourceColumn = call.SourceColumn;
        }
        private InterimUserFunction(InterimUserFunction cloneFrom)
        {
            VariableReference = null;
            Name = cloneFrom.Name;
            Function = cloneFrom.Function;
            SourceLine = cloneFrom.SourceLine;
            SourceColumn = cloneFrom.SourceColumn;
        }

        public IInterimOperand Clone(BasicBlock block, bool maintainSSAReferences = false)
            => new InterimUserFunction(this);

        public IEnumerable<Opcode> EmitOpcodes()
            => Enumerable.Empty<Opcode>();

        public bool Equals(IInterimOperand other)
            => other is InterimUserFunction userFunc && userFunc.Function == Function;
    }
    public readonly struct InterimBuiltInFunction : IInterimOperand
    {
        public string Name { get; }
        public bool IsRecursive => false;
        public bool IsInvariant { get; }
        public bool IsInert { get; }
        public Type Type { get; }
        public short SourceLine { get; }
        public short SourceColumn { get; }

        public InterimBuiltInFunction(string name, IRInstruction call)
        {
            name = name.Replace("()", "");
            if (!Optimization.Optimizer.FunctionManager.Exists(name))
                throw new Exceptions.KOSNotInvokableException(name);
            Name = name;
            IsInvariant = Optimization.Optimizer.FunctionManager.IsFunctionInvariant(name);
            IsInert = Optimization.Optimizer.FunctionManager.IsFunctionInert(name);
            Type = Optimization.Optimizer.FunctionManager.FunctionReturnType(name);
            SourceLine = call.SourceLine;
            SourceColumn = call.SourceColumn;
        }
        private InterimBuiltInFunction(InterimBuiltInFunction cloneFrom)
        {
            Name = cloneFrom.Name;
            IsInvariant = cloneFrom.IsInvariant;
            IsInert= cloneFrom.IsInert;
            Type = cloneFrom.Type;
            SourceLine = cloneFrom.SourceLine;
            SourceColumn = cloneFrom.SourceColumn;
        }

        public IInterimOperand Clone(BasicBlock block, bool maintainSSAReferences = false)
            => new InterimBuiltInFunction(this);

        public IEnumerable<Opcode> EmitOpcodes()
            => Enumerable.Empty<Opcode>();

        public bool Equals(IInterimOperand other)
            => other is InterimBuiltInFunction builtIn && builtIn.Name.Equals(Name, StringComparison.OrdinalIgnoreCase);

        public InterimConstantValue Evaluate(IEnumerable<IInterimOperand> arguments)
        {
            Optimization.InterimCPU interimCPU = Optimization.Optimizer.InterimCPU;
            interimCPU.Boot();  // Clear the stack out of caution.
            interimCPU.PushArgumentStack(new Execution.KOSArgMarkerType());
            foreach (IInterimOperand arg in arguments)
            {
                object argValue = (arg as IEvaluatableToConstant)?.Evaluate().Value
                    ?? throw new ArgumentNullException(arg.ToString());
                interimCPU.PushArgumentStack(argValue);
            }
            Optimization.Optimizer.FunctionManager.CallFunction(Name);
            return new InterimConstantValue(interimCPU.PopValueArgument(), SourceLine, SourceColumn);
        }
    }
}
