using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;


namespace kOS
{
    public class kOSBinding : Attribute
    {
        public string[] Contexts;
        public kOSBinding(params string[] contexts) { Contexts = contexts; }
    }
    
    public class BindingManager
    {
        public CPU cpu;

        public Dictionary<String, BindingSetDlg> Setters = new Dictionary<String, BindingSetDlg>();
        public Dictionary<String, BindingGetDlg> Getters = new Dictionary<String, BindingGetDlg>();
        public List<Binding> Bindings = new List<Binding>();
        
        public delegate void BindingSetDlg      (CPU cpu, object val);
        public delegate object BindingGetDlg    (CPU cpu);

        public BindingManager(CPU cpu, String context)
        {
            this.cpu = cpu;

            var contexts = new string[1];
            contexts[0] = context;

            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                kOSBinding attr = (kOSBinding)t.GetCustomAttributes(typeof(kOSBinding), true).FirstOrDefault();
                if (attr != null)
                {
                    if (attr.Contexts.Count() == 0 || attr.Contexts.Intersect(contexts).Any())
                    {
                        Binding b = (Binding)Activator.CreateInstance(t);
                        b.AddTo(this);
                        Bindings.Add(b);
                    }
                }
            }
        }

        public void AddGetter(String name, BindingGetDlg dlg)
        {
            Variable v = cpu.FindVariable(name);
            if (v == null) v = cpu.FindVariable(name.Split(":".ToCharArray())[0]);
            
            if (v != null)
            {
                if (v is BoundVariable)
                {
                    ((BoundVariable)v).Get = dlg;
                }
            }
            else
            {
                var bv = cpu.CreateBoundVariable(name);
                bv.Get = dlg;
                bv.cpu = cpu;
            }
        }

        public void AddSetter(String name, BindingSetDlg dlg)
        {
            Variable v = cpu.FindVariable(name.ToLower());
            if (v != null)
            {
                if (v is BoundVariable)
                {
                    ((BoundVariable)v).Set = dlg;
                }
            }
            else
            {
                var bv = cpu.CreateBoundVariable(name.ToLower());
                bv.Set = dlg;
                bv.cpu = cpu;
            }
        }

        public void Update(float time)
        {
            foreach (Binding b in Bindings)
            {
                b.Update(time);
            }
        }
    }

    public class Binding
    {
        public virtual void AddTo(BindingManager manager) { }

        public virtual void Update(float time) { }
    }

    public class BoundVariable : Variable
    {
        public BindingManager.BindingSetDlg Set;
        public BindingManager.BindingGetDlg Get;
        public CPU cpu;

        public override object Value
        {
            get
            {
                return Get(cpu);
            }
            set
            {
                Set(cpu, value);
            }
        }
    }

    [kOSBinding]
    public class TestBindings : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            //manager.AddGetter("TEST1", delegate(CPU cpu) { return 4; });
            //manager.AddSetter("TEST1", delegate(CPU cpu, object val) { cpu.PrintLine(val.ToString()); });
        }
    }
}
