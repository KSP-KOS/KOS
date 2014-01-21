using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using kOS.Context;

namespace kOS.Binding
{
    public delegate void BindingSetDlg(ICPU cpu, object val);
    public delegate object BindingGetDlg(ICPU cpu);
    public class BindingManager : IBindingManager
    {
        private readonly List<IBinding> bindings = new List<IBinding>();


        public BindingManager(ICPU cpu, string context)
        {
            Cpu = cpu;

            var contexts = new string[1];
            contexts[0] = context;

            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = (KOSBinding)t.GetCustomAttributes(typeof(KOSBinding), true).FirstOrDefault();

                if (attr == null || attr.Contexts.Any() && !attr.Contexts.Intersect(contexts).Any()) continue;
                var b = (IBinding)Activator.CreateInstance(t);
                b.BindTo(this);
                bindings.Add(b);
            }
        }

        public ICPU Cpu { get; set; }

        public void AddGetter(string name, BindingGetDlg dlg)
        {
            var v = Cpu.FindVariable(name) ?? Cpu.FindVariable(name.Split(":".ToCharArray())[0]);

            if (v != null)
            {
                var variable = v as BoundVariable;
                if (variable != null)
                {
                    variable.Get = dlg;
                }
            }
            else
            {
                var bv = Cpu.CreateBoundVariable(name);
                bv.Get = dlg;
                bv.Cpu = Cpu;
            }
        }

        public void AddSetter(string name, BindingSetDlg dlg)
        {
            var v = Cpu.FindVariable(name.ToLower());
            if (v != null)
            {
                var variable = v as BoundVariable;
                if (variable != null)
                {
                    variable.Set = dlg;
                }
            }
            else
            {
                var bv = Cpu.CreateBoundVariable(name.ToLower());
                bv.Set = dlg;
                bv.Cpu = Cpu;
            }
        }

        public void Update(float time)
        {
            foreach (var b in bindings)
            {
                b.Update(time);
            }
        }
    }
}