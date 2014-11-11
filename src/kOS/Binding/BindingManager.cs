using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using kOS.Execution;
using kOS.Safe.Binding;

namespace kOS.Binding
{
    public class BindingManager : IDisposable
    {
        private readonly SharedObjects shared;
        private readonly List<Binding> bindings = new List<Binding>();
        private readonly Dictionary<string, BoundVariable> vars = new Dictionary<string, BoundVariable>();
        private FlightControlManager flightControl;

        public delegate void BindingSetDlg(CPU cpu, object val);
        public delegate object BindingGetDlg(CPU cpu);

        public BindingManager(SharedObjects shared)
        {
            this.shared = shared;
            this.shared.BindingMgr = this;
        }

        public void LoadBindings()
        {
            var contexts = new string[1];
            contexts[0] = "ksp";

            bindings.Clear();
            vars.Clear();
            flightControl = null;

            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = (kOSBinding)t.GetCustomAttributes(typeof(kOSBinding), true).FirstOrDefault();
                if (attr == null) continue;
                if (attr.Contexts.Any() && !attr.Contexts.Intersect(contexts).Any()) continue;

                var b = (Binding)Activator.CreateInstance(t);
                b.AddTo(shared);
                bindings.Add(b);

                var manager = b as FlightControlManager;
                if (manager != null)
                {
                    flightControl = manager;
                }
            }
        }

        public void AddBoundVariable(string name, BindingGetDlg getDelegate, BindingSetDlg setDelegate)
        {
            BoundVariable variable;
            if (vars.ContainsKey(name))
            {
                variable = vars[name];
            }
            else
            {
                variable = new BoundVariable
                    {
                        Name = name, 
                        Cpu = (CPU)shared.Cpu
                    };
                vars.Add(name, variable);
                shared.Cpu.AddVariable(variable, name);
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

        public void AddSetter(string name, BindingSetDlg dlg)
        {
            AddBoundVariable(name, null, dlg);
        }

        public void PreUpdate()
        {
            // update the bindings
            foreach (var b in bindings)
            {
                b.Update();
            }

            // clear bound variables values
            foreach (var variable in vars.Values)
            {
                variable.ClearValue();
            }
        }

        public void PostUpdate()
        {
            // save bound variables values
            foreach (BoundVariable variable in vars.Values)
            {
                variable.SaveValue();
            }
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
            flightControl.UnBind();
        }

        public void Dispose()
        {
            flightControl.Dispose();
        }
    }
}
