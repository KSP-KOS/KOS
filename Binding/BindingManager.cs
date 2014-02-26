using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using kOS.Execution;

namespace kOS.Binding
{
    public class BindingManager
    {
        private SharedObjects _shared;
        private List<Binding> _bindings = new List<Binding>();
        private Dictionary<string, BoundVariable> _vars = new Dictionary<string, BoundVariable>();
        private BindingFlightControls _flightControl = null;

        public delegate void BindingSetDlg(CPU cpu, object val);
        public delegate object BindingGetDlg(CPU cpu);

        public BindingManager(SharedObjects shared)
        {
            _shared = shared;
            _shared.BindingMgr = this;
        }

        public void LoadBindings()
        {
            var contexts = new string[1];
            contexts[0] = "ksp";

            _bindings.Clear();
            _vars.Clear();
            _flightControl = null;

            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                kOSBinding attr = (kOSBinding)t.GetCustomAttributes(typeof(kOSBinding), true).FirstOrDefault();
                if (attr != null)
                {
                    if (attr.Contexts.Count() == 0 || attr.Contexts.Intersect(contexts).Any())
                    {
                        Binding b = (Binding)Activator.CreateInstance(t);
                        b.AddTo(_shared);
                        _bindings.Add(b);

                        if (b is BindingFlightControls)
                        {
                            _flightControl = (BindingFlightControls)b;
                        }
                    }
                }
            }
        }

        public void AddBoundVariable(string name, BindingGetDlg getDelegate, BindingSetDlg setDelegate)
        {
            BoundVariable variable;
            if (_vars.ContainsKey(name))
            {
                variable = _vars[name];
            }
            else
            {
                variable = new BoundVariable();
                variable.Name = name;
                variable.cpu = _shared.Cpu;
                _vars.Add(name, variable);
                _shared.Cpu.AddVariable(variable, name);
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

        public void PreUpdate(double time)
        {
            // update the bindings
            foreach (Binding b in _bindings)
            {
                b.Update();
            }

            // clear bound variables values
            foreach (BoundVariable variable in _vars.Values)
            {
                variable.ClearValue();
            }
        }

        public void PostUpdate()
        {
            // save bound variables values
            foreach (BoundVariable variable in _vars.Values)
            {
                variable.SaveValue();
            }
        }

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (_flightControl != null)
            {
                _flightControl.ToggleFlyByWire(paramName, enabled);
            }
        }
    }
}
