using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public class IRScope
    {
        private readonly Dictionary<string, IRVariableBase> variables =
            new Dictionary<string, IRVariableBase>(StringComparer.OrdinalIgnoreCase);
        private IRScope parent;
        private readonly HashSet<IRScope> childScopes = new HashSet<IRScope>();
        private readonly HashSet<BasicBlock> blocks = new HashSet<BasicBlock>();

        private int nextChildIndex = 0;
        private int index;

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
        public IReadOnlyCollection<IRScope> Children => childScopes;
        public IReadOnlyCollection<BasicBlock> Blocks => blocks;
        public BasicBlock HeaderBlock { get; set; }
        public BasicBlock FooterBlock { get; set; }
        public IReadOnlyCollection<string> Variables => variables.Keys;
        public bool IsGlobalScope { get; internal set; } = false;

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
                variables[variable.Name] = variable;
                return true;
            }
            return ParentScope?.TryStoreVariable(variable) ?? false;
        }

        public IRVariableBase GetVariable(string name)
        {
            if (variables.ContainsKey(name))
                return variables[name];
            return ParentScope?.GetVariable(name);
        }

        public bool IsVariableInScope(string name)
        {
            if (variables.ContainsKey(name))
                return true;
            return ParentScope?.IsVariableInScope(name) ?? false;
        }

        public IRScope GetScopeForVariableNamed(string name)
        {
            if (IsGlobalScope)
                return this;
            if (variables.ContainsKey(name))
                return this;
            return ParentScope.GetScopeForVariableNamed(name);
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
        {
            if (IsGlobalScope)
                return "IRScope: Global";
            return $"IRScope: {IndexString()}";
        }
        private string IndexString()
        {
            if (ParentScope == null)
                return $"{index}";
            return $"{ParentScope.IndexString()}.{index}";
        }
    }
}
