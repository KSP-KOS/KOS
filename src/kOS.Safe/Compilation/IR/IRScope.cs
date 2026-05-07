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
        private IRScope parent;
        private readonly HashSet<IRScope> childScopes = new HashSet<IRScope>();
        private readonly HashSet<BasicBlock> blocks = new HashSet<BasicBlock>();
        private readonly HashSet<string> variables = new HashSet<string>();

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
        public IReadOnlyCollection<string> Variables => variables;
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
        public bool IsGlobalScope => ParentScope == null;

        /// <summary>
        /// Initializes a new instance of the <see cref="IRScope"/> class.
        /// </summary>
        /// <param name="parent">The parent scope.</param>
        /// <param name="headerBlock">The first block in this scope.</param>
        public IRScope(IRScope parent, BasicBlock headerBlock)
        {
            ParentScope = parent;
            HeaderBlock = headerBlock;
        }

        public void StoreLocalVariable(string variableName)
        {
            variables.Add(variableName);
        }
        public void StoreGlobalVariable(string variableName)
        {
            if (!IsGlobalScope)
                ParentScope.StoreGlobalVariable(variableName);
            else
                StoreLocalVariable(variableName);
        }

        public bool IsVariableInScope(string name, bool includeParent = true)
        {
            if (variables.Contains(name))
                return true;
            return includeParent && (ParentScope?.IsVariableInScope(name, includeParent) ?? false);
        }

        public IRScope GetScopeForVariableNamed(string name)
        {
            if (IsGlobalScope)
                return this;
            if (variables.Contains(name))
                return this;
            return ParentScope.GetScopeForVariableNamed(name);
        }

        public void ClearVariable(string name)
        {
            variables.Remove(name);
        }

        public bool IsEqualOrEncompassedBy(IRScope scope)
            => this == scope || IsEncompassedBy(scope);
        public bool IsEncompassedBy(IRScope scope)
        {
            if (scope.IsGlobalScope)
                return true;
            if (scope.childScopes.Contains(this))
                return true;
            return IsGlobalScope || ParentScope.IsEncompassedBy(scope);
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
            if (IsGlobalScope)
                return "Global";
            if (ParentScope.IsGlobalScope)
                return $"{index}";
            return $"{ParentScope.IndexString()}.{index}";
        }
    }
}
