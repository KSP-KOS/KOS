using kOS.Safe.Binding;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Binding
{
    public class BaseBindingManager : IBindingManager
    {
        private readonly SafeSharedObjects shared;
        private readonly List<kOS.Safe.Binding.SafeBindingBase> bindings = new List<kOS.Safe.Binding.SafeBindingBase>();
        private readonly Dictionary<string, BoundVariable> variables;

        // Note: When we were using .Net 3.5, This used to be a Dictionary rather than a HashSet of pairs.  But that had to
        // change because of one .Net 4.x change to how reflection on Attributes works.  In .Net 3.5, an Attribute called
        // [Foo(1,2)] attached to classA was considered un-equal to an attribute with the same values ([Foo(1,2)]) attached
        // to classB.  But in .Net 4.0, which class the attribute is attached to is no longer part of its equality test,
        // therefore both those examples would be "equal" classes because they are the same name Foo with the same paramters (1,2).
        // This meant that when we had many classes decorated with exactly the same thing, [Binding("ksp")], these Attributes
        // could be unique keys in a Dictionary in .Net 3.5 because they weren't attached to the same class, but in .Net 4.0
        // they became key clashes because they were now considered "equal" and all such Attributes after the first were
        // refusing to be stored in the dictionary.
        private static readonly HashSet<KeyValuePair<BindingAttribute, Type>> rawAttributes = new HashSet<KeyValuePair<BindingAttribute, Type>>();

        public BaseBindingManager(SafeSharedObjects shared)
        {
            variables = new Dictionary<string, BoundVariable>(StringComparer.OrdinalIgnoreCase);
            this.shared = shared;
            this.shared.BindingMgr = this;
        }

        public void Load()
        {
            var contexts = new string[1];
            contexts[0] = "ksp";

            bindings.Clear();
            variables.Clear();

            foreach (KeyValuePair<BindingAttribute, Type> attrTypePair in rawAttributes)
            {
                var type = attrTypePair.Value;
                if (attrTypePair.Key.Contexts.Any() && !attrTypePair.Key.Contexts.Intersect(contexts).Any()) continue;
                var instanceWithABinding = (SafeBindingBase)Activator.CreateInstance(type);
                instanceWithABinding.AddTo(shared);
                bindings.Add(instanceWithABinding);

                LoadInstanceWithABinding(instanceWithABinding);
            }
        }

        protected virtual void LoadInstanceWithABinding(SafeBindingBase instanceWithABinding)
        {

        }

        public static void RegisterMethod(BindingAttribute attr, Type type)
        {
            KeyValuePair<BindingAttribute, Type> attrTypePair = new KeyValuePair<BindingAttribute, Type>(attr, type);
            if (attr != null && !rawAttributes.Contains(attrTypePair))
            {
                rawAttributes.Add(attrTypePair);
            }
        }

        public void AddBoundVariable(string name, BindingGetDlg getDelegate, BindingSetDlg setDelegate)
        {
            BoundVariable variable;
            if (variables.ContainsKey(name))
            {
                variable = variables[name];
            }
            else
            {
                variable = new BoundVariable
                {
                    Name = name,
                };
                variables.Add(name, variable);
                shared.Cpu.AddVariable(variable, name, false);
            }

            if (getDelegate != null)
                variable.Get = getDelegate;

            if (setDelegate != null)
                variable.Set = setDelegate;
        }

        public void AddGetter(string name, BindingGetDlg dlg)
        {
            AddBoundVariable(name, dlg, null);
        }

        public void AddGetter(IEnumerable<string> names, BindingGetDlg dlg)
        {
            foreach (var name in names)
            {
                AddBoundVariable(name, dlg, null);
            }
        }

        public void AddSetter(string name, BindingSetDlg dlg)
        {
            AddBoundVariable(name, null, dlg);
        }

        public void AddSetter(IEnumerable<string> names, BindingSetDlg dlg)
        {
            foreach (var name in names)
            {
                AddBoundVariable(name, null, dlg);
            }
        }

        public bool HasGetter(string name)
        {
            return variables.ContainsKey(name);
        }

        public void PreUpdate()
        {
            foreach (var variable in variables)
            {
                variable.Value.ClearCache();
            }
            // update the bindings
            foreach (var b in bindings)
            {
                b.Update();
            }
        }

        public void PostUpdate()
        {
        }

        public virtual void ToggleFlyByWire(string paramName, bool enabled)
        {
        }

        public virtual void SelectAutopilotMode(string autopilotMode)
        {
        }
    }
}