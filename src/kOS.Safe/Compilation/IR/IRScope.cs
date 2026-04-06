using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class represents a variable scope, analogous to <see cref="kOS.Safe.Execution.VariableScope"/>.
    /// </summary>
    public class IRScope
    {
        private readonly Dictionary<string, IRVariableBase> variables =
            new Dictionary<string, IRVariableBase>(StringComparer.OrdinalIgnoreCase);
        private IRScope parent;
        private readonly HashSet<IRScope> childScopes = new HashSet<IRScope>();
        private readonly HashSet<BasicBlock> blocks = new HashSet<BasicBlock>();
        private readonly Dictionary<string, string> functionRefs = new Dictionary<string, string>();

        private int nextChildIndex = 0;
        private int index;

        /// <summary>
        /// Gets or sets the parent scope.
        /// </summary>
        public IRScope ParentScope
        {
            get => parent;
            set
            {
                parent?.childScopes.Remove(this);
                parent = value;
                if (parent != null)
                {
                    parent.childScopes.Add(this);
                    index = parent.nextChildIndex++;
                }
                else
                    index = 0;
            }
        }
        /// <summary>
        /// Gets the collection of child scopes.
        /// </summary>
        public IReadOnlyCollection<IRScope> Children => childScopes;
        /// <summary>
        /// Gets the collection of blocks that associate with this scope.
        /// </summary>
        public IReadOnlyCollection<BasicBlock> Blocks => blocks;
        /// <summary>
        /// Gets or sets the block where this scope is pushed upon entered.
        /// </summary>
        public BasicBlock HeaderBlock { get; set; }
        /// <summary>
        /// Gets or sets the block where this scope is popped upon exiting.
        /// </summary>
        public BasicBlock FooterBlock { get; set; }
        /// <summary>
        /// Gets the collection of variables associated with this scope.
        /// </summary>
        public IReadOnlyCollection<IRVariableBase> Variables => variables.Values;
        /// <summary>
        /// Gets the collection of variable names associated with this
        /// scope.
        /// </summary>
        public IReadOnlyCollection<string> VariableNames => variables.Keys;
        /// <summary>
        /// Gets the collection of variables written to within this scope.
        /// This differs from <see cref="Variables"/> in that it can
        /// contain multiple SSA variables of the same base name.
        /// </summary>
        public HashSet<IRVariableBase> VariablesWritten { get; } = new HashSet<IRVariableBase>();
        /// <summary>
        /// Gets a value indicating whether this instance represents the
        /// global scope.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance represents the global scope; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Multiple instances can represent the global scope
        /// simultaneously. The global scope implicitly contains every
        /// variable name.
        /// </remarks>
        public bool IsGlobalScope { get; internal set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="IRScope"/> class.
        /// </summary>
        /// <param name="parent">The parent scope.</param>
        public IRScope(IRScope parent)
        {
            ParentScope = parent;
        }

        public void StoreLocalVariable(IRVariableBase variable)
        {
            variables[variable.Name] = variable;
        }
        public void StoreGlobalVariable(IRVariableBase variable)
        {
            if (!IsGlobalScope)
                ParentScope.StoreGlobalVariable(variable);
            else
                StoreLocalVariable(variable);
        }
        public void StoreVariable(IRVariableBase variable)
        {
            if (!TryStoreVariable(variable))
                StoreGlobalVariable(variable);
        }
        public bool TryStoreVariable(IRVariableBase variable)
        {
            if (variables.ContainsKey(variable.Name))
            {
                StoreLocalVariable(variable);
                return true;
            }
            return ParentScope?.TryStoreVariable(variable) ?? false;
        }

        public void EnrollFunction(string variable, string functionRef)
        {
            functionRefs[variable] = functionRef.Split('-').First();
        }
        public string GetFunctionNameFromVariable(string variable)
        {
            if (GetScopeForVariableNamed(variable).functionRefs.TryGetValue(variable, out var functionRef))
                return functionRef;
            return null;
        }

        public IRVariableBase GetVariableNamed(string name)
        {
            if (variables.ContainsKey(name))
                return variables[name];
            return ParentScope?.GetVariableNamed(name);
        }

        public bool IsVariableInScope(string name)
        {
            if (variables.ContainsKey(name))
                return true;
            return ParentScope?.IsVariableInScope(name) ?? false;
        }
        public bool IsVariableInScope(IRVariableBase variable)
        {
            return variable.Scope.IsEqualOrEncompassedBy(this);
        }

        public IRScope GetScopeForVariableNamed(string name)
        {
            if (IsGlobalScope)
                return this;
            if (variables.ContainsKey(name))
                return this;
            return ParentScope.GetScopeForVariableNamed(name);
        }

        public void ClearVariable(string name)
        {
            variables.Remove(name);
        }
        public void ClearVariable(IRVariableBase variable)
            => ClearVariable(variable.Name);

        public bool IsEqualOrEncompassedBy(IRScope scope)
            => this == scope || IsEncompassedBy(scope);
        public bool IsEncompassedBy(IRScope scope)
        {
            if (scope.IsGlobalScope)
                return true;
            if (scope.childScopes.Contains(this))
                return true;
            return ParentScope?.IsEncompassedBy(scope) ?? false;
        }

        public IRScope GetGlobalScope()
        {
            if (IsGlobalScope)
                return this;
            return ParentScope.GetGlobalScope();
        }

        public void EnrollBlock(BasicBlock block)
        {
            block.Scope?.RemoveBlock(block);
            blocks.Add(block);
        }
        public void RemoveBlock(BasicBlock block)
            => blocks.Remove(block);

        public override string ToString()
            => $"IRScope: {IndexString()}";
        public string IndexString()
        {
            if (IsGlobalScope || ParentScope == null)
                return "Global";
            if (ParentScope.IsGlobalScope)
                return $"{index}";
            return $"{ParentScope.IndexString()}.{index}";
        }
    }
}
