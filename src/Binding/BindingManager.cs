using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using kOS.Execution;

namespace kOS.Binding
{
    public class BindingManager : IDisposable
    {
        private readonly SharedObjects _shared;
        private readonly List<Binding> _bindings = new List<Binding>();
        private readonly Dictionary<string, BoundVariable> _vars = new Dictionary<string, BoundVariable>();
        private FlightControlManager _flightControl;

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

            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = (kOSBinding)t.GetCustomAttributes(typeof(kOSBinding), true).FirstOrDefault();
                if (attr == null) continue;
                if (attr.Contexts.Any() && !attr.Contexts.Intersect(contexts).Any()) continue;

                var b = (Binding)Activator.CreateInstance(t);
                b.AddTo(_shared);
                _bindings.Add(b);

                var manager = b as FlightControlManager;
                if (manager != null)
                {
                    _flightControl = manager;
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
                variable = new BoundVariable
                    {
                        Name = name, 
                        Cpu = _shared.Cpu
                    };
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

        public void PreUpdate()
        {
            // update the bindings
            foreach (var b in _bindings)
            {
                b.Update();
            }

            // clear bound variables values
            foreach (var variable in _vars.Values)
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

        public void UnBindAll()
        {
            _flightControl.UnBind();
        }

        public void Dispose()
        {
            _flightControl.Dispose();
        }
    }
}
