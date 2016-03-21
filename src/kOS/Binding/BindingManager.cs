using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using kOS.Safe.Binding;
using kOS.Safe.Utilities;

namespace kOS.Binding
{
    [AssemblyWalk(AttributeType = typeof(BindingAttribute), StaticRegisterMethod = "RegisterMethod")]
    public class BindingManager : IDisposable, IBindingManager
    {
        private readonly SharedObjects shared;
        private readonly List<kOS.Safe.Binding.SafeBinding> bindings = new List<kOS.Safe.Binding.SafeBinding>();
        private readonly Dictionary<string, BoundVariable> variables;
        private static readonly Dictionary<BindingAttribute, Type> rawAttributes = new Dictionary<BindingAttribute, Type>();
        private FlightControlManager flightControl;


        public BindingManager(SharedObjects shared)
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
            flightControl = null;

            foreach (BindingAttribute attr in rawAttributes.Keys)
            {
                var t = rawAttributes[attr];
                if (attr.Contexts.Any() && !attr.Contexts.Intersect(contexts).Any()) continue;
                var b = (kOS.Safe.Binding.SafeBinding)Activator.CreateInstance(t);
                b.AddTo(shared);
                bindings.Add(b);

                var manager = b as FlightControlManager;
                if (manager != null)
                {
                    flightControl = manager;
                }
            }
        }

        public static void RegisterMethod(BindingAttribute attr, Type type)
        {
            if (attr != null && !rawAttributes.ContainsKey(attr))
            {
                rawAttributes.Add(attr, type);
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

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (flightControl != null)
            {
                flightControl.ToggleFlyByWire(paramName, enabled);
            }
        }

        public void UnBindAll()
        {
            if (flightControl != null)
            {
                flightControl.UnBind ();
            }
        }

        public void Dispose()
        {
            if (flightControl != null)
            {
                flightControl.Dispose();
            }
        }

        public void SelectAutopilotMode(string autopilotMode)
        {
            if (flightControl != null)
            {
                flightControl.SelectAutopilotMode(autopilotMode);
            }
        }
    }
}
